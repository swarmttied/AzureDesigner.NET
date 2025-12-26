using System;
using System.Collections.Generic;
using System.Linq;
using AzureDesigner.Models;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using SKLIb;

namespace AzureDesigner.AIContexts.Translation
{
    public class TranslationFunctions : IFunctionCalled, INameToIdResolver, IAIFunctionsSource
    {
        IDictionary<string, int> _nodeDict;
        readonly IIdMapping _idMapping;

        public event EventHandler<FunctionCallEventArgs> FunctionCalled;

        public TranslationFunctions(IIdMapping idMapping)
        {
            _idMapping = idMapping;
        }

        void INameToIdResolver.SetResolverSource(IEnumerable<Node> nodes)
        {
            _nodeDict = nodes
                .Where(o => o.Type.Contains("Microsoft.CognitiveServices/accounts/TextTranslation", StringComparison.InvariantCultureIgnoreCase))
                .ToDictionary(n => n.Name, n => n.Id);
        }

        [KernelFunction]
        public int ResolveTranslationAccountNameToID(string name)
        {
            FunctionCalled?.Invoke(this, new FunctionCallEventArgs($"{nameof(ResolveTranslationAccountNameToID)}(\"{name}\") "));

            if (!_nodeDict.TryGetValue(name, out int id))
            {
                return -1;
            }

            return id;
        }

        public IEnumerable<AITool> GetAIFunctions()
        {
            return [AIFunctionFactory.Create(ResolveTranslationAccountNameToID)];
        }
    }
}