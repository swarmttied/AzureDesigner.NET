using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureDesigner.WinUI.Models
{
    public class TraceMessage
    {
        public DateTime TimeStamp { get; set; } = DateTime.Now;
        public string Type { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }
}
