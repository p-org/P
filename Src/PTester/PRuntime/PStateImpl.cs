using System;
using System.Collections.Generic;
using System.Linq;


namespace P.Runtime
{
    public abstract class StateImpl : ICloneable
    {
        #region Constructors
        /// <summary>
        /// This function is called when the stateimp is loaded first time.
        /// </summary>
        protected StateImpl()
        {
            implMachines = new List<PrtImplMachine>();
            specMachines = new List<PrtSpecMachine>();
            specObservers = new Dictionary<PrtEventValue, List<PrtSpecMachine>>();
        }
        #endregion

        #region Fields
        /// <summary>
        /// Map from the statemachine id to the instance of the statemachine.
        /// </summary>
        private List<PrtImplMachine> implMachines;

        public List<PrtImplMachine> ImplMachines
        {
            get
            {
                return implMachines;
            }
        }

        /// <summary>
        /// List of monitors
        /// </summary>
        private List<PrtSpecMachine> specMachines;

        /// <summary>
        /// Stores the exception encoutered during exploration.
        /// </summary>
        private Exception exception;

        private Dictionary<PrtEventValue, List<PrtSpecMachine>> specObservers;
        #endregion

        #region Getters and Setters
        public bool Deadlock
        {
            get
            {
                bool enabled = false;
                foreach (var x in implMachines)
                {
                    if (enabled) break;
                    enabled = enabled || (x.currentStatus == PrtMachineStatus.Enabled);
                }
                bool hot = false;
                foreach (var x in specMachines)
                {
                    if (hot) break;
                    hot = hot || x.IsHot;
                }
                return (!enabled && hot);
            }
        }

        public Exception Exception
        {
            get { return exception; }
            set { exception = value; }
        }

        #endregion

        #region Clone Function
        public abstract StateImpl MakeSkeleton();

        public object Clone()
        {
            var clonedState = MakeSkeleton();
            //clone all the fields
            clonedState.implMachines = new List<PrtImplMachine>();
            foreach (var machine in implMachines)
            {
                clonedState.implMachines.Add(machine.Clone());
            }

            clonedState.specMachines = new List<PrtSpecMachine>();
            foreach (var monitor in specMachines)
            {
                clonedState.specMachines.Add((PrtSpecMachine)monitor.Clone());
            }

            return clonedState;

        }
        #endregion


        public int NextMachineInstanceNumber(Type machineType)
        {
            return implMachines.Where(m => m.GetType() == machineType).Count() + 1;
        }

        public void Announce(PrtEventValue ev, PrtValue payload, PrtMachine parent)
        {
            if (specObservers.ContainsKey(ev))
            {
                foreach (var spec in specObservers[ev])
                {
                    spec.PrtEnqueueEvent(ev, payload, parent);
                }
            }
        }

        public void AddImplMachineToStateImpl(PrtImplMachine machine)
        {
            implMachines.Add(machine);
        }

        public void AddSpecMachineToStateImpl(PrtSpecMachine spec)
        {
            specMachines.Add(spec);
            foreach(var ev in spec.observes)
            {
                if(specObservers.ContainsKey(ev))
                {
                    specObservers[ev].Add(spec);
                }
                else
                {
                    specObservers[ev] = new List<PrtSpecMachine>();
                    specObservers[ev].Add(spec);
                }
            }
        }

        public void Trace(string message, params object[] arguments)
        {
            Console.WriteLine(String.Format(message, arguments));
        }



        public void SetPendingChoicesAsBoolean(PrtImplMachine process)
        {
            throw new NotImplementedException();
        }

        public object GetSelectedChoiceValue(PrtImplMachine process)
        {
            throw new NotImplementedException();
        }
    }

    
}
