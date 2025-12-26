using System.ComponentModel;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.AppService;
using Azure.ResourceManager.AppService.Models;
using AzureDesigner.Models;
using Microsoft.SemanticKernel;
using OpenAI.Assistants;
using SKLIb;

namespace AzureDesigner.AIContexts.Sites;

public class SitesFunctions : IFunctionCalled, INameToIdResolver
{
    readonly ICredentialFactory _credentialFactory;
    readonly IIdMapping _idMapping;

    

    public event EventHandler<FunctionCallEventArgs> FunctionCalled;

    public SitesFunctions(ICredentialFactory credentialFactory, IIdMapping idMapping)
    {
        _credentialFactory = credentialFactory ?? throw new ArgumentNullException(nameof(credentialFactory));
        _idMapping = idMapping ?? throw new ArgumentNullException(nameof(idMapping));
    }


     [KernelFunction]
    //[Description("The function to call for get info of resource type '*/sites/*', '*/sites/*'")]
    public async Task<WebSiteDataLite> GetSitesInfo([Description("The unique identifier in the form /subscriptions/<subscrptionId>/resourceGroups/<resourceGroup>/providers/Microsoft.Web/sites/<siteName>")] int id)
    {
        string fullId = _idMapping.GetFullId(id);

        FunctionCalled?.Invoke(this, new FunctionCallEventArgs($"""{nameof(GetSitesInfo)}("{id}")"""));

        if (string.IsNullOrWhiteSpace(fullId))
            return null;

        var credential = _credentialFactory.CreateCredential();
        var armClient = new ArmClient(credential);
        var resourceId = new ResourceIdentifier(fullId);
        var webSiteResource = await armClient.GetWebSiteResource(resourceId).GetAsync();
        var resource = webSiteResource.Value;
        var data = resource.Data;

        // Offload to a lighter object which is serializable with selected properties
        var liteData = new WebSiteDataLite
        {
            Id= _idMapping.GetCompactId(data.Id),
            State = data.State,
            ManagedIdentityClientIds = await GetSiteUserAssignedManagedIdentityIds(data),
            OutboundIPAddresses = await GetSiteOutboundIpAddresses(data)
        };

        return liteData;
    }

    [KernelFunction]
    public async Task SetSiteState(int id, bool runState)
    {
        FunctionCalled?.Invoke(this, new FunctionCallEventArgs($"""{nameof(SetSiteState)}("{id}", "{runState}")"""));
        string fullId = _idMapping.GetFullId(id);
       

        if (string.IsNullOrWhiteSpace(fullId))
            throw new ArgumentNullException(nameof(fullId));

        var credential = _credentialFactory.CreateCredential();
        var armClient = new ArmClient(credential);
        var resourceId = new ResourceIdentifier(fullId);
        var webSiteResource = await armClient.GetWebSiteResource(resourceId).GetAsync();

        if (webSiteResource.Value == null)
            throw new InvalidOperationException("WebSiteResource not found.");

        Response response = null;

        if (runState)
        {
            response = await webSiteResource.Value.StartAsync();
        }
        else
        {
            response = await webSiteResource.Value.StopAsync();
        }

        if (response.Status != 200 && response.Status != 202)
            throw new InvalidOperationException($"Failed to set run state. Status code: {response.Status}");

    }

    [KernelFunction]
    public async Task DeleteSiteSettings(int id, string[] settingNames)
    {
        FunctionCalled?.Invoke(this, new FunctionCallEventArgs($"""{nameof(DeleteSiteSettings)}("{id}", "{string.Join(",", settingNames)}")"""));
       
        string fullId = _idMapping.GetFullId(id);

        var credential = _credentialFactory.CreateCredential();
        var armClient = new ArmClient(credential);
        var resourceId = new ResourceIdentifier(fullId);

        var websiteResource = armClient.GetWebSiteResource(new ResourceIdentifier(resourceId));

        Response<AppServiceConfigurationDictionary> config = await websiteResource.GetApplicationSettingsAsync();

        foreach (var settingName in settingNames)
        {
            if (config.Value.Properties.ContainsKey(settingName))
            {
                config.Value.Properties.Remove(settingName);
            }
        }
        await websiteResource.UpdateApplicationSettingsAsync(config.Value);
    }

    [KernelFunction]
    public async Task AddSiteSettings([Description("The service id of the web site or function site to which the new settings will be added")] int id, 
                IDictionary<string, string> newSettings)
    {
        FunctionCalled?.Invoke(this, new FunctionCallEventArgs($"""{nameof(AddSiteSettings)}("{id}", "{string.Join(",", newSettings.Select(kv => kv.Key + "=" + kv.Value))}")"""));
       
        string fullId = _idMapping.GetFullId(id);
        var credential = _credentialFactory.CreateCredential();
        var armClient = new ArmClient(credential);
        var resourceId = new ResourceIdentifier(fullId);
        var websiteResource = armClient.GetWebSiteResource(new ResourceIdentifier(resourceId));
        Response<AppServiceConfigurationDictionary> config = await websiteResource.GetApplicationSettingsAsync();
        foreach (var setting in newSettings)
        {
            config.Value.Properties[setting.Key] = setting.Value;
        }
        await websiteResource.UpdateApplicationSettingsAsync(config.Value);
    }

    [KernelFunction]
    public async Task UpdateSiteSettings(int id, IDictionary<string, string> settings)
    {
        FunctionCalled?.Invoke(this, new FunctionCallEventArgs($"""{nameof(UpdateSiteSettings)}("{id}", "{string.Join(",", settings.Select(kv => kv.Key + "=" + kv.Value))}")"""));
       
        string fullId = _idMapping.GetFullId(id);
        var credential = _credentialFactory.CreateCredential();
        var armClient = new ArmClient(credential);
        var resourceId = new ResourceIdentifier(fullId);
        var websiteResource = armClient.GetWebSiteResource(new ResourceIdentifier(resourceId));
        Response<AppServiceConfigurationDictionary> config = await websiteResource.GetApplicationSettingsAsync();
        foreach (var setting in settings)
        {
            if (config.Value.Properties.ContainsKey(setting.Key))
            {
                config.Value.Properties[setting.Key] = setting.Value;
            }           
        }
        await websiteResource.UpdateApplicationSettingsAsync(config.Value);
    }

    async Task<string[]> GetSiteUserAssignedManagedIdentityIds(WebSiteData siteData)
    {
        if (siteData.Identity == null || siteData.Identity.UserAssignedIdentities == null)
        {
            return Array.Empty<string>();
        }
        List<string> identities = new();
        foreach (var identity in siteData.Identity.UserAssignedIdentities)
        {
            identities.Add(Convert.ToString(identity.Value.ClientId));
        }
        var values = identities.ToArray();
        return values;
    }


    async Task<string[]> GetSiteOutboundIpAddresses(WebSiteData siteData)
    {
        string[] ips = siteData.OutboundIPAddresses?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
        return ips;
    }

    IDictionary<string, int> _nodeDict;
    void INameToIdResolver.SetResolverSource(IEnumerable<Node> nodes)
    {
        _nodeDict = nodes.Where(o => o.Type.Contains("site", StringComparison.InvariantCultureIgnoreCase)).ToDictionary(n => n.Name, no => no.Id);
    }

    [KernelFunction]
    public int ResolveSiteNameToID(string name)
    {
        FunctionCalled?.Invoke(this, new FunctionCallEventArgs($"""{nameof(ResolveSiteNameToID)}("{name})" """));

        if (!_nodeDict.TryGetValue(name, out int id))
        {
            return -1;
        }

        return id;
    }
}
