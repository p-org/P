using System;
using System.Collections;
using System.IO;
using System.Threading;

namespace Microsoft.Zing
{
    /// <summary>
    /// The ZingExplorer Class:
    /// Performs the state space exploration using different search algorithms based on the ZingerConfiguration.
    /// </summary>
    public abstract class ZingExplorer
    {
        #region Variable Declarations

        /// <summary>
        /// Cancellation token so that Zinger can be terminated from command line.
        /// </summary>
        protected CancellationTokenSource CancelTokenZingExplorer;

        /// <summary>
        /// Stores the liveness error traces
        /// </summary>
        protected ArrayList AcceptingCycles;

        /// <summary>
        /// Stores the safety error traces
        /// </summary>
        protected ArrayList SafetyErrors;

        /// <summary>
        /// Stores the return value for the explorer
        /// </summary>
        protected ZingerResult lastErrorFound;

        /// <summary>
        /// Traversal Info for the Initial state of the system
        /// </summary>
        protected TraversalInfo StartStateTraversalInfo;

        /// <summary>
        /// StateImpl for the start state
        /// </summary>
        private StateImpl startStateStateImpl;

        public StateImpl StartStateStateImpl
        {
            get { return startStateStateImpl; }
            set { startStateStateImpl = value; }
        }

        public State InitialState
        {
            get
            {
                return new State(startStateStateImpl);
            }
        }

        #endregion Variable Declarations

        #region Abstract Functions

        protected abstract ZingerResult IterativeSearchStateSpace();

        protected abstract void SearchStateSpace(object obj);

        protected abstract bool MustExplore(TraversalInfo ti);

        protected abstract void VisitState(TraversalInfo ti);

        #endregion Abstract Functions

        public ZingerResult Explore()
        {
            //set the execute tracestatements to false
            //trace statements should be executed only when error trace is generated.
            ZingerConfiguration.ExecuteTraceStatements = false;

            if (StartStateTraversalInfo.IsInvalidEndState())
            {
                bool oldCT = ZingerConfiguration.CompactTraces;
                ZingerConfiguration.CompactTraces = false;
                SafetyErrors.Add(StartStateTraversalInfo.GenerateTrace());
                ZingerConfiguration.CompactTraces = oldCT;
                this.lastErrorFound = StartStateTraversalInfo.ErrorCode;
            }

            var result = IterativeSearchStateSpace();

            //In the end of the plugin is enabled then call the plugin
            if (ZingerConfiguration.ZPlugin != null)
            {
                Console.WriteLine("Executed EndPlugin Function");
                ZingerConfiguration.ZPlugin.zPlugin.EndPlugin();
            }

            //Handle Zinger Return Status
            switch (result)
            {
                case ZingerResult.Success:
                    ZingerUtilities.PrintSuccessMessage("##################");
                    ZingerUtilities.PrintSuccessMessage("Check Passed");
                    ZingerUtilities.PrintSuccessMessage("##################");
                    return ZingerResult.Success;

                case ZingerResult.ZingRuntimeError:
                    ZingerUtilities.PrintErrorMessage("Zinger Internal Runtime Exception");
                    break;

                case ZingerResult.ProgramRuntimeError:
                    ZingerUtilities.PrintErrorMessage("Program Runtime Error");
                    break;

                case ZingerResult.Assertion:
                    ZingerUtilities.PrintErrorMessage("##################");
                    ZingerUtilities.PrintErrorMessage("Check Failed");
                    ZingerUtilities.PrintErrorMessage("##################");
                    break;

                case ZingerResult.Deadlock:
                    ZingerUtilities.PrintErrorMessage("Deadlock Detected !");
                    break;

                case ZingerResult.AcceptanceCyleFound:
                    ZingerUtilities.PrintErrorMessage("##################");
                    ZingerUtilities.PrintErrorMessage("Liveness Check Failed");
                    ZingerUtilities.PrintErrorMessage("##################");
                    break;

                case ZingerResult.DFSStackOverFlowError:
                    ZingerUtilities.PrintErrorMessage("##################");
                    ZingerUtilities.PrintErrorMessage("Check Failed");
                    ZingerUtilities.PrintErrorMessage("##################");
                    ZingerUtilities.PrintErrorMessage(String.Format("DFS Stack Size Exceeded {0}", ZingerConfiguration.BoundDFSStackLength));
                    break;

                case ZingerResult.ZingerMotionPlanningInvocation:
                    break;

                default:
                    ZingerUtilities.PrintErrorMessage("Zinger threw an unknown error. Please report this to the Zing developers");
                    break;
            }

            ZingerConfiguration.ExecuteTraceStatements = true;

            if (ZingerConfiguration.DetailedZingTrace)
            {
                PrintErrorTracesDetailed();
            }
            if (ZingerConfiguration.EnableTrace)
            {
                PrintErrorTracesToFile();
            }

            return result;
        }

