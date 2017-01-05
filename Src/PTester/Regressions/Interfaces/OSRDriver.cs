#pragma warning disable CS0162, CS0164, CS0168
using P.Runtime;
using System;
using System.Collections.Generic;

namespace P.Program
{
    public partial class Application : StateImpl
    {
        private class OSRDriver_Events
        {
            public static PrtEventValue halt;
            public static PrtEventValue @null;
            public static PrtEventValue eUnit;
            public static PrtEventValue eNo;
            public static PrtEventValue eYes;
            public static PrtEventValue eTimerStopped;
            public static PrtEventValue eOperationFailure;
            public static PrtEventValue eOperationSuccess;
            public static PrtEventValue eStoppingFailure;
            public static PrtEventValue eStoppingSuccess;
            public static PrtEventValue eSetLedStateToStableUsingControlTransfer;
            public static PrtEventValue eStartDebounceTimer;
            public static PrtEventValue eSetLedStateToUnstableUsingControlTransfer;
            public static PrtEventValue eUpdateBarGraphStateUsingControlTransfer;
            public static PrtEventValue eStopTimer;
            public static PrtEventValue eTransferFailure;
            public static PrtEventValue eTransferSuccess;
            public static PrtEventValue eSwitchStatusChange;
            public static PrtEventValue eTimerFired;
            public static PrtEventValue eD0Exit;
            public static PrtEventValue eD0Entry;
            static OSRDriver_Events()
            {
                halt = new PrtEventValue(new PrtEvent("halt", OSRDriver_Types.typeNull, 1, false));
                @null = new PrtEventValue(new PrtEvent("null", OSRDriver_Types.typeNull, 1, false));
                eUnit = new PrtEventValue(new PrtEvent("eUnit", OSRDriver_Types.typeNull, 1, false));
                eNo = new PrtEventValue(new PrtEvent("eNo", OSRDriver_Types.typeNull, 1, false));
                eYes = new PrtEventValue(new PrtEvent("eYes", OSRDriver_Types.typeNull, 1, false));
                eTimerStopped = new PrtEventValue(new PrtEvent("eTimerStopped", OSRDriver_Types.typeNull, 1, false));
                eOperationFailure = new PrtEventValue(new PrtEvent("eOperationFailure", OSRDriver_Types.typeNull, 1, false));
                eOperationSuccess = new PrtEventValue(new PrtEvent("eOperationSuccess", OSRDriver_Types.typeNull, 1, false));
                eStoppingFailure = new PrtEventValue(new PrtEvent("eStoppingFailure", OSRDriver_Types.typeNull, 1, false));
                eStoppingSuccess = new PrtEventValue(new PrtEvent("eStoppingSuccess", OSRDriver_Types.typeNull, 1, false));
                eSetLedStateToStableUsingControlTransfer = new PrtEventValue(new PrtEvent("eSetLedStateToStableUsingControlTransfer", OSRDriver_Types.typeNull, 1, true));
                eStartDebounceTimer = new PrtEventValue(new PrtEvent("eStartDebounceTimer", OSRDriver_Types.typeNull, 1, true));
                eSetLedStateToUnstableUsingControlTransfer = new PrtEventValue(new PrtEvent("eSetLedStateToUnstableUsingControlTransfer", OSRDriver_Types.typeNull, 1, true));
                eUpdateBarGraphStateUsingControlTransfer = new PrtEventValue(new PrtEvent("eUpdateBarGraphStateUsingControlTransfer", OSRDriver_Types.typeNull, 1, true));
                eStopTimer = new PrtEventValue(new PrtEvent("eStopTimer", OSRDriver_Types.typeNull, 1, true));
                eTransferFailure = new PrtEventValue(new PrtEvent("eTransferFailure", OSRDriver_Types.typeNull, 1, true));
                eTransferSuccess = new PrtEventValue(new PrtEvent("eTransferSuccess", OSRDriver_Types.typeNull, 1, true));
                eSwitchStatusChange = new PrtEventValue(new PrtEvent("eSwitchStatusChange", OSRDriver_Types.typeNull, 1, true));
                eTimerFired = new PrtEventValue(new PrtEvent("eTimerFired", OSRDriver_Types.typeNull, 1, false));
                eD0Exit = new PrtEventValue(new PrtEvent("eD0Exit", OSRDriver_Types.typeNull, 1, true));
                eD0Entry = new PrtEventValue(new PrtEvent("eD0Entry", OSRDriver_Types.typeNull, 1, true));
            }
        }

        private class OSRDriver_Types
        {
            public static PrtType typeNull;
            public static PrtNamedTupleType typeNmdTupType_1;
            public static PrtNamedTupleType typeNmdTupType_2;
            public static PrtNamedTupleType typeNmdTupType_3;
            public static PrtNamedTupleType typeNmdTupType_4;
            public static PrtNamedTupleType typeNmdTupType_5;
            public static PrtNamedTupleType typeNmdTupType_6;
            public static PrtNamedTupleType typeNmdTupType_7;
            public static PrtNamedTupleType typeNmdTupType_8;
            public static PrtNamedTupleType typeNmdTupType_9;
            public static PrtNamedTupleType typeNmdTupType_10;
            public static PrtNamedTupleType typeNmdTupType_11;
            public static PrtNamedTupleType typeNmdTupType_12;
            public static PrtNamedTupleType typeNmdTupType_13;
            public static PrtNamedTupleType typeNmdTupType_14;
            public static PrtNamedTupleType typeNmdTupType_15;
            public static PrtInterfaceType typeInterfaceType_16;
            public static PrtType typeBool;
            public static PrtType typeInt;
            public static PrtType typeEvent;
            public static PrtType typeMachine;
            public static PrtType typeAny;
            static OSRDriver_Types()
            {
                OSRDriver_Types.typeNull = new PrtNullType();
                OSRDriver_Types.typeNmdTupType_1 = new PrtNamedTupleType(new object[]{"_payload_13", OSRDriver_Types.typeNull});
                OSRDriver_Types.typeNmdTupType_2 = new PrtNamedTupleType(new object[]{"_payload_11", OSRDriver_Types.typeNull});
                OSRDriver_Types.typeNmdTupType_3 = new PrtNamedTupleType(new object[]{"_payload_10", OSRDriver_Types.typeNull});
                OSRDriver_Types.typeNmdTupType_4 = new PrtNamedTupleType(new object[]{"_payload_9", OSRDriver_Types.typeNull});
                OSRDriver_Types.typeNmdTupType_5 = new PrtNamedTupleType(new object[]{"_payload_8", OSRDriver_Types.typeNull});
                OSRDriver_Types.typeNmdTupType_6 = new PrtNamedTupleType(new object[]{"_payload_7", OSRDriver_Types.typeNull});
                OSRDriver_Types.typeNmdTupType_7 = new PrtNamedTupleType(new object[]{"_payload_6", OSRDriver_Types.typeNull});
                OSRDriver_Types.typeNmdTupType_8 = new PrtNamedTupleType(new object[]{"_payload_5", OSRDriver_Types.typeNull});
                OSRDriver_Types.typeNmdTupType_9 = new PrtNamedTupleType(new object[]{"_payload_4", OSRDriver_Types.typeNull});
                OSRDriver_Types.typeNmdTupType_10 = new PrtNamedTupleType(new object[]{"_payload_3", OSRDriver_Types.typeNull});
                OSRDriver_Types.typeNmdTupType_11 = new PrtNamedTupleType(new object[]{"_payload_2", OSRDriver_Types.typeNull});
                OSRDriver_Types.typeNmdTupType_12 = new PrtNamedTupleType(new object[]{"_payload_1", OSRDriver_Types.typeNull});
                OSRDriver_Types.typeNmdTupType_13 = new PrtNamedTupleType(new object[]{"_payload_12", OSRDriver_Types.typeNull});
                OSRDriver_Types.typeNmdTupType_14 = new PrtNamedTupleType(new object[]{"_payload_0", OSRDriver_Types.typeNull});
                OSRDriver_Types.typeNmdTupType_15 = new PrtNamedTupleType(new object[]{"_payload_skip", OSRDriver_Types.typeNull});
                ((typeInterfaceType_16).permissions).Add(OSRDriver_Events.eD0Entry);
                ((typeInterfaceType_16).permissions).Add(OSRDriver_Events.eD0Exit);
                ((typeInterfaceType_16).permissions).Add(OSRDriver_Events.eOperationSuccess);
                ((typeInterfaceType_16).permissions).Add(OSRDriver_Events.eStoppingFailure);
                ((typeInterfaceType_16).permissions).Add(OSRDriver_Events.eStoppingSuccess);
                ((typeInterfaceType_16).permissions).Add(OSRDriver_Events.eSwitchStatusChange);
                ((typeInterfaceType_16).permissions).Add(OSRDriver_Events.eTimerFired);
                ((typeInterfaceType_16).permissions).Add(OSRDriver_Events.eTimerStopped);
                ((typeInterfaceType_16).permissions).Add(OSRDriver_Events.eTransferFailure);
                ((typeInterfaceType_16).permissions).Add(OSRDriver_Events.eTransferSuccess);
                OSRDriver_Types.typeBool = new PrtBoolType();
                OSRDriver_Types.typeInt = new PrtIntType();
                OSRDriver_Types.typeEvent = new PrtEventType();
                OSRDriver_Types.typeMachine = new PrtMachineType();
                OSRDriver_Types.typeAny = new PrtAnyType();
            }
        }

