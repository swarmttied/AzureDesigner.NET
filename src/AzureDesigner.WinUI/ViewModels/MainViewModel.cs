using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using AzureDesigner.AIContexts;
using AzureDesigner.AIContexts.AppInsights;
using AzureDesigner.AIContexts.Identities;
using AzureDesigner.AIContexts.Sites;
using AzureDesigner.Models;
using AzureDesigner.Services;
using AzureDesigner.WinUI.Controls;
using AzureDesigner.WinUI.Models;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SKLIb;
using WinRT;

namespace AzureDesigner.WinUI.ViewModels;

public class MainViewModel : ViewModelBase
{
    #region Fields

    private string _title = null!;
    private bool _isNotBusy;
    private readonly ISubscriptionService _subscriptionService;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly IDependencyHelper _dependencyHelper;
    private List<NodeViewModel> _allServices = null!;
    private Queue<int> _countdownQueue = new Queue<int>();
    private NodeViewModel _requestedNode = null!;
    internal Dictionary<int, NodeViewModel> _nodesLookup = new();
    bool _fetchingDependencies = false; // TODO: this might be redundant with IsNotBusy
   
    private ObservableCollection<Subscription> _subscriptions = new();
    private ObservableCollection<ResourceGroup> _resourceGroups = new();
    private Subscription subscription = null!;
    bool _appsOnly = false;
    private ObservableCollection<NodeViewModel> _services = new();
    NodeViewModel _selectedService = null!;
    private bool _enableGetDependencies;
    private Timer? _countdownTimer;
    private int _countdownSeconds;
    private string _getDependencyText = string.Empty;
    private readonly IIdentityRepository _identityRepository;
    private readonly IAppInsightsRepository _appInsightsRepository;
    readonly IIconPathSource _iconPathSource;
    readonly IPromptsSourceRepository _promptsSourceRepository;
    readonly IAIFixer _aIFixer;
    readonly IResourceTypeNameMapper _resourceTypeNameMapper;
    string _informationText = string.Empty;
    IIdMapping _idMapping;
    readonly object[] _skTools;

    // Backing fields for new bindable properties
    private int? _resourceCount;
    private int? _subscriptionCount;
    private int? _tracedCount;
    private int? _analyzedCount;

    #endregion

    #region Constructor

    public MainViewModel(ISubscriptionService subscriptionService, IDependencyHelper dependencyHelper, IIdentityRepository identityRepository,
       IAIFixer aIFixer, IResourceTypeNameMapper resourceTypeNameMapper, IIdMapping idMapping,
        IPromptsSourceRepository promptsSourceRepository, IAppInsightsRepository appInsightsRepository,
        IIconPathSource iconPathSource, object[] skTools)
    {
        Title = "Main Window";
        InfoBarText = "";
        IsNotBusy = false;
        _subscriptionService = subscriptionService;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _dependencyHelper = dependencyHelper;
        _iconPathSource = iconPathSource;
        _promptsSourceRepository = promptsSourceRepository;
        _appInsightsRepository = appInsightsRepository;
        _resourceTypeNameMapper = resourceTypeNameMapper;
        _idMapping = idMapping;
        _skTools = skTools;
        _dependencyHelper.DependenciesReceived += DependencyHelper_DependenciesReceived;
        _dependencyHelper.SKClient.RequestSent += SKClient_RequestSent;
        _dependencyHelper.SKClient.ResponseReceived += SKClient_ResponseReceived;
        _dependencyHelper.SKClient.RateExceeded += SKClient_RateExceeded;
        _dependencyHelper.SKClient.ExceptionOccurred += SKClient_ExceptionOccured;
        _aIFixer = aIFixer;
        _aIFixer.SKClient.RequestSent += SKClient_RequestSent;
        _aIFixer.SKClient.ResponseReceived += SKClient_ResponseReceived;
        _aIFixer.SKClient.RateExceeded += SKClient_RateExceeded;
        _aIFixer.SKClient.ExceptionOccurred += SKClient_ExceptionOccured;
        _aIFixer.IssueFixed += Fixer_IssueFixed;
        _aIFixer.RiskFixed += Fixer_RiskFixed;
        _aIFixer.ExceptionOccurred += SKClient_ExceptionOccured;

        EvaluateEnableGetDependencies();
        LoadSubscriptionIds();
        _identityRepository = identityRepository;
        HookCalledEvent(skTools);
    }

