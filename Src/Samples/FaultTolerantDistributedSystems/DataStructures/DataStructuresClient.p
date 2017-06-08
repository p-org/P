/**********************************
DSClientMachine:
The DSClientMachine creates the datastructure state machine. 
It creates the replicated datastructure state machine if fault tolerance is needed.
**********************************/

machine DSClientMachine : DSClientInterface
{
    var numOfOperations
    start state Init {
        entry (payload: int){

        }
    }
     
}