using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;
using P.Program;

namespace PingPong
{
    public class Driver
    {
        static P.Program.Application currImpl;
        static P.Runtime.PrtImplMachine mainMachine;
        static Dictionary<P.Runtime.PrtImplMachine, MachineId> machineToId;
        static PSharpRuntime runtime;

        static void Main(string[] args)
        {
            currImpl = new P.Program.Application(true);
            var randomScheduler = new Random(0);

            Run(randomScheduler);
        }

        [Microsoft.PSharp.Test]
        public static void Test(PSharpRuntime runtime)
        {
            Driver.runtime = runtime;
            machineToId = new Dictionary<P.Runtime.PrtImplMachine, MachineId>();
            currImpl = new P.Program.Application(true);

            P.Runtime.StateImpl.CreateMachineCallBack = 
                new Action<P.Runtime.PrtImplMachine>(m => CreateMachineCallback(m));

            P.Runtime.StateImpl.EnqueueCallBack =
                new Action<P.Runtime.PrtImplMachine, P.Runtime.PrtImplMachine>((m1, m2) => EnqueueCallback(m1, m2));

            mainMachine = currImpl.EnabledMachines.FirstOrDefault();
            var id = runtime.CreateMachine(typeof(PSharpMachine), new MachineInitEvent(mainMachine));
            machineToId.Add(mainMachine, id);
        }

        static void CreateMachineCallback(P.Runtime.PrtImplMachine p_machine)
        {
            var id = runtime.CreateMachine(typeof(PSharpMachine), new MachineInitEvent(p_machine));
            machineToId.Add(p_machine, id);
        }

        static void EnqueueCallback(P.Runtime.PrtImplMachine source, P.Runtime.PrtImplMachine target)
        {
            runtime.SendEvent(machineToId[target], new Unit());
        }


        static void Run(Random randomScheduler)
        {
            Console.WriteLine("-----------------------------------------------------");
            Console.WriteLine("New Schedule:");
            Console.WriteLine("-----------------------------------------------------");
            var numOfSteps = 0;
            while (numOfSteps < 10)
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
                    if (currImpl.Exception is P.Runtime.PrtAssumeFailureException)
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
            
        }

        public static bool IsEnabled(P.Runtime.PrtImplMachine p_machine)
        {
            return currImpl.EnabledMachines.Contains(p_machine);
        }

        public static bool IsMainMachine(P.Runtime.PrtImplMachine p_machine)
        {
            return mainMachine == p_machine;
        }

        public static bool IsFaulted()
        {
            if (currImpl.Exception != null)
            {
                Console.WriteLine("Exception hit during execution: {0}", currImpl.Exception.ToString());
                return true;
            }
            return false;
        }
    }

    public class MachineInitEvent : Microsoft.PSharp.Event
    {
        public P.Runtime.PrtImplMachine p_machine;

        public MachineInitEvent(P.Runtime.PrtImplMachine p_machine)
        {
            this.p_machine = p_machine;
        }
    }


    public class Unit : Microsoft.PSharp.Event { }

    class PSharpMachine : Microsoft.PSharp.Machine
    {
        P.Runtime.PrtImplMachine p_machine;

        [Microsoft.PSharp.Start]
        [Microsoft.PSharp.OnEntry(nameof(Configure))]
        [Microsoft.PSharp.OnEventDoAction(typeof(Unit), nameof(Step))]
        class Init : Microsoft.PSharp.MachineState { }

        void Configure()
        {
            var e = (this.ReceivedEvent as MachineInitEvent);
            p_machine = e.p_machine;
            if (Driver.IsMainMachine(p_machine))
            {
                Step();
            }
        }

        void Step()
        {
            if (Driver.IsEnabled(p_machine))
            {
                p_machine.PrtRunStateMachine();

                if (Driver.IsFaulted())
                {
                    throw new Exception("Done");
                }

                this.Raise(new Unit());
            }
        }
    }




}

