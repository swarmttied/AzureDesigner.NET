using System.ComponentModel;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.ApplicationInsights;
using Azure.ResourceManager.Resources;
using AzureDesigner.Models;
using Microsoft.SemanticKernel;
using SKLIb;

namespace AzureDesigner.AIContexts.AppInsights
{
    public class AppInsightsFunctions : IFunctionCalled
    {
        readonly ICredentialFactory _credentialFactory;
        readonly IIdMapping _idMapping;
        public AppInsightsFunctions(ICredentialFactory credentialFactory, IIdMapping idMapping)
        {
            _credentialFactory = credentialFactory;
            _idMapping = idMapping;
        }

        public event EventHandler<FunctionCallEventArgs> FunctionCalled;

        [KernelFunction]    
        public async Task<AppInsightsDataLite> GetAppInsightsInfo(int id)
        {
            string fullId = _idMapping.GetFullId(id);
            FunctionCalled?.Invoke(this, new FunctionCallEventArgs($"""{nameof(GetAppInsightsInfo)}("{id}")"""));

            if (string.IsNullOrWhiteSpace(fullId))
                return null;

            var credential = _credentialFactory.CreateCredential();
            var armClient = new ArmClient(credential);
            var resourceId = new ResourceIdentifier(fullId);
            var componentResource = await armClient.GetApplicationInsightsComponentResource(resourceId).GetAsync();
            var component = componentResource.Value;
            var data = component.Data;

            // Offload to a lighter object which is serializable with selected properties
            var liteData = new AppInsightsDataLite
            {
                ProvisioningState = data.ProvisioningState,
                Name = data.Name,
                Id = _idMapping.GetCompactId(data.Id),
                RetentionInDays = data.RetentionInDays,
                PrivateLinkScopedResources = await GetPrivateLinkScopedResources(data)
            };

            return liteData;
        }

        async Task<List<ResourceIdentifier>> GetPrivateLinkScopedResources(ApplicationInsightsComponentData appInsightsData)
        {
            var result = new List<ResourceIdentifier>();
            foreach (var resource in appInsightsData.PrivateLinkScopedResources)
            {
                var resourceId = resource.ToString();
                var resourceIdentifier = new ResourceIdentifier(resourceId);
                result.Add(resourceIdentifier);
            }
            return await Task.FromResult(result);
        }
    }
}
