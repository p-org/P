#pragma warning disable CS0162, CS0164, CS0168, C0649
namespace P.Program
{
    using P.Runtime;
    using System;
    using System.Collections.Generic;

    public partial class Application : StateImpl
    {
        private class Events_1178678920
        {
            public static PrtEventValue halt;
            public static PrtEventValue @null;
            public static PrtEventValue eObjectEncountered;
            public static PrtEventValue eStopTimerReturned;
            public static PrtEventValue eUnit;
            public static PrtEventValue eStopDoorCloseTimer;
            public static PrtEventValue eStartDoorCloseTimer;
            public static PrtEventValue eSendCommandToResetDoor;
            public static PrtEventValue eSendCommandToStopDoor;
            public static PrtEventValue eSendCommandToCloseDoor;
            public static PrtEventValue eSendCommandToOpenDoor;
            public static PrtEventValue eOperationFailure;
            public static PrtEventValue eOperationSuccess;
            public static PrtEventValue eTimerFired;
            public static PrtEventValue eObjectDetected;
            public static PrtEventValue eDoorStopped;
            public static PrtEventValue eDoorClosed;
            public static PrtEventValue eDoorOpened;
            public static PrtEventValue eResetDoor;
            public static PrtEventValue eCloseDoor;
            public static PrtEventValue eOpenDoor;
            static Events_1178678920()
            {
                halt = new PrtEventValue(new PrtEvent("halt", Types_1178678920.typeNull, 1, false));
                @null = new PrtEventValue(new PrtEvent("null", Types_1178678920.typeNull, 1, false));
                eObjectEncountered = new PrtEventValue(new PrtEvent("eObjectEncountered", Types_1178678920.typeNull, 1, false));
                eStopTimerReturned = new PrtEventValue(new PrtEvent("eStopTimerReturned", Types_1178678920.typeNull, 1, false));
                eUnit = new PrtEventValue(new PrtEvent("eUnit", Types_1178678920.typeNull, 1, false));
                eStopDoorCloseTimer = new PrtEventValue(new PrtEvent("eStopDoorCloseTimer", Types_1178678920.typeNull, 1, true));
                eStartDoorCloseTimer = new PrtEventValue(new PrtEvent("eStartDoorCloseTimer", Types_1178678920.typeNull, 1, true));
                eSendCommandToResetDoor = new PrtEventValue(new PrtEvent("eSendCommandToResetDoor", Types_1178678920.typeNull, 1, true));
                eSendCommandToStopDoor = new PrtEventValue(new PrtEvent("eSendCommandToStopDoor", Types_1178678920.typeNull, 1, true));
                eSendCommandToCloseDoor = new PrtEventValue(new PrtEvent("eSendCommandToCloseDoor", Types_1178678920.typeNull, 1, true));
                eSendCommandToOpenDoor = new PrtEventValue(new PrtEvent("eSendCommandToOpenDoor", Types_1178678920.typeNull, 1, true));
                eOperationFailure = new PrtEventValue(new PrtEvent("eOperationFailure", Types_1178678920.typeNull, 1, false));
                eOperationSuccess = new PrtEventValue(new PrtEvent("eOperationSuccess", Types_1178678920.typeNull, 1, false));
                eTimerFired = new PrtEventValue(new PrtEvent("eTimerFired", Types_1178678920.typeNull, 1, true));
                eObjectDetected = new PrtEventValue(new PrtEvent("eObjectDetected", Types_1178678920.typeNull, 1, false));
                eDoorStopped = new PrtEventValue(new PrtEvent("eDoorStopped", Types_1178678920.typeNull, 1, false));
                eDoorClosed = new PrtEventValue(new PrtEvent("eDoorClosed", Types_1178678920.typeNull, 1, false));
                eDoorOpened = new PrtEventValue(new PrtEvent("eDoorOpened", Types_1178678920.typeNull, 1, false));
                eResetDoor = new PrtEventValue(new PrtEvent("eResetDoor", Types_1178678920.typeNull, 1, false));
                eCloseDoor = new PrtEventValue(new PrtEvent("eCloseDoor", Types_1178678920.typeNull, 1, true));
                eOpenDoor = new PrtEventValue(new PrtEvent("eOpenDoor", Types_1178678920.typeNull, 1, true));
            }
        }

        private class Types_1178678920
        {
            public static PrtType typeNull;
            public static PrtNamedTupleType typeNmdTupType_1;
            public static PrtNamedTupleType typeNmdTupType_2;
            public static PrtType typeMachine;
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
            public static PrtNamedTupleType typeNmdTupType_16;
            public static PrtNamedTupleType typeNmdTupType_17;
            public static PrtNamedTupleType typeNmdTupType_18;
            public static PrtNamedTupleType typeNmdTupType_19;
            public static PrtNamedTupleType typeNmdTupType_20;
            public static PrtNamedTupleType typeNmdTupType_21;
            public static PrtNamedTupleType typeNmdTupType_22;
            public static PrtNamedTupleType typeNmdTupType_23;
            public static PrtNamedTupleType typeNmdTupType_24;
            public static PrtNamedTupleType typeNmdTupType_25;
            public static PrtNamedTupleType typeNmdTupType_26;
            public static PrtType typeBool;
            public static PrtType typeInt;
            public static PrtType typeEvent;
            public static PrtType typeAny;
            static Types_1178678920()
            {
                Types_1178678920.typeNull = new PrtNullType();
                Types_1178678920.typeNmdTupType_1 = new PrtNamedTupleType(new object[]{"_payload_20", Types_1178678920.typeNull});
                Types_1178678920.typeNmdTupType_2 = new PrtNamedTupleType(new object[]{"_payload_19", Types_1178678920.typeNull});
                Types_1178678920.typeMachine = new PrtMachineType();
                Types_1178678920.typeNmdTupType_4 = new PrtNamedTupleType(new object[]{"payload", Types_1178678920.typeMachine});
                Types_1178678920.typeNmdTupType_5 = new PrtNamedTupleType(new object[]{"_payload_18", Types_1178678920.typeNull});
                Types_1178678920.typeNmdTupType_6 = new PrtNamedTupleType(new object[]{"_payload_8", Types_1178678920.typeNull});
                Types_1178678920.typeNmdTupType_7 = new PrtNamedTupleType(new object[]{"_payload_7", Types_1178678920.typeNull});
                Types_1178678920.typeNmdTupType_8 = new PrtNamedTupleType(new object[]{"_payload_6", Types_1178678920.typeNull});
                Types_1178678920.typeNmdTupType_9 = new PrtNamedTupleType(new object[]{"_payload_5", Types_1178678920.typeNull});
                Types_1178678920.typeNmdTupType_10 = new PrtNamedTupleType(new object[]{"_payload_4", Types_1178678920.typeNull});
                Types_1178678920.typeNmdTupType_11 = new PrtNamedTupleType(new object[]{"_payload_3", Types_1178678920.typeNull});
                Types_1178678920.typeNmdTupType_12 = new PrtNamedTupleType(new object[]{"_payload_2", Types_1178678920.typeNull});
                Types_1178678920.typeNmdTupType_13 = new PrtNamedTupleType(new object[]{"_payload_11", Types_1178678920.typeNull});
                Types_1178678920.typeNmdTupType_14 = new PrtNamedTupleType(new object[]{"_payload_9", Types_1178678920.typeNull});
                Types_1178678920.typeNmdTupType_15 = new PrtNamedTupleType(new object[]{"_payload_10", Types_1178678920.typeNull});
                Types_1178678920.typeNmdTupType_16 = new PrtNamedTupleType(new object[]{"_payload_12", Types_1178678920.typeNull});
                Types_1178678920.typeNmdTupType_17 = new PrtNamedTupleType(new object[]{"_payload_16", Types_1178678920.typeNull});
                Types_1178678920.typeNmdTupType_18 = new PrtNamedTupleType(new object[]{"_payload_15", Types_1178678920.typeNull});
                Types_1178678920.typeNmdTupType_19 = new PrtNamedTupleType(new object[]{"_payload_14", Types_1178678920.typeNull});
                Types_1178678920.typeNmdTupType_20 = new PrtNamedTupleType(new object[]{"_payload_13", Types_1178678920.typeNull});
                Types_1178678920.typeNmdTupType_21 = new PrtNamedTupleType(new object[]{"_payload_1", Types_1178678920.typeNull});
                Types_1178678920.typeNmdTupType_22 = new PrtNamedTupleType(new object[]{"_payload_0", Types_1178678920.typeNull});
                Types_1178678920.typeNmdTupType_23 = new PrtNamedTupleType(new object[]{"_payload_22", Types_1178678920.typeNull});
                Types_1178678920.typeNmdTupType_24 = new PrtNamedTupleType(new object[]{"_payload_21", Types_1178678920.typeNull});
                Types_1178678920.typeNmdTupType_25 = new PrtNamedTupleType(new object[]{"_payload_skip", Types_1178678920.typeNull});
                Types_1178678920.typeNmdTupType_26 = new PrtNamedTupleType(new object[]{"_payload_17", Types_1178678920.typeNull});
                Types_1178678920.typeBool = new PrtBoolType();
                Types_1178678920.typeInt = new PrtIntType();
                Types_1178678920.typeEvent = new PrtEventType();
                Types_1178678920.typeAny = new PrtAnyType();
            }
        }

