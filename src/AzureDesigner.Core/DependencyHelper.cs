using System.Diagnostics;
using Newtonsoft.Json;
using AzureDesigner.AIContexts;
using AzureDesigner.Models;
using SKLIb;
using System.ComponentModel.Design.Serialization;

namespace AzureDesigner
{
    public interface IDependencyHelper
    {
        event EventHandler<NodeEventArgs>? DependenciesReceived;

        Task SetDependenciesAsync(Node root, IPromptsSource promptsSource);

        ISKClient SKClient { get; }

        IEnumerable<Node>? ReferenceNodes { get; set; }
    }

    public class DependencyHelper : IDependencyHelper
    {
        readonly ISKClient _skClient;
        Dictionary<int, Node> _nodesLookup;
        string _serviceIds = null!;
        readonly IIdMapping _idMapping;

        public event EventHandler<NodeEventArgs>? DependenciesReceived;
        public event EventHandler<NodeEventArgs>? IssueFixed;

        public DependencyHelper(ISKClient skClient, IIdMapping idMapping)
        {
            _skClient = skClient ?? throw new ArgumentNullException(nameof(skClient));
            _skClient.ResponseReceived += SKClient_ResponseReceived;
            _idMapping = idMapping;
        }

        protected virtual void OnDependenciesReceived(NodeEventArgs e)
        {
            DependenciesReceived?.Invoke(this, e);
        }

        protected virtual void OnIssueFixed(NodeEventArgs e)
        {
            IssueFixed?.Invoke(this, e);
        }

        private IEnumerable<Node>? _referenceNodes;
        public IEnumerable<Node>? ReferenceNodes
        {
            get => _referenceNodes;
            set
            {
                _referenceNodes = value;
                _nodesLookup = ReferenceNodes.ToDictionary(n => n.Id, n => n);
                _serviceIds = string.Join(",", ReferenceNodes.Select(n => n.Id));
            }
        }

        public ISKClient SKClient => _skClient;

        private void SKClient_ResponseReceived(object? sender, SKClient.ResponseEventArgs e)
        {
            Dependencies? dependencies = null;
            dependencies = JsonConvert.DeserializeObject<Dependencies>(e.JsonResponse ?? string.Empty);      
            var root = _nodesLookup[dependencies.Id];
            root.Risks = dependencies.Risks.Select(o => new Risk { Description = o }).ToList();
            root.Issues = new Dictionary<int, IEnumerable<Issue>>();
           
            foreach (var issue in dependencies.Issues)
            {
                if (issue.Value != null && issue.Value.Any())
                {
                    root.Issues[issue.Key] = issue.Value.Select(o => new Issue { ServiceId=issue.Key, Description = o }).ToList();
                }
            }

            if (root.Dependencies == null)
                root.Dependencies = new List<Node>();
            else
                root.Dependencies.Clear();
            foreach (var dependencyId in dependencies.DependencyIds)
            {
                if (_nodesLookup.TryGetValue(dependencyId, out Node? dependencyNode))
                {
                    dependencyNode.IsTraced = true;
                    root.Dependencies.Add(dependencyNode);
                }
            }
            root.IsTraced = true;
            OnDependenciesReceived(new NodeEventArgs(root));
        }

        public async Task SetDependenciesAsync(Node root, IPromptsSource promptsSource)
        {
            if (ReferenceNodes == null)
                throw new InvalidOperationException($"{nameof(ReferenceNodes)} is null. Set {nameof(ReferenceNodes)} before calling {nameof(SetDependenciesAsync)}.");
            _serviceIds = string.Join(",", _idMapping.GetCompactIds());
            var prompt = promptsSource.GetDependencyPrompt(root);
            await _skClient.RunAsync(prompt);
        }
    }
}
