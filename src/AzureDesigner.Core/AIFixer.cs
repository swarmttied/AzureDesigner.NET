using Azure.Core;
using AzureDesigner.Models;
using Newtonsoft.Json;
using SKLIb;

namespace AzureDesigner
{
    // Pseudocode plan:
    // - Add using Newtonsoft.Json.
    // - Replace System.Text.Json.JsonSerializer.Deserialize<T> with JsonConvert.DeserializeObject<T>.
    // - Replace System.Text.Json.JsonSerializer.Serialize with JsonConvert.SerializeObject.
    // - Update catch blocks to catch Newtonsoft.Json.JsonException.

    public interface IAIFixer
    {
        event EventHandler<FixIssueEventArgs>? IssueFixed;
        event EventHandler<FixRiskEventArgs>? RiskFixed;
        event EventHandler<ExceptionEventArgs>? ExceptionOccurred;
        
        Task FixRisksAsync(FixRiskRequest request);
        ISKClient SKClient { get; }

        Task FixDependencyIssuesAsync(FixIssueRequest request);
    }

    public class AIFixer : IAIFixer
    {
        readonly ISKClient _skClient;

        public event EventHandler<FixIssueEventArgs>? IssueFixed;
        public event EventHandler<FixRiskEventArgs>? RiskFixed;
        public event EventHandler<ExceptionEventArgs>? ExceptionOccurred;

        public AIFixer(ISKClient skClient)
        {
            _skClient = skClient ?? throw new ArgumentNullException(nameof(skClient));
            _skClient.ResponseReceived += SkClient_ResponseReceived;
        }

        private void SkClient_ResponseReceived(object? sender, SKClient.ResponseEventArgs e)
        {
            try
            {
                if (e.JsonResponse.Contains("IssueFix"))
                {
                    var response = JsonConvert.DeserializeObject<FixIssueRequest>(e.JsonResponse);
                    IssueFixed?.Invoke(this, new FixIssueEventArgs(response));
                }
                else if (e.JsonResponse.Contains("RiskFix"))
                {
                    var response = JsonConvert.DeserializeObject<FixRiskRequest>(e.JsonResponse);
                    RiskFixed?.Invoke(this, new FixRiskEventArgs(response));
                }
            }
            catch (Newtonsoft.Json.JsonException)
            {
                // Not a valid FixIssueRequest or FixRiskRequest
                ExceptionOccurred?.Invoke(this, new ExceptionEventArgs(new InvalidDataException("Invalid JSON response.")));
            }
            catch (Exception ex)
            {
                ExceptionOccurred?.Invoke(this, new ExceptionEventArgs(ex));
            }
        }

        public async Task FixIssueAsync(int rootServiceId, int serviceId, Issue issue)
        {
            var request = new FixIssueRequest
            {
                RootServiceId = rootServiceId,
                ServiceId = serviceId,
                Issues = [issue]
            };
            await FixDependencyIssuesAsync(request);
        }

        public async Task FixDependencyIssuesAsync(FixIssueRequest request)
        {
            request.RequestType = "IssueFix";
            string payload = JsonConvert.SerializeObject(request);
            string prompt =
@$"Go thru the items in the Issues attribute of the JSON below and try fix the issue one by one. No need to resolve the ServiceID. Use that directly to the tool you think is needed for the fix.
Mark it accordingly in the Fixed field. You don't have to do anything with regard to the RootServiceId and ServiceId fields.
{payload}
Return to me the updated JSON.";
            await _skClient.RunAsync(prompt);
        }

        public async Task FixRisksAsync(FixRiskRequest request)
        {
            request.RequestType = "RiskFix";
            string payload = JsonConvert.SerializeObject(request);
            string prompt =
@$"Go thru the items in the Risks attribute of the JSON below and try fix the risk one by one.No need to resolve the ServiceID. Use that directly to the tool you think is needed for the fix.
Mark it accordingly in the Fixed field.
{payload}
Return to me the updated JSON object.";
            await _skClient.RunAsync(prompt);
        }

        public ISKClient SKClient => _skClient;
    }
}
