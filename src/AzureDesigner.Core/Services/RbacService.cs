using System.Text.Json;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Authorization;
using Azure.ResourceManager.Authorization.Models;

namespace AzureDesigner.Services;

public interface IRbacService
{
    Task<IEnumerable<string>> GetClientIdsWithRbacAsync(string resourceId, Guid roleDefGuid);
    Task AddClientIdWithRbacAsync(string resourceId, Guid roleDefGuid, string clientId);
}

public class RbacService : IRbacService
{
    readonly ICredentialFactory _credentialFactory;
    public RbacService(ICredentialFactory credentialFactory)
    {
        _credentialFactory = credentialFactory;
    }

    public async Task<IEnumerable<string>> GetClientIdsWithRbacAsync(string resourceId, Guid roleDefGuid)
    {
        /*
        Pseudocode (detailed plan for modification):
        - Introduce a constant for the built-in Azure Role Definition ID of "Key Vault Secrets User"
          GUID: 4633458b-17de-408a-b874-0445c86b69e6
        - In GetVaultManagedIdentityIdsWithRbacAccess:
            * Create credential and ArmClient (already present)
            * Enumerate role assignments at vault scope
            * For each assignment:
                - Ensure PrincipalType == ServicePrincipal
                - Ensure RoleDefinitionId is not null
                - Filter: RoleDefinitionId ends with the target GUID (case-insensitive) since the full id includes subscription scope
                - Add PrincipalId (ObjectId) to a HashSet to avoid duplicates
            * Return the HashSet as IEnumerable<string>
        - Keep existing signature and attributes
        - No other behavioral changes to other methods
       */

        if (string.IsNullOrWhiteSpace(resourceId))
            return [];

        var credential = _credentialFactory.CreateCredential();
        var armClient = new ArmClient(credential);
        var resoureIdentifier = new ResourceIdentifier(resourceId);

        var roleAssignments = armClient.GetRoleAssignments(resoureIdentifier);
        var objectIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        IList<RoleAssignmentData> roleAssignmentDatas = new List<RoleAssignmentData>();

        string roleDefGuidStr = roleDefGuid.ToString();

        await foreach (var assignment in roleAssignments.GetAllAsync())
        {
            // Filter only "Key Vault Secrets User" assignments for service principals (managed identities surface as service principals)
            var roleDefId = assignment.Data.RoleDefinitionId;
            if (assignment.Data.PrincipalType == RoleManagementPrincipalType.ServicePrincipal &&
                !string.IsNullOrEmpty(roleDefId) &&
                roleDefId.Name.EndsWith(roleDefGuidStr, StringComparison.OrdinalIgnoreCase))
            {
                var principalId = assignment.Data.PrincipalId?.ToString();
                if (!string.IsNullOrEmpty(principalId))
                {
                    roleAssignmentDatas.Add(assignment.Data);
                    objectIds.Add(principalId);
                }
            }
        }

        var clientIds = await ConvertToClientIdsAsync(objectIds);

        return clientIds;
    }

    public async Task AddClientIdWithRbacAsync(string resourceId, Guid roleDefGuid, string clientId)
    {
        /*
         Pseudocode (detailed plan):
         - Guard: if any input invalid -> return.
         - Resolve service principal objectId from provided clientId (application ID):
             * Call helper GetServicePrincipalObjectIdAsync(clientId):
                 - If cache not used, build Graph request:
                   GET https://graph.microsoft.com/v1.0/servicePrincipals?$filter=appId eq '{clientId}'
                 - Auth using credential (_credentialFactory) with scope https://graph.microsoft.com/.default
                 - Parse JSON: value[0].id => objectId (Guid).
                 - Return Guid? or null.
         - If objectId not found -> return (silent no-op).
         - Build full roleDefinitionId:
             * Extract subscriptionId from resourceId (ResourceIdentifier).
             * roleDefinitionId = /subscriptions/{subscriptionId}/providers/Microsoft.Authorization/roleDefinitions/{roleDefGuid}
         - Create ArmClient.
         - Get RoleAssignmentCollection for the resource scope.
         - Check if existing assignment already present:
             * Enumerate GetAllAsync()
             * Match PrincipalId == objectId && RoleDefinitionId ends with roleDefGuid
             * If found -> return (idempotent).
         - Create RoleAssignmentCreateOrUpdateContent with:
             * roleDefinitionId (ResourceIdentifier)
             * principalId (Guid)
             * PrincipalType = ServicePrincipal
         - Call CreateOrUpdateAsync with new Guid as roleAssignment name.
         - Finish (no return value).
        */

        if (string.IsNullOrWhiteSpace(resourceId) ||
            roleDefGuid == Guid.Empty ||
            string.IsNullOrWhiteSpace(clientId))
            return;

        var objectId = await GetServicePrincipalObjectIdAsync(clientId);
        if (objectId is null)
            return; // Service principal not found.

        var credential = _credentialFactory.CreateCredential();
        var armClient = new ArmClient(credential);
        var scopeId = new ResourceIdentifier(resourceId);

        // Build fully qualified role definition id
        var subscriptionId = scopeId.SubscriptionId;
        if (string.IsNullOrWhiteSpace(subscriptionId))
            return;

        var fullRoleDefinitionId = $"/subscriptions/{subscriptionId}/providers/Microsoft.Authorization/roleDefinitions/{roleDefGuid}";

        var roleAssignments = armClient.GetRoleAssignments(scopeId);
        string roleDefGuidStr = roleDefGuid.ToString();

        await foreach (var assignment in roleAssignments.GetAllAsync())
        {
            if (assignment.Data.PrincipalId == objectId &&
                assignment.Data.RoleDefinitionId?.Name.EndsWith(roleDefGuidStr, StringComparison.OrdinalIgnoreCase) == true)
            {
                // Already assigned
                return;
            }
        }

        var content = new RoleAssignmentCreateOrUpdateContent(new ResourceIdentifier(fullRoleDefinitionId), objectId.Value)
        {
            PrincipalType = RoleManagementPrincipalType.ServicePrincipal
        };
        try
        {
            await roleAssignments.CreateOrUpdateAsync(WaitUntil.Completed, Guid.NewGuid().ToString(), content);
        }
        catch
        (RequestFailedException ex) when (ex.Status == 409)
        {
            // Conflict - role assignment
        }
    }

