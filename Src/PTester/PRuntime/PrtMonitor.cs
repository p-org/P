using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P.PRuntime
{
    public abstract class PrtMonitor : ICloneable
    {
        public abstract bool IsHot
        {
            get;
        }

        public abstract PrtState StartState
        {
            get;
        }

        public abstract void Invoke();

        public object Clone()
        {
            return null;
        }
    }
}
