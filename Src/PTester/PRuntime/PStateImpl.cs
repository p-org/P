using System;
using System.Collections.Generic;
using System.Linq;


namespace P.PRuntime
{
    public abstract class PStateImpl : ICloneable
    {
        #region Constructors
        /// <summary>
        /// This function is called when the stateimp is loaded first time.
        /// </summary>
        protected PStateImpl()
        {
            statemachines = new Dictionary<int, PrtMachine>();
            monitors = new List<PrtMonitor>();
        }
        #endregion

        #region Fields
        /// <summary>
        /// Map from the statemachine id to the instance of the statemachine.
        /// </summary>
        private Dictionary<int, PrtMachine> statemachines;

        public List<PrtMachine> AllStateMachines
        {
            get
            {
                return statemachines.Values.ToList();
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
                foreach (var x in statemachines.Values)
                {
                    if (enabled) break;
                    enabled = enabled || x.IsEnabled;
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
        public abstract PStateImpl MakeSkeleton();

        public object Clone()
        {
            var clonedState = MakeSkeleton();
            //clone all the fields
            clonedState.statemachines = new Dictionary<int, PrtMachine>();
            foreach(var machine in statemachines)
            {
                clonedState.statemachines.Add(machine.Key, machine.Value.Clone());
            }

            clonedState.monitors = new List<PrtMonitor>();
            foreach(var monitor in monitors)
            {
                clonedState.monitors.Add(monitor.Clone());
            }

            return clonedState;

        }
        #endregion

        public void AddStateMachineToStateImpl(PrtMachine machine)
        {
            statemachines.Add(statemachines.Count, machine);
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
