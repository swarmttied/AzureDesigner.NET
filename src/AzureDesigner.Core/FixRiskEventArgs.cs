using AzureDesigner.Models;

namespace AzureDesigner
{
    public class FixRiskEventArgs
    {
        public FixRiskRequest Request { get; }
        public FixRiskEventArgs(FixRiskRequest request)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
        }
    }
}