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

public class AppInsightsRepository : IAppInsightsRepository
{
    readonly ICredentialFactory _credentialFactory;
    readonly IIdMapping _idMapping;

    Dictionary<Guid, string> _appInsightsIdLookup = new();

    public event EventHandler<FunctionCallEventArgs> FunctionCalled;

    public AppInsightsRepository(ICredentialFactory credentialFactory, IIdMapping idMapping)
    {
        _credentialFactory = credentialFactory ?? throw new ArgumentNullException(nameof(credentialFactory));
        _idMapping = idMapping;
    }

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
