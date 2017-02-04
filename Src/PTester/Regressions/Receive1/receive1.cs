#pragma warning disable CS0162, CS0164, CS0168, CS0649
namespace P.Program
{
    using P.Runtime;
    using System;
    using System.Collections.Generic;

    public partial class Application : StateImpl
    {
        private partial class Events
        {
            public static PrtEventValue G;
            public static PrtEventValue F;
            public static PrtEventValue E;
            static public void Events_receive1()
            {
                G = new PrtEventValue(new PrtEvent("G", Types.type_4_1235668830, PrtEvent.DefaultMaxInstances, false));
                F = new PrtEventValue(new PrtEvent("F", Types.type_0_1235668830, PrtEvent.DefaultMaxInstances, false));
                E = new PrtEventValue(new PrtEvent("E", Types.type_4_1235668830, PrtEvent.DefaultMaxInstances, false));
            }
        }

        private partial class Types
        {
            public static PrtType type_0_1235668830;
            public static PrtType type_1_1235668830;
            public static PrtType type_2_1235668830;
            public static PrtType type_3_1235668830;
            public static PrtType type_4_1235668830;
            public static PrtType type_5_1235668830;
            public static PrtType type_6_1235668830;
            public static PrtType type_7_1235668830;
            public static PrtType type_8_1235668830;
            static public void Types_receive1()
            {
                Types.type_0_1235668830 = new PrtNullType();
                Types.type_1_1235668830 = new PrtAnyType();
                Types.type_2_1235668830 = new PrtMachineType();
                Types.type_3_1235668830 = new PrtEventType();
                Types.type_4_1235668830 = new PrtIntType();
                Types.type_5_1235668830 = new PrtBoolType();
                Types.type_6_1235668830 = new PrtNamedTupleType(new object[]{"y", Types.type_2_1235668830, "z", Types.type_4_1235668830});
                Types.type_7_1235668830 = new PrtNamedTupleType(new object[]{"x", Types.type_4_1235668830, "a", Types.type_4_1235668830, "b", Types.type_4_1235668830, "c", Types.type_3_1235668830});
                Types.type_8_1235668830 = new PrtNamedTupleType(new object[]{"b", Types.type_2_1235668830});
            }
        }

        public static PrtImplMachine CreateMachine_B(StateImpl application, PrtValue payload)
        {
            var machine = new B(application, PrtImplMachine.DefaultMaxBufferSize, false);
            (application).Trace("<CreateLog> Created Machine B-{0}", (machine).instanceNumber);
            (((machine).self).permissions).Add(Events.G);
            (((machine).self).permissions).Add(Events.F);
            (((machine).self).permissions).Add(Events.E);
            (((machine).self).permissions).Add(Events.halt);
            ((machine).sends).Add(Events.E);
            ((machine).sends).Add(Events.halt);
            ((machine).sends).Add(Events.F);
            ((machine).sends).Add(Events.G);
            (machine).currentPayload = payload;
            return machine;
        }

        public static PrtImplMachine CreateMachine_Main(StateImpl application, PrtValue payload)
        {
            var machine = new Main(application, PrtImplMachine.DefaultMaxBufferSize, false);
            (application).Trace("<CreateLog> Created Machine Main-{0}", (machine).instanceNumber);
            (((machine).self).permissions).Add(Events.G);
            (((machine).self).permissions).Add(Events.F);
            (((machine).self).permissions).Add(Events.E);
            (((machine).self).permissions).Add(Events.halt);
            ((machine).sends).Add(Events.E);
            ((machine).sends).Add(Events.halt);
            ((machine).sends).Add(Events.F);
            ((machine).sends).Add(Events.G);
            (machine).currentPayload = payload;
            return machine;
        }

        public class B : PrtImplMachine
        {
            public override PrtState StartState
            {
                get
                {
                    return B_Init;
                }
            }

            public override PrtImplMachine MakeSkeleton()
            {
                return new B();
            }

