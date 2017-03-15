#pragma warning disable CS0162, CS0164, CS0168, CS0649
namespace P.Program
{
    using P.Runtime;
    using System;
    using System.Collections.Generic;

    public partial class Application : StateImpl
    {
        public partial class Events
        {
            public static PrtEventValue PING;
            public static PrtEventValue PONG;
            public static PrtEventValue ROUND_DONE;
            public static PrtEventValue REGISTER_CLIENT;
            public static PrtEventValue UNREGISTER_CLIENT;
            public static PrtEventValue NODE_DOWN;
            public static PrtEventValue TIMER_CANCELED;
            public static PrtEventValue M_PING;
            public static PrtEventValue M_PONG;
            public static PrtEventValue M_START;
            public static void Events_PingPongBugRepro()
            {
                PING = new PrtEventValue(new PrtEvent("PING", Types.type_1_205668158, PrtEvent.DefaultMaxInstances, false));
                PONG = new PrtEventValue(new PrtEvent("PONG", Types.type_1_205668158, PrtEvent.DefaultMaxInstances, false));
                ROUND_DONE = new PrtEventValue(new PrtEvent("ROUND_DONE", Types.type_8_205668158, PrtEvent.DefaultMaxInstances, false));
                REGISTER_CLIENT = new PrtEventValue(new PrtEvent("REGISTER_CLIENT", Types.type_1_205668158, PrtEvent.DefaultMaxInstances, false));
                UNREGISTER_CLIENT = new PrtEventValue(new PrtEvent("UNREGISTER_CLIENT", Types.type_1_205668158, PrtEvent.DefaultMaxInstances, false));
                NODE_DOWN = new PrtEventValue(new PrtEvent("NODE_DOWN", Types.type_1_205668158, PrtEvent.DefaultMaxInstances, false));
                TIMER_CANCELED = new PrtEventValue(new PrtEvent("TIMER_CANCELED", Types.type_8_205668158, PrtEvent.DefaultMaxInstances, false));
                M_PING = new PrtEventValue(new PrtEvent("M_PING", Types.type_1_205668158, PrtEvent.DefaultMaxInstances, false));
                M_PONG = new PrtEventValue(new PrtEvent("M_PONG", Types.type_1_205668158, PrtEvent.DefaultMaxInstances, false));
                M_START = new PrtEventValue(new PrtEvent("M_START", Types.type_0_205668158, PrtEvent.DefaultMaxInstances, false));
            }
        }

        public partial class Types
        {
            public static PrtType type_1_205668158;
            public static PrtType type_2_205668158;
            public static PrtType type_0_205668158;
            public static PrtType type_4_205668158;
            public static PrtType type_3_205668158;
            public static PrtType type_5_205668158;
            public static PrtType type_6_205668158;
            public static PrtType type_7_205668158;
            public static PrtType type_8_205668158;
            public static PrtType type_9_205668158;
            static public void Types_PingPongBugRepro()
            {
                Types.type_1_205668158 = new PrtMachineType();
                Types.type_2_205668158 = new PrtBoolType();
                Types.type_0_205668158 = new PrtMapType(Types.type_1_205668158, Types.type_2_205668158);
                Types.type_4_205668158 = new PrtIntType();
                Types.type_3_205668158 = new PrtMapType(Types.type_1_205668158, Types.type_4_205668158);
                Types.type_5_205668158 = new PrtSeqType(Types.type_1_205668158);
                Types.type_6_205668158 = new PrtAnyType();
                Types.type_7_205668158 = new PrtEventType();
                Types.type_8_205668158 = new PrtNullType();
                Types.type_9_205668158 = new PrtTupleType(new PrtType[]{Types.type_4_205668158, Types.type_1_205668158});
            }
        }

        public static PrtSpecMachine CreateSpecMachine_Liveness(StateImpl application)
        {
            var machine = new Liveness(application);
            (application).Trace("<CreateLog> Created spec Machine Liveness");
            ((machine).observes).Add(Events.NODE_DOWN);
            ((machine).observes).Add(Events.M_START);
            return machine;
        }

        public static PrtImplMachine CreateMachine_FailureDetector(StateImpl application, PrtValue payload)
        {
            var machine = new FailureDetector(application, PrtImplMachine.DefaultMaxBufferSize, false);
            (application).Trace("<CreateLog> Created Machine FailureDetector-{0}", (machine).instanceNumber);
            (((machine).self).permissions).Add(Events.TIMEOUT);
            (((machine).self).permissions).Add(Events.UNIT);
            (((machine).self).permissions).Add(Events.CANCEL);
            (((machine).self).permissions).Add(Events.START);
            (((machine).self).permissions).Add(Events.CANCEL_FAILURE);
            (((machine).self).permissions).Add(Events.CANCEL_SUCCESS);
            (((machine).self).permissions).Add(Events.M_START);
            (((machine).self).permissions).Add(Events.M_PONG);
            (((machine).self).permissions).Add(Events.M_PING);
            (((machine).self).permissions).Add(Events.TIMER_CANCELED);
            (((machine).self).permissions).Add(Events.NODE_DOWN);
            (((machine).self).permissions).Add(Events.UNREGISTER_CLIENT);
            (((machine).self).permissions).Add(Events.REGISTER_CLIENT);
            (((machine).self).permissions).Add(Events.ROUND_DONE);
            (((machine).self).permissions).Add(Events.PONG);
            (((machine).self).permissions).Add(Events.PING);
            (((machine).self).permissions).Add(Events.halt);
            ((machine).sends).Add(Events.M_START);
            ((machine).sends).Add(Events.M_PONG);
            ((machine).sends).Add(Events.M_PING);
            ((machine).sends).Add(Events.TIMER_CANCELED);
            ((machine).sends).Add(Events.NODE_DOWN);
            ((machine).sends).Add(Events.UNREGISTER_CLIENT);
            ((machine).sends).Add(Events.REGISTER_CLIENT);
            ((machine).sends).Add(Events.ROUND_DONE);
            ((machine).sends).Add(Events.PONG);
            ((machine).sends).Add(Events.PING);
            ((machine).sends).Add(Events.halt);
            ((machine).sends).Add(Events.UNIT);
            ((machine).sends).Add(Events.CANCEL);
            ((machine).sends).Add(Events.START);
            ((machine).sends).Add(Events.CANCEL_FAILURE);
            ((machine).sends).Add(Events.CANCEL_SUCCESS);
            ((machine).sends).Add(Events.TIMEOUT);
            (machine).currentPayload = payload;
            return machine;
        }