        public static PrtImplMachine CreateMachine_Main(StateImpl application, PrtValue payload)
        {
            var machine = new Main(application, PrtImplMachine.DefaultMaxBufferSize, false);
            (((machine).self).permissions).Add(Events_1178678920.eObjectEncountered);
            (((machine).self).permissions).Add(Events_1178678920.eStopTimerReturned);
            (((machine).self).permissions).Add(Events_1178678920.eUnit);
            (((machine).self).permissions).Add(Events_1178678920.eStopDoorCloseTimer);
            (((machine).self).permissions).Add(Events_1178678920.eStartDoorCloseTimer);
            (((machine).self).permissions).Add(Events_1178678920.eSendCommandToResetDoor);
            (((machine).self).permissions).Add(Events_1178678920.eSendCommandToStopDoor);
            (((machine).self).permissions).Add(Events_1178678920.eSendCommandToCloseDoor);
            (((machine).self).permissions).Add(Events_1178678920.eSendCommandToOpenDoor);
            (((machine).self).permissions).Add(Events_1178678920.eOperationFailure);
            (((machine).self).permissions).Add(Events_1178678920.eOperationSuccess);
            (((machine).self).permissions).Add(Events_1178678920.eTimerFired);
            (((machine).self).permissions).Add(Events_1178678920.eObjectDetected);
            (((machine).self).permissions).Add(Events_1178678920.eDoorStopped);
            (((machine).self).permissions).Add(Events_1178678920.eDoorClosed);
            (((machine).self).permissions).Add(Events_1178678920.eDoorOpened);
            (((machine).self).permissions).Add(Events_1178678920.eResetDoor);
            (((machine).self).permissions).Add(Events_1178678920.eCloseDoor);
            (((machine).self).permissions).Add(Events_1178678920.eOpenDoor);
            ((machine).sends).Add(Events_1178678920.eObjectEncountered);
            ((machine).sends).Add(Events_1178678920.eStopTimerReturned);
            ((machine).sends).Add(Events_1178678920.eUnit);
            ((machine).sends).Add(Events_1178678920.eStopDoorCloseTimer);
            ((machine).sends).Add(Events_1178678920.eStartDoorCloseTimer);
            ((machine).sends).Add(Events_1178678920.eSendCommandToResetDoor);
            ((machine).sends).Add(Events_1178678920.eSendCommandToStopDoor);
            ((machine).sends).Add(Events_1178678920.eSendCommandToCloseDoor);
            ((machine).sends).Add(Events_1178678920.eSendCommandToOpenDoor);
            ((machine).sends).Add(Events_1178678920.eOperationFailure);
            ((machine).sends).Add(Events_1178678920.eOperationSuccess);
            ((machine).sends).Add(Events_1178678920.eTimerFired);
            ((machine).sends).Add(Events_1178678920.eObjectDetected);
            ((machine).sends).Add(Events_1178678920.eDoorStopped);
            ((machine).sends).Add(Events_1178678920.eDoorClosed);
            ((machine).sends).Add(Events_1178678920.eDoorOpened);
            ((machine).sends).Add(Events_1178678920.eResetDoor);
            ((machine).sends).Add(Events_1178678920.eCloseDoor);
            ((machine).sends).Add(Events_1178678920.eOpenDoor);
            (machine).currentPayload = payload;
            return machine;
        }

        public static PrtImplMachine CreateMachine_Elevator(StateImpl application, PrtValue payload)
        {
            var machine = new Elevator(application, PrtImplMachine.DefaultMaxBufferSize, false);
            (((machine).self).permissions).Add(Events_1178678920.eObjectEncountered);
            (((machine).self).permissions).Add(Events_1178678920.eStopTimerReturned);
            (((machine).self).permissions).Add(Events_1178678920.eUnit);
            (((machine).self).permissions).Add(Events_1178678920.eStopDoorCloseTimer);
            (((machine).self).permissions).Add(Events_1178678920.eStartDoorCloseTimer);
            (((machine).self).permissions).Add(Events_1178678920.eSendCommandToResetDoor);
            (((machine).self).permissions).Add(Events_1178678920.eSendCommandToStopDoor);
            (((machine).self).permissions).Add(Events_1178678920.eSendCommandToCloseDoor);
            (((machine).self).permissions).Add(Events_1178678920.eSendCommandToOpenDoor);
            (((machine).self).permissions).Add(Events_1178678920.eOperationFailure);
            (((machine).self).permissions).Add(Events_1178678920.eOperationSuccess);
            (((machine).self).permissions).Add(Events_1178678920.eTimerFired);
            (((machine).self).permissions).Add(Events_1178678920.eObjectDetected);
            (((machine).self).permissions).Add(Events_1178678920.eDoorStopped);
            (((machine).self).permissions).Add(Events_1178678920.eDoorClosed);
            (((machine).self).permissions).Add(Events_1178678920.eDoorOpened);
            (((machine).self).permissions).Add(Events_1178678920.eResetDoor);
            (((machine).self).permissions).Add(Events_1178678920.eCloseDoor);
            (((machine).self).permissions).Add(Events_1178678920.eOpenDoor);
            ((machine).sends).Add(Events_1178678920.eObjectEncountered);
            ((machine).sends).Add(Events_1178678920.eStopTimerReturned);
            ((machine).sends).Add(Events_1178678920.eUnit);
            ((machine).sends).Add(Events_1178678920.eStopDoorCloseTimer);
            ((machine).sends).Add(Events_1178678920.eStartDoorCloseTimer);
            ((machine).sends).Add(Events_1178678920.eSendCommandToResetDoor);
            ((machine).sends).Add(Events_1178678920.eSendCommandToStopDoor);
            ((machine).sends).Add(Events_1178678920.eSendCommandToCloseDoor);
            ((machine).sends).Add(Events_1178678920.eSendCommandToOpenDoor);
            ((machine).sends).Add(Events_1178678920.eOperationFailure);
            ((machine).sends).Add(Events_1178678920.eOperationSuccess);
            ((machine).sends).Add(Events_1178678920.eTimerFired);
            ((machine).sends).Add(Events_1178678920.eObjectDetected);
            ((machine).sends).Add(Events_1178678920.eDoorStopped);
            ((machine).sends).Add(Events_1178678920.eDoorClosed);
            ((machine).sends).Add(Events_1178678920.eDoorOpened);
            ((machine).sends).Add(Events_1178678920.eResetDoor);
            ((machine).sends).Add(Events_1178678920.eCloseDoor);
            ((machine).sends).Add(Events_1178678920.eOpenDoor);
            (machine).currentPayload = payload;
            return machine;
        }

