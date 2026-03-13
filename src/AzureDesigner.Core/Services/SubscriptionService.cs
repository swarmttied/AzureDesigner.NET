using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.AppService;
using Azure.ResourceManager.AppService.Models;
using Azure.ResourceManager.Resources;
using AzureDesigner.Models;

namespace AzureDesigner.Services;

public interface ISubscriptionService
{
    Task<IEnumerable<Node>> GetServicesAsync(string subscriptionId);
    Task<IEnumerable<Subscription>> GetSubscriptionIds();

    Task<IEnumerable<ResourceGroup>> GetResourceGroupsAsync(string subscriptionId);

    Task<IEnumerable<Setting>> GetAppSettingsAsync(string resourceId);
}

public class SubscriptionService : ISubscriptionService
{
    private readonly ICredentialFactory _credentialFactory;

    public SubscriptionService(ICredentialFactory credentialFactory)
    {
        _credentialFactory = credentialFactory;
    }

    public async Task<IEnumerable<Subscription>> GetSubscriptionIds()
    {
        TokenCredential credential = _credentialFactory.CreateCredential();
        ArmClient armClient = new(credential);

        var subscriptions = armClient.GetSubscriptions();
        List<Subscription> subscriptionIds = new();

        await foreach (var subscription in subscriptions)
        {
            if (subscription.HasData)
            {
                subscriptionIds.Add(new Subscription
                {
                    Id = subscription.Data.SubscriptionId,
                    Name = subscription.Data.DisplayName
                });
            }
        }

        return subscriptionIds;
    }

    public async Task<IEnumerable<Node>> GetServicesAsync(string subscriptionId)
    {
        TokenCredential credential = _credentialFactory.CreateCredential();
        ArmClient armClient = new(credential);
        var subscription = armClient.GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"));

        List<Node> services = new();

        await foreach (var resource in subscription.GetGenericResourcesAsync())
        {
            if (!resource.HasData)
                continue;

            services.Add(new Node
            {
                ResourceId = resource.Data?.Id ?? "",
                Name = resource.Data.Name,
                Type = GetType(resource.Data),
                ResourceGroupName = resource.Data?.Id?.ResourceGroupName ?? "",
                Location = resource.Data.Location.DisplayName,
                PortalUrl = $"https://portal.azure.com/#@/resource{resource.Data.Id}"
            });
        }

        return services;

        string GetType(GenericResourceData data)
        {
            if (string.IsNullOrEmpty(data.Kind))
                return data.ResourceType.ToString();
            return $"{data.ResourceType}/{data.Kind}"; 
        }
    }

    public async Task<IEnumerable<Setting>> GetAppSettingsAsync(string resourceId)
    {      
        if (!resourceId.Contains("Microsoft.Web/site"))
            return [];

        TokenCredential credential = _credentialFactory.CreateCredential();
        ArmClient armClient = new(credential);

        // After getting the WebSiteResource (web app or function app)
        var websiteResource = armClient.GetWebSiteResource(new ResourceIdentifier(resourceId));

        Response<AppServiceConfigurationDictionary> config = await websiteResource.GetApplicationSettingsAsync();

        var appSettings = new List<Setting>();
        if (config.Value?.Properties == null)
            return appSettings;

        foreach (var kvp in config.Value.Properties)
        {
            appSettings.Add(new Setting
            {
                Key = kvp.Key,
                Value = kvp.Value
            });
        }


        return appSettings;
    }

    public async Task<IEnumerable<ResourceGroup>> GetResourceGroupsAsync(string subscriptionId)
    {
        TokenCredential credential = _credentialFactory.CreateCredential();
        ArmClient armClient = new(credential);
        var subscription = armClient.GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"));

        List<ResourceGroup> resourceGroups = new();

        await foreach (var resourceGroup in subscription.GetResourceGroups())
        {
            if (resourceGroup.HasData)
            {
                resourceGroups.Add(new ResourceGroup
                {
                    Id = resourceGroup.Data.Id,
                    Name = resourceGroup.Data.Name,
                    Location = resourceGroup.Data.Location.DisplayName
                });
            }
        }

        return resourceGroups;
    }
}
