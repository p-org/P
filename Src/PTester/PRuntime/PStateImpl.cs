using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace P.PRuntime
{
    public abstract class PStateImpl
    {
        #region Constructors
        /// <summary>
        /// This function is called when the stateimp is loaded first time.
        /// </summary>
        protected PStateImpl()
        {
            statemachines = new Dictionary<int, BaseMachine>();
            nextStateMachineId = 0;
        }
        #endregion

        public void AddStateMachine(BaseMachine machine)
        {
            statemachines.Add(nextStateMachineId, machine);
            nextStateMachineId++;
        }

        /// <summary>
        /// Map from the statemachine id to the instance of the statemachine.
        /// </summary>
        private Dictionary<int, BaseMachine> statemachines;

        /// <summary>
        /// Represents the next statemachine id.  
        /// </summary>
        private int nextStateMachineId;

        public abstract IEnumerable<BaseMachine> AllAliveMachines
        {
            get;
        }

        public abstract IEnumerable<BaseMonitor> AllInstalledMonitors
        {
            get;
        }

        public bool Deadlock
        {
            get
            {
                bool enabled = false;
                foreach (var x in AllAliveMachines)
                {
                    if (enabled) break;
                    enabled = enabled || x.IsEnabled;
                }
                bool hot = false;
                foreach (var x in AllInstalledMonitors)
                {
                    if (hot) break;
                    hot = hot || x.IsHot;
                }
                return (!enabled && hot);
            }
        }

        public void Trace(string message, params object[] arguments)
        {
            Console.WriteLine(String.Format(message, arguments));
        }

        public Exception Exception
        {
            get { return exception; }
            set { exception = value; }
        }

        private Exception exception;

        private bool isCall;

        //IExplorable
        public bool IsCall
        {
            get { return isCall; }
            set { isCall = value; }
        }

        private bool isReturn;

        //IExplorable
        public bool IsReturn
        {
            get { return isReturn; }
            set { isReturn = value; }
        }

        public void SetPendingChoicesAsBoolean(BaseMachine process)
        {
            throw new NotImplementedException();
        }

        public object GetSelectedChoiceValue(BaseMachine process)
        {
            throw new NotImplementedException();
        }
    }

    
}
