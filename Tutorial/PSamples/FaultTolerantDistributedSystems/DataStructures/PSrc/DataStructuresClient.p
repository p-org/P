/**********************************
DSClientMachine:
The DSClientMachine creates the datastructure state machine. 
It creates the replicated datastructure state machine if fault tolerance is needed.
**********************************/

machine DSClientMachine
sends eSMROperation, eDSOperation;
{
    var numOfOperations : int;
    var repDS : SMRServerInterface;
    var operationId : int;
    var respOpId: int;
    start state Init {
        entry (payload: data){
            numOfOperations = payload as int;
            repDS = new SMRServerInterface((client = this to SMRClientInterface, reorder = false, isRoot = true, ft = FT1, val = 0));
            raise local;
        }

        //install the common handler
		on eSMRResponse do (payload: SMRResponseType){
            respOpId = payload.clientOpId;
			raise payload.response, payload.val;
		} 

		on eSMRLeaderUpdated do (payload: (int, SMRServerInterface)) {
			repDS = payload.1;
		}
        
        on local push StartPumpingRequests;
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
            var operation : DSOperation;
            var val : int;
            if(numOfOperations == 0)
            {
                raise halt;
            }
            else
            {
                //perform random operation
                operation = ChooseOp();
                val = ChooseVal();

                //send the operation to replicated data-structure
                SendSMROperation(operationId, repDS, eDSOperation, (op = operation, val = val), this to SMRClientInterface);

                print "Performed operation {0}({1}) with operation id = {2}", operation, val, operationId;

                operationId = operationId + 1;
            }
        }

        on eDSOperationResp do (payload: DSOperationRespType) {
            if(payload.val as bool == false)
            {
                print "operation id : {0} failed", respOpId, payload.val;
            }
            else
            {
                print "operation id : {0} successful, response = {1}", respOpId, payload.val;
            }
        }

    }     
}