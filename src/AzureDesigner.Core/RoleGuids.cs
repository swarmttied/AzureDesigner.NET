namespace AzureDesigner;

public interface IRoleGuids
{
    Guid this[string roleName] { get; }
}

public class RoleGuids : IRoleGuids
{
    readonly Dictionary<string, Guid> _roleNameToGuid = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase)
    {
        // https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles
        { RoleNames.KeyVaultSecretsUser, Guid.Parse("4633458b-17de-408a-b874-0445c86b69e6") },
        { RoleNames.StorageBlobDataContributor, Guid.Parse("ba92f5b4-2d11-453d-a403-e96b0029c9fe") },
        { RoleNames.CognitiveServicesOpenAIUser, Guid.Parse("5e0bd9bd-7b93-4f28-af87-19fc36ad61bd") }
    };

    public Guid this[string roleName] => _roleNameToGuid[roleName];
}
