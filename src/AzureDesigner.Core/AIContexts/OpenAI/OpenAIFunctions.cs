using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AzureDesigner.Models;
using AzureDesigner.Services;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using SKLIb;

namespace AzureDesigner.AIContexts.OpenAI
{
    public class OpenAIFunctions : IFunctionCalled, INameToIdResolver, IAIFunctionsSource
    {
        readonly ICredentialFactory _credentialFactory;
        readonly IRbacService _rbacService;
        readonly IRoleGuids _roleGuids;
        readonly IIdMapping _idMapping;

        public event EventHandler<FunctionCallEventArgs> FunctionCalled;

        public OpenAIFunctions(ICredentialFactory credentialFactory, IRbacService rbacService,
            IRoleGuids roleGuids, IIdMapping idMapping)
        {
            _credentialFactory = credentialFactory;
            _rbacService = rbacService;
            _roleGuids = roleGuids;
            _idMapping = idMapping;
        }

        [KernelFunction]
        public async Task<IEnumerable<string>> GetOpenAIManagedIdentityIdsWithRbacAccess(int id, string roleName)
        {
            string fullId = _idMapping.GetFullId(id);
            FunctionCalled?.Invoke(this, new FunctionCallEventArgs($"""{nameof(GetOpenAIManagedIdentityIdsWithRbacAccess)}("{id}", "{roleName}")"""));
            var roleDefGuid = _roleGuids[roleName];
            var clientIds = await _rbacService.GetClientIdsWithRbacAsync(fullId, roleDefGuid);
            return clientIds;
        }

        [KernelFunction]
        public async Task AddManagedIdentityWithRbacRoleToOpenAI(int openAIId, string roleName, string clientId)
        {
            string fullId = _idMapping.GetFullId(openAIId);
            var roleDefGuid = _roleGuids[roleName];
            await _rbacService.AddClientIdWithRbacAsync(fullId, roleDefGuid, clientId);
        }

        IDictionary<string, int> _nodeDict;
        void INameToIdResolver.SetResolverSource(IEnumerable<Node> nodes)
        {
            _nodeDict = nodes.Where(o => o.Type.Contains("openai", StringComparison.InvariantCultureIgnoreCase)).ToDictionary(n => n.Name, no => no.Id);
        }

        [KernelFunction]
        public int ResolveOpenAINameToID(string name)
        {
            FunctionCalled?.Invoke(this, new FunctionCallEventArgs($"""{nameof(ResolveOpenAINameToID)}("{name})" """));

            if (!_nodeDict.TryGetValue(name, out int id))
            {
                return -1;
            }
            return id;
        }

        public IEnumerable<AITool> GetAIFunctions()
        {
            return [AIFunctionFactory.Create(ResolveOpenAINameToID),
                    AIFunctionFactory.Create(GetOpenAIManagedIdentityIdsWithRbacAccess),
                    AIFunctionFactory.Create(AddManagedIdentityWithRbacRoleToOpenAI)];
        }
    }
}
