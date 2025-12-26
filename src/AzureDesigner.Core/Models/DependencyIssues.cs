using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AzureDesigner.Models;

namespace AzureDesigner.Models
{
    public class DependencyIssues
    {
        public required int ServiceId { get; set; }
        public required IList<Issue> Issues { get; set; }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            if (obj is null || obj.GetType() != GetType())
                return false;
            var other = (DependencyIssues)obj;
            return ServiceId == other.ServiceId;
        }

        public override int GetHashCode()
        {
            return ServiceId.GetHashCode();
        }
    }
}
