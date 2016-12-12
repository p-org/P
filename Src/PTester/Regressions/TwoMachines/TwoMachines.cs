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

        public static PrtEventValue halt;
        public static PrtEventValue @null;
        public static PrtEventValue intPayloadEvent;
        public static PrtEventValue boolPayloadEvent;
        public static PrtEventValue Ping;
        public static PrtType typeNull;
        public static PrtNamedTupleType typeNmdTupType_1;
        public static PrtNamedTupleType typeNmdTupType_2;
        public static PrtType typeAny;
        public static PrtType typeMachine;
        public static PrtType typeEvent;
        public static PrtType typeBool;
        public static PrtType typeInt;
        static Application()
        {
            typeNull = new PrtNullType();
            typeNmdTupType_1 = new PrtNamedTupleType(new object[]{"_payload_0", typeNull});
            typeNmdTupType_2 = new PrtNamedTupleType(new object[]{"_payload_skip", typeNull});
            typeAny = new PrtAnyType();
            typeMachine = new PrtMachineType();
            typeEvent = new PrtEventType();
            typeBool = new PrtBoolType();
            typeInt = new PrtIntType();
            halt = new PrtEventValue(new PrtEvent("halt", typeNull, 1, false));
            @null = new PrtEventValue(new PrtEvent("null", typeNull, 1, false));
            intPayloadEvent = new PrtEventValue(new PrtEvent("intPayloadEvent", typeInt, PrtEvent.DefaultMaxInstances, false));
            boolPayloadEvent = new PrtEventValue(new PrtEvent("boolPayloadEvent", typeBool, PrtEvent.DefaultMaxInstances, false));
            Ping = new PrtEventValue(new PrtEvent("Ping", typeMachine, 1, false));
        }

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

            public PrtValue varBool
            {
                get
                {
                    return fields[0];
                }

                set
                {
                    fields[0] = value;
                }
            }

            public PrtValue varInt
            {
                get
                {
                    return fields[1];
                }

                set
                {
                    fields[1] = value;
                }
            }

            public PrtValue pongId
            {
                get
                {
                    return fields[2];
                }

                set
                {
                    fields[2] = value;
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

            public class AnonFun0_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun0_StackFrame : PrtFunStackFrame
                {
                    public AnonFun0_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun0_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_0
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    Main parent = (Main)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    switch (currFun.returnToLocation)
                    {
                    }

                    (((PrtMachineValue)(new PrtMachineValue((PrtImplMachine)(parent)))).mach).PrtEnqueueEvent((PrtEventValue)(boolPayloadEvent), new PrtBoolValue(true), parent);
                    parent.PrtFunContSend(this, currFun.locals, currFun.returnToLocation);
                    AnonFun0_0:
                        ;
                    parent.varBool = new PrtBoolValue(false);
                    (((PrtMachineValue)(new PrtMachineValue((PrtImplMachine)(parent)))).mach).PrtEnqueueEvent((PrtEventValue)(boolPayloadEvent), parent.varBool, parent);
                    parent.PrtFunContSend(this, currFun.locals, currFun.returnToLocation);
                    AnonFun0_1:
                        ;
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
                    return new AnonFun0_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun0_Class AnonFun0;
            public class AnonFun1_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun1_StackFrame : PrtFunStackFrame
                {
                    public AnonFun1_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun1_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    Main parent = (Main)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    switch (currFun.returnToLocation)
                    {
                    }
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
                    return new AnonFun1_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun1_Class AnonFun1;
            public class Main_Init_Class : PrtState
            {
                public Main_Init_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Main_Init_Class Main_Init;
            static Main()
            {
                AnonFun0 = new AnonFun0_Class();
                AnonFun1 = new AnonFun1_Class();
                Main_Init = new Main_Init_Class("Main_Init", AnonFun0, AnonFun1, false, StateTemperature.Hot);
            }
        }
    }
}
