using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureDesigner.Models
{
    public class Issue
    {
        public required int ServiceId { get; set; }
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string? Description { get; set; }

        public bool Fixed { get; set; } = false;

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            if (obj is null || obj.GetType() != GetType())
                return false;
            var other = (Issue)obj;
            return Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id?.GetHashCode() ?? 0;
        }
    }
}
