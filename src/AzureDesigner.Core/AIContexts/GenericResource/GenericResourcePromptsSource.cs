using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AzureDesigner.Models;

namespace AzureDesigner.AIContexts.GenericResource;

public class GenericResourcePromptsSource : IPromptsSource
{
    public string GetDependencyPrompt(Node root)
    {
        throw new NotImplementedException();
    }
}
