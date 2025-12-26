using System.ComponentModel;
using Azure.ResourceManager;
using Azure.ResourceManager.ManagedServiceIdentities;
using Azure.ResourceManager.Storage.Models;
using AzureDesigner.Models;
using Microsoft.SemanticKernel;
using SKLIb;

namespace AzureDesigner.AIContexts.Identities;

public interface IIdentityRepository
{
    int ResolveManagedIdentityClientIDToID(Guid clientId);
    Task LoadIdentities(string subdcriptionId);
}

public class IdentityRepository : IIdentityRepository
{
    readonly ICredentialFactory _credentialFactory;
    readonly IIdMapping _idMapping;

    Dictionary<Guid, string> _managedIdentityLookup = new();

    public event EventHandler<FunctionCallEventArgs> FunctionCalled;

    public IdentityRepository(ICredentialFactory credentialFactory, IIdMapping idMapping)
    {
        _credentialFactory = credentialFactory ?? throw new ArgumentNullException(nameof(credentialFactory));
        _idMapping = idMapping;
    }

    public async Task LoadIdentities(string subdcriptionId)
    {

        var armClient = new ArmClient(_credentialFactory.CreateCredential(), subdcriptionId);
        var subscription = await armClient.GetDefaultSubscriptionAsync();

        var idenitetiesCollection = subscription.GetUserAssignedIdentitiesAsync();

        await foreach (var identity in idenitetiesCollection)
        {
            var data = identity.Data;
            var clientId = data.ClientId ?? Guid.NewGuid();
            _managedIdentityLookup[clientId] = data.Id.ToString();
        }
    }


    [KernelFunction]
    public int ResolveManagedIdentityClientIDToID(       
        Guid managedIdentityclientId)
    {
        FunctionCalled?.Invoke(this, new FunctionCallEventArgs($"""{nameof(ResolveManagedIdentityClientIDToID)}("{managedIdentityclientId}") """));

        if (!_managedIdentityLookup.ContainsKey(managedIdentityclientId))
        {
            return -1;
        }

        string fullId = _managedIdentityLookup[managedIdentityclientId];
        int compactId = _idMapping.GetCompactId(fullId);
        return compactId;
    }
}
