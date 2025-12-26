using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureDesigner.Models
{
    public class Dependencies
    {
        public string? RequestType { get; set; }

        // The ID of the resource for which dependencies are being described
        public required int Id { get; set; }
        public required int[] DependencyIds { get; set; }
        public Dictionary<int, IEnumerable<string>> Issues { get; set; } = new Dictionary<int, IEnumerable<string>>();
        public IList<string> Risks { get; set; } = new List<string>();
    }
}
