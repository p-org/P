using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using P.Runtime;
using Microsoft.PSharp;
using Microsoft.PSharp.Utilities;

namespace P.Tester
{
    public enum TestResult
    {
        /// <summary>
        /// No errors were found within the specified limits of the search (if any).
        /// </summary>
        Success = 0,

        /// <summary>
        /// Invalid parameters passed
        /// </summary>
        InvalidParameters = 1,

        /// <summary>
        /// An assertion failure was encountered.
        /// </summary>
        AssertionFailure = 2,

        /// <summary>
        /// An execution was found in which all P machines are blocked and at least one liveness monitor
        /// is in a hot state.
        /// </summary>
        Deadlock = 3,

        /// <summary>
        /// A lasso violating a liveness monitor was discovered.
        /// </summary>
        AcceptingCyleFound = 4,

        /// <summary>
        /// An internal error was encountered, typically indicating a bug in the compiler or runtime.
        /// </summary>
        InternalError = 5,

        /// <summary>
        /// Search stack size exceeded the maximum size.
        /// </summary>
        StackOverFlowError = 6,

        /// <summary>
        /// The search was canceled.
        /// </summary>
        Canceled = 7,

        /// <summary>
        /// Timeout
        /// </summary>
        Timeout = 8,
    }

    public class CommandLineOptions
    {
        public string inputFileName;
        public bool printStats;
        public int timeout;
        public bool UsePSharp = false;
        public bool isRefinement;
        public string LHSModel;
        public string RHSModel;
    }

    public class PTesterCommandLine
    {
        public static CommandLineOptions ParseCommandLine(string[] args)
        {
            var options = new CommandLineOptions();
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                if (arg[0] == '-' || arg[0] == '/')
                {
                    string option = arg.TrimStart('/', '-').ToLower();
                    string param = string.Empty;

                    int sepIndex = option.IndexOf(':');

                    if (sepIndex > 0)
                    {
                        param = option.Substring(sepIndex + 1);
                        option = option.Substring(0, sepIndex);
                    }
                    else if (sepIndex == 0)
                    {
                        PrintHelp(arg, "Malformed option");
                        return null;
                    }

                    switch (option)
                    {
                        case "?":
                        case "h":
                            {
                                PrintHelp(null, null);
                                Environment.Exit((int)TestResult.Success);
                                break;
                            }
                        case "stats":
                            options.printStats = true;
                            break;

                        case "timeout":
                            if (param.Length != 0)
                            {
                                options.timeout = int.Parse(param);
                            }
                            break;
                        case "psharp":
                            options.UsePSharp = true;
                            break;
                        case "break":
                            System.Diagnostics.Debugger.Launch();
                            break;
                        case "lhs":
                            if (param.Length != 0)
                            {
                                options.LHSModel = param;
                                options.RHSModel = null;
                                options.isRefinement = true;
                            }
                            else
                            {
                                PrintHelp(arg, "missing file name");
                                return null;
                            }
                            break;
                        case "rhs":
                            if (param.Length != 0)
                            {
                                options.RHSModel = param;
                                options.LHSModel = null;
                                options.isRefinement = true;
                            }
                            else
                            {
                                PrintHelp(arg, "missing file name");
                                return null;
                            }
                            break;
                        default:
                            PrintHelp(arg, "Invalid option");
                            return null;
                    }
                }
                else
                {
                    if (options.inputFileName != null)
                    {
                        PrintHelp(arg, "Only one input file is allowed");
                        return null;
                    }

                    if (!File.Exists(arg))
                    {
                        PrintHelp(arg, "Cannot find input file");
                        return null;
                    }

                    options.inputFileName = arg;
                }
            }

            if (!options.isRefinement && options.inputFileName == null)
            {
                PrintHelp(null, "No input file specified");
                return null;
            }
            return options;
        }

        public static void PrintHelp(string arg, string errorMessage)
        {
            if (errorMessage != null)
            {
                if (arg != null)
                    PTesterUtil.PrintErrorMessage(String.Format("Error: \"{0}\" - {1}", arg, errorMessage));
                else
                    PTesterUtil.PrintErrorMessage(String.Format("Error: {0}", errorMessage));
            }

            Console.Write("HELP ME");
        }

        public static void Main(string[] args)
        {
            var options = ParseCommandLine(args);
            if (options == null)
            {
                Environment.Exit((int)TestResult.InvalidParameters);
            }

            if (options.isRefinement)
            {
                var refinementCheck = new RefinementChecking(options);
                if (options.LHSModel == null)
                {
                    refinementCheck.RunCheckerRHS();
                }
                else
                {
                    refinementCheck.RunCheckerLHS();
                }
                return;
            }

            var asm = Assembly.LoadFrom(options.inputFileName);
            StateImpl s = (StateImpl)asm.CreateInstance("P.Program.Application",
                                                        false,
                                                        BindingFlags.CreateInstance,
                                                        null,
                                                        new object[] { true },
                                                        null,
                                                        new object[] { });
            if (s == null)
                throw new ArgumentException("Invalid assembly");

            if (options.UsePSharp)
            {
                RunPSharpTester(s);
                return;
            }

            int maxNumOfSchedules = 10000;
            int maxDepth = 1000;
            int numOfSchedules = 0;
            int numOfSteps = 0;
            var randomScheduler = new Random(DateTime.Now.Millisecond);
            while (numOfSchedules < maxNumOfSchedules)
            {
                var currImpl = (StateImpl)s.Clone();
                if (numOfSchedules % 10 == 0)
                {
                    Console.WriteLine("-----------------------------------------------------");
                    Console.WriteLine("Total Schedules Explored: {0}", numOfSchedules);
                    Console.WriteLine("-----------------------------------------------------");
                }
                numOfSteps = 0;
                while (numOfSteps < maxDepth)
                {
                    if (currImpl.EnabledMachines.Count == 0)
                    {
                        break;
                    }

                    var num = currImpl.EnabledMachines.Count;
                    var choosenext = randomScheduler.Next(0, num);
                    currImpl.EnabledMachines[choosenext].PrtRunStateMachine();
                    if (currImpl.Exception != null)
                    {
                        if (currImpl.Exception is PrtAssumeFailureException)
                        {
                            break;
                        }
                        else if (currImpl.Exception is PrtException)
                        {
                            Console.WriteLine(currImpl.errorTrace.ToString());
                            Console.WriteLine("ERROR: {0}", currImpl.Exception.Message);
                            Environment.Exit(-1);
                        }
                        else
                        {
                            Console.WriteLine(currImpl.errorTrace.ToString());
                            Console.WriteLine("[Internal Exception]: Please report to the P Team");
                            Console.WriteLine(currImpl.Exception.ToString());
                            Environment.Exit(-1);
                        }
                    }
                    numOfSteps++;
                }
                numOfSchedules++;
            }

        }

