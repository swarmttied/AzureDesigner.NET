using System.Text.Json;
using AzureDesigner.Models;

namespace AzureDesigner.AIContexts.Sites
{
    public class SitesPromptsSource : IPromptsSource
    {
        public string GetDependencyPrompt(Node root)
        {
            string settingsJson = JsonSerializer.Serialize(root.Settings, new JsonSerializerOptions { WriteIndented = true });
            string prompt =
$@"You will resolve for the IDs of the different resources that this resource (ID:{root.Id} Type: '{root.Type}') depends on. Let's refer to this service as the 'root'.
You find those references in the Settings section and  in root's info.After learning about the root and its dependencies, you will also identify Risks and Issues regarding them.
Do not fix anything. Just identify the dependencies, risks and issues. Consider only ID's > 0 as valid resource IDs. Ignore any ID <= 0.

References can be any of the following formats:
- Resource ID of the resource. Example value is '/subscriptions/xxxx/resourceGroups/xxxx/providers/Microsoft.Storage/storageAccounts/xxx'. 
  Here the resource type is 'storageAccounts' and the resource name is the 'xxx' that follows.
- Name of the reource which you can use to resolve for the ID. Use the key name for context to identify the resource type
- URLs, endpoints, connection strings which may contain the name of the resource and the type of the resource
    Example value is 'https://arc-open.openai.azure.com'. Here the resource name is 'arc-open' and the resource type is 'open-ai'.
- If two or more setting values are the same, consider the key name to differentiate their types. 
- For a Client ID settings, the value is a Managed Identity client ID and you can use this to resolve for the ID of the Managed Identity resource.
- Database connection strings may contain both the database server name and the database name. Resolve for both resources. For db name, use the format '<db server name>/<db name>'.
- APPINSIGHTS_INSTRUMENTATIONKEY is a GUID you can use to resolve for the ID of an AppInsights resource
  
Remember, this site cannot depend on itself so its ID should not be part of the Depenendency IDs.

Risk Analyses:
----------------------------------
Risks pertain to security risks of the root. Use the following guidelines on when to add entry in the Risks. 
1. The value for the setting 'ManagedIdentityClientId' is not in the root's managed identity client id's. Show the settings and root managed identity client IDs. Evaluate only the 'ManagedIdentityClientId' setting.
   Include the root's MI client ID in the description of this issue.If the setting is not present, then skip this risk.
If the root sub type is a Function app, then evaluate the following additional risks:
    2. Presence of setting 'AzureWebJobsStorage' whose value is a connection string. This is a security risk for it contains the account key.
       When fixing this later, you should also fix Issue #3 so not to lose the name of the storage account.
    3. Absence of setting 'AzureWebJobsStorage__accountname'. The value have been set to the name of a Storage Account in the 'AzureWebJobsStorage' connection string.
    4. Absence of setting 'AzureWebJobsStorage__clientId'. The value should have been set to a Managed Identity client id of the root. Include the root's MI client id in the description of this issue.
    5. Absence of setting 'AzureWebJobsStorage__credential'. The value should have been set to 'managedidentity'.

Issues Analyses:
----------------------------------
Issues are things that can break the app and cause downtime.

For the root:
- Its state is not 'Running'
- The 'ManagedIdentityClientId' setting is missing. It should match one of the assigned managed identity client IDs of the root. 
  If there is at least one match, then there is no issue and no need to add an entry to the Issues collection.
  If there is no match or the setting is missing, then include the root's managed identity client ID in the description of this issue.

For every dependency you identify, check if there are any Issues with regard to that dependency. Use the following guidelines on when to add entry in the Issues section per type of service.
You must get the info of each dependency type if a tool is available to evaluate the conditions.

You may see dependency resource types with an Issue anylysis item like 'Role/s to check: <specified role/s>'. Here is how to evaluate this:
Make sure that Risk Analysis item 2 is OK. If not, then you cannot evaluate this item and must report it as an Issue.
Otherwise, compare the root's managed identity client ID's of Risk #1 with each of the managed identity client ID's of the resource with specified role/s. 
If there is no Managed Identity with such role, this it is an Issue. If there is at least one match, then there is no issue and no need to add an entry to the Risks collection. 
Return the issue in this format ""{{root managed identity client ID}} is missing {{role}}""

- Sites:
    > State is not 'Running'
- Storage Accounts:    
    > Role/s to check: '{RoleNames.StorageBlobDataContributor}'     
- Key Vaults:
    > Role/s to check: '{RoleNames.KeyVaultSecretsUser}'     
- OpenAI:
    > Role/s to check: '{RoleNames.CognitiveServicesOpenAIUser}'. 
      

Do not evaluate Issues and Risks beyond the guidelines provided. If there are no Issues or Risks identified, no need to say so in the result.

You must return the result in JSON like this. Remove all comments. Validate it before returning to me.
{{
    ""RequestType"": ""Dependencies"", // Do not modify this
    ""Id"": ""{root.Id}"", // Do not modify this
    ""DependencyIds"": [ ""service id 1"", ""service id 2"" ],
    ""Risks"": [ ""Details about the issue 1"", ""More issues "" ]
    ""Issues"": {{ 
                    ""ResourceId 1"", [""Certain issue with Resource Id1"", ""more issue...""],  
                    ""ResourceId 2"", [""Certain issue with Resource Id2"", ""more issue...""]    
                }}
}}


Settings:
{settingsJson}";

            return prompt.Trim();
        }
    }
}
