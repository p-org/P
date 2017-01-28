#pragma warning disable CS0162, CS0164, CS0168, CS0649
namespace P.Program
{
    using P.Runtime;
    using System;
    using System.Collections.Generic;

    public partial class Application : StateImpl
    {
        private class Events_552749559
        {
            public static PrtEventValue halt;
            public static PrtEventValue @null;
            public static PrtEventValue PingIgnored;
            public static PrtEventValue Success;
            public static PrtEventValue Pong;
            public static PrtEventValue Ping;
            static Events_552749559()
            {
                halt = new PrtEventValue(new PrtEvent("halt", Types_552749559.typeNull, 1, false));
                @null = new PrtEventValue(new PrtEvent("null", Types_552749559.typeNull, 1, false));
                PingIgnored = new PrtEventValue(new PrtEvent("PingIgnored", Types_552749559.typeNull, PrtEvent.DefaultMaxInstances, false));
                Success = new PrtEventValue(new PrtEvent("Success", Types_552749559.typeNull, PrtEvent.DefaultMaxInstances, false));
                Pong = new PrtEventValue(new PrtEvent("Pong", Types_552749559.typeNull, 1, false));
                Ping = new PrtEventValue(new PrtEvent("Ping", Types_552749559.typeMachine, 1, false));
            }
        }

        private class Types_552749559
        {
            public static PrtType typeNull;
            public static PrtNamedTupleType typeNmdTupType_1;
            public static PrtNamedTupleType typeNmdTupType_2;
            public static PrtNamedTupleType typeNmdTupType_3;
            public static PrtNamedTupleType typeNmdTupType_4;
            public static PrtNamedTupleType typeNmdTupType_5;
            public static PrtType typeMachine;
            public static PrtNamedTupleType typeNmdTupType_7;
            public static PrtType typeAny;
            public static PrtType typeInt;
            public static PrtType typeEvent;
            public static PrtType typeBool;
            static Types_552749559()
            {
                Types_552749559.typeNull = new PrtNullType();
                Types_552749559.typeNmdTupType_1 = new PrtNamedTupleType(new object[]{"_payload_2", Types_552749559.typeNull});
                Types_552749559.typeNmdTupType_2 = new PrtNamedTupleType(new object[]{"_payload_0", Types_552749559.typeNull});
                Types_552749559.typeNmdTupType_3 = new PrtNamedTupleType(new object[]{"_payload_skip", Types_552749559.typeNull});
                Types_552749559.typeNmdTupType_4 = new PrtNamedTupleType(new object[]{"_payload_1", Types_552749559.typeNull});
                Types_552749559.typeNmdTupType_5 = new PrtNamedTupleType(new object[]{"_payload_3", Types_552749559.typeNull});
                Types_552749559.typeMachine = new PrtMachineType();
                Types_552749559.typeNmdTupType_7 = new PrtNamedTupleType(new object[]{"payload", Types_552749559.typeMachine});
                Types_552749559.typeAny = new PrtAnyType();
                Types_552749559.typeInt = new PrtIntType();
                Types_552749559.typeEvent = new PrtEventType();
                Types_552749559.typeBool = new PrtBoolType();
            }
        }

        public static PrtImplMachine CreateMachine_Main(StateImpl application, PrtValue payload)
        {
            var machine = new Main(application, PrtImplMachine.DefaultMaxBufferSize, false);
            (application).Trace("<CreateLog> Created Machine Main-{0}", (machine).instanceNumber);
            (((machine).self).permissions).Add(Events_552749559.PingIgnored);
            (((machine).self).permissions).Add(Events_552749559.Success);
            (((machine).self).permissions).Add(Events_552749559.Pong);
            (((machine).self).permissions).Add(Events_552749559.Ping);
            (((machine).self).permissions).Add(Events_552749559.halt);
            ((machine).sends).Add(Events_552749559.halt);
            ((machine).sends).Add(Events_552749559.PingIgnored);
            ((machine).sends).Add(Events_552749559.Success);
            ((machine).sends).Add(Events_552749559.Pong);
            ((machine).sends).Add(Events_552749559.Ping);
            (machine).currentPayload = payload;
            return machine;
        }

