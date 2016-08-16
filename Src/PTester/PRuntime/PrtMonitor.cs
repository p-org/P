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
        public int instanceNumber;
        public List<PrtValue> monitorFields;
        public PrtEvent currentTrigger;
        public PrtValue currentPayload;
        private PrtFunStack invertedFunStack;
        #endregion

        public abstract string Name
        {
            get;
        }

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