    private void HookCalledEvent(object[] functions)
    {
        foreach (var function in functions.OfType<IFunctionCalled>())
        {
            function.FunctionCalled += SKClient_FunctionCalled;
        }
    }


    #endregion

    #region Trace Filtering

    bool _showRequests = false;
    public bool ShowRequests
    {
        get => _showRequests;
        set
        {
            if (_showRequests == value)
                return;
            _showRequests = value;
            FilterTraceMessages();
            OnPropertyChanged();
        }
    }

    bool _showResponses = false;
    public bool ShowResponses
    {
        get => _showResponses;
        set
        {
            if (_showResponses == value)
                return;
            _showResponses = value;
            FilterTraceMessages();
            OnPropertyChanged();
        }
    }

    bool _showFunctionCalls = false;
    public bool ShowFunctionCalls
    {
        get => _showFunctionCalls;
        set
        {
            if (_showFunctionCalls == value)
                return;
            _showFunctionCalls = value;
            FilterTraceMessages();
            OnPropertyChanged();
        }
    }

    bool _showExceptions = false;
    public bool ShowExceptions
    {
        get => _showExceptions;
        set
        {
            if (_showExceptions == value)
                return;
            _showExceptions = value;
            FilterTraceMessages();
            OnPropertyChanged();
        }
    }

    bool _showRateLimits = false;
    public bool ShowRateLimits
    {
        get => _showRateLimits;
        set
        {
            if (_showRateLimits == value)
                return;
            _showRateLimits = value;
            FilterTraceMessages();
            OnPropertyChanged();
        }
    }

    bool NoTraceMessageFilter => !ShowRequests && !ShowResponses && !ShowFunctionCalls && !ShowExceptions && !ShowRateLimits;

    List<TraceMessage> _allTraceMessages = new();
    void FilterTraceMessages()
    {
       
        if (NoTraceMessageFilter)
        {
            TraceMessages = new ObservableCollection<TraceMessage>(_allTraceMessages);
            return;
        }

        var filtered =  _allTraceMessages.Where(tm =>
            (tm.Type == "Request" && ShowRequests) ||
            (tm.Type == "Response" && ShowResponses) ||
            (tm.Type == "FunctionCall" && ShowFunctionCalls) ||
            (tm.Type == "Exception" && ShowExceptions) ||
            (tm.Type == "RateLimit" && ShowRateLimits)
        ).ToList();
        TraceMessages = new ObservableCollection<TraceMessage>(filtered);
    }

    #endregion

    #region Properties

    ObservableCollection<TraceMessage> _traceMessages = [];
    
