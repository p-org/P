using System;
using System.Collections.Generic;
using System.IO;
using P.Runtime;
using System.Reflection;
using System.Security.Policy;
using System.Runtime.Serialization.Formatters.Binary;

namespace P.Tester
{
    

    public class RefinementChecking
    {
        string LHSModel;
        string RHSModel;

        int maxLengthOfExecution = 1000;
        int maxRHSSchedules = 10000;
        int maxLHSSchedules = 100;
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
                Environment.Exit(-1);
            }
        }
        public RefinementChecking(CommandLineOptions options)
        {
            LHSModel = options.LHSModel;
            if(LHSModel!= null && !File.Exists(LHSModel))
            {
                Console.WriteLine("LHSModel file: {0} does not exist", LHSModel);
                Environment.Exit(-1);
            }
            RHSModel = options.RHSModel;
            if (RHSModel != null && !File.Exists(RHSModel))
            {
                Console.WriteLine("LHSModel file: {0} does not exist", RHSModel);
                Environment.Exit(-1);
            }
            allTracesRHS = new List<VisibleTrace>();
        }

        public void RunCheckerRHS()
        {
            var asm = Assembly.LoadFrom(RHSModel);
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
            while (numOfSchedules < maxRHSSchedules)
            {
                var currImpl = (StateImpl)rhsStateImpl.Clone();
                if (numOfSchedules % 10 == 0)
                {
                    Console.WriteLine("-----------------------------------------------------");
                    Console.WriteLine("Total Schedules Explored: {0}", numOfSchedules);
                    Console.WriteLine("-----------------------------------------------------");
                }
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
                        AddTrace(currImpl.currentVisibleTrace);
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
            

            Stream writeStream = File.Open("alltraces", FileMode.Create);
            BinaryFormatter bFormat = new BinaryFormatter();
            bFormat.Serialize(writeStream, allTracesRHS);
            Console.WriteLine("Traces cached successfully");
        }

        public void RunCheckerLHS()
        {
            //Deserialize allTraces
            Stream readStream = File.Open("alltraces", FileMode.Open);
            BinaryFormatter bFormat = new BinaryFormatter();
            allTracesRHS = (List<VisibleTrace>)bFormat.Deserialize(readStream);
            Console.WriteLine("Traces loaded successfully");

            //starting the second phase
            var asm = Assembly.LoadFrom(LHSModel);
            StateImpl lhsStateImpl = (StateImpl)asm.CreateInstance("P.Program.Application",
                                                        false,
                                                        BindingFlags.CreateInstance,
                                                        null,
                                                        new object[] { true },
                                                        null,
                                                        new object[] { });
            if (lhsStateImpl == null)
                throw new ArgumentException("Invalid RHS assembly");

            int numOfSchedules = 0;
            int numOfSteps = 0;
            var randomScheduler = new Random(DateTime.Now.Millisecond);
            while (numOfSchedules < maxLHSSchedules)
            {
                var currImpl = (StateImpl)lhsStateImpl.Clone();
                if(numOfSchedules % 10 == 0)
                {
                    Console.WriteLine("-----------------------------------------------------");
                    Console.WriteLine("Total Schedules Explored: {0}", numOfSchedules);
                    Console.WriteLine("-----------------------------------------------------");
                }
                
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
                        CheckTraceContainment(currImpl.currentVisibleTrace);
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

            Console.WriteLine("Performed trace-containment check for {0} random executions and it succeeded", numOfSchedules);
        }
    }
}
