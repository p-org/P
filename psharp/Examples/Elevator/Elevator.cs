//-----------------------------------------------------------------------
// <copyright file="Elevator.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
//      EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
//      OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
// ----------------------------------------------------------------------------------
//      The example companies, organizations, products, domain names,
//      e-mail addresses, logos, people, places, and events depicted
//      herein are fictitious.  No association with any real company,
//      organization, product, domain name, email address, logo, person,
//      places, or events is intended or should be inferred.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace Elevator
{
    #region Events

    internal class eOpenDoor : Event { }
    internal class eCloseDoor : Event { }
    internal class eResetDoor : Event { }
    internal class eDoorOpened : Event { }
    internal class eDoorClosed : Event { }
    internal class eDoorStopped : Event { }
    internal class eObjectDetected : Event { }
    internal class eTimerFired : Event { }
    internal class eOperationSuccess : Event { }
    internal class eOperationFailure : Event { }
    internal class eSendCommandToOpenDoor : Event { }
    internal class eSendCommandToCloseDoor : Event { }
    internal class eSendCommandToStopDoor : Event { }
    internal class eSendCommandToResetDoor : Event { }
    internal class eStartDoorCloseTimer : Event { }
    internal class eStopDoorCloseTimer : Event { }
    internal class eUnit : Event { }
    internal class eStopTimerReturned : Event { }
    internal class eObjectEncountered : Event { }

    #endregion

    #region Machines

    internal class Elevator : Machine
    {
        private Machine Timer;
        private Machine Door;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                Console.WriteLine("Initializing Elevator");
                (this.Machine as Elevator).Timer =
                    Machine.Factory.CreateMachine<Timer>(this.Machine);

                (this.Machine as Elevator).Door =
                    Machine.Factory.CreateMachine<Door>(this.Machine);

                Console.WriteLine("{0} raising event {1} from state {2}\n", this.Machine,
                    typeof(eSendCommandToResetDoor), this);
                this.Raise(new eUnit());
            }
        }

        private class DoorClosed : State
        {
            protected override void OnEntry()
            {
                Console.WriteLine("{0} sending event {1} to {2}\n", this.Machine,
                    typeof(eSendCommandToResetDoor), (this.Machine as Elevator).Door);
                this.Send((this.Machine as Elevator).Door, new eSendCommandToResetDoor());
            }

            protected override HashSet<Type> DefineIgnoredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eCloseDoor)
                };
            }
        }

        private class DoorOpening : State
        {
            protected override void OnEntry()
            {
                Console.WriteLine("{0} sending event {1} to {2}\n", this.Machine,
                    typeof(eSendCommandToOpenDoor), (this.Machine as Elevator).Door);
                this.Send((this.Machine as Elevator).Door, new eSendCommandToOpenDoor());
            }

            protected override HashSet<Type> DefineIgnoredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eOpenDoor)
                };
            }

            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eCloseDoor)
                };
            }
        }

        private class DoorOpened : State
        {
            protected override void OnEntry()
            {
                Console.WriteLine("{0} sending event {1} to {2}\n", this.Machine,
                    typeof(eSendCommandToResetDoor), (this.Machine as Elevator).Door);
                this.Send((this.Machine as Elevator).Door, new eSendCommandToResetDoor());

                Console.WriteLine("{0} sending event {1} to {2}\n", this.Machine,
                    typeof(eStartDoorCloseTimer), (this.Machine as Elevator).Timer);
                this.Send((this.Machine as Elevator).Timer, new eStartDoorCloseTimer());
            }

            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eCloseDoor)
                };
            }
        }

        private class DoorOpenedOkToClose : State
        {
            protected override void OnEntry()
            {
                Console.WriteLine("{0} sending event {1} to {2}\n", this.Machine,
                    typeof(eStartDoorCloseTimer), (this.Machine as Elevator).Timer);
                this.Send((this.Machine as Elevator).Timer, new eStartDoorCloseTimer());
            }

            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eOpenDoor)
                };
            }
        }

        private class DoorClosing : State
        {
            protected override void OnEntry()
            {
                Console.WriteLine("{0} sending event {1} to {2}\n", this.Machine,
                    typeof(eSendCommandToCloseDoor), (this.Machine as Elevator).Door);
                this.Send((this.Machine as Elevator).Door, new eSendCommandToCloseDoor());
            }

            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eCloseDoor)
                };
            }
        }

        private class StoppingDoor : State
        {
            protected override void OnEntry()
            {
                Console.WriteLine("{0} sending event {1} to {2}\n", this.Machine,
                    typeof(eSendCommandToStopDoor), (this.Machine as Elevator).Door);
                this.Send((this.Machine as Elevator).Door, new eSendCommandToStopDoor());
            }

            protected override HashSet<Type> DefineIgnoredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eOpenDoor),
                    typeof(eObjectDetected)
                };
            }

            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eCloseDoor)
                };
            }
        }

        private class StoppingTimer : State
        {
            protected override void OnEntry()
            {
                Console.WriteLine("{0} sending event {1} to {2}\n", this.Machine,
                    typeof(eStopDoorCloseTimer), (this.Machine as Elevator).Timer);
                this.Send((this.Machine as Elevator).Timer, new eStopDoorCloseTimer());
            }

            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eOpenDoor),
                    typeof(eCloseDoor),
                    typeof(eObjectDetected)
                };
            }
        }

        private class WaitingForTimer : State
        {
            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eOpenDoor),
                    typeof(eCloseDoor),
                    typeof(eObjectDetected)
                };
            }
        }

        private class ReturnState : State
        {
            protected override void OnEntry()
            {
                Console.WriteLine("{0} raising event {1} from state {2}\n", this.Machine,
                    typeof(eStopTimerReturned), this);
                this.Raise(new eStopTimerReturned());
            }
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eUnit), typeof(DoorClosed));

            StepStateTransitions doorClosedDict = new StepStateTransitions();
            doorClosedDict.Add(typeof(eOpenDoor), typeof(DoorOpening));

            StepStateTransitions doorOpeningDict = new StepStateTransitions();
            doorOpeningDict.Add(typeof(eDoorOpened), typeof(DoorOpened));

            StepStateTransitions doorOpenedDict = new StepStateTransitions();
            doorOpenedDict.Add(typeof(eTimerFired), typeof(DoorOpenedOkToClose));
            doorOpenedDict.Add(typeof(eStopTimerReturned), typeof(DoorOpened));

            StepStateTransitions doorOpenedOkToCloseDict = new StepStateTransitions();
            doorOpenedOkToCloseDict.Add(typeof(eStopTimerReturned), typeof(DoorClosing));
            doorOpenedOkToCloseDict.Add(typeof(eTimerFired), typeof(DoorClosing));

            StepStateTransitions doorClosingDict = new StepStateTransitions();
            doorClosingDict.Add(typeof(eOpenDoor), typeof(StoppingDoor));
            doorClosingDict.Add(typeof(eDoorClosed), typeof(DoorClosed));
            doorClosingDict.Add(typeof(eObjectDetected), typeof(DoorOpening));

            StepStateTransitions stoppingDoorDict = new StepStateTransitions();
            stoppingDoorDict.Add(typeof(eDoorOpened), typeof(DoorOpened));
            stoppingDoorDict.Add(typeof(eDoorClosed), typeof(DoorClosed));
            stoppingDoorDict.Add(typeof(eDoorStopped), typeof(DoorOpening));

            StepStateTransitions stoppingTimerDict = new StepStateTransitions();
            stoppingTimerDict.Add(typeof(eOperationSuccess), typeof(ReturnState));
            stoppingTimerDict.Add(typeof(eOperationFailure), typeof(WaitingForTimer));

            StepStateTransitions waitingForTimerDict = new StepStateTransitions();
            waitingForTimerDict.Add(typeof(eTimerFired), typeof(ReturnState));

            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(DoorClosed), doorClosedDict);
            dict.Add(typeof(DoorOpening), doorOpeningDict);
            dict.Add(typeof(DoorOpened), doorOpenedDict);
            dict.Add(typeof(DoorOpenedOkToClose), doorOpenedOkToCloseDict);
            dict.Add(typeof(DoorClosing), doorClosingDict);
            dict.Add(typeof(StoppingDoor), stoppingDoorDict);
            dict.Add(typeof(StoppingTimer), stoppingTimerDict);
            dict.Add(typeof(WaitingForTimer), waitingForTimerDict);

            return dict;
        }

        protected override Dictionary<Type, CallStateTransitions> DefineCallStateTransitions()
        {
            Dictionary<Type, CallStateTransitions> dict = new Dictionary<Type, CallStateTransitions>();

            CallStateTransitions doorOpenedDict = new CallStateTransitions();
            doorOpenedDict.Add(typeof(eOpenDoor), typeof(StoppingTimer));

            CallStateTransitions doorOpenedOkToCloseDict = new CallStateTransitions();
            doorOpenedOkToCloseDict.Add(typeof(eCloseDoor), typeof(StoppingTimer));

            dict.Add(typeof(DoorOpened), doorOpenedDict);
            dict.Add(typeof(DoorOpenedOkToClose), doorOpenedOkToCloseDict);

            return dict;
        }
    }

    [Main]
    [Ghost]
    internal class User : Machine
    {
        private Machine Elevator;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                Console.WriteLine("Initializing User");
                (this.Machine as User).Elevator =
                    Machine.Factory.CreateMachine<Elevator>(this.Machine);

                Console.WriteLine("{0} raising event {1} from state {2}\n", this.Machine,
                    typeof(eUnit), this);
                this.Raise(new eUnit());
            }
        }

        private class Loop : State
        {
            protected override void OnEntry()
            {
                // We don't want the user to make an action too often ...
                Model.Sleep(5000);

                if (Model.Havoc.Boolean)
                {
                    Console.WriteLine("{0} sending event {1} to {2}\n", this.Machine,
                        typeof(eOpenDoor), (this.Machine as User).Elevator);
                    this.Send((this.Machine as User).Elevator, new eOpenDoor());
                }
                else if (Model.Havoc.Boolean)
                {
                    Console.WriteLine("{0} sending event {1} to {2}\n", this.Machine,
                        typeof(eCloseDoor), (this.Machine as User).Elevator);
                    this.Send((this.Machine as User).Elevator, new eCloseDoor());
                }

                Console.WriteLine("{0} raising event {1} from state {2}\n", this.Machine,
                    typeof(eUnit), this);
                this.Raise(new eUnit());
            }
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eUnit), typeof(Loop));

            StepStateTransitions loopDict = new StepStateTransitions();
            loopDict.Add(typeof(eUnit), typeof(Loop));

            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(Loop), loopDict);

            return dict;
        }
    }

    [Ghost]
    internal class Door : Machine
    {
        private Machine Elevator;

        [Initial]
        private class _Init : State
        {
            protected override void OnEntry()
            {
                Console.WriteLine("Initializing Door");
                (this.Machine as Door).Elevator = (Machine) this.Payload;

                Console.WriteLine("{0} raising event {1} from state {2}\n", this.Machine,
                    typeof(eUnit), this);
                this.Raise(new eUnit());
            }

            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eSendCommandToResetDoor)
                };
            }
        }

        private class Init : State
        {
            protected override HashSet<Type> DefineIgnoredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eSendCommandToStopDoor),
                    typeof(eSendCommandToResetDoor),
                    typeof(eResetDoor)
                };
            }
        }

        private class OpenDoor : State
        {
            protected override void OnEntry()
            {
                Console.WriteLine("{0} sending event {1} to {2}\n", this.Machine,
                        typeof(eDoorOpened), (this.Machine as Door).Elevator);
                this.Send((this.Machine as Door).Elevator, new eDoorOpened());

                Console.WriteLine("{0} raising event {1} from state {2}\n", this.Machine,
                    typeof(eUnit), this);
                this.Raise(new eUnit());
            }
        }

        private class ConsiderClosingDoor : State
        {
            protected override void OnEntry()
            {
                if (Model.Havoc.Boolean)
                {
                    Console.WriteLine("{0} raising event {1} from state {2}\n", this.Machine,
                        typeof(eUnit), this);
                    this.Raise(new eUnit());
                }
                else if (Model.Havoc.Boolean)
                {
                    Console.WriteLine("{0} raising event {1} from state {2}\n", this.Machine,
                        typeof(eUnit), this);
                    this.Raise(new eObjectEncountered());
                }
            }
        }

        private class ObjectEncountered : State
        {
            protected override void OnEntry()
            {
                Console.WriteLine("{0} sending event {1} to {2}\n", this.Machine,
                        typeof(eObjectDetected), (this.Machine as Door).Elevator);
                this.Send((this.Machine as Door).Elevator, new eObjectDetected());

                Console.WriteLine("{0} raising event {1} from state {2}\n", this.Machine,
                    typeof(eUnit), this);
                this.Raise(new eUnit());
            }

            protected override HashSet<Type> DefineIgnoredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eSendCommandToStopDoor)
                };
            }
        }

        private class CloseDoor : State
        {
            protected override void OnEntry()
            {
                Console.WriteLine("{0} sending event {1} to {2}\n", this.Machine,
                        typeof(eDoorClosed), (this.Machine as Door).Elevator);
                this.Send((this.Machine as Door).Elevator, new eDoorClosed());

                Console.WriteLine("{0} raising event {1} from state {2}\n", this.Machine,
                    typeof(eUnit), this);
                this.Raise(new eUnit());
            }
        }

        private class StopDoor : State
        {
            protected override void OnEntry()
            {
                Console.WriteLine("{0} sending event {1} to {2}\n", this.Machine,
                        typeof(eDoorStopped), (this.Machine as Door).Elevator);
                this.Send((this.Machine as Door).Elevator, new eDoorStopped());

                Console.WriteLine("{0} raising event {1} from state {2}\n", this.Machine,
                    typeof(eUnit), this);
                this.Raise(new eUnit());
            }

            protected override HashSet<Type> DefineIgnoredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eSendCommandToStopDoor)
                };
            }
        }

        private class ResetDoor : State
        {
            protected override HashSet<Type> DefineIgnoredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eSendCommandToOpenDoor),
                    typeof(eSendCommandToCloseDoor),
                    typeof(eSendCommandToStopDoor)
                };
            }
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions _initDict = new StepStateTransitions();
            _initDict.Add(typeof(eUnit), typeof(Init));

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eSendCommandToOpenDoor), typeof(OpenDoor));
            initDict.Add(typeof(eSendCommandToCloseDoor), typeof(ConsiderClosingDoor));

            StepStateTransitions openDoorDict = new StepStateTransitions();
            openDoorDict.Add(typeof(eUnit), typeof(ResetDoor));

            StepStateTransitions considerClosingDoorDict = new StepStateTransitions();
            considerClosingDoorDict.Add(typeof(eUnit), typeof(CloseDoor));
            considerClosingDoorDict.Add(typeof(eObjectEncountered), typeof(ObjectEncountered));
            considerClosingDoorDict.Add(typeof(eSendCommandToStopDoor), typeof(StopDoor));

            StepStateTransitions objectEncounteredDict = new StepStateTransitions();
            objectEncounteredDict.Add(typeof(eUnit), typeof(Init));

            StepStateTransitions closeDoorDict = new StepStateTransitions();
            closeDoorDict.Add(typeof(eUnit), typeof(ResetDoor));

            StepStateTransitions stopDoorDict = new StepStateTransitions();
            stopDoorDict.Add(typeof(eUnit), typeof(OpenDoor));

            StepStateTransitions resetDoorDict = new StepStateTransitions();
            resetDoorDict.Add(typeof(eSendCommandToResetDoor), typeof(Init));

            dict.Add(typeof(_Init), _initDict);
            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(OpenDoor), openDoorDict);
            dict.Add(typeof(ConsiderClosingDoor), considerClosingDoorDict);
            dict.Add(typeof(ObjectEncountered), objectEncounteredDict);
            dict.Add(typeof(CloseDoor), closeDoorDict);
            dict.Add(typeof(StopDoor), stopDoorDict);
            dict.Add(typeof(ResetDoor), resetDoorDict);

            return dict;
        }
    }

    [Ghost]
    internal class Timer : Machine
    {
        private Machine Elevator;

        [Initial]
        private class _Init : State
        {
            protected override void OnEntry()
            {
                Console.WriteLine("Initializing Timer");
                (this.Machine as Timer).Elevator = (Machine) this.Payload;

                Console.WriteLine("{0} raising event {1} from state {2}\n", this.Machine,
                    typeof(eUnit), this);
                this.Raise(new eUnit());
            }
        }

        private class Init : State
        {
            protected override HashSet<Type> DefineIgnoredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eStopDoorCloseTimer)
                };
            }
        }

        private class TimerStarted : State
        {
            protected override void OnEntry()
            {
                if (Model.Havoc.Boolean)
                {
                    Console.WriteLine("{0} raising event {1} from state {2}\n", this.Machine,
                        typeof(eUnit), this);
                    this.Raise(new eUnit());
                }
            }

            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eStartDoorCloseTimer)
                };
            }
        }

        private class SendTimerFired : State
        {
            protected override void OnEntry()
            {
                Console.WriteLine("{0} sending event {1} to {2}\n", this.Machine,
                        typeof(eTimerFired), (this.Machine as Timer).Elevator);
                this.Send((this.Machine as Timer).Elevator, new eTimerFired());

                Console.WriteLine("{0} raising event {1} from state {2}\n", this.Machine,
                    typeof(eUnit), this);
                this.Raise(new eUnit());
            }

            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eStartDoorCloseTimer)
                };
            }
        }

        private class ConsiderStopping : State
        {
            protected override void OnEntry()
            {
                if (Model.Havoc.Boolean)
                {
                    Console.WriteLine("{0} sending event {1} to {2}\n", this.Machine,
                        typeof(eOperationFailure), (this.Machine as Timer).Elevator);
                    this.Send((this.Machine as Timer).Elevator, new eOperationFailure());

                    Console.WriteLine("{0} sending event {1} to {2}\n", this.Machine,
                        typeof(eTimerFired), (this.Machine as Timer).Elevator);
                    this.Send((this.Machine as Timer).Elevator, new eTimerFired());
                }
                else
                {
                    Console.WriteLine("{0} sending event {1} to {2}\n", this.Machine,
                        typeof(eOperationSuccess), (this.Machine as Timer).Elevator);
                    this.Send((this.Machine as Timer).Elevator, new eOperationSuccess());
                }


                Console.WriteLine("{0} raising event {1} from state {2}\n", this.Machine,
                    typeof(eUnit), this);
                this.Raise(new eUnit());
            }

            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eStartDoorCloseTimer)
                };
            }
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions _initDict = new StepStateTransitions();
            _initDict.Add(typeof(eUnit), typeof(Init));

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eStartDoorCloseTimer), typeof(TimerStarted));

            StepStateTransitions timerStartedDict = new StepStateTransitions();
            timerStartedDict.Add(typeof(eUnit), typeof(SendTimerFired));
            timerStartedDict.Add(typeof(eStopDoorCloseTimer), typeof(ConsiderStopping));

            StepStateTransitions sendTimerFiredDict = new StepStateTransitions();
            sendTimerFiredDict.Add(typeof(eUnit), typeof(Init));

            StepStateTransitions considerStoppingDict = new StepStateTransitions();
            considerStoppingDict.Add(typeof(eUnit), typeof(Init));

            dict.Add(typeof(_Init), _initDict);
            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(TimerStarted), timerStartedDict);
            dict.Add(typeof(SendTimerFired), sendTimerFiredDict);
            dict.Add(typeof(ConsiderStopping), considerStoppingDict);

            return dict;
        }
    }

    #endregion

    /// <summary>
    /// This is an example of usign P#.
    /// 
    /// This example implements an elevator and its environment.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Registering events to the runtime.\n");
            Runtime.RegisterNewEvent(typeof(eOpenDoor));
            Runtime.RegisterNewEvent(typeof(eCloseDoor));
            Runtime.RegisterNewEvent(typeof(eResetDoor));
            Runtime.RegisterNewEvent(typeof(eDoorOpened));
            Runtime.RegisterNewEvent(typeof(eDoorClosed));
            Runtime.RegisterNewEvent(typeof(eDoorStopped));
            Runtime.RegisterNewEvent(typeof(eObjectDetected));
            Runtime.RegisterNewEvent(typeof(eTimerFired));
            Runtime.RegisterNewEvent(typeof(eOperationSuccess));
            Runtime.RegisterNewEvent(typeof(eOperationFailure));
            Runtime.RegisterNewEvent(typeof(eSendCommandToOpenDoor));
            Runtime.RegisterNewEvent(typeof(eSendCommandToCloseDoor));
            Runtime.RegisterNewEvent(typeof(eSendCommandToStopDoor));
            Runtime.RegisterNewEvent(typeof(eSendCommandToResetDoor));
            Runtime.RegisterNewEvent(typeof(eStartDoorCloseTimer));
            Runtime.RegisterNewEvent(typeof(eStopDoorCloseTimer));
            Runtime.RegisterNewEvent(typeof(eUnit));
            Runtime.RegisterNewEvent(typeof(eStopTimerReturned));
            Runtime.RegisterNewEvent(typeof(eObjectEncountered));

            Console.WriteLine("Registering state machines to the runtime.\n");
            Runtime.RegisterNewMachine(typeof(Elevator));
            Runtime.RegisterNewMachine(typeof(User));
            Runtime.RegisterNewMachine(typeof(Door));
            Runtime.RegisterNewMachine(typeof(Timer));

            Console.WriteLine("Configuring the runtime.\n");
            Runtime.Options.Mode = Runtime.Mode.BugFinding;

            Console.WriteLine("Starting the runtime.\n");
            Runtime.Start();
            Runtime.Wait();

            Console.WriteLine("Performing cleanup.\n");
            Runtime.Dispose();
        }
    }
}
