using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureDesigner.AIContexts.Sites
{
    public class WebSiteDataLite
    {
        public int Id { get; set; }
        public string State { get; set; } = string.Empty;
        public string[] ManagedIdentityClientIds { get; set; } = [];
        public string[] OutboundIPAddresses { get; set; } = Array.Empty<string>();
    }
}
