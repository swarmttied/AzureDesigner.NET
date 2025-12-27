using System.ComponentModel;
using System.Resources;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.CosmosDB;
using Azure.ResourceManager.Storage;
using AzureDesigner.Models;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using SKLIb;

namespace AzureDesigner.AIContexts.CosmosDB
{
    public class CosmosDBFunctions(ICredentialFactory credentialFactory, IIdMapping idMapping) : IFunctionCalled, INameToIdResolver, IAIFunctionsSource
    {
        readonly ICredentialFactory _credentialFactory = credentialFactory;
        readonly IIdMapping _idMapping = idMapping;

        public event EventHandler<FunctionCallEventArgs> FunctionCalled;

        [KernelFunction]
        [Description("The function to call for get info of resource types in the form 'microsoft.documentdb/*'. Do not call for SQL Sever DB.")]
        public async Task<CosmosDBAccountData> GetCosmosDBInfo(int id)
        {
            string fullId = _idMapping.GetFullId(id);
            FunctionCalled?.Invoke(this, new FunctionCallEventArgs($"""{nameof(GetCosmosDBInfo)}("{id}")"""));

            if (string.IsNullOrWhiteSpace(fullId))
                return null;

            var credential = _credentialFactory.CreateCredential();
            var armClient = new ArmClient(credential);
            var resourceId = new ResourceIdentifier(fullId);
            try
            {
                var cosmosDB = armClient.GetCosmosDBAccountResource(resourceId);
                CosmosDBAccountResource cosmosAccount = await cosmosDB.GetAsync();
                CosmosDBAccountResource resource = cosmosAccount;
                CosmosDBAccountData data = resource.Data;

                return data;
            }
            catch
            {
                return null;
            }
        }

        [KernelFunction]
        [Description("Updates the local authentication settings of resource types 'microsoft.documentdb/databaseaccounts', 'microsoft.documentdb/databaseaccounts/globaldocumentdb'")]
        public async Task<bool> UpdateCosmosDBLocalAuth(int id, bool enable)
        {
            string fullId = _idMapping.GetFullId(id);
            FunctionCalled?.Invoke(this, new FunctionCallEventArgs($"""{nameof(UpdateCosmosDBLocalAuth)}("{id}")"""));

            if (string.IsNullOrWhiteSpace(fullId))
                return false;

            try
            {
                var credential = _credentialFactory.CreateCredential();
                var armClient = new ArmClient(credential);
                var resourceId = new ResourceIdentifier(fullId);

                var cosmosDB = armClient.GetCosmosDBAccountResource(resourceId);

                var patchData = new Azure.ResourceManager.CosmosDB.Models.CosmosDBAccountPatch()
                {
                    DisableLocalAuth = enable
                };

                await cosmosDB.UpdateAsync(Azure.WaitUntil.Completed, patchData);
                return true;
            }
            catch
            {
                return false;
            }
        }
        [KernelFunction]
        public async Task<bool> UpdateCosmosDBPublicNetworkAccess(int id, string ipAddress)
        {
            string fullId = _idMapping.GetFullId(id);
            FunctionCalled?.Invoke(this, new FunctionCallEventArgs($"""{nameof(UpdateCosmosDBPublicNetworkAccess)}("{id}")"""));

            if (string.IsNullOrWhiteSpace(fullId))
                return false;

            try
            {
                var credential = _credentialFactory.CreateCredential();
                var armClient = new ArmClient(credential);
                var resourceId = new ResourceIdentifier(fullId);

                var cosmosDB = armClient.GetCosmosDBAccountResource(resourceId);

                var patchData = new Azure.ResourceManager.CosmosDB.Models.CosmosDBAccountPatch()
                {
                    PublicNetworkAccess = Azure.ResourceManager.CosmosDB.Models.CosmosDBPublicNetworkAccess.Disabled
                };

                await cosmosDB.UpdateAsync(Azure.WaitUntil.Started, patchData);
                return true;
            }
            catch
            {
                return false;
            }
        }

        IDictionary<string, int> _nodeDict;
        void INameToIdResolver.SetResolverSource(IEnumerable<Node> nodes)
        {
            _nodeDict = nodes.Where(o => o.Type.Contains("microsoft.documentdb", StringComparison.InvariantCultureIgnoreCase)).ToDictionary(n => n.Name, no => no.Id);
        }

        [KernelFunction]
        public int ResolveCosmosDBNameToID(string name)
        {
            FunctionCalled?.Invoke(this, new FunctionCallEventArgs($"""{nameof(ResolveCosmosDBNameToID)}("{name})" """));

            if (!_nodeDict.TryGetValue(name, out int id))
            {
                return -1;
            }

            return id;
        }

        public IEnumerable<AITool> GetAIFunctions()
            => [AIFunctionFactory.Create(GetCosmosDBInfo),
                AIFunctionFactory.Create(UpdateCosmosDBLocalAuth),
                AIFunctionFactory.Create(UpdateCosmosDBPublicNetworkAccess)
            ];
    }
}