        public static PrtImplMachine CreateMachine_PONG(StateImpl application, PrtValue payload)
        {
            var machine = new PONG(application, PrtImplMachine.DefaultMaxBufferSize, false);
            (application).Trace("<CreateLog> Created Machine PONG-{0}", (machine).instanceNumber);
            (((machine).self).permissions).Add(Events_552749559.PingIgnored);
            (((machine).self).permissions).Add(Events_552749559.Success);
            (((machine).self).permissions).Add(Events_552749559.Pong);
            (((machine).self).permissions).Add(Events_552749559.Ping);
            (((machine).self).permissions).Add(Events_552749559.halt);
            ((machine).sends).Add(Events_552749559.Pong);
            ((machine).sends).Add(Events_552749559.Ping);
            ((machine).sends).Add(Events_552749559.halt);
            ((machine).sends).Add(Events_552749559.PingIgnored);
            ((machine).sends).Add(Events_552749559.Success);
            (machine).currentPayload = payload;
            return machine;
        }

        public class Main : PrtImplMachine
        {
            public override PrtState StartState
            {
                get
                {
                    return Main_Ping_Init;
                }
            }

            public PrtValue count
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

            public PrtValue pongId
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
                (fields).Add(PrtValue.PrtMkDefaultValue(Types_552749559.typeInt));
                (fields).Add(PrtValue.PrtMkDefaultValue(Types_552749559.typeMachine));
            }