        public ZingerResult ExploreWithDronacharya()
        {
            ZingerUtilities.PrintMessage("Starting new Zinger iteration ... ");
            //Terminate only when there are no motionPlanFrontier states remaining.
            while (true)
            {
                //set the execute tracestatements to false
                //trace statements should be executed only when error trace is generated.
                ZingerConfiguration.ExecuteTraceStatements = false;

                if (StartStateTraversalInfo.IsInvalidEndState())
                {
                    bool oldCT = ZingerConfiguration.CompactTraces;
                    ZingerConfiguration.CompactTraces = false;
                    SafetyErrors.Add(StartStateTraversalInfo.GenerateTrace());
                    ZingerConfiguration.CompactTraces = oldCT;
                    this.lastErrorFound = StartStateTraversalInfo.ErrorCode;
                }

                var result = IterativeSearchStateSpace();

                //Handle Zinger Return Status
                switch (lastErrorFound)
                {
                    case ZingerResult.Success:
                        ZingerUtilities.PrintSuccessMessage("##################");
                        ZingerUtilities.PrintSuccessMessage("Check Passed");
                        ZingerUtilities.PrintSuccessMessage("##################");
                        return ZingerResult.Success;

                    case ZingerResult.ZingRuntimeError:
                        ZingerUtilities.PrintErrorMessage("Zinger Internal Runtime Exception");
                        break;

                    case ZingerResult.ProgramRuntimeError:
                        ZingerUtilities.PrintErrorMessage("Program Runtime Error");
                        break;

                    case ZingerResult.Assertion:
                        ZingerUtilities.PrintErrorMessage("##################");
                        ZingerUtilities.PrintErrorMessage("Check Failed");
                        ZingerUtilities.PrintErrorMessage("##################");
                        break;

                    case ZingerResult.Deadlock:
                        ZingerUtilities.PrintErrorMessage("Deadlock Detected !");
                        break;

                    case ZingerResult.AcceptanceCyleFound:
                        ZingerUtilities.PrintErrorMessage("##################");
                        ZingerUtilities.PrintErrorMessage("Liveness Check Failed");
                        ZingerUtilities.PrintErrorMessage("##################");
                        break;

                    case ZingerResult.DFSStackOverFlowError:
                        ZingerUtilities.PrintErrorMessage("##################");
                        ZingerUtilities.PrintErrorMessage("Check Failed");
                        ZingerUtilities.PrintErrorMessage("##################");
                        ZingerUtilities.PrintErrorMessage(String.Format("DFS Stack Size Exceeded {0}", ZingerConfiguration.BoundDFSStackLength));
                        break;

                    case ZingerResult.ZingerMotionPlanningInvocation:
                        break;

                    default:
                        ZingerUtilities.PrintErrorMessage("Zinger threw an unknown error. Please report this to the Zing developers");
                        break;
                }

                //if the error is something other than motionPlanningInvocation
                if (lastErrorFound != ZingerResult.ZingerMotionPlanningInvocation)
                {
                    ZingerConfiguration.ExecuteTraceStatements = true;

                    if (ZingerConfiguration.DetailedZingTrace)
                    {
                        PrintErrorTracesDetailed();
                    }
                    if (ZingerConfiguration.EnableTrace)
                    {
                        PrintErrorTracesToFile();
                    }
                }
                else
                {
                    ZingDronacharya.RunMotionPlanner(ZingerConfiguration.ZDronacharya);
                }
                //In the end if the plugin is enabled then call the plugin
                if (ZingerConfiguration.ZPlugin != null)
                {
                    Console.WriteLine("Executed EndPlugin Function");
                    ZingerConfiguration.ZPlugin.zPlugin.EndPlugin();
                }
                return result;
            }
        }

        #region Constructor

        public ZingExplorer()
        {
            startStateStateImpl = StateImpl.Load(ZingerConfiguration.ZingModelFile);
            Initialize();
        }

        private void Initialize()
        {
            //Fingerprint the start state.
            Fingerprint fp = startStateStateImpl.Fingerprint;
            CancelTokenZingExplorer = new CancellationTokenSource();

            SafetyErrors = new ArrayList();
            AcceptingCycles = new ArrayList();
            lastErrorFound = ZingerResult.Success;
            StartStateTraversalInfo = GetTraversalInfoForTrace(null);
        }

        #endregion Constructor

        #region Helper Functions for Generating TraversalInfo

        protected TraversalInfo GetTraversalInfoForTrace(Trace trace)
        {
            TraversalInfo ti = new ExecutionState((StateImpl)StartStateStateImpl.Clone(), null, null);
            TraversalInfo newTi = null;

            if (trace != null)
            {
                if (trace.Count > 0)
                {
                    for (int i = 0; i < trace.Count; ++i)
                    {
                        newTi = ti.GetSuccessorN((int)trace[i].Selection);
                        System.Diagnostics.Debug.Assert(newTi != null);
                        ti = newTi;
                    }
                }
            }

            return ti;
        }

        #endregion Helper Functions for Generating TraversalInfo

        #region PrintErrorTraces

