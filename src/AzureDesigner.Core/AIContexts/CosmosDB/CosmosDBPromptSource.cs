using AzureDesigner.Models;

namespace AzureDesigner.AIContexts.CosmosDB;

public class CosmosDbPromptSource : IPromptsSource
{
    public string GetDependencyPrompt(Node root)
    {
        string prompt =
$@"You are searching all the services that this service with serviceID {root.Id} and type {root.Type} depends on. From here on, we refer to this service as 'root'.
You must get the info about the root first. The info contains the full resource IDs of the resources that the root depends on. Use them to resolve for the compact IDs.

Remember, the root cannot depend on itself so its Service ID should not be part of the Dependency IDs.

Return to me the Id in JSON schema like this:
{{
    ""RequestType"": ""Dependencies"", // Do not modify this
    ""Id"": ""{root.Id}"", // Do not modify this
    ""DependencyIds"": [ ""compact id 1"", ""compact id 2"" ],
    ""Risks"": [ ""Details about the issue 1"", ""More issues "" ]  
}}

Risks pertain to security risks. Check for the following root info properties and add an entry in the Risks array if the following conditions are true. Each condition should be a separate entry in the Risks array.

Conditions to check using the CosmosDBAccountData properties:
- DisableLocalAuth is false or null (Local authentication/access keys are enabled)
- PublicNetworkAccess is not 'Disabled' (Public network access is enabled)
- PrivateEndpointConnections is null or empty (No private endpoint is configured)

For each risk identified, provide a clear description of the security concern.

Validate the result JSON";

        return prompt;
    }
}
