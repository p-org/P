using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P.Runtime
{
    public abstract class PrtSpecMachine : PrtMachine
    {
        #region Fields
        public List<PrtValue> observes;
        public bool IsHot;
        #endregion

        public abstract PrtSpecMachine MakeSkeleton();

        public PrtSpecMachine() : base()
        {
            observes = new List<PrtValue>();
            IsHot = false;
        }
        public object Clone()
        {
            var clonedMonitor = MakeSkeleton();
            foreach (var fd in fields)
            {
                clonedMonitor.fields.Add(fd.Clone());
            }
            clonedMonitor.stateStack = this.stateStack.Clone();
            clonedMonitor.nextSMOperation = this.nextSMOperation;
            clonedMonitor.stateExitReason = this.stateExitReason;
            clonedMonitor.stateImpl = this.stateImpl;
            return clonedMonitor;
        }

        public override void PrtEnqueueEvent(StateImpl application, PrtValue e, PrtValue arg, PrtMachine source)
        {
            throw new NotImplementedException();
        }
    }
}
