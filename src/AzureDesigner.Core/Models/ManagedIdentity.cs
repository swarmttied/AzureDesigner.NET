namespace AzureDesigner.Models
{
    public class ManagedIdentity
    {
        public string Id { get; set; } = string.Empty;
        public Guid ClientId { get; set; } = Guid.Empty;
    }
}
