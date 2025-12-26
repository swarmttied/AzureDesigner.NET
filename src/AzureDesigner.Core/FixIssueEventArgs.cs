using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AzureDesigner.Models;

namespace AzureDesigner
{
    public class FixIssueEventArgs : EventArgs
    {
        public FixIssueEventArgs(FixIssueRequest request)
        {
            Request = request;
        }

        public FixIssueRequest Request { get; }
    }
}
