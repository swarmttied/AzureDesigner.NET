using Azure.Core;
using Azure.Identity;

namespace AzureDesigner
{
    public interface ICredentialFactory
    {
        TokenCredential CreateCredential();
    }

    public class CredentialFactory(string tenantId) : ICredentialFactory
    {
        readonly string _tenantId = tenantId;       

        TokenCredential _credential = null!;
        public TokenCredential CreateCredential()
        {
            _credential ??= new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions
            {
                TenantId = _tenantId
            });
            return _credential;
        }
    }
}