        public static StateImpl main_s;

        public static void RunPSharpTester(StateImpl s)
        {
            main_s = s;

            var configuration = Configuration.Create()
                .WithNumberOfIterations(1000);
            configuration.MaxSchedulingSteps = 1000;


            var engine = Microsoft.PSharp.TestingServices.TestingEngineFactory.CreateBugFindingEngine(
                configuration, PSharpWrapper.Execute);
            engine.Run();

            Console.WriteLine("Bugs found: {0}", engine.TestReport.NumOfFoundBugs);
        }
    }

    public class PSharpWrapper
    {
        public static void Execute(PSharpRuntime runtime)
        {
            runtime.CreateMachine(typeof(PSharpMachine), new MachineInitEvent((StateImpl)PTesterCommandLine.main_s.Clone()));
        }

        public class Unit : Microsoft.PSharp.Event { }

        public class MachineInitEvent : Microsoft.PSharp.Event
        {
            public StateImpl s;

            public MachineInitEvent(StateImpl s)
            {
                this.s = s;
            }
        }

        class PSharpMachine : Microsoft.PSharp.Machine
        {
            StateImpl currImpl;
            Dictionary<PrtSpecMachine, Type> specToMonitor;

            [Microsoft.PSharp.Start]
            [Microsoft.PSharp.OnEntry(nameof(Configure))]
            [Microsoft.PSharp.OnEventDoAction(typeof(Unit), nameof(Step))]
            class Init : Microsoft.PSharp.MachineState { }

            void Configure()
            {
                var e = (this.ReceivedEvent as MachineInitEvent);
                this.currImpl = e.s;
                this.specToMonitor = new Dictionary<PrtSpecMachine, Type>();

                // register monitors
                foreach (var spec in currImpl.GetAllSpecMachines())
                {
                    var genericTy = typeof(PSharpMonitor<int>).GetGenericTypeDefinition();
                    var specTy = spec.GetType();
                    var monitorTy = genericTy.MakeGenericType(specTy);
                    this.specToMonitor.Add(spec, monitorTy);

                    this.Id.Runtime.RegisterMonitor(monitorTy);
                }

                this.Raise(new Unit());
            }

            void Step()
            {
                if (currImpl.EnabledMachines.Count == 0)
                {
                    return;
                }

                foreach (var tup in specToMonitor)
                {
                    Event ev = tup.Key.currentTemperature == StateTemperature.Hot ? (Event) new MoveToHot() :
                        tup.Key.currentTemperature == StateTemperature.Warm ? (Event) new MoveToWarm() :
                        (Event) new MoveToCold();

                    this.Monitor(tup.Value, ev);
                }


                var num = currImpl.EnabledMachines.Count;
                var choosenext = this.RandomInteger(num);
                currImpl.EnabledMachines[choosenext].PrtRunStateMachine();
                if (currImpl.Exception != null)
                {
                    if (currImpl.Exception is PrtAssumeFailureException)
                    {
                        return;
                    }
                    else
                    {
                        this.Assert(false, currImpl.Exception.ToString());
                    }
                }

                this.Raise(new Unit());
            }

        }

        class MoveToHot : Event { }
        class MoveToCold : Event { }
        class MoveToWarm : Event { }

        class PSharpMonitor<T> : Monitor
        {
            [Start]
            [Cold]
            [OnEventDoAction(typeof(MoveToHot), nameof(GotHot))]
            [OnEventDoAction(typeof(MoveToCold), nameof(GotCold))]
            [OnEventDoAction(typeof(MoveToWarm), nameof(GotWarm))]
            class S1 : MonitorState { }

            [Hot]
            [OnEventDoAction(typeof(MoveToHot), nameof(GotHot))]
            [OnEventDoAction(typeof(MoveToCold), nameof(GotCold))]
            [OnEventDoAction(typeof(MoveToWarm), nameof(GotWarm))]
            class S2 : MonitorState { }

            [OnEventDoAction(typeof(MoveToHot), nameof(GotHot))]
            [OnEventDoAction(typeof(MoveToCold), nameof(GotCold))]
            [OnEventDoAction(typeof(MoveToWarm), nameof(GotWarm))]
            class S3 : MonitorState { }

            void GotHot()
            {
                this.Goto(typeof(S2));
            }

            void GotCold()
            {
                this.Goto(typeof(S1));
            }

            void GotWarm()
            {
                this.Goto(typeof(S3));
            }

        }
    }

}
