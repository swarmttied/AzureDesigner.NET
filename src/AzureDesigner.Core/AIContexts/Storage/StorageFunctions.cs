using System.ComponentModel;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;
using AzureDesigner.Models;
using AzureDesigner.Services;
using Microsoft.SemanticKernel;
using SKLIb;

namespace AzureDesigner.AIContexts.Storage;

public class StorageFunctions : IFunctionCalled, INameToIdResolver
{
    readonly ICredentialFactory _credentialFactory;
    readonly IRbacService _rbacService;
    readonly IRoleGuids _roleGuids;
    readonly IIdMapping _idMapping;

    public event EventHandler<FunctionCallEventArgs> FunctionCalled;


    public StorageFunctions(ICredentialFactory credentialFactory, IRbacService rbacService,
        IRoleGuids roleGuids, IIdMapping idMapping)
    {
        _credentialFactory = credentialFactory ?? throw new ArgumentNullException(nameof(credentialFactory));
        _rbacService = rbacService ?? throw new ArgumentNullException(nameof(rbacService));
        _roleGuids = roleGuids ?? throw new ArgumentNullException(nameof(roleGuids));
        _idMapping = idMapping;
    }

    [KernelFunction]
    public async Task<StorageAccountData> GetStorageAccountInfo(int id)
    {
        string fullId = _idMapping.GetFullId(id);
        FunctionCalled?.Invoke(this, new FunctionCallEventArgs($"""{nameof(GetStorageAccountInfo)}("{id}")"""));
        var credential = _credentialFactory.CreateCredential();
        var armClient = new ArmClient(credential);
        var resourceId = new ResourceIdentifier(fullId);
        var storageAccount = await armClient.GetStorageAccountResource(resourceId).GetAsync();
        StorageAccountResource resource = storageAccount.Value;
        StorageAccountData data = resource.Data;

        // Option 1 : Access properties directly from the StorageAccountData object
        // If you asked AI something like "Is local user enabled on storage account <name>?"
        // This KernelFunction will be called and the property can be accessed directly from the returned data.
        //data.IsLocalUserEnabled

        // Storage Account Data is serializable so it's fine to return for AI context
        return data;
    }

    [KernelFunction()]
    public async Task<string> GetIpAddress()
    {
        using var client = new HttpClient();
        string ip = await client.GetStringAsync("https://api.ipify.org");
        return ip;

    }

    [KernelFunction]
    public async Task<IEnumerable<string>> GetStorageManagedIdentityIdsWithRbacAccess(int id, string roleName)
    {
        string fullId = _idMapping.GetFullId(id);
        FunctionCalled?.Invoke(this, new FunctionCallEventArgs($"""{nameof(GetStorageManagedIdentityIdsWithRbacAccess)}("{id}", "{roleName})"""));
        var roleDefGuid = _roleGuids[roleName];
        var clientIds = await _rbacService.GetClientIdsWithRbacAsync(fullId, roleDefGuid);
        return clientIds;
    }

    [KernelFunction]
    public async Task AddManagedIdentityWithRbacRoleToStorage(int storageId, string roleName, string clientId)
    {
        FunctionCalled?.Invoke(this, new FunctionCallEventArgs($"""{nameof(AddManagedIdentityWithRbacRoleToStorage)}("{storageId}", "{roleName}", "{clientId}")"""));
        string fullId = _idMapping.GetFullId(storageId);
        var roleDefGuid = _roleGuids[roleName];
        await _rbacService.AddClientIdWithRbacAsync(fullId, roleDefGuid, clientId);
    }

    [KernelFunction]
    public async Task UpdateStorageAccountPublicNetworkAccess(int id, string ipAddress)
    {
        string fullId = _idMapping.GetFullId(id);
        FunctionCalled?.Invoke(this, new FunctionCallEventArgs($"""{nameof(UpdateStorageAccountPublicNetworkAccess)}("{id}", "{ipAddress}")"""));
        if (string.IsNullOrWhiteSpace(fullId))
            throw new ArgumentNullException(nameof(id));
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentNullException(nameof(ipAddress));

        var credential = _credentialFactory.CreateCredential();
        var armClient = new ArmClient(credential);
        var resourceId = new ResourceIdentifier(fullId);
        var storageAccountResource = await armClient.GetStorageAccountResource(resourceId).GetAsync();

        var networkRuleSet = storageAccountResource.Value.Data.NetworkRuleSet ?? new StorageAccountNetworkRuleSet(StorageNetworkDefaultAction.Allow);

        if (networkRuleSet.IPRules == null)
        {
            throw new InvalidOperationException("IPRules collection is null and cannot be assigned because it is read-only.");
        }

        if (!networkRuleSet.IPRules.Any(r => r.IPAddressOrRange == ipAddress))
        {
            var ipRule = new StorageAccountIPRule(ipAddress)
            {
                Action = StorageAccountNetworkRuleAction.Allow
            };
            networkRuleSet.IPRules.Add(ipRule);
        }

        // Prepare update options
        var updateOptions = new StorageAccountPatch
        {
            NetworkRuleSet = networkRuleSet
        };

        await storageAccountResource.Value.UpdateAsync(updateOptions);
    }