    public ObservableCollection<TraceMessage> TraceMessages
    {
        get => _traceMessages;
        set
        {
            if (_traceMessages == value)
                return;
            _traceMessages = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<Subscription> Subscriptions
    {
        get => _subscriptions;
        set
        {
            if (_subscriptions == value)
                return;
            _subscriptions = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<ResourceGroup> ResourceGroups
    {
        get => _resourceGroups;
        set
        {
            if (_resourceGroups == value)
                return;
            _resourceGroups = value;
            OnPropertyChanged();
        }
    }

    private ResourceGroup _resourceGroup;
    public ResourceGroup ResourceGroup
    {
        get => _resourceGroup;
        set
        {
            if (_resourceGroup == value)
                return;
            _resourceGroup = value;
            OnPropertyChanged();
        }
    }

    public ICommand LoadDataCommand => new RelayCommand(LoadData);

    public ICommand GetDependenciesCommand => new RelayCommand(GetDendencies);

    public Subscription Subscription
    {
        get => subscription;
        set
        {
            if (subscription == value)
                return;
            subscription = value;
            OnPropertyChanged();
        }
    }

    public bool AppsOnly
    {
        get => _appsOnly;
        set
        {
            try
            {
                // Somthing could go wrong here. Haven't investigated yet.

                if (_appsOnly == value)
                    return;

                if (_allServices == null || _allServices.Count == 0)
                    return;

                _appsOnly = value;
                int lastId = SelectedService?.Node.Id ?? -1;
                if (_appsOnly)
                {
                    Services.Clear();
                    foreach (var service in _allServices.Where(s => s.Node.Type.Contains("sites")))
                    {
                        Services.Add(service);
                    }
                }
                else
                {
                    Services.Clear();
                    foreach (var service in _allServices)
                    {
                        Services.Add(service);
                    }
                    ApplyServiceTypeFilter();
                }
                SelectedService = _nodesLookup.ContainsKey(lastId) ? _nodesLookup[lastId] : null!;
                OnPropertyChanged();
                EvaluateEnableGetDependencies();
            }
            catch (Exception ex)
            {
                // Handle exceptions as needed, e.g., log them
                Console.WriteLine($"Error in AppsOnly property setter: {ex.Message}");
            }
        }
    }

    public ObservableCollection<NodeViewModel> Services
    {
        get => _services;
        set
        {
            if (_services == value)
                return;
            _services = value;
            OnPropertyChanged();
        }
    }

    public string Title
    {
        get => _title;
        set
        {
            if (_title == value)
                return;
            _title = value;
            OnPropertyChanged();
        }
    }

    public bool IsNotBusy
    {
        get => _isNotBusy;
        set
        {
            if (_isNotBusy == value)
                return;
            _isNotBusy = value;
            OnPropertyChanged();
        }
    }

    public NodeViewModel SelectedService
    {
        get => _selectedService;
        set
        {
            if (_selectedService == value)
                return;
            _selectedService = value;
            EvaluateEnableGetDependencies();
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanFixIssues));
            OnPropertyChanged(nameof(CanFixRisk));
            OnPropertyChanged(nameof(SettingsApplicable));
        }
    }

    public string InfoBarText
    {
        get => _getDependencyText;
        set
        {
            if (_getDependencyText == value)
                return;
            _getDependencyText = value;
            OnPropertyChanged();
        }
    }

    public bool EnableGetDependencies
    {
        get => _enableGetDependencies;
        set
        {
            if (_enableGetDependencies == value)
                return;
            _enableGetDependencies = value;
            OnPropertyChanged();
        }
    }

    public bool SettingsApplicable => SelectedService != null && (SelectedService.Type.Contains("sites", StringComparison.OrdinalIgnoreCase));

    public int? ResourceCount
    {
        get => _resourceCount;
        set
        {
            if (_resourceCount == value)
                return;
            _resourceCount = value;
            OnPropertyChanged();
        }
    }

    public int? SubscriptionCount
    {
        get => _subscriptionCount;
        set
        {
            if (_subscriptionCount == value)
                return;
            _subscriptionCount = value;
            OnPropertyChanged();
        }
    }

    public int? TracedCount
    {
        get => _tracedCount;
        set
        {
            if (_tracedCount == value)
                return;
            _tracedCount = value;
            OnPropertyChanged();
        }
    }

    public int? AnalyzedCount
    {
        get => _analyzedCount;
        set
        {
            if (_analyzedCount == value)
                return;
            _analyzedCount = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region Data Loading Methods

    private async void LoadSubscriptionIds()
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            IsNotBusy = false;
            Subscriptions.Clear();
        });

        var subs = await _subscriptionService.GetSubscriptionIds();
        _dispatcherQueue.TryEnqueue(() =>
        {
            Subscriptions.Clear();
            foreach (var sub in subs.OrderBy(o => o.Name))
            {
                Subscriptions.Add(sub);
            }
            if (Subscription == null && Subscriptions.Count > 0)
            {
                Subscription = Subscriptions[0];
            }

            IsNotBusy = true;
        });
    }

    private async void LoadResourceGroups()
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            IsNotBusy = false;
            ResourceGroups.Clear();
        });

        var resourceGroups = await _subscriptionService.GetResourceGroupsAsync(Subscription.Id);
        _dispatcherQueue.TryEnqueue(() =>
        {
            ResourceGroups.Clear();
            foreach (var rg in resourceGroups.OrderBy(o => o.Name))
            {
                ResourceGroups.Add(rg);
            }

            IsNotBusy = true;
        });
    }

    private void LoadData(object parameter)
    {
        LoadServices();
    }

    public void LoadServices()
    {
        Task.Run(async () =>
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                IsNotBusy = false;
                InfoBarText = "Fetching resources";
            });

            await _identityRepository.LoadIdentities(Subscription.Id);
            await _appInsightsRepository.LoadAppInsights(Subscription.Id);

            var resources = await _subscriptionService.GetServicesAsync(Subscription.Id);
            AssignCompactIds(resources);
            SetResolvers(resources);

            _dependencyHelper.ReferenceNodes = resources;
            _allServices = [.. resources.Select(s => new NodeViewModel(s, this, _resourceTypeNameMapper))];
            SetIconPaths();

            _dispatcherQueue.TryEnqueue(() =>
            {
                SelectedService = null!;
                Services.Clear();
                foreach (var service in _allServices.OrderBy(o => o.Name))
                {
                    service.EnableGetDependencies = true;
                    Services.Add(service);
                    _nodesLookup[service.Id] = service;
                }

                if (Services.Count > 0)
                    SelectedService = Services.FirstOrDefault();
                else
                    SelectedService = null!;

                AddServiceTypes();
                SubscriptionCount = 1; // 1 for now
                ResourceCount = resources.Count();
                IsNotBusy = true;
                InfoBarText = "";
            });
        });

    }

    private void SetIconPaths()
    {
        foreach (var service in _allServices)
        {
            service.IconPath = _iconPathSource[service.Node.Type];
        }
    }

    void SetResolvers(IEnumerable<Node> resources)
    {
        foreach (INameToIdResolver resolver in _skTools.OfType<INameToIdResolver>())
        {
            resolver.SetResolverSource(resources);
        }
    }

    void AssignCompactIds(IEnumerable<Node> resources)
    {
        foreach (var res in resources)
        {
            _idMapping.Map(res.ResourceId);
            res.Id = _idMapping.GetCompactId(res.ResourceId);
        }
    }


    #endregion

    #region Dependency Methods

    internal void GetDendencies(object parameter)
    {
        NodeViewModel? nvm = parameter as NodeViewModel;
        if (nvm is null || SelectedService == null)
        {
            return;
        }
        if (nvm != SelectedService)
        {
            SelectedService = nvm;
        }

        _dispatcherQueue.TryEnqueue(async () =>
        {
            _informationText = $"""Analysing resource "{SelectedService.Name}" """;
            InfoBarText = _informationText;
            if (SelectedService.Settings == null)
                SelectedService.Settings = await _subscriptionService.GetAppSettingsAsync(SelectedService.Node.ResourceId);
            foreach (var service in _allServices)
            {
                service.EnableGetDependencies = false;
            }

            _requestedNode = SelectedService;
            if (_requestedNode.Dependencies != null && _requestedNode.Dependencies.Count > 0)
                _requestedNode.Dependencies.Clear();
            var promptsSource = _promptsSourceRepository.GetPromptsSource(SelectedService.Type);

            if (promptsSource == null)
            {
                ShowNotImplementedDialog();
                EnableGetDependencies = true;
                InfoBarText = "";
            }
            else
                await _dependencyHelper.SetDependenciesAsync(_requestedNode.Node, promptsSource);
        });
    }

    void EvaluateEnableGetDependencies()
    {
        EnableGetDependencies = SelectedService != null &&
            !_fetchingDependencies && _countdownQueue.Count == 0 && _countdownSeconds == 0;

        IsNotBusy = EnableGetDependencies;
        if (SelectedService != null)
            SelectedService.EnableGetDependencies = EnableGetDependencies;
    }

    #endregion

    #region Event Handlers

    private void SKClient_ResponseReceived(object? sender, SKClient.ResponseEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            var traceMessage = new TraceMessage
            {
                TimeStamp = DateTime.Now,
                Text = $"{e.Response}",
                Type = "Response",
            };
            _allTraceMessages.Add(traceMessage);
            if (ShowResponses)
            {
                TraceMessages.Add(traceMessage);
            }
        });
    }

    private void SKClient_RequestSent(object? sender, SKClient.RequestEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            _fetchingDependencies = true;
            EvaluateEnableGetDependencies();
            var traceMessage = new TraceMessage
            {
                TimeStamp = DateTime.Now,
                Text = $"{e.Prompt}",
                Type = "Request"
            };
            _allTraceMessages.Add(traceMessage);
            if (ShowRequests || NoTraceMessageFilter)
            {
                TraceMessages.Add(traceMessage);
            }
        });
    }

    private void DependencyHelper_DependenciesReceived(object? sender, NodeEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            _requestedNode.SetDependencies(e.Node.Dependencies, _nodesLookup);
            _requestedNode.SetRisks(e.Node.Risks);
            _requestedNode.SetIssues(e.Node.Issues);
            _fetchingDependencies = false;
            EvaluateEnableGetDependencies();

            _requestedNode.Refresh();
            foreach (var nodeVM in _requestedNode.Dependencies)
            {
                nodeVM.Refresh();
            }
            SetServicesEnableGetDependencies(true);

            OnDependenciesDetermined(new NodeViewModelEventArgs(SelectedService));
             OnPropertyChanged(nameof(CanFixIssues));
            OnPropertyChanged(nameof(CanFixRisk));
        });
    }

    private void SKClient_RateExceeded(object? sender, SKClient.RateExceededEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            var traceMessage = new TraceMessage
            {
                TimeStamp = DateTime.Now,
                Text = $"Limit exceeded. Retry after {e.WaitTimeInSeconds} seconds.",
                Type = "RateLimit"
            };
            _allTraceMessages.Add(traceMessage);
            if (ShowRateLimits || NoTraceMessageFilter)
            {
                TraceMessages.Add(traceMessage);
            }                     
        });
        _countdownQueue.Enqueue(e.WaitTimeInSeconds);
        EvaluateEnableGetDependencies();
        StartCountdown(_countdownQueue.Dequeue());
    }

    private void SKClient_FunctionCalled(object? sender, FunctionCallEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            var traceMessage = new TraceMessage
            {
                TimeStamp = DateTime.Now,
                Text = $"{e.FunctionName}",
                Type = "FunctionCall"
            };
            _allTraceMessages.Add(traceMessage);
            if (ShowFunctionCalls || NoTraceMessageFilter)
            {
                TraceMessages.Add(traceMessage);
            }           
        });
    }

    private void SKClient_ExceptionOccured(object? sender, ExceptionEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            var traceMessage = new TraceMessage
            {
                TimeStamp = DateTime.Now,
                Text = $"Exception: {e.Exception.Message}",
                Type = "Exception"
            };
            _allTraceMessages.Add(traceMessage);
            if (ShowExceptions || NoTraceMessageFilter)
            {
                TraceMessages.Add(traceMessage);
            }
            _fetchingDependencies = false;
            SetServicesEnableGetDependencies(true);
            EvaluateEnableGetDependencies();
        });
    }

    #endregion

    #region Countdown Methods

    public void StartCountdown(int seconds)
    {
        _countdownSeconds = seconds;
        _countdownTimer?.Dispose();
        _countdownTimer = new Timer(CountdownCallback, null, 1000, 1000);
    }

    private void CountdownCallback(object? state)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            if (_countdownSeconds > 0)
            {
                _countdownSeconds--;
                InfoBarText = $"Retrying in {_countdownSeconds} sec.";
            }
            else
            {
                if (_countdownQueue.Count > 0)
                {
                    StartCountdown(_countdownQueue.Dequeue());
                }
                else
                {
                    InfoBarText = _informationText;
                    _countdownTimer?.Dispose();
                    _countdownTimer = null;
                    EvaluateEnableGetDependencies();
                }
            }
        });
    }

    #endregion

    #region Dependendencies Determined Event

    public event EventHandler<NodeViewModelEventArgs>? DependenciesDetermined;

    protected virtual void OnDependenciesDetermined(NodeViewModelEventArgs args)
    {
        _informationText = string.Empty;
        DependenciesDetermined?.Invoke(this, args);
    }

    private void SetServicesEnableGetDependencies(bool enable)
    {
        foreach (var service in _allServices)
        {
            service.EnableGetDependencies = enable;
        }
    }

    #endregion

    #region Fix Issues

    public bool CanFixIssues
        => SelectedService != null && SelectedService.Issues != null && SelectedService.Issues.Count > 0;

    public RelayCommand FixIssuesCommand => new RelayCommand(FixIssues);

    private void FixIssues(object obj)
    {
        if (SelectedService == null)
            return;

        foreach (DependencyIssuesViewModel issue in SelectedService.Issues)
        {
            FixIssueRequest request = new FixIssueRequest
            {
                RootServiceId = SelectedService.Id,
                ServiceId = issue.DependencyIssues.ServiceId,
                Issues = issue.DependencyIssues.Issues.ToArray()
            };
            _dispatcherQueue.TryEnqueue(() =>
            {
                Task.Run(async () => await _aIFixer.FixDependencyIssuesAsync(request));
            });
        }
    }


    private void Fixer_IssueFixed(object? sender, FixIssueEventArgs e)
    {
        // TODO: Temporary. Need to update only the affected node.
        _dispatcherQueue.TryEnqueue(() =>
        {
            var rootNode = _nodesLookup.ContainsKey(e.Request.RootServiceId) ? _nodesLookup[e.Request.RootServiceId] : null;
            if (rootNode != null)
            {
                var dep = rootNode.Issues.SingleOrDefault(o => o.DependencyIssues.ServiceId == e.Request.ServiceId);
                foreach (var issue in e.Request.Issues)
                {
                    if (!issue.Fixed)
                        continue;
                    // TODO: Fix this. Issue with equality comparison.
                    dep?.DependencyIssues.Issues?.Remove(issue);
                }
                rootNode.Refresh();
            }
            _fetchingDependencies = false;
            EvaluateEnableGetDependencies();
            SetServicesEnableGetDependencies(true);
            OnPropertyChanged(nameof(SelectedService));
        });
    }

   
    public void FixIssue(Issue selectedIssue)
    {
        if (SelectedService == null || selectedIssue == null)
            return;
        FixIssueRequest request = new FixIssueRequest
        {
            RequestType = "IssueFix",
            RootServiceId = SelectedService.Id,
            ServiceId = selectedIssue.ServiceId,
            Issues = new List<Issue> { selectedIssue}
        };
        _dispatcherQueue.TryEnqueue(() =>
        {
            Task.Run(async () => await _aIFixer.FixDependencyIssuesAsync(request));
        });
    }

    #endregion

    #region Fix Risks 

    public bool CanFixRisk
        => SelectedService != null && SelectedService.Risks != null && SelectedService.Risks.Count > 0;
   

    public RelayCommand FixRisksCommand => new RelayCommand(FixRisks);

    private void FixRisks(object obj)
    {
        if (SelectedService == null)
            return;
        FixRiskRequest request = new()
        {
            RequestType = "RiskFix",
            ServiceType = SelectedService.Type,
            ServiceId = SelectedService.Id,
            Risks = SelectedService.Risks?.ToList() ?? new List<Risk>()
        };

        _dispatcherQueue.TryEnqueue(() =>
        {
            Task.Run(async () => await _aIFixer.FixRisksAsync(request));
        });
    }

    private void Fixer_RiskFixed(object? sender, FixRiskEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            var service = _nodesLookup.ContainsKey(e.Request.ServiceId) ? _nodesLookup[e.Request.ServiceId] : null;
            if (service != null)
            {
                foreach (var risk in e.Request.Risks)
                {
                    if (!risk.Fixed)
                        continue;
                    service.Risks?.Remove(risk);  
                }
                service.Refresh();
            }
           
            _fetchingDependencies = false;
            EvaluateEnableGetDependencies();
            SetServicesEnableGetDependencies(true);
            OnPropertyChanged(nameof(SelectedService));
        });
    }

    Risk _selectedRisk;
    public Risk SelectedRisk
    {
        get => _selectedRisk;
        set
        {
            if (_selectedRisk == value)
                return;
            _selectedRisk = value;
            OnPropertyChanged();
        }
    }

    public void FixRisk()
    {
        if (SelectedService == null || SelectedRisk == null)
            return;
        FixRiskRequest request = new FixRiskRequest
        {
            RequestType = "RiskFix",
            ServiceType = SelectedService.Type,
            ServiceId = SelectedService.Id,
            Risks = new List<Risk> { SelectedRisk }
        };
        _dispatcherQueue.TryEnqueue(() =>
        {
            Task.Run(async () => await _aIFixer.FixRisksAsync(request));
        });
    }


    #endregion

    #region Resource Type filter 

    ObservableCollection<string> _serviceTypes = new();
    public ObservableCollection<string> ServiceTypes
    {
        get => _serviceTypes;
        set
        {
            if (_serviceTypes == value)
                return;
            _serviceTypes = value;
            OnPropertyChanged();
        }
    }

    string _selectedServiceType = string.Empty;
    public string SelectedServiceType
    {
        get => _selectedServiceType;
        set
        {
            if (_selectedServiceType == value)
                return;
            _selectedServiceType = value;
            OnPropertyChanged();
            if (AppsOnly)
                return;
            ApplyServiceTypeFilter();
        }
    }

    private void ApplyServiceTypeFilter()
    {
        if (_selectedServiceType != "All")
        {
            Services.Clear();
            foreach (var service in _allServices.Where(s => s.TypeFriendlyName == SelectedServiceType)
                .OrderBy(o => o.Name))
            {
                Services.Add(service);
            }
        }
        else
        {
            Services.Clear();
            foreach (var service in _allServices.OrderBy(o => o.Name))
            {
                Services.Add(service);
            }
        }
    }

    private void AddServiceTypes()
    {
        ServiceTypes.Clear();
        ServiceTypes.Add("All");
        var types = _allServices.Select(s => s.TypeFriendlyName).Distinct().OrderBy(t => t);
        foreach (var type in types)
        {
            ServiceTypes.Add(type);
        }
        SelectedServiceType = "All";
    }
    // In WinUI 3, you can use ContentDialog for popups similar to MessageBox in WPF/WinForms.
    // This is a helper method you can add to your ViewModel or call from your View's code-behind.

    public event EventHandler OnShowNotImplementedDialog;


    #endregion

    #region Not imnplemented dialog

    public void ShowNotImplementedDialog()
    {
        OnShowNotImplementedDialog?.Invoke(this, EventArgs.Empty);

    }

    #endregion

    #region By the numbers




    #endregion



}
