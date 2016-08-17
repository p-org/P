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
            statemachines = new List<PrtMachine>();
            monitors = new List<PrtMonitor>();
        }
        #endregion

        #region Fields
        /// <summary>
        /// Map from the statemachine id to the instance of the statemachine.
        /// </summary>
        private List<PrtMachine> statemachines;

        public List<PrtMachine> AllStateMachines
        {
            get
            {
                return statemachines;
            }
        }

        /// <summary>
        /// List of monitors
        /// </summary>
        private List<PrtMonitor> monitors;
        
        /// <summary>
        /// Stores the exception encoutered during exploration.
        /// </summary>
        private Exception exception;

        #endregion

        #region Getters and Setters
        public bool Deadlock
        {
            get
            {
                bool enabled = false;
                foreach (var x in statemachines)
                {
                    if (enabled) break;
                    enabled = enabled || (x.currentStatus == PrtMachineStatus.Enabled);
                }
                bool hot = false;
                foreach (var x in monitors)
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
            clonedState.statemachines = new List<PrtMachine>();
            foreach(var machine in statemachines)
            {
                clonedState.statemachines.Add(machine.Clone());
            }

            clonedState.monitors = new List<PrtMonitor>();
            foreach(var monitor in monitors)
            {
                clonedState.monitors.Add((PrtMonitor)monitor.Clone());
            }

            return clonedState;

        }
        #endregion


        public int NextMachineInstanceNumber(Type machineType)
        {
            return statemachines.Where(m => m.GetType() == machineType).Count() + 1;
        }

        public void AddStateMachineToStateImpl(PrtMachine machine)
        {
            statemachines.Add(machine);
        }

        public void Trace(string message, params object[] arguments)
        {
            Console.WriteLine(String.Format(message, arguments));
        }



        public void SetPendingChoicesAsBoolean(PrtMachine process)
        {
            throw new NotImplementedException();
        }

        public object GetSelectedChoiceValue(PrtMachine process)
        {
            throw new NotImplementedException();
        }
    }

    
}