    // Option 2 - more specific function to get a specific property
    [KernelFunction]
    public async Task<bool?> IsStorageLocalUserEnabled(int id)
    {
        string fullId = _idMapping.GetFullId(id);
        FunctionCalled?.Invoke(this, new FunctionCallEventArgs($"""{nameof(IsStorageLocalUserEnabled)}("{id}")"""));
        var credential = _credentialFactory.CreateCredential();
        var armClient = new ArmClient(credential);
        var resourceId = new ResourceIdentifier(fullId);
        var storageAccount = await armClient.GetStorageAccountResource(resourceId).GetAsync();
        StorageAccountResource resource = storageAccount.Value;
        StorageAccountData data = resource.Data;
        return data.IsLocalUserEnabled;
    }

    [KernelFunction]
    public async Task UpdateStorageBlobPublicAccess(int id, bool allowBlobPublicAccess)
    {
        string fullId = _idMapping.GetFullId(id);
        FunctionCalled?.Invoke(this, new FunctionCallEventArgs($"""{nameof(UpdateStorageBlobPublicAccess)}("{id}", {allowBlobPublicAccess})"""));

        if (string.IsNullOrWhiteSpace(fullId))
            throw new ArgumentNullException(nameof(id));

        var credential = _credentialFactory.CreateCredential();
        var armClient = new ArmClient(credential);
        var resourceId = new ResourceIdentifier(fullId);
        var storageAccount = await armClient.GetStorageAccountResource(resourceId).GetAsync();

        if (storageAccount.Value == null)
            throw new InvalidOperationException("Storage account not found");

        // Only update if the current state is different from desired state
        if (storageAccount.Value.Data.AllowBlobPublicAccess != allowBlobPublicAccess)
        {
            var patch = new StorageAccountPatch()
            {
                AllowBlobPublicAccess = allowBlobPublicAccess
            };

            var response = await storageAccount.Value.UpdateAsync(patch);

            if (response.GetRawResponse().Status != 200 && response.GetRawResponse().Status != 202)
                throw new InvalidOperationException($"Failed to update public blob access. Status code: {response.GetRawResponse().Status}");
        }
    }

    [KernelFunction]
    public async Task UpdateStorageIsLocalUserEnabled(int id, bool enableLocalUser)
    {
        string fullId = _idMapping.GetFullId(id);
        FunctionCalled?.Invoke(this, new FunctionCallEventArgs($"""{nameof(UpdateStorageIsLocalUserEnabled)}("{id}", {enableLocalUser})"""));

        if (string.IsNullOrWhiteSpace(fullId))
            throw new ArgumentNullException(nameof(id));

        var credential = _credentialFactory.CreateCredential();
        var armClient = new ArmClient(credential);
        var resourceId = new ResourceIdentifier(fullId);
        var storageAccount = await armClient.GetStorageAccountResource(resourceId).GetAsync();

        if (storageAccount.Value == null)
            throw new InvalidOperationException("Storage account not found");

        // Only update if the current state is different from desired state
        if (storageAccount.Value.Data.IsLocalUserEnabled != enableLocalUser)
        {
            var patch = new StorageAccountPatch()
            {
                IsLocalUserEnabled = enableLocalUser
            };

            var response = await storageAccount.Value.UpdateAsync(patch);

            if (response.GetRawResponse().Status != 200 && response.GetRawResponse().Status != 202)
                throw new InvalidOperationException($"Failed to update local user access. Status code: {response.GetRawResponse().Status}");
        }
    }

    [KernelFunction]
    public async Task UpdateStorageAllowedSharedKeyAccess(int id, bool allowSharedKeyAccess)
    {
        string fullId = _idMapping.GetFullId(id);
        FunctionCalled?.Invoke(this, new FunctionCallEventArgs($"""{nameof(UpdateStorageAllowedSharedKeyAccess)}("{id}", {allowSharedKeyAccess})"""));

        if (string.IsNullOrWhiteSpace(fullId))
            throw new ArgumentNullException(nameof(fullId));

        var credential = _credentialFactory.CreateCredential();
        var armClient = new ArmClient(credential);
        var resourceId = new ResourceIdentifier(fullId);
        var storageAccount = await armClient.GetStorageAccountResource(resourceId).GetAsync();

        if (storageAccount.Value == null)
            throw new InvalidOperationException("Storage account not found");

        // Only update if the current state is different from desired state
        if (storageAccount.Value.Data.AllowSharedKeyAccess != allowSharedKeyAccess)
        {
            var patch = new StorageAccountPatch()
            {
                AllowSharedKeyAccess = allowSharedKeyAccess
            };

            var response = await storageAccount.Value.UpdateAsync(patch);

            if (response.GetRawResponse().Status != 200 && response.GetRawResponse().Status != 202)
                throw new InvalidOperationException($"Failed to update shared key access. Status code: {response.GetRawResponse().Status}");
        }
    }

    IDictionary<string, int> _nodeDict;
    void INameToIdResolver.SetResolverSource(IEnumerable<Node> nodes)
    {
        _nodeDict = nodes.Where(o => o.Type.Contains("storageaccount", StringComparison.InvariantCultureIgnoreCase)).ToDictionary(n => n.Name, no => no.Id);
    }

    [KernelFunction]
    public int ResolveStorageAccountNameToID(string name)
    {
        FunctionCalled?.Invoke(this, new FunctionCallEventArgs($"""{nameof(ResolveStorageAccountNameToID)}("{name})" """));

        if (!_nodeDict.TryGetValue(name, out int id))
        {
            return -1;
        }

        return id;
    }
}



