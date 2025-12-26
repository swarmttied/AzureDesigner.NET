namespace AzureDesigner.Models
{
    public class FixIssueRequest
    {
        public string? RequestType { get; set; }
        public int RootServiceId { get; set; }
        public string? ServiceType { get; set; }
        public int ServiceId { get; set; }
        public IEnumerable<Issue>? Issues { get; set; }

    }
}