            public class ignore_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return false;
                    }
                }

                internal class ignore_StackFrame : PrtFunStackFrame
                {
                    public ignore_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public ignore_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    Main parent = (Main)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new ignore_StackFrame(this, locals, retLoc);
                }
            }

            public static ignore_Class ignore;
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
                    Main parent = (Main)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

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
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun1_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun1_Class AnonFun1;
            public class AnonFun2_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun2_StackFrame : PrtFunStackFrame
                {
                    public AnonFun2_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun2_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun2_StackFrame(this, locals, retLoc);
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

                internal class AnonFun3_StackFrame : PrtFunStackFrame
                {
                    public AnonFun3_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun3_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    Main parent = (Main)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun3_1;
                        case 2:
                            goto AnonFun3_2;
                        case 3:
                            goto AnonFun3_3;
                    }

                    (parent).count = (new PrtIntValue(((PrtIntValue)((parent).count)).nt + ((PrtIntValue)(new PrtIntValue(1))).nt)).Clone();
                    if (!((PrtBoolValue)(new PrtBoolValue(((parent).count).Equals(new PrtIntValue(1))))).bl)
                        goto AnonFun3_if_0_else;
                    (((PrtMachineValue)((parent).pongId)).mach).PrtEnqueueEvent((PrtEventValue)(Events_552749559.Ping), parent.self, parent, (PrtMachineValue)((parent).pongId));
                    (parent).PrtFunContSend(this, (currFun).locals, 1);
                    return;
                    AnonFun3_1:
                        ;
                    goto AnonFun3_if_0_end;
                    AnonFun3_if_0_else:
                        ;
                    AnonFun3_if_0_end:
                        ;
                    if (!((PrtBoolValue)(new PrtBoolValue(((parent).count).Equals(new PrtIntValue(2))))).bl)
                        goto AnonFun3_if_1_else;
                    (((PrtMachineValue)((parent).pongId)).mach).PrtEnqueueEvent((PrtEventValue)(Events_552749559.halt), Events_552749559.@null, parent, (PrtMachineValue)((parent).pongId));
                    (parent).PrtFunContSend(this, (currFun).locals, 2);
                    return;
                    AnonFun3_2:
                        ;
                    (((PrtMachineValue)((parent).pongId)).mach).PrtEnqueueEvent((PrtEventValue)(Events_552749559.PingIgnored), Events_552749559.@null, parent, (PrtMachineValue)((parent).pongId));
                    (parent).PrtFunContSend(this, (currFun).locals, 3);
                    return;
                    AnonFun3_3:
                        ;
                    goto AnonFun3_if_1_end;
                    AnonFun3_if_1_else:
                        ;
                    AnonFun3_if_1_end:
                        ;
                    if (!!(Events_552749559.Success).Equals(Events_552749559.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Tst\\\\RegressionTests\\\\Integration\\\\Correct\\\\SEM_TwoMachines_1\\\\EventSentAfterSentHalt.p (31, 4): Raised event must be non-null");
                    (application).Trace("<RaiseLog> Machine Main-{0} raised Event {1}", (parent).instanceNumber, (((PrtEventValue)(Events_552749559.Success)).evt).name);
                    (parent).currentTrigger = Events_552749559.Success;
                    (parent).currentPayload = Events_552749559.@null;
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

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun3_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun3_Class AnonFun3;
            public class AnonFun4_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun4_StackFrame : PrtFunStackFrame
                {
                    public AnonFun4_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun4_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun4_StackFrame(this, locals, retLoc);
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

                internal class AnonFun5_StackFrame : PrtFunStackFrame
                {
                    public AnonFun5_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun5_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun5_1;
                    }

                    (parent).pongId = (application).CreateInterfaceOrMachine((parent).renamedName, "PONG", Events_552749559.@null);
                    (parent).PrtFunContNewMachine(this, (currFun).locals, 1);
                    return;
                    AnonFun5_1:
                        ;
                    if (!!(Events_552749559.Success).Equals(Events_552749559.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Tst\\\\RegressionTests\\\\Integration\\\\Correct\\\\SEM_TwoMachines_1\\\\EventSentAfterSentHalt.p (15, 4): Raised event must be non-null");
                    (application).Trace("<RaiseLog> Machine Main-{0} raised Event {1}", (parent).instanceNumber, (((PrtEventValue)(Events_552749559.Success)).evt).name);
                    (parent).currentTrigger = Events_552749559.Success;
                    (parent).currentPayload = Events_552749559.@null;
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

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun5_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun5_Class AnonFun5;
            public class AnonFun6_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun6_StackFrame : PrtFunStackFrame
                {
                    public AnonFun6_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun6_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun6_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun6_Class AnonFun6;
            public class AnonFun7_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun7_StackFrame : PrtFunStackFrame
                {
                    public AnonFun7_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun7_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun7_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun7_Class AnonFun7;
            public class AnonFun8_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun8_StackFrame : PrtFunStackFrame
                {
                    public AnonFun8_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun8_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun8_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun8_Class AnonFun8;
            public class AnonFun9_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun9_StackFrame : PrtFunStackFrame
                {
                    public AnonFun9_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun9_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun9_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun9_Class AnonFun9;
            public class AnonFun10_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun10_StackFrame : PrtFunStackFrame
                {
                    public AnonFun10_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun10_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun10_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun10_Class AnonFun10;
            public class AnonFun11_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun11_StackFrame : PrtFunStackFrame
                {
                    public AnonFun11_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun11_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun11_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun11_Class AnonFun11;
            public class AnonFun12_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun12_StackFrame : PrtFunStackFrame
                {
                    public AnonFun12_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun12_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun12_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun12_Class AnonFun12;
            public class Main_Ping_WaitPong_Class : PrtState
            {
                public Main_Ping_WaitPong_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Main_Ping_WaitPong_Class Main_Ping_WaitPong;
            public class Main_Ping_SendPing_Class : PrtState
            {
                public Main_Ping_SendPing_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Main_Ping_SendPing_Class Main_Ping_SendPing;
            public class Main_Ping_Init_Class : PrtState
            {
                public Main_Ping_Init_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Main_Ping_Init_Class Main_Ping_Init;
            public class Main_Done_Class : PrtState
            {
                public Main_Done_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Main_Done_Class Main_Done;
            static Main()
            {
                ignore = new ignore_Class();
                AnonFun0 = new AnonFun0_Class();
                AnonFun1 = new AnonFun1_Class();
                AnonFun2 = new AnonFun2_Class();
                AnonFun3 = new AnonFun3_Class();
                AnonFun4 = new AnonFun4_Class();
                AnonFun5 = new AnonFun5_Class();
                AnonFun6 = new AnonFun6_Class();
                AnonFun7 = new AnonFun7_Class();
                AnonFun8 = new AnonFun8_Class();
                AnonFun9 = new AnonFun9_Class();
                AnonFun10 = new AnonFun10_Class();
                AnonFun11 = new AnonFun11_Class();
                AnonFun12 = new AnonFun12_Class();
                Main_Ping_WaitPong = new Main_Ping_WaitPong_Class("Main_Ping_WaitPong", AnonFun1, AnonFun2, false, StateTemperature.Warm);
                Main_Ping_SendPing = new Main_Ping_SendPing_Class("Main_Ping_SendPing", AnonFun3, AnonFun4, false, StateTemperature.Warm);
                Main_Ping_Init = new Main_Ping_Init_Class("Main_Ping_Init", AnonFun5, AnonFun6, false, StateTemperature.Warm);
                Main_Done = new Main_Done_Class("Main_Done", AnonFun0, AnonFun12, false, StateTemperature.Warm);
                PrtTransition transition_1 = new PrtTransition(AnonFun7, Main_Ping_SendPing, false);
                Main_Ping_WaitPong.transitions.Add(Events_552749559.Pong, transition_1);
                PrtTransition transition_2 = new PrtTransition(AnonFun8, Main_Ping_WaitPong, false);
                Main_Ping_SendPing.transitions.Add(Events_552749559.Success, transition_2);
                PrtTransition transition_3 = new PrtTransition(AnonFun9, Main_Ping_SendPing, false);
                Main_Ping_Init.transitions.Add(Events_552749559.Success, transition_3);
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

            public class ignore_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return false;
                    }
                }

                internal class ignore_StackFrame : PrtFunStackFrame
                {
                    public ignore_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public ignore_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    PONG parent = (PONG)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new ignore_StackFrame(this, locals, retLoc);
                }
            }

            public static ignore_Class ignore;
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
                    PONG parent = (PONG)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

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

                    public PrtValue _payload_2
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
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun1_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun1_Class AnonFun1;
            public class AnonFun2_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun2_StackFrame : PrtFunStackFrame
                {
                    public AnonFun2_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun2_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun2_StackFrame(this, locals, retLoc);
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

                internal class AnonFun3_StackFrame : PrtFunStackFrame
                {
                    public AnonFun3_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun3_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_3
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
                    PrtValue swap;
                    if (!((PrtBoolValue)(new PrtBoolValue(false))).bl)
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Tst\\\\RegressionTests\\\\Integration\\\\Correct\\\\SEM_TwoMachines_1\\\\EventSentAfterSentHalt.p (48, 23): Assert failed");
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun3_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun3_Class AnonFun3;
            public class AnonFun4_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun4_StackFrame : PrtFunStackFrame
                {
                    public AnonFun4_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun4_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun4_StackFrame(this, locals, retLoc);
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

                internal class AnonFun5_StackFrame : PrtFunStackFrame
                {
                    public AnonFun5_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun5_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun5_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun5_Class AnonFun5;
            public class AnonFun6_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun6_StackFrame : PrtFunStackFrame
                {
                    public AnonFun6_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun6_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue payload
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
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun6_1;
                    }

                    (((PrtMachineValue)((currFun).locals[0])).mach).PrtEnqueueEvent((PrtEventValue)(Events_552749559.Pong), Events_552749559.@null, parent, (PrtMachineValue)((currFun).locals[0]));
                    (parent).PrtFunContSend(this, (currFun).locals, 1);
                    return;
                    AnonFun6_1:
                        ;
                    if (!!(Events_552749559.Success).Equals(Events_552749559.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Tst\\\\RegressionTests\\\\Integration\\\\Correct\\\\SEM_TwoMachines_1\\\\EventSentAfterSentHalt.p (54, 7): Raised event must be non-null");
                    (application).Trace("<RaiseLog> Machine PONG-{0} raised Event {1}", (parent).instanceNumber, (((PrtEventValue)(Events_552749559.Success)).evt).name);
                    (parent).currentTrigger = Events_552749559.Success;
                    (parent).currentPayload = Events_552749559.@null;
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

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun6_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun6_Class AnonFun6;
            public class AnonFun7_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun7_StackFrame : PrtFunStackFrame
                {
                    public AnonFun7_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun7_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun7_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun7_Class AnonFun7;
            public class AnonFun8_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun8_StackFrame : PrtFunStackFrame
                {
                    public AnonFun8_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun8_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun8_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun8_Class AnonFun8;
            public class PONG_Pong_WaitPing_Class : PrtState
            {
                public PONG_Pong_WaitPing_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static PONG_Pong_WaitPing_Class PONG_Pong_WaitPing;
            public class PONG_Pong_SendPong_Class : PrtState
            {
                public PONG_Pong_SendPong_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static PONG_Pong_SendPong_Class PONG_Pong_SendPong;
            static PONG()
            {
                ignore = new ignore_Class();
                AnonFun0 = new AnonFun0_Class();
                AnonFun1 = new AnonFun1_Class();
                AnonFun2 = new AnonFun2_Class();
                AnonFun3 = new AnonFun3_Class();
                AnonFun4 = new AnonFun4_Class();
                AnonFun5 = new AnonFun5_Class();
                AnonFun6 = new AnonFun6_Class();
                AnonFun7 = new AnonFun7_Class();
                AnonFun8 = new AnonFun8_Class();
                PONG_Pong_WaitPing = new PONG_Pong_WaitPing_Class("PONG_Pong_WaitPing", AnonFun1, AnonFun2, false, StateTemperature.Warm);
                PONG_Pong_SendPong = new PONG_Pong_SendPong_Class("PONG_Pong_SendPong", AnonFun6, AnonFun7, false, StateTemperature.Warm);
                PONG_Pong_WaitPing.dos.Add(Events_552749559.PingIgnored, AnonFun3);
                PrtTransition transition_1 = new PrtTransition(AnonFun0, PONG_Pong_SendPong, false);
                PONG_Pong_WaitPing.transitions.Add(Events_552749559.Ping, transition_1);
                PrtTransition transition_2 = new PrtTransition(AnonFun8, PONG_Pong_WaitPing, false);
                PONG_Pong_SendPong.transitions.Add(Events_552749559.Success, transition_2);
            }
        }
    }
}
