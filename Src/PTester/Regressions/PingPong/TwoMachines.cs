using P.Runtime;
using System.Collections.Generic;
using System;

namespace TwoMachines
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

        public static PrtEventValue halt = new PrtEventValue(new PrtEvent("halt", typeNull, 1, false));
        public static PrtEventValue @null = new PrtEventValue(new PrtEvent("null", typeNull, 1, false));
        public static PrtEventValue Fail = new PrtEventValue(new PrtEvent("Fail", typeNull, PrtEvent.DefaultMaxInstances, false));
        public static PrtEventValue Success = new PrtEventValue(new PrtEvent("Success", typeBool, PrtEvent.DefaultMaxInstances, false));
        public static PrtEventValue Pong = new PrtEventValue(new PrtEvent("Pong", typeInt, 1, false));
        public static PrtEventValue Ping = new PrtEventValue(new PrtEvent("Ping", typeMachine, 1, false));

        static PrtType typeNull;
        static PrtType typeMachine;
        static PrtType typeInt;
        static PrtType typeBool;
        static PrtType typeNmdtuple;

        static Application()
        {
            typeNull = new PrtNullType();
            typeInt = new PrtIntType();
            typeBool = new PrtBoolType();
            typeMachine = new PrtMachineType();
            typeNmdtuple = new PrtNamedTupleType("a", typeInt, "b", typeInt);
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
                return null;
            }
        }

        public static F2_Class F2;
        public class F1_Class : PrtFun
        {
            #region locals
            //Assuming: locals of functions are enumerated in "locals" list in the order they are
            //declared in the P program: formals first, then local vars
            //Question: is below correct:
            //C# variable of the type Par1_Class will be instantiated when the function "F1" is 
            //called (hence, , i.e., in the function call translation.
            //Question: what for are the two different constructors for PrtFunDtackFrame?
            
            internal class Par1_Class: PrtFunStackFrame
            {
                public Par1_Class(PrtFun fun, List<PrtValue> locs) : base(fun, locs)
                {

                }
                public Par1_Class(PrtFun fun, List<PrtValue> locs, int retLocation)
                    : base(fun, locs, retLocation)
                {

                }
                public override PrtFunStackFrame Clone()
                {
                    return this.Clone();
                }
                public PrtValue Par1
                {
                    get
                    {
                        return locals[0];
                    }
                }
            }
            internal class Par2_Class : PrtFunStackFrame
            {
                public Par2_Class(PrtFun fun, List<PrtValue> locs) : base(fun, locs)
                {

                }
                public Par2_Class(PrtFun fun, List<PrtValue> locs, int retLocation)
                    : base(fun, locs, retLocation)
                {

                }
                public override PrtFunStackFrame Clone()
                {
                    return this.Clone();
                }
                public PrtValue Par2
                {
                    get
                    {
                        return locals[1];
                    }
                }

            }
            internal class VarInt_Class : PrtFunStackFrame
            {
                public VarInt_Class(PrtFun fun, List<PrtValue> locs) : base(fun, locs)
                {

                }
                public VarInt_Class(PrtFun fun, List<PrtValue> locs, int retLocation)
                    : base(fun, locs, retLocation)
                {

                }
                public override PrtFunStackFrame Clone()
                {
                    return this.Clone();
                }
                public PrtValue VarInt
                {
                    get
                    {
                        return locals[2];
                    }
                }

            }
            #endregion
            public override bool IsAnonFun
            {
                get
                {
                    return false;
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
                return null;
            }
        }

        public static F1_Class F1;
        public PrtImplMachine CreateMainMachine()
        {
            var mainMachine = new Main(this, 10, false);
            AddImplMachineToStateImpl(mainMachine);
            return mainMachine;
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

            public class AnonFun2_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
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
                    return null;
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
                    return null;
                }
            }

            public static AnonFun3_Class AnonFun3;
            public class foo_Class : PrtFun
            {
                #region locals
                //Assuming: locals of functions are enumerated in "locals" list in the order they are
                //declared in the P program: formals first, then local vars
                //Question: is below correct:
                //C# variable of the type Par1_Class will be instantiated when the function "F1" is 
                //called (hence, , i.e., in the function call translation.
                //Question: what for are the two different constructors for PrtFunDtackFrame?

                internal class Par1_Class : PrtFunStackFrame
                {
                    public Par1_Class(PrtFun fun, List<PrtValue> locs) : base(fun, locs)
                    {

                    }
                    public Par1_Class(PrtFun fun, List<PrtValue> locs, int retLocation)
                        : base(fun, locs, retLocation)
                    {

                    }
                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }
                    public PrtValue Par1
                    {
                        get
                        {
                            return locals[0];
                        }
                    }
                }
                internal class Par2_Class : PrtFunStackFrame
                {
                    public Par2_Class(PrtFun fun, List<PrtValue> locs) : base(fun, locs)
                    {

                    }
                    public Par2_Class(PrtFun fun, List<PrtValue> locs, int retLocation)
                        : base(fun, locs, retLocation)
                    {

                    }
                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }
                    public PrtValue Par2
                    {
                        get
                        {
                            return locals[1];
                        }
                    }

                }
                #endregion
                public override bool IsAnonFun
                {
                    get
                    {
                        return false;
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
                    return null;
                }
            }

            public static foo_Class foo;
            public class AnonFun1_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
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
                    return null;
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
                    return null;
                }
            }

            public static AnonFun0_Class AnonFun0;
            public class ignore_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return false;
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
                    return null;
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
                AnonFun2 = new AnonFun2_Class();
                AnonFun3 = new AnonFun3_Class();
                foo = new foo_Class();
                AnonFun1 = new AnonFun1_Class();
                AnonFun0 = new AnonFun0_Class();
                ignore = new ignore_Class();
                PONG_Pong_SendPong = new PONG_Pong_SendPong_Class("PONG_Pong_SendPong", AnonFun2, AnonFun3, false, StateTemperature.Warm);
                PONG_Pong_SendPong.dos.Add(Ping, F1);
                PONG_Pong_SendPong.deferredSet.Add(Fail);
                PrtTransition transition_1 = new PrtTransition(foo, PONG_Pong_WaitPing, false);
                PONG_Pong_SendPong.transitions.Add(Success, transition_1);
                PONG_Pong_WaitPing = new PONG_Pong_WaitPing_Class("PONG_Pong_WaitPing", AnonFun1, AnonFun0, false, StateTemperature.Warm);
                PONG_Pong_WaitPing.dos.Add(Success, ignore);
                PrtTransition transition_2 = new PrtTransition(F1, PONG_Pong_SendPong, false);
                PONG_Pong_WaitPing.transitions.Add(Ping, transition_2);
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

            #region variables
            //Assuming: machine variables are enumerated in the "fields" in the order they are
            //declared in the P program
            public PrtValue PongId
            {
                get
                {
                    return fields[0];
                }
            }
            public PrtValue VarInt
            {
                get
                {
                    return fields[1];
                }
            }
            public PrtValue VarBool
            {
                get
                {
                    return fields[2];
                }
            }
            public PrtValue VarTpl
            {
                get
                {
                    return fields[3];
                }
            }
            #endregion

            public class AnonFun0_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
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
                    return null;
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
                    return null;
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
                    return null;
                }
            }

            public static ignore_Class ignore;
            public class AnonFun9_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
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
                    return null;
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
                    return null;
                }
            }

            public static AnonFun10_Class AnonFun10;
            public class AnonFun2_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
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
                    return null;
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
                    return null;
                }
            }

            public static AnonFun3_Class AnonFun3;
            public class foo_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return false;
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
                    return null;
                }
            }

            public static foo_Class foo;
            public class AnonFun11_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
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
                    return null;
                }
            }

            public static AnonFun11_Class AnonFun11;
            public class AnonFun4_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
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
                    return null;
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
                    return null;
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
                    return null;
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
                    return null;
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
                    return null;
                }
            }

            public static AnonFun8_Class AnonFun8;
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
                AnonFun9 = new AnonFun9_Class();
                AnonFun10 = new AnonFun10_Class();
                AnonFun2 = new AnonFun2_Class();
                AnonFun3 = new AnonFun3_Class();
                foo = new foo_Class();
                AnonFun11 = new AnonFun11_Class();
                AnonFun4 = new AnonFun4_Class();
                AnonFun5 = new AnonFun5_Class();
                AnonFun6 = new AnonFun6_Class();
                AnonFun7 = new AnonFun7_Class();
                AnonFun8 = new AnonFun8_Class();
                Main_Done = new Main_Done_Class("Main_Done", AnonFun0, AnonFun1, false, StateTemperature.Warm);
                Main_Done.dos.Add(Fail, ignore);
                Main_Done.dos.Add(Success, AnonFun9);
                Main_Done.dos.Add(Pong, AnonFun10);
                Main_Ping_WaitPong = new Main_Ping_WaitPong_Class("Main_Ping_WaitPong", AnonFun2, AnonFun3, false, StateTemperature.Warm);
                Main_Ping_WaitPong.dos.Add(Success, AnonFun11);
                PrtTransition transition_1 = new PrtTransition(foo, Main_Ping_SendPing, false);
                Main_Ping_WaitPong.transitions.Add(Pong, transition_1);
                Main_Ping_SendPing = new Main_Ping_SendPing_Class("Main_Ping_SendPing", AnonFun4, AnonFun5, false, StateTemperature.Cold);
                Main_Ping_SendPing.dos.Add(Pong, foo);
                Main_Ping_SendPing.deferredSet.Add(Fail);
                PrtTransition transition_2 = new PrtTransition(foo, Main_Ping_WaitPong, false);
                Main_Ping_SendPing.transitions.Add(Success, transition_2);
                Main_Init = new Main_Init_Class("Main_Init", AnonFun6, AnonFun7, true, StateTemperature.Hot);
                Main_Init.dos.Add(Fail, ignore);
                PrtTransition transition_3 = new PrtTransition(AnonFun8, Main_Ping_WaitPong, false);
                Main_Init.transitions.Add(null, transition_3);
                PrtTransition transition_4 = new PrtTransition(F2, Main_Ping_SendPing, false);
                Main_Init.transitions.Add(Success, transition_4);
            }
        }
    }
}
