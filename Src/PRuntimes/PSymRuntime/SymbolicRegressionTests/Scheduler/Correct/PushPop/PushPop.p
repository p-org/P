event eOpenDoor assume 1;
event eCloseDoor assume 1;
event eResetDoor assert 1;
event eDoorOpened assert 1;
event eDoorClosed assert 1;
event eDoorStopped assert 1;
event eObjectDetected assert 1;
event eTimerFired assume 1;
event eOperationSuccess assert 1;
event eOperationFailure assert 1;
event eSendCommandToOpenDoor assume 1;
event eSendCommandToCloseDoor assume 1;
event eSendCommandToStopDoor assume 1;
event eSendCommandToResetDoor assume 1;
event eStopTimerReturned assume 1;
event eStoppingTimer assume 1;
event eStopTimer assume 1;
event eUnit assert 1;
event eStartTimer;
event eObjectEncountered assert 1;

machine Elevator {
    var TimerV, DoorV: machine;

    start state Init {
        entry {
            TimerV = new Timer(this);
            DoorV = new Door(this);
            raise eUnit;
        }

        on eUnit goto DoorClosed;
    }

    state DoorClosed {
        ignore eCloseDoor;

        entry {
            send DoorV, eSendCommandToResetDoor;
        }

        on eOpenDoor goto DoorOpening;
    }

    state DoorOpening {
        ignore eOpenDoor;
        defer eCloseDoor;

        entry {
            send DoorV, eSendCommandToOpenDoor;
        }

        on eDoorOpened goto DoorOpened;
    }

    state DoorOpened {
        defer eCloseDoor;

        entry {
            send DoorV, eSendCommandToResetDoor;
            send TimerV, eStartTimer;
        }

        on eOpenDoor push StoppingTimer;
        on eTimerFired goto DoorOpenedOkToClose;
    }

    state DoorOpenedOkToClose {
        defer eOpenDoor;
        entry {
          send TimerV, eStartTimer;
        }

        on eCloseDoor push StoppingTimer;
        on eTimerFired goto DoorClosing;
    }

    state StoppingTimer {
        defer eOpenDoor, eObjectDetected, eCloseDoor;
        entry {
            send TimerV, eStopTimer;
        }

        on eOperationSuccess goto StoppedTimer;
        on eOperationFailure goto WaitingForTimer;
    }

    state WaitingForTimer {
        defer eOpenDoor, eObjectDetected, eCloseDoor;
        ignore eOperationFailure;
        on eTimerFired goto StoppedTimer;
    }

    state StoppedTimer {
        entry { pop; }
    }

    state DoorClosing {
        defer eCloseDoor;

        entry {
            send DoorV,eSendCommandToCloseDoor;
        }

        on eOpenDoor goto StoppingDoor;
        on eDoorClosed goto DoorClosed;
        on eObjectDetected goto DoorOpening;
    }

    state StoppingDoor {
        defer eCloseDoor;
        ignore eOpenDoor, eObjectDetected;

        entry {
             send DoorV,eSendCommandToStopDoor;
        }

        on eDoorOpened goto DoorOpened;
        on eDoorClosed goto DoorClosed;
        on eDoorStopped goto DoorOpening;
    }
}

machine Main {
    var ElevatorV : machine;
    var count: int;
    start state Init {
        entry {
            ElevatorV = new Elevator();
            goto Loop;
        }
    }

    state Loop {
        entry {
            if ($) {
		send ElevatorV, eOpenDoor;
            } else if ($) {
                send ElevatorV,eCloseDoor;
            }

            if(count == 2)
            {
                goto Done;
            }
            count = count + 1;
            goto Loop;
        }
    }

    state Done {}
}

machine Door {
    var ElevatorV : machine;
    var objectCount: int;

    start state _Init {
	entry (payload: machine) { ElevatorV = payload; goto WaitForCommands; }

    }

    state WaitForCommands
    {
        ignore eSendCommandToStopDoor, eSendCommandToResetDoor, eResetDoor;
        on eSendCommandToOpenDoor goto OpenDoor;
        on eSendCommandToCloseDoor goto ConsiderClosingDoor;
    }

    state OpenDoor {
        entry {
            send ElevatorV,eDoorOpened;
            raise eUnit;
        }

        on eUnit goto ResetDoor;
    }

    state ConsiderClosingDoor {
        entry {
            if (objectCount == 0) {
                raise eUnit;
            } else if ($) {
                raise eUnit;
            } else if ($) {
                objectCount = objectCount + 1;
                raise eObjectEncountered;
            }
        }

        on eUnit goto CloseDoor;
        on eObjectEncountered goto ObjectEncountered;
        on eSendCommandToStopDoor goto StopDoor;
    }

    state ObjectEncountered {
        entry {
            send ElevatorV, eObjectDetected;
            goto WaitForCommands;
        }
    }

    state CloseDoor {
        entry {
             send ElevatorV,eDoorClosed;
             goto ResetDoor;
        }
    }

    state StopDoor {
        entry {
            send ElevatorV,eDoorStopped;
            goto OpenDoor;
        }
    }

    state ResetDoor {
        ignore eSendCommandToOpenDoor, eSendCommandToCloseDoor, eSendCommandToStopDoor;
        on eSendCommandToResetDoor goto WaitForCommands;
    }
}

machine Timer {
    var creator: machine;
    var count : int;
    start state Init {
        entry (client: machine) {
            creator = client;
            goto WaitForStart;
        }
    }
    state WaitForStart {
        ignore eStopTimer;
        on eStartTimer goto TimerStarted;
    }
    state TimerStarted {
        entry {
          if ($) {
            raise eUnit;
          }
        }
        on eUnit goto SendTimerFired;
        on eStopTimer goto ConsiderStopping;
    }
    state ConsiderStopping {
        defer eStartTimer;
        entry {
            if ($) {
                send creator, eOperationFailure;
                send creator, eTimerFired;
            } else {
                send creator, eOperationSuccess;
                send creator, eTimerFired;
            }
            raise eUnit;
        }
        on eUnit goto WaitForStart;
    }
    state SendTimerFired {
        defer eStartTimer;
        entry {
            send creator, eTimerFired;
            raise eUnit;
        }
        on eUnit goto WaitForStart;
    }
}
