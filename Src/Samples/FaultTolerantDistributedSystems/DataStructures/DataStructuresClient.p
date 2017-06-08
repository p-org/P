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
            repDS = new SMRServerInterface(client = this as SMRClientInterface, reorder = false, id = 0);
            goto StartPumpingRequests;
        }
    }
    state StartPumpingRequests {
        entry {
            if(numOfOperations == 0)
            {
                raise halt;
            }
            else
            {
                
            }
        }
    }     
}