using Azure.Core;
using Azure.Identity;

namespace AzureDesigner
{
    public interface ICredentialFactory
    {
        TokenCredential CreateCredential();
    }

    public class CredentialFactory : ICredentialFactory
    {
        readonly string _tenantId;
        public CredentialFactory(string tenantId)
        {
            _tenantId = tenantId;
        }

        TokenCredential _credential = null!;
        public TokenCredential CreateCredential()
        {
            _credential ??= new AzureCliCredential();
                
            //    new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions
            //{
            //    TenantId = _tenantId
            //});
            return _credential;
        }
    }
}
