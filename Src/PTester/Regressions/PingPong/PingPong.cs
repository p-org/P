using P.Runtime;
using System.Collections.Generic;

namespace P.Program
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
        public static PrtEventValue E = new PrtEventValue(new PrtEvent("E", new PrtNullType(), PrtEvent.DefaultMaxInstances, false));
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
                internal class AnonFun1_Class_StackFrame : PrtFunStackFrame
                {
                    public AnonFun1_Class_StackFrame(PrtFun fun, List<PrtValue> locs) : base(fun, locs)
                    {

                    }
                    public AnonFun1_Class_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation)
                        : base(fun, locs, retLocation)
                    {

                    }
                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
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
                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun1_Class_StackFrame(this, locals, retLoc);
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
                internal class AnonFun0_Class_StackFrame : PrtFunStackFrame
                {
                    public AnonFun0_Class_StackFrame(PrtFun fun, List<PrtValue> locs) : base(fun, locs)
                    {

                    }
                    public AnonFun0_Class_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation)
                        : base(fun, locs, retLocation)
                    {

                    }
                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
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
                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    //throw new NotImplementedException();
                    return new AnonFun0_Class_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun0_Class AnonFun0;
            public class AnonFun2_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }
                internal class AnonFun2_Class_StackFrame : PrtFunStackFrame
                {
                    public AnonFun2_Class_StackFrame(PrtFun fun, List<PrtValue> locs) : base(fun, locs)
                    {

                    }
                    public AnonFun2_Class_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation)
                        : base(fun, locs, retLocation)
                    {

                    }
                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
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
                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    //throw new NotImplementedException();
                    return new AnonFun2_Class_StackFrame(this, locals, retLoc);
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
                internal class AnonFun3_Class_StackFrame : PrtFunStackFrame
                {
                    public AnonFun3_Class_StackFrame(PrtFun fun, List<PrtValue> locs) : base(fun, locs)
                    {

                    }
                    public AnonFun3_Class_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation)
                        : base(fun, locs, retLocation)
                    {

                    }
                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
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
                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    //throw new NotImplementedException();
                    return new AnonFun3_Class_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun3_Class AnonFun3;
            public class Action2_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return false;
                    }
                }
                internal class Action2_Class_StackFrame : PrtFunStackFrame
                {
                    public Action2_Class_StackFrame(PrtFun fun, List<PrtValue> locs) : base(fun, locs)
                    {

                    }
                    public Action2_Class_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation)
                        : base(fun, locs, retLocation)
                    {

                    }
                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
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
                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    //throw new NotImplementedException();
                    return new Action2_Class_StackFrame(this, locals, retLoc);
                }
            }

            public static Action2_Class Action2;
            public class AnonFun4_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }
                internal class AnonFun4_Class_StackFrame : PrtFunStackFrame
                {
                    public AnonFun4_Class_StackFrame(PrtFun fun, List<PrtValue> locs) : base(fun, locs)
                    {

                    }
                    public AnonFun4_Class_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation)
                        : base(fun, locs, retLocation)
                    {

                    }
                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
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
                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    //throw new NotImplementedException();
                    return new AnonFun4_Class_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun4_Class AnonFun4;
            public class AnonFun5_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }
                internal class AnonFun5_Class_StackFrame : PrtFunStackFrame
                {
                    public AnonFun5_Class_StackFrame(PrtFun fun, List<PrtValue> locs) : base(fun, locs)
                    {

                    }
                    public AnonFun5_Class_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation)
                        : base(fun, locs, retLocation)
                    {

                    }
                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
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
                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    //throw new NotImplementedException();
                    return new AnonFun5_Class_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun5_Class AnonFun5;
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
            static Main()
            {
                AnonFun1 = new AnonFun1_Class();
                AnonFun0 = new AnonFun0_Class();
                AnonFun2 = new AnonFun2_Class();
                AnonFun3 = new AnonFun3_Class();
                Action2 = new Action2_Class();
                AnonFun4 = new AnonFun4_Class();
                AnonFun5 = new AnonFun5_Class();
                Main_InitPingPong = new Main_InitPingPong_Class("Main_InitPingPong", AnonFun1, AnonFun0, false, StateTemperature.Warm);
                PrtTransition transition_1 = new PrtTransition(AnonFun2, Main_Fail, false);
                Main_InitPingPong.transitions.Add(dummy, transition_1);
                PrtTransition transition_2 = new PrtTransition(PrtCommonFunctions.SkipFun, Main_Success, true);
                Main_InitPingPong.transitions.Add(E, transition_2);
                Main_Fail = new Main_Fail_Class("Main_Fail", AnonFun3, AnonFun0, false, StateTemperature.Warm);
                PrtTransition transition_3 = new PrtTransition(Action2, Main_InitPingPong, false);
                Main_Fail.transitions.Add(dummy, transition_3);
                PrtTransition transition_4 = new PrtTransition(AnonFun4, Main_Success, false);
                Main_Fail.transitions.Add(E, transition_4);
                Main_Success = new Main_Success_Class("Main_Success", AnonFun0, AnonFun0, false, StateTemperature.Warm);
                PrtTransition transition_5 = new PrtTransition(AnonFun5, Main_InitPingPong, false);
                Main_Success.transitions.Add(dummy, transition_5);
                PrtTransition transition_6 = new PrtTransition(PrtCommonFunctions.SkipFun, Main_Fail, true);
                Main_Success.transitions.Add(E, transition_6);
            }
        }
    }
}
