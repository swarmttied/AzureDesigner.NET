using AzureDesigner.Models;

namespace AzureDesigner.AIContexts.KeyVault;

public class KeyVaultPromptSource : IPromptsSource
{
    public string GetDependencyPrompt(Node root)
    {
        string prompt =
$@"You will resolve for the IDs of the different resources that this resource (ID:{root.Id} Type: '{root.Type}') depends on. From here on, we refer to this service as 'root'.
You must get the info about the root first. The reference resource IDs in the info contains the name and the service type. Use them to resolve for the resource IDs.

Remember, the root cannot depend on itself so its Service ID should not be part of the Dependency IDs.

Return results in this JSON format:
{{
    ""RequestType"": ""Dependencies"",
    ""Id"": ""{root.Id}"",
    ""DependencyIds"": [ ""service id 1"", ""service id 2"" ],
    ""Risks"": [ ""attribute name=attributevalue - issue description"" ], 
}}      

Use the guidelines in https://learn.microsoft.com/en-us/azure/key-vault/general/best-practices for identifying risk in the root.
Add only risk that can be fixed by changing the attribute's value in the risks array. 
Additionally, check user's ip address is not in the specified virtual networks and IP addresses.

Do not fix the risks, just identify them.";

        return prompt;
    }
}
