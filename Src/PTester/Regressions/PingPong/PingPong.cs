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
        public List<PrtMachine> MainMachines;


        #region Constructors
        public Application() :base()
        {
            //initialize all the fields
        }

        public override PStateImpl MakeSkeleton()
        {
            return new Application();
        }

        public Application(bool initialize) : base()
        {
            //initialize all non-static fields
            MainMachines = new List<PrtMachine>();

            //create the main machine
            CreateMainMachine();

        }
        #endregion

        //pass the right parameters here !!
        public static Event dummy = new Event("dummy", PrtType.NullType, 100, false);

        public Machine<Main> CreateMainMachine()
        {
            var mainMachine = new Main(this, 10);
            AddStateMachineToStateImpl(mainMachine);
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
            public Main(PStateImpl app, int maxB) : base(app, maxB)
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
                    ContStackFrame currCont = parent.cont.PopContFrame();
                    if (currCont.returnTolocation == 0)
                        goto Loc_0;
                    else
                        goto Ret;


                    Loc_0:
                    parent.EnqueueEvent(application, dummy, PrtValue.NullValue, parent);
                    parent.cont.Send(1, currCont.locals);

                    Ret:
                    parent.cont.Return(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    locals.AddRange(args);
                    //no local variables hence nothing to add
                    return locals;
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
                    ContStackFrame currCont = parent.cont.PopContFrame();
                    if (currCont.returnTolocation == 0)
                        goto Loc_0;
                    else
                        goto Ret;


                    Loc_0:
                    throw new PrtAssertFailureException();

                    Ret:
                    parent.cont.Return(null);

                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    locals.AddRange(args);
                    //no local variables hence nothing to add
                    return locals;
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