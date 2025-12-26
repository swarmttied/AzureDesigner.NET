using System;
using System.Collections.Generic;

namespace AzureDesigner.WinUI
{
    public interface IResourceTypeNameMapper
    {
        string this[string resourceType] { get; }
    }

    public class ResourceTypeNameMapper : IResourceTypeNameMapper
    {
        private readonly Dictionary<string, string> _typeMappings = new(StringComparer.InvariantCultureIgnoreCase)
        {
            { "microsoft.keyvault/vaults", "Key Vault"},
            { "microsoft.web/sites/app", "Web App" },
            { "microsoft.web/sites/app,linux", "Web App" },
            { "microsoft.web/sites/functionapp", "Function App" },
            { "microsoft.web/sites/functionapp,linux", "Function App" },
            { "microsoft.storage/storageaccounts/storagev2", "Storage Accounts" },
            { "microsoft.storage/storageaccounts/storage", "Storage Accounts (classic)" },
            { "microsoft.logic/workflows", "Logic App" },
            { "microsoft.managedidentity/userassignedidentities", "Managed Identity" },
            { "microsoft.cognitiveservices/accounts/openai", "Azure OpenAI" },
            { "microsoft.documentdb/databaseaccounts", "Cosmos DB" },
            { "microsoft.documentdb/databaseaccounts/globaldocumentdb", "Cosmos DB" },
            { "microsoft.sql/servers/databases/v12.0,user,vcore", "SQL Database" },
            { "microsoft.sql/servers/v12.0", "SQL Server" },
            { "Microsoft.Sql/servers/databases/v12.0,user,vcore,serverless", "SQL Database (serverless)" }
        };

        public string this[string resourceType]
        {
            get => resourceType is not null && _typeMappings.TryGetValue(resourceType, out var name)
                ? name
                : resourceType;
        }
    }
}
