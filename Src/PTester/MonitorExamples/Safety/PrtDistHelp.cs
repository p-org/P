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
            public static void Events_PrtDistHelp()
            {
            }
        }

        public partial class Types
        {
            public static PrtType type_0_45101315;
            public static PrtType type_1_45101315;
            public static PrtType type_2_45101315;
            public static PrtType type_3_45101315;
            public static PrtType type_4_45101315;
            public static PrtType type_5_45101315;
            public static PrtType type_6_45101315;
            static public void Types_PrtDistHelp()
            {
                Types.type_0_45101315 = new PrtNullType();
                Types.type_1_45101315 = new PrtAnyType();
                Types.type_2_45101315 = new PrtMachineType();
                Types.type_3_45101315 = new PrtEventType();
                Types.type_4_45101315 = new PrtIntType();
                Types.type_5_45101315 = new PrtBoolType();
                Types.type_6_45101315 = new PrtNamedTupleType(new object[]{"retVal", Types.type_2_45101315});
            }
        }

        public class _CREATECONTAINER_Class : PrtFun
        {
            public override bool IsAnonFun
            {
                get
                {
                    return false;
                }
            }

            internal class _CREATECONTAINER_StackFrame : PrtFunStackFrame
            {
                public _CREATECONTAINER_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                {
                }

                public _CREATECONTAINER_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                {
                }

                public override PrtFunStackFrame Clone()
                {
                    return this.Clone();
                }

                public PrtValue var_retVal
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

            public override void Execute(StateImpl application, PrtMachine parent)
            {
                _CREATECONTAINER_StackFrame currFun = (_CREATECONTAINER_StackFrame)(parent.PrtPopFunStackFrame());
                PrtValue swap;
                switch ((currFun).returnToLocation)
                {
                    case 1:
                        goto _CREATECONTAINER_1;
                }

                (currFun).var_retVal = (application).CreateInterfaceOrMachine((parent).renamedName, "Container", Events.@null);
                (parent).PrtFunContNewMachine(this, (currFun).locals, 1);
                return;
                _CREATECONTAINER_1:
                    ;
                (parent).PrtFunContReturnVal((currFun).var_retVal, (currFun).locals);
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

                (locals).Add(PrtValue.PrtMkDefaultValue(Types.type_2_45101315));
                return locals;
            }

            public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
            {
                return new _CREATECONTAINER_StackFrame(this, locals, retLoc);
            }

            public override string ToString()
            {
                return "_CREATECONTAINER";
            }
        }

        public static _CREATECONTAINER_Class _CREATECONTAINER = new _CREATECONTAINER_Class();
        public class _SEND_Class : PrtFun
        {
            public override bool IsAnonFun
            {
                get
                {
                    return false;
                }
            }

            internal class _SEND_StackFrame : PrtFunStackFrame
            {
                public _SEND_StackFrame(PrtFun fun, List<PrtValue> _locals): base (fun, _locals)
                {
                }

                public _SEND_StackFrame(PrtFun fun, List<PrtValue> _locals, int retLocation): base (fun, _locals, retLocation)
                {
                }

                public override PrtFunStackFrame Clone()
                {
                    return this.Clone();
                }

                public PrtValue var_target
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

                public PrtValue var_e
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

                public PrtValue var_p
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

            public override void Execute(StateImpl application, PrtMachine parent)
            {
                _SEND_StackFrame currFun = (_SEND_StackFrame)(parent.PrtPopFunStackFrame());
                PrtValue swap;
                switch ((currFun).returnToLocation)
                {
                    case 1:
                        goto _SEND_1;
                }

                (((PrtMachineValue)((currFun).var_target)).mach).PrtEnqueueEvent((PrtEventValue)((currFun).var_e), (currFun).var_p, parent, (PrtMachineValue)((currFun).var_target));
                (parent).PrtFunContSend(this, (currFun).locals, 1);
                return;
                _SEND_1:
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
                return new _SEND_StackFrame(this, locals, retLoc);
            }

            public override string ToString()
            {
                return "_SEND";
            }
        }

        public static _SEND_Class _SEND = new _SEND_Class();
        public static PrtImplMachine CreateMachine_Container(StateImpl application, PrtValue payload)
        {
            var machine = new Container(application, PrtImplMachine.DefaultMaxBufferSize, false);
            (application).Trace("<CreateLog> Created Machine Container-{0}", (machine).instanceNumber);
            (((machine).self).permissions).Add(Events.halt);
            ((machine).sends).Add(Events.halt);
            (machine).currentPayload = payload;
            return machine;
        }

        public class Container : PrtImplMachine
        {
            public override PrtState StartState
            {
                get
                {
                    return Container_Init;
                }
            }

            public override PrtImplMachine MakeSkeleton()
            {
                return new Container();
            }

            public override int NextInstanceNumber(StateImpl app)
            {
                return app.NextMachineInstanceNumber(this.GetType());
            }

            public override string Name
            {
                get
                {
                    return "Container";
                }
            }

            public Container(): base ()
            {
            }

            public Container(StateImpl app, int maxB, bool assume): base (app, maxB, assume)
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
                    Container parent = (Container)(_parent);
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
                    Container parent = (Container)(_parent);
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
                    Container parent = (Container)(_parent);
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
            public class Container_Init_Class : PrtState
            {
                public Container_Init_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Container_Init_Class Container_Init;
            static Container()
            {
                Container_Init = new Container_Init_Class("Container_Init", AnonFun0, AnonFun1, false, StateTemperature.Warm);
            }
        }
    }
}
