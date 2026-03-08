using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AzureDesigner.Models;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using SKLIb;

namespace AzureDesigner.AIContexts.SqlServerDb;

public class SqlServerDbFunctions(IIdMapping idMapping) : IFunctionCalled, INameToIdResolver, IAIFunctionsSource
{
    readonly IIdMapping _idMapping = idMapping;
    public event EventHandler<FunctionCallEventArgs> FunctionCalled;

    IDictionary<string, int> _nodeDict;
    void INameToIdResolver.SetResolverSource(IEnumerable<Node> nodes)
    {
        _nodeDict = nodes.Where(o => o.Type.Contains("microsoft.sql/servers/databases/", StringComparison.InvariantCultureIgnoreCase)).ToDictionary(n => n.Name, no => no.Id);

    }

    [KernelFunction]
    public int ResolveSqlServerDbNameToID(string name)
    {
        FunctionCalled?.Invoke(this, new FunctionCallEventArgs($"""{nameof(ResolveSqlServerDbNameToID)}("{name})" """));

        if (!_nodeDict.TryGetValue(name, out int id))
        {
            return -1;
        }

        return id;
    }

    public IEnumerable<AITool> GetAIFunctions()
    {
        return [AIFunctionFactory.Create(ResolveSqlServerDbNameToID)];
    }
}
