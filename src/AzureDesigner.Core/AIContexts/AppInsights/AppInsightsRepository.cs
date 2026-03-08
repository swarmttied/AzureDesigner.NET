using Azure.ResourceManager;
using Azure.ResourceManager.ApplicationInsights;
using Microsoft.SemanticKernel;
using SKLIb;

namespace AzureDesigner.AIContexts.AppInsights;

public interface IAppInsightsRepository
{
    event EventHandler<FunctionCallEventArgs> FunctionCalled;

    Task LoadAppInsights(string subdcriptionId);
    int ResolveAppInsightInstrumentationKeyToID(Guid instrumentationKey);
}

public class AppInsightsRepository(ICredentialFactory credentialFactory, IIdMapping idMapping) : IAppInsightsRepository
{
    readonly ICredentialFactory _credentialFactory = credentialFactory ?? throw new ArgumentNullException(nameof(credentialFactory));
    readonly IIdMapping _idMapping = idMapping;

    Dictionary<Guid, string> _appInsightsIdLookup = new();

    public event EventHandler<FunctionCallEventArgs> FunctionCalled;

    public async Task LoadAppInsights(string subdcriptionId)
    {

        var armClient = new ArmClient(_credentialFactory.CreateCredential(), subdcriptionId);
        var subscription = await armClient.GetDefaultSubscriptionAsync();

        var appInsights = subscription.GetApplicationInsightsComponentsAsync();

        await foreach (var appInsight in appInsights)
        {
            var data = appInsight.Data;
            var id = data?.Id;
            var key = Guid.Parse(data?.InstrumentationKey);
            _appInsightsIdLookup[key] = id;
        }
    }


    [KernelFunction]
    public int ResolveAppInsightInstrumentationKeyToID(
        Guid instrumentationKey)
    {
        FunctionCalled?.Invoke(this, new FunctionCallEventArgs($"""{nameof(ResolveAppInsightInstrumentationKeyToID)}("{instrumentationKey}") """));

        if (!_appInsightsIdLookup.ContainsKey(instrumentationKey))
        {
            return -1;
        }

        string fullId = _appInsightsIdLookup[instrumentationKey];
        int compactId = _idMapping.GetCompactId(fullId);
        return compactId;
    }
}
