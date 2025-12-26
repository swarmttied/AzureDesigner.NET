using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AzureDesigner.Models;
using Microsoft.SemanticKernel;
using SKLIb;

namespace AzureDesigner.AIContexts.SqlServer
{
    public class SqlServerFunctions : IFunctionCalled, INameToIdResolver
    {
        IDictionary<string, int> _nodeDict;
        readonly IIdMapping _idMapping;

        public event EventHandler<FunctionCallEventArgs> FunctionCalled;

        public SqlServerFunctions(IIdMapping idMapping)
        {
            _idMapping = idMapping;
        }

        void INameToIdResolver.SetResolverSource(IEnumerable<Node> nodes)
        {
            _nodeDict = nodes.Where(o => o.Type.Contains("microsoft.sql/servers/v12.0", StringComparison.InvariantCultureIgnoreCase)).ToDictionary(n => n.Name, no => no.Id);
        }

        [KernelFunction]
        public int ResolveSqlServerNameToID(string name)
        {
            FunctionCalled?.Invoke(this, new FunctionCallEventArgs($"""{nameof(ResolveSqlServerNameToID)}("{name})" """));

            if (!_nodeDict.TryGetValue(name, out int id))
            {
                return -1;
            }

            return id;
        }
    }
}