        public void PrintErrorTracesToFile()
        {
            StreamWriter tracer = new StreamWriter(File.Open(ZingerConfiguration.traceLogFile, FileMode.Create));
            var safetyTraces = (Trace[])SafetyErrors.ToArray(typeof(Trace));
            var livenessTraces = (Trace[])AcceptingCycles.ToArray(typeof(Trace));
            for (int i = 0; i < safetyTraces.Length; i++)
            {
                Trace trace = safetyTraces[i];
                State[] states;
                states = trace.GetStates(InitialState);
                tracer.WriteLine("Safety Error Trace");
                tracer.WriteLine("Trace-Log {0}:", i);
                for (int j = 0; j < states.Length; j++)
                {
                    ZingEvent[] traceLogs;
                    traceLogs = states[j].GetTraceLog();

                    for (int k = 0; k < traceLogs.Length; k++)
                    {
                        tracer.Write("{0}", traceLogs[k]);
                        tracer.Flush();
                    }
                    if (states[j].Error != null)
                    {
                        if (states[j].Error is ZingAssertionFailureException)
                        {
                            tracer.WriteLine();
                            tracer.WriteLine("Error:");
                            tracer.WriteLine("P Assertion failed:");
                            tracer.WriteLine("Expression: assert({0})", (states[j].Error as ZingAssertionFailureException).Expression);
                            tracer.WriteLine("Comment: {0}", (states[j].Error as ZingAssertionFailureException).Comment);
                        }
                        else
                        {
                            Console.WriteLine();
                            Console.WriteLine("Error:");
                            Console.WriteLine("{0}", states[j].Error);
                        }
                    }
                }
            }

            for (int i = 0; i < livenessTraces.Length; i++)
            {
                Trace trace = livenessTraces[i];
                State[] states;
                states = trace.GetStates(InitialState);
                tracer.WriteLine("Liveness Error Trace --- ");
                tracer.WriteLine("Trace-Log {0}:", i);
                for (int j = 0; j < states.Length; j++)
                {
                    ZingEvent[] traceLogs;
                    traceLogs = states[j].GetTraceLog();

                    for (int k = 0; k < traceLogs.Length; k++)
                    {
                        tracer.Write("{0}", traceLogs[k]);
                    }
                    if (states[j].Error != null)
                    {
                        tracer.WriteLine();
                        tracer.WriteLine("Error:");
                        if (states[j].Error is ZingAssertionFailureException)
                        {
                            tracer.WriteLine("P Assertion failed:");
                            tracer.WriteLine("Expression: assert({0})", (states[j].Error as ZingAssertionFailureException).Expression);
                            tracer.WriteLine("Comment: {0}", (states[j].Error as ZingAssertionFailureException).Comment);
                        }
                        else
                        {
                            Console.WriteLine();
                            Console.WriteLine("Error:");
                            Console.WriteLine("{0}", states[j].Error);
                        }
                    }
                }
            }

            tracer.Close();
        }

        public void PrintErrorTracesDetailed()
        {
            var safetyTraces = (Trace[])SafetyErrors.ToArray(typeof(Trace));
            var livenessTraces = (Trace[])AcceptingCycles.ToArray(typeof(Trace));
            for (int i = 0; i < safetyTraces.Length; i++)
            {
                Trace trace = safetyTraces[i];
                State[] states;
                states = trace.GetStates(InitialState);
                Console.WriteLine(" *******************************************************************************");
                Console.WriteLine(" Error trace {0}: length: {1} states", i, states.Length);
                for (int j = 0; j < states.Length; j++)
                {
                    if (j == 0)
                        Console.Write("#### State {0} : \r\n {1}", j, states[j]);
                    else
                    {
                        if (trace[j - 1].IsExecution)
                            Console.Write("#### State {0} (ran process {1}) :\r\n{2}", j, trace[j - 1].Selection, states[j]);
                        else
                            Console.Write("#### State {0} (took choice {1}) :\r\n{2}", j, trace[j - 1].Selection, states[j]);
                    }

                    if (states[j].Error != null)
                    {
                        Console.WriteLine();
                        Console.WriteLine("Error in state:");
                        Console.WriteLine("{0}", states[j].Error);
                    }
                }
            }

            for (int i = 0; i < livenessTraces.Length; i++)
            {
                Trace trace = livenessTraces[i];
                State[] states;
                states = trace.GetStates(InitialState);
                for (int j = 0; j < states.Length; j++)
                {
                    if (states[j].IsAcceptanceState)
                    {
                        Console.WriteLine();
                        Console.WriteLine("#### Accepting State ####");
                    }

                    if (j == 0)
                        Console.Write("#### State {0} : \r\n {1}", j, states[j]);
                    else
                    {
                        if (trace[j - 1].IsExecution)
                            Console.Write("#### State {0} (ran process {1}) :\r\n{2}", j, trace[j - 1].Selection, states[j]);
                        else
                            Console.Write("#### State {0} (took choice {1}) :\r\n{2}", j, trace[j - 1].Selection, states[j]);
                    }

                    if (states[j].Error != null)
                    {
                        Console.WriteLine();
                        Console.WriteLine("Error in state:");
                        Console.WriteLine("{0}", states[j].Error);
                    }
                }
            }
        }

        #endregion PrintErrorTraces
    }
}