enum MyState { State0, State1 };

machine FaultTolerantMachine {
    var service: IService;
    var  reliableStorage: IReliableStorage;
    start state Init (x: IService, y: IReliableStorage) {
        service = x;
        reliableStorage = y;
        send reliableStorage, eQueryState, this;
        receive {
            case eQueryStateResponse: (s: MyState) {
                if (s == State0)
                {
                    goto State0;
                }
                else 
                {
                    goto State1;
                }
            }
        }
    }

    start state State0 {
        entry {
            send service, eDoOpI;
            if (PossiblyHalt()) raise halt;
            send reliableStorage, eUpdateState1;
        }
    }

    state State1 {
        entry { 
            send service, eDoOpJ; 
            if (PossiblyHalt()) raise halt;
            send reliableStorage, eUpdateState0;
        }
    }

    fun PossiblyHalt() : bool
    {
        receive {
            case halt: { return true; }
            case null: { return false; }
        }
    }
}

machine Service {
    var i, j: int;
    var donei, donej: bool;

    start state Init {
        on eDoOpI do {
            if (!donei) {
                i = i + 1;
                donei = true;
            }
            donej = false;
        }
        on eDoOpJ do {
            if (!donej) {
                j = j + 1;
                donej = true;
            }
            donei = false;
        }
    }
}

machine ReliableStorage {
    var s: MyState;
    start state Init {
        entry {
            s = State0;
        }
        on eQueryState do (m: machine) {
            send m, eQueryStateResponse, s;
        }
        on eUpdateState0 {
            s = State0;
        }
        on eUpdateState1 {
            s = State1;
        }
    }
}