using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P.Runtime
{
    public abstract class PrtMonitor : ICloneable
    {
        #region Fields
        public List<PrtValue> monitorFields;
        public PrtStateStack stateStack;
        public List<PrtEvent> observes;
        public bool IsHot;
        #endregion

        public PrtMonitor()
        {
            monitorFields = new List<PrtValue>();
            stateStack = new PrtStateStack();
            observes = new List<PrtEvent>();
            IsHot = false;
        }
        public abstract string Name
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
