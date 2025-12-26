using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using AzureDesigner.Models;
using AzureDesigner.WinUI.ViewModels;
using System.Linq;

namespace AzureDesigner.WinUI.Models;

public class NodeViewModel : ViewModelBase
{
    readonly MainViewModel _mainWindowViewModel; // this is a workaround the lack of RelativeSource in x:Bind
    readonly Node _node;
    readonly IResourceTypeNameMapper _typeNameMapper;

    public NodeViewModel(Node node, MainViewModel mainWindowViewModel,
        IResourceTypeNameMapper typeNameMapper)
    {
        _node = node;
        _mainWindowViewModel = mainWindowViewModel;
        _typeNameMapper = typeNameMapper ?? throw new ArgumentNullException(nameof(typeNameMapper));
    }

    public Node Node
    {
        get => _node;       
    }

   
    public int Id
    {
        get => _node.Id;       
    }

    public string Name
    {
        get => _node.Name;
        set
        {
            if (_node.Name != value)
            {
                _node.Name = value;
                OnPropertyChanged();
            }
        }
    }

    public string Type
    {
        get => _node.Type;
        set
        {
            if (_node.Type != value)
            {
                _node.Type = value;
                OnPropertyChanged();
            }
        }
    }

    public string TypeFriendlyName
    {
        get
        {
            var friendlyName = _typeNameMapper[_node.Type];
            return friendlyName;
        }
    }

    public string Location
    {
        get => _node.Location;
        set
        {
            if (_node.Location != value)
            {
                _node.Location = value;
                OnPropertyChanged();
            }
        }
    }

    public string ResourceGroupName
    {
        get => _node.ResourceGroupName;
        set
        {
            if (_node.ResourceGroupName != value)
            {
                _node.ResourceGroupName = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsTraced
    {
        get => _node.IsTraced;
        set
        {
           if (_node.IsTraced != value)
            {
                _node.IsTraced = value;
                OnPropertyChanged();
            }
        }
    }

    public string PortalUrl
    {
        get => _node.PortalUrl;
        set
        {
            if (_node.PortalUrl != value)
            {
                _node.PortalUrl = value;
                OnPropertyChanged();
            }
        }
    }



    ObservableCollection<Risk> _risks = [];
    public ObservableCollection<Risk>? Risks
    {
        get => _risks;
    }

    public void SetRisks(IEnumerable<Risk> risks)
    {
        _risks.Clear();
        if (risks == null)
            return;
        foreach (var risk in risks)
        {
            if (risk == null)
                throw new ArgumentNullException(nameof(risk));
            _risks.Add(risk);
        }
    }

    ObservableCollection<DependencyIssuesViewModel> _issues = new();
    public ObservableCollection<DependencyIssuesViewModel>? Issues
    {
        get => _issues;
    }

    public string[] IssueDescriptionsFlattend
    {
        get
        {
            IList<string> descriptions = new List<string>();
            foreach (var issue in _issues)
            {
                if (issue.DependencyIssues.Issues == null)
                    continue;
                foreach (var desc in issue.DependencyIssues.Issues)
                {
                    // TODO: Inlude or use name instead of service id?
                    descriptions.Add($"{desc.Description}");
                }
            }

            return descriptions.ToArray();
        }
    }

    public void SetIssues(IDictionary<int, IEnumerable<Issue>> issues)    
    {
        _issues.Clear();
        if (issues == null) 
            return;
        foreach (var issue in issues)
        {
            var dependencyNode = _mainWindowViewModel._nodesLookup.GetValueOrDefault(issue.Key);
            var issueViewModel = new DependencyIssuesViewModel
            {
                Node = dependencyNode,
                DependencyIssues = new DependencyIssues { ServiceId = issue.Key, Issues = issue.Value.ToList() }
            };
            _issues.Add(issueViewModel);
        }
        OnPropertyChanged(nameof(IssueDescriptionsFlattend));
    }

    public void Refresh()
    {
        //OnPropertyChanged(nameof(Id));
        //OnPropertyChanged(nameof(Name));
        //OnPropertyChanged(nameof(Type));
        //OnPropertyChanged(nameof(Location));
        //OnPropertyChanged(nameof(ResourceGroupName));
        OnPropertyChanged(nameof(IconPath));
        OnPropertyChanged(nameof(IsTraced));
        OnPropertyChanged(nameof(Risks));
        OnPropertyChanged(nameof(Issues));
        OnPropertyChanged(nameof(IssueDescriptionsFlattend));
    }


    public void SetDependencies(IEnumerable<Node> dependencies, IDictionary<int, NodeViewModel> nodesLookup)
    {
        _dependencies.Clear();

        if (dependencies == null)
            return;

        foreach (var dependency in dependencies)
        {
            if (dependency == null)
                throw new ArgumentNullException(nameof(dependency));

            if (nodesLookup.TryGetValue(dependency.Id, out var viewModel))
            {
                _dependencies.Add(viewModel);
            }
            else
            {
                _dependencies.Add(new NodeViewModel(dependency, _mainWindowViewModel,
                    _typeNameMapper));
            }
        }
    }


    ObservableCollection<NodeViewModel> _dependencies = [];
  
    public ObservableCollection<NodeViewModel>? Dependencies
    {
        get => _dependencies;
    }


    public IEnumerable<Setting>? Settings
    {
        get => _node.Settings;
        set
        {
            if (_node.Settings != value)
            {
                _node.Settings = value;
                OnPropertyChanged();
            }
        }
    }

    private string? _iconPath;
  

    public string? IconPath
    {
        get => _iconPath;
        set
        {
            if (_iconPath != value)
            {
                _iconPath = value;
                OnPropertyChanged();
            }
        }
    }

    #region RelativeSource workaround

    public ICommand GetDependenciesCommand => new RelayCommand(GetDendencies);

    private void GetDendencies(object obj)
    {
        _mainWindowViewModel.GetDendencies(this);
    }

    private bool _enableGetDependencies;
    public bool EnableGetDependencies
    {
        set { 
          if (_enableGetDependencies != value)
            {
                _enableGetDependencies = value;
                OnPropertyChanged();               
            }
        }
        get => _enableGetDependencies;

    }

    public void RefreshEnableGetDependencies()
    {
        OnPropertyChanged(nameof(EnableGetDependencies));
    }

    #endregion
}
