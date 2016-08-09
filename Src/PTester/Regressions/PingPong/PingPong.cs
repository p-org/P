using P.PRuntime;
using System.Collections;
using System.Collections.Generic;
using System;
/*
* Simple P program

event dummy;

machine Main {
start state Init {
entry {
send this, dummy;
}
on dummy goto Fail;
}

state Fail {
entry {
assert false;
}
}

}
*/
#region template
/*    
      class Application : PStateImpl {
        // one list for each machine and monitor type
        List<A> A_list;
        List<B> B_list;
         ...
         // implement AllMachines, AllMonitors
         

        // What is the design of the constructor?
        public Application() { ... } 

        Each event becomes a static field in Application class
      
        public static Event A = new Event(...);

        Each static function  B becomes a class and static field in Application class

        // Can static functions be called from monitors
        // If yes, the type parameter must be BaseMachine; if no, it can be Machine
        public class B_Fun : Fun<BaseMachine> {
            // implement the abstract methods in Fun
        }

        public static B_Fun B = new B_Fun();  // static field declaration in Application

        Each machine becomes a class in Application class

        public class Foo : Machine {
            public Foo(int instance): base(instance, numFields, maxBufferSize) {
                // initialize fields
            }

            Create getter/setter for each field so that code in functions looks nice

            Each function A in machine Foo becomes a class and a static field

            public class A_Fun : Fun<Foo> {
                // implement the abstract methods in Fun
            }
            public static A_Fun A = new A_Fun();

            Each state X in machine Foo becomes a static field
            
            public static State X = new State(...);

            static {
                // Create transitions
                // Wire up the states and transitions
                // Put the appropriate funs in states and transitions 
                // Presumably the static fields containing funs have already been initialized
            }
        }
     */
#endregion

namespace SimpleMachine
{
    public class Application : PStateImpl
    {
        public List<BaseMachine> MainMachines;

        //getters
        public int SizeOfMainMachines
        {
            get
            {
                return MainMachines.Count;
            }
        }

        public override IEnumerable<BaseMachine> AllAliveMachines
        {
            get
            {
                List<BaseMachine> ret = new List<BaseMachine>();
                ret.AddRange(MainMachines);
                return ret;
            }
        }

        public override IEnumerable<BaseMonitor> AllInstalledMonitors
        {
            get
            {
                return new List<BaseMonitor>();
            }
        }

        #region Constructors
        public Application() :base()
        {
            //initialize all the fields
        }

        public Application(bool initialize) : base()
        {
            //initialize all the fields
            CreateMainMachine();

        }
        #endregion

        //pass the right parameters here !!
        public static Event dummy = new Event("dummy", null, 0, false);

        public Machine<Main> CreateMainMachine()
        {
            var mainMachine = new Main(this, SizeOfMainMachines, 10);
            AddStateMachine(mainMachine);
            MainMachines.Add(mainMachine);

            return mainMachine;
        }

        public class Main : Machine<Main>
        {
            public override State<Main> StartState
            {
                get
                {
                    return Init_State;
                }
            }

            public override string Name
            {
                get
                {
                    return "Main";
                }
            }
            //constructor
            public Main(PStateImpl app, int instance, int maxB) : base (app, instance, maxB)
            {
                fields = new List<PrtValue>();
            }
            //getters and setters


            #region Functions
            public class Anon_0 : Fun<Main>
            {
                public override string Name
                {
                    get
                    {
                        return "Init_Entry";
                    }
                }
                public override void Execute(PStateImpl application, Main parent)
                {
                    throw new NotImplementedException();
                }

                public override void PushFrame(Main parent, params PrtValue[] args)
                {
                    throw new NotImplementedException();
                }
            }

            public class Anon_1 : Fun<Main>
            {
                public override string Name
                {
                    get
                    {
                        return "Fail_Entry";
                    }
                }
                public override void Execute(PStateImpl application, Main parent)
                {
                    throw new NotImplementedException();
                }

                public override void PushFrame(Main parent, params PrtValue[] args)
                {
                    throw new NotImplementedException();
                }
            }

            public static Anon_0 Anon_0_Fun;

            public static Anon_1 Anon_1_Fun;
            #endregion

            #region States
            public class Init : State<Main>
            {
                public Init(string name, Fun<Main> entryFun, Fun<Main> exitFun, bool hasNullTransition, StateTemperature temperature) 
                    :base (name, entryFun, exitFun, hasNullTransition, temperature)
                {

                }
            }

            public class Fail : State<Main>
            {
                public Fail(string name, Fun<Main> entryFun, Fun<Main> exitFun, bool hasNullTransition, StateTemperature temperature) 
                    :base (name, entryFun, exitFun, hasNullTransition, temperature)
                {

                }
            }

            public static Init Init_State;
            public static Fail Fail_State;
            #endregion

            static Main()
            {
                //initialize functions
                Anon_0_Fun = new Anon_0();
                Anon_1_Fun = new Anon_1();

                //initialize states 
                Init_State = new Init("Init", Anon_0_Fun, null, false, StateTemperature.Warm);
                Fail_State = new Fail("Fail", Anon_1_Fun, null, false, StateTemperature.Warm);

                //create transition and add them to the state
                Transition<Main> transition_1 = new Transition<Main>(null, Fail_State);

                //add transition
                Init_State.transitions.Add(dummy, transition_1);

            }

            
        }

    }

    
} 