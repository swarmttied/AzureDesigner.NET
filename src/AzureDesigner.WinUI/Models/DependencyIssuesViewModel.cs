using AzureDesigner.Models;

namespace AzureDesigner.WinUI.Models
{
    public class DependencyIssuesViewModel : ViewModelBase
    {
        private NodeViewModel _node;
        public NodeViewModel Node
        {
            get => _node;
            internal set
            {
                if (_node != value)
                {
                    _node = value;
                    OnPropertyChanged();
                }
            }
        }

        Issue _selectedIssue;
        public Issue SelectedIssue
        {
            get => _selectedIssue;
            set
            {
                if (_selectedIssue == value)
                    return;
                _selectedIssue = value;
                OnPropertyChanged();
            }
        }

        private DependencyIssues _dependencyIssues;
        public DependencyIssues DependencyIssues
        {
            get => _dependencyIssues;
            internal set
            {
                if (_dependencyIssues != value)
                {
                    _dependencyIssues = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
