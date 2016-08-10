using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

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
            nextStateMachineId = 0;
        }
        #endregion

        #region Fields
        /// <summary>
        /// Map from the statemachine id to the instance of the statemachine.
        /// </summary>
        private Dictionary<int, PrtMachine> statemachines;

        /// <summary>
        /// Stores the exception encoutered during exploration.
        /// </summary>
        private Exception exception;

        /// <summary>
        /// Indicates that a PrtMethod call is invoked
        /// </summary>
        private bool isCall;

        /// <summary>
        /// Indicates that a PrtMethod returned
        /// </summary>
        private bool isReturn;
        #endregion

        #region Getters and Setters
        public abstract IEnumerable<PrtMachine> AllAliveMachines
        {
            get;
        }

        public abstract IEnumerable<PrtMonitor> AllInstalledMonitors
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

        public Exception Exception
        {
            get { return exception; }
            set { exception = value; }
        }

        //IExplorable
        public bool IsCall
        {
            get { return isCall; }
            set { isCall = value; }
        }

        //IExplorable
        public bool IsReturn
        {
            get { return isReturn; }
            set { isReturn = value; }
        }
        #endregion

        #region Clone Function
        public abstract PStateImpl MakeSkeleton();

        public object Clone()
        {
            clonedState = MakeSkeleton();


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
