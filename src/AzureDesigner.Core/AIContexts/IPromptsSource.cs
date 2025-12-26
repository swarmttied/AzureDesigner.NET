using AzureDesigner.Models;

namespace AzureDesigner.AIContexts;

public interface IPromptsSource
{
    string GetDependencyPrompt(Node root);
}