using Microsoft.Formula.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Pc
{
    public interface ICompilerOutput
    {
        void WriteMessage(string msg, SeverityKind severity);
    }
}