        public static PrtImplMachine CreateMachine_Timer(StateImpl application, PrtValue payload)
        {
            var machine = new Timer(application, PrtImplMachine.DefaultMaxBufferSize, false);
            (((machine).self).permissions).Add(Events_1178678920.eObjectEncountered);
            (((machine).self).permissions).Add(Events_1178678920.eStopTimerReturned);
            (((machine).self).permissions).Add(Events_1178678920.eUnit);
            (((machine).self).permissions).Add(Events_1178678920.eStopDoorCloseTimer);
            (((machine).self).permissions).Add(Events_1178678920.eStartDoorCloseTimer);
            (((machine).self).permissions).Add(Events_1178678920.eSendCommandToResetDoor);
            (((machine).self).permissions).Add(Events_1178678920.eSendCommandToStopDoor);
            (((machine).self).permissions).Add(Events_1178678920.eSendCommandToCloseDoor);
            (((machine).self).permissions).Add(Events_1178678920.eSendCommandToOpenDoor);
            (((machine).self).permissions).Add(Events_1178678920.eOperationFailure);
            (((machine).self).permissions).Add(Events_1178678920.eOperationSuccess);
            (((machine).self).permissions).Add(Events_1178678920.eTimerFired);
            (((machine).self).permissions).Add(Events_1178678920.eObjectDetected);
            (((machine).self).permissions).Add(Events_1178678920.eDoorStopped);
            (((machine).self).permissions).Add(Events_1178678920.eDoorClosed);
            (((machine).self).permissions).Add(Events_1178678920.eDoorOpened);
            (((machine).self).permissions).Add(Events_1178678920.eResetDoor);
            (((machine).self).permissions).Add(Events_1178678920.eCloseDoor);
            (((machine).self).permissions).Add(Events_1178678920.eOpenDoor);
            ((machine).sends).Add(Events_1178678920.eObjectEncountered);
            ((machine).sends).Add(Events_1178678920.eStopTimerReturned);
            ((machine).sends).Add(Events_1178678920.eUnit);
            ((machine).sends).Add(Events_1178678920.eStopDoorCloseTimer);
            ((machine).sends).Add(Events_1178678920.eStartDoorCloseTimer);
            ((machine).sends).Add(Events_1178678920.eSendCommandToResetDoor);
            ((machine).sends).Add(Events_1178678920.eSendCommandToStopDoor);
            ((machine).sends).Add(Events_1178678920.eSendCommandToCloseDoor);
            ((machine).sends).Add(Events_1178678920.eSendCommandToOpenDoor);
            ((machine).sends).Add(Events_1178678920.eOperationFailure);
            ((machine).sends).Add(Events_1178678920.eOperationSuccess);
            ((machine).sends).Add(Events_1178678920.eTimerFired);
            ((machine).sends).Add(Events_1178678920.eObjectDetected);
            ((machine).sends).Add(Events_1178678920.eDoorStopped);
            ((machine).sends).Add(Events_1178678920.eDoorClosed);
            ((machine).sends).Add(Events_1178678920.eDoorOpened);
            ((machine).sends).Add(Events_1178678920.eResetDoor);
            ((machine).sends).Add(Events_1178678920.eCloseDoor);
            ((machine).sends).Add(Events_1178678920.eOpenDoor);
            (machine).currentPayload = payload;
            return machine;
        }

        public static PrtImplMachine CreateMachine_Door(StateImpl application, PrtValue payload)
        {
            var machine = new Door(application, PrtImplMachine.DefaultMaxBufferSize, false);
            (((machine).self).permissions).Add(Events_1178678920.eOperationSuccess);
            (((machine).self).permissions).Add(Events_1178678920.eTimerFired);
            (((machine).self).permissions).Add(Events_1178678920.eObjectDetected);
            (((machine).self).permissions).Add(Events_1178678920.eDoorStopped);
            (((machine).self).permissions).Add(Events_1178678920.eDoorClosed);
            (((machine).self).permissions).Add(Events_1178678920.eDoorOpened);
            (((machine).self).permissions).Add(Events_1178678920.eResetDoor);
            (((machine).self).permissions).Add(Events_1178678920.eCloseDoor);
            (((machine).self).permissions).Add(Events_1178678920.eOpenDoor);
            (((machine).self).permissions).Add(Events_1178678920.eObjectEncountered);
            (((machine).self).permissions).Add(Events_1178678920.eStopTimerReturned);
            (((machine).self).permissions).Add(Events_1178678920.eUnit);
            (((machine).self).permissions).Add(Events_1178678920.eStopDoorCloseTimer);
            (((machine).self).permissions).Add(Events_1178678920.eStartDoorCloseTimer);
            (((machine).self).permissions).Add(Events_1178678920.eSendCommandToResetDoor);
            (((machine).self).permissions).Add(Events_1178678920.eSendCommandToStopDoor);
            (((machine).self).permissions).Add(Events_1178678920.eSendCommandToCloseDoor);
            (((machine).self).permissions).Add(Events_1178678920.eSendCommandToOpenDoor);
            (((machine).self).permissions).Add(Events_1178678920.eOperationFailure);
            ((machine).sends).Add(Events_1178678920.eObjectEncountered);
            ((machine).sends).Add(Events_1178678920.eStopTimerReturned);
            ((machine).sends).Add(Events_1178678920.eUnit);
            ((machine).sends).Add(Events_1178678920.eStopDoorCloseTimer);
            ((machine).sends).Add(Events_1178678920.eStartDoorCloseTimer);
            ((machine).sends).Add(Events_1178678920.eSendCommandToResetDoor);
            ((machine).sends).Add(Events_1178678920.eSendCommandToStopDoor);
            ((machine).sends).Add(Events_1178678920.eSendCommandToCloseDoor);
            ((machine).sends).Add(Events_1178678920.eSendCommandToOpenDoor);
            ((machine).sends).Add(Events_1178678920.eOperationFailure);
            ((machine).sends).Add(Events_1178678920.eOperationSuccess);
            ((machine).sends).Add(Events_1178678920.eTimerFired);
            ((machine).sends).Add(Events_1178678920.eObjectDetected);
            ((machine).sends).Add(Events_1178678920.eDoorStopped);
            ((machine).sends).Add(Events_1178678920.eDoorClosed);
            ((machine).sends).Add(Events_1178678920.eDoorOpened);
            ((machine).sends).Add(Events_1178678920.eResetDoor);
            ((machine).sends).Add(Events_1178678920.eCloseDoor);
            ((machine).sends).Add(Events_1178678920.eOpenDoor);
            (machine).currentPayload = payload;
            return machine;
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

            public PrtValue ElevatorV
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
                (fields).Add(PrtValue.PrtMkDefaultValue(Types_1178678920.typeMachine));
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
                        case 4:
                            goto AnonFun3_4;
                    }

