using P.Runtime;
using System.Collections.Generic;

namespace MyPProgramGenerated
{
    public class Application : StateImpl
    {
        public Application(): base ()
        {
        }

        public Application(bool initialize): base ()
        {
            CreateMainMachine();
        }

        public override StateImpl MakeSkeleton()
        {
            return new Application();
        }

        public static PrtEventValue halt = new PrtEventValue(new PrtEvent("halt", new PrtNullType(), 1, false));
        public static PrtEventValue @null = new PrtEventValue(new PrtEvent("null", new PrtNullType(), 1, false));
        public static PrtEventValue dummy = new PrtEventValue(new PrtEvent("dummy", new PrtNullType(), PrtEvent.DefaultMaxInstances, false));
        public PrtImplMachine CreateMainMachine()
        {
            var mainMachine = new Main(this, 10, false);
            AddImplMachineToStateImpl(mainMachine);
            return mainMachine;
        }

        public class Main : PrtImplMachine
        {
            public override PrtState StartState
            {
                get
                {
                    return Main_InitPingPong;
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

            public Main(): base ()
            {
            }

            public Main(StateImpl app, int maxB, bool assume): base (app, maxB, assume)
            {
            }

            public class AnonFun1_Class : PrtFun
            {
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

            public static AnonFun1_Class AnonFun1;
            public class AnonFun0_Class : PrtFun
            {
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

            public static AnonFun0_Class AnonFun0;
            public class foo_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return false;
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

            public static foo_Class foo;
            public class bar_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return false;
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

            public static bar_Class bar;
            public class AnonFun2_Class : PrtFun
            {
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

            public static AnonFun2_Class AnonFun2;
            public class AnonFun3_Class : PrtFun
            {
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

            public static AnonFun3_Class AnonFun3;
            public class Main_InitPingPong_Class : PrtState
            {
                public Main_InitPingPong_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Main_InitPingPong_Class Main_InitPingPong;
            public class Main_Fail_Class : PrtState
            {
                public Main_Fail_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Main_Fail_Class Main_Fail;
            public class Main_Success_Class : PrtState
            {
                public Main_Success_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Main_Success_Class Main_Success;
            public class Main_NewState_Class : PrtState
            {
                public Main_NewState_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Main_NewState_Class Main_NewState;
            static Main()
            {
                AnonFun1 = new AnonFun1_Class();
                AnonFun0 = new AnonFun0_Class();
                foo = new foo_Class();
                bar = new bar_Class();
                AnonFun2 = new AnonFun2_Class();
                AnonFun3 = new AnonFun3_Class();
                Main_InitPingPong = new Main_InitPingPong_Class("Main_InitPingPong", AnonFun1, AnonFun0, false, StateTemperature.Warm);
                Main_Fail = new Main_Fail_Class("Main_Fail", foo, bar, false, StateTemperature.Warm);
                Main_Success = new Main_Success_Class("Main_Success", AnonFun2, foo, false, StateTemperature.Warm);
                Main_NewState = new Main_NewState_Class("Main_NewState", AnonFun3, AnonFun0, false, StateTemperature.Warm);
            }
        }
    }
}
