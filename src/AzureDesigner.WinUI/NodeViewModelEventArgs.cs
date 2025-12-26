using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AzureDesigner.WinUI.Models;

namespace AzureDesigner.WinUI
{
    public class NodeViewModelEventArgs : EventArgs
    {
        public NodeViewModelEventArgs(NodeViewModel node)
        {
            Node = node;
        }

        public NodeViewModel Node { get; }
    }
}
