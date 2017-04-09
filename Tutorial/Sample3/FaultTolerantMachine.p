enum MyState { State0, State1 }

event eDoOpI;
event eDoOpJ;
event eQueryState: machine;
event eQueryStateResponse: MyState;
event eUpdateState0;
event eUpdateState1;

type IService() = { eDoOpI, eDoOpJ };
type IReliableStorage() = { eQueryState, eUpdateState0, eUpdateState1 };
type Pair = (IService, IReliableStorage);

machine FaultTolerantMachine 
receives eQueryStateResponse, halt;
{
    var service: IService;
    var  reliableStorage: IReliableStorage;
    start state Init {
        entry (arg: (IService, IReliableStorage)) {
            service = arg.0;
            reliableStorage = arg.1;
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
    }

    state State0 {
        entry {
            var _halt: bool;
            send service, eDoOpI;
            _halt = PossiblyHalt();
            if (_halt) raise halt;
            send reliableStorage, eUpdateState1;
        }
    }

    state State1 {
        entry { 
            var _halt: bool;
            send service, eDoOpJ; 
            _halt = PossiblyHalt();
            if (_halt) raise halt;
            send reliableStorage, eUpdateState0;
        }
    }

    fun PossiblyHalt() : bool
    {
        var retVal: bool;
        receive {
            case halt: { retVal = true; }
            case null: { retVal = false; }
        }
        return retVal;
    }
}

machine Service 
receives eDoOpI, eDoOpJ;
{
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

machine ReliableStorage
receives eQueryState, eUpdateState0, eUpdateState1;
{
    var s: MyState;
    start state Init {
        entry {
            s = State0;
        }
        on eQueryState do (m: machine) {
            send m, eQueryStateResponse, s;
        }
        on eUpdateState0 do {
            s = State0;
        }
        on eUpdateState1 do {
            s = State1;
        }
    }
}