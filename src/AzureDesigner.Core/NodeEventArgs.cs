using AzureDesigner.Models;

namespace AzureDesigner;

public class NodeEventArgs : EventArgs
{
    public NodeEventArgs(Node node)
    {
        Node = node;
    }
    public Node Node { get; }
    public string? ErrorMessage { get; set; }
    public bool IsError => !string.IsNullOrEmpty(ErrorMessage);
}
