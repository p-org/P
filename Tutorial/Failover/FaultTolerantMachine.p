enum MyState { State0, State1 }

event eDoOpI;
event eDoOpJ;
event eQueryState: machine;
event eQueryStateResponse: MyState;
event eUpdateToState0;
event eUpdateToState1;

interface IService() receives eDoOpI, eDoOpJ;
interface IReliableStorage() receives eQueryState, eUpdateToState0, eUpdateToState1;
type Pair = (IService, IReliableStorage);

machine FaultTolerantMachine
receives eQueryStateResponse, halt;
{
    var service: IService;
    var reliableStorage: IReliableStorage;
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
            send service, eDoOpI;
            PossiblyRaiseHalt();
            send reliableStorage, eUpdateToState1;
            goto State1;
        }
    }

    state State1 {
        entry { 
            send service, eDoOpJ; 
            PossiblyRaiseHalt();
            send reliableStorage, eUpdateToState0;
            goto State0;
        }
    }

    fun PossiblyRaiseHalt()
    {
        receive {
	    case halt: { raise halt; }
            case null: { }
        }
    }
}
		       
machine Service
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

            assert i == j + 1;
        }
        on eDoOpJ do {
            if (!donej) {
                j = j + 1;
                donej = true;
            }
            donei = false;

            assert i == j;
        }
    }
}

machine ReliableStorage
{
    var s: MyState;
    start state Init {
        entry {
            s = State0;
        }
        on eQueryState do (m: machine) {
            send m, eQueryStateResponse, s;
        }
        on eUpdateToState0 do {
            s = State0;
        }
        on eUpdateToState1 do {
            s = State1;
        }
    }
}
