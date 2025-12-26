Please refer to our [hackathon page](https://innovationstudio.microsoft.com/hackathons/hackathon2025/project/97834) to learn more about this project.


## Developer Setup ##

1. Install [Visual Studio](https://visualstudio.microsoft.com/) (VS).
2. Install [Azure CLI for Windows](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli-windows?view=azure-cli-latest&pivots=msi). Restart your VS after installing this.
3. Install the _WinUI application Development_ workload in VS.Go to _Tools > Get Tools and Features... > Modify_ and select the workload.
4. Install the [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) (if not already installed).
5. Set the startup project should be the _AzureDesigner.WinUI._
6. Change the values in the _appsettings.json_ accordingly.
7. Press F5 or right-click on the start-up project and press debug.

If you are still blocked somehow, reach out to Giovanni Bejarasco (gibejara).

## AI Context
AI contexts is the logical concept that groups the prompt and the tools for the AI. It maps to a resource type. For example, 
the `Sites` AI context is used to analyze and create dependency graphs for web apps and function apps. You can find it in the `AIContexts\Sites` folder of
the _AzureDesigner.Core_ project.


### Creating your own AI Context
1. Create a new folder in the `AIContexts` folder of the _AzureDesigner.Core. Name it accordingly to the resource type you want to target.
2. Create a new class that implements the `IPromptsSource` interface. The naming convention is `<ResourceType>PromptsSource`. For example, for the `Sites` context, the class is named `SitesPromptsSource`.

... in progress

## Service Types
Azure service types used in the application is actually a combination of Type/Kind to provider granular differentiation. 
Use these values in the `PromptSourceRepository` method to return the `IPromptsSource` object for the specific Azure service.

__NOTE__: The true values might have different casing, so make sure you use the exact values returned by the codes.


_Table is AI-generated and may be incomplete_

| Azure Service                  | Type (ResourceType.Type)                  | Kind (data.Kind)      | Type/Kind Value                                 |
|------------------------------- |------------------------------------------ |---------------------- |------------------------------------------------|
| App Service (Web App)          | Web/sites                       | app                  | Web/sites/app                        |
| App Service (Function App)     | Web/sites                       | functionapp          | Web/sites/functionapp                |
| App Service (API App)          | Web/sites                       | api                  | Web/sites/api                        |
| App Service Plan               | Web/serverfarms                 |               | Web/serverfarms                      |
| Storage Account (General v2)   | Storage/storageAccounts         | StorageV2            | Storage/storageAccounts/StorageV2     |
| Storage Account (Blob)         | Storage/storageAccounts         | BlobStorage          | Storage/storageAccounts/BlobStorage   |
| Storage Account (File)         | Storage/storageAccounts         | FileStorage          | Storage/storageAccounts/FileStorage   |
| SQL Server                     | Sql/servers                     |               | Sql/servers                          |
| SQL Database                   | Sql/servers/databases           |               | Sql/servers/databases                |
| Virtual Machine                | Compute/virtualMachines         |               | Compute/virtualMachines              |
| Virtual Machine Scale Set      | Compute/virtualMachineScaleSets |               | Compute/virtualMachineScaleSets       |
| Cosmos DB                      | DocumentDB/databaseAccounts     |               | DocumentDB/databaseAccounts          |
| Key Vault                      | KeyVault/vaults                 |               | KeyVault/vaults                      |
| Application Insights           | Insights/components             |               | Insights/components                  |
| Logic App                      | Logic/workflows                 |               | Logic/workflows                      |
| Event Hub Namespace            | EventHub/namespaces             |               | EventHub/namespaces                  |
| Event Hub                      | EventHub/namespaces/eventhubs   |               | EventHub/namespaces/eventhubs        |
| Service Bus Namespace          | ServiceBus/namespaces           |               | ServiceBus/namespaces                |
| Service Bus Queue              | ServiceBus/namespaces/queues    |               | ServiceBus/namespaces/queues         |
| Container Registry             | ContainerRegistry/registries     |               | ContainerRegistry/registries          |
| AKS Cluster                    | ContainerService/managedClusters|               | ContainerService/managedClusters      |
| Application Gateway            | Network/applicationGateways     |               | Network/applicationGateways           |
| Load Balancer                  | Network/loadBalancers           |               | Network/loadBalancers                |
| Virtual Network                | Network/virtualNetworks         |               | Network/virtualNetworks              |
| Public IP Address              | Network/publicIPAddresses       |               | Network/publicIPAddresses            |
| DNS Zone                       | Network/dnsZones                |               | Network/dnsZones                     |
| Resource Group                 | Resources/resourceGroups        |               | Resources/resourceGroups             |
| Automation Account             | Automation/automationAccounts   |               | Automation/automationAccounts        |
| Batch Account                  | Batch/batchAccounts             |               | Batch/batchAccounts                  |
| Data Factory                   | DataFactory/factories           |               | DataFactory/factories                |
| Synapse Workspace              | Synapse/workspaces              |               | Synapse/workspaces                   |
| Redis Cache                    | Cache/Redis                     |               | Cache/Redis                          |
| Cognitive Services             | CognitiveServices/accounts      |               | CognitiveServices/accounts           |
| Managed Identity               | ManagedIdentity/userAssignedIdentities |         | ManagedIdentity/userAssignedIdentities|


## ThemeResources
| ThemeResource Key                       |Purpose                                                   |Typical Target Element         | 
|-----------------------------------------|----------------------------------------------------------|-------------------------------|
| AccentFillColorDefaultBrush             | Main accent color for controls (e.g., buttons, toggles)  | Button, ToggleSwitch, etc.    | 
| AccentTextFillColorPrimaryBrush         | Foreground color for text on accent backgrounds          | TextBlock, Button             | 
| ControlBackgroundFillColorDefaultBrush  | Default background for controls                          | Button, TextBox, ComboBox     | 
| ControlStrokeColorDefaultBrush          | Default border color for controls                        | Button, TextBox, ComboBox     | 
| ControlStrongStrokeColorDefaultBrush    | Stronger border color for controls (e.g., focused state) | Button, TextBox, ComboBox     | 
| ControlElevationBorderBrush             | Border for elevated controls (e.g., flyouts, popups)     | Flyout, Popup                 |
| SystemFillColorAttentionBrush           | Used for attention-seeking UI (e.g., warnings, errors)   | InfoBar, MessageBar           | 
| SystemFillColorSuccessBrush             | Used for success UI (e.g., success messages)             | InfoBar, MessageBar           | 
| SystemFillColorCautionBrush             | Used for caution UI (e.g., warnings)                     | InfoBar, MessageBar           | 
| SystemFillColorCriticalBrush            | Used for critical UI (e.g., errors)                      | InfoBar, MessageBar           | 
| TextFillColorSecondaryBrush             | Secondary text color (less prominent than primary)       | TextBlock, TextBox            | 
| TextFillColorTertiaryBrush              | Tertiary text color (even less prominent)                | TextBlock, TextBox            | 
| TextFillColorDisabledBrush              | Text color for disabled state                            | TextBlock, TextBox            | 
| ListViewItemBackgroundSelectedBrush     | Background for selected items in a ListView              | ListViewItem                  | 
| ListViewItemBackgroundPointerOverBrush  | Background for hovered items in a ListView               | ListViewItem                  | 
| MenuBarBackgroundBrush                  | Background for menu bars                                 | MenuBar                       | 
| MenuBarItemForegroundBrush              | Foreground for menu bar items                            | MenuBarItem                   | 
| HyperlinkForeground                     | Foreground color for hyperlinks                          | Hyperlink, HyperlinkButton    |