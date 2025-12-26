using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKLIb;
public class FunctionCallEventArgs : EventArgs
{
    public FunctionCallEventArgs(string functionName)
    {
        FunctionName = functionName;
    }

    public string FunctionName { get; private set; }
}

