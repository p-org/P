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
            public static void Events_tuporder()
            {
            }
        }

        public partial class Types
        {
            public static PrtType type_0_628838531;
            public static PrtType type_1_628838531;
            public static PrtType type_2_628838531;
            public static PrtType type_3_628838531;
            public static PrtType type_4_628838531;
            public static PrtType type_5_628838531;
            public static PrtType type_6_628838531;
            static public void Types_tuporder()
            {
                Types.type_0_628838531 = new PrtIntType();
                Types.type_1_628838531 = new PrtBoolType();
                Types.type_2_628838531 = new PrtNullType();
                Types.type_3_628838531 = new PrtMachineType();
                Types.type_4_628838531 = new PrtEventType();
                Types.type_5_628838531 = new PrtNamedTupleType(new object[]{"y", Types.type_3_628838531});
                Types.type_6_628838531 = new PrtNamedTupleType(new object[]{"y", Types.type_1_628838531});
            }
        }

        public static PrtImplMachine CreateMachine_N(StateImpl application, PrtValue payload)
        {
            var machine = new N(application, PrtImplMachine.DefaultMaxBufferSize, false);
            (application).TraceLine("<CreateLog> Created Machine N-{0}", (machine).instanceNumber);
            ((machine).self).permissions = null;
            (machine).sends = null;
            (machine).currentPayload = payload;
            return machine;
        }

        public static PrtImplMachine CreateMachine_M(StateImpl application, PrtValue payload)
        {
            var machine = new M(application, PrtImplMachine.DefaultMaxBufferSize, false);
            (application).TraceLine("<CreateLog> Created Machine M-{0}", (machine).instanceNumber);
            ((machine).self).permissions = null;
            (machine).sends = null;
            (machine).currentPayload = payload;
            return machine;
        }

        public class N : PrtImplMachine
        {
            public override PrtState StartState
            {
                get
                {
                    return N_S1;
                }
            }

            public override PrtImplMachine MakeSkeleton()
            {
                return new N();
            }

            public override int NextInstanceNumber(StateImpl app)
            {
                return app.NextMachineInstanceNumber(this.GetType());
            }

            public override string Name
            {
                get
                {
                    return "N";
                }
            }

            public N(): base ()
            {
            }

            public N(StateImpl app, int maxB, bool assume): base (app, maxB, assume)
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
                    N parent = (N)(_parent);
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
                    public foo_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                    {
                    }

                    public foo_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue var_x
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

                    public PrtValue var_y
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
                    N parent = (N)(_parent);
                    foo_StackFrame currFun = (foo_StackFrame)(parent.PrtPopFunStackFrame());
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto foo_1;
                    }

                    (currFun).var_y = (application).CreateInterfaceOrMachine((parent).renamedName, "M", Events.@null);
                    (parent).PrtFunContNewMachine(this, (currFun).locals, 1);
                    return;
                    foo_1:
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

                    (locals).Add(PrtValue.PrtMkDefaultValue(Types.type_3_628838531));
                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new foo_StackFrame(this, locals, retLoc);
                }

                public override string ToString()
                {
                    return "foo";
                }
            }

            public static foo_Class foo = new foo_Class();
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
                    N parent = (N)(_parent);
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
                    N parent = (N)(_parent);
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
            public class N_S1_Class : PrtState
            {
                public N_S1_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static N_S1_Class N_S1;
            static N()
            {
                N_S1 = new N_S1_Class("N_S1", AnonFun1, AnonFun0, false, StateTemperature.Warm);
            }
        }

        public class M : PrtImplMachine
        {
            public override PrtState StartState
            {
                get
                {
                    return M_S1;
                }
            }

            public override PrtImplMachine MakeSkeleton()
            {
                return new M();
            }

            public override int NextInstanceNumber(StateImpl app)
            {
                return app.NextMachineInstanceNumber(this.GetType());
            }

            public override string Name
            {
                get
                {
                    return "M";
                }
            }

            public M(): base ()
            {
            }

            public M(StateImpl app, int maxB, bool assume): base (app, maxB, assume)
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
                    M parent = (M)(_parent);
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
                    public foo_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                    {
                    }

                    public foo_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue var_x
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

                    public PrtValue var_y
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
                    M parent = (M)(_parent);
                    foo_StackFrame currFun = (foo_StackFrame)(parent.PrtPopFunStackFrame());
                    PrtValue swap;
                    (currFun).var_y = (new PrtBoolValue(true)).Clone();
                    parent.PrtFunContReturn((currFun).locals);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    (locals).Add(PrtValue.PrtMkDefaultValue(Types.type_1_628838531));
                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new foo_StackFrame(this, locals, retLoc);
                }

                public override string ToString()
                {
                    return "foo";
                }
            }

            public static foo_Class foo = new foo_Class();
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
                    M parent = (M)(_parent);
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
                    M parent = (M)(_parent);
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
            public class M_S1_Class : PrtState
            {
                public M_S1_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static M_S1_Class M_S1;
            static M()
            {
                M_S1 = new M_S1_Class("M_S1", AnonFun1, AnonFun0, false, StateTemperature.Warm);
            }
        }
    }
}