        async Task<Guid?> GetServicePrincipalObjectIdAsync(string clientId)
    {
        /*
         Pseudocode:
         - Guard: if clientId invalid -> null.
         - Acquire token for Graph.
         - Build request:
             GET https://graph.microsoft.com/v1.0/servicePrincipals?$filter=appId eq '{clientId}'
           (Ensure clientId is properly escaped - it's a GUID so direct insertion.)
         - Send request with bearer token.
         - Parse JSON: value array; first element id => objectId.
         - If id parsable as Guid => return; else null.
        */
        if (string.IsNullOrWhiteSpace(clientId))
            return null;

        var credential = _credentialFactory.CreateCredential();
        var token = await credential.GetTokenAsync(new TokenRequestContext(["https://graph.microsoft.com/.default"]), CancellationToken.None);

        var url = $"https://graph.microsoft.com/v1.0/servicePrincipals?$filter=appId eq '{clientId}'";
        using var http = new HttpClient();
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);

        using var resp = await http.SendAsync(req, CancellationToken.None);
        if (!resp.IsSuccessStatusCode)
            return null;

        await using var stream = await resp.Content.ReadAsStreamAsync(CancellationToken.None);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: CancellationToken.None);

        if (doc.RootElement.TryGetProperty("value", out var valueEl) &&
            valueEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var sp in valueEl.EnumerateArray())
            {
                if (sp.TryGetProperty("id", out var idEl))
                {
                    var idStr = idEl.GetString();
                    if (Guid.TryParse(idStr, out var guid))
                        return guid;
                }
                break; // Only first needed
            }
        }
        return null;
    }

    async Task<IEnumerable<string>> ConvertToClientIdsAsync(IEnumerable<string> objectIds)
    {
        /*
         Pseudocode (detailed plan):
         - Guard clause: if objectIds is null/empty => return [].
         - Normalize:
           * Deduplicate objectIds case-insensitively while preserving original input order for final mapping.
         - Acquire TokenCredential from _credentialFactory.
         - Prepare batching for Graph getByIds (safe batch size <= 1000).
         - For each batch:
           * Build POST https://graph.microsoft.com/v1.0/directoryObjects/getByIds with JSON body:
               { "ids": [ ...batchIds ], "types": ["servicePrincipal"] }
           * Get access token for scope "https://graph.microsoft.com/.default".
           * Send request via HttpClient with Authorization: Bearer <token>.
           * Parse JSON response "value" array:
               - For each object with properties id and appId, map id (objectId) -> appId (clientId).
         - Build result list in the same order as input, for those found in the mapping.
         - Return the clientId list.
        */

        if (objectIds is null)
            return [];

        // Deduplicate while preserving input order
        var orderedUniqueIds = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var id in objectIds)
        {
            if (string.IsNullOrWhiteSpace(id)) continue;
            if (seen.Add(id)) orderedUniqueIds.Add(id);
        }

        if (orderedUniqueIds.Count == 0)
            return [];

        var credential = _credentialFactory.CreateCredential();
        var clientIdsByObjectId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        const int batchSize = 1000; // Graph getByIds supports up to 1000 IDs per request
        for (int i = 0; i < orderedUniqueIds.Count; i += batchSize)
        {
            var batch = orderedUniqueIds.Skip(i).Take(batchSize).ToArray();

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/directoryObjects/getByIds");
            var payload = new
            {
                ids = batch,
                types = new[] { "servicePrincipal" }
            };

            string json = JsonSerializer.Serialize(payload);
            request.Content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var token = await credential
                .GetTokenAsync(new TokenRequestContext(["https://graph.microsoft.com/.default"]), CancellationToken.None)
                .ConfigureAwait(false);

            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);

            using var http = new HttpClient();
            using var response = await http.SendAsync(request, CancellationToken.None).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(CancellationToken.None).ConfigureAwait(false);
            using var doc = await System.Text.Json.JsonDocument.ParseAsync(stream, cancellationToken: CancellationToken.None).ConfigureAwait(false);

            if (doc.RootElement.TryGetProperty("value", out var valueEl) && valueEl.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var element in valueEl.EnumerateArray())
                {
                    // Expecting servicePrincipal objects: properties "id" (objectId) and "appId" (clientId)
                    if (element.TryGetProperty("id", out var idEl) &&
                        element.TryGetProperty("appId", out var appIdEl))
                    {
                        var oid = idEl.GetString();
                        var appId = appIdEl.GetString();
                        if (!string.IsNullOrWhiteSpace(oid) && !string.IsNullOrWhiteSpace(appId))
                        {
                            clientIdsByObjectId[oid] = appId;
                        }
                    }
                }
            }
        }

        // Build result list in the same order as the original input sequence (including duplicates if any)
        var result = new List<string>();
        foreach (var originalId in objectIds)
        {
            if (string.IsNullOrWhiteSpace(originalId)) continue;
            if (clientIdsByObjectId.TryGetValue(originalId, out var clientId))
                result.Add(clientId);
        }

        return result;
    }

}
