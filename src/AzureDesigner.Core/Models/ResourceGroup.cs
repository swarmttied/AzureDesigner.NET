using System;
using System.Collections.Generic;
using System.Text;

namespace AzureDesigner.Models;

public class ResourceGroup
{
    public required string Name { get; set; }
    public required string Id { get; set; }

    public required string Location { get; set; }
}