        public static PrtImplMachine CreateMachine_OSRDriverMachine(StateImpl application, PrtValue payload)
        {
            var machine = new OSRDriverMachine(application, PrtImplMachine.DefaultMaxBufferSize, false);
            (((machine).self).permissions).Add(OSRDriver_Events.eSwitchStatusChange);
            (((machine).self).permissions).Add(OSRDriver_Events.eD0Exit);
            (((machine).self).permissions).Add(OSRDriver_Events.eD0Entry);
            (((machine).self).permissions).Add(OSRDriver_Events.eOperationSuccess);
            (((machine).self).permissions).Add(OSRDriver_Events.eTransferSuccess);
            (((machine).self).permissions).Add(OSRDriver_Events.eTransferFailure);
            (((machine).self).permissions).Add(OSRDriver_Events.eTimerFired);
            (((machine).self).permissions).Add(OSRDriver_Events.eTimerStopped);
            (((machine).self).permissions).Add(OSRDriver_Events.eStoppingSuccess);
            (((machine).self).permissions).Add(OSRDriver_Events.eStoppingFailure);
            ((machine).sends).Add(OSRDriver_Events.eStopTimer);
            ((machine).sends).Add(OSRDriver_Events.eStartDebounceTimer);
            ((machine).sends).Add(OSRDriver_Events.eSetLedStateToUnstableUsingControlTransfer);
            ((machine).sends).Add(OSRDriver_Events.eSetLedStateToStableUsingControlTransfer);
            ((machine).sends).Add(OSRDriver_Events.eUpdateBarGraphStateUsingControlTransfer);
            (machine).currentPayload = payload;
            return machine;
        }

        public class OSRDriverMachine : PrtImplMachine
        {
            public override PrtState StartState
            {
                get
                {
                    return OSRDriverMachine_Driver_Init;
                }
            }

            public PrtValue check
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

            public PrtValue SwitchV
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

            public PrtValue LEDV
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

            public PrtValue TimerV
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

            public override PrtImplMachine MakeSkeleton()
            {
                return new OSRDriverMachine();
            }

            public override int NextInstanceNumber(StateImpl app)
            {
                return app.NextMachineInstanceNumber(this.GetType());
            }

            public override string Name
            {
                get
                {
                    return "OSRDriverMachine";
                }
            }

            public OSRDriverMachine(): base ()
            {
            }

            public OSRDriverMachine(StateImpl app, int maxB, bool assume): base (app, maxB, assume)
            {
                (fields).Add(PrtValue.PrtMkDefaultValue(OSRDriver_Types.typeBool));
                (fields).Add(PrtValue.PrtMkDefaultValue(OSRDriver_Types.typeMachine));
                (fields).Add(PrtValue.PrtMkDefaultValue(OSRDriver_Types.typeMachine));
                (fields).Add(PrtValue.PrtMkDefaultValue(OSRDriver_Types.typeMachine));
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
            public class SetLedStateToStableUsingControlTransfer_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return false;
                    }
                }

                internal class SetLedStateToStableUsingControlTransfer_StackFrame : PrtFunStackFrame
                {
                    public SetLedStateToStableUsingControlTransfer_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public SetLedStateToStableUsingControlTransfer_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto SetLedStateToStableUsingControlTransfer_1;
                    }

