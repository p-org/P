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
            public static PrtEventValue event_eRequest;
            public static PrtEventValue event_eResponse;
            public static PrtEventValue event_eProcessReq;
            public static PrtEventValue event_eReqSuccessful;
            public static PrtEventValue event_eReqFailed;
            public static void Events_refinement()
            {
                event_eRequest = new PrtEventValue(new PrtEvent("eRequest", Types.type_requestType, PrtEvent.DefaultMaxInstances, false));
                event_eResponse = new PrtEventValue(new PrtEvent("eResponse", Types.type_responseType, PrtEvent.DefaultMaxInstances, false));
                event_eProcessReq = new PrtEventValue(new PrtEvent("eProcessReq", Types.type_6_2147315917, PrtEvent.DefaultMaxInstances, false));
                event_eReqSuccessful = new PrtEventValue(new PrtEvent("eReqSuccessful", Types.type_13_2147315917, PrtEvent.DefaultMaxInstances, false));
                event_eReqFailed = new PrtEventValue(new PrtEvent("eReqFailed", Types.type_13_2147315917, PrtEvent.DefaultMaxInstances, false));
            }
        }

        public partial class Types
        {
            public static PrtType type_HelperInterface;
            public static PrtType type_ServerHelperInterface;
            public static PrtType type_ClientInterface;
            public static PrtType type_ServerClientInterface;
            public static PrtType type_TestDriver0;
            public static PrtType type_6_2147315917;
            public static PrtType type_7_2147315917;
            public static PrtType type_responseType;
            public static PrtType type_requestType;
            public static PrtType type_9_2147315917;
            public static PrtType type_10_2147315917;
            public static PrtType type_11_2147315917;
            public static PrtType type_12_2147315917;
            public static PrtType type_13_2147315917;
            public static PrtType type_RespIdsAreMonotonicallyIncreasing;
            public static PrtType type_ReqIdsAreMonotonicallyIncreasing;
            public static PrtType type_HelperMachine;
            public static PrtType type_ServerMachine;
            public static PrtType type_ClientMachine;
            public static PrtType type_UNSOUNDAbstractServerMachine;
            public static PrtType type_AbstractServerMachine;
            static public void Types_refinement()
            {
                Types.type_HelperInterface = new PrtInterfaceType("HelperInterface");
                Types.type_ServerHelperInterface = new PrtInterfaceType("ServerHelperInterface");
                Types.type_ClientInterface = new PrtInterfaceType("ClientInterface");
                Types.type_ServerClientInterface = new PrtInterfaceType("ServerClientInterface");
                Types.type_TestDriver0 = new PrtMachineType();
                Types.type_6_2147315917 = new PrtIntType();
                Types.type_7_2147315917 = new PrtBoolType();
                Types.type_responseType = new PrtNamedTupleType(new object[]{"id", Types.type_6_2147315917, "success", Types.type_7_2147315917});
                Types.type_requestType = new PrtNamedTupleType(new object[]{"source", Types.type_ClientInterface, "id", Types.type_6_2147315917});
                Types.type_9_2147315917 = new PrtNamedTupleType(new object[]{"server", Types.type_ServerClientInterface});
                Types.type_10_2147315917 = new PrtNamedTupleType(new object[]{"successful", Types.type_7_2147315917});
                Types.type_11_2147315917 = new PrtNamedTupleType(new object[]{"index", Types.type_6_2147315917});
                Types.type_12_2147315917 = new PrtEventType();
                Types.type_13_2147315917 = new PrtNullType();
                Types.type_RespIdsAreMonotonicallyIncreasing = Types.type_TestDriver0;
                Types.type_ReqIdsAreMonotonicallyIncreasing = Types.type_TestDriver0;
                Types.type_HelperMachine = Types.type_TestDriver0;
                Types.type_ServerMachine = Types.type_TestDriver0;
                Types.type_ClientMachine = Types.type_TestDriver0;
                Types.type_UNSOUNDAbstractServerMachine = Types.type_TestDriver0;
                Types.type_AbstractServerMachine = Types.type_TestDriver0;
            }
        }

        public static PrtImplMachine CreateMachine_TestDriver0(StateImpl application, PrtValue payload)
        {
            var machine = new TestDriver0(application, PrtImplMachine.DefaultMaxBufferSize, false);
            (application).TraceLine("<CreateLog> Created Machine TestDriver0-{0}", (machine).instanceNumber);
            (machine).currentPayload = payload;
            return machine;
        }

        public static PrtImplMachine CreateMachine_HelperMachine(StateImpl application, PrtValue payload)
        {
            var machine = new HelperMachine(application, PrtImplMachine.DefaultMaxBufferSize, false);
            (application).TraceLine("<CreateLog> Created Machine HelperMachine-{0}", (machine).instanceNumber);
            ((machine).self).permissions = null;
            ((machine).sends).Add(Events.event_eReqFailed);
            ((machine).sends).Add(Events.event_eReqSuccessful);
            (machine).currentPayload = payload;
            return machine;
        }

        public static PrtImplMachine CreateMachine_ServerMachine(StateImpl application, PrtValue payload)
        {
            var machine = new ServerMachine(application, PrtImplMachine.DefaultMaxBufferSize, false);
            (application).TraceLine("<CreateLog> Created Machine ServerMachine-{0}", (machine).instanceNumber);
            ((machine).self).permissions = null;
            ((machine).sends).Add(Events.event_eProcessReq);
            ((machine).sends).Add(Events.event_eResponse);
            (machine).currentPayload = payload;
            return machine;
        }

        public static PrtImplMachine CreateMachine_ClientMachine(StateImpl application, PrtValue payload)
        {
            var machine = new ClientMachine(application, PrtImplMachine.DefaultMaxBufferSize, false);
            (application).TraceLine("<CreateLog> Created Machine ClientMachine-{0}", (machine).instanceNumber);
            ((machine).self).permissions = null;
            ((machine).sends).Add(Events.event_eRequest);
            (machine).currentPayload = payload;
            return machine;
        }

        public static PrtImplMachine CreateMachine_UNSOUNDAbstractServerMachine(StateImpl application, PrtValue payload)
        {
            var machine = new UNSOUNDAbstractServerMachine(application, PrtImplMachine.DefaultMaxBufferSize, false);
            (application).TraceLine("<CreateLog> Created Machine UNSOUNDAbstractServerMachine-{0}", (machine).instanceNumber);
            ((machine).self).permissions = null;
            ((machine).sends).Add(Events.event_eResponse);
            (machine).currentPayload = payload;
            return machine;
        }

        public static PrtSpecMachine CreateSpecMachine_RespIdsAreMonotonicallyIncreasing(StateImpl application)
        {
            var machine = new RespIdsAreMonotonicallyIncreasing(application);
            (application).TraceLine("<CreateLog> Created spec Machine RespIdsAreMonotonicallyIncreasing");
            ((machine).observes).Add(Events.event_eResponse);
            (machine).PrtEnqueueEvent(PrtEventValue.@null, PrtEventValue.@null, null);
            return machine;
        }

        public static PrtSpecMachine CreateSpecMachine_ReqIdsAreMonotonicallyIncreasing(StateImpl application)
        {
            var machine = new ReqIdsAreMonotonicallyIncreasing(application);
            (application).TraceLine("<CreateLog> Created spec Machine ReqIdsAreMonotonicallyIncreasing");
            ((machine).observes).Add(Events.event_eRequest);
            (machine).PrtEnqueueEvent(PrtEventValue.@null, PrtEventValue.@null, null);
            return machine;
        }

        public static PrtImplMachine CreateMachine_AbstractServerMachine(StateImpl application, PrtValue payload)
        {
            var machine = new AbstractServerMachine(application, PrtImplMachine.DefaultMaxBufferSize, false);
            (application).TraceLine("<CreateLog> Created Machine AbstractServerMachine-{0}", (machine).instanceNumber);
            ((machine).self).permissions = null;
            ((machine).sends).Add(Events.event_eResponse);
            (machine).currentPayload = payload;
            return machine;
        }

        public class TestDriver0 : PrtImplMachine
        {
            public override PrtState StartState
            {
                get
                {
                    return TestDriver0_Init;
                }
            }

            public override PrtImplMachine MakeSkeleton()
            {
                return new TestDriver0();
            }

            public override int NextInstanceNumber(StateImpl app)
            {
                return app.NextMachineInstanceNumber(this.GetType());
            }

            public override string Name
            {
                get
                {
                    return "TestDriver0";
                }
            }

            public TestDriver0(): base ()
            {
            }

            public TestDriver0(StateImpl app, int maxB, bool assume): base (app, maxB, assume)
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
                    public ignore_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                    {
                    }

                    public ignore_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    TestDriver0 parent = (TestDriver0)(_parent);
                    ignore_StackFrame currFun = (ignore_StackFrame)(parent.PrtPopFunStackFrame());
                    PrtValue swap;
                    parent.PrtFunContReturn((currFun).locals);
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

                public override string ToString()
                {
                    return "ignore";
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
                    public AnonFun0_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                    {
                    }

                    public AnonFun0_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue var__payload_skip
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
                    TestDriver0 parent = (TestDriver0)(_parent);
                    AnonFun0_StackFrame currFun = (AnonFun0_StackFrame)(parent.PrtPopFunStackFrame());
                    PrtValue swap;
                    parent.PrtFunContReturn((currFun).locals);
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

                public override string ToString()
                {
                    return "AnonFun0";
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
                    public AnonFun1_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                    {
                    }

                    public AnonFun1_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue var__payload_skip
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
                    TestDriver0 parent = (TestDriver0)(_parent);
                    AnonFun1_StackFrame currFun = (AnonFun1_StackFrame)(parent.PrtPopFunStackFrame());
                    PrtValue swap;
                    parent.PrtFunContReturn((currFun).locals);
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

                public override string ToString()
                {
                    return "AnonFun1";
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
                    public AnonFun2_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                    {
                    }

                    public AnonFun2_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue var__payload_0
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

                    public PrtValue var_server
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
                    TestDriver0 parent = (TestDriver0)(_parent);
                    AnonFun2_StackFrame currFun = (AnonFun2_StackFrame)(parent.PrtPopFunStackFrame());
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun2_1;
                        case 2:
                            goto AnonFun2_2;
                    }

                    (currFun).var_server = (application).CreateInterface((parent).renamedName, "ServerClientInterface", Events.@null);
                    (parent).PrtFunContNewMachine(this, (currFun).locals, 1);
                    return;
                    AnonFun2_1:
                        ;
                    (application).CreateInterface((parent).renamedName, "ClientInterface", (currFun).var_server);
                    (parent).PrtFunContNewMachine(this, (currFun).locals, 2);
                    return;
                    AnonFun2_2:
                        ;
                    parent.PrtFunContReturn((currFun).locals);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    (locals).Add(PrtValue.PrtMkDefaultValue(Types.type_ServerClientInterface));
                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun2_StackFrame(this, locals, retLoc);
                }

                public override string ToString()
                {
                    return "AnonFun2";
                }
            }

            public static AnonFun2_Class AnonFun2 = new AnonFun2_Class();
            public class TestDriver0_Init_Class : PrtState
            {
                public TestDriver0_Init_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static TestDriver0_Init_Class TestDriver0_Init;
            static TestDriver0()
            {
                TestDriver0_Init = new TestDriver0_Init_Class("TestDriver0_Init", AnonFun2, AnonFun1, false, StateTemperature.Warm);
            }
        }

        public class HelperMachine : PrtImplMachine
        {
            public override PrtState StartState
            {
                get
                {
                    return HelperMachine_Init;
                }
            }

            public PrtValue var_server
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

            public override PrtImplMachine MakeSkeleton()
            {
                return new HelperMachine();
            }

            public override int NextInstanceNumber(StateImpl app)
            {
                return app.NextMachineInstanceNumber(this.GetType());
            }

            public override string Name
            {
                get
                {
                    return "HelperMachine";
                }
            }

            public HelperMachine(): base ()
            {
            }

            public HelperMachine(StateImpl app, int maxB, bool assume): base (app, maxB, assume)
            {
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_ServerHelperInterface));
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
                    public ignore_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                    {
                    }

                    public ignore_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    HelperMachine parent = (HelperMachine)(_parent);
                    ignore_StackFrame currFun = (ignore_StackFrame)(parent.PrtPopFunStackFrame());
                    PrtValue swap;
                    parent.PrtFunContReturn((currFun).locals);
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

                public override string ToString()
                {
                    return "ignore";
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
                    public AnonFun0_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                    {
                    }

                    public AnonFun0_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue var_payload
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
                    HelperMachine parent = (HelperMachine)(_parent);
                    AnonFun0_StackFrame currFun = (AnonFun0_StackFrame)(parent.PrtPopFunStackFrame());
                    PrtValue swap;
                    (parent).var_server = ((currFun).var_payload).Clone();
                    parent.PrtFunContReturn((currFun).locals);
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

                public override string ToString()
                {
                    return "AnonFun0";
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
                    public AnonFun1_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                    {
                    }

                    public AnonFun1_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue var__payload_skip
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
                    HelperMachine parent = (HelperMachine)(_parent);
                    AnonFun1_StackFrame currFun = (AnonFun1_StackFrame)(parent.PrtPopFunStackFrame());
                    PrtValue swap;
                    parent.PrtFunContReturn((currFun).locals);
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

                public override string ToString()
                {
                    return "AnonFun1";
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
                    public AnonFun2_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                    {
                    }

                    public AnonFun2_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue var__payload_3
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
                    HelperMachine parent = (HelperMachine)(_parent);
                    AnonFun2_StackFrame currFun = (AnonFun2_StackFrame)(parent.PrtPopFunStackFrame());
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun2_1;
                        case 2:
                            goto AnonFun2_2;
                    }

                    if (!((PrtBoolValue)(new PrtBoolValue((application).GetSelectedChoiceValue((PrtImplMachine)(parent))))).bl)
                        goto AnonFun2_if_0_else;
                    (((PrtMachineValue)((parent).var_server)).mach).PrtEnqueueEvent((PrtEventValue)(Events.event_eReqSuccessful), Events.@null, parent, (PrtMachineValue)((parent).var_server));
                    (parent).PrtFunContSend(this, (currFun).locals, 1);
                    return;
                    AnonFun2_1:
                        ;
                    goto AnonFun2_if_0_end;
                    AnonFun2_if_0_else:
                        ;
                    (((PrtMachineValue)((parent).var_server)).mach).PrtEnqueueEvent((PrtEventValue)(Events.event_eReqFailed), Events.@null, parent, (PrtMachineValue)((parent).var_server));
                    (parent).PrtFunContSend(this, (currFun).locals, 2);
                    return;
                    AnonFun2_2:
                        ;
                    AnonFun2_if_0_end:
                        ;
                    parent.PrtFunContReturn((currFun).locals);
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

                public override string ToString()
                {
                    return "AnonFun2";
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
                    public AnonFun3_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                    {
                    }

                    public AnonFun3_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue var__payload_skip
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
                    HelperMachine parent = (HelperMachine)(_parent);
                    AnonFun3_StackFrame currFun = (AnonFun3_StackFrame)(parent.PrtPopFunStackFrame());
                    PrtValue swap;
                    parent.PrtFunContReturn((currFun).locals);
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

                public override string ToString()
                {
                    return "AnonFun3";
                }
            }

            public static AnonFun3_Class AnonFun3 = new AnonFun3_Class();
            public class HelperMachine_Init_Class : PrtState
            {
                public HelperMachine_Init_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static HelperMachine_Init_Class HelperMachine_Init;
            static HelperMachine()
            {
                HelperMachine_Init = new HelperMachine_Init_Class("HelperMachine_Init", AnonFun0, AnonFun1, false, StateTemperature.Warm);
                HelperMachine_Init.dos.Add(Events.event_eProcessReq, AnonFun2);
            }
        }

        public class ServerMachine : PrtImplMachine
        {
            public override PrtState StartState
            {
                get
                {
                    return ServerMachine_Init;
                }
            }

            public PrtValue var_helper
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

            public override PrtImplMachine MakeSkeleton()
            {
                return new ServerMachine();
            }

            public override int NextInstanceNumber(StateImpl app)
            {
                return app.NextMachineInstanceNumber(this.GetType());
            }

            public override string Name
            {
                get
                {
                    return "ServerMachine";
                }
            }

            public ServerMachine(): base ()
            {
            }

            public ServerMachine(StateImpl app, int maxB, bool assume): base (app, maxB, assume)
            {
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_HelperInterface));
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
                    public ignore_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                    {
                    }

                    public ignore_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    ServerMachine parent = (ServerMachine)(_parent);
                    ignore_StackFrame currFun = (ignore_StackFrame)(parent.PrtPopFunStackFrame());
                    PrtValue swap;
                    parent.PrtFunContReturn((currFun).locals);
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

                public override string ToString()
                {
                    return "ignore";
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
                    public AnonFun0_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                    {
                    }

                    public AnonFun0_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue var__payload_skip
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
                    ServerMachine parent = (ServerMachine)(_parent);
                    AnonFun0_StackFrame currFun = (AnonFun0_StackFrame)(parent.PrtPopFunStackFrame());
                    PrtValue swap;
                    parent.PrtFunContReturn((currFun).locals);
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

                public override string ToString()
                {
                    return "AnonFun0";
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
                    public AnonFun1_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                    {
                    }

                    public AnonFun1_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue var__payload_skip
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
                    ServerMachine parent = (ServerMachine)(_parent);
                    AnonFun1_StackFrame currFun = (AnonFun1_StackFrame)(parent.PrtPopFunStackFrame());
                    PrtValue swap;
                    parent.PrtFunContReturn((currFun).locals);
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

                public override string ToString()
                {
                    return "AnonFun1";
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
                    public AnonFun2_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                    {
                    }

                    public AnonFun2_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue var_payload
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

                    public PrtValue var__payload_2
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
                    ServerMachine parent = (ServerMachine)(_parent);
                    AnonFun2_StackFrame currFun = (AnonFun2_StackFrame)(parent.PrtPopFunStackFrame());
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun2_1;
                    }

                    (((PrtMachineValue)((((PrtTupleValue)((currFun).var_payload)).fieldValues[0]).Clone())).mach).PrtEnqueueEvent((PrtEventValue)(Events.event_eResponse), new PrtNamedTupleValue(Types.type_responseType, (((PrtTupleValue)((currFun).var_payload)).fieldValues[1]).Clone(), new PrtBoolValue(false)), parent, (PrtMachineValue)((((PrtTupleValue)((currFun).var_payload)).fieldValues[0]).Clone()));
                    (parent).PrtFunContSend(this, (currFun).locals, 1);
                    return;
                    AnonFun2_1:
                        ;
                    parent.PrtFunContReturn((currFun).locals);
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

                public override string ToString()
                {
                    return "AnonFun2";
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
                    public AnonFun3_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                    {
                    }

                    public AnonFun3_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue var_payload
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

                    public PrtValue var__payload_1
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
                    ServerMachine parent = (ServerMachine)(_parent);
                    AnonFun3_StackFrame currFun = (AnonFun3_StackFrame)(parent.PrtPopFunStackFrame());
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun3_1;
                    }

                    (((PrtMachineValue)((((PrtTupleValue)((currFun).var_payload)).fieldValues[0]).Clone())).mach).PrtEnqueueEvent((PrtEventValue)(Events.event_eResponse), new PrtNamedTupleValue(Types.type_responseType, (((PrtTupleValue)((currFun).var_payload)).fieldValues[1]).Clone(), new PrtBoolValue(true)), parent, (PrtMachineValue)((((PrtTupleValue)((currFun).var_payload)).fieldValues[0]).Clone()));
                    (parent).PrtFunContSend(this, (currFun).locals, 1);
                    return;
                    AnonFun3_1:
                        ;
                    parent.PrtFunContReturn((currFun).locals);
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

                public override string ToString()
                {
                    return "AnonFun3";
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
                    public AnonFun4_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                    {
                    }

                    public AnonFun4_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue var_payload
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
                    ServerMachine parent = (ServerMachine)(_parent);
                    AnonFun4_StackFrame currFun = (AnonFun4_StackFrame)(parent.PrtPopFunStackFrame());
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun4_1;
                        case 2:
                            goto AnonFun4_2;
                        case 3:
                            goto AnonFun4_3;
                        case 4:
                            goto AnonFun4_4;
                        case 5:
                            goto AnonFun4_5;
                    }

                    (((PrtMachineValue)((parent).var_helper)).mach).PrtEnqueueEvent((PrtEventValue)(Events.event_eProcessReq), (((PrtTupleValue)((currFun).var_payload)).fieldValues[1]).Clone(), parent, (PrtMachineValue)((parent).var_helper));
                    (parent).PrtFunContSend(this, (currFun).locals, 1);
                    return;
                    AnonFun4_1:
                        ;
                    (((PrtImplMachine)(parent)).receiveSet).Add(Events.event_eReqFailed);
                    (((PrtImplMachine)(parent)).receiveSet).Add(Events.event_eReqSuccessful);
                    (parent).PrtFunContReceive(this, (currFun).locals, 2);
                    return;
                    AnonFun4_2:
                        ;
                    if (((parent).currentTrigger).Equals(Events.event_eReqFailed))
                    {
                        (currFun).locals[1] = ((parent).currentPayload).Clone();
                        (parent).PrtPushFunStackFrame(AnonFun2, (currFun).locals);
                        goto AnonFun4_4;
                    }

                    if (((parent).currentTrigger).Equals(Events.event_eReqSuccessful))
                    {
                        (currFun).locals[1] = ((parent).currentPayload).Clone();
                        (parent).PrtPushFunStackFrame(AnonFun3, (currFun).locals);
                        goto AnonFun4_5;
                    }

                    if (!false)
                        throw new PrtAssertFailureException("Internal error");
                    AnonFun4_4:
                        ;
                    (AnonFun2).Execute(application, parent);
                    if (((parent).continuation).reason == PrtContinuationReason.Return)
                        goto AnonFun4_3;
                    else
                    {
                        (parent).PrtPushFunStackFrame((currFun).fun, (currFun).locals, 4);
                        return;
                    }

                    AnonFun4_5:
                        ;
                    (AnonFun3).Execute(application, parent);
                    if (((parent).continuation).reason == PrtContinuationReason.Return)
                        goto AnonFun4_3;
                    else
                    {
                        (parent).PrtPushFunStackFrame((currFun).fun, (currFun).locals, 5);
                        return;
                    }

                    AnonFun4_3:
                        ;
                    parent.PrtFunContReturn((currFun).locals);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    (locals).Add(PrtValue.@null);
                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun4_StackFrame(this, locals, retLoc);
                }

                public override string ToString()
                {
                    return "AnonFun4";
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
                    public AnonFun5_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                    {
                    }

                    public AnonFun5_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue var__payload_skip
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
                    ServerMachine parent = (ServerMachine)(_parent);
                    AnonFun5_StackFrame currFun = (AnonFun5_StackFrame)(parent.PrtPopFunStackFrame());
                    PrtValue swap;
                    parent.PrtFunContReturn((currFun).locals);
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

                public override string ToString()
                {
                    return "AnonFun5";
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
                    public AnonFun6_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                    {
                    }

                    public AnonFun6_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue var__payload_skip
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
                    ServerMachine parent = (ServerMachine)(_parent);
                    AnonFun6_StackFrame currFun = (AnonFun6_StackFrame)(parent.PrtPopFunStackFrame());
                    PrtValue swap;
                    parent.PrtFunContReturn((currFun).locals);
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

                public override string ToString()
                {
                    return "AnonFun6";
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
                    public AnonFun7_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                    {
                    }

                    public AnonFun7_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue var__payload_0
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
                    ServerMachine parent = (ServerMachine)(_parent);
                    AnonFun7_StackFrame currFun = (AnonFun7_StackFrame)(parent.PrtPopFunStackFrame());
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun7_1;
                    }

                    (parent).var_helper = (application).CreateInterface((parent).renamedName, "HelperInterface", PrtValue.PrtConvertValue(parent.self, Types.type_ServerHelperInterface));
                    (parent).PrtFunContNewMachine(this, (currFun).locals, 1);
                    return;
                    AnonFun7_1:
                        ;
                    (application).TraceLine("<GotoLog> Machine {0}-{1} goes to {2}", (parent).Name, (parent).instanceNumber, (ServerMachine_WaitForRequests).name);
                    (parent).currentTrigger = Events.@null;
                    (parent).currentPayload = Events.@null;
                    (parent).destOfGoto = ServerMachine_WaitForRequests;
                    (parent).PrtFunContGoto();
                    return;
                    parent.PrtFunContReturn((currFun).locals);
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

                public override string ToString()
                {
                    return "AnonFun7";
                }
            }

            public static AnonFun7_Class AnonFun7 = new AnonFun7_Class();
            public class ServerMachine_WaitForRequests_Class : PrtState
            {
                public ServerMachine_WaitForRequests_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static ServerMachine_WaitForRequests_Class ServerMachine_WaitForRequests;
            public class ServerMachine_Init_Class : PrtState
            {
                public ServerMachine_Init_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static ServerMachine_Init_Class ServerMachine_Init;
            static ServerMachine()
            {
                ServerMachine_WaitForRequests = new ServerMachine_WaitForRequests_Class("ServerMachine_WaitForRequests", AnonFun0, AnonFun1, false, StateTemperature.Warm);
                ServerMachine_Init = new ServerMachine_Init_Class("ServerMachine_Init", AnonFun7, AnonFun6, false, StateTemperature.Warm);
                ServerMachine_WaitForRequests.dos.Add(Events.event_eRequest, AnonFun4);
            }
        }

        public class ClientMachine : PrtImplMachine
        {
            public override PrtState StartState
            {
                get
                {
                    return ClientMachine_Init;
                }
            }

            public PrtValue var_lastRecvSuccessfulReqId
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

            public PrtValue var_nextReqId
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

            public PrtValue var_server
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
                return new ClientMachine();
            }

            public override int NextInstanceNumber(StateImpl app)
            {
                return app.NextMachineInstanceNumber(this.GetType());
            }

            public override string Name
            {
                get
                {
                    return "ClientMachine";
                }
            }

            public ClientMachine(): base ()
            {
            }

            public ClientMachine(StateImpl app, int maxB, bool assume): base (app, maxB, assume)
            {
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_6_2147315917));
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_6_2147315917));
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_ServerClientInterface));
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
                    public ignore_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                    {
                    }

                    public ignore_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    ClientMachine parent = (ClientMachine)(_parent);
                    ignore_StackFrame currFun = (ignore_StackFrame)(parent.PrtPopFunStackFrame());
                    PrtValue swap;
                    parent.PrtFunContReturn((currFun).locals);
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

                public override string ToString()
                {
                    return "ignore";
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
                    public AnonFun0_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                    {
                    }

                    public AnonFun0_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue var__payload_1
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

                    public PrtValue var_index
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
                    ClientMachine parent = (ClientMachine)(_parent);
                    AnonFun0_StackFrame currFun = (AnonFun0_StackFrame)(parent.PrtPopFunStackFrame());
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun0_1;
                    }

                    (currFun).var_index = (new PrtIntValue(0)).Clone();
                    AnonFun0_loop_start_0:
                        ;
                    if (!((PrtBoolValue)(new PrtBoolValue(((PrtIntValue)((currFun).var_index)).nt < ((PrtIntValue)(new PrtIntValue(2))).nt))).bl)
                        goto AnonFun0_loop_end_0;
                    (((PrtMachineValue)((parent).var_server)).mach).PrtEnqueueEvent((PrtEventValue)(Events.event_eRequest), new PrtNamedTupleValue(Types.type_requestType, PrtValue.PrtConvertValue(parent.self, Types.type_ClientInterface), (parent).var_nextReqId), parent, (PrtMachineValue)((parent).var_server));
                    (parent).PrtFunContSend(this, (currFun).locals, 1);
                    return;
                    AnonFun0_1:
                        ;
                    (parent).var_nextReqId = (new PrtIntValue(((PrtIntValue)((parent).var_nextReqId)).nt + ((PrtIntValue)(new PrtIntValue(1))).nt)).Clone();
                    (currFun).var_index = (new PrtIntValue(((PrtIntValue)((currFun).var_index)).nt + ((PrtIntValue)(new PrtIntValue(1))).nt)).Clone();
                    goto AnonFun0_loop_start_0;
                    AnonFun0_loop_end_0:
                        ;
                    parent.PrtFunContReturn((currFun).locals);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    (locals).Add(PrtValue.PrtMkDefaultValue(Types.type_6_2147315917));
                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun0_StackFrame(this, locals, retLoc);
                }

                public override string ToString()
                {
                    return "AnonFun0";
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
                    public AnonFun1_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                    {
                    }

                    public AnonFun1_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue var__payload_skip
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
                    ClientMachine parent = (ClientMachine)(_parent);
                    AnonFun1_StackFrame currFun = (AnonFun1_StackFrame)(parent.PrtPopFunStackFrame());
                    PrtValue swap;
                    parent.PrtFunContReturn((currFun).locals);
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

                public override string ToString()
                {
                    return "AnonFun1";
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
                    public AnonFun2_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                    {
                    }

                    public AnonFun2_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue var_payload
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
                    ClientMachine parent = (ClientMachine)(_parent);
                    AnonFun2_StackFrame currFun = (AnonFun2_StackFrame)(parent.PrtPopFunStackFrame());
                    PrtValue swap;
                    if (!((PrtBoolValue)(new PrtBoolValue(((PrtIntValue)((((PrtTupleValue)((currFun).var_payload)).fieldValues[0]).Clone())).nt > ((PrtIntValue)((parent).var_lastRecvSuccessfulReqId)).nt))).bl)
                        throw new PrtAssertFailureException("Client.p(40,9,40,15): error PC1001: Assert failed");
                    (parent).var_lastRecvSuccessfulReqId = ((((PrtTupleValue)((currFun).var_payload)).fieldValues[0]).Clone()).Clone();
                    parent.PrtFunContReturn((currFun).locals);
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

                public override string ToString()
                {
                    return "AnonFun2";
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
                    public AnonFun3_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                    {
                    }

                    public AnonFun3_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue var_payload
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
                    ClientMachine parent = (ClientMachine)(_parent);
                    AnonFun3_StackFrame currFun = (AnonFun3_StackFrame)(parent.PrtPopFunStackFrame());
                    PrtValue swap;
                    (parent).var_nextReqId = (new PrtIntValue(1)).Clone();
                    (parent).var_server = ((currFun).var_payload).Clone();
                    (application).TraceLine("<GotoLog> Machine {0}-{1} goes to {2}", (parent).Name, (parent).instanceNumber, (ClientMachine_StartPumpingRequests).name);
                    (parent).currentTrigger = Events.@null;
                    (parent).currentPayload = Events.@null;
                    (parent).destOfGoto = ClientMachine_StartPumpingRequests;
                    (parent).PrtFunContGoto();
                    return;
                    parent.PrtFunContReturn((currFun).locals);
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

                public override string ToString()
                {
                    return "AnonFun3";
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
                    public AnonFun4_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                    {
                    }

                    public AnonFun4_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue var__payload_skip
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
                    ClientMachine parent = (ClientMachine)(_parent);
                    AnonFun4_StackFrame currFun = (AnonFun4_StackFrame)(parent.PrtPopFunStackFrame());
                    PrtValue swap;
                    parent.PrtFunContReturn((currFun).locals);
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

                public override string ToString()
                {
                    return "AnonFun4";
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
                    public AnonFun5_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                    {
                    }

                    public AnonFun5_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue var__payload_0
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
                    ClientMachine parent = (ClientMachine)(_parent);
                    AnonFun5_StackFrame currFun = (AnonFun5_StackFrame)(parent.PrtPopFunStackFrame());
                    PrtValue swap;
                    parent.PrtFunContReturn((currFun).locals);
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

                public override string ToString()
                {
                    return "AnonFun5";
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
                    public AnonFun6_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                    {
                    }

                    public AnonFun6_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue var__payload_skip
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
                    ClientMachine parent = (ClientMachine)(_parent);
                    AnonFun6_StackFrame currFun = (AnonFun6_StackFrame)(parent.PrtPopFunStackFrame());
                    PrtValue swap;
                    parent.PrtFunContReturn((currFun).locals);
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

                public override string ToString()
                {
                    return "AnonFun6";
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
                    public AnonFun7_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                    {
                    }

                    public AnonFun7_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue var__payload_skip
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
                    ClientMachine parent = (ClientMachine)(_parent);
                    AnonFun7_StackFrame currFun = (AnonFun7_StackFrame)(parent.PrtPopFunStackFrame());
                    PrtValue swap;
                    parent.PrtFunContReturn((currFun).locals);
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

                public override string ToString()
                {
                    return "AnonFun7";
                }
            }

            public static AnonFun7_Class AnonFun7 = new AnonFun7_Class();
            public class ClientMachine_StartPumpingRequests_Class : PrtState
            {
                public ClientMachine_StartPumpingRequests_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static ClientMachine_StartPumpingRequests_Class ClientMachine_StartPumpingRequests;
            public class ClientMachine_Init_Class : PrtState
            {
                public ClientMachine_Init_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static ClientMachine_Init_Class ClientMachine_Init;
            static ClientMachine()
            {
                ClientMachine_StartPumpingRequests = new ClientMachine_StartPumpingRequests_Class("ClientMachine_StartPumpingRequests", AnonFun0, AnonFun1, false, StateTemperature.Warm);
                ClientMachine_Init = new ClientMachine_Init_Class("ClientMachine_Init", AnonFun3, AnonFun5, false, StateTemperature.Warm);
                ClientMachine_StartPumpingRequests.dos.Add(Events.event_eResponse, AnonFun2);
            }
        }

        public class UNSOUNDAbstractServerMachine : PrtImplMachine
        {
            public override PrtState StartState
            {
                get
                {
                    return UNSOUNDAbstractServerMachine_Init;
                }
            }

            public override PrtImplMachine MakeSkeleton()
            {
                return new UNSOUNDAbstractServerMachine();
            }

            public override int NextInstanceNumber(StateImpl app)
            {
                return app.NextMachineInstanceNumber(this.GetType());
            }

            public override string Name
            {
                get
                {
                    return "UNSOUNDAbstractServerMachine";
                }
            }

            public UNSOUNDAbstractServerMachine(): base ()
            {
            }

            public UNSOUNDAbstractServerMachine(StateImpl app, int maxB, bool assume): base (app, maxB, assume)
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
                    public ignore_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                    {
                    }

                    public ignore_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    UNSOUNDAbstractServerMachine parent = (UNSOUNDAbstractServerMachine)(_parent);
                    ignore_StackFrame currFun = (ignore_StackFrame)(parent.PrtPopFunStackFrame());
                    PrtValue swap;
                    parent.PrtFunContReturn((currFun).locals);
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

                public override string ToString()
                {
                    return "ignore";
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
                    public AnonFun0_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                    {
                    }

                    public AnonFun0_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue var__payload_skip
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
                    UNSOUNDAbstractServerMachine parent = (UNSOUNDAbstractServerMachine)(_parent);
                    AnonFun0_StackFrame currFun = (AnonFun0_StackFrame)(parent.PrtPopFunStackFrame());
                    PrtValue swap;
                    parent.PrtFunContReturn((currFun).locals);
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

                public override string ToString()
                {
                    return "AnonFun0";
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
                    public AnonFun1_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                    {
                    }

                    public AnonFun1_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue var__payload_skip
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
                    UNSOUNDAbstractServerMachine parent = (UNSOUNDAbstractServerMachine)(_parent);
                    AnonFun1_StackFrame currFun = (AnonFun1_StackFrame)(parent.PrtPopFunStackFrame());
                    PrtValue swap;
                    parent.PrtFunContReturn((currFun).locals);
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

                public override string ToString()
                {
                    return "AnonFun1";
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
                    public AnonFun2_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                    {
                    }

                    public AnonFun2_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue var_payload
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

                    public PrtValue var_successful
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
                    UNSOUNDAbstractServerMachine parent = (UNSOUNDAbstractServerMachine)(_parent);
                    AnonFun2_StackFrame currFun = (AnonFun2_StackFrame)(parent.PrtPopFunStackFrame());
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun2_1;
                    }

                    (currFun).var_successful = (new PrtBoolValue(true)).Clone();
                    (((PrtMachineValue)((((PrtTupleValue)((currFun).var_payload)).fieldValues[0]).Clone())).mach).PrtEnqueueEvent((PrtEventValue)(Events.event_eResponse), new PrtNamedTupleValue(Types.type_responseType, (((PrtTupleValue)((currFun).var_payload)).fieldValues[1]).Clone(), (currFun).var_successful), parent, (PrtMachineValue)((((PrtTupleValue)((currFun).var_payload)).fieldValues[0]).Clone()));
                    (parent).PrtFunContSend(this, (currFun).locals, 1);
                    return;
                    AnonFun2_1:
                        ;
                    parent.PrtFunContReturn((currFun).locals);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    (locals).Add(PrtValue.PrtMkDefaultValue(Types.type_7_2147315917));
                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun2_StackFrame(this, locals, retLoc);
                }

                public override string ToString()
                {
                    return "AnonFun2";
                }
            }

            public static AnonFun2_Class AnonFun2 = new AnonFun2_Class();
            public class UNSOUNDAbstractServerMachine_Init_Class : PrtState
            {
                public UNSOUNDAbstractServerMachine_Init_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static UNSOUNDAbstractServerMachine_Init_Class UNSOUNDAbstractServerMachine_Init;
            static UNSOUNDAbstractServerMachine()
            {
                UNSOUNDAbstractServerMachine_Init = new UNSOUNDAbstractServerMachine_Init_Class("UNSOUNDAbstractServerMachine_Init", AnonFun0, AnonFun1, false, StateTemperature.Warm);
                UNSOUNDAbstractServerMachine_Init.dos.Add(Events.event_eRequest, AnonFun2);
            }
        }

        public class RespIdsAreMonotonicallyIncreasing : PrtSpecMachine
        {
            public override PrtState StartState
            {
                get
                {
                    return RespIdsAreMonotonicallyIncreasing_Init;
                }
            }

            public PrtValue var_previousId
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
                return new RespIdsAreMonotonicallyIncreasing();
            }

            public override string Name
            {
                get
                {
                    return "RespIdsAreMonotonicallyIncreasing";
                }
            }

            public RespIdsAreMonotonicallyIncreasing(): base ()
            {
            }

            public RespIdsAreMonotonicallyIncreasing(StateImpl app): base (app)
            {
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_6_2147315917));
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
                    public ignore_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                    {
                    }

                    public ignore_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    RespIdsAreMonotonicallyIncreasing parent = (RespIdsAreMonotonicallyIncreasing)(_parent);
                    ignore_StackFrame currFun = (ignore_StackFrame)(parent.PrtPopFunStackFrame());
                    PrtValue swap;
                    parent.PrtFunContReturn((currFun).locals);
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

                public override string ToString()
                {
                    return "ignore";
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
                    public AnonFun0_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                    {
                    }

                    public AnonFun0_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue var__payload_skip
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
                    RespIdsAreMonotonicallyIncreasing parent = (RespIdsAreMonotonicallyIncreasing)(_parent);
                    AnonFun0_StackFrame currFun = (AnonFun0_StackFrame)(parent.PrtPopFunStackFrame());
                    PrtValue swap;
                    parent.PrtFunContReturn((currFun).locals);
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

                public override string ToString()
                {
                    return "AnonFun0";
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
                    public AnonFun1_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                    {
                    }

                    public AnonFun1_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue var__payload_skip
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
                    RespIdsAreMonotonicallyIncreasing parent = (RespIdsAreMonotonicallyIncreasing)(_parent);
                    AnonFun1_StackFrame currFun = (AnonFun1_StackFrame)(parent.PrtPopFunStackFrame());
                    PrtValue swap;
                    parent.PrtFunContReturn((currFun).locals);
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

                public override string ToString()
                {
                    return "AnonFun1";
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
                    public AnonFun2_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                    {
                    }

                    public AnonFun2_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue var_payload
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
                    RespIdsAreMonotonicallyIncreasing parent = (RespIdsAreMonotonicallyIncreasing)(_parent);
                    AnonFun2_StackFrame currFun = (AnonFun2_StackFrame)(parent.PrtPopFunStackFrame());
                    PrtValue swap;
                    if (!((PrtBoolValue)(new PrtBoolValue(((((PrtTupleValue)((currFun).var_payload)).fieldValues[0]).Clone()).Equals(new PrtIntValue(((PrtIntValue)((parent).var_previousId)).nt + ((PrtIntValue)(new PrtIntValue(1))).nt))))).bl)
                        throw new PrtAssertFailureException("AbstractionAndSpec.p(28,9,28,15): error PC1001: Assert failed");
                    (parent).var_previousId = ((((PrtTupleValue)((currFun).var_payload)).fieldValues[0]).Clone()).Clone();
                    parent.PrtFunContReturn((currFun).locals);
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

                public override string ToString()
                {
                    return "AnonFun2";
                }
            }

            public static AnonFun2_Class AnonFun2 = new AnonFun2_Class();
            public class RespIdsAreMonotonicallyIncreasing_Init_Class : PrtState
            {
                public RespIdsAreMonotonicallyIncreasing_Init_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static RespIdsAreMonotonicallyIncreasing_Init_Class RespIdsAreMonotonicallyIncreasing_Init;
            static RespIdsAreMonotonicallyIncreasing()
            {
                RespIdsAreMonotonicallyIncreasing_Init = new RespIdsAreMonotonicallyIncreasing_Init_Class("RespIdsAreMonotonicallyIncreasing_Init", AnonFun0, AnonFun1, false, StateTemperature.Warm);
                RespIdsAreMonotonicallyIncreasing_Init.dos.Add(Events.event_eResponse, AnonFun2);
            }
        }

        public class ReqIdsAreMonotonicallyIncreasing : PrtSpecMachine
        {
            public override PrtState StartState
            {
                get
                {
                    return ReqIdsAreMonotonicallyIncreasing_Init;
                }
            }

            public PrtValue var_previousId
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
                return new ReqIdsAreMonotonicallyIncreasing();
            }

            public override string Name
            {
                get
                {
                    return "ReqIdsAreMonotonicallyIncreasing";
                }
            }

            public ReqIdsAreMonotonicallyIncreasing(): base ()
            {
            }

            public ReqIdsAreMonotonicallyIncreasing(StateImpl app): base (app)
            {
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_6_2147315917));
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
                    public ignore_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                    {
                    }

                    public ignore_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    ReqIdsAreMonotonicallyIncreasing parent = (ReqIdsAreMonotonicallyIncreasing)(_parent);
                    ignore_StackFrame currFun = (ignore_StackFrame)(parent.PrtPopFunStackFrame());
                    PrtValue swap;
                    parent.PrtFunContReturn((currFun).locals);
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

                public override string ToString()
                {
                    return "ignore";
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
                    public AnonFun0_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                    {
                    }

                    public AnonFun0_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue var__payload_skip
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
                    ReqIdsAreMonotonicallyIncreasing parent = (ReqIdsAreMonotonicallyIncreasing)(_parent);
                    AnonFun0_StackFrame currFun = (AnonFun0_StackFrame)(parent.PrtPopFunStackFrame());
                    PrtValue swap;
                    parent.PrtFunContReturn((currFun).locals);
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

                public override string ToString()
                {
                    return "AnonFun0";
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
                    public AnonFun1_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                    {
                    }

                    public AnonFun1_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue var__payload_skip
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
                    ReqIdsAreMonotonicallyIncreasing parent = (ReqIdsAreMonotonicallyIncreasing)(_parent);
                    AnonFun1_StackFrame currFun = (AnonFun1_StackFrame)(parent.PrtPopFunStackFrame());
                    PrtValue swap;
                    parent.PrtFunContReturn((currFun).locals);
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

                public override string ToString()
                {
                    return "AnonFun1";
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
                    public AnonFun2_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                    {
                    }

                    public AnonFun2_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue var_payload
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
                    ReqIdsAreMonotonicallyIncreasing parent = (ReqIdsAreMonotonicallyIncreasing)(_parent);
                    AnonFun2_StackFrame currFun = (AnonFun2_StackFrame)(parent.PrtPopFunStackFrame());
                    PrtValue swap;
                    if (!((PrtBoolValue)(new PrtBoolValue(((((PrtTupleValue)((currFun).var_payload)).fieldValues[1]).Clone()).Equals(new PrtIntValue(((PrtIntValue)((parent).var_previousId)).nt + ((PrtIntValue)(new PrtIntValue(1))).nt))))).bl)
                        throw new PrtAssertFailureException("AbstractionAndSpec.p(18,9,18,15): error PC1001: Assert failed");
                    (parent).var_previousId = ((((PrtTupleValue)((currFun).var_payload)).fieldValues[1]).Clone()).Clone();
                    parent.PrtFunContReturn((currFun).locals);
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

                public override string ToString()
                {
                    return "AnonFun2";
                }
            }

            public static AnonFun2_Class AnonFun2 = new AnonFun2_Class();
            public class ReqIdsAreMonotonicallyIncreasing_Init_Class : PrtState
            {
                public ReqIdsAreMonotonicallyIncreasing_Init_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static ReqIdsAreMonotonicallyIncreasing_Init_Class ReqIdsAreMonotonicallyIncreasing_Init;
            static ReqIdsAreMonotonicallyIncreasing()
            {
                ReqIdsAreMonotonicallyIncreasing_Init = new ReqIdsAreMonotonicallyIncreasing_Init_Class("ReqIdsAreMonotonicallyIncreasing_Init", AnonFun0, AnonFun1, false, StateTemperature.Warm);
                ReqIdsAreMonotonicallyIncreasing_Init.dos.Add(Events.event_eRequest, AnonFun2);
            }
        }

        public class AbstractServerMachine : PrtImplMachine
        {
            public override PrtState StartState
            {
                get
                {
                    return AbstractServerMachine_Init;
                }
            }

            public override PrtImplMachine MakeSkeleton()
            {
                return new AbstractServerMachine();
            }

            public override int NextInstanceNumber(StateImpl app)
            {
                return app.NextMachineInstanceNumber(this.GetType());
            }

            public override string Name
            {
                get
                {
                    return "AbstractServerMachine";
                }
            }

            public AbstractServerMachine(): base ()
            {
            }

            public AbstractServerMachine(StateImpl app, int maxB, bool assume): base (app, maxB, assume)
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
                    public ignore_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                    {
                    }

                    public ignore_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    AbstractServerMachine parent = (AbstractServerMachine)(_parent);
                    ignore_StackFrame currFun = (ignore_StackFrame)(parent.PrtPopFunStackFrame());
                    PrtValue swap;
                    parent.PrtFunContReturn((currFun).locals);
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

                public override string ToString()
                {
                    return "ignore";
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
                    public AnonFun0_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                    {
                    }

                    public AnonFun0_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue var__payload_skip
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
                    AbstractServerMachine parent = (AbstractServerMachine)(_parent);
                    AnonFun0_StackFrame currFun = (AnonFun0_StackFrame)(parent.PrtPopFunStackFrame());
                    PrtValue swap;
                    parent.PrtFunContReturn((currFun).locals);
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

                public override string ToString()
                {
                    return "AnonFun0";
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
                    public AnonFun1_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                    {
                    }

                    public AnonFun1_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue var__payload_skip
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
                    AbstractServerMachine parent = (AbstractServerMachine)(_parent);
                    AnonFun1_StackFrame currFun = (AnonFun1_StackFrame)(parent.PrtPopFunStackFrame());
                    PrtValue swap;
                    parent.PrtFunContReturn((currFun).locals);
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

                public override string ToString()
                {
                    return "AnonFun1";
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
                    public AnonFun2_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                    {
                    }

                    public AnonFun2_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue var_payload
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

                    public PrtValue var_successful
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
                    AbstractServerMachine parent = (AbstractServerMachine)(_parent);
                    AnonFun2_StackFrame currFun = (AnonFun2_StackFrame)(parent.PrtPopFunStackFrame());
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun2_1;
                    }

                    (currFun).var_successful = (new PrtBoolValue((application).GetSelectedChoiceValue((PrtImplMachine)(parent)))).Clone();
                    (((PrtMachineValue)((((PrtTupleValue)((currFun).var_payload)).fieldValues[0]).Clone())).mach).PrtEnqueueEvent((PrtEventValue)(Events.event_eResponse), new PrtNamedTupleValue(Types.type_responseType, (((PrtTupleValue)((currFun).var_payload)).fieldValues[1]).Clone(), (currFun).var_successful), parent, (PrtMachineValue)((((PrtTupleValue)((currFun).var_payload)).fieldValues[0]).Clone()));
                    (parent).PrtFunContSend(this, (currFun).locals, 1);
                    return;
                    AnonFun2_1:
                        ;
                    parent.PrtFunContReturn((currFun).locals);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    (locals).Add(PrtValue.PrtMkDefaultValue(Types.type_7_2147315917));
                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun2_StackFrame(this, locals, retLoc);
                }

                public override string ToString()
                {
                    return "AnonFun2";
                }
            }

            public static AnonFun2_Class AnonFun2 = new AnonFun2_Class();
            public class AbstractServerMachine_Init_Class : PrtState
            {
                public AbstractServerMachine_Init_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static AbstractServerMachine_Init_Class AbstractServerMachine_Init;
            static AbstractServerMachine()
            {
                AbstractServerMachine_Init = new AbstractServerMachine_Init_Class("AbstractServerMachine_Init", AnonFun0, AnonFun1, false, StateTemperature.Warm);
                AbstractServerMachine_Init.dos.Add(Events.event_eRequest, AnonFun2);
            }
        }
    }
}
