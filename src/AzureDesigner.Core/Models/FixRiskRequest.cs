using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureDesigner.Models
{
    public class FixRiskRequest
    {
        public string? RequestType { get; set; }
        public int ServiceId { get; set; }
        public string? ServiceType { get; set; }       
        public List<Risk>? Risks { get; set; }
    }
}
