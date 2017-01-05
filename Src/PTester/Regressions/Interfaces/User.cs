#pragma warning disable CS0162, CS0164, CS0168
using P.Runtime;
using System;
using System.Collections.Generic;

namespace P.Program
{
    public class Application : StateImpl
    {
        private class User_Events
        {
            public static PrtEventValue halt;
            public static PrtEventValue @null;
            public static PrtEventValue eStoppingFailure;
            public static PrtEventValue eStoppingSuccess;
            public static PrtEventValue eSetLedStateToStableUsingControlTransfer;
            public static PrtEventValue eStartDebounceTimer;
            public static PrtEventValue eSetLedStateToUnstableUsingControlTransfer;
            public static PrtEventValue eNo;
            public static PrtEventValue eYes;
            public static PrtEventValue eTimerStopped;
            public static PrtEventValue eOperationFailure;
            public static PrtEventValue eOperationSuccess;
            public static PrtEventValue eUnit;
            public static PrtEventValue eUpdateBarGraphStateUsingControlTransfer;
            public static PrtEventValue eStopTimer;
            public static PrtEventValue eTransferFailure;
            public static PrtEventValue eTransferSuccess;
            public static PrtEventValue eSwitchStatusChange;
            public static PrtEventValue eTimerFired;
            public static PrtEventValue eD0Exit;
            public static PrtEventValue eD0Entry;
            static User_Events()
            {
                halt = new PrtEventValue(new PrtEvent("halt", User_Types.typeNull, 1, false));
                @null = new PrtEventValue(new PrtEvent("null", User_Types.typeNull, 1, false));
                eStoppingFailure = new PrtEventValue(new PrtEvent("eStoppingFailure", User_Types.typeNull, 1, false));
                eStoppingSuccess = new PrtEventValue(new PrtEvent("eStoppingSuccess", User_Types.typeNull, 1, false));
                eSetLedStateToStableUsingControlTransfer = new PrtEventValue(new PrtEvent("eSetLedStateToStableUsingControlTransfer", User_Types.typeNull, 1, true));
                eStartDebounceTimer = new PrtEventValue(new PrtEvent("eStartDebounceTimer", User_Types.typeNull, 1, true));
                eSetLedStateToUnstableUsingControlTransfer = new PrtEventValue(new PrtEvent("eSetLedStateToUnstableUsingControlTransfer", User_Types.typeNull, 1, true));
                eNo = new PrtEventValue(new PrtEvent("eNo", User_Types.typeNull, 1, false));
                eYes = new PrtEventValue(new PrtEvent("eYes", User_Types.typeNull, 1, false));
                eTimerStopped = new PrtEventValue(new PrtEvent("eTimerStopped", User_Types.typeNull, 1, false));
                eOperationFailure = new PrtEventValue(new PrtEvent("eOperationFailure", User_Types.typeNull, 1, false));
                eOperationSuccess = new PrtEventValue(new PrtEvent("eOperationSuccess", User_Types.typeNull, 1, false));
                eUnit = new PrtEventValue(new PrtEvent("eUnit", User_Types.typeNull, 1, false));
                eUpdateBarGraphStateUsingControlTransfer = new PrtEventValue(new PrtEvent("eUpdateBarGraphStateUsingControlTransfer", User_Types.typeNull, 1, true));
                eStopTimer = new PrtEventValue(new PrtEvent("eStopTimer", User_Types.typeNull, 1, true));
                eTransferFailure = new PrtEventValue(new PrtEvent("eTransferFailure", User_Types.typeNull, 1, true));
                eTransferSuccess = new PrtEventValue(new PrtEvent("eTransferSuccess", User_Types.typeNull, 1, true));
                eSwitchStatusChange = new PrtEventValue(new PrtEvent("eSwitchStatusChange", User_Types.typeNull, 1, true));
                eTimerFired = new PrtEventValue(new PrtEvent("eTimerFired", User_Types.typeNull, 1, false));
                eD0Exit = new PrtEventValue(new PrtEvent("eD0Exit", User_Types.typeNull, 1, true));
                eD0Entry = new PrtEventValue(new PrtEvent("eD0Entry", User_Types.typeNull, 1, true));
            }
        }

