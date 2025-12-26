// S


using Azure.Identity;
using Microsoft.Extensions.Configuration;
using SKLIb;

class Program
{
    static void Main(string[] args)
    {
        // Build configuration from appsettings.json and environment variables
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        // Retrieve SKClient settings from configuration
        var skClientSettings = new SKClient.Settings
        {
            Endpoint = configuration["SKClient:Endpoint"],
            Deployment = configuration["SKClient:Deployment"],
            Instructions = null,// configuration["SKClient:Instructions"],
            // Add other settings as needed
        };

        // Create a TokenCredential (use DefaultAzureCredential for example)
        var credential = new AzureCliCredential();

        // Instantiate SKClient with settings and credential
        var skClient = new SKClient(skClientSettings, credential);

        skClient.ResponseReceived += SkClient_ResponseReceived;

        string prompt = @"
Search for service in the Sercies section that is being referred by anything in the ettings JSON. Return to me the Id in JSON schema like this:
{

    ""Id"": ""123-456-7890"", // Do not modify this  
	""ServiceIds: [ ""service id 1"", ""service id 2"", ... ]
}

Settings:
{
  ""FUNCTIONS_EXTENSION_VERSION"": ""~4"",
  ""FUNCTIONS_WORKER_RUNTIME"": ""dotnet-isolated"",
  ""WEBSITE_USE_PLACEHOLDER_DOTNETISOLATED"": ""1"",
  ""APPLICATIONINSIGHTS_CONNECTION_STRING"": ""InstrumentationKey=d7f93706-5bc9-4653-8c6a-50fea3c87dea;IngestionEndpoint=https://canadacentral-1.in.applicationinsights.azure.com/;LiveEndpoint=https://canadacentral.livediagnostics.monitor.azure.com/;ApplicationId=5659b3d4-81dc-454a-a0c3-887e906b2a05"",
  ""AzureWebJobsStorage"": ""DefaultEndpointsProtocol=https;AccountName=gbbfuncstorage;AccountKey=GAM3WYW3Lb\u002BgSwSpE1Y6m2y43e4g3klrFiocKJwpzDw7/89fV16HaxJHVJitMw5kPh//mbYbZPMd\u002BASt0O/CkA==;EndpointSuffix=core.windows.net"",
  ""WEBSITE_CONTENTAZUREFILECONNECTIONSTRING"": ""DefaultEndpointsProtocol=https;AccountName=gbbfuncstorage;AccountKey=GAM3WYW3Lb\u002BgSwSpE1Y6m2y43e4g3klrFiocKJwpzDw7/89fV16HaxJHVJitMw5kPh//mbYbZPMd\u002BASt0O/CkA==;EndpointSuffix=core.windows.net"",
  ""WEBSITE_CONTENTSHARE"": ""storageopsb609""
}

Service IDs:
/subscriptions/a6c78a63-6e6d-4d66-8d84-3989715f3111/resourceGroups/study/providers/Microsoft.ManagedIdentity/userAssignedIdentities/study-managed-identity, /subscriptions/a6c78a63-6e6d-4d66-8d84-3989715f3111/resourceGroups/study/providers/Microsoft.KeyVault/vaults/gioskv, /subscriptions/a6c78a63-6e6d-4d66-8d84-3989715f3111/resourceGroups/study/providers/Microsoft.Storage/storageAccounts/gbbstudystore, /subscriptions/a6c78a63-6e6d-4d66-8d84-3989715f3111/resourceGroups/quantum/providers/Microsoft.Storage/storageAccounts/gioquantumstorage, /subscriptions/a6c78a63-6e6d-4d66-8d84-3989715f3111/resourceGroups/quantum/providers/Microsoft.Quantum/Workspaces/demo-ws1, /subscriptions/a6c78a63-6e6d-4d66-8d84-3989715f3111/resourceGroups/study/providers/Microsoft.Sql/servers/giobsql/databases/master, /subscriptions/a6c78a63-6e6d-4d66-8d84-3989715f3111/resourceGroups/study/providers/microsoft.insights/components/winedbapp, /subscriptions/a6c78a63-6e6d-4d66-8d84-3989715f3111/resourceGroups/study/providers/Microsoft.Web/sites/winedbapp, /subscriptions/a6c78a63-6e6d-4d66-8d84-3989715f3111/resourceGroups/study/providers/Microsoft.Web/serverFarms/ASP-study-b2c2, /subscriptions/a6c78a63-6e6d-4d66-8d84-3989715f3111/resourceGroups/study/providers/Microsoft.Sql/servers/giobsql, /subscriptions/a6c78a63-6e6d-4d66-8d84-3989715f3111/resourceGroups/study/providers/Microsoft.Storage/storageAccounts/gbbfuncstorage, /subscriptions/a6c78a63-6e6d-4d66-8d84-3989715f3111/resourceGroups/study/providers/Microsoft.Web/serverFarms/ASP-study-85af, /subscriptions/a6c78a63-6e6d-4d66-8d84-3989715f3111/resourceGroups/study/providers/microsoft.insights/components/storageops, /subscriptions/a6c78a63-6e6d-4d66-8d84-3989715f3111/resourceGroups/study/providers/Microsoft.Web/sites/storageops, /subscriptions/a6c78a63-6e6d-4d66-8d84-3989715f3111/resourceGroups/study/providers/Microsoft.ManagedIdentity/userAssignedIdentities/storageops-id-8359, /subscriptions/a6c78a63-6e6d-4d66-8d84-3989715f3111/resourceGroups/study/providers/microsoft.insights/actiongroups/Application Insights Smart Detection, /subscriptions/a6c78a63-6e6d-4d66-8d84-3989715f3111/resourceGroups/study/providers/Microsoft.CognitiveServices/accounts/gbb-open-ai, /subscriptions/a6c78a63-6e6d-4d66-8d84-3989715f3111/resourceGroups/study/providers/Microsoft.CognitiveServices/accounts/the-r-m6e97o8p-swedencentral, /subscriptions/a6c78a63-6e6d-4d66-8d84-3989715f3111/resourceGroups/study/providers/Microsoft.CognitiveServices/accounts/the-r-m6eie2af-francecentral, /subscriptions/a6c78a63-6e6d-4d66-8d84-3989715f3111/resourceGroups/study/providers/Microsoft.AnalysisServices/servers/giobbas, /subscriptions/a6c78a63-6e6d-4d66-8d84-3989715f3111/resourceGroups/funct-to-funct-calls/providers/Microsoft.Storage/storageAccounts/giocalleestorage, /subscriptions/a6c78a63-6e6d-4d66-8d84-3989715f3111/resourceGroups/funct-to-funct-calls/providers/Microsoft.Web/sites/giocallee, /subscriptions/a6c78a63-6e6d-4d66-8d84-3989715f3111/resourceGroups/funct-to-funct-calls/providers/Microsoft.Web/serverFarms/WestUSPlan, /subscriptions/a6c78a63-6e6d-4d66-8d84-3989715f3111/resourceGroups/funct-to-funct-calls/providers/Microsoft.Storage/storageAccounts/giocallerstorage, /subscriptions/a6c78a63-6e6d-4d66-8d84-3989715f3111/resourceGroups/funct-to-funct-calls/providers/Microsoft.Web/sites/giocaller, /subscriptions/a6c78a63-6e6d-4d66-8d84-3989715f3111/resourceGroups/gio-iac/providers/Microsoft.OperationalInsights/workspaces/gio-log-analytics-test1, /subscriptions/a6c78a63-6e6d-4d66-8d84-3989715f3111/resourceGroups/gio-iac/providers/Microsoft.ContainerRegistry/registries/slackerslab, /subscriptions/a6c78a63-6e6d-4d66-8d84-3989715f3111/resourceGroups/gio-iac/providers/Microsoft.Resources/templateSpecs/sqldatabase, /subscriptions/a6c78a63-6e6d-4d66-8d84-3989715f3111/resourceGroups/gio-iac/providers/Microsoft.Resources/templateSpecs/sqldatabase/versions/1.0, /subscriptions/a6c78a63-6e6d-4d66-8d84-3989715f3111/resourceGroups/gio-iac/providers/Microsoft.Resources/templateSpecs/applicationinsights, /subscriptions/a6c78a63-6e6d-4d66-8d84-3989715f3111/resourceGroups/gio-iac/providers/Microsoft.Resources/templateSpecs/applicationinsights/versions/1.0, /subscriptions/a6c78a63-6e6d-4d66-8d84-3989715f3111/resourceGroups/gio-iac/providers/Microsoft.Resources/templateSpecs/appservice, /subscriptions/a6c78a63-6e6d-4d66-8d84-3989715f3111/resourceGroups/gio-iac/providers/Microsoft.Resources/templateSpecs/appservice/versions/1.0, /subscriptions/a6c78a63-6e6d-4d66-8d84-3989715f3111/resourceGroups/gio-iac/providers/Microsoft.Resources/templateSpecs/applicationinsights/versions/2.0";

        skClient.RunAsync(prompt).Wait();

    }

    private static void SkClient_ResponseReceived(object? sender, SKClient.ResponseEventArgs e)
    {
        Console.WriteLine(e.JsonResponse);
        Console.ReadKey();
    }
}
