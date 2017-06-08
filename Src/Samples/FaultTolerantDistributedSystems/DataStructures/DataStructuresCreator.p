/**********************************
DSCreatorMachine:
The DSCreatorMachine creates the datastructure state machine. 
It creates the replicated datastructure state machine if fault tolerance is needed.
**********************************/

machine DSCreatorMachine 
{
    start state Init {
        entry (constArg: ((SMRClientInterface, int, bool))){

        }
    }
}