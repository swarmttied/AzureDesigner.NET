using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureDesigner.Models;

public record Subscription
{
    public required string Id { get; set; }
    public required string Name { get; set; }
   
}
