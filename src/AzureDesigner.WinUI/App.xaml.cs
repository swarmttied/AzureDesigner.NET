using Azure.Core;
using Azure.Core.Extensions;
using Azure.Identity;
using Azure.ResourceManager.Resources;
using AzureDesigner.AIContexts;
using AzureDesigner.AIContexts.AppInsights;
using AzureDesigner.AIContexts.CosmosDB;
using AzureDesigner.AIContexts.GenericResource;
using AzureDesigner.AIContexts.Identities;
using AzureDesigner.AIContexts.KeyVault;
using AzureDesigner.AIContexts.Language;
using AzureDesigner.AIContexts.OpenAI;
using AzureDesigner.AIContexts.Sites;
using AzureDesigner.AIContexts.SqlServer;
using AzureDesigner.AIContexts.SqlServerDb;
using AzureDesigner.AIContexts.Storage;
using AzureDesigner.AIContexts.Translation;
using AzureDesigner.Services;
using AzureDesigner.WinUI.Pages;
using AzureDesigner.WinUI.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.UI.Xaml;
using SKLIb;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AzureDesigner.WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    readonly IConfiguration _configuration;
    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();
        var settingsFile = "appsettings.json";
        // .SetBasePath(AppContext.BaseDirectory) as it's not available in .NET 9 for WinUI.
        _configuration = new ConfigurationBuilder()
            .AddJsonFile(settingsFile, optional: true, reloadOnChange: true)
            .Build();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        MainViewModel viewModel = CreateViewModel();
        MainPage mainPage = new(viewModel);
        m_window = new MainWindow(mainPage);
        m_window.Activate();
    }

    private Window? m_window;

    private MainViewModel CreateViewModel()
    {
        ICredentialFactory credentialFactory = new CredentialFactory(_configuration["ResourcesTenantID"]);
        var skClientSettings = new SKClient.Settings
        {
            Endpoint = _configuration["SKClient:Endpoint"],
            Deployment = _configuration["SKClient:Deployment"],
            Instructions = _configuration["SKClient:Instructions"],
            SaveChatHistory = Convert.ToBoolean(_configuration["SKClient:SaveChatHistory"] ?? "false")
        };
        IIdMapping idMapping = new IdMapping();
        IIdentityRepository identityRepo = new IdentityRepository(credentialFactory, idMapping);
        //ISitesRepository sitesRepository = new SitesRepository(credentialFactory);
        IRoleGuids roleGuids = new RoleGuids();
        IRbacService rbacService = new RbacService(credentialFactory);
        GenericResourceFunctions genericResourceFunctions = new();
        StorageFunctions storageFunctions = new(credentialFactory, rbacService, roleGuids, idMapping);
        CosmosDBFunctions cosmosDBFunctions = new(credentialFactory, idMapping);
        SitesFunctions sitesFunctions = new(credentialFactory, idMapping);       
        KeyVaultFunctions keyVaultFunctions = new(credentialFactory, rbacService, roleGuids, idMapping);
        OpenAIFunctions openAIFunctions = new(credentialFactory, rbacService, roleGuids, idMapping);
        SqlServerDbFunctions sqlServerDbFunctions = new(idMapping);
        SqlServerFunctions sqlServerFunctions = new(idMapping);
        TranslationFunctions translationFunctions = new(idMapping);
        LanguageFunctions languageFunctions = new(idMapping);
        AppInsightsRepository appInsightsRepository = new(credentialFactory, idMapping);
        AppInsightsFunctions appInsightsFunctions = new(credentialFactory, idMapping);

        object[] SKTools = [
            genericResourceFunctions,
            identityRepo,
            storageFunctions,
            cosmosDBFunctions,
            sitesFunctions,
            keyVaultFunctions,
            openAIFunctions, 
            sqlServerDbFunctions,
            sqlServerFunctions,
            translationFunctions,
            languageFunctions,
            appInsightsRepository,
            appInsightsFunctions
        ];

        ICredentialFactory aiCredentialFactory = new CredentialFactory(_configuration["AITenantID"]);
        ISKClient sKClient = new SKClient(skClientSettings,aiCredentialFactory.CreateCredential(), SKTools);
        ISubscriptionService subscriptionService = new SubscriptionService(credentialFactory);
        IDependencyHelper dependencyHelper = new DependencyHelper(sKClient, idMapping);
        ISKClient skClientForAIFixer = new SKClient(skClientSettings, aiCredentialFactory.CreateCredential(), SKTools);
        IAIFixer aiFixer = new AIFixer(skClientForAIFixer);
        IPromptsSourceRepository promptsSourceRepository = new PromptsSourceRepository();
        IResourceTypeNameMapper resourceTypeNameMapper = new ResourceTypeNameMapper();
        IconPathSource iconPathSource = new IconPathSource();

        // the objeects passed to the 'functions' param are only for handling the events during a function call
        // so that you can save the response in the AI trace log. If you don't want the logs,
        // you can may not include that object
        return new MainViewModel(
            subscriptionService: subscriptionService,
            dependencyHelper: dependencyHelper,
            aIFixer: aiFixer,
            identityRepository: identityRepo,
            iconPathSource: iconPathSource,
            promptsSourceRepository: promptsSourceRepository,
            appInsightsRepository: appInsightsRepository,
            skTools: SKTools,
            resourceTypeNameMapper: resourceTypeNameMapper,
            idMapping: idMapping
        );
    }
}