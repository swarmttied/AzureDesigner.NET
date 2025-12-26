using AzureDesigner.Models;

namespace AzureDesigner.AIContexts.AppInsights
{
    public class AppInsightsPromptsSource : IPromptsSource
    {
        public string GetDependencyPrompt(Node root)
        {
            string prompt =
$@"Find all services that this service with serviceID {root.Id} and type {root.Type} depends on. We identify this as 'root'.
First, get the root's info and use it to identify dependencies in the Service IDs section. 

The root cannot depend on itself so its Service ID should not be part of the Dependency IDs.

Return the results in the JSON schema format below. Remove all comments. Validate it before returning to me.
{{
    ""RequestType"": ""Dependencies"", // Do not modify this
    ""Id"": ""{root.Id}"", // Do not modify this
    ""DependencyIds"": [ ""service id 1"", ""service id 2"" ],
    ""Risks"": [ ""attribute name=attributevalue - issue description"" ],
    ""Issues"": {{ 
                    ""ResourceId 1"", [""Certain issue with Resource Id1"", ""more issue...""],  
                    ""ResourceId 2"", [""Certain issue with Resource Id2"", ""more issue...""]    
                }}
}}      

Use the guidelines in https://learn.microsoft.com/en-us/azure/well-architected/service-guides/application-insights for identifying risk in the root.
Add only risk that can be fixed by changing the attribute's value.";


            return prompt;
        }
    }
}
