using P.Runtime;
using System;
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
            CreateMachine_Blah(this);
            CreateMachine_Main(this, PrtValue.NullValue);
        }

        public override StateImpl MakeSkeleton()
        {
            return new Application();
        }

        public static PrtEventValue halt;
        public static PrtEventValue @null;
        public static PrtEventValue boolPayloadEvent;
        public static PrtEventValue Success;
        public static PrtEventValue intPayloadEvent;
        public static PrtEventValue Ping;
        public static PrtType typeNull;
        public static PrtNamedTupleType typeNmdTupType_1;
        public static PrtNamedTupleType typeNmdTupType_2;
        public static PrtNamedTupleType typeNmdTupType_3;
        public static PrtType typeAny;
        public static PrtType typeEvent;
        public static PrtType typeMachine;
        public static PrtType typeBool;
        public static PrtType typeInt;
        static Application()
        {
            typeNull = new PrtNullType();
            typeNmdTupType_1 = new PrtNamedTupleType(new object[]{"_payload_1", typeNull});
            typeNmdTupType_2 = new PrtNamedTupleType(new object[]{"_payload_0", typeNull});
            typeNmdTupType_3 = new PrtNamedTupleType(new object[]{"_payload_skip", typeNull});
            typeAny = new PrtAnyType();
            typeEvent = new PrtEventType();
            typeMachine = new PrtMachineType();
            typeBool = new PrtBoolType();
            typeInt = new PrtIntType();
            halt = new PrtEventValue(new PrtEvent("halt", typeNull, 1, false));
            @null = new PrtEventValue(new PrtEvent("null", typeNull, 1, false));
            boolPayloadEvent = new PrtEventValue(new PrtEvent("boolPayloadEvent", typeBool, PrtEvent.DefaultMaxInstances, false));
            Success = new PrtEventValue(new PrtEvent("Success", typeNull, PrtEvent.DefaultMaxInstances, false));
            intPayloadEvent = new PrtEventValue(new PrtEvent("intPayloadEvent", typeInt, PrtEvent.DefaultMaxInstances, false));
            Ping = new PrtEventValue(new PrtEvent("Ping", typeMachine, 1, false));
        }

        public static void CreateMachine_Blah(StateImpl application)
        {
            var machine = new Blah(application);
            (application).AddSpecMachineToStateImpl(machine);
        }

        public static PrtImplMachine CreateMachine_PONG(StateImpl application, PrtValue payload)
        {
            var machine = new PONG(application, 111, true);
            (machine).currentPayload = payload;
            (application).AddImplMachineToStateImpl(machine);
            return machine;
        }

        public static PrtImplMachine CreateMachine_Main(StateImpl application, PrtValue payload)
        {
            var machine = new Main(application, 10, false);
            (machine).currentPayload = payload;
            (application).AddImplMachineToStateImpl(machine);
            return machine;
        }

        public class Blah : PrtSpecMachine
        {
            public override PrtState StartState
            {
                get
                {
                    return Blah_Init;
                }
            }

            public override PrtSpecMachine MakeSkeleton()
            {
                return new Blah();
            }

            public override string Name
            {
                get
                {
                    return "Blah";
                }
            }

            public Blah(): base ()
            {
            }

            public Blah(StateImpl app): base (app)
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
                    Blah parent = (Blah)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtImplMachine createdMachine;
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
                    return new AnonFun1_StackFrame(this, locals, retLoc);
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
                    Blah parent = (Blah)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtImplMachine createdMachine;
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
                    return new AnonFun0_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun0_Class AnonFun0;
            public class Blah_Init_Class : PrtState
            {
                public Blah_Init_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Blah_Init_Class Blah_Init;
            static Blah()
            {
                AnonFun1 = new AnonFun1_Class();
                AnonFun0 = new AnonFun0_Class();
                Blah_Init = new Blah_Init_Class("Blah_Init", AnonFun1, AnonFun0, false, StateTemperature.Warm);
            }
        }

        public class PONG : PrtImplMachine
        {
            public override PrtState StartState
            {
                get
                {
                    return PONG_Pong_WaitPing;
                }
            }

            public override PrtImplMachine MakeSkeleton()
            {
                return new PONG();
            }

            public override int NextInstanceNumber(StateImpl app)
            {
                return app.NextMachineInstanceNumber(this.GetType());
            }

            public override string Name
            {
                get
                {
                    return "PONG";
                }
            }

            public PONG(): base ()
            {
            }

            public PONG(StateImpl app, int maxB, bool assume): base (app, maxB, assume)
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

                    public PrtValue _payload_1
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
                    PONG parent = (PONG)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtImplMachine createdMachine;
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
                    PONG parent = (PONG)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtImplMachine createdMachine;
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
                    return new AnonFun1_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun1_Class AnonFun1;
            public class PONG_Pong_WaitPing_Class : PrtState
            {
                public PONG_Pong_WaitPing_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static PONG_Pong_WaitPing_Class PONG_Pong_WaitPing;
            static PONG()
            {
                AnonFun0 = new AnonFun0_Class();
                AnonFun1 = new AnonFun1_Class();
                PONG_Pong_WaitPing = new PONG_Pong_WaitPing_Class("PONG_Pong_WaitPing", AnonFun0, AnonFun1, false, StateTemperature.Warm);
            }
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
                    PrtImplMachine createdMachine;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun0_1;
                        case 2:
                            goto AnonFun0_2;
                        case 3:
                            goto AnonFun0_3;
                    }

                    (((PrtMachineValue)(new PrtMachineValue((PrtImplMachine)(parent)))).mach).PrtEnqueueEvent((PrtEventValue)(boolPayloadEvent), new PrtBoolValue(true), parent);
                    (parent).PrtFunContSend(this, (currFun).locals, (currFun).returnToLocation);
                    return;
                    AnonFun0_1:
                        ;
                    (parent).varBool = new PrtBoolValue(false);
                    (((PrtMachineValue)(new PrtMachineValue((PrtImplMachine)(parent)))).mach).PrtEnqueueEvent((PrtEventValue)(boolPayloadEvent), (parent).varBool, parent);
                    (parent).PrtFunContSend(this, (currFun).locals, (currFun).returnToLocation);
                    return;
                    AnonFun0_2:
                        ;
                    createdMachine = CreateMachine_PONG(application, PrtValue.NullValue);
                    (parent).PrtFunContNewMachine(this, (currFun).locals, createdMachine, 3);
                    return;
                    AnonFun0_3:
                        ;
                    createdMachine = ((parent).continuation).createdMachine;
                    ((parent).continuation).createdMachine = null;
                    (parent).pongId = new PrtMachineValue(createdMachine);
                    if (!!(Success).Equals(PrtValue.NullValue))
                        throw new PrtAssertFailureException("C:\\\\Users\\\\qadeer\\\\Work\\\\P\\\\Src\\\\PTester\\\\Regressions\\\\TwoMachines\\\\TwoMachines.p (59, 10): Raised event must be non-null");
                    Console.Write("<RaiseLog> Machine Main-{0} raised Event {1}\\n", (parent).instanceNumber, ((Success).evt).name);
                    (parent).currentTrigger = Success;
                    (parent).currentPayload = PrtValue.NullValue;
                    (parent).PrtFunContRaise();
                    return;
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
                    PrtImplMachine createdMachine;
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