                    (parent).PrtFunContNondet(this, (currFun).locals, 4);
                    return;
                    AnonFun3_4:
                        ;
                    if (!((PrtBoolValue)(new PrtBoolValue(((parent).continuation).ReturnAndResetNondet()))).bl)
                        goto AnonFun3_if_1_else;
                    (((PrtMachineValue)((parent).ElevatorV)).mach).PrtEnqueueEvent((PrtEventValue)(Events_1178678920.eOpenDoor), Events_1178678920.@null, parent, (PrtMachineValue)((parent).ElevatorV));
                    (parent).PrtFunContSend(this, (currFun).locals, 1);
                    return;
                    AnonFun3_1:
                        ;
                    goto AnonFun3_if_1_end;
                    AnonFun3_if_1_else:
                        ;
                    (parent).PrtFunContNondet(this, (currFun).locals, 3);
                    return;
                    AnonFun3_3:
                        ;
                    if (!((PrtBoolValue)(new PrtBoolValue(((parent).continuation).ReturnAndResetNondet()))).bl)
                        goto AnonFun3_if_0_else;
                    (((PrtMachineValue)((parent).ElevatorV)).mach).PrtEnqueueEvent((PrtEventValue)(Events_1178678920.eCloseDoor), Events_1178678920.@null, parent, (PrtMachineValue)((parent).ElevatorV));
                    (parent).PrtFunContSend(this, (currFun).locals, 2);
                    return;
                    AnonFun3_2:
                        ;
                    goto AnonFun3_if_0_end;
                    AnonFun3_if_0_else:
                        ;
                    AnonFun3_if_0_end:
                        ;
                    AnonFun3_if_1_end:
                        ;
                    if (!!(Events_1178678920.eUnit).Equals(Events_1178678920.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Tst\\\\RegressionTests\\\\Integration\\\\Correct\\\\Elevator\\\\Elevator.p (148, 13): Raised event must be non-null");
                    Console.Write("<RaiseLog> Machine Main-{0} raised Event {1}\n", (parent).instanceNumber, (((PrtEventValue)(Events_1178678920.eUnit)).evt).name);
                    (parent).currentTrigger = Events_1178678920.eUnit;
                    (parent).currentPayload = Events_1178678920.@null;
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
                    Main parent = (Main)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun6_1;
                    }

                    (parent).ElevatorV = (application).CreateInterfaceOrMachine((parent).renamedName, "Elevator", Events_1178678920.@null);
                    (parent).PrtFunContNewMachine(this, (currFun).locals, 1);
                    return;
                    AnonFun6_1:
                        ;
                    if (!!(Events_1178678920.eUnit).Equals(Events_1178678920.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Tst\\\\RegressionTests\\\\Integration\\\\Correct\\\\Elevator\\\\Elevator.p (135, 13): Raised event must be non-null");
                    Console.Write("<RaiseLog> Machine Main-{0} raised Event {1}\n", (parent).instanceNumber, (((PrtEventValue)(Events_1178678920.eUnit)).evt).name);
                    (parent).currentTrigger = Events_1178678920.eUnit;
                    (parent).currentPayload = Events_1178678920.@null;
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
            public class Main_Loop_Class : PrtState
            {
                public Main_Loop_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Main_Loop_Class Main_Loop;
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
                AnonFun0 = new AnonFun0_Class();
                AnonFun1 = new AnonFun1_Class();
                AnonFun2 = new AnonFun2_Class();
                AnonFun3 = new AnonFun3_Class();
                AnonFun4 = new AnonFun4_Class();
                AnonFun5 = new AnonFun5_Class();
                AnonFun6 = new AnonFun6_Class();
                AnonFun7 = new AnonFun7_Class();
                Main_Loop = new Main_Loop_Class("Main_Loop", AnonFun3, AnonFun4, false, StateTemperature.Warm);
                Main_Init = new Main_Init_Class("Main_Init", AnonFun6, AnonFun7, false, StateTemperature.Warm);
                PrtTransition transition_1 = new PrtTransition(AnonFun5, Main_Loop, false);
                Main_Loop.transitions.Add(Events_1178678920.eUnit, transition_1);
                PrtTransition transition_2 = new PrtTransition(AnonFun0, Main_Loop, false);
                Main_Init.transitions.Add(Events_1178678920.eUnit, transition_2);
            }
        }

        public class Elevator : PrtImplMachine
        {
            public override PrtState StartState
            {
                get
                {
                    return Elevator_Init;
                }
            }

            public PrtValue TimerV
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

            public PrtValue DoorV
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
                return new Elevator();
            }

            public override int NextInstanceNumber(StateImpl app)
            {
                return app.NextMachineInstanceNumber(this.GetType());
            }

            public override string Name
            {
                get
                {
                    return "Elevator";
                }
            }

            public Elevator(): base ()
            {
            }

            public Elevator(StateImpl app, int maxB, bool assume): base (app, maxB, assume)
            {
                (fields).Add(PrtValue.PrtMkDefaultValue(Types_1178678920.typeMachine));
                (fields).Add(PrtValue.PrtMkDefaultValue(Types_1178678920.typeMachine));
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
                    Elevator parent = (Elevator)(_parent);
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
                    Elevator parent = (Elevator)(_parent);
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
                    Elevator parent = (Elevator)(_parent);
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
                    Elevator parent = (Elevator)(_parent);
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
                    Elevator parent = (Elevator)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun3_1;
                    }

                    (((PrtMachineValue)((parent).TimerV)).mach).PrtEnqueueEvent((PrtEventValue)(Events_1178678920.eStopDoorCloseTimer), Events_1178678920.@null, parent, (PrtMachineValue)((parent).TimerV));
                    (parent).PrtFunContSend(this, (currFun).locals, 1);
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
                    Elevator parent = (Elevator)(_parent);
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
                    Elevator parent = (Elevator)(_parent);
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
                    Elevator parent = (Elevator)(_parent);
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
                    Elevator parent = (Elevator)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun7_1;
                    }

                    (((PrtMachineValue)((parent).DoorV)).mach).PrtEnqueueEvent((PrtEventValue)(Events_1178678920.eSendCommandToStopDoor), Events_1178678920.@null, parent, (PrtMachineValue)((parent).DoorV));
                    (parent).PrtFunContSend(this, (currFun).locals, 1);
                    return;
                    AnonFun7_1:
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
                    Elevator parent = (Elevator)(_parent);
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
                    Elevator parent = (Elevator)(_parent);
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
                    Elevator parent = (Elevator)(_parent);
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
                    Elevator parent = (Elevator)(_parent);
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
                    Elevator parent = (Elevator)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun12_1;
                    }

                    (((PrtMachineValue)((parent).DoorV)).mach).PrtEnqueueEvent((PrtEventValue)(Events_1178678920.eSendCommandToCloseDoor), Events_1178678920.@null, parent, (PrtMachineValue)((parent).DoorV));
                    (parent).PrtFunContSend(this, (currFun).locals, 1);
                    return;
                    AnonFun12_1:
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
                    Elevator parent = (Elevator)(_parent);
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
                    Elevator parent = (Elevator)(_parent);
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
                    Elevator parent = (Elevator)(_parent);
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
                    Elevator parent = (Elevator)(_parent);
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
                    Elevator parent = (Elevator)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun17_1;
                    }