        public static PrtSpecMachine CreateSpecMachine_Safety(StateImpl application)
        {
            var machine = new Safety(application);
            (application).Trace("<CreateLog> Created spec Machine Safety");
            ((machine).observes).Add(Events.M_PONG);
            ((machine).observes).Add(Events.M_PING);
            return machine;
        }

        public static PrtImplMachine CreateMachine_Main(StateImpl application, PrtValue payload)
        {
            var machine = new Main(application, PrtImplMachine.DefaultMaxBufferSize, false);
            (application).Trace("<CreateLog> Created Machine Main-{0}", (machine).instanceNumber);
            (((machine).self).permissions).Add(Events.TIMEOUT);
            (((machine).self).permissions).Add(Events.UNIT);
            (((machine).self).permissions).Add(Events.CANCEL);
            (((machine).self).permissions).Add(Events.START);
            (((machine).self).permissions).Add(Events.CANCEL_FAILURE);
            (((machine).self).permissions).Add(Events.CANCEL_SUCCESS);
            (((machine).self).permissions).Add(Events.M_START);
            (((machine).self).permissions).Add(Events.M_PONG);
            (((machine).self).permissions).Add(Events.M_PING);
            (((machine).self).permissions).Add(Events.TIMER_CANCELED);
            (((machine).self).permissions).Add(Events.NODE_DOWN);
            (((machine).self).permissions).Add(Events.UNREGISTER_CLIENT);
            (((machine).self).permissions).Add(Events.REGISTER_CLIENT);
            (((machine).self).permissions).Add(Events.ROUND_DONE);
            (((machine).self).permissions).Add(Events.PONG);
            (((machine).self).permissions).Add(Events.PING);
            (((machine).self).permissions).Add(Events.halt);
            ((machine).sends).Add(Events.M_START);
            ((machine).sends).Add(Events.M_PONG);
            ((machine).sends).Add(Events.M_PING);
            ((machine).sends).Add(Events.TIMER_CANCELED);
            ((machine).sends).Add(Events.NODE_DOWN);
            ((machine).sends).Add(Events.UNREGISTER_CLIENT);
            ((machine).sends).Add(Events.REGISTER_CLIENT);
            ((machine).sends).Add(Events.ROUND_DONE);
            ((machine).sends).Add(Events.PONG);
            ((machine).sends).Add(Events.PING);
            ((machine).sends).Add(Events.halt);
            ((machine).sends).Add(Events.UNIT);
            ((machine).sends).Add(Events.CANCEL);
            ((machine).sends).Add(Events.START);
            ((machine).sends).Add(Events.CANCEL_FAILURE);
            ((machine).sends).Add(Events.CANCEL_SUCCESS);
            ((machine).sends).Add(Events.TIMEOUT);
            (machine).currentPayload = payload;
            return machine;
        }

        public static PrtImplMachine CreateMachine_Node(StateImpl application, PrtValue payload)
        {
            var machine = new Node(application, PrtImplMachine.DefaultMaxBufferSize, false);
            (application).Trace("<CreateLog> Created Machine Node-{0}", (machine).instanceNumber);
            (((machine).self).permissions).Add(Events.TIMEOUT);
            (((machine).self).permissions).Add(Events.halt);
            (((machine).self).permissions).Add(Events.UNIT);
            (((machine).self).permissions).Add(Events.CANCEL);
            (((machine).self).permissions).Add(Events.START);
            (((machine).self).permissions).Add(Events.CANCEL_FAILURE);
            (((machine).self).permissions).Add(Events.CANCEL_SUCCESS);
            (((machine).self).permissions).Add(Events.M_START);
            (((machine).self).permissions).Add(Events.M_PONG);
            (((machine).self).permissions).Add(Events.M_PING);
            (((machine).self).permissions).Add(Events.TIMER_CANCELED);
            (((machine).self).permissions).Add(Events.NODE_DOWN);
            (((machine).self).permissions).Add(Events.UNREGISTER_CLIENT);
            (((machine).self).permissions).Add(Events.REGISTER_CLIENT);
            (((machine).self).permissions).Add(Events.ROUND_DONE);
            (((machine).self).permissions).Add(Events.PONG);
            (((machine).self).permissions).Add(Events.PING);
            ((machine).sends).Add(Events.M_START);
            ((machine).sends).Add(Events.M_PONG);
            ((machine).sends).Add(Events.M_PING);
            ((machine).sends).Add(Events.TIMER_CANCELED);
            ((machine).sends).Add(Events.NODE_DOWN);
            ((machine).sends).Add(Events.UNREGISTER_CLIENT);
            ((machine).sends).Add(Events.REGISTER_CLIENT);
            ((machine).sends).Add(Events.ROUND_DONE);
            ((machine).sends).Add(Events.PONG);
            ((machine).sends).Add(Events.PING);
            ((machine).sends).Add(Events.halt);
            ((machine).sends).Add(Events.UNIT);
            ((machine).sends).Add(Events.CANCEL);
            ((machine).sends).Add(Events.START);
            ((machine).sends).Add(Events.CANCEL_FAILURE);
            ((machine).sends).Add(Events.CANCEL_SUCCESS);
            ((machine).sends).Add(Events.TIMEOUT);
            (machine).currentPayload = payload;
            return machine;
        }

        public class Liveness : PrtSpecMachine
        {
            public override PrtState StartState
            {
                get
                {
                    return Liveness_Init;
                }
            }

            public PrtValue nodes
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

            public override PrtSpecMachine MakeSkeleton()
            {
                return new Liveness();
            }

            public override string Name
            {
                get
                {
                    return "Liveness";
                }
            }

            public Liveness(): base ()
            {
            }

            public Liveness(StateImpl app): base (app)
            {
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_0_205668158));
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
                    Liveness parent = (Liveness)(_parent);
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

            public static ignore_Class ignore = new ignore_Class();
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
                    Liveness parent = (Liveness)(_parent);
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

            public static AnonFun0_Class AnonFun0 = new AnonFun0_Class();
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
                    Liveness parent = (Liveness)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    (parent).nodes = ((currFun).locals[0]).Clone();
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

            public static AnonFun1_Class AnonFun1 = new AnonFun1_Class();
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
                    Liveness parent = (Liveness)(_parent);
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

