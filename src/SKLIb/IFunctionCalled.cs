using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKLIb;

public interface IFunctionCalled
{
    event EventHandler<FunctionCallEventArgs> FunctionCalled;
}