                    (((PrtMachineValue)((parent).TimerV)).mach).PrtEnqueueEvent((PrtEventValue)(Events_1178678920.eStartDoorCloseTimer), Events_1178678920.@null, parent, (PrtMachineValue)((parent).TimerV));
                    (parent).PrtFunContSend(this, (currFun).locals, 1);
                    return;
                    AnonFun17_1:
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
                    Elevator parent = (Elevator)(_parent);
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
                    Elevator parent = (Elevator)(_parent);
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
                    Elevator parent = (Elevator)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun20_1;
                        case 2:
                            goto AnonFun20_2;
                    }

                    (((PrtMachineValue)((parent).DoorV)).mach).PrtEnqueueEvent((PrtEventValue)(Events_1178678920.eSendCommandToResetDoor), Events_1178678920.@null, parent, (PrtMachineValue)((parent).DoorV));
                    (parent).PrtFunContSend(this, (currFun).locals, 1);
                    return;
                    AnonFun20_1:
                        ;
                    (((PrtMachineValue)((parent).TimerV)).mach).PrtEnqueueEvent((PrtEventValue)(Events_1178678920.eStartDoorCloseTimer), Events_1178678920.@null, parent, (PrtMachineValue)((parent).TimerV));
                    (parent).PrtFunContSend(this, (currFun).locals, 2);
                    return;
                    AnonFun20_2:
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
                    Elevator parent = (Elevator)(_parent);
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
                    Elevator parent = (Elevator)(_parent);
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
                    Elevator parent = (Elevator)(_parent);
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
                    Elevator parent = (Elevator)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun24_1;
                    }

                    (((PrtMachineValue)((parent).DoorV)).mach).PrtEnqueueEvent((PrtEventValue)(Events_1178678920.eSendCommandToOpenDoor), Events_1178678920.@null, parent, (PrtMachineValue)((parent).DoorV));
                    (parent).PrtFunContSend(this, (currFun).locals, 1);
                    return;
                    AnonFun24_1:
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
                    Elevator parent = (Elevator)(_parent);
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
                    Elevator parent = (Elevator)(_parent);
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
                    Elevator parent = (Elevator)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun27_1;
                    }

                    (((PrtMachineValue)((parent).DoorV)).mach).PrtEnqueueEvent((PrtEventValue)(Events_1178678920.eSendCommandToResetDoor), Events_1178678920.@null, parent, (PrtMachineValue)((parent).DoorV));
                    (parent).PrtFunContSend(this, (currFun).locals, 1);
                    return;
                    AnonFun27_1:
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
                    Elevator parent = (Elevator)(_parent);
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
                    Elevator parent = (Elevator)(_parent);
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
                    Elevator parent = (Elevator)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun30_1;
                        case 2:
                            goto AnonFun30_2;
                    }

                    (parent).TimerV = (application).CreateInterfaceOrMachine((parent).renamedName, "Timer", parent.self);
                    (parent).PrtFunContNewMachine(this, (currFun).locals, 1);
                    return;
                    AnonFun30_1:
                        ;
                    (parent).DoorV = (application).CreateInterfaceOrMachine((parent).renamedName, "Door", parent.self);
                    (parent).PrtFunContNewMachine(this, (currFun).locals, 2);
                    return;
                    AnonFun30_2:
                        ;
                    if (!!(Events_1178678920.eUnit).Equals(Events_1178678920.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Tst\\\\RegressionTests\\\\Integration\\\\Correct\\\\Elevator\\\\Elevator.p (28, 13): Raised event must be non-null");
                    Console.Write("<RaiseLog> Machine Elevator-{0} raised Event {1}\n", (parent).instanceNumber, (((PrtEventValue)(Events_1178678920.eUnit)).evt).name);
                    (parent).currentTrigger = Events_1178678920.eUnit;
                    (parent).currentPayload = Events_1178678920.@null;
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
                    Elevator parent = (Elevator)(_parent);
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
                    Elevator parent = (Elevator)(_parent);
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
                    Elevator parent = (Elevator)(_parent);
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
                    Elevator parent = (Elevator)(_parent);
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
                    Elevator parent = (Elevator)(_parent);
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
                    Elevator parent = (Elevator)(_parent);
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
                    Elevator parent = (Elevator)(_parent);
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
                    Elevator parent = (Elevator)(_parent);
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
                    Elevator parent = (Elevator)(_parent);
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
                    Elevator parent = (Elevator)(_parent);
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
                    Elevator parent = (Elevator)(_parent);
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
                    Elevator parent = (Elevator)(_parent);
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
                    Elevator parent = (Elevator)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    if (!!(Events_1178678920.eStopTimerReturned).Equals(Events_1178678920.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Tst\\\\RegressionTests\\\\Integration\\\\Correct\\\\Elevator\\\\Elevator.p (124, 13): Raised event must be non-null");
                    Console.Write("<RaiseLog> Machine Elevator-{0} raised Event {1}\n", (parent).instanceNumber, (((PrtEventValue)(Events_1178678920.eStopTimerReturned)).evt).name);
                    (parent).currentTrigger = Events_1178678920.eStopTimerReturned;
                    (parent).currentPayload = Events_1178678920.@null;
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
                    Elevator parent = (Elevator)(_parent);
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
            public class Elevator_WaitingForTimer_Class : PrtState
            {
                public Elevator_WaitingForTimer_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Elevator_WaitingForTimer_Class Elevator_WaitingForTimer;
            public class Elevator_StoppingTimer_Class : PrtState
            {
                public Elevator_StoppingTimer_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Elevator_StoppingTimer_Class Elevator_StoppingTimer;
            public class Elevator_StoppingDoor_Class : PrtState
            {
                public Elevator_StoppingDoor_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Elevator_StoppingDoor_Class Elevator_StoppingDoor;
            public class Elevator_DoorClosing_Class : PrtState
            {
                public Elevator_DoorClosing_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Elevator_DoorClosing_Class Elevator_DoorClosing;
            public class Elevator_DoorOpenedOkToClose_Class : PrtState
            {
                public Elevator_DoorOpenedOkToClose_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Elevator_DoorOpenedOkToClose_Class Elevator_DoorOpenedOkToClose;
            public class Elevator_DoorOpened_Class : PrtState
            {
                public Elevator_DoorOpened_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Elevator_DoorOpened_Class Elevator_DoorOpened;
            public class Elevator_DoorOpening_Class : PrtState
            {
                public Elevator_DoorOpening_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Elevator_DoorOpening_Class Elevator_DoorOpening;
            public class Elevator_DoorClosed_Class : PrtState
            {
                public Elevator_DoorClosed_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Elevator_DoorClosed_Class Elevator_DoorClosed;
            public class Elevator_Init_Class : PrtState
            {
                public Elevator_Init_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Elevator_Init_Class Elevator_Init;
            public class Elevator_ReturnState_Class : PrtState
            {
                public Elevator_ReturnState_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Elevator_ReturnState_Class Elevator_ReturnState;
            static Elevator()
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
                Elevator_WaitingForTimer = new Elevator_WaitingForTimer_Class("Elevator_WaitingForTimer", AnonFun0, AnonFun1, false, StateTemperature.Warm);
                Elevator_StoppingTimer = new Elevator_StoppingTimer_Class("Elevator_StoppingTimer", AnonFun3, AnonFun4, false, StateTemperature.Warm);
                Elevator_StoppingDoor = new Elevator_StoppingDoor_Class("Elevator_StoppingDoor", AnonFun7, AnonFun8, false, StateTemperature.Warm);
                Elevator_DoorClosing = new Elevator_DoorClosing_Class("Elevator_DoorClosing", AnonFun12, AnonFun13, false, StateTemperature.Warm);
                Elevator_DoorOpenedOkToClose = new Elevator_DoorOpenedOkToClose_Class("Elevator_DoorOpenedOkToClose", AnonFun17, AnonFun18, false, StateTemperature.Warm);
                Elevator_DoorOpened = new Elevator_DoorOpened_Class("Elevator_DoorOpened", AnonFun20, AnonFun21, false, StateTemperature.Warm);
                Elevator_DoorOpening = new Elevator_DoorOpening_Class("Elevator_DoorOpening", AnonFun24, AnonFun25, false, StateTemperature.Warm);
                Elevator_DoorClosed = new Elevator_DoorClosed_Class("Elevator_DoorClosed", AnonFun27, AnonFun28, false, StateTemperature.Warm);
                Elevator_Init = new Elevator_Init_Class("Elevator_Init", AnonFun30, AnonFun31, false, StateTemperature.Warm);
                Elevator_ReturnState = new Elevator_ReturnState_Class("Elevator_ReturnState", AnonFun43, AnonFun44, false, StateTemperature.Warm);
                Elevator_WaitingForTimer.deferredSet.Add(Events_1178678920.eObjectDetected);
                Elevator_WaitingForTimer.deferredSet.Add(Events_1178678920.eCloseDoor);
                Elevator_WaitingForTimer.deferredSet.Add(Events_1178678920.eOpenDoor);
                PrtTransition transition_1 = new PrtTransition(AnonFun2, Elevator_ReturnState, false);
                Elevator_WaitingForTimer.transitions.Add(Events_1178678920.eTimerFired, transition_1);
                Elevator_StoppingTimer.deferredSet.Add(Events_1178678920.eObjectDetected);
                Elevator_StoppingTimer.deferredSet.Add(Events_1178678920.eCloseDoor);
                Elevator_StoppingTimer.deferredSet.Add(Events_1178678920.eOpenDoor);
                PrtTransition transition_2 = new PrtTransition(AnonFun5, Elevator_WaitingForTimer, false);
                Elevator_StoppingTimer.transitions.Add(Events_1178678920.eOperationFailure, transition_2);
                PrtTransition transition_3 = new PrtTransition(AnonFun6, Elevator_ReturnState, false);
                Elevator_StoppingTimer.transitions.Add(Events_1178678920.eOperationSuccess, transition_3);
                Elevator_StoppingDoor.dos.Add(Events_1178678920.eObjectDetected, ignore);
                Elevator_StoppingDoor.dos.Add(Events_1178678920.eOpenDoor, ignore);
                Elevator_StoppingDoor.deferredSet.Add(Events_1178678920.eCloseDoor);
                PrtTransition transition_4 = new PrtTransition(AnonFun9, Elevator_DoorOpening, false);
                Elevator_StoppingDoor.transitions.Add(Events_1178678920.eDoorStopped, transition_4);
                PrtTransition transition_5 = new PrtTransition(AnonFun10, Elevator_DoorClosed, false);
                Elevator_StoppingDoor.transitions.Add(Events_1178678920.eDoorClosed, transition_5);
                PrtTransition transition_6 = new PrtTransition(AnonFun11, Elevator_DoorOpened, false);
                Elevator_StoppingDoor.transitions.Add(Events_1178678920.eDoorOpened, transition_6);
                Elevator_DoorClosing.deferredSet.Add(Events_1178678920.eCloseDoor);
                PrtTransition transition_7 = new PrtTransition(AnonFun14, Elevator_DoorOpening, false);
                Elevator_DoorClosing.transitions.Add(Events_1178678920.eObjectDetected, transition_7);
                PrtTransition transition_8 = new PrtTransition(AnonFun15, Elevator_DoorClosed, false);
                Elevator_DoorClosing.transitions.Add(Events_1178678920.eDoorClosed, transition_8);
                PrtTransition transition_9 = new PrtTransition(AnonFun16, Elevator_StoppingDoor, false);
                Elevator_DoorClosing.transitions.Add(Events_1178678920.eOpenDoor, transition_9);
                Elevator_DoorOpenedOkToClose.deferredSet.Add(Events_1178678920.eOpenDoor);
                PrtTransition transition_10 = new PrtTransition(PrtFun.IgnoreFun, Elevator_StoppingTimer, true);
                Elevator_DoorOpenedOkToClose.transitions.Add(Events_1178678920.eCloseDoor, transition_10);
                PrtTransition transition_11 = new PrtTransition(AnonFun19, Elevator_DoorClosing, false);
                Elevator_DoorOpenedOkToClose.transitions.Add(Events_1178678920.eTimerFired, transition_11);
                PrtTransition transition_12 = new PrtTransition(AnonFun19, Elevator_DoorClosing, false);
                Elevator_DoorOpenedOkToClose.transitions.Add(Events_1178678920.eStopTimerReturned, transition_12);
                Elevator_DoorOpened.deferredSet.Add(Events_1178678920.eCloseDoor);
                PrtTransition transition_13 = new PrtTransition(PrtFun.IgnoreFun, Elevator_StoppingTimer, true);
                Elevator_DoorOpened.transitions.Add(Events_1178678920.eOpenDoor, transition_13);
                PrtTransition transition_14 = new PrtTransition(AnonFun22, Elevator_DoorOpened, false);
                Elevator_DoorOpened.transitions.Add(Events_1178678920.eStopTimerReturned, transition_14);
                PrtTransition transition_15 = new PrtTransition(AnonFun23, Elevator_DoorOpenedOkToClose, false);
                Elevator_DoorOpened.transitions.Add(Events_1178678920.eTimerFired, transition_15);
                Elevator_DoorOpening.dos.Add(Events_1178678920.eOpenDoor, ignore);
                Elevator_DoorOpening.deferredSet.Add(Events_1178678920.eCloseDoor);
                PrtTransition transition_16 = new PrtTransition(AnonFun26, Elevator_DoorOpened, false);
                Elevator_DoorOpening.transitions.Add(Events_1178678920.eDoorOpened, transition_16);
                Elevator_DoorClosed.dos.Add(Events_1178678920.eCloseDoor, ignore);
                PrtTransition transition_17 = new PrtTransition(AnonFun29, Elevator_DoorOpening, false);
                Elevator_DoorClosed.transitions.Add(Events_1178678920.eOpenDoor, transition_17);
                PrtTransition transition_18 = new PrtTransition(AnonFun32, Elevator_DoorClosed, false);
                Elevator_Init.transitions.Add(Events_1178678920.eUnit, transition_18);
            }
        }

        public class Timer : PrtImplMachine
        {
            public override PrtState StartState
            {
                get
                {
                    return Timer__Init;
                }
            }

            public PrtValue ElevatorV
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
                return new Timer();
            }

            public override int NextInstanceNumber(StateImpl app)
            {
                return app.NextMachineInstanceNumber(this.GetType());
            }

            public override string Name
            {
                get
                {
                    return "Timer";
                }
            }

            public Timer(): base ()
            {
            }

            public Timer(StateImpl app, int maxB, bool assume): base (app, maxB, assume)
            {
                (fields).Add(PrtValue.PrtMkDefaultValue(Types_1178678920.typeMachine));
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
                    Timer parent = (Timer)(_parent);
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

                    public PrtValue _payload_22
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
                    Timer parent = (Timer)(_parent);
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

                    (parent).PrtFunContNondet(this, (currFun).locals, 4);
                    return;
                    AnonFun0_4:
                        ;
                    if (!((PrtBoolValue)(new PrtBoolValue(((parent).continuation).ReturnAndResetNondet()))).bl)
                        goto AnonFun0_if_0_else;
                    (((PrtMachineValue)((parent).ElevatorV)).mach).PrtEnqueueEvent((PrtEventValue)(Events_1178678920.eOperationFailure), Events_1178678920.@null, parent, (PrtMachineValue)((parent).ElevatorV));
                    (parent).PrtFunContSend(this, (currFun).locals, 1);
                    return;
                    AnonFun0_1:
                        ;
                    (((PrtMachineValue)((parent).ElevatorV)).mach).PrtEnqueueEvent((PrtEventValue)(Events_1178678920.eTimerFired), Events_1178678920.@null, parent, (PrtMachineValue)((parent).ElevatorV));
                    (parent).PrtFunContSend(this, (currFun).locals, 2);
                    return;
                    AnonFun0_2:
                        ;
                    goto AnonFun0_if_0_end;
                    AnonFun0_if_0_else:
                        ;
                    (((PrtMachineValue)((parent).ElevatorV)).mach).PrtEnqueueEvent((PrtEventValue)(Events_1178678920.eOperationSuccess), Events_1178678920.@null, parent, (PrtMachineValue)((parent).ElevatorV));
                    (parent).PrtFunContSend(this, (currFun).locals, 3);
                    return;
                    AnonFun0_3:
                        ;
                    AnonFun0_if_0_end:
                        ;
                    if (!!(Events_1178678920.eUnit).Equals(Events_1178678920.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Tst\\\\RegressionTests\\\\Integration\\\\Correct\\\\Elevator\\\\Elevator.p (268, 13): Raised event must be non-null");
                    Console.Write("<RaiseLog> Machine Timer-{0} raised Event {1}\n", (parent).instanceNumber, (((PrtEventValue)(Events_1178678920.eUnit)).evt).name);
                    (parent).currentTrigger = Events_1178678920.eUnit;
                    (parent).currentPayload = Events_1178678920.@null;
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
                    Timer parent = (Timer)(_parent);
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

                    public PrtValue _payload_21
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
                    Timer parent = (Timer)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun2_1;
                    }

                    (((PrtMachineValue)((parent).ElevatorV)).mach).PrtEnqueueEvent((PrtEventValue)(Events_1178678920.eTimerFired), Events_1178678920.@null, parent, (PrtMachineValue)((parent).ElevatorV));
                    (parent).PrtFunContSend(this, (currFun).locals, 1);
                    return;
                    AnonFun2_1:
                        ;
                    if (!!(Events_1178678920.eUnit).Equals(Events_1178678920.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Tst\\\\RegressionTests\\\\Integration\\\\Correct\\\\Elevator\\\\Elevator.p (254, 41): Raised event must be non-null");
                    Console.Write("<RaiseLog> Machine Timer-{0} raised Event {1}\n", (parent).instanceNumber, (((PrtEventValue)(Events_1178678920.eUnit)).evt).name);
                    (parent).currentTrigger = Events_1178678920.eUnit;
                    (parent).currentPayload = Events_1178678920.@null;
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
                    Timer parent = (Timer)(_parent);
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

                    public PrtValue _payload_20
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
                    Timer parent = (Timer)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun4_1;
                    }

                    (parent).PrtFunContNondet(this, (currFun).locals, 1);
                    return;
                    AnonFun4_1:
                        ;
                    if (!((PrtBoolValue)(new PrtBoolValue(((parent).continuation).ReturnAndResetNondet()))).bl)
                        goto AnonFun4_if_0_else;
                    if (!!(Events_1178678920.eUnit).Equals(Events_1178678920.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Tst\\\\RegressionTests\\\\Integration\\\\Correct\\\\Elevator\\\\Elevator.p (245, 23): Raised event must be non-null");
                    Console.Write("<RaiseLog> Machine Timer-{0} raised Event {1}\n", (parent).instanceNumber, (((PrtEventValue)(Events_1178678920.eUnit)).evt).name);
                    (parent).currentTrigger = Events_1178678920.eUnit;
                    (parent).currentPayload = Events_1178678920.@null;
                    (parent).PrtFunContRaise();
                    return;
                    goto AnonFun4_if_0_end;
                    AnonFun4_if_0_else:
                        ;
                    AnonFun4_if_0_end:
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
                    Timer parent = (Timer)(_parent);
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

                    public PrtValue _payload_19
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
                    Timer parent = (Timer)(_parent);
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
                    Timer parent = (Timer)(_parent);
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
                    Timer parent = (Timer)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    (parent).ElevatorV = ((currFun).locals[0]).Clone();
                    if (!!(Events_1178678920.eUnit).Equals(Events_1178678920.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Tst\\\\RegressionTests\\\\Integration\\\\Correct\\\\Elevator\\\\Elevator.p (232, 50): Raised event must be non-null");
                    Console.Write("<RaiseLog> Machine Timer-{0} raised Event {1}\n", (parent).instanceNumber, (((PrtEventValue)(Events_1178678920.eUnit)).evt).name);
                    (parent).currentTrigger = Events_1178678920.eUnit;
                    (parent).currentPayload = Events_1178678920.@null;
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
                    Timer parent = (Timer)(_parent);
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
                    Timer parent = (Timer)(_parent);
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
                    Timer parent = (Timer)(_parent);
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
                    Timer parent = (Timer)(_parent);
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
                    Timer parent = (Timer)(_parent);
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
                    Timer parent = (Timer)(_parent);
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
                    Timer parent = (Timer)(_parent);
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
                    Timer parent = (Timer)(_parent);
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
                    Timer parent = (Timer)(_parent);
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
                    Timer parent = (Timer)(_parent);
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
                    Timer parent = (Timer)(_parent);
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
                    Timer parent = (Timer)(_parent);
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
            public class Timer_ConsiderStopping_Class : PrtState
            {
                public Timer_ConsiderStopping_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Timer_ConsiderStopping_Class Timer_ConsiderStopping;
            public class Timer_SendTimerFired_Class : PrtState
            {
                public Timer_SendTimerFired_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Timer_SendTimerFired_Class Timer_SendTimerFired;
            public class Timer_TimerStarted_Class : PrtState
            {
                public Timer_TimerStarted_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Timer_TimerStarted_Class Timer_TimerStarted;
            public class Timer_Init_Class : PrtState
            {
                public Timer_Init_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Timer_Init_Class Timer_Init;
            public class Timer__Init_Class : PrtState
            {
                public Timer__Init_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Timer__Init_Class Timer__Init;
            static Timer()
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
                AnonFun13 = new AnonFun13_Class();
                AnonFun14 = new AnonFun14_Class();
                AnonFun15 = new AnonFun15_Class();
                AnonFun16 = new AnonFun16_Class();
                AnonFun17 = new AnonFun17_Class();
                AnonFun18 = new AnonFun18_Class();
                AnonFun19 = new AnonFun19_Class();
                AnonFun20 = new AnonFun20_Class();
                Timer_ConsiderStopping = new Timer_ConsiderStopping_Class("Timer_ConsiderStopping", AnonFun0, AnonFun1, false, StateTemperature.Warm);
                Timer_SendTimerFired = new Timer_SendTimerFired_Class("Timer_SendTimerFired", AnonFun2, AnonFun3, false, StateTemperature.Warm);
                Timer_TimerStarted = new Timer_TimerStarted_Class("Timer_TimerStarted", AnonFun4, AnonFun5, false, StateTemperature.Warm);
                Timer_Init = new Timer_Init_Class("Timer_Init", AnonFun6, AnonFun7, false, StateTemperature.Warm);
                Timer__Init = new Timer__Init_Class("Timer__Init", AnonFun8, AnonFun9, false, StateTemperature.Warm);
                Timer_ConsiderStopping.deferredSet.Add(Events_1178678920.eStartDoorCloseTimer);
                PrtTransition transition_1 = new PrtTransition(AnonFun10, Timer_Init, false);
                Timer_ConsiderStopping.transitions.Add(Events_1178678920.eUnit, transition_1);
                Timer_SendTimerFired.deferredSet.Add(Events_1178678920.eStartDoorCloseTimer);
                PrtTransition transition_2 = new PrtTransition(AnonFun11, Timer_Init, false);
                Timer_SendTimerFired.transitions.Add(Events_1178678920.eUnit, transition_2);
                Timer_TimerStarted.deferredSet.Add(Events_1178678920.eStartDoorCloseTimer);
                PrtTransition transition_3 = new PrtTransition(AnonFun12, Timer_ConsiderStopping, false);
                Timer_TimerStarted.transitions.Add(Events_1178678920.eStopDoorCloseTimer, transition_3);
                PrtTransition transition_4 = new PrtTransition(AnonFun13, Timer_SendTimerFired, false);
                Timer_TimerStarted.transitions.Add(Events_1178678920.eUnit, transition_4);
                Timer_Init.dos.Add(Events_1178678920.eStopDoorCloseTimer, ignore);
                PrtTransition transition_5 = new PrtTransition(AnonFun14, Timer_TimerStarted, false);
                Timer_Init.transitions.Add(Events_1178678920.eStartDoorCloseTimer, transition_5);
                PrtTransition transition_6 = new PrtTransition(AnonFun15, Timer_Init, false);
                Timer__Init.transitions.Add(Events_1178678920.eUnit, transition_6);
            }
        }

        public class Door : PrtImplMachine
        {
            public override PrtState StartState
            {
                get
                {
                    return Door__Init;
                }
            }

            public PrtValue ElevatorV
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
                return new Door();
            }

            public override int NextInstanceNumber(StateImpl app)
            {
                return app.NextMachineInstanceNumber(this.GetType());
            }

            public override string Name
            {
                get
                {
                    return "Door";
                }
            }

            public Door(): base ()
            {
            }

            public Door(StateImpl app, int maxB, bool assume): base (app, maxB, assume)
            {
                (fields).Add(PrtValue.PrtMkDefaultValue(Types_1178678920.typeMachine));
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
                    Door parent = (Door)(_parent);
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

                    public PrtValue _payload_18
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
                    Door parent = (Door)(_parent);
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
                    Door parent = (Door)(_parent);
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

                    public PrtValue _payload_17
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
                    Door parent = (Door)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun2_1;
                    }

                    (((PrtMachineValue)((parent).ElevatorV)).mach).PrtEnqueueEvent((PrtEventValue)(Events_1178678920.eDoorStopped), Events_1178678920.@null, parent, (PrtMachineValue)((parent).ElevatorV));
                    (parent).PrtFunContSend(this, (currFun).locals, 1);
                    return;
                    AnonFun2_1:
                        ;
                    if (!!(Events_1178678920.eUnit).Equals(Events_1178678920.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Tst\\\\RegressionTests\\\\Integration\\\\Correct\\\\Elevator\\\\Elevator.p (213, 42): Raised event must be non-null");
                    Console.Write("<RaiseLog> Machine Door-{0} raised Event {1}\n", (parent).instanceNumber, (((PrtEventValue)(Events_1178678920.eUnit)).evt).name);
                    (parent).currentTrigger = Events_1178678920.eUnit;
                    (parent).currentPayload = Events_1178678920.@null;
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
                    Door parent = (Door)(_parent);
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
                    Door parent = (Door)(_parent);
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
                    Door parent = (Door)(_parent);
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
                    Door parent = (Door)(_parent);
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
                    Door parent = (Door)(_parent);
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
                    Door parent = (Door)(_parent);
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
                    Door parent = (Door)(_parent);
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
                    Door parent = (Door)(_parent);
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
                    Door parent = (Door)(_parent);
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
                    Door parent = (Door)(_parent);
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

                    public PrtValue _payload_16
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
                    Door parent = (Door)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun13_1;
                    }

                    (((PrtMachineValue)((parent).ElevatorV)).mach).PrtEnqueueEvent((PrtEventValue)(Events_1178678920.eDoorClosed), Events_1178678920.@null, parent, (PrtMachineValue)((parent).ElevatorV));
                    (parent).PrtFunContSend(this, (currFun).locals, 1);
                    return;
                    AnonFun13_1:
                        ;
                    if (!!(Events_1178678920.eUnit).Equals(Events_1178678920.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Tst\\\\RegressionTests\\\\Integration\\\\Correct\\\\Elevator\\\\Elevator.p (205, 42): Raised event must be non-null");
                    Console.Write("<RaiseLog> Machine Door-{0} raised Event {1}\n", (parent).instanceNumber, (((PrtEventValue)(Events_1178678920.eUnit)).evt).name);
                    (parent).currentTrigger = Events_1178678920.eUnit;
                    (parent).currentPayload = Events_1178678920.@null;
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
                    Door parent = (Door)(_parent);
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
                    Door parent = (Door)(_parent);
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

                    public PrtValue _payload_15
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
                    Door parent = (Door)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun16_1;
                    }

                    (((PrtMachineValue)((parent).ElevatorV)).mach).PrtEnqueueEvent((PrtEventValue)(Events_1178678920.eObjectDetected), Events_1178678920.@null, parent, (PrtMachineValue)((parent).ElevatorV));
                    (parent).PrtFunContSend(this, (currFun).locals, 1);
                    return;
                    AnonFun16_1:
                        ;
                    if (!!(Events_1178678920.eUnit).Equals(Events_1178678920.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Tst\\\\RegressionTests\\\\Integration\\\\Correct\\\\Elevator\\\\Elevator.p (197, 13): Raised event must be non-null");
                    Console.Write("<RaiseLog> Machine Door-{0} raised Event {1}\n", (parent).instanceNumber, (((PrtEventValue)(Events_1178678920.eUnit)).evt).name);
                    (parent).currentTrigger = Events_1178678920.eUnit;
                    (parent).currentPayload = Events_1178678920.@null;
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
                    Door parent = (Door)(_parent);
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
                    Door parent = (Door)(_parent);
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

                    public PrtValue _payload_14
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
                    Door parent = (Door)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun19_1;
                        case 2:
                            goto AnonFun19_2;
                    }

                    (parent).PrtFunContNondet(this, (currFun).locals, 2);
                    return;
                    AnonFun19_2:
                        ;
                    if (!((PrtBoolValue)(new PrtBoolValue(((parent).continuation).ReturnAndResetNondet()))).bl)
                        goto AnonFun19_if_1_else;
                    if (!!(Events_1178678920.eUnit).Equals(Events_1178678920.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Tst\\\\RegressionTests\\\\Integration\\\\Correct\\\\Elevator\\\\Elevator.p (183, 17): Raised event must be non-null");
                    Console.Write("<RaiseLog> Machine Door-{0} raised Event {1}\n", (parent).instanceNumber, (((PrtEventValue)(Events_1178678920.eUnit)).evt).name);
                    (parent).currentTrigger = Events_1178678920.eUnit;
                    (parent).currentPayload = Events_1178678920.@null;
                    (parent).PrtFunContRaise();
                    return;
                    goto AnonFun19_if_1_end;
                    AnonFun19_if_1_else:
                        ;
                    (parent).PrtFunContNondet(this, (currFun).locals, 1);
                    return;
                    AnonFun19_1:
                        ;
                    if (!((PrtBoolValue)(new PrtBoolValue(((parent).continuation).ReturnAndResetNondet()))).bl)
                        goto AnonFun19_if_0_else;
                    if (!!(Events_1178678920.eObjectEncountered).Equals(Events_1178678920.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Tst\\\\RegressionTests\\\\Integration\\\\Correct\\\\Elevator\\\\Elevator.p (185, 17): Raised event must be non-null");
                    Console.Write("<RaiseLog> Machine Door-{0} raised Event {1}\n", (parent).instanceNumber, (((PrtEventValue)(Events_1178678920.eObjectEncountered)).evt).name);
                    (parent).currentTrigger = Events_1178678920.eObjectEncountered;
                    (parent).currentPayload = Events_1178678920.@null;
                    (parent).PrtFunContRaise();
                    return;
                    goto AnonFun19_if_0_end;
                    AnonFun19_if_0_else:
                        ;
                    AnonFun19_if_0_end:
                        ;
                    AnonFun19_if_1_end:
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
                    Door parent = (Door)(_parent);
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
                    Door parent = (Door)(_parent);
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
                    Door parent = (Door)(_parent);
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
                    Door parent = (Door)(_parent);
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
                    Door parent = (Door)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun24_1;
                    }

                    (((PrtMachineValue)((parent).ElevatorV)).mach).PrtEnqueueEvent((PrtEventValue)(Events_1178678920.eDoorOpened), Events_1178678920.@null, parent, (PrtMachineValue)((parent).ElevatorV));
                    (parent).PrtFunContSend(this, (currFun).locals, 1);
                    return;
                    AnonFun24_1:
                        ;
                    if (!!(Events_1178678920.eUnit).Equals(Events_1178678920.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Tst\\\\RegressionTests\\\\Integration\\\\Correct\\\\Elevator\\\\Elevator.p (174, 13): Raised event must be non-null");
                    Console.Write("<RaiseLog> Machine Door-{0} raised Event {1}\n", (parent).instanceNumber, (((PrtEventValue)(Events_1178678920.eUnit)).evt).name);
                    (parent).currentTrigger = Events_1178678920.eUnit;
                    (parent).currentPayload = Events_1178678920.@null;
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
                    Door parent = (Door)(_parent);
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
                    Door parent = (Door)(_parent);
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
                    Door parent = (Door)(_parent);
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
                    Door parent = (Door)(_parent);
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
                    Door parent = (Door)(_parent);
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
                    Door parent = (Door)(_parent);
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
                    Door parent = (Door)(_parent);
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
                    Door parent = (Door)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    (parent).ElevatorV = ((currFun).locals[0]).Clone();
                    if (!!(Events_1178678920.eUnit).Equals(Events_1178678920.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Tst\\\\RegressionTests\\\\Integration\\\\Correct\\\\Elevator\\\\Elevator.p (159, 50): Raised event must be non-null");
                    Console.Write("<RaiseLog> Machine Door-{0} raised Event {1}\n", (parent).instanceNumber, (((PrtEventValue)(Events_1178678920.eUnit)).evt).name);
                    (parent).currentTrigger = Events_1178678920.eUnit;
                    (parent).currentPayload = Events_1178678920.@null;
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
                    Door parent = (Door)(_parent);
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
                    Door parent = (Door)(_parent);
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
            public class Door_ResetDoor_Class : PrtState
            {
                public Door_ResetDoor_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Door_ResetDoor_Class Door_ResetDoor;
            public class Door_StopDoor_Class : PrtState
            {
                public Door_StopDoor_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Door_StopDoor_Class Door_StopDoor;
            public class Door_CloseDoor_Class : PrtState
            {
                public Door_CloseDoor_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Door_CloseDoor_Class Door_CloseDoor;
            public class Door_ObjectEncountered_Class : PrtState
            {
                public Door_ObjectEncountered_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Door_ObjectEncountered_Class Door_ObjectEncountered;
            public class Door_ConsiderClosingDoor_Class : PrtState
            {
                public Door_ConsiderClosingDoor_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Door_ConsiderClosingDoor_Class Door_ConsiderClosingDoor;
            public class Door_OpenDoor_Class : PrtState
            {
                public Door_OpenDoor_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Door_OpenDoor_Class Door_OpenDoor;
            public class Door_Init_Class : PrtState
            {
                public Door_Init_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Door_Init_Class Door_Init;
            public class Door__Init_Class : PrtState
            {
                public Door__Init_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Door__Init_Class Door__Init;
            static Door()
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
                Door_ResetDoor = new Door_ResetDoor_Class("Door_ResetDoor", AnonFun0, AnonFun1, false, StateTemperature.Warm);
                Door_StopDoor = new Door_StopDoor_Class("Door_StopDoor", AnonFun2, AnonFun3, false, StateTemperature.Warm);
                Door_CloseDoor = new Door_CloseDoor_Class("Door_CloseDoor", AnonFun13, AnonFun14, false, StateTemperature.Warm);
                Door_ObjectEncountered = new Door_ObjectEncountered_Class("Door_ObjectEncountered", AnonFun16, AnonFun17, false, StateTemperature.Warm);
                Door_ConsiderClosingDoor = new Door_ConsiderClosingDoor_Class("Door_ConsiderClosingDoor", AnonFun19, AnonFun20, false, StateTemperature.Warm);
                Door_OpenDoor = new Door_OpenDoor_Class("Door_OpenDoor", AnonFun24, AnonFun25, false, StateTemperature.Warm);
                Door_Init = new Door_Init_Class("Door_Init", AnonFun27, AnonFun28, false, StateTemperature.Warm);
                Door__Init = new Door__Init_Class("Door__Init", AnonFun32, AnonFun33, false, StateTemperature.Warm);
                Door_ResetDoor.dos.Add(Events_1178678920.eSendCommandToStopDoor, ignore);
                Door_ResetDoor.dos.Add(Events_1178678920.eSendCommandToCloseDoor, ignore);
                Door_ResetDoor.dos.Add(Events_1178678920.eSendCommandToOpenDoor, ignore);
                PrtTransition transition_1 = new PrtTransition(AnonFun11, Door_Init, false);
                Door_ResetDoor.transitions.Add(Events_1178678920.eSendCommandToResetDoor, transition_1);
                PrtTransition transition_2 = new PrtTransition(AnonFun12, Door_OpenDoor, false);
                Door_StopDoor.transitions.Add(Events_1178678920.eUnit, transition_2);
                PrtTransition transition_3 = new PrtTransition(AnonFun15, Door_ResetDoor, false);
                Door_CloseDoor.transitions.Add(Events_1178678920.eUnit, transition_3);
                PrtTransition transition_4 = new PrtTransition(AnonFun18, Door_Init, false);
                Door_ObjectEncountered.transitions.Add(Events_1178678920.eUnit, transition_4);
                PrtTransition transition_5 = new PrtTransition(AnonFun21, Door_StopDoor, false);
                Door_ConsiderClosingDoor.transitions.Add(Events_1178678920.eSendCommandToStopDoor, transition_5);
                PrtTransition transition_6 = new PrtTransition(AnonFun22, Door_ObjectEncountered, false);
                Door_ConsiderClosingDoor.transitions.Add(Events_1178678920.eObjectEncountered, transition_6);
                PrtTransition transition_7 = new PrtTransition(AnonFun23, Door_CloseDoor, false);
                Door_ConsiderClosingDoor.transitions.Add(Events_1178678920.eUnit, transition_7);
                PrtTransition transition_8 = new PrtTransition(AnonFun26, Door_ResetDoor, false);
                Door_OpenDoor.transitions.Add(Events_1178678920.eUnit, transition_8);
                Door_Init.dos.Add(Events_1178678920.eResetDoor, ignore);
                Door_Init.dos.Add(Events_1178678920.eSendCommandToResetDoor, ignore);
                Door_Init.dos.Add(Events_1178678920.eSendCommandToStopDoor, ignore);
                PrtTransition transition_9 = new PrtTransition(AnonFun30, Door_ConsiderClosingDoor, false);
                Door_Init.transitions.Add(Events_1178678920.eSendCommandToCloseDoor, transition_9);
                PrtTransition transition_10 = new PrtTransition(AnonFun31, Door_OpenDoor, false);
                Door_Init.transitions.Add(Events_1178678920.eSendCommandToOpenDoor, transition_10);
                PrtTransition transition_11 = new PrtTransition(AnonFun34, Door_Init, false);
                Door__Init.transitions.Add(Events_1178678920.eUnit, transition_11);
            }
        }
    }
}
