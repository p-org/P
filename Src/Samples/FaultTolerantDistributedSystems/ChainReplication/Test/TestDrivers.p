/*******************************************************************************
We create test driver to create the SMR protocol with FT 1
and use a simple test harness that performs (add, substract and read).
The test driver and the refinement check tests that the SMR protocol satisfies the 
linearizability property in the presence of failures.
*******************************************************************************/
event dummyOp;
event dummyResp : int;
machine TestDriver1 : SMRClientInterface
sends eSMROperation;
{
	var SMRLeader : SMRServerInterface;
	var totalOperations : int;
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
			raise payload.response, payload.val; 
		}
		on eSMRLeaderUpdated do (payload: (int, SMRServerInterface)) {
			SMRLeader = payload.1;
		}


		state StartPumpingOperations {
			entry {

				if(totalOperations == 0)
					raise halt;
				
				SendSMROperation(SMRLeader, dummyOp, $, this);
				totalOperations = totalOperations - 1;
			}

			on null goto StartPumpingOperations;
			on 
		}
	}
}

machine SMRReplicatedMachine : SMRReplicatedMachineInterface 
sends eSMRResponse;
{
	start state Init {
	}
}