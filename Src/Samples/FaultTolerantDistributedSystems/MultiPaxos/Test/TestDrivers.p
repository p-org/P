/*******************************************************************************
We create test driver to create the SMR protocol with FT 1
*******************************************************************************/
event dummyOp;
machine TestDriver1 : SMRClientInterface
sends eSMROperation;
{
	var SMRLeader : SMRServerInterface;
	var totalOperations : int;
	var operationId : int;
	var commitMap : map[int, int];
	start state Init {
		
		entry {
			//create a SMR implementation with FT = 1
			var args: SMRServerConstrutorType;
			args.client = this as SMRClientInterface;
			args.reorder = false;
			args.isRoot = true;
			args.ft = FT1;
			args.val = 0;
			SMRLeader = new SMRServerInterface(args);
			totalOperations = 10;
			raise local;
		}

		on local push StartPumpingOperations;
		on eSMRResponse do (payload: SMRResponseType) {
			//assert that responses are linearized
			if(payload.respId in commitMap)
			{
				assert(payload.clientOpId == commitMap[payload.respId]);
			}
			else
			{
				commitMap[payload.respId] = payload.clientOpId;
			}
		}
		on eSMRLeaderUpdated do (payload: (int, SMRServerInterface)) {
			SMRLeader = payload.1;
		}

	}
	state StartPumpingOperations {
		entry {

			if(totalOperations == 0)
				raise halt;
			
			SendSMROperation(operationId, SMRLeader, dummyOp, true, this);
			operationId = operationId + 1;
			totalOperations = totalOperations - 1;
		}

		on null goto StartPumpingOperations;
		
	}
	
}

machine TestDriver2 : SMRClientInterface
sends eSMROperation;
{
	var SMRLeader : SMRServerInterface;
	var totalOperations : int;
	var operationId : int;
	var commitMap : map[int, int];
	start state Init {
		
		entry {
			//create a SMR implementation with FT = 1
			var args: SMRServerConstrutorType;
			args.client = this as SMRClientInterface;
			args.reorder = false;
			args.isRoot = true;
			args.ft = FT2;
			args.val = 0;
			SMRLeader = new SMRServerInterface(args);
			totalOperations = 5;
			raise local;
		}

		on local push StartPumpingOperations;
		on eSMRResponse do (payload: SMRResponseType) {
			//assert that responses are linearized
			if(payload.respId in commitMap)
			{
				assert(payload.clientOpId == commitMap[payload.respId]);
			}
			else
			{
				commitMap[payload.respId] = payload.clientOpId;
			}
		}
		on eSMRLeaderUpdated do (payload: (int, SMRServerInterface)) {
			SMRLeader = payload.1;
		}

	}
	state StartPumpingOperations {
		entry {

			if(totalOperations == 0)
				raise halt;
			
			SendSMROperation(operationId, SMRLeader, dummyOp, true, this);
			operationId = operationId + 1;
			totalOperations = totalOperations - 1;
		}

		on null goto StartPumpingOperations;
		
	}
	
}

machine SMRReplicatedMachine : SMRReplicatedMachineInterface 
sends eSMRResponse;
{
	var isLeader: bool;
	start state Init {
		//install common handler
        on eSMRReplicatedMachineOperation do (payload:SMRRepMachOperationType){
			SendSMRResponse(payload.smrop.source, dummyOp, (val = true, ), payload.smrop.clientOpId, payload.respId, isLeader);
        }

        on eSMRReplicatedLeader do {
			isLeader = true;
		}
	}
}