                    (((PrtMachineValue)((parent).LEDV)).mach).PrtEnqueueEvent((PrtEventValue)(OSRDriver_Events.eSetLedStateToStableUsingControlTransfer), OSRDriver_Events.@null, parent, (PrtMachineValue)((parent).LEDV));
                    (parent).PrtFunContSend(this, (currFun).locals, (currFun).returnToLocation);
                    return;
                    SetLedStateToStableUsingControlTransfer_1:
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
                    return new SetLedStateToStableUsingControlTransfer_StackFrame(this, locals, retLoc);
                }
            }

            public static SetLedStateToStableUsingControlTransfer_Class SetLedStateToStableUsingControlTransfer;
            public class UpdateBarGraphStateUsingControlTransfer_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return false;
                    }
                }

                internal class UpdateBarGraphStateUsingControlTransfer_StackFrame : PrtFunStackFrame
                {
                    public UpdateBarGraphStateUsingControlTransfer_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public UpdateBarGraphStateUsingControlTransfer_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto UpdateBarGraphStateUsingControlTransfer_1;
                    }

                    (((PrtMachineValue)((parent).LEDV)).mach).PrtEnqueueEvent((PrtEventValue)(OSRDriver_Events.eUpdateBarGraphStateUsingControlTransfer), OSRDriver_Events.@null, parent, (PrtMachineValue)((parent).LEDV));
                    (parent).PrtFunContSend(this, (currFun).locals, (currFun).returnToLocation);
                    return;
                    UpdateBarGraphStateUsingControlTransfer_1:
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
                    return new UpdateBarGraphStateUsingControlTransfer_StackFrame(this, locals, retLoc);
                }
            }

            public static UpdateBarGraphStateUsingControlTransfer_Class UpdateBarGraphStateUsingControlTransfer;
            public class CheckIfSwitchStatusChanged_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return false;
                    }
                }

                internal class CheckIfSwitchStatusChanged_StackFrame : PrtFunStackFrame
                {
                    public CheckIfSwitchStatusChanged_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public CheckIfSwitchStatusChanged_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto CheckIfSwitchStatusChanged_1;
                    }

                    (parent).PrtFunContNondet(this, (currFun).locals, 1);
                    return;
                    CheckIfSwitchStatusChanged_1:
                        ;
                    if (!((PrtBoolValue)(new PrtBoolValue(((parent).continuation).ReturnAndResetNondet()))).bl)
                        goto CheckIfSwitchStatusChanged_if_0_else;
                    (parent).PrtFunContReturnVal(new PrtBoolValue(true), (currFun).locals);
                    return;
                    goto CheckIfSwitchStatusChanged_if_0_end;
                    CheckIfSwitchStatusChanged_if_0_else:
                        ;
                    (parent).PrtFunContReturnVal(new PrtBoolValue(false), (currFun).locals);
                    return;
                    CheckIfSwitchStatusChanged_if_0_end:
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
                    return new CheckIfSwitchStatusChanged_StackFrame(this, locals, retLoc);
                }
            }

            public static CheckIfSwitchStatusChanged_Class CheckIfSwitchStatusChanged;
            public class StoreSwitchAndEnableSwitchStatusChange_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return false;
                    }
                }

                internal class StoreSwitchAndEnableSwitchStatusChange_StackFrame : PrtFunStackFrame
                {
                    public StoreSwitchAndEnableSwitchStatusChange_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public StoreSwitchAndEnableSwitchStatusChange_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    return new StoreSwitchAndEnableSwitchStatusChange_StackFrame(this, locals, retLoc);
                }
            }

            public static StoreSwitchAndEnableSwitchStatusChange_Class StoreSwitchAndEnableSwitchStatusChange;
            public class CompleteDStateTransition_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return false;
                    }
                }

                internal class CompleteDStateTransition_StackFrame : PrtFunStackFrame
                {
                    public CompleteDStateTransition_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public CompleteDStateTransition_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    return new CompleteDStateTransition_StackFrame(this, locals, retLoc);
                }
            }

            public static CompleteDStateTransition_Class CompleteDStateTransition;
            public class StartDebounceTimer_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return false;
                    }
                }

                internal class StartDebounceTimer_StackFrame : PrtFunStackFrame
                {
                    public StartDebounceTimer_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public StartDebounceTimer_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto StartDebounceTimer_1;
                    }

                    (((PrtMachineValue)((parent).TimerV)).mach).PrtEnqueueEvent((PrtEventValue)(OSRDriver_Events.eStartDebounceTimer), OSRDriver_Events.@null, parent, (PrtMachineValue)((parent).TimerV));
                    (parent).PrtFunContSend(this, (currFun).locals, (currFun).returnToLocation);
                    return;
                    StartDebounceTimer_1:
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
                    return new StartDebounceTimer_StackFrame(this, locals, retLoc);
                }
            }

            public static StartDebounceTimer_Class StartDebounceTimer;
            public class SetLedStateToUnstableUsingControlTransfer_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return false;
                    }
                }

                internal class SetLedStateToUnstableUsingControlTransfer_StackFrame : PrtFunStackFrame
                {
                    public SetLedStateToUnstableUsingControlTransfer_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public SetLedStateToUnstableUsingControlTransfer_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto SetLedStateToUnstableUsingControlTransfer_1;
                    }

                    (((PrtMachineValue)((parent).LEDV)).mach).PrtEnqueueEvent((PrtEventValue)(OSRDriver_Events.eSetLedStateToUnstableUsingControlTransfer), OSRDriver_Events.@null, parent, (PrtMachineValue)((parent).LEDV));
                    (parent).PrtFunContSend(this, (currFun).locals, (currFun).returnToLocation);
                    return;
                    SetLedStateToUnstableUsingControlTransfer_1:
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
                    return new SetLedStateToUnstableUsingControlTransfer_StackFrame(this, locals, retLoc);
                }
            }

            public static SetLedStateToUnstableUsingControlTransfer_Class SetLedStateToUnstableUsingControlTransfer;
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

                    public PrtValue _payload_12
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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

                    public PrtValue _payload_11
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun3_1;
                    }

                    (((PrtMachineValue)((parent).TimerV)).mach).PrtEnqueueEvent((PrtEventValue)(OSRDriver_Events.eStopTimer), OSRDriver_Events.@null, parent, (PrtMachineValue)((parent).TimerV));
                    (parent).PrtFunContSend(this, (currFun).locals, (currFun).returnToLocation);
                    return;
                    AnonFun3_1:
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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

                    public PrtValue _payload_10
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    if (!!(OSRDriver_Events.eUnit).Equals(OSRDriver_Events.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Src\\\\PTester\\\\Regressions\\\\Interfaces\\\\OSRDriver.p (165, 4): Raised event must be non-null");
                    Console.Write("<RaiseLog> Machine OSRDriverMachine-{0} raised Event {1}\\n", (parent).instanceNumber, (((PrtEventValue)(OSRDriver_Events.eUnit)).evt).name);
                    (parent).currentTrigger = OSRDriver_Events.eUnit;
                    (parent).currentPayload = OSRDriver_Events.@null;
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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

                    public PrtValue _payload_9
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    if (!!(OSRDriver_Events.eUnit).Equals(OSRDriver_Events.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Src\\\\PTester\\\\Regressions\\\\Interfaces\\\\OSRDriver.p (154, 4): Raised event must be non-null");
                    Console.Write("<RaiseLog> Machine OSRDriverMachine-{0} raised Event {1}\\n", (parent).instanceNumber, (((PrtEventValue)(OSRDriver_Events.eUnit)).evt).name);
                    (parent).currentTrigger = OSRDriver_Events.eUnit;
                    (parent).currentPayload = OSRDriver_Events.@null;
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    return new AnonFun13_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun13_Class AnonFun13;
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

                    public PrtValue _payload_8
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun14_1;
                    }

                    (parent).PrtPushFunStackFrame(SetLedStateToStableUsingControlTransfer, (SetLedStateToStableUsingControlTransfer).CreateLocals());
                    AnonFun14_1:
                        ;
                    (SetLedStateToStableUsingControlTransfer).Execute(application, parent);
                    if (((parent).continuation).reason == PrtContinuationReason.Return)
                    {
                    }
                    else
                    {
                        (parent).PrtPushFunStackFrame(SetLedStateToStableUsingControlTransfer, (currFun).locals, 1);
                        return;
                    }

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

            public static AnonFun14_Class AnonFun14;
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    return new AnonFun15_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun15_Class AnonFun15;
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    return new AnonFun16_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun16_Class AnonFun16;
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

                    public PrtValue _payload_7
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun17_1;
                    }

                    (parent).PrtPushFunStackFrame(StartDebounceTimer, (StartDebounceTimer).CreateLocals());
                    AnonFun17_1:
                        ;
                    (StartDebounceTimer).Execute(application, parent);
                    if (((parent).continuation).reason == PrtContinuationReason.Return)
                    {
                    }
                    else
                    {
                        (parent).PrtPushFunStackFrame(StartDebounceTimer, (currFun).locals, 1);
                        return;
                    }

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

            public static AnonFun17_Class AnonFun17;
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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

            public static AnonFun18_Class AnonFun18;
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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

            public static AnonFun19_Class AnonFun19;
            public class AnonFun20_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun20_StackFrame : PrtFunStackFrame
                {
                    public AnonFun20_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun20_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    return new AnonFun20_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun20_Class AnonFun20;
            public class AnonFun21_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun21_StackFrame : PrtFunStackFrame
                {
                    public AnonFun21_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun21_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    return new AnonFun21_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun21_Class AnonFun21;
            public class AnonFun22_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun22_StackFrame : PrtFunStackFrame
                {
                    public AnonFun22_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun22_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_6
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun22_1;
                    }

                    (parent).PrtPushFunStackFrame(SetLedStateToUnstableUsingControlTransfer, (SetLedStateToUnstableUsingControlTransfer).CreateLocals());
                    AnonFun22_1:
                        ;
                    (SetLedStateToUnstableUsingControlTransfer).Execute(application, parent);
                    if (((parent).continuation).reason == PrtContinuationReason.Return)
                    {
                    }
                    else
                    {
                        (parent).PrtPushFunStackFrame(SetLedStateToUnstableUsingControlTransfer, (currFun).locals, 1);
                        return;
                    }

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
                    return new AnonFun22_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun22_Class AnonFun22;
            public class AnonFun23_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun23_StackFrame : PrtFunStackFrame
                {
                    public AnonFun23_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun23_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    return new AnonFun23_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun23_Class AnonFun23;
            public class AnonFun24_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun24_StackFrame : PrtFunStackFrame
                {
                    public AnonFun24_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun24_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    return new AnonFun24_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun24_Class AnonFun24;
            public class AnonFun25_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun25_StackFrame : PrtFunStackFrame
                {
                    public AnonFun25_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun25_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_5
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun25_1;
                    }

                    (parent).PrtPushFunStackFrame(UpdateBarGraphStateUsingControlTransfer, (UpdateBarGraphStateUsingControlTransfer).CreateLocals());
                    AnonFun25_1:
                        ;
                    (UpdateBarGraphStateUsingControlTransfer).Execute(application, parent);
                    if (((parent).continuation).reason == PrtContinuationReason.Return)
                    {
                    }
                    else
                    {
                        (parent).PrtPushFunStackFrame(UpdateBarGraphStateUsingControlTransfer, (currFun).locals, 1);
                        return;
                    }

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
                    return new AnonFun25_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun25_Class AnonFun25;
            public class AnonFun26_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun26_StackFrame : PrtFunStackFrame
                {
                    public AnonFun26_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun26_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    return new AnonFun26_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun26_Class AnonFun26;
            public class AnonFun27_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun27_StackFrame : PrtFunStackFrame
                {
                    public AnonFun27_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun27_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    return new AnonFun27_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun27_Class AnonFun27;
            public class AnonFun28_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun28_StackFrame : PrtFunStackFrame
                {
                    public AnonFun28_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun28_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    return new AnonFun28_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun28_Class AnonFun28;
            public class AnonFun29_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun29_StackFrame : PrtFunStackFrame
                {
                    public AnonFun29_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun29_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_4
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun29_1;
                        case 2:
                            goto AnonFun29_2;
                    }

                    (parent).PrtPushFunStackFrame(StoreSwitchAndEnableSwitchStatusChange, (StoreSwitchAndEnableSwitchStatusChange).CreateLocals());
                    AnonFun29_1:
                        ;
                    (StoreSwitchAndEnableSwitchStatusChange).Execute(application, parent);
                    if (((parent).continuation).reason == PrtContinuationReason.Return)
                    {
                    }
                    else
                    {
                        (parent).PrtPushFunStackFrame(StoreSwitchAndEnableSwitchStatusChange, (currFun).locals, 1);
                        return;
                    }

                    (parent).PrtPushFunStackFrame(CheckIfSwitchStatusChanged, (CheckIfSwitchStatusChanged).CreateLocals());
                    AnonFun29_2:
                        ;
                    (CheckIfSwitchStatusChanged).Execute(application, parent);
                    if (((parent).continuation).reason == PrtContinuationReason.Return)
                    {
                        (parent).check = ((parent).continuation).retVal;
                    }
                    else
                    {
                        (parent).PrtPushFunStackFrame(CheckIfSwitchStatusChanged, (currFun).locals, 2);
                        return;
                    }

                    if (!((PrtBoolValue)((parent).check)).bl)
                        goto AnonFun29_if_0_else;
                    if (!!(OSRDriver_Events.eYes).Equals(OSRDriver_Events.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Src\\\\PTester\\\\Regressions\\\\Interfaces\\\\OSRDriver.p (94, 5): Raised event must be non-null");
                    Console.Write("<RaiseLog> Machine OSRDriverMachine-{0} raised Event {1}\\n", (parent).instanceNumber, (((PrtEventValue)(OSRDriver_Events.eYes)).evt).name);
                    (parent).currentTrigger = OSRDriver_Events.eYes;
                    (parent).currentPayload = OSRDriver_Events.@null;
                    (parent).PrtFunContRaise();
                    return;
                    goto AnonFun29_if_0_end;
                    AnonFun29_if_0_else:
                        ;
                    if (!!(OSRDriver_Events.eNo).Equals(OSRDriver_Events.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Src\\\\PTester\\\\Regressions\\\\Interfaces\\\\OSRDriver.p (96, 5): Raised event must be non-null");
                    Console.Write("<RaiseLog> Machine OSRDriverMachine-{0} raised Event {1}\\n", (parent).instanceNumber, (((PrtEventValue)(OSRDriver_Events.eNo)).evt).name);
                    (parent).currentTrigger = OSRDriver_Events.eNo;
                    (parent).currentPayload = OSRDriver_Events.@null;
                    (parent).PrtFunContRaise();
                    return;
                    AnonFun29_if_0_end:
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
                    return new AnonFun29_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun29_Class AnonFun29;
            public class AnonFun30_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun30_StackFrame : PrtFunStackFrame
                {
                    public AnonFun30_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun30_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    return new AnonFun30_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun30_Class AnonFun30;
            public class AnonFun31_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun31_StackFrame : PrtFunStackFrame
                {
                    public AnonFun31_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun31_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    return new AnonFun31_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun31_Class AnonFun31;
            public class AnonFun32_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun32_StackFrame : PrtFunStackFrame
                {
                    public AnonFun32_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun32_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    return new AnonFun32_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun32_Class AnonFun32;
            public class AnonFun33_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun33_StackFrame : PrtFunStackFrame
                {
                    public AnonFun33_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun33_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun33_1;
                    }

                    (parent).PrtPushFunStackFrame(CompleteDStateTransition, (CompleteDStateTransition).CreateLocals());
                    AnonFun33_1:
                        ;
                    (CompleteDStateTransition).Execute(application, parent);
                    if (((parent).continuation).reason == PrtContinuationReason.Return)
                    {
                    }
                    else
                    {
                        (parent).PrtPushFunStackFrame(CompleteDStateTransition, (currFun).locals, 1);
                        return;
                    }

                    if (!!(OSRDriver_Events.eOperationSuccess).Equals(OSRDriver_Events.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Src\\\\PTester\\\\Regressions\\\\Interfaces\\\\OSRDriver.p (57, 4): Raised event must be non-null");
                    Console.Write("<RaiseLog> Machine OSRDriverMachine-{0} raised Event {1}\\n", (parent).instanceNumber, (((PrtEventValue)(OSRDriver_Events.eOperationSuccess)).evt).name);
                    (parent).currentTrigger = OSRDriver_Events.eOperationSuccess;
                    (parent).currentPayload = OSRDriver_Events.@null;
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
                    return new AnonFun33_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun33_Class AnonFun33;
            public class AnonFun34_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun34_StackFrame : PrtFunStackFrame
                {
                    public AnonFun34_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun34_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    return new AnonFun34_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun34_Class AnonFun34;
            public class AnonFun35_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun35_StackFrame : PrtFunStackFrame
                {
                    public AnonFun35_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun35_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    return new AnonFun35_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun35_Class AnonFun35;
            public class AnonFun36_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun36_StackFrame : PrtFunStackFrame
                {
                    public AnonFun36_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun36_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    return new AnonFun36_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun36_Class AnonFun36;
            public class AnonFun37_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun37_StackFrame : PrtFunStackFrame
                {
                    public AnonFun37_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun37_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    return new AnonFun37_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun37_Class AnonFun37;
            public class AnonFun38_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun38_StackFrame : PrtFunStackFrame
                {
                    public AnonFun38_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun38_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    return new AnonFun38_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun38_Class AnonFun38;
            public class AnonFun39_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun39_StackFrame : PrtFunStackFrame
                {
                    public AnonFun39_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun39_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    return new AnonFun39_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun39_Class AnonFun39;
            public class AnonFun40_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun40_StackFrame : PrtFunStackFrame
                {
                    public AnonFun40_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun40_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun40_1;
                    }

                    (parent).PrtPushFunStackFrame(CompleteDStateTransition, (CompleteDStateTransition).CreateLocals());
                    AnonFun40_1:
                        ;
                    (CompleteDStateTransition).Execute(application, parent);
                    if (((parent).continuation).reason == PrtContinuationReason.Return)
                    {
                    }
                    else
                    {
                        (parent).PrtPushFunStackFrame(CompleteDStateTransition, (currFun).locals, 1);
                        return;
                    }

                    if (!!(OSRDriver_Events.eOperationSuccess).Equals(OSRDriver_Events.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Src\\\\PTester\\\\Regressions\\\\Interfaces\\\\OSRDriver.p (37, 4): Raised event must be non-null");
                    Console.Write("<RaiseLog> Machine OSRDriverMachine-{0} raised Event {1}\\n", (parent).instanceNumber, (((PrtEventValue)(OSRDriver_Events.eOperationSuccess)).evt).name);
                    (parent).currentTrigger = OSRDriver_Events.eOperationSuccess;
                    (parent).currentPayload = OSRDriver_Events.@null;
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
                    return new AnonFun40_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun40_Class AnonFun40;
            public class AnonFun41_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun41_StackFrame : PrtFunStackFrame
                {
                    public AnonFun41_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun41_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    return new AnonFun41_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun41_Class AnonFun41;
            public class AnonFun42_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun42_StackFrame : PrtFunStackFrame
                {
                    public AnonFun42_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun42_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    return new AnonFun42_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun42_Class AnonFun42;
            public class AnonFun43_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun43_StackFrame : PrtFunStackFrame
                {
                    public AnonFun43_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun43_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    return new AnonFun43_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun43_Class AnonFun43;
            public class AnonFun44_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun44_StackFrame : PrtFunStackFrame
                {
                    public AnonFun44_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun44_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    return new AnonFun44_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun44_Class AnonFun44;
            public class AnonFun45_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun45_StackFrame : PrtFunStackFrame
                {
                    public AnonFun45_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun45_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    return new AnonFun45_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun45_Class AnonFun45;
            public class AnonFun46_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun46_StackFrame : PrtFunStackFrame
                {
                    public AnonFun46_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun46_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun46_1;
                        case 2:
                            goto AnonFun46_2;
                        case 3:
                            goto AnonFun46_3;
                    }

                    (parent).TimerV = (application).CreateInterfaceOrMachine((parent).renamedName, "TimerInterface", parent.self);
                    (parent).PrtFunContNewMachine(this, (currFun).locals, 1);
                    return;
                    AnonFun46_1:
                        ;
                    (parent).LEDV = (application).CreateInterfaceOrMachine((parent).renamedName, "LEDInterface", parent.self);
                    (parent).PrtFunContNewMachine(this, (currFun).locals, 2);
                    return;
                    AnonFun46_2:
                        ;
                    (parent).SwitchV = (application).CreateInterfaceOrMachine((parent).renamedName, "SwitchInterface", parent.self);
                    (parent).PrtFunContNewMachine(this, (currFun).locals, 3);
                    return;
                    AnonFun46_3:
                        ;
                    if (!!(OSRDriver_Events.eUnit).Equals(OSRDriver_Events.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Src\\\\PTester\\\\Regressions\\\\Interfaces\\\\OSRDriver.p (20, 4): Raised event must be non-null");
                    Console.Write("<RaiseLog> Machine OSRDriverMachine-{0} raised Event {1}\\n", (parent).instanceNumber, (((PrtEventValue)(OSRDriver_Events.eUnit)).evt).name);
                    (parent).currentTrigger = OSRDriver_Events.eUnit;
                    (parent).currentPayload = OSRDriver_Events.@null;
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
                    return new AnonFun46_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun46_Class AnonFun46;
            public class AnonFun47_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun47_StackFrame : PrtFunStackFrame
                {
                    public AnonFun47_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun47_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    return new AnonFun47_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun47_Class AnonFun47;
            public class AnonFun48_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun48_StackFrame : PrtFunStackFrame
                {
                    public AnonFun48_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun48_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    return new AnonFun48_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun48_Class AnonFun48;
            public class AnonFun49_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun49_StackFrame : PrtFunStackFrame
                {
                    public AnonFun49_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun49_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_13
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    if (!!(OSRDriver_Events.eTimerStopped).Equals(OSRDriver_Events.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Src\\\\PTester\\\\Regressions\\\\Interfaces\\\\OSRDriver.p (198, 4): Raised event must be non-null");
                    Console.Write("<RaiseLog> Machine OSRDriverMachine-{0} raised Event {1}\\n", (parent).instanceNumber, (((PrtEventValue)(OSRDriver_Events.eTimerStopped)).evt).name);
                    (parent).currentTrigger = OSRDriver_Events.eTimerStopped;
                    (parent).currentPayload = OSRDriver_Events.@null;
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
                    return new AnonFun49_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun49_Class AnonFun49;
            public class AnonFun50_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun50_StackFrame : PrtFunStackFrame
                {
                    public AnonFun50_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun50_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    return new AnonFun50_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun50_Class AnonFun50;
            public class AnonFun51_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun51_StackFrame : PrtFunStackFrame
                {
                    public AnonFun51_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun51_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    return new AnonFun51_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun51_Class AnonFun51;
            public class AnonFun52_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun52_StackFrame : PrtFunStackFrame
                {
                    public AnonFun52_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun52_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    return new AnonFun52_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun52_Class AnonFun52;
            public class AnonFun53_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun53_StackFrame : PrtFunStackFrame
                {
                    public AnonFun53_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun53_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    return new AnonFun53_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun53_Class AnonFun53;
            public class AnonFun54_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun54_StackFrame : PrtFunStackFrame
                {
                    public AnonFun54_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun54_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    return new AnonFun54_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun54_Class AnonFun54;
            public class AnonFun55_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun55_StackFrame : PrtFunStackFrame
                {
                    public AnonFun55_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun55_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    return new AnonFun55_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun55_Class AnonFun55;
            public class AnonFun56_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun56_StackFrame : PrtFunStackFrame
                {
                    public AnonFun56_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun56_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    return new AnonFun56_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun56_Class AnonFun56;
            public class AnonFun57_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun57_StackFrame : PrtFunStackFrame
                {
                    public AnonFun57_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun57_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    return new AnonFun57_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun57_Class AnonFun57;
            public class AnonFun58_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun58_StackFrame : PrtFunStackFrame
                {
                    public AnonFun58_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun58_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    return new AnonFun58_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun58_Class AnonFun58;
            public class AnonFun59_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun59_StackFrame : PrtFunStackFrame
                {
                    public AnonFun59_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun59_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    return new AnonFun59_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun59_Class AnonFun59;
            public class AnonFun60_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun60_StackFrame : PrtFunStackFrame
                {
                    public AnonFun60_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun60_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    return new AnonFun60_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun60_Class AnonFun60;
            public class AnonFun61_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun61_StackFrame : PrtFunStackFrame
                {
                    public AnonFun61_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun61_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    return new AnonFun61_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun61_Class AnonFun61;
            public class AnonFun62_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun62_StackFrame : PrtFunStackFrame
                {
                    public AnonFun62_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun62_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    return new AnonFun62_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun62_Class AnonFun62;
            public class AnonFun63_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun63_StackFrame : PrtFunStackFrame
                {
                    public AnonFun63_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun63_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    return new AnonFun63_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun63_Class AnonFun63;
            public class AnonFun64_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun64_StackFrame : PrtFunStackFrame
                {
                    public AnonFun64_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun64_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
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
                    OSRDriverMachine parent = (OSRDriverMachine)(_parent);
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
                    return new AnonFun64_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun64_Class AnonFun64;
            public class OSRDriverMachine_sWaitingForTimerToFlushDriver_Class : PrtState
            {
                public OSRDriverMachine_sWaitingForTimerToFlushDriver_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static OSRDriverMachine_sWaitingForTimerToFlushDriver_Class OSRDriverMachine_sWaitingForTimerToFlushDriver;
            public class OSRDriverMachine_sStoppingTimerDriver_Class : PrtState
            {
                public OSRDriverMachine_sStoppingTimerDriver_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static OSRDriverMachine_sStoppingTimerDriver_Class OSRDriverMachine_sStoppingTimerDriver;
            public class OSRDriverMachine_sStoppingTimerOnD0ExitDriver_Class : PrtState
            {
                public OSRDriverMachine_sStoppingTimerOnD0ExitDriver_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static OSRDriverMachine_sStoppingTimerOnD0ExitDriver_Class OSRDriverMachine_sStoppingTimerOnD0ExitDriver;
            public class OSRDriverMachine_sStoppingTimerOnStatusChangeDriver_Class : PrtState
            {
                public OSRDriverMachine_sStoppingTimerOnStatusChangeDriver_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static OSRDriverMachine_sStoppingTimerOnStatusChangeDriver_Class OSRDriverMachine_sStoppingTimerOnStatusChangeDriver;
            public class OSRDriverMachine_sUpdatingLedStateToStableDriver_Class : PrtState
            {
                public OSRDriverMachine_sUpdatingLedStateToStableDriver_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static OSRDriverMachine_sUpdatingLedStateToStableDriver_Class OSRDriverMachine_sUpdatingLedStateToStableDriver;
            public class OSRDriverMachine_sWaitingForTimerDriver_Class : PrtState
            {
                public OSRDriverMachine_sWaitingForTimerDriver_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static OSRDriverMachine_sWaitingForTimerDriver_Class OSRDriverMachine_sWaitingForTimerDriver;
            public class OSRDriverMachine_sUpdatingLedStateToUnstableDriver_Class : PrtState
            {
                public OSRDriverMachine_sUpdatingLedStateToUnstableDriver_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static OSRDriverMachine_sUpdatingLedStateToUnstableDriver_Class OSRDriverMachine_sUpdatingLedStateToUnstableDriver;
            public class OSRDriverMachine_sUpdatingBarGraphStateDriver_Class : PrtState
            {
                public OSRDriverMachine_sUpdatingBarGraphStateDriver_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static OSRDriverMachine_sUpdatingBarGraphStateDriver_Class OSRDriverMachine_sUpdatingBarGraphStateDriver;
            public class OSRDriverMachine_sStoringSwitchAndCheckingIfStateChangedDriver_Class : PrtState
            {
                public OSRDriverMachine_sStoringSwitchAndCheckingIfStateChangedDriver_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static OSRDriverMachine_sStoringSwitchAndCheckingIfStateChangedDriver_Class OSRDriverMachine_sStoringSwitchAndCheckingIfStateChangedDriver;
            public class OSRDriverMachine_sCompletingD0ExitDriver_Class : PrtState
            {
                public OSRDriverMachine_sCompletingD0ExitDriver_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static OSRDriverMachine_sCompletingD0ExitDriver_Class OSRDriverMachine_sCompletingD0ExitDriver;
            public class OSRDriverMachine_sWaitingForSwitchStatusChangeDriver_Class : PrtState
            {
                public OSRDriverMachine_sWaitingForSwitchStatusChangeDriver_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static OSRDriverMachine_sWaitingForSwitchStatusChangeDriver_Class OSRDriverMachine_sWaitingForSwitchStatusChangeDriver;
            public class OSRDriverMachine_sCompleteD0EntryDriver_Class : PrtState
            {
                public OSRDriverMachine_sCompleteD0EntryDriver_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static OSRDriverMachine_sCompleteD0EntryDriver_Class OSRDriverMachine_sCompleteD0EntryDriver;
            public class OSRDriverMachine_sDxDriver_Class : PrtState
            {
                public OSRDriverMachine_sDxDriver_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static OSRDriverMachine_sDxDriver_Class OSRDriverMachine_sDxDriver;
            public class OSRDriverMachine_Driver_Init_Class : PrtState
            {
                public OSRDriverMachine_Driver_Init_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static OSRDriverMachine_Driver_Init_Class OSRDriverMachine_Driver_Init;
            public class OSRDriverMachine_sReturningTimerStoppedDriver_Class : PrtState
            {
                public OSRDriverMachine_sReturningTimerStoppedDriver_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static OSRDriverMachine_sReturningTimerStoppedDriver_Class OSRDriverMachine_sReturningTimerStoppedDriver;
            static OSRDriverMachine()
            {
                ignore = new ignore_Class();
                SetLedStateToStableUsingControlTransfer = new SetLedStateToStableUsingControlTransfer_Class();
                UpdateBarGraphStateUsingControlTransfer = new UpdateBarGraphStateUsingControlTransfer_Class();
                CheckIfSwitchStatusChanged = new CheckIfSwitchStatusChanged_Class();
                StoreSwitchAndEnableSwitchStatusChange = new StoreSwitchAndEnableSwitchStatusChange_Class();
                CompleteDStateTransition = new CompleteDStateTransition_Class();
                StartDebounceTimer = new StartDebounceTimer_Class();
                SetLedStateToUnstableUsingControlTransfer = new SetLedStateToUnstableUsingControlTransfer_Class();
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
                AnonFun13 = new AnonFun13_Class();
                AnonFun14 = new AnonFun14_Class();
                AnonFun15 = new AnonFun15_Class();
                AnonFun16 = new AnonFun16_Class();
                AnonFun17 = new AnonFun17_Class();
                AnonFun18 = new AnonFun18_Class();
                AnonFun19 = new AnonFun19_Class();
                AnonFun20 = new AnonFun20_Class();
                AnonFun21 = new AnonFun21_Class();
                AnonFun22 = new AnonFun22_Class();
                AnonFun23 = new AnonFun23_Class();
                AnonFun24 = new AnonFun24_Class();
                AnonFun25 = new AnonFun25_Class();
                AnonFun26 = new AnonFun26_Class();
                AnonFun27 = new AnonFun27_Class();
                AnonFun28 = new AnonFun28_Class();
                AnonFun29 = new AnonFun29_Class();
                AnonFun30 = new AnonFun30_Class();
                AnonFun31 = new AnonFun31_Class();
                AnonFun32 = new AnonFun32_Class();
                AnonFun33 = new AnonFun33_Class();
                AnonFun34 = new AnonFun34_Class();
                AnonFun35 = new AnonFun35_Class();
                AnonFun36 = new AnonFun36_Class();
                AnonFun37 = new AnonFun37_Class();
                AnonFun38 = new AnonFun38_Class();
                AnonFun39 = new AnonFun39_Class();
                AnonFun40 = new AnonFun40_Class();
                AnonFun41 = new AnonFun41_Class();
                AnonFun42 = new AnonFun42_Class();
                AnonFun43 = new AnonFun43_Class();
                AnonFun44 = new AnonFun44_Class();
                AnonFun45 = new AnonFun45_Class();
                AnonFun46 = new AnonFun46_Class();
                AnonFun47 = new AnonFun47_Class();
                AnonFun48 = new AnonFun48_Class();
                AnonFun49 = new AnonFun49_Class();
                AnonFun50 = new AnonFun50_Class();
                AnonFun51 = new AnonFun51_Class();
                AnonFun52 = new AnonFun52_Class();
                AnonFun53 = new AnonFun53_Class();
                AnonFun54 = new AnonFun54_Class();
                AnonFun55 = new AnonFun55_Class();
                AnonFun56 = new AnonFun56_Class();
                AnonFun57 = new AnonFun57_Class();
                AnonFun58 = new AnonFun58_Class();
                AnonFun59 = new AnonFun59_Class();
                AnonFun60 = new AnonFun60_Class();
                AnonFun61 = new AnonFun61_Class();
                AnonFun62 = new AnonFun62_Class();
                AnonFun63 = new AnonFun63_Class();
                AnonFun64 = new AnonFun64_Class();
                OSRDriverMachine_sWaitingForTimerToFlushDriver = new OSRDriverMachine_sWaitingForTimerToFlushDriver_Class("OSRDriverMachine_sWaitingForTimerToFlushDriver", AnonFun0, AnonFun1, false, StateTemperature.Warm);
                OSRDriverMachine_sStoppingTimerDriver = new OSRDriverMachine_sStoppingTimerDriver_Class("OSRDriverMachine_sStoppingTimerDriver", AnonFun3, AnonFun4, false, StateTemperature.Warm);
                OSRDriverMachine_sStoppingTimerOnD0ExitDriver = new OSRDriverMachine_sStoppingTimerOnD0ExitDriver_Class("OSRDriverMachine_sStoppingTimerOnD0ExitDriver", AnonFun8, AnonFun9, false, StateTemperature.Warm);
                OSRDriverMachine_sStoppingTimerOnStatusChangeDriver = new OSRDriverMachine_sStoppingTimerOnStatusChangeDriver_Class("OSRDriverMachine_sStoppingTimerOnStatusChangeDriver", AnonFun11, AnonFun12, false, StateTemperature.Warm);
                OSRDriverMachine_sUpdatingLedStateToStableDriver = new OSRDriverMachine_sUpdatingLedStateToStableDriver_Class("OSRDriverMachine_sUpdatingLedStateToStableDriver", AnonFun14, AnonFun15, false, StateTemperature.Warm);
                OSRDriverMachine_sWaitingForTimerDriver = new OSRDriverMachine_sWaitingForTimerDriver_Class("OSRDriverMachine_sWaitingForTimerDriver", AnonFun17, AnonFun18, false, StateTemperature.Warm);
                OSRDriverMachine_sUpdatingLedStateToUnstableDriver = new OSRDriverMachine_sUpdatingLedStateToUnstableDriver_Class("OSRDriverMachine_sUpdatingLedStateToUnstableDriver", AnonFun22, AnonFun23, false, StateTemperature.Warm);
                OSRDriverMachine_sUpdatingBarGraphStateDriver = new OSRDriverMachine_sUpdatingBarGraphStateDriver_Class("OSRDriverMachine_sUpdatingBarGraphStateDriver", AnonFun25, AnonFun26, false, StateTemperature.Warm);
                OSRDriverMachine_sStoringSwitchAndCheckingIfStateChangedDriver = new OSRDriverMachine_sStoringSwitchAndCheckingIfStateChangedDriver_Class("OSRDriverMachine_sStoringSwitchAndCheckingIfStateChangedDriver", AnonFun29, AnonFun30, false, StateTemperature.Warm);
                OSRDriverMachine_sCompletingD0ExitDriver = new OSRDriverMachine_sCompletingD0ExitDriver_Class("OSRDriverMachine_sCompletingD0ExitDriver", AnonFun33, AnonFun34, false, StateTemperature.Warm);
                OSRDriverMachine_sWaitingForSwitchStatusChangeDriver = new OSRDriverMachine_sWaitingForSwitchStatusChangeDriver_Class("OSRDriverMachine_sWaitingForSwitchStatusChangeDriver", AnonFun36, AnonFun37, false, StateTemperature.Warm);
                OSRDriverMachine_sCompleteD0EntryDriver = new OSRDriverMachine_sCompleteD0EntryDriver_Class("OSRDriverMachine_sCompleteD0EntryDriver", AnonFun40, AnonFun41, false, StateTemperature.Warm);
                OSRDriverMachine_sDxDriver = new OSRDriverMachine_sDxDriver_Class("OSRDriverMachine_sDxDriver", AnonFun43, AnonFun44, false, StateTemperature.Warm);
                OSRDriverMachine_Driver_Init = new OSRDriverMachine_Driver_Init_Class("OSRDriverMachine_Driver_Init", AnonFun46, AnonFun47, false, StateTemperature.Warm);
                OSRDriverMachine_sReturningTimerStoppedDriver = new OSRDriverMachine_sReturningTimerStoppedDriver_Class("OSRDriverMachine_sReturningTimerStoppedDriver", AnonFun49, AnonFun50, false, StateTemperature.Warm);
                OSRDriverMachine_sWaitingForTimerToFlushDriver.dos.Add(OSRDriver_Events.eD0Entry, ignore);
                OSRDriverMachine_sWaitingForTimerToFlushDriver.dos.Add(OSRDriver_Events.eD0Entry, PrtFun.IgnoreFun);
                OSRDriverMachine_sWaitingForTimerToFlushDriver.deferredSet.Add(OSRDriver_Events.eSwitchStatusChange);
                OSRDriverMachine_sWaitingForTimerToFlushDriver.deferredSet.Add(OSRDriver_Events.eD0Exit);
                PrtTransition transition_1 = new PrtTransition(AnonFun2, OSRDriverMachine_sReturningTimerStoppedDriver, false);
                OSRDriverMachine_sWaitingForTimerToFlushDriver.transitions.Add(OSRDriver_Events.eTimerFired, transition_1);
                OSRDriverMachine_sStoppingTimerDriver.dos.Add(OSRDriver_Events.eD0Entry, ignore);
                OSRDriverMachine_sStoppingTimerDriver.dos.Add(OSRDriver_Events.eD0Entry, PrtFun.IgnoreFun);
                PrtTransition transition_2 = new PrtTransition(AnonFun5, OSRDriverMachine_sReturningTimerStoppedDriver, false);
                OSRDriverMachine_sStoppingTimerDriver.transitions.Add(OSRDriver_Events.eTimerFired, transition_2);
                PrtTransition transition_3 = new PrtTransition(AnonFun6, OSRDriverMachine_sWaitingForTimerToFlushDriver, false);
                OSRDriverMachine_sStoppingTimerDriver.transitions.Add(OSRDriver_Events.eStoppingFailure, transition_3);
                PrtTransition transition_4 = new PrtTransition(AnonFun7, OSRDriverMachine_sReturningTimerStoppedDriver, false);
                OSRDriverMachine_sStoppingTimerDriver.transitions.Add(OSRDriver_Events.eStoppingSuccess, transition_4);
                OSRDriverMachine_sStoppingTimerOnD0ExitDriver.dos.Add(OSRDriver_Events.eD0Entry, ignore);
                OSRDriverMachine_sStoppingTimerOnD0ExitDriver.dos.Add(OSRDriver_Events.eD0Entry, PrtFun.IgnoreFun);
                OSRDriverMachine_sStoppingTimerOnD0ExitDriver.deferredSet.Add(OSRDriver_Events.eSwitchStatusChange);
                OSRDriverMachine_sStoppingTimerOnD0ExitDriver.deferredSet.Add(OSRDriver_Events.eD0Exit);
                PrtTransition transition_5 = new PrtTransition(PrtFun.IgnoreFun, OSRDriverMachine_sStoppingTimerDriver, true);
                OSRDriverMachine_sStoppingTimerOnD0ExitDriver.transitions.Add(OSRDriver_Events.eUnit, transition_5);
                PrtTransition transition_6 = new PrtTransition(AnonFun10, OSRDriverMachine_sCompletingD0ExitDriver, false);
                OSRDriverMachine_sStoppingTimerOnD0ExitDriver.transitions.Add(OSRDriver_Events.eTimerStopped, transition_6);
                OSRDriverMachine_sStoppingTimerOnStatusChangeDriver.dos.Add(OSRDriver_Events.eD0Entry, ignore);
                OSRDriverMachine_sStoppingTimerOnStatusChangeDriver.dos.Add(OSRDriver_Events.eD0Entry, PrtFun.IgnoreFun);
                OSRDriverMachine_sStoppingTimerOnStatusChangeDriver.deferredSet.Add(OSRDriver_Events.eSwitchStatusChange);
                OSRDriverMachine_sStoppingTimerOnStatusChangeDriver.deferredSet.Add(OSRDriver_Events.eD0Exit);
                PrtTransition transition_7 = new PrtTransition(AnonFun13, OSRDriverMachine_sStoringSwitchAndCheckingIfStateChangedDriver, false);
                OSRDriverMachine_sStoppingTimerOnStatusChangeDriver.transitions.Add(OSRDriver_Events.eTimerStopped, transition_7);
                PrtTransition transition_8 = new PrtTransition(PrtFun.IgnoreFun, OSRDriverMachine_sStoppingTimerDriver, true);
                OSRDriverMachine_sStoppingTimerOnStatusChangeDriver.transitions.Add(OSRDriver_Events.eUnit, transition_8);
                OSRDriverMachine_sUpdatingLedStateToStableDriver.dos.Add(OSRDriver_Events.eD0Entry, ignore);
                OSRDriverMachine_sUpdatingLedStateToStableDriver.dos.Add(OSRDriver_Events.eD0Entry, PrtFun.IgnoreFun);
                OSRDriverMachine_sUpdatingLedStateToStableDriver.deferredSet.Add(OSRDriver_Events.eSwitchStatusChange);
                OSRDriverMachine_sUpdatingLedStateToStableDriver.deferredSet.Add(OSRDriver_Events.eD0Exit);
                PrtTransition transition_9 = new PrtTransition(AnonFun16, OSRDriverMachine_sWaitingForSwitchStatusChangeDriver, false);
                OSRDriverMachine_sUpdatingLedStateToStableDriver.transitions.Add(OSRDriver_Events.eTransferSuccess, transition_9);
                OSRDriverMachine_sWaitingForTimerDriver.dos.Add(OSRDriver_Events.eD0Entry, ignore);
                OSRDriverMachine_sWaitingForTimerDriver.dos.Add(OSRDriver_Events.eD0Entry, PrtFun.IgnoreFun);
                PrtTransition transition_10 = new PrtTransition(AnonFun19, OSRDriverMachine_sStoppingTimerOnD0ExitDriver, false);
                OSRDriverMachine_sWaitingForTimerDriver.transitions.Add(OSRDriver_Events.eD0Exit, transition_10);
                PrtTransition transition_11 = new PrtTransition(AnonFun20, OSRDriverMachine_sStoppingTimerOnStatusChangeDriver, false);
                OSRDriverMachine_sWaitingForTimerDriver.transitions.Add(OSRDriver_Events.eSwitchStatusChange, transition_11);
                PrtTransition transition_12 = new PrtTransition(AnonFun21, OSRDriverMachine_sUpdatingLedStateToStableDriver, false);
                OSRDriverMachine_sWaitingForTimerDriver.transitions.Add(OSRDriver_Events.eTimerFired, transition_12);
                OSRDriverMachine_sUpdatingLedStateToUnstableDriver.dos.Add(OSRDriver_Events.eD0Entry, ignore);
                OSRDriverMachine_sUpdatingLedStateToUnstableDriver.dos.Add(OSRDriver_Events.eD0Entry, PrtFun.IgnoreFun);
                OSRDriverMachine_sUpdatingLedStateToUnstableDriver.deferredSet.Add(OSRDriver_Events.eSwitchStatusChange);
                OSRDriverMachine_sUpdatingLedStateToUnstableDriver.deferredSet.Add(OSRDriver_Events.eD0Exit);
                PrtTransition transition_13 = new PrtTransition(AnonFun24, OSRDriverMachine_sWaitingForTimerDriver, false);
                OSRDriverMachine_sUpdatingLedStateToUnstableDriver.transitions.Add(OSRDriver_Events.eTransferSuccess, transition_13);
                OSRDriverMachine_sUpdatingBarGraphStateDriver.dos.Add(OSRDriver_Events.eD0Entry, ignore);
                OSRDriverMachine_sUpdatingBarGraphStateDriver.dos.Add(OSRDriver_Events.eD0Entry, PrtFun.IgnoreFun);
                OSRDriverMachine_sUpdatingBarGraphStateDriver.deferredSet.Add(OSRDriver_Events.eSwitchStatusChange);
                OSRDriverMachine_sUpdatingBarGraphStateDriver.deferredSet.Add(OSRDriver_Events.eD0Exit);
                PrtTransition transition_14 = new PrtTransition(AnonFun27, OSRDriverMachine_sUpdatingLedStateToUnstableDriver, false);
                OSRDriverMachine_sUpdatingBarGraphStateDriver.transitions.Add(OSRDriver_Events.eTransferFailure, transition_14);
                PrtTransition transition_15 = new PrtTransition(AnonFun28, OSRDriverMachine_sUpdatingLedStateToUnstableDriver, false);
                OSRDriverMachine_sUpdatingBarGraphStateDriver.transitions.Add(OSRDriver_Events.eTransferSuccess, transition_15);
                OSRDriverMachine_sStoringSwitchAndCheckingIfStateChangedDriver.dos.Add(OSRDriver_Events.eD0Entry, ignore);
                OSRDriverMachine_sStoringSwitchAndCheckingIfStateChangedDriver.dos.Add(OSRDriver_Events.eD0Entry, PrtFun.IgnoreFun);
                PrtTransition transition_16 = new PrtTransition(AnonFun31, OSRDriverMachine_sWaitingForTimerDriver, false);
                OSRDriverMachine_sStoringSwitchAndCheckingIfStateChangedDriver.transitions.Add(OSRDriver_Events.eNo, transition_16);
                PrtTransition transition_17 = new PrtTransition(AnonFun32, OSRDriverMachine_sUpdatingBarGraphStateDriver, false);
                OSRDriverMachine_sStoringSwitchAndCheckingIfStateChangedDriver.transitions.Add(OSRDriver_Events.eYes, transition_17);
                PrtTransition transition_18 = new PrtTransition(AnonFun35, OSRDriverMachine_sDxDriver, false);
                OSRDriverMachine_sCompletingD0ExitDriver.transitions.Add(OSRDriver_Events.eOperationSuccess, transition_18);
                OSRDriverMachine_sWaitingForSwitchStatusChangeDriver.dos.Add(OSRDriver_Events.eD0Entry, ignore);
                OSRDriverMachine_sWaitingForSwitchStatusChangeDriver.dos.Add(OSRDriver_Events.eD0Entry, PrtFun.IgnoreFun);
                PrtTransition transition_19 = new PrtTransition(AnonFun38, OSRDriverMachine_sStoringSwitchAndCheckingIfStateChangedDriver, false);
                OSRDriverMachine_sWaitingForSwitchStatusChangeDriver.transitions.Add(OSRDriver_Events.eSwitchStatusChange, transition_19);
                PrtTransition transition_20 = new PrtTransition(AnonFun39, OSRDriverMachine_sCompletingD0ExitDriver, false);
                OSRDriverMachine_sWaitingForSwitchStatusChangeDriver.transitions.Add(OSRDriver_Events.eD0Exit, transition_20);
                OSRDriverMachine_sCompleteD0EntryDriver.deferredSet.Add(OSRDriver_Events.eSwitchStatusChange);
                PrtTransition transition_21 = new PrtTransition(AnonFun42, OSRDriverMachine_sWaitingForSwitchStatusChangeDriver, false);
                OSRDriverMachine_sCompleteD0EntryDriver.transitions.Add(OSRDriver_Events.eOperationSuccess, transition_21);
                OSRDriverMachine_sDxDriver.dos.Add(OSRDriver_Events.eD0Exit, ignore);
                OSRDriverMachine_sDxDriver.dos.Add(OSRDriver_Events.eD0Exit, PrtFun.IgnoreFun);
                OSRDriverMachine_sDxDriver.deferredSet.Add(OSRDriver_Events.eSwitchStatusChange);
                PrtTransition transition_22 = new PrtTransition(AnonFun45, OSRDriverMachine_sCompleteD0EntryDriver, false);
                OSRDriverMachine_sDxDriver.transitions.Add(OSRDriver_Events.eD0Entry, transition_22);
                OSRDriverMachine_Driver_Init.deferredSet.Add(OSRDriver_Events.eSwitchStatusChange);
                PrtTransition transition_23 = new PrtTransition(AnonFun48, OSRDriverMachine_sDxDriver, false);
                OSRDriverMachine_Driver_Init.transitions.Add(OSRDriver_Events.eUnit, transition_23);
                OSRDriverMachine_sReturningTimerStoppedDriver.dos.Add(OSRDriver_Events.eD0Entry, ignore);
                OSRDriverMachine_sReturningTimerStoppedDriver.dos.Add(OSRDriver_Events.eD0Entry, PrtFun.IgnoreFun);
            }
        }
    }
}
