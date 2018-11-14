using Microsoft.PSharp;
using System;
using System.Runtime;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using PrtSharp;
using PrtSharp.Values;
using System.Threading;
using System.Threading.Tasks;

namespace Main {
    internal partial class Main : PMachine
    {
        
        public void ForeignFun()
        {
            Main currentMachine = this;
        }
        
    }

    public static partial class GlobalFunctions_Main
    {
        public static void GlobalForeignFun(PMachine currentMachine)
        {
        }
    }
}