        private class User_Types
        {
            public static PrtType typeNull;
            public static PrtNamedTupleType typeNmdTupType_1;
            public static PrtNamedTupleType typeNmdTupType_2;
            public static PrtNamedTupleType typeNmdTupType_3;
            public static PrtNamedTupleType typeNmdTupType_4;
            public static PrtInterfaceType typeInterfaceType_5;
            public static PrtType typeAny;
            public static PrtType typeInt;
            public static PrtType typeEvent;
            public static PrtType typeMachine;
            public static PrtType typeBool;
            static User_Types()
            {
                User_Types.typeNull = new PrtNullType();
                User_Types.typeNmdTupType_1 = new PrtNamedTupleType(new object[]{"_payload_skip", User_Types.typeNull});
                User_Types.typeNmdTupType_2 = new PrtNamedTupleType(new object[]{"_payload_1", User_Types.typeNull});
                User_Types.typeNmdTupType_3 = new PrtNamedTupleType(new object[]{"_payload_0", User_Types.typeNull});
                User_Types.typeNmdTupType_4 = new PrtNamedTupleType(new object[]{"_payload_2", User_Types.typeNull});
                ((typeInterfaceType_5).permissions).Add(User_Events.eD0Entry);
                ((typeInterfaceType_5).permissions).Add(User_Events.eD0Exit);
                ((typeInterfaceType_5).permissions).Add(User_Events.eOperationSuccess);
                ((typeInterfaceType_5).permissions).Add(User_Events.eStoppingFailure);
                ((typeInterfaceType_5).permissions).Add(User_Events.eStoppingSuccess);
                ((typeInterfaceType_5).permissions).Add(User_Events.eSwitchStatusChange);
                ((typeInterfaceType_5).permissions).Add(User_Events.eTimerFired);
                ((typeInterfaceType_5).permissions).Add(User_Events.eTimerStopped);
                ((typeInterfaceType_5).permissions).Add(User_Events.eTransferFailure);
                ((typeInterfaceType_5).permissions).Add(User_Events.eTransferSuccess);
                User_Types.typeAny = new PrtAnyType();
                User_Types.typeInt = new PrtIntType();
                User_Types.typeEvent = new PrtEventType();
                User_Types.typeMachine = new PrtMachineType();
                User_Types.typeBool = new PrtBoolType();
            }
        }

        public static PrtImplMachine CreateMachine_UserMachine(StateImpl application, PrtValue payload)
        {
            var machine = new UserMachine(application, PrtImplMachine.DefaultMaxBufferSize, false);
            (((machine).self).permissions).Add(User_Events.eOperationFailure);
            (((machine).self).permissions).Add(User_Events.eTimerStopped);
            (((machine).self).permissions).Add(User_Events.eYes);
            (((machine).self).permissions).Add(User_Events.eNo);
            (((machine).self).permissions).Add(User_Events.eSetLedStateToUnstableUsingControlTransfer);
            (((machine).self).permissions).Add(User_Events.eStartDebounceTimer);
            (((machine).self).permissions).Add(User_Events.eSetLedStateToStableUsingControlTransfer);
            (((machine).self).permissions).Add(User_Events.eStoppingSuccess);
            (((machine).self).permissions).Add(User_Events.eStoppingFailure);
            (((machine).self).permissions).Add(User_Events.eOperationSuccess);
            (((machine).self).permissions).Add(User_Events.eUnit);
            (((machine).self).permissions).Add(User_Events.eUpdateBarGraphStateUsingControlTransfer);
            (((machine).self).permissions).Add(User_Events.eStopTimer);
            (((machine).self).permissions).Add(User_Events.eTransferFailure);
            (((machine).self).permissions).Add(User_Events.eTransferSuccess);
            (((machine).self).permissions).Add(User_Events.eSwitchStatusChange);
            (((machine).self).permissions).Add(User_Events.eTimerFired);
            (((machine).self).permissions).Add(User_Events.eD0Exit);
            (((machine).self).permissions).Add(User_Events.eD0Entry);
            ((machine).sends).Add(User_Events.eD0Exit);
            ((machine).sends).Add(User_Events.eD0Entry);
            (machine).currentPayload = payload;
            return machine;
        }

        public class UserMachine : PrtImplMachine
        {
            public override PrtState StartState
            {
                get
                {
                    return UserMachine_User_Init;
                }
            }

            public PrtValue Driver
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
                return new UserMachine();
            }

            public override int NextInstanceNumber(StateImpl app)
            {
                return app.NextMachineInstanceNumber(this.GetType());
            }

            public override string Name
            {
                get
                {
                    return "UserMachine";
                }
            }

            public UserMachine(): base ()
            {
            }

            public UserMachine(StateImpl app, int maxB, bool assume): base (app, maxB, assume)
            {
                (fields).Add(PrtValue.PrtMkDefaultValue(User_Types.typeMachine));
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
                    UserMachine parent = (UserMachine)(_parent);
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
                    UserMachine parent = (UserMachine)(_parent);
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
                    UserMachine parent = (UserMachine)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun1_1;
                    }

