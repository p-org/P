using P.Runtime;
using System.Collections.Generic;
//Manually generated new compiler output for a simplest P program:
//machine Main {
//	start state Init {
//	}
//}
namespace DummyMachine
{
    public class Application : StateImpl
    {
        #region Constructors
        public Application() : base()
        {
            //initialize all fields
        }
        //(TODO)template code: move to PStateImpl?
        public override StateImpl MakeSkeleton()
        {
            return new Application();
        }
        //(TODO)template code: move to PStateImpl?
        public Application(bool initialize) : base()
        {
            //create the main machine
            CreateMainMachine();
        }
        #endregion
        #region Events
        //no events in this test
        public static PrtEventValue dummy = new PrtEventValue(new PrtEvent("dummy", new PrtNullType(), PrtEvent.DefaultMaxInstances, false));
        #endregion
        //(TODO)template code: move to StateImpl?
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
            public Main() : base()
            {

            }
            //constructor
            public Main(StateImpl app, int maxB) : base(app, maxB)
            {
                // initialize fields
            }
            //getters and setters


            #region Functions
            //Anon functions for entry functions of each state of Main:
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
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }
                    //no local variables hence nothing to add
                    return locals;
                }
            }

            public static Anon_0 Anon_0_Fun;
            #endregion

            #region States
            //For each state of Main:
            public class Init : PrtState
            {
                public Init(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature)
                    : base(name, entryFun, exitFun, hasNullTransition, temperature)
                {

                }
            }

            public static Init Init_State;
            #endregion

            static Main()
            {
                //initialize functions
                Anon_0_Fun = new Anon_0();

                //initialize states 
                Init_State = new Init("Init", Anon_0_Fun, PrtCommonFunctions.SkipFun, false, StateTemperature.Warm);

                //create transitions and add them to the state
                //PrtTransition transition_1 = new PrtTransition(PrtCommonFunctions.SkipFun, Fail_State, false);

                //add transition
                //Init_State.transitions.Add(dummy, transition_1);

            }
        }

    }
}
        /*
        //old code start++++++++++++++++++++++++++++++++++++++++++++++:
        //similar field for each Machine and Monitor:
        List<Main> MainMachines;

        //TODO: implemented differently in PingPong.cs
        public override IEnumerable<BaseMachine> AllAliveMachines
        {
            get
            {
                //generate such loop for each Machine:
                foreach (var m in MainMachines)
                {
                    yield return m;
                }
                yield break;
            }
        }
        //TODO: implemented differently in PingPong.cs
        public override IEnumerable<BaseMonitor> AllInstalledMonitors
        {
            get
            {
                //similar (as in AllMachines) foreach loop for each Monitor
                yield break;
            }
        }
        #region Constructors
        public Application() : base()
        {
            //initialize all fields
        }
        public Application(bool initialize) : base(initialize)
        {
            //initialize all fields
        }
        #endregion
        //similar class Machine for each Machine and
        //class Monitor for each Monitor
        public class Main : Machine<Main>
        {
            public Main(int instance) : base(instance, 0, -1)
            {

            }
            //similar class for each function local in Main machine:
            internal class AnonFun0_Fun : Fun<Main>
            {
                //no body for now:
                public void Execute() { }
                //no body for now:
                public void PushFrame() { }
            }
            //declare similar static field for each function local in Main machine:
            public static AnonFun0_Fun AnonFun0 = new AnonFun0_Fun();
            //declare similar static field for each state of the Main machine:
            public static State Init = new State();
        }
        //declare static class (similar to AnonFun0_Fun) and  
        //static field for each static function:
        static class StaticFun : StaticFun<Main>
        {
            //no body for now:
            public void Execute() { }
            //no body for now:
            public void PushFrame() { }
        }
        //declare static field for each event:
        public static Event NullEvent = new Event();
        public static Event HaltEvent = new Event();
    }
    //old code end++++++++++++++++++++++++++++++++++++++++++++++:
} */