            public override int NextInstanceNumber(StateImpl app)
            {
                return app.NextMachineInstanceNumber(this.GetType());
            }

            public override string Name
            {
                get
                {
                    return "B";
                }
            }

            public B(): base ()
            {
            }

            public B(StateImpl app, int maxB, bool assume): base (app, maxB, assume)
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
                    B parent = (B)(_parent);
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
                    B parent = (B)(_parent);
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

                    public PrtValue payload1
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

                    public PrtValue y
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

                    public PrtValue z
                    {
                        get
                        {
                            return locals[2];
                        }

                        set
                        {
                            locals[2] = value;
                        }
                    }

                    public PrtValue payload2
                    {
                        get
                        {
                            return locals[3];
                        }

                        set
                        {
                            locals[3] = value;
                        }
                    }

                    public PrtValue payload3
                    {
                        get
                        {
                            return locals[4];
                        }

                        set
                        {
                            locals[4] = value;
                        }
                    }

                    public PrtValue x
                    {
                        get
                        {
                            return locals[5];
                        }

                        set
                        {
                            locals[5] = value;
                        }
                    }

                    public PrtValue a
                    {
                        get
                        {
                            return locals[6];
                        }

                        set
                        {
                            locals[6] = value;
                        }
                    }

                    public PrtValue b
                    {
                        get
                        {
                            return locals[7];
                        }

                        set
                        {
                            locals[7] = value;
                        }
                    }

