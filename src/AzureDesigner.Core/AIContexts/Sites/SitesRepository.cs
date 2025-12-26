using Azure.ResourceManager;
using Azure.ResourceManager.AppService;
using AzureDesigner.Models;
using Microsoft.SemanticKernel;
using SKLIb;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureDesigner.AIContexts.Sites
{
    public interface ISitesRepository : IFunctionCalled
    {
        string GetSiteServiceId(string siteName);
        Task LoadSitesAsync(string subscriptionId);
    }

    public class SitesRepository : ISitesRepository
    {
        readonly ICredentialFactory _credentialFactory;

        // Use site name as key
        Dictionary<string, string> _siteLookup = new();

        public event EventHandler<FunctionCallEventArgs> FunctionCalled;

        public SitesRepository(ICredentialFactory credentialFactory)
        {
            _credentialFactory = credentialFactory ?? throw new ArgumentNullException(nameof(credentialFactory));
        }

        public async Task LoadSitesAsync(string subscriptionId)
        {
            var armClient = new ArmClient(_credentialFactory.CreateCredential(), subscriptionId);
            var subscription = await armClient.GetDefaultSubscriptionAsync();
            var sitesCollection = subscription.GetWebSites();

            foreach (var site in sitesCollection)
            {
                var data = site.Data;
                var name = data.Name ?? string.Empty;
                if (!string.IsNullOrEmpty(name))
                {
                    _siteLookup[name] = site.Data.Id;
                }
            }
        }

        [KernelFunction]
        public string GetSiteServiceId([Description("The human-readable name of the site")] string siteName)
        {
            FunctionCalled?.Invoke(this, new FunctionCallEventArgs($"""{nameof(GetSiteServiceId)}("{siteName}")"""));

            if (_siteLookup.TryGetValue(siteName, out var siteId))
            {
                return siteId;
            }
            return "";
        }
    }
}
