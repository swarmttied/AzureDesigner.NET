using Azure.Core;

namespace AzureDesigner.AIContexts.AppInsights
{
    public class AppInsightsDataLite
    {
        public int Id { get; set; }
        public string ProvisioningState { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int? RetentionInDays { get; set; }
        public List<ResourceIdentifier> PrivateLinkScopedResources { get; set; } = [];
    }
}