                    public PrtValue c
                    {
                        get
                        {
                            return locals[8];
                        }

                        set
                        {
                            locals[8] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    B parent = (B)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun1_1;
                    }

                    (currFun).locals[5] = ((currFun).locals[4]).Clone();
                    (((PrtMachineValue)((currFun).locals[1])).mach).PrtEnqueueEvent((PrtEventValue)(Events.G), (currFun).locals[5], parent, (PrtMachineValue)((currFun).locals[1]));
                    (parent).PrtFunContSend(this, (currFun).locals, 1);
                    return;
                    AnonFun1_1:
                        ;
                    (currFun).locals[6] = (new PrtIntValue(10)).Clone();
                    (currFun).locals[7] = (new PrtIntValue(11)).Clone();
                    if (!((PrtBoolValue)(new PrtBoolValue(((currFun).locals[7]).Equals(new PrtIntValue(((PrtIntValue)((currFun).locals[6])).nt + ((PrtIntValue)((currFun).locals[2])).nt))))).bl)
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Src\\\\PTester\\\\Regressions\\\\Receive1\\\\receive1.p (49, 8): Assert failed");
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    (locals).Add(PrtValue.PrtMkDefaultValue(Types.type_4_1235668830));
                    (locals).Add(PrtValue.PrtMkDefaultValue(Types.type_4_1235668830));
                    (locals).Add(PrtValue.PrtMkDefaultValue(Types.type_4_1235668830));
                    (locals).Add(PrtValue.PrtMkDefaultValue(Types.type_3_1235668830));
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

                    public PrtValue payload1
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

                    public PrtValue y
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

                    public PrtValue z
                    {
                        get
                        {
                            return locals[2];
                        }

                        set
                        {
                            locals[2] = value;
                        }
                    }

                    public PrtValue payload2
                    {
                        get
                        {
                            return locals[3];
                        }

                        set
                        {
                            locals[3] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    B parent = (B)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun2_1;
                        case 2:
                            goto AnonFun2_2;
                        case 3:
                            goto AnonFun2_3;
                    }

                    if (!((PrtBoolValue)(new PrtBoolValue(((currFun).locals[3]).Equals(new PrtIntValue(0))))).bl)
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Src\\\\PTester\\\\Regressions\\\\Receive1\\\\receive1.p (38, 6): Assert failed");
                    (((PrtImplMachine)(parent)).receiveSet).Add(Events.G);
                    (parent).PrtFunContReceive(this, (currFun).locals, 1);
                    return;
                    AnonFun2_1:
                        ;
                    if ((parent).currentTrigger == Events.G)
                    {
                        (currFun).locals[4] = ((parent).currentPayload).Clone();
                        (currFun).locals[5] = PrtValue.PrtMkDefaultValue(Types.type_4_1235668830);
                        (currFun).locals[6] = PrtValue.PrtMkDefaultValue(Types.type_4_1235668830);
                        (currFun).locals[7] = PrtValue.PrtMkDefaultValue(Types.type_4_1235668830);
                        (currFun).locals[8] = PrtValue.PrtMkDefaultValue(Types.type_3_1235668830);
                        (parent).PrtPushFunStackFrame(AnonFun1, (currFun).locals);
                        goto AnonFun2_3;
                    }

                    if (!false)
                        throw new PrtAssertFailureException("Internal error");
                    AnonFun2_3:
                        ;
                    (AnonFun1).Execute(application, parent);
                    if (((parent).continuation).reason == PrtContinuationReason.Return)
                        goto AnonFun2_2;
                    else
                    {
                        (parent).PrtPushFunStackFrame((currFun).fun, (currFun).locals, 3);
                        return;
                    }

                    AnonFun2_2:
                        ;
                    if (!((PrtBoolValue)(new PrtBoolValue(((currFun).locals[3]).Equals(new PrtIntValue(0))))).bl)
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Src\\\\PTester\\\\Regressions\\\\Receive1\\\\receive1.p (52, 6): Assert failed");
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    (locals).Add(PrtValue.@null);
                    (locals).Add(PrtValue.@null);
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

                    public PrtValue payload1
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

                    public PrtValue y
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

                    public PrtValue z
                    {
                        get
                        {
                            return locals[2];
                        }

                        set
                        {
                            locals[2] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    B parent = (B)(_parent);
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

                    (currFun).locals[2] = (new PrtIntValue(((PrtIntValue)((currFun).locals[2])).nt + ((PrtIntValue)(new PrtIntValue(1))).nt)).Clone();
                    (currFun).locals[1] = ((currFun).locals[0]).Clone();
                    (((PrtImplMachine)(parent)).receiveSet).Add(Events.E);
                    (parent).PrtFunContReceive(this, (currFun).locals, 1);
                    return;
                    AnonFun3_1:
                        ;
                    if ((parent).currentTrigger == Events.E)
                    {
                        (currFun).locals[3] = ((parent).currentPayload).Clone();
                        (parent).PrtPushFunStackFrame(AnonFun2, (currFun).locals);
                        goto AnonFun3_3;
                    }

                    if (!false)
                        throw new PrtAssertFailureException("Internal error");
                    AnonFun3_3:
                        ;
                    (AnonFun2).Execute(application, parent);
                    if (((parent).continuation).reason == PrtContinuationReason.Return)
                        goto AnonFun3_2;
                    else
                    {
                        (parent).PrtPushFunStackFrame((currFun).fun, (currFun).locals, 3);
                        return;
                    }

                    AnonFun3_2:
                        ;
                    if (!((PrtBoolValue)(new PrtBoolValue(((currFun).locals[1]).Equals((currFun).locals[0])))).bl)
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Src\\\\PTester\\\\Regressions\\\\Receive1\\\\receive1.p (55, 4): Assert failed");
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    (locals).Add(PrtValue.PrtMkDefaultValue(Types.type_2_1235668830));
                    (locals).Add(PrtValue.PrtMkDefaultValue(Types.type_4_1235668830));
                    (locals).Add(PrtValue.@null);
                    (locals).Add(PrtValue.@null);
                    (locals).Add(PrtValue.@null);
                    (locals).Add(PrtValue.@null);
                    (locals).Add(PrtValue.@null);
                    (locals).Add(PrtValue.@null);
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
                    B parent = (B)(_parent);
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
            public class B_Init_Class : PrtState
            {
                public B_Init_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static B_Init_Class B_Init;
            static B()
            {
                ignore = new ignore_Class();
                AnonFun0 = new AnonFun0_Class();
                AnonFun1 = new AnonFun1_Class();
                AnonFun2 = new AnonFun2_Class();
                AnonFun3 = new AnonFun3_Class();
                AnonFun4 = new AnonFun4_Class();
                B_Init = new B_Init_Class("B_Init", AnonFun3, AnonFun4, false, StateTemperature.Warm);
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

            public PrtValue x
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
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_4_1235668830));
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
            public class foo_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return false;
                    }
                }

                internal class foo_StackFrame : PrtFunStackFrame
                {
                    public foo_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public foo_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue b
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

                    public PrtValue p
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
                    Main parent = (Main)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto foo_1;
                        case 2:
                            goto foo_2;
                        case 3:
                            goto foo_3;
                        case 4:
                            goto foo_4;
                        case 5:
                            goto foo_5;
                        case 6:
                            goto foo_6;
                        case 7:
                            goto foo_7;
                    }

                    (((PrtMachineValue)((currFun).locals[0])).mach).PrtEnqueueEvent((PrtEventValue)(Events.E), new PrtIntValue(0), parent, (PrtMachineValue)((currFun).locals[0]));
                    (parent).PrtFunContSend(this, (currFun).locals, 1);
                    return;
                    foo_1:
                        ;
                    (((PrtMachineValue)((currFun).locals[0])).mach).PrtEnqueueEvent((PrtEventValue)(Events.G), new PrtIntValue(1), parent, (PrtMachineValue)((currFun).locals[0]));
                    (parent).PrtFunContSend(this, (currFun).locals, 2);
                    return;
                    foo_2:
                        ;
                    (((PrtImplMachine)(parent)).receiveSet).Add(Events.G);
                    (((PrtImplMachine)(parent)).receiveSet).Add(Events.F);
                    (((PrtImplMachine)(parent)).receiveSet).Add(Events.E);
                    (parent).PrtFunContReceive(this, (currFun).locals, 3);
                    return;
                    foo_3:
                        ;
                    if ((parent).currentTrigger == Events.G)
                    {
                        (currFun).locals[2] = ((parent).currentPayload).Clone();
                        (parent).PrtPushFunStackFrame(AnonFun1, (currFun).locals);
                        goto foo_5;
                    }

                    if ((parent).currentTrigger == Events.F)
                    {
                        (currFun).locals[2] = ((parent).currentPayload).Clone();
                        (parent).PrtPushFunStackFrame(AnonFun2, (currFun).locals);
                        goto foo_6;
                    }

                    if ((parent).currentTrigger == Events.E)
                    {
                        (currFun).locals[2] = ((parent).currentPayload).Clone();
                        (parent).PrtPushFunStackFrame(AnonFun3, (currFun).locals);
                        goto foo_7;
                    }

                    if (!false)
                        throw new PrtAssertFailureException("Internal error");
                    foo_5:
                        ;
                    (AnonFun1).Execute(application, parent);
                    if (((parent).continuation).reason == PrtContinuationReason.Return)
                        goto foo_4;
                    else
                    {
                        (parent).PrtPushFunStackFrame((currFun).fun, (currFun).locals, 5);
                        return;
                    }

                    foo_6:
                        ;
                    (AnonFun2).Execute(application, parent);
                    if (((parent).continuation).reason == PrtContinuationReason.Return)
                        goto foo_4;
                    else
                    {
                        (parent).PrtPushFunStackFrame((currFun).fun, (currFun).locals, 6);
                        return;
                    }

                    foo_7:
                        ;
                    (AnonFun3).Execute(application, parent);
                    if (((parent).continuation).reason == PrtContinuationReason.Return)
                        goto foo_4;
                    else
                    {
                        (parent).PrtPushFunStackFrame((currFun).fun, (currFun).locals, 7);
                        return;
                    }

                    foo_4:
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

                    (locals).Add(PrtValue.@null);
                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new foo_StackFrame(this, locals, retLoc);
                }
            }

            public static foo_Class foo;
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

                    public PrtValue b
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

                    public PrtValue p
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

                    public PrtValue payload
                    {
                        get
                        {
                            return locals[2];
                        }

                        set
                        {
                            locals[2] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    Main parent = (Main)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    (parent).x = (new PrtIntValue(((PrtIntValue)(new PrtIntValue(((PrtIntValue)((parent).x)).nt + ((PrtIntValue)((currFun).locals[1])).nt))).nt + ((PrtIntValue)((currFun).locals[2])).nt)).Clone();
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

                    public PrtValue b
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

                    public PrtValue p
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

                    public PrtValue _payload_1
                    {
                        get
                        {
                            return locals[2];
                        }

                        set
                        {
                            locals[2] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    Main parent = (Main)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    (parent).x = (new PrtIntValue(((PrtIntValue)(new PrtIntValue(((PrtIntValue)((parent).x)).nt + ((PrtIntValue)((currFun).locals[1])).nt))).nt + ((PrtIntValue)(new PrtIntValue(2))).nt)).Clone();
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

                    public PrtValue b
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

                    public PrtValue p
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

                    public PrtValue payload
                    {
                        get
                        {
                            return locals[2];
                        }

                        set
                        {
                            locals[2] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    Main parent = (Main)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    (parent).x = (new PrtIntValue(((PrtIntValue)(new PrtIntValue(((PrtIntValue)((parent).x)).nt + ((PrtIntValue)((currFun).locals[1])).nt))).nt + ((PrtIntValue)(new PrtIntValue(1))).nt)).Clone();
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

                    public PrtValue b
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
                    Main parent = (Main)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun4_1;
                        case 2:
                            goto AnonFun4_2;
                    }

                    (currFun).locals[1] = (application).CreateInterfaceOrMachine((parent).renamedName, "B", parent.self);
                    (parent).PrtFunContNewMachine(this, (currFun).locals, 1);
                    return;
                    AnonFun4_1:
                        ;
                    (parent).x = (new PrtIntValue(((PrtIntValue)((parent).x)).nt + ((PrtIntValue)(new PrtIntValue(1))).nt)).Clone();
                    if (!((PrtBoolValue)(new PrtBoolValue(((parent).x).Equals(new PrtIntValue(1))))).bl)
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Src\\\\PTester\\\\Regressions\\\\Receive1\\\\receive1.p (13, 4): Assert failed");
                    (parent).PrtPushFunStackFrame(foo, (foo).CreateLocals((currFun).locals[1], new PrtIntValue(0)));
                    AnonFun4_2:
                        ;
                    (foo).Execute(application, parent);
                    if (((parent).continuation).reason == PrtContinuationReason.Return)
                    {
                    }
                    else
                    {
                        (parent).PrtPushFunStackFrame((currFun).fun, (currFun).locals, 2);
                        return;
                    }

                    if (!((PrtBoolValue)(new PrtBoolValue(((parent).x).Equals(new PrtIntValue(2))))).bl)
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Src\\\\PTester\\\\Regressions\\\\Receive1\\\\receive1.p (15, 4): Assert failed");
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    (locals).Add(PrtValue.PrtMkDefaultValue(Types.type_2_1235668830));
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
                    return new AnonFun5_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun5_Class AnonFun5;
            public class Main_Init_Class : PrtState
            {
                public Main_Init_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Main_Init_Class Main_Init;
            static Main()
            {
                ignore = new ignore_Class();
                foo = new foo_Class();
                AnonFun0 = new AnonFun0_Class();
                AnonFun1 = new AnonFun1_Class();
                AnonFun2 = new AnonFun2_Class();
                AnonFun3 = new AnonFun3_Class();
                AnonFun4 = new AnonFun4_Class();
                AnonFun5 = new AnonFun5_Class();
                Main_Init = new Main_Init_Class("Main_Init", AnonFun4, AnonFun5, false, StateTemperature.Warm);
            }
        }
    }
}
