using P.Runtime;
using System.Collections.Generic;

namespace Dummy
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
        public static PrtEventValue Dummy1 = new PrtEventValue(new PrtEvent("Dummy1", new PrtIntType(), 1, false));
        public static PrtEventValue Dummy2 = new PrtEventValue(new PrtEvent("Dummy2", new PrtBoolType(), 2, true));
        public static PrtEventValue Dummy = new PrtEventValue(new PrtEvent("Dummy", new PrtNullType(), PrtEvent.DefaultMaxInstances, false));
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
                    return Main_Init;
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
                    //throw new NotImplementedException();
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
            public class Main_Init_Class : PrtState
            {
                public Main_Init_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Main_Init_Class Main_Init;
            static Main()
            {
                AnonFun1 = new AnonFun1_Class();
                AnonFun0 = new AnonFun0_Class();
                Main_Init = new Main_Init_Class("Main_Init", AnonFun1, AnonFun0, false, StateTemperature.Warm);
            }
        }
    }
}