            public static AnonFun2_Class AnonFun2 = new AnonFun2_Class();
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
                    Liveness parent = (Liveness)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    ((PrtMapValue)((parent).nodes)).Remove((currFun).locals[0]);
                    if (!((PrtBoolValue)(new PrtBoolValue((new PrtIntValue(((parent).nodes).Size())).Equals(new PrtIntValue(0))))).bl)
                        goto AnonFun3_if_0_else;
                    if (!!(Events.UNIT).Equals(Events.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Tst\\\\RegressionTests\\\\Feature1SMLevelDecls\\\\DynamicError\\\\BugRepro\\\\PingPongBugRepro.p (178, 5): Raised event must be non-null");
                    (application).Trace("<RaiseLog> Machine Liveness-{0} raised Event {1}", (parent).instanceNumber, (((PrtEventValue)(Events.UNIT)).evt).name);
                    (parent).currentTrigger = Events.UNIT;
                    (parent).currentPayload = Events.@null;
                    (parent).PrtFunContRaise();
                    return;
                    goto AnonFun3_if_0_end;
                    AnonFun3_if_0_else:
                        ;
                    AnonFun3_if_0_end:
                        ;
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

            public static AnonFun3_Class AnonFun3 = new AnonFun3_Class();
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
                    Liveness parent = (Liveness)(_parent);
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

            public static AnonFun4_Class AnonFun4 = new AnonFun4_Class();
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
                    Liveness parent = (Liveness)(_parent);
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

            public static AnonFun5_Class AnonFun5 = new AnonFun5_Class();
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
                    Liveness parent = (Liveness)(_parent);
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

            public static AnonFun6_Class AnonFun6 = new AnonFun6_Class();
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
                    Liveness parent = (Liveness)(_parent);
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

            public static AnonFun7_Class AnonFun7 = new AnonFun7_Class();
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
                    Liveness parent = (Liveness)(_parent);
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

            public static AnonFun8_Class AnonFun8 = new AnonFun8_Class();
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
                    Liveness parent = (Liveness)(_parent);
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

            public static AnonFun9_Class AnonFun9 = new AnonFun9_Class();
            public class Liveness_Wait_Class : PrtState
            {
                public Liveness_Wait_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Liveness_Wait_Class Liveness_Wait;
            public class Liveness_Init_Class : PrtState
            {
                public Liveness_Init_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Liveness_Init_Class Liveness_Init;
            public class Liveness_Done_Class : PrtState
            {
                public Liveness_Done_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Liveness_Done_Class Liveness_Done;
            static Liveness()
            {
                Liveness_Wait = new Liveness_Wait_Class("Liveness_Wait", AnonFun1, AnonFun2, false, StateTemperature.Hot);
                Liveness_Init = new Liveness_Init_Class("Liveness_Init", AnonFun6, AnonFun7, false, StateTemperature.Warm);
                Liveness_Done = new Liveness_Done_Class("Liveness_Done", AnonFun8, AnonFun9, false, StateTemperature.Warm);
                Liveness_Wait.dos.Add(Events.NODE_DOWN, AnonFun3);
                PrtTransition transition_1 = new PrtTransition(AnonFun5, Liveness_Done, false);
                Liveness_Wait.transitions.Add(Events.UNIT, transition_1);
                PrtTransition transition_2 = new PrtTransition(AnonFun0, Liveness_Wait, false);
                Liveness_Init.transitions.Add(Events.M_START, transition_2);
            }
        }

        public class FailureDetector : PrtImplMachine
        {
            public override PrtState StartState
            {
                get
                {
                    return FailureDetector_Init;
                }
            }

            public PrtValue timer
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

            public PrtValue responses
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

            public PrtValue alive
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

            public PrtValue attempts
            {
                get
                {
                    return fields[3];
                }

                set
                {
                    fields[3] = value;
                }
            }

            public PrtValue clients
            {
                get
                {
                    return fields[4];
                }

                set
                {
                    fields[4] = value;
                }
            }

            public PrtValue nodes
            {
                get
                {
                    return fields[5];
                }

                set
                {
                    fields[5] = value;
                }
            }

            public override PrtImplMachine MakeSkeleton()
            {
                return new FailureDetector();
            }

            public override int NextInstanceNumber(StateImpl app)
            {
                return app.NextMachineInstanceNumber(this.GetType());
            }

            public override string Name
            {
                get
                {
                    return "FailureDetector";
                }
            }

            public FailureDetector(): base ()
            {
            }

            public FailureDetector(StateImpl app, int maxB, bool assume): base (app, maxB, assume)
            {
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_1_205668158));
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_0_205668158));
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_0_205668158));
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_4_205668158));
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_0_205668158));
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_5_205668158));
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
                    FailureDetector parent = (FailureDetector)(_parent);
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

            public static ignore_Class ignore = new ignore_Class();
            public class Notify_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return false;
                    }
                }

                internal class Notify_StackFrame : PrtFunStackFrame
                {
                    public Notify_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public Notify_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue i
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

                    public PrtValue j
                    {
                        get
                        {
                            return locals[1];
                        }

                        set
                        {
                            locals[1] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    FailureDetector parent = (FailureDetector)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto Notify_1;
                    }

                    (currFun).locals[0] = (new PrtIntValue(0)).Clone();
                    Notify_loop_start_1:
                        ;
                    if (!((PrtBoolValue)(new PrtBoolValue(((PrtIntValue)((currFun).locals[0])).nt < ((PrtIntValue)(new PrtIntValue(((parent).nodes).Size()))).nt))).bl)
                        goto Notify_loop_end_1;
                    if (!((PrtBoolValue)(new PrtBoolValue(((PrtBoolValue)(new PrtBoolValue(((PrtMapValue)((parent).alive)).Contains((((PrtSeqValue)((parent).nodes)).Lookup((currFun).locals[0])).Clone())))).bl && ((PrtBoolValue)(new PrtBoolValue(!((PrtBoolValue)(new PrtBoolValue(((PrtMapValue)((parent).responses)).Contains((((PrtSeqValue)((parent).nodes)).Lookup((currFun).locals[0])).Clone())))).bl))).bl))).bl)
                        goto Notify_if_0_else;
                    ((PrtMapValue)((parent).alive)).Remove((((PrtSeqValue)((parent).nodes)).Lookup((currFun).locals[0])).Clone());
                    (currFun).locals[1] = (new PrtIntValue(0)).Clone();
                    Notify_loop_start_0:
                        ;
                    if (!((PrtBoolValue)(new PrtBoolValue(((PrtIntValue)((currFun).locals[1])).nt < ((PrtIntValue)(new PrtIntValue(((parent).clients).Size()))).nt))).bl)
                        goto Notify_loop_end_0;
                    (((PrtMachineValue)((((PrtSeqValue)(((PrtMapValue)((parent).clients)).Keys())).Lookup((currFun).locals[1])).Clone())).mach).PrtEnqueueEvent((PrtEventValue)(Events.NODE_DOWN), (((PrtSeqValue)((parent).nodes)).Lookup((currFun).locals[0])).Clone(), parent, (PrtMachineValue)((((PrtSeqValue)(((PrtMapValue)((parent).clients)).Keys())).Lookup((currFun).locals[1])).Clone()));
                    (parent).PrtFunContSend(this, (currFun).locals, 1);
                    return;
                    Notify_1:
                        ;
                    (currFun).locals[1] = (new PrtIntValue(((PrtIntValue)((currFun).locals[1])).nt + ((PrtIntValue)(new PrtIntValue(1))).nt)).Clone();
                    goto Notify_loop_start_0;
                    Notify_loop_end_0:
                        ;
                    goto Notify_if_0_end;
                    Notify_if_0_else:
                        ;
                    Notify_if_0_end:
                        ;
                    (currFun).locals[0] = (new PrtIntValue(((PrtIntValue)((currFun).locals[0])).nt + ((PrtIntValue)(new PrtIntValue(1))).nt)).Clone();
                    goto Notify_loop_start_1;
                    Notify_loop_end_1:
                        ;
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
                    return new Notify_StackFrame(this, locals, retLoc);
                }
            }

            public static Notify_Class Notify = new Notify_Class();
            public class SendPingsToAliveSet_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return false;
                    }
                }

                internal class SendPingsToAliveSet_StackFrame : PrtFunStackFrame
                {
                    public SendPingsToAliveSet_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public SendPingsToAliveSet_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue i
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
                    FailureDetector parent = (FailureDetector)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto SendPingsToAliveSet_1;
                    }

                    (currFun).locals[0] = (new PrtIntValue(0)).Clone();
                    SendPingsToAliveSet_loop_start_0:
                        ;
                    if (!((PrtBoolValue)(new PrtBoolValue(((PrtIntValue)((currFun).locals[0])).nt < ((PrtIntValue)(new PrtIntValue(((parent).nodes).Size()))).nt))).bl)
                        goto SendPingsToAliveSet_loop_end_0;
                    if (!((PrtBoolValue)(new PrtBoolValue(((PrtBoolValue)(new PrtBoolValue(((PrtMapValue)((parent).alive)).Contains((((PrtSeqValue)((parent).nodes)).Lookup((currFun).locals[0])).Clone())))).bl && ((PrtBoolValue)(new PrtBoolValue(!((PrtBoolValue)(new PrtBoolValue(((PrtMapValue)((parent).responses)).Contains((((PrtSeqValue)((parent).nodes)).Lookup((currFun).locals[0])).Clone())))).bl))).bl))).bl)
                        goto SendPingsToAliveSet_if_0_else;
                    (application).Announce((PrtEventValue)(Events.M_PING), (((PrtSeqValue)((parent).nodes)).Lookup((currFun).locals[0])).Clone(), parent);
                    (((PrtMachineValue)((((PrtSeqValue)((parent).nodes)).Lookup((currFun).locals[0])).Clone())).mach).PrtEnqueueEvent((PrtEventValue)(Events.PING), parent.self, parent, (PrtMachineValue)((((PrtSeqValue)((parent).nodes)).Lookup((currFun).locals[0])).Clone()));
                    (parent).PrtFunContSend(this, (currFun).locals, 1);
                    return;
                    SendPingsToAliveSet_1:
                        ;
                    goto SendPingsToAliveSet_if_0_end;
                    SendPingsToAliveSet_if_0_else:
                        ;
                    SendPingsToAliveSet_if_0_end:
                        ;
                    (currFun).locals[0] = (new PrtIntValue(((PrtIntValue)((currFun).locals[0])).nt + ((PrtIntValue)(new PrtIntValue(1))).nt)).Clone();
                    goto SendPingsToAliveSet_loop_start_0;
                    SendPingsToAliveSet_loop_end_0:
                        ;
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
                    return new SendPingsToAliveSet_StackFrame(this, locals, retLoc);
                }
            }

            public static SendPingsToAliveSet_Class SendPingsToAliveSet = new SendPingsToAliveSet_Class();
            public class InitializeAliveSet_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return false;
                    }
                }

                internal class InitializeAliveSet_StackFrame : PrtFunStackFrame
                {
                    public InitializeAliveSet_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public InitializeAliveSet_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue i
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
                    FailureDetector parent = (FailureDetector)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    (currFun).locals[0] = (new PrtIntValue(0)).Clone();
                    InitializeAliveSet_loop_start_0:
                        ;
                    if (!((PrtBoolValue)(new PrtBoolValue(((PrtIntValue)((currFun).locals[0])).nt < ((PrtIntValue)(new PrtIntValue(((parent).nodes).Size()))).nt))).bl)
                        goto InitializeAliveSet_loop_end_0;
                    ((PrtMapValue)((parent).alive)).Update((((PrtSeqValue)((parent).nodes)).Lookup((currFun).locals[0])).Clone(), (new PrtBoolValue(true)).Clone());
                    (currFun).locals[0] = (new PrtIntValue(((PrtIntValue)((currFun).locals[0])).nt + ((PrtIntValue)(new PrtIntValue(1))).nt)).Clone();
                    goto InitializeAliveSet_loop_start_0;
                    InitializeAliveSet_loop_end_0:
                        ;
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
                    return new InitializeAliveSet_StackFrame(this, locals, retLoc);
                }
            }

            public static InitializeAliveSet_Class InitializeAliveSet = new InitializeAliveSet_Class();
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
                    FailureDetector parent = (FailureDetector)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun0_1;
                    }

                    (parent).attempts = (new PrtIntValue(0)).Clone();
                    (parent).responses = (PrtValue.PrtMkDefaultValue(Types.type_0_205668158)).Clone();
                    (((PrtMachineValue)((parent).timer)).mach).PrtEnqueueEvent((PrtEventValue)(Events.START), new PrtIntValue(1000), parent, (PrtMachineValue)((parent).timer));
                    (parent).PrtFunContSend(this, (currFun).locals, 1);
                    return;
                    AnonFun0_1:
                        ;
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

            public static AnonFun0_Class AnonFun0 = new AnonFun0_Class();
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
                    FailureDetector parent = (FailureDetector)(_parent);
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

            public static AnonFun1_Class AnonFun1 = new AnonFun1_Class();
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
                    FailureDetector parent = (FailureDetector)(_parent);
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

            public static AnonFun2_Class AnonFun2 = new AnonFun2_Class();
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
                    FailureDetector parent = (FailureDetector)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun3_1;
                        case 2:
                            goto AnonFun3_2;
                    }

                    (parent).PrtPushFunStackFrame(SendPingsToAliveSet, (SendPingsToAliveSet).CreateLocals(new PrtIntValue(0)));
                    AnonFun3_1:
                        ;
                    (SendPingsToAliveSet).Execute(application, parent);
                    if (((parent).continuation).reason == PrtContinuationReason.Return)
                    {
                    }
                    else
                    {
                        (parent).PrtPushFunStackFrame((currFun).fun, (currFun).locals, 1);
                        return;
                    }

                    (((PrtMachineValue)((parent).timer)).mach).PrtEnqueueEvent((PrtEventValue)(Events.START), new PrtIntValue(100), parent, (PrtMachineValue)((parent).timer));
                    (parent).PrtFunContSend(this, (currFun).locals, 2);
                    return;
                    AnonFun3_2:
                        ;
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

            public static AnonFun3_Class AnonFun3 = new AnonFun3_Class();
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
                    FailureDetector parent = (FailureDetector)(_parent);
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

            public static AnonFun4_Class AnonFun4 = new AnonFun4_Class();
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
                    FailureDetector parent = (FailureDetector)(_parent);
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

            public static AnonFun5_Class AnonFun5 = new AnonFun5_Class();
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
                    FailureDetector parent = (FailureDetector)(_parent);
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

            public static AnonFun6_Class AnonFun6 = new AnonFun6_Class();
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
                    FailureDetector parent = (FailureDetector)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun7_1;
                        case 2:
                            goto AnonFun7_2;
                    }

                    (parent).nodes = ((currFun).locals[0]).Clone();
                    (parent).PrtPushFunStackFrame(InitializeAliveSet, (InitializeAliveSet).CreateLocals(new PrtIntValue(0)));
                    AnonFun7_1:
                        ;
                    (InitializeAliveSet).Execute(application, parent);
                    if (((parent).continuation).reason == PrtContinuationReason.Return)
                    {
                    }
                    else
                    {
                        (parent).PrtPushFunStackFrame((currFun).fun, (currFun).locals, 1);
                        return;
                    }

                    (parent).timer = (application).CreateInterfaceOrMachine((parent).renamedName, "Timer", parent.self);
                    (parent).PrtFunContNewMachine(this, (currFun).locals, 2);
                    return;
                    AnonFun7_2:
                        ;
                    if (!!(Events.UNIT).Equals(Events.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Tst\\\\RegressionTests\\\\Feature1SMLevelDecls\\\\DynamicError\\\\BugRepro\\\\PingPongBugRepro.p (57, 10): Raised event must be non-null");
                    (application).Trace("<RaiseLog> Machine FailureDetector-{0} raised Event {1}", (parent).instanceNumber, (((PrtEventValue)(Events.UNIT)).evt).name);
                    (parent).currentTrigger = Events.UNIT;
                    (parent).currentPayload = Events.@null;
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
                    return new AnonFun7_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun7_Class AnonFun7 = new AnonFun7_Class();
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
                    FailureDetector parent = (FailureDetector)(_parent);
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

            public static AnonFun8_Class AnonFun8 = new AnonFun8_Class();
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
                    FailureDetector parent = (FailureDetector)(_parent);
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

            public static AnonFun9_Class AnonFun9 = new AnonFun9_Class();
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
                    FailureDetector parent = (FailureDetector)(_parent);
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

            public static AnonFun10_Class AnonFun10 = new AnonFun10_Class();
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
                    FailureDetector parent = (FailureDetector)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    (parent).currentTrigger = Events.@null;
                    (parent).currentPayload = Events.@null;
                    (parent).PrtFunContPop();
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
                    return new AnonFun11_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun11_Class AnonFun11 = new AnonFun11_Class();
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
                    FailureDetector parent = (FailureDetector)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    if (!!(Events.ROUND_DONE).Equals(Events.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Tst\\\\RegressionTests\\\\Feature1SMLevelDecls\\\\DynamicError\\\\BugRepro\\\\PingPongBugRepro.p (91, 49): Raised event must be non-null");
                    (application).Trace("<RaiseLog> Machine FailureDetector-{0} raised Event {1}", (parent).instanceNumber, (((PrtEventValue)(Events.ROUND_DONE)).evt).name);
                    (parent).currentTrigger = Events.ROUND_DONE;
                    (parent).currentPayload = Events.@null;
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
                    return new AnonFun12_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun12_Class AnonFun12 = new AnonFun12_Class();
            public class AnonFun13_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun13_StackFrame : PrtFunStackFrame
                {
                    public AnonFun13_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun13_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    FailureDetector parent = (FailureDetector)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun13_1;
                    }

                    (parent).attempts = (new PrtIntValue(((PrtIntValue)((parent).attempts)).nt + ((PrtIntValue)(new PrtIntValue(1))).nt)).Clone();
                    if (!((PrtBoolValue)(new PrtBoolValue(((PrtBoolValue)(new PrtBoolValue(((PrtIntValue)(new PrtIntValue(((parent).responses).Size()))).nt < ((PrtIntValue)(new PrtIntValue(((parent).alive).Size()))).nt))).bl && ((PrtBoolValue)(new PrtBoolValue(((PrtIntValue)((parent).attempts)).nt < ((PrtIntValue)(new PrtIntValue(2))).nt))).bl))).bl)
                        goto AnonFun13_if_0_else;
                    if (!!(Events.UNIT).Equals(Events.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Tst\\\\RegressionTests\\\\Feature1SMLevelDecls\\\\DynamicError\\\\BugRepro\\\\PingPongBugRepro.p (80, 5): Raised event must be non-null");
                    (application).Trace("<RaiseLog> Machine FailureDetector-{0} raised Event {1}", (parent).instanceNumber, (((PrtEventValue)(Events.UNIT)).evt).name);
                    (parent).currentTrigger = Events.UNIT;
                    (parent).currentPayload = Events.@null;
                    (parent).PrtFunContRaise();
                    return;
                    goto AnonFun13_if_0_end;
                    AnonFun13_if_0_else:
                        ;
                    AnonFun13_if_0_end:
                        ;
                    (parent).PrtPushFunStackFrame(Notify, (Notify).CreateLocals(new PrtIntValue(1), new PrtIntValue(0)));
                    AnonFun13_1:
                        ;
                    (Notify).Execute(application, parent);
                    if (((parent).continuation).reason == PrtContinuationReason.Return)
                    {
                    }
                    else
                    {
                        (parent).PrtPushFunStackFrame((currFun).fun, (currFun).locals, 1);
                        return;
                    }

                    if (!!(Events.ROUND_DONE).Equals(Events.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Tst\\\\RegressionTests\\\\Feature1SMLevelDecls\\\\DynamicError\\\\BugRepro\\\\PingPongBugRepro.p (83, 4): Raised event must be non-null");
                    (application).Trace("<RaiseLog> Machine FailureDetector-{0} raised Event {1}", (parent).instanceNumber, (((PrtEventValue)(Events.ROUND_DONE)).evt).name);
                    (parent).currentTrigger = Events.ROUND_DONE;
                    (parent).currentPayload = Events.@null;
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
                    return new AnonFun13_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun13_Class AnonFun13 = new AnonFun13_Class();
            public class AnonFun14_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun14_StackFrame : PrtFunStackFrame
                {
                    public AnonFun14_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun14_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    FailureDetector parent = (FailureDetector)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun14_1;
                    }

                    if (!((PrtBoolValue)(new PrtBoolValue(((PrtMapValue)((parent).alive)).Contains((currFun).locals[0])))).bl)
                        goto AnonFun14_if_1_else;
                    ((PrtMapValue)((parent).responses)).Update((currFun).locals[0], (new PrtBoolValue(true)).Clone());
                    if (!((PrtBoolValue)(new PrtBoolValue((new PrtIntValue(((parent).responses).Size())).Equals(new PrtIntValue(((parent).alive).Size()))))).bl)
                        goto AnonFun14_if_0_else;
                    (((PrtMachineValue)((parent).timer)).mach).PrtEnqueueEvent((PrtEventValue)(Events.CANCEL), Events.@null, parent, (PrtMachineValue)((parent).timer));
                    (parent).PrtFunContSend(this, (currFun).locals, 1);
                    return;
                    AnonFun14_1:
                        ;
                    if (!!(Events.TIMER_CANCELED).Equals(Events.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Tst\\\\RegressionTests\\\\Feature1SMLevelDecls\\\\DynamicError\\\\BugRepro\\\\PingPongBugRepro.p (73, 7): Raised event must be non-null");
                    (application).Trace("<RaiseLog> Machine FailureDetector-{0} raised Event {1}", (parent).instanceNumber, (((PrtEventValue)(Events.TIMER_CANCELED)).evt).name);
                    (parent).currentTrigger = Events.TIMER_CANCELED;
                    (parent).currentPayload = Events.@null;
                    (parent).PrtFunContRaise();
                    return;
                    goto AnonFun14_if_0_end;
                    AnonFun14_if_0_else:
                        ;
                    AnonFun14_if_0_end:
                        ;
                    goto AnonFun14_if_1_end;
                    AnonFun14_if_1_else:
                        ;
                    AnonFun14_if_1_end:
                        ;
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
                    return new AnonFun14_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun14_Class AnonFun14 = new AnonFun14_Class();
            public class AnonFun15_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun15_StackFrame : PrtFunStackFrame
                {
                    public AnonFun15_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun15_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    FailureDetector parent = (FailureDetector)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    if (!((PrtBoolValue)(new PrtBoolValue(((PrtMapValue)((parent).clients)).Contains((currFun).locals[0])))).bl)
                        goto AnonFun15_if_0_else;
                    ((PrtMapValue)((parent).clients)).Remove((currFun).locals[0]);
                    goto AnonFun15_if_0_end;
                    AnonFun15_if_0_else:
                        ;
                    AnonFun15_if_0_end:
                        ;
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
                    return new AnonFun15_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun15_Class AnonFun15 = new AnonFun15_Class();
            public class AnonFun16_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun16_StackFrame : PrtFunStackFrame
                {
                    public AnonFun16_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun16_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    FailureDetector parent = (FailureDetector)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    ((PrtMapValue)((parent).clients)).Update((currFun).locals[0], (new PrtBoolValue(true)).Clone());
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
                    return new AnonFun16_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun16_Class AnonFun16 = new AnonFun16_Class();
            public class AnonFun17_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun17_StackFrame : PrtFunStackFrame
                {
                    public AnonFun17_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun17_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    FailureDetector parent = (FailureDetector)(_parent);
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
                    return new AnonFun17_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun17_Class AnonFun17 = new AnonFun17_Class();
            public class AnonFun18_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun18_StackFrame : PrtFunStackFrame
                {
                    public AnonFun18_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun18_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    FailureDetector parent = (FailureDetector)(_parent);
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
                    return new AnonFun18_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun18_Class AnonFun18 = new AnonFun18_Class();
            public class AnonFun19_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun19_StackFrame : PrtFunStackFrame
                {
                    public AnonFun19_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun19_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    FailureDetector parent = (FailureDetector)(_parent);
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
                    return new AnonFun19_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun19_Class AnonFun19 = new AnonFun19_Class();
            public class FailureDetector_Reset_Class : PrtState
            {
                public FailureDetector_Reset_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static FailureDetector_Reset_Class FailureDetector_Reset;
            public class FailureDetector_SendPing_Class : PrtState
            {
                public FailureDetector_SendPing_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static FailureDetector_SendPing_Class FailureDetector_SendPing;
            public class FailureDetector_Init_Class : PrtState
            {
                public FailureDetector_Init_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static FailureDetector_Init_Class FailureDetector_Init;
            public class FailureDetector_WaitForCancelResponse_Class : PrtState
            {
                public FailureDetector_WaitForCancelResponse_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static FailureDetector_WaitForCancelResponse_Class FailureDetector_WaitForCancelResponse;
            static FailureDetector()
            {
                FailureDetector_Reset = new FailureDetector_Reset_Class("FailureDetector_Reset", AnonFun0, AnonFun1, false, StateTemperature.Warm);
                FailureDetector_SendPing = new FailureDetector_SendPing_Class("FailureDetector_SendPing", AnonFun3, AnonFun4, false, StateTemperature.Warm);
                FailureDetector_Init = new FailureDetector_Init_Class("FailureDetector_Init", AnonFun7, AnonFun8, false, StateTemperature.Warm);
                FailureDetector_WaitForCancelResponse = new FailureDetector_WaitForCancelResponse_Class("FailureDetector_WaitForCancelResponse", AnonFun9, AnonFun10, false, StateTemperature.Warm);
                FailureDetector_Reset.dos.Add(Events.PONG, PrtFun.IgnoreFun);
                PrtTransition transition_1 = new PrtTransition(AnonFun2, FailureDetector_SendPing, false);
                FailureDetector_Reset.transitions.Add(Events.TIMEOUT, transition_1);
                FailureDetector_SendPing.dos.Add(Events.TIMEOUT, AnonFun13);
                FailureDetector_SendPing.dos.Add(Events.PONG, AnonFun14);
                PrtTransition transition_2 = new PrtTransition(PrtFun.IgnoreFun, FailureDetector_WaitForCancelResponse, true);
                FailureDetector_SendPing.transitions.Add(Events.TIMER_CANCELED, transition_2);
                PrtTransition transition_3 = new PrtTransition(AnonFun5, FailureDetector_SendPing, false);
                FailureDetector_SendPing.transitions.Add(Events.UNIT, transition_3);
                PrtTransition transition_4 = new PrtTransition(AnonFun6, FailureDetector_Reset, false);
                FailureDetector_SendPing.transitions.Add(Events.ROUND_DONE, transition_4);
                FailureDetector_Init.dos.Add(Events.UNREGISTER_CLIENT, AnonFun15);
                FailureDetector_Init.dos.Add(Events.REGISTER_CLIENT, AnonFun16);
                PrtTransition transition_5 = new PrtTransition(PrtFun.IgnoreFun, FailureDetector_SendPing, true);
                FailureDetector_Init.transitions.Add(Events.UNIT, transition_5);
                FailureDetector_WaitForCancelResponse.dos.Add(Events.CANCEL_FAILURE, AnonFun11);
                FailureDetector_WaitForCancelResponse.dos.Add(Events.CANCEL_SUCCESS, AnonFun12);
                FailureDetector_WaitForCancelResponse.deferredSet.Add(Events.PONG);
                FailureDetector_WaitForCancelResponse.deferredSet.Add(Events.TIMEOUT);
            }
        }

        public class Safety : PrtSpecMachine
        {
            public override PrtState StartState
            {
                get
                {
                    return Safety_Init;
                }
            }

            public PrtValue pending
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

            public override PrtSpecMachine MakeSkeleton()
            {
                return new Safety();
            }

            public override string Name
            {
                get
                {
                    return "Safety";
                }
            }

            public Safety(): base ()
            {
            }

            public Safety(StateImpl app): base (app)
            {
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_3_205668158));
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
                    Safety parent = (Safety)(_parent);
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

            public static ignore_Class ignore = new ignore_Class();
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
                    Safety parent = (Safety)(_parent);
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

            public static AnonFun0_Class AnonFun0 = new AnonFun0_Class();
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
                    Safety parent = (Safety)(_parent);
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

            public static AnonFun1_Class AnonFun1 = new AnonFun1_Class();
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
                    Safety parent = (Safety)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    if (!((PrtBoolValue)(new PrtBoolValue(((PrtMapValue)((parent).pending)).Contains((currFun).locals[0])))).bl)
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Tst\\\\RegressionTests\\\\Feature1SMLevelDecls\\\\DynamicError\\\\BugRepro\\\\PingPongBugRepro.p (158, 4): Assert failed");
                    if (!((PrtBoolValue)(new PrtBoolValue(((PrtIntValue)(new PrtIntValue(0))).nt < ((PrtIntValue)((((PrtMapValue)((parent).pending)).Lookup((currFun).locals[0])).Clone())).nt))).bl)
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Tst\\\\RegressionTests\\\\Feature1SMLevelDecls\\\\DynamicError\\\\BugRepro\\\\PingPongBugRepro.p (159, 4): Assert failed");
                    ((PrtMapValue)((parent).pending)).Update((currFun).locals[0], (new PrtIntValue(((PrtIntValue)((((PrtMapValue)((parent).pending)).Lookup((currFun).locals[0])).Clone())).nt - ((PrtIntValue)(new PrtIntValue(1))).nt)).Clone());
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

            public static AnonFun2_Class AnonFun2 = new AnonFun2_Class();
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
                    Safety parent = (Safety)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    if (!((PrtBoolValue)(new PrtBoolValue(!((PrtBoolValue)(new PrtBoolValue(((PrtMapValue)((parent).pending)).Contains((currFun).locals[0])))).bl))).bl)
                        goto AnonFun3_if_1_else;
                    ((PrtMapValue)((parent).pending)).Update((currFun).locals[0], (new PrtIntValue(0)).Clone());
                    goto AnonFun3_if_1_end;
                    AnonFun3_if_1_else:
                        ;
                    AnonFun3_if_1_end:
                        ;
                    ((PrtMapValue)((parent).pending)).Update((currFun).locals[0], (new PrtIntValue(((PrtIntValue)((((PrtMapValue)((parent).pending)).Lookup((currFun).locals[0])).Clone())).nt + ((PrtIntValue)(new PrtIntValue(1))).nt)).Clone());
                    if (!((PrtBoolValue)(new PrtBoolValue(((PrtIntValue)((((PrtMapValue)((parent).pending)).Lookup((currFun).locals[0])).Clone())).nt <= ((PrtIntValue)(new PrtIntValue(3))).nt))).bl)
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Tst\\\\RegressionTests\\\\Feature1SMLevelDecls\\\\DynamicError\\\\BugRepro\\\\PingPongBugRepro.p (155, 4): Assert failed");
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

            public static AnonFun3_Class AnonFun3 = new AnonFun3_Class();
            public class Safety_Init_Class : PrtState
            {
                public Safety_Init_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Safety_Init_Class Safety_Init;
            static Safety()
            {
                Safety_Init = new Safety_Init_Class("Safety_Init", AnonFun0, AnonFun1, false, StateTemperature.Warm);
                Safety_Init.dos.Add(Events.M_PONG, AnonFun2);
                Safety_Init.dos.Add(Events.M_PING, AnonFun3);
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

            public PrtValue n
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

            public PrtValue i
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

            public PrtValue nodemap
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

            public PrtValue nodeseq
            {
                get
                {
                    return fields[3];
                }

                set
                {
                    fields[3] = value;
                }
            }

            public PrtValue fd
            {
                get
                {
                    return fields[4];
                }

                set
                {
                    fields[4] = value;
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
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_1_205668158));
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_4_205668158));
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_0_205668158));
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_5_205668158));
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_1_205668158));
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

            public static ignore_Class ignore = new ignore_Class();
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
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun0_1;
                        case 2:
                            goto AnonFun0_2;
                        case 3:
                            goto AnonFun0_3;
                        case 4:
                            goto AnonFun0_4;
                    }

                    (parent).i = (new PrtIntValue(0)).Clone();
                    AnonFun0_loop_start_0:
                        ;
                    if (!((PrtBoolValue)(new PrtBoolValue(((PrtIntValue)((parent).i)).nt < ((PrtIntValue)(new PrtIntValue(2))).nt))).bl)
                        goto AnonFun0_loop_end_0;
                    (parent).n = (application).CreateInterfaceOrMachine((parent).renamedName, "Node", Events.@null);
                    (parent).PrtFunContNewMachine(this, (currFun).locals, 1);
                    return;
                    AnonFun0_1:
                        ;
                    ((PrtSeqValue)((parent).nodeseq)).Insert(((PrtTupleValue)(new PrtTupleValue((parent).i, (parent).n))).fieldValues[0], ((PrtTupleValue)(new PrtTupleValue((parent).i, (parent).n))).fieldValues[1]);
                    ((PrtMapValue)((parent).nodemap)).Update((parent).n, (new PrtBoolValue(true)).Clone());
                    (parent).i = (new PrtIntValue(((PrtIntValue)((parent).i)).nt + ((PrtIntValue)(new PrtIntValue(1))).nt)).Clone();
                    goto AnonFun0_loop_start_0;
                    AnonFun0_loop_end_0:
                        ;
                    (application).Announce((PrtEventValue)(Events.M_START), (parent).nodemap, parent);
                    (parent).fd = (application).CreateInterfaceOrMachine((parent).renamedName, "FailureDetector", (parent).nodeseq);
                    (parent).PrtFunContNewMachine(this, (currFun).locals, 2);
                    return;
                    AnonFun0_2:
                        ;
                    (((PrtMachineValue)((parent).fd)).mach).PrtEnqueueEvent((PrtEventValue)(Events.REGISTER_CLIENT), parent.self, parent, (PrtMachineValue)((parent).fd));
                    (parent).PrtFunContSend(this, (currFun).locals, 3);
                    return;
                    AnonFun0_3:
                        ;
                    (parent).i = (new PrtIntValue(0)).Clone();
                    AnonFun0_loop_start_1:
                        ;
                    if (!((PrtBoolValue)(new PrtBoolValue(((PrtIntValue)((parent).i)).nt < ((PrtIntValue)(new PrtIntValue(2))).nt))).bl)
                        goto AnonFun0_loop_end_1;
                    (((PrtMachineValue)((((PrtSeqValue)((parent).nodeseq)).Lookup((parent).i)).Clone())).mach).PrtEnqueueEvent((PrtEventValue)(Events.halt), Events.@null, parent, (PrtMachineValue)((((PrtSeqValue)((parent).nodeseq)).Lookup((parent).i)).Clone()));
                    (parent).PrtFunContSend(this, (currFun).locals, 4);
                    return;
                    AnonFun0_4:
                        ;
                    (parent).i = (new PrtIntValue(((PrtIntValue)((parent).i)).nt + ((PrtIntValue)(new PrtIntValue(1))).nt)).Clone();
                    goto AnonFun0_loop_start_1;
                    AnonFun0_loop_end_1:
                        ;
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

            public static AnonFun0_Class AnonFun0 = new AnonFun0_Class();
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

            public static AnonFun1_Class AnonFun1 = new AnonFun1_Class();
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

            public static AnonFun2_Class AnonFun2 = new AnonFun2_Class();
            public class Main_Init_Class : PrtState
            {
                public Main_Init_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Main_Init_Class Main_Init;
            static Main()
            {
                Main_Init = new Main_Init_Class("Main_Init", AnonFun0, AnonFun1, false, StateTemperature.Warm);
                Main_Init.dos.Add(Events.NODE_DOWN, PrtFun.IgnoreFun);
            }
        }

        public class Node : PrtImplMachine
        {
            public override PrtState StartState
            {
                get
                {
                    return Node_WaitPing;
                }
            }

            public override PrtImplMachine MakeSkeleton()
            {
                return new Node();
            }

            public override int NextInstanceNumber(StateImpl app)
            {
                return app.NextMachineInstanceNumber(this.GetType());
            }

            public override string Name
            {
                get
                {
                    return "Node";
                }
            }

            public Node(): base ()
            {
            }

            public Node(StateImpl app, int maxB, bool assume): base (app, maxB, assume)
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
                    Node parent = (Node)(_parent);
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

            public static ignore_Class ignore = new ignore_Class();
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
                    Node parent = (Node)(_parent);
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

            public static AnonFun0_Class AnonFun0 = new AnonFun0_Class();
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
                    Node parent = (Node)(_parent);
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

            public static AnonFun1_Class AnonFun1 = new AnonFun1_Class();
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
                    Node parent = (Node)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun2_1;
                    }

                    (application).Announce((PrtEventValue)(Events.M_PONG), parent.self, parent);
                    (((PrtMachineValue)(PrtValue.PrtCastValue((currFun).locals[0], Types.type_1_205668158))).mach).PrtEnqueueEvent((PrtEventValue)(Events.PONG), parent.self, parent, (PrtMachineValue)(PrtValue.PrtCastValue((currFun).locals[0], Types.type_1_205668158)));
                    (parent).PrtFunContSend(this, (currFun).locals, 1);
                    return;
                    AnonFun2_1:
                        ;
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

            public static AnonFun2_Class AnonFun2 = new AnonFun2_Class();
            public class Node_WaitPing_Class : PrtState
            {
                public Node_WaitPing_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Node_WaitPing_Class Node_WaitPing;
            static Node()
            {
                Node_WaitPing = new Node_WaitPing_Class("Node_WaitPing", AnonFun0, AnonFun1, false, StateTemperature.Warm);
                Node_WaitPing.dos.Add(Events.PING, AnonFun2);
            }
        }
    }
}
