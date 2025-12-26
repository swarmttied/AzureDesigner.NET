using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureDesigner.Models;

public record Setting
{
    public string Key { get; set; }
    public string Value { get; set; }
}
