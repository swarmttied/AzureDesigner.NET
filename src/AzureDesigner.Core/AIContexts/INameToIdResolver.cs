using AzureDesigner.Models;

namespace AzureDesigner.AIContexts
{
    public interface INameToIdResolver
    {
        void SetResolverSource(IEnumerable<Node> nodes);
    }
}