                    (((PrtMachineValue)((parent).Driver)).mach).PrtEnqueueEvent((PrtEventValue)(User_Events.eD0Exit), User_Events.@null, parent, (PrtMachineValue)((parent).Driver));
                    (parent).PrtFunContSend(this, (currFun).locals, (currFun).returnToLocation);
                    return;
                    AnonFun1_1:
                        ;
                    if (!!(User_Events.eUnit).Equals(User_Events.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Src\\\\PTester\\\\Regressions\\\\Interfaces\\\\User.p (28, 4): Raised event must be non-null");
                    Console.Write("<RaiseLog> Machine UserMachine-{0} raised Event {1}\\n", (parent).instanceNumber, (((PrtEventValue)(User_Events.eUnit)).evt).name);
                    (parent).currentTrigger = User_Events.eUnit;
                    (parent).currentPayload = User_Events.@null;
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
                    UserMachine parent = (UserMachine)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun2_1;
                    }

                    (((PrtMachineValue)((parent).Driver)).mach).PrtEnqueueEvent((PrtEventValue)(User_Events.eD0Entry), User_Events.@null, parent, (PrtMachineValue)((parent).Driver));
                    (parent).PrtFunContSend(this, (currFun).locals, (currFun).returnToLocation);
                    return;
                    AnonFun2_1:
                        ;
                    if (!!(User_Events.eUnit).Equals(User_Events.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Src\\\\PTester\\\\Regressions\\\\Interfaces\\\\User.p (20, 4): Raised event must be non-null");
                    Console.Write("<RaiseLog> Machine UserMachine-{0} raised Event {1}\\n", (parent).instanceNumber, (((PrtEventValue)(User_Events.eUnit)).evt).name);
                    (parent).currentTrigger = User_Events.eUnit;
                    (parent).currentPayload = User_Events.@null;
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

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    UserMachine parent = (UserMachine)(_parent);
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
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    UserMachine parent = (UserMachine)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun4_1;
                    }

                    (parent).Driver = CreateInterfaceOrMachine((parent).renamedName, OSRDriverInterface, User_Events.@null);
                    (parent).PrtFunContNewMachine(this, (currFun).locals, 1);
                    return;
                    AnonFun4_1:
                        ;
                    if (!!(User_Events.eUnit).Equals(User_Events.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Src\\\\PTester\\\\Regressions\\\\Interfaces\\\\User.p (12, 4): Raised event must be non-null");
                    Console.Write("<RaiseLog> Machine UserMachine-{0} raised Event {1}\\n", (parent).instanceNumber, (((PrtEventValue)(User_Events.eUnit)).evt).name);
                    (parent).currentTrigger = User_Events.eUnit;
                    (parent).currentPayload = User_Events.@null;
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
                    UserMachine parent = (UserMachine)(_parent);
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
                    UserMachine parent = (UserMachine)(_parent);
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
                    UserMachine parent = (UserMachine)(_parent);
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
                    UserMachine parent = (UserMachine)(_parent);
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
                    UserMachine parent = (UserMachine)(_parent);
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
                    UserMachine parent = (UserMachine)(_parent);
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
                    UserMachine parent = (UserMachine)(_parent);
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
            public class UserMachine_S0_Class : PrtState
            {
                public UserMachine_S0_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static UserMachine_S0_Class UserMachine_S0;
            public class UserMachine_User_Init_Class : PrtState
            {
                public UserMachine_User_Init_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static UserMachine_User_Init_Class UserMachine_User_Init;
            public class UserMachine_S1_Class : PrtState
            {
                public UserMachine_S1_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static UserMachine_S1_Class UserMachine_S1;
            static UserMachine()
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
                UserMachine_S0 = new UserMachine_S0_Class("UserMachine_S0", AnonFun2, AnonFun3, false, StateTemperature.Warm);
                UserMachine_User_Init = new UserMachine_User_Init_Class("UserMachine_User_Init", AnonFun4, AnonFun5, false, StateTemperature.Warm);
                UserMachine_S1 = new UserMachine_S1_Class("UserMachine_S1", AnonFun1, AnonFun0, false, StateTemperature.Warm);
                PrtTransition transition_1 = new PrtTransition(AnonFun10, UserMachine_S1, false);
                UserMachine_S0.transitions.Add(User_Events.eUnit, transition_1);
                PrtTransition transition_2 = new PrtTransition(AnonFun11, UserMachine_S0, false);
                UserMachine_User_Init.transitions.Add(User_Events.eUnit, transition_2);
                PrtTransition transition_3 = new PrtTransition(AnonFun9, UserMachine_S0, false);
                UserMachine_S1.transitions.Add(User_Events.eUnit, transition_3);
            }
        }
    }
}
