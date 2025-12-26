using AzureDesigner.Models;
using Microsoft.SemanticKernel;
using SKLIb;

namespace AzureDesigner.AIContexts.GenericResource;

public class GenericResourceFunctions : IFunctionCalled, INameToIdResolver
{
    public event EventHandler<FunctionCallEventArgs> FunctionCalled;

    Dictionary<string, int> _fullResourceIdToIdMap = new(StringComparer.InvariantCultureIgnoreCase);

    public void SetResolverSource(IEnumerable<Node> nodes)
    {
        _fullResourceIdToIdMap = nodes.ToDictionary(n => n.ResourceId, n => n.Id, StringComparer.InvariantCultureIgnoreCase);
    }

    [KernelFunction]
    public int ResolveFullResourceIdToCompactId(string fullResourceId)
    {
        FunctionCalled?.Invoke(this, new FunctionCallEventArgs($"""{nameof(ResolveFullResourceIdToCompactId)}("{fullResourceId}")"""));

        if (_fullResourceIdToIdMap.TryGetValue(fullResourceId, out var compactId))
        {
            return compactId;
        }

        throw new ArgumentException($"Unknown full resource ID: {fullResourceId}", nameof(fullResourceId));
    }
}
