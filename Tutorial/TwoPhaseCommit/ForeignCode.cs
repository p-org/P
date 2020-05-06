using System;
using System.Runtime;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Plang.CoyoteRuntime;
using Plang.CoyoteRuntime.Values;
using Plang.CoyoteRuntime.Exceptions;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable 162, 219, 414
namespace TwoPhaseCommit
{
    public static partial class GlobalFunctions {
		public static PrtNamedTuple ChooseTransaction(PMachine pMachine)
        {
            return (new PrtNamedTuple(new string[] { "client", "key", "val" }, pMachine.self, ((PrtInt)10), ((PrtInt)1)));
        }
	}
}