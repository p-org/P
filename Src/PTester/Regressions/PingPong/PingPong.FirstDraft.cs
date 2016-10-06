using P.Runtime;
using System.Collections.Generic;
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
    public class Application : StateImpl
    {
        #region Constructors
        public Application() :base()
        {
            //initialize all the fields
        }

        public override StateImpl MakeSkeleton()
        {
            return new Application();
        }

        public Application(bool initialize) : base()
        {
            //create the main machine
            CreateMainMachine();
        }
        #endregion

        public static PrtEventValue dummy = new PrtEventValue(new PrtEvent("dummy", new PrtNullType(), PrtEvent.DefaultMaxInstances, false));

        public PrtImplMachine CreateMainMachine()
        {
            var mainMachine = new Main(this, 10);
            AddImplMachineToStateImpl(mainMachine);
            return mainMachine;
        }

        public class Main : PrtImplMachine
        {
            public override PrtState StartState
            {
                get
                {
                    return Init_State;
                }
            }

            public override PrtImplMachine MakeSkeleton()
            {
                return new Main();
            }
            public override int NextInstanceNumber(StateImpl app)
            {
                return app.NextMachineInstanceNumber(this.GetType());
            }

            public override string Name
            {
                get
                {
                    return "Main";
                }
            }

            //constructor called for cloning
            public Main(): base()
            {

            }
            //constructor
            public Main(StateImpl app, int maxB) : base(app, maxB)
            {
                // initialize fields
            }
            //getters and setters


            #region Functions
            public class Anon_0 : PrtFun
            {
                public override string Name
                {
                    get
                    {
                        return "Init_Entry";
                    }
                }

                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }
                public override void Execute(StateImpl application, PrtMachine parent)
                {
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    if (currFun.returnTolocation == 0)
                        goto Loc_0;
                    else
                        goto Ret;


                    Loc_0:
                    parent.PrtEnqueueEvent(dummy, PrtValue.NullValue, parent);
                    parent.PrtFunContSend(this, currFun.locals, 1);

                    Ret:
                    parent.PrtFunContReturn(null);
                   
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach(var item in args)
                    {
                        locals.Add(item.Clone());
                    }
                    //no local variables hence nothing to add
                    return locals;
                }
            }

            public class Anon_1 : PrtFun
            {
                public override string Name
                {
                    get
                    {
                        return "Fail_Entry";
                    }
                }

                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }
                public override void Execute(StateImpl application, PrtMachine parent)
                {
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    if (currFun.returnTolocation == 0)
                        goto Loc_0;
                    else
                        goto Ret;


                    Loc_0:
                    throw new PrtAssertFailureException();

                    Ret:
                    parent.PrtFunContReturn(null);

                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }
                    //no local variables hence nothing to add
                    return locals;
                }
            }

            public static Anon_0 Anon_0_Fun;

            public static Anon_1 Anon_1_Fun;
            #endregion

            #region States
            public class Init : PrtState
            {
                public Init(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature) 
                    :base (name, entryFun, exitFun, hasNullTransition, temperature)
                {

                }
            }

            public class Fail : PrtState
            {
                public Fail(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature) 
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
                Init_State = new Init("Init", Anon_0_Fun, PrtCommonFunctions.SkipFun, false, StateTemperature.Warm);
                Fail_State = new Fail("Fail", Anon_1_Fun, PrtCommonFunctions.SkipFun, false, StateTemperature.Warm);

                //create transition and add them to the state
                PrtTransition transition_1 = new PrtTransition(PrtCommonFunctions.SkipFun, Fail_State, false);

                //add transition
                Init_State.transitions.Add(dummy, transition_1);

            }

            
        }

    }

    
} 