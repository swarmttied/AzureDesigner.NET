using AzureDesigner.Models;

namespace AzureDesigner.AIContexts.Storage;

public class StoragePromptSource : IPromptsSource
{
    public string GetDependencyPrompt(Node root)
    {
        string prompt =
$@"You will resolve for the IDs of the different resources that this resource (ID:{root.Id} Type: '{root.Type}') depends on. From here on, we refer to this service as 'root'.
You must get the info about the root first. The reference resource IDs in the info contains the name and the service type. Use them to resolve for the resource IDs.

Remember, the root cannot depend on itself so its Service ID should not be part of the Dependency IDs.

Risks pertain to security risks. Check for the following root info and add an entry in the Risks array if the following conditions are true. Each condition should be a separate entry in the Risks array.
Do not fix the risks, just identify them.

Conditions to check:
- Local user is enabled 
- Blob can be accessed publicly
- Allows Shared Key Access 
- User's ip address is not in the public network access

If the condition is already false, do not include it in the risks.

Return to me the Id in JSON schema like this:
{{
    ""RequestType"": ""Dependencies"", // Do not modify this
    ""Id"": ""{root.Id}"", // Do not modify this
    ""DependencyIds"": [ ""resource id 1"", ""resource id 2"" ],
    ""Risks"": [ ""Details about the issue 1"", ""More issues "" ]  
}}

Validate the result JSON.";


        return prompt;
    }
}
