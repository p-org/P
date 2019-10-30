using Microsoft.Coyote;
using System;
using System.Runtime;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Plang.PrtSharp;
using Plang.PrtSharp.Values;
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

    public static partial class GlobalFunctions
    {
        public static void GlobalForeignFun(PMachine currentMachine)
        {
        }
    }

    public partial class T : IPrtValue
    {
        public IPrtValue Clone()
        {
            return this;
        }

        public bool Equals(IPrtValue other)
        {
            return this == other;
        }
    }
}