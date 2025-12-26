using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.KeyVault.Models;
using AzureDesigner.Models;
using AzureDesigner.Services;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using SKLIb;

namespace AzureDesigner.AIContexts.KeyVault;

public class KeyVaultFunctions : IFunctionCalled, INameToIdResolver, IAIFunctionsSource
{



    readonly ICredentialFactory _credentialFactory;
    readonly IRbacService _rbacService;
    readonly IRoleGuids _roleGuids;
    readonly IIdMapping _idMapping;

    public KeyVaultFunctions(ICredentialFactory credentialFactory, IRbacService rbacService,
        IRoleGuids roleGuids, IIdMapping idMapping)
    {
        _credentialFactory = credentialFactory;
        _rbacService = rbacService;
        _roleGuids = roleGuids;
        _idMapping = idMapping;
    }

    public event EventHandler<FunctionCallEventArgs> FunctionCalled;

    [KernelFunction]
    public async Task<KeyVaultData> GetVaultInfo(int id)
    {
        string fullId = _idMapping.GetFullId(id);
        FunctionCalled?.Invoke(this, new FunctionCallEventArgs($"""{nameof(GetVaultInfo)}("{id}")"""));

        var data = await GetInfoAsync(fullId);

        try
        {
            var jsonString = System.Text.Json.JsonSerializer.Serialize(data);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return data;
    }

    async Task<KeyVaultData> GetInfoAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        var credential = _credentialFactory.CreateCredential();
        var armClient = new ArmClient(credential);
        var resourceId = new ResourceIdentifier(id);
        var vaultResource = await armClient.GetKeyVaultResource(resourceId).GetAsync();

        KeyVaultData data = vaultResource.Value.Data;
        return data;
    }

    [KernelFunction]
    public async Task<IEnumerable<string>> GetVaultManagedIdentityIdsWithRbacAccess(int id, string roleName)
    {
        string fullId = _idMapping.GetFullId(id);
        FunctionCalled?.Invoke(this, new FunctionCallEventArgs($"""{nameof(GetVaultManagedIdentityIdsWithRbacAccess)}("{id}", "{roleName}")"""));
        var roleDefGuid = _roleGuids[roleName];
        var clientIds = await _rbacService.GetClientIdsWithRbacAsync(fullId, roleDefGuid);
        return clientIds;
    }

    [KernelFunction]
    public async Task AddManagedIdentityWithRbacRoleToKeyVault(int keyVaultId, string roleName, string clientId)
    {
        FunctionCalled?.Invoke(this, new FunctionCallEventArgs($"""{nameof(AddManagedIdentityWithRbacRoleToKeyVault)}("{keyVaultId}", "{roleName}", "{clientId}")"""));
        string fullId = _idMapping.GetFullId(keyVaultId);
        var roleDefGuid = _roleGuids[roleName];
        await _rbacService.AddClientIdWithRbacAsync(fullId, roleDefGuid, clientId);
    }


    [KernelFunction]
    public async Task UpdatePublicNetworkAccess(int id, bool enabled)
    {
        string fullId = _idMapping.GetFullId(id);
        FunctionCalled?.Invoke(this, new FunctionCallEventArgs($"""{nameof(UpdatePublicNetworkAccess)}("{id}", "{enabled}")"""));

        if (string.IsNullOrWhiteSpace(fullId))
            throw new ArgumentNullException(nameof(id));

        var credential = _credentialFactory.CreateCredential();
        var armClient = new ArmClient(credential);
        var resourceId = new ResourceIdentifier(fullId);
        var keyVaultResource = await armClient.GetKeyVaultResource(resourceId).GetAsync();

        if (keyVaultResource.Value == null)
            throw new InvalidOperationException("KeyVaultResource not found.");

        string newPublicAccess = enabled ? "Enabled" : "Disabled";

        var patch = new KeyVaultPatch
        {
            Properties = new KeyVaultPatchProperties
            {
                PublicNetworkAccess = newPublicAccess
            }
        };

        var response = await keyVaultResource.Value.UpdateAsync(patch);

        if (response.GetRawResponse().Status != 200 && response.GetRawResponse().Status != 202)
            throw new InvalidOperationException($"Failed to disable PublicNetworkAccess. Status code: {response.GetRawResponse().Status}");
    }

    [KernelFunction]
    public async Task<string> GetIpAddress()
    {
        using var client = new HttpClient();
        string ip = await client.GetStringAsync("https://api.ipify.org");
        return ip;
    }

    [KernelFunction]
    public async Task UpdateKeyVaultPublicNetworkAccessIpAddress(int id, string ipAddress)
    {
        string fullId = _idMapping.GetFullId(id);
        FunctionCalled?.Invoke(this, new FunctionCallEventArgs($"""{nameof(UpdateKeyVaultPublicNetworkAccessIpAddress)}("{id}", "{ipAddress}")"""));
        if (string.IsNullOrWhiteSpace(fullId))
            throw new ArgumentNullException(nameof(id));
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentNullException(nameof(ipAddress));

        var credential = _credentialFactory.CreateCredential();
        var armClient = new ArmClient(credential);
        var resourceId = new ResourceIdentifier(fullId);
        var keyVault = await armClient.GetKeyVaultResource(resourceId).GetAsync();

        var data = keyVault.Value.Data;

        var existingRuleSet = data.Properties.NetworkRuleSet;
        var ipRules = existingRuleSet?.IPRules != null
        ? existingRuleSet.IPRules.ToList()
        : new List<KeyVaultIPRule>();

        if (!ipRules.Any(r => r.AddressRange == ipAddress))
        {
            ipRules.Add(new KeyVaultIPRule(ipAddress));
        }

        var newRuleSet = new KeyVaultNetworkRuleSet
        {
            Bypass = existingRuleSet?.Bypass ?? "AzureServices",
            DefaultAction = existingRuleSet?.DefaultAction ?? "Deny",
            IPRules = { }
        };

        foreach (var rule in ipRules)
        {
            newRuleSet.IPRules.Add(rule);
        }

        // Prepare the patch
        var patch = new KeyVaultPatch
        {
            Properties = new KeyVaultPatchProperties
            {
                NetworkRuleSet = newRuleSet
            }
        };

        var response = await keyVault.Value.UpdateAsync(patch);

        if (response.GetRawResponse().Status != 200 && response.GetRawResponse().Status != 202)
            throw new InvalidOperationException($"Failed to update Key Vault network rules. Status code: {response.GetRawResponse().Status}");
    }

    IDictionary<string, int> _nodeDict;
    void INameToIdResolver.SetResolverSource(IEnumerable<Node> nodes)
    {
        _nodeDict = nodes.Where(o => o.Type.Contains("keyvault", StringComparison.InvariantCultureIgnoreCase)).ToDictionary(n => n.Name, no => no.Id);
    }

    [KernelFunction]
    public int ResolveKeyVaultNameToID(string name)
    {
        FunctionCalled?.Invoke(this, new FunctionCallEventArgs($"""{nameof(ResolveKeyVaultNameToID)}("{name})" """));

        if (!_nodeDict.TryGetValue(name, out int id))
        {
            return -1;
        }

        return id;
    }

    public IEnumerable<AITool> GetAIFunctions()
    {
        return [AIFunctionFactory.Create(GetVaultInfo),
                AIFunctionFactory.Create(GetVaultManagedIdentityIdsWithRbacAccess),
                AIFunctionFactory.Create(AddManagedIdentityWithRbacRoleToKeyVault),
                AIFunctionFactory.Create(UpdatePublicNetworkAccess),
                AIFunctionFactory.Create(GetIpAddress),
                AIFunctionFactory.Create(UpdateKeyVaultPublicNetworkAccessIpAddress),
                AIFunctionFactory.Create(ResolveKeyVaultNameToID)
        ];
    }
}
