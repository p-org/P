using System;
using System.Collections.Generic;
using System.IO;
using P.Runtime;
using System.Reflection;

namespace P.Tester
{
    

    public class RefinementChecking
    {
        string LHSModel;
        string RHSModel;

        int maxLengthOfExecution = 1000;

        List<VisibleTrace> allTracesRHS;
        public void AddTrace(VisibleTrace tc)
        {
            if (allTracesRHS.Contains(tc))
                return;
            else
                allTracesRHS.Add(tc);
        }

        public void CheckTraceContainment(VisibleTrace tc)
        {
            if (!allTracesRHS.Contains(tc))
            {
                Console.WriteLine("Following trace not contained in abstraction ::");
                Console.WriteLine(tc.ToString());
            }
        }
        public RefinementChecking(string lhs, string rhs)
        {
            LHSModel = lhs;
            if(!File.Exists(LHSModel))
            {
                Console.WriteLine("LHSModel file: {0} does not exist", LHSModel);
                Environment.Exit(-1);
            }
            RHSModel = rhs;
            if (!File.Exists(RHSModel))
            {
                Console.WriteLine("LHSModel file: {0} does not exist", RHSModel);
                Environment.Exit(-1);
            }
            allTracesRHS = new List<VisibleTrace>();
        }

        public void RunChecker()
        {
            //create a appdomain for RHS
            var rhsDomain = AppDomain.CreateDomain("RHSModel");
            //First phase compute all possible traces of RHS
            rhsDomain.Load(@"C:\Workspace\P\Bld\Drops\Debug\x64\Binaries\Prt.dll");
            var asm = rhsDomain.Load(RHSModel);
            StateImpl rhsStateImpl = (StateImpl)asm.CreateInstance("P.Program.Application",
                                                        false,
                                                        BindingFlags.CreateInstance,
                                                        null,
                                                        new object[] { true },
                                                        null,
                                                        new object[] { });
            if (rhsStateImpl == null)
                throw new ArgumentException("Invalid RHS assembly");

            int numOfSchedules = 0;
            int numOfSteps = 0;
            var randomScheduler = new Random(DateTime.Now.Millisecond);
            while (numOfSchedules < 10000)
            {
                var currImpl = (StateImpl)rhsStateImpl.Clone();
                Console.WriteLine("-----------------------------------------------------");
                Console.WriteLine("New Schedule: {0}", numOfSchedules);
                Console.WriteLine("-----------------------------------------------------");
                numOfSteps = 0;
                while (true)
                {
                    if(numOfSteps >= maxLengthOfExecution)
                    {
                        Console.WriteLine("There is an execution in RHS of length more than {0}", maxLengthOfExecution);
                        return;
                    }

                    if (currImpl.EnabledMachines.Count == 0)
                    {
                        //execution terminated, add the trace to set of traces
                        AddTrace(currImpl.currentTrace);
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
                        else
                        {
                            Console.WriteLine("Exception hit during execution: {0}", currImpl.Exception.ToString());
                            Environment.Exit(-1);
                        }
                    }
                    numOfSteps++;
                }
                numOfSchedules++;
            }

            Console.WriteLine("Loaded all traces of RHS");
            Console.WriteLine("Total Traces: {0}", allTracesRHS.Count);
            Console.WriteLine("Press any key to continue");
            Console.ReadKey();
            //starting the second phase
            asm = Assembly.LoadFrom(LHSModel);
            StateImpl lhsStateImpl = (StateImpl)asm.CreateInstance("P.Program.Application",
                                                        false,
                                                        BindingFlags.CreateInstance,
                                                        null,
                                                        new object[] { true },
                                                        null,
                                                        new object[] { });
            if (rhsStateImpl == null)
                throw new ArgumentException("Invalid RHS assembly");

            numOfSchedules = 0;
            numOfSteps = 0;
            randomScheduler = new Random(DateTime.Now.Millisecond);
            while (numOfSchedules < 100)
            {
                var currImpl = (StateImpl)lhsStateImpl.Clone();
                Console.WriteLine("-----------------------------------------------------");
                Console.WriteLine("New Schedule: {0}", numOfSchedules);
                Console.WriteLine("-----------------------------------------------------");
                numOfSteps = 0;
                while (true)
                {
                    if (numOfSteps >= maxLengthOfExecution)
                    {
                        Console.WriteLine("There is an execution in LHS of length more than {0}", maxLengthOfExecution);
                        return;
                    }

                    if (currImpl.EnabledMachines.Count == 0)
                    {
                        //execution terminated, add the trace to set of traces
                        CheckTraceContainment(currImpl.currentTrace);
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
                        else
                        {
                            Console.WriteLine("Exception hit during execution: {0}", currImpl.Exception.ToString());
                            Environment.Exit(-1);
                        }
                    }
                    numOfSteps++;
                }
                numOfSchedules++;
            }
        }
    }
}
