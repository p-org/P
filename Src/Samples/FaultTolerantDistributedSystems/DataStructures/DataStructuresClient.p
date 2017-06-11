/**********************************
DSClientMachine:
The DSClientMachine creates the datastructure state machine. 
It creates the replicated datastructure state machine if fault tolerance is needed.
**********************************/

machine DSClientMachine : SMRClientInterface
{
    var numOfOperations : int;
    var repDS : SMRServerInterface;
    start state Init {
        entry (payload: int){
            numOfOperations = payload;
            repDS = new SMRServerInterface((client = this as SMRClientInterface, reorder = false, val = 0));
            goto StartPumpingRequests;
        }
    }

    fun ChooseOp() : DSOperation {
        if($) {
            return ADD;
        } else if ($) {
            return REMOVE;
        } else {
            return READ;
        }
    }

    fun ChooseVal() : int {
        // return a random value between 0 - 10
        var index : int;
        index = 0;
        while(index < 10)
        {
            if($)
            {
                return index;
            }
            index = index + 1;
        }

        return index;
    }

    state StartPumpingRequests {
        entry {
            if(numOfOperations == 0)
            {
                raise halt;
            }
            else
            {
                //perform random operation
            }
        }
    }     
}