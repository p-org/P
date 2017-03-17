using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using P.Runtime;

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
        public bool isRefinement;
        public string LHSModel;
        public string RHSModel;
        public bool verbose;
        public int numberOfSchedules;
        public CommandLineOptions()
        {
            inputFileName = null;
            printStats = false;
            timeout = 0;
            isRefinement = false;
            LHSModel = null;
            RHSModel = null;
            verbose = false;
            numberOfSchedules = 1000;
        }
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
                        case "v":
                        case "verbose":
                            options.verbose = true;
                            break;
                        case "ns":
                            if (param.Length != 0)
                            {
                                options.numberOfSchedules = int.Parse(param);
                            }
                            break;
                        case "timeout":
                            if (param.Length != 0)
                            {
                                options.timeout = int.Parse(param);
                            }
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

            Console.WriteLine("---------------------------------------------");
            Console.WriteLine("Options ::");
            Console.WriteLine("---------------------------------------------");
            Console.WriteLine("-h                       Print the help message");
            Console.WriteLine("-v or -verbose           Print the execution trace during exploration");
            Console.WriteLine("-ns:<int>                Number of schedulers <int> to explore");
            Console.WriteLine("-lhs:<LHS Model Dll>     Load the pre-computed traces of RHS Model and perform trace containment");
            Console.WriteLine("-rhs:<RHS Model Dll>     Compute all possible trace of the RHS Model using sampling and dump it in a file on disk");
        }

        public static void Main(string[] args)
        {
            var options = ParseCommandLine(args);
            if (options == null)
            {
                Environment.Exit((int)TestResult.InvalidParameters);
            }

            if(options.isRefinement)
            {
                var refinementCheck = new RefinementChecking(options);
                if(options.LHSModel == null)
                {
                    refinementCheck.RunCheckerRHS();
                }
                else
                {
                    refinementCheck.RunCheckerLHS();
                }
                return;
            }
            else
            {
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

                int maxNumOfSchedules = options.numberOfSchedules;
                int maxDepth = 1000;
                int numOfSchedules = 1;
                int numOfSteps = 0;
                var randomScheduler = new Random(DateTime.Now.Millisecond);
                while (numOfSchedules <= maxNumOfSchedules)
                {
                    var currImpl = (StateImpl)s.Clone();
                    if (numOfSchedules % 10 == 0)
                    {
                        Console.WriteLine("-----------------------------------------------------");
                        Console.WriteLine("Total Schedules Explored: {0}", numOfSchedules);
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

                        //print the execution if verbose
                        if(options.verbose)
                        {
                            Console.WriteLine("-----------------------------------------------------");
                            Console.WriteLine("Execution {0}", numOfSchedules);
                            Console.WriteLine(currImpl.errorTrace.ToString());
                        }
                    }
                    numOfSchedules++;
                }
            }
            
        }
    }
}
