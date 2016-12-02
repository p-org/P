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
        public static PrtEventValue NotUsed2;
        public static PrtEventValue NotUsed3;
        public static PrtEventValue NotUsed;
        public static PrtEventValue Fail;
        public static PrtEventValue Success;
        public static PrtEventValue Pong;
        public static PrtEventValue Ping;
        public static PrtType typeAny;
        public static PrtSeqType typeSeqType_1;
        public static PrtType typeInt;
        public static PrtMapType typeMapType_3;
        public static PrtNamedTupleType typeNmdTupType_4;
        public static PrtNamedTupleType typeNmdTupType_5;
        public static PrtType typeBool;
        public static PrtTupleType typeTupType_7;
        public static PrtType typeNameType_8;
        public static PrtNamedTupleType typeNmdTupType_9;
        public static PrtSeqType typeSeqType_10;
        public static PrtSeqType typeSeqType_11;
        public static PrtNamedTupleType typeNmdTupType_12;
        public static PrtNamedTupleType typeNmdTupType_13;
        public static PrtType typeMachine;
        public static PrtNamedTupleType typeNmdTupType_15;
        public static PrtType typeNull;
        public static PrtNamedTupleType typeNmdTupType_17;
        public static PrtNamedTupleType typeNmdTupType_18;
        public static PrtNamedTupleType typeNmdTupType_19;
        public static PrtMapType typeMapType_20;
        public static PrtMapType typeMapType_21;
        public static PrtNamedTupleType typeNmdTupType_22;
        public static PrtMapType typeMapType_23;
        public static PrtNamedTupleType typeNmdTupType_24;
        public static PrtNamedTupleType typeNmdTupType_25;
        public static PrtNamedTupleType typeNmdTupType_26;
        public static PrtTupleType typeTupType_27;
        public static PrtNamedTupleType typeNmdTupType_28;
        public static PrtNamedTupleType typeNmdTupType_29;
        public static PrtType typeEvent;
        static Application()
        {
            typeAny = new PrtAnyType();
            typeSeqType_1 = new PrtSeqType(typeAny);
            typeInt = new PrtIntType();
            typeMapType_3 = new PrtMapType(typeInt, typeSeqType_1);
            typeNmdTupType_4 = new PrtNamedTupleType(new object[]{"a", typeSeqType_1, "b", typeMapType_3});
            typeNmdTupType_5 = new PrtNamedTupleType(new object[]{"b", typeMapType_3});
            typeBool = new PrtBoolType();
            typeTupType_7 = new PrtTupleType(new PrtType[]{typeInt, typeBool});
            typeNameType_8 = new PrtIntType();
            typeNmdTupType_9 = new PrtNamedTupleType(new object[]{"par1", typeTupType_7, "par2", typeNameType_8});
            typeSeqType_10 = new PrtSeqType(typeInt);
            typeSeqType_11 = new PrtSeqType(typeSeqType_10);
            typeNmdTupType_12 = new PrtNamedTupleType(new object[]{"par1", typeBool, "par2", typeInt});
            typeNmdTupType_13 = new PrtNamedTupleType(new object[]{"par2", typeInt});
            typeMachine = new PrtMachineType();
            typeNmdTupType_15 = new PrtNamedTupleType(new object[]{"payload", typeMachine});
            typeNull = new PrtNullType();
            typeNmdTupType_17 = new PrtNamedTupleType(new object[]{"_payload_2", typeNull});
            typeNmdTupType_18 = new PrtNamedTupleType(new object[]{"_payload_skip", typeNull});
            typeNmdTupType_19 = new PrtNamedTupleType(new object[]{"a", typeInt, "b", typeInt});
            typeMapType_20 = new PrtMapType(typeInt, typeAny);
            typeMapType_21 = new PrtMapType(typeInt, typeMapType_20);
            typeNmdTupType_22 = new PrtNamedTupleType(new object[]{"_payload_3", typeNull});
            typeMapType_23 = new PrtMapType(typeAny, typeAny);
            typeNmdTupType_24 = new PrtNamedTupleType(new object[]{"par2", typeNameType_8});
            typeNmdTupType_25 = new PrtNamedTupleType(new object[]{"_payload_1", typeNull});
            typeNmdTupType_26 = new PrtNamedTupleType(new object[]{"_payload_0", typeNull});
            typeTupType_27 = new PrtTupleType(new PrtType[]{typeBool});
            typeNmdTupType_28 = new PrtNamedTupleType(new object[]{"varInt", typeInt});
            typeNmdTupType_29 = new PrtNamedTupleType(new object[]{"b", typeInt});
            typeEvent = new PrtEventType();
            halt = new PrtEventValue(new PrtEvent("halt", typeNull, 1, false));
            @null = new PrtEventValue(new PrtEvent("null", typeNull, 1, false));
            NotUsed2 = new PrtEventValue(new PrtEvent("NotUsed2", typeMapType_21, PrtEvent.DefaultMaxInstances, false));
            NotUsed3 = new PrtEventValue(new PrtEvent("NotUsed3", typeNmdTupType_4, PrtEvent.DefaultMaxInstances, false));
            NotUsed = new PrtEventValue(new PrtEvent("NotUsed", typeSeqType_11, PrtEvent.DefaultMaxInstances, false));
            Fail = new PrtEventValue(new PrtEvent("Fail", typeMapType_23, PrtEvent.DefaultMaxInstances, false));
            Success = new PrtEventValue(new PrtEvent("Success", typeTupType_7, PrtEvent.DefaultMaxInstances, false));
            Pong = new PrtEventValue(new PrtEvent("Pong", typeSeqType_10, 1, false));
            Ping = new PrtEventValue(new PrtEvent("Ping", typeNmdTupType_19, 1, false));
        }

        public class F2_Class : PrtFun
        {
            public override bool IsAnonFun
            {
                get
                {
                    return false;
                }
            }

            internal class F2_StackFrame : PrtFunStackFrame
            {
                public F2_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                {
                }

                public F2_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                return new F2_StackFrame(this, locals, retLoc);
            }
        }

        public static F2_Class F2;
        public class F1_Class : PrtFun
        {
            public override bool IsAnonFun
            {
                get
                {
                    return false;
                }
            }

            internal class F1_StackFrame : PrtFunStackFrame
            {
                public F1_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                {
                }

                public F1_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                {
                }

                public override PrtFunStackFrame Clone()
                {
                    return this.Clone();
                }

                public PrtValue par1
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

                public PrtValue par2
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

                public PrtValue varInt
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
                return new F1_StackFrame(this, locals, retLoc);
            }
        }

        public static F1_Class F1;
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

            public PrtValue varTpl
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

            public PrtValue varBool
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

            public PrtValue varInt
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

            public PrtValue pongId
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

            public PrtValue varEnum2
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

            public PrtValue varEnum
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

            public PrtValue varNmdTpl
            {
                get
                {
                    return fields[6];
                }

                set
                {
                    fields[6] = value;
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
                    return new AnonFun1_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun1_Class AnonFun1;
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
                    return new ignore_StackFrame(this, locals, retLoc);
                }
            }

            public static ignore_Class ignore;
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
                    return new AnonFun3_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun3_Class AnonFun3;
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
                    return new AnonFun8_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun8_Class AnonFun8;
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
                    return new AnonFun7_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun7_Class AnonFun7;
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
                    return new AnonFun12_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun12_Class AnonFun12;
            public class Main_Done_Class : PrtState
            {
                public Main_Done_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Main_Done_Class Main_Done;
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
                ignore = new ignore_Class();
                AnonFun2 = new AnonFun2_Class();
                AnonFun3 = new AnonFun3_Class();
                AnonFun8 = new AnonFun8_Class();
                AnonFun4 = new AnonFun4_Class();
                AnonFun5 = new AnonFun5_Class();
                AnonFun6 = new AnonFun6_Class();
                AnonFun7 = new AnonFun7_Class();
                AnonFun11 = new AnonFun11_Class();
                AnonFun12 = new AnonFun12_Class();
                Main_Done = new Main_Done_Class("Main_Done", AnonFun0, AnonFun1, false, StateTemperature.Warm);
                Main_Done.dos.Add(Fail, ignore);
                Main_Ping_WaitPong = new Main_Ping_WaitPong_Class("Main_Ping_WaitPong", AnonFun2, AnonFun3, false, StateTemperature.Warm);
                Main_Ping_WaitPong.dos.Add(Success, AnonFun8);
                Main_Ping_SendPing = new Main_Ping_SendPing_Class("Main_Ping_SendPing", AnonFun4, AnonFun5, false, StateTemperature.Cold);
                Main_Ping_SendPing.deferredSet.Add(Fail);
                Main_Init = new Main_Init_Class("Main_Init", AnonFun6, AnonFun7, true, StateTemperature.Hot);
                PrtTransition transition_1 = new PrtTransition(AnonFun11, Main_Ping_WaitPong, false);
                Main_Init.transitions.Add(null, transition_1);
                PrtTransition transition_2 = new PrtTransition(AnonFun12, Main_Ping_WaitPong, false);
                Main_Init.transitions.Add(Fail, transition_2);
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
                    return new AnonFun3_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun3_Class AnonFun3;
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
                    return new ignore_StackFrame(this, locals, retLoc);
                }
            }

            public static ignore_Class ignore;
            public class PONG_Pong_SendPong_Class : PrtState
            {
                public PONG_Pong_SendPong_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static PONG_Pong_SendPong_Class PONG_Pong_SendPong;
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
                AnonFun2 = new AnonFun2_Class();
                AnonFun3 = new AnonFun3_Class();
                ignore = new ignore_Class();
                PONG_Pong_SendPong = new PONG_Pong_SendPong_Class("PONG_Pong_SendPong", AnonFun0, AnonFun1, false, StateTemperature.Warm);
                PONG_Pong_SendPong.dos.Add(Ping, F2);
                PONG_Pong_SendPong.deferredSet.Add(Fail);
                PONG_Pong_WaitPing = new PONG_Pong_WaitPing_Class("PONG_Pong_WaitPing", AnonFun2, AnonFun3, false, StateTemperature.Warm);
                PONG_Pong_WaitPing.dos.Add(Success, ignore);
            }
        }
    }
}
