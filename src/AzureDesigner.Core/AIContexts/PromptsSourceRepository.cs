using AzureDesigner.AIContexts.Identities;
using AzureDesigner.AIContexts.KeyVault;
using AzureDesigner.AIContexts.Sites;
using AzureDesigner.AIContexts.SqlServerDb;
using AzureDesigner.AIContexts.Storage;
using AzureDesigner.AIContexts.CosmosDB;
using AzureDesigner.AIContexts.AppInsights;

namespace AzureDesigner.AIContexts;

public interface IPromptsSourceRepository
{
    IPromptsSource GetPromptsSource(string type);
}

public class PromptsSourceRepository : IPromptsSourceRepository
{
    private readonly Dictionary<string, IPromptsSource> _sources = new();

    public IPromptsSource GetPromptsSource(string type)
    {
        // Convert input type to lowercase for comparison
        type = type.ToLower();

        if (!_sources.TryGetValue(type, out var source))
        {
            switch (type)
            {
                case "microsoft.web/sites/app":
                case "microsoft.web/sites/app,linux":
                case "microsoft.web/sites/functionapp":
                case "microsoft.web/sites/functionapp,linux":
                    source = new SitesPromptsSource();
                    break;
                case "microsoft.storage/storageaccounts/storagev2":
                case "microsoft.storage/storageaccounts/storage":
                    source = new StoragePromptSource();
                    break;
                case "microsoft.managedidentity/userassignedidentities":
                    source = new IdentitiesPromptSource();
                    break;
                case "microsoft.keyvault/vaults":
                    source = new KeyVaultPromptSource();
                    break;
                case "microsoft.sql/servers/databases/v12.0,user,vcore":
                case "microsoft.sql/servers/databases/v12.0,system":
                    source = new SqlServerDbPromptSource();
                    break;
                case "microsoft.documentdb/databaseaccounts/globaldocumentdb":
                case "microsoft.documentdb/databaseaccounts":
                    source = new CosmosDbPromptSource();
                    break;
                case "microsoft.insights/components/web":
                case "microsoft.insights/components":
                    source = new AppInsightsPromptsSource();
                    break;
                default:
                    source = null;
                    break;
            }

            if (source != null)
                _sources[type] = source;
        }
        return source;
    }
}




