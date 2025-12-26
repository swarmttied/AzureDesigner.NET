namespace AzureDesigner.Models;

public class Node
{
    public int Id { get; set; }

    public required string ResourceId { get; set; }

    public required string Name { get; set; }

    public required string Type { get; set; }

    public required string Location { get; set; }

    public required string ResourceGroupName { get; set; }

    public bool IsTraced { get; set; }

    public IList<Node>? Dependencies { get; set; }

    public IList<Risk>? Risks { get; set; }

    public IDictionary<int, IEnumerable<Issue>>? Issues { get; set; }

    public IEnumerable<Setting>? Settings { get; set; }

    public string PortalUrl { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"{Name} ({Type})";
    }
}