//We implemented a fault-tolerant 2-PC protocol.
//There is a single co-ordinator and 2 participants. The two participants are two bank accounts.
//transactions are of the form (1, -1) where the 1 is added to first account and 1 is deducted from the other


//Events used for interaction between client and coordinator
event TRANSACTION : (source: machine, val1: int, val2: int);
event TRANSACTION_FAIL;
event TRANSACTION_SUCCESS : (tid: int);
event READ_TRANSACTION : (source: machine, tid: int);
event TRANSACTION_VALUE :(tid:int, acc: int, val: int);

//Events used for interaction between coordinator and participants
event TM_PREPARE : (tid: int, val: int);
event RM_PREPARED : (tid: int);
event RM_NOTPREPARED : (tid: int);
event TM_COMMIT : (tid: int);
event TM_ABORT : (tid: int);
event RM_STATUS_QUERY : (source: SMR_REPLICATED_MACHINE_IN, tid: int);
event READ_QUERY : (tid: int);
event READ_RESPONSE : (tid:int, val: int);

event local;
event done;
event nextTransaction;

interface CLIENT_MACHINE_PUBLIC_IN TRANSACTION_FAIL, TRANSACTION_SUCCESS, TRANSACTION_VALUE;
interface COOR_MACHINE_PUBLIC_IN SMR_RESPONSE, SMR_SERVER_UPDATE, TRANSACTION, READ_TRANSACTION;


module TPC_Client 
sends TRANSACTION, READ_TRANSACTION, STARTTIMER, CANCELTIMER
creates Timer_Machine
{
	machine Client_Machine
	receives TRANSACTION_FAIL, TRANSACTION_SUCCESS, TRANSACTION_VALUE, 
			 CANCEL_SUCCESS, CANCEL_FAIL, TIMEOUT
	{
		var coor: COOR_MACHINE_PUBLIC_IN;
		var numOfOperation : int;
		var success : seq[int];
		var value : int;
		var timer : Timer_Machine;
		start state Init {
			entry {
				Print();
				coor = (payload as (COOR_MACHINE_PUBLIC_IN, int, int)).0;
				numOfOperation = (payload as (COOR_MACHINE_PUBLIC_IN, int, int)).1;
				value = (payload as (COOR_MACHINE_PUBLIC_IN, int, int)).2;
				timer = new Timer_Machine(this as TIMER_CLIENT_IN);
				raise local;
			}
			on local goto StartPumping;
		}
		
		fun CancelTimer() {
			send timer, CANCELTIMER;
			receive 
			{
				case CANCEL_SUCCESS : {}
				case CANCEL_FAIL : {
					receive { 
						case TIMEOUT: {}
					}
				}
			}
			raise local;
		}
		state StartPumping {
			entry {
				if(numOfOperation == 0)
					raise done;
				SEND(coor, TRANSACTION, (source = this as CLIENT_MACHINE_PUBLIC_IN, val1 = value, val2 = value));
				send timer, STARTTIMER;
				
				value = value + 1;
				numOfOperation = numOfOperation - 1;
			}
			on TRANSACTION_FAIL do CancelTimer;
			on TRANSACTION_SUCCESS goto StartPumping with {
				success += (0, payload.tid);
			};
			on TIMEOUT goto StartPumping;
			on local goto StartPumping;
			on done goto Block with { i  = 0;};
		}
		
		var i : int;
		state ReadTransaction {
			ignore TIMEOUT, TRANSACTION_FAIL, TRANSACTION_SUCCESS;
			entry {
					if(i >= sizeof(success))
						raise done;
					SEND(coor, READ_TRANSACTION, (source = this as CLIENT_MACHINE_PUBLIC_IN, tid = success[i]));
					i = i + 1;
			}
			on null goto ReadTransaction;
			on done goto Block;
			on TRANSACTION_VALUE do {
				assert(payload.val == success[payload.tid]);
			};
		}
		
		state Block{
			entry {
				Print();
			}
			ignore TIMEOUT, TRANSACTION_FAIL, TRANSACTION_SUCCESS;
			on TRANSACTION_VALUE do {
				assert(payload.val == success[payload.tid]);
			};
		}
	}
}

module TPC_Coordinator
sends SMR_OPERATION, TRANSACTION_FAIL, TRANSACTION_SUCCESS, TRANSACTION_VALUE,
	  STARTTIMER, CANCELTIMER  
