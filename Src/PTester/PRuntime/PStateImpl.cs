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
            exception = null;
            linkMap = new Dictionary<string, Dictionary<string, string>>();
            renameMap = new Dictionary<string, string>();
            isSafeMap = new Dictionary<string, bool>();
            monitorMap = new Dictionary<string, List<string>>();
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

        public Dictionary<string, Dictionary<string, string>> linkMap;
        public Dictionary<string, string> renameMap;
        public Dictionary<string, bool> isSafeMap;
        public Dictionary<string, List<string>> monitorMap;
        public delegate PrtImplMachine CreateMachineDelegate(StateImpl application, PrtValue payload);
        public Dictionary<string, CreateMachineDelegate> createMap;
        public Dictionary<string, List<PrtEventValue>> interfaceMap;
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

            clonedState.exception = this.exception;

            foreach(var item in specObservers)
            {
                clonedState.specObservers.Add(item.Key.Clone() as PrtEventValue, item.Value.ToList());
            }

            //the following values are not going to change and hence no need to clone them.
            clonedState.linkMap = linkMap;
            clonedState.renameMap = renameMap;
            clonedState.isSafeMap = isSafeMap;
            clonedState.monitorMap = monitorMap;
            clonedState.createMap = createMap;

            return clonedState;

        }
        #endregion


        public PrtInterfaceValue CreateInterfaceOrMachine(string currMachRenameName, string interfaceOrMachineName, PrtValue payload = null)
        {
            var renamedImpMachine = linkMap[currMachRenameName][interfaceOrMachineName];
            var impMachineName = renameMap[renamedImpMachine];
            var machine = createMap[impMachineName](this, payload);
            machine.isSafe = isSafeMap[renamedImpMachine];
            machine.renamedName = renamedImpMachine;
            if(interfaceMap.ContainsKey(interfaceOrMachineName))
            {
                return new PrtInterfaceValue(machine, interfaceMap[interfaceOrMachineName]);
            }
            else
            {
                return new PrtInterfaceValue(machine, machine.self.permissions);
            }
        }

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