creates SMR_Machine, Timer_Machine
{
	machine TPC_Coor_Machine
	receives SMR_RESPONSE, SMR_SERVER_UPDATE, TRANSACTION, READ_TRANSACTION,
			 CANCEL_SUCCESS, CANCEL_FAIL, TIMEOUT
	{
		var transId : int;
		var part : map[int, SMR_SERVER_IN];
		var timer : Timer_Machine;
		var transStatus : map[int, (bool, int)];
		var currentTransaction: (source: machine, val1: int, val2: int);
		start state Init {
			entry {
				var temp : SMR_SERVER_IN;
				var container : machine;
				var clients : seq[SMR_CLIENT_IN];
				clients += (0, this as SMR_CLIENT_IN);
				
				transId = 0;
				//create timer
				timer = new Timer_Machine(this as TIMER_CLIENT_IN);
				container = CREATECONTAINER();
				CreateSMR(container, (clients, false, 0));
				receive {
					case SMR_SERVER_UPDATE : { part[payload.0] =  payload.1; }
				}
				container = CREATECONTAINER();
				CreateSMR(container, (clients, false, 1));
				receive {
					case SMR_SERVER_UPDATE : { part[payload.0] =  payload.1; }
				}
				
				raise local;
			}
			on local push WaitForReq;
			on SMR_SERVER_UPDATE do {
				part[payload.0] =  payload.1;
			};
			
			on SMR_RESPONSE do {
				raise payload.response, payload.val;
			};
		}
		
		fun CreateSMR(cont : machine, param: any) : machine
		[container = cont]
		{
			var smr : machine;
			smr = new SMR_SERVER_IN(param);
			return smr;
		}
		
		state WaitForReq {
			ignore RM_NOTPREPARED, RM_PREPARED;
			on TRANSACTION goto ProcessTransaction with {
				currentTransaction = payload;
				transId = transId + 1;
			};
			on READ_TRANSACTION do {
				if($)
					SEND_REL(part[0], SMR_OPERATION, (source = this as SMR_CLIENT_IN, command = READ_QUERY, val = (tid = payload.tid, )));
				else
					SEND_REL(part[1], SMR_OPERATION, (source = this as SMR_CLIENT_IN, command = READ_QUERY, val = (tid = payload.tid, )));
			};
			on READ_RESPONSE do {
				assert(transStatus[payload.tid].1 == payload.val);
			};
		}
		
		
		fun AbortCurrentTransaction() {
			var i : int;
			i =0;
			while(i < sizeof(part))
			{
				SEND(part[i], SMR_OPERATION, (source = this as SMR_CLIENT_IN, command = TM_ABORT, val = (tid = transId, )));
				i = i + 1;
			}

			SEND(currentTransaction.source, TRANSACTION_FAIL, null);
			transStatus[transId] = (false, -1);
			
			CancelTimer();
			raise nextTransaction;
		}
		
		fun CancelTimer() {
			send timer, CANCELTIMER;
			receive 
			{
				case CANCEL_SUCCESS : {}
				case CANCEL_FAIL : {
					receive { 
						case TIMEOUT: {}
					}
				}
			}
		}
		
		var prepareCount : int;
		state ProcessTransaction {
			defer TRANSACTION, READ_TRANSACTION;
			entry{
				var i: int;
				prepareCount = 0;
				i = 0;
				while(i < sizeof(part))
				{
					SEND(part[i], SMR_OPERATION, (source = this as SMR_CLIENT_IN, command = TM_PREPARE, val = (tid = transId, val = currentTransaction.val1)));
					i = i + 1;
				}
				
				//start timer 
				send timer, STARTTIMER;
			}
			on TIMEOUT do AbortCurrentTransaction;
			
			on RM_NOTPREPARED do {
				if(payload.tid != transId)
					return;
				else
					AbortCurrentTransaction();
				
			};
			
			on RM_PREPARED do {
				var i : int;
				if(payload.tid == transId)
				{
					prepareCount = 	prepareCount + 1;
					if(prepareCount ==  2)
					{
						
						i =0;
						while(i< sizeof(part))
						{
							//commit current transaction
							SEND(part[i], SMR_OPERATION, (source = this as SMR_CLIENT_IN, command = TM_COMMIT, val = (tid = transId, )));
							i = i + 1;
						} 
						
						transStatus[transId] = (true, currentTransaction.val1);
						SEND(currentTransaction.source, TRANSACTION_SUCCESS, (tid = transId,));
						CancelTimer();
						raise nextTransaction;
					}
				}
			};
			
			on nextTransaction goto WaitForReq;
			
			on RM_STATUS_QUERY do {
				if((payload.tid in transStatus) && transStatus[payload.tid].0)
					SEND(payload.source, SMR_RM_OPERATION, (source = this as SMR_CLIENT_IN, command = TM_COMMIT, val = (tid = payload.tid, )));
				else
					SEND(payload.source, SMR_RM_OPERATION, (source = this as SMR_CLIENT_IN, command = TM_ABORT, val = (tid = payload.tid, )));
			};
			
			on READ_RESPONSE do {
				assert(transStatus[payload.tid].1 == payload.val);
			};
		}
	}
}

module TPC_Participant
sends SMR_RESPONSE
{
	machine Participant_Machine
	receives SMR_RM_OPERATION
	{
		var log : map[int, int];
		var myId : int;
		var client : SMR_CLIENT_IN;
		var preparedValue: (tid: int, val: int);
		start state Init {
			entry {
				myId = payload as int;
				raise local;
			}
			on local push WaitForPrepare;
			on SMR_RM_OPERATION do {
				client = payload.source;
				raise payload.command, payload.val;
			};
			on READ_QUERY do {
				SEND_REL(client, SMR_RESPONSE, (response = READ_RESPONSE, val = (tid = payload.tid, val = log[payload.tid])));
			};
		}
		
		state WaitForPrepare{
			on TM_PREPARE goto WaitForCommitOrAbort with 
			{
				preparedValue = payload;
				if(true)// not considering the case when it says abort
					SEND(client, SMR_RESPONSE, (response = RM_PREPARED, val = (tid = payload.tid,)));
				else
					SEND(client, SMR_RESPONSE, (response = RM_NOTPREPARED, val = (tid = payload.tid,)));
			};
			on TM_COMMIT do { assert(false); };
			ignore TM_ABORT;
		}
		
		state WaitForCommitOrAbort{
			on TM_COMMIT goto WaitForPrepare with {
				assert(preparedValue.tid == payload.tid);
				if(preparedValue.tid == payload.tid)
				{
					log[preparedValue.tid] = preparedValue.val;
				}
			};
			on TM_ABORT goto WaitForPrepare;
			on TM_PREPARE do {
				assert(preparedValue.tid < payload.tid);
				SEND(client, SMR_RESPONSE, (response = RM_STATUS_QUERY, val = (source = this, tid = preparedValue.tid)));
			};
		}
		
	}
}








