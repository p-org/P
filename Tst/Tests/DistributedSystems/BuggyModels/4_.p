event REQ_REPLICA:(seqNum:int, key:int, val:int);
event RESP_REPLICA_COMMIT:int;
event RESP_REPLICA_ABORT:int;
event GLOBAL_ABORT:int;
event GLOBAL_COMMIT:int;
event WRITE_REQ:(client:id, key:int, val:int);
event WRITE_FAIL;
event WRITE_SUCCESS;
event READ_REQ_REPLICA:int;
event READ_REQ:(client:id, key:int);
event READ_FAIL:int;
event READ_SUCCESS:int;
event REP_READ_FAIL;
event REP_READ_SUCCESS:int;
event Unit;
event Timeout;
event StartTimer:int;
event CancelTimer;
event CancelTimerFailure;
event CancelTimerSuccess;
event MONITOR_WRITE_SUCCESS:(m: id, key:int, val:int);
event MONITOR_WRITE_FAILURE:(m: id, key:int, val:int);
event MONITOR_READ_SUCCESS:(m: id, key:int, val:int);
event MONITOR_READ_FAILURE:id;
event MONITOR_UPDATE:(m:id, key:int, val:int);
event goEnd;
event final;

/*
All the external APIs which are called by the protocol
*/
model machine Timer {
	var target: id;
	start state Init {
		entry {
			target = (id)payload;
			raise(Unit);
		}
		on Unit goto Loop;
	}

	state Loop {
		ignore CancelTimer;
		on StartTimer goto TimerStarted;
	}

	state TimerStarted {
		entry {
			if (*) {
				send(target, Timeout);
				raise(Unit);
			}
		}
		on Unit goto Loop;
		on CancelTimer goto Loop {
			if (*) {
				send(target, CancelTimerFailure);
				send(target, Timeout);
			} else {
				send(target, CancelTimerSuccess);
			}		
		};
	}
}

machine Replica 
{

	var coordinator: id;
    var data: map[int,int];
	var pendingWriteReq: (seqNum: int, key: int, val: int);
	var shouldCommit: bool;
	var lastSeqNum: int;
	
	state Init {
		entry {

			coordinator = (id)payload;
			lastSeqNum = 0;
			raise(Unit);
		}
		on Unit goto Loop;
	}

	action HandleReqReplica {
		pendingWriteReq = ((seqNum:int, key:int, val:int))payload;
		assert (pendingWriteReq.seqNum > lastSeqNum);
		shouldCommit = ShouldCommitWrite();
		if (shouldCommit) {
			_SEND(coordinator, RESP_REPLICA_COMMIT, pendingWriteReq.seqNum);
		} else {
			_SEND(coordinator, RESP_REPLICA_ABORT, pendingWriteReq.seqNum);
		}
	}

	action HandleGlobalAbort {
		assert (pendingWriteReq.seqNum >= payload);
		if (pendingWriteReq.seqNum == payload) {
			lastSeqNum = payload;
		}
	}

	action HandleGlobalCommit {
		assert (pendingWriteReq.seqNum >= payload);
		if (pendingWriteReq.seqNum == payload) {
			data.update(pendingWriteReq.key, pendingWriteReq.val);
			//invoke Termination(MONITOR_UPDATE, (m = this, key = pendingWriteReq.key, val = pendingWriteReq.val));
			lastSeqNum = payload;
		}
	}

	action ReadData{
		if(payload in data)
			_SEND(coordinator, REP_READ_SUCCESS, data[payload]);
		else
			_SEND(coordinator, REP_READ_FAIL, null);
	}
	
	state Loop {
		on GLOBAL_ABORT do HandleGlobalAbort;
		on GLOBAL_COMMIT do HandleGlobalCommit;
		on REQ_REPLICA do HandleReqReplica;
		on READ_REQ_REPLICA do ReadData;
	}

	model fun ShouldCommitWrite(): bool 
	{
		return *;
	}

	//necessary variable
	var sendPort:id;
    var receivePort:id;
    var initMessage:(nodemanager:id, param:any, sender:id, receiver:id);

    //common start state
    start state BootingState {
		entry {
			initMessage = ((nodemanager:id, param:any, sender:id, receiver:id))payload;
			sendPort = initMessage.sender;
            receivePort = initMessage.receiver;
            send(receivePort, hostM, this);
            raise(StartE, initMessage.param);
		}
		on StartE goto Init;
	}

	//send to the sender machine
	fun _SEND(target:id, e:eid, p:any) {
		send(sendPort, sendMessage, (source = this, target = target, e = e, p = p));
	}

    fun _SENDRELIABLE(target:id, e:eid, p:any) {
        send(sendPort, sendRelMessage, (source = this, target = target, e = e, p = p));
    }
	// This function sets up the entire VM and sets up the nodeManager.
	var model_s:id;
	var model_r:id;
	var model_h:id;
	model fun _CREATENODE() : id {
		//set up the VM
		model_s = new SenderMachine((nodemanager = null, param = 3));
		model_r = new ReceiverMachine((nodemanager = null, param = null));
        model_h = new NodeManager((nodemanager = null, param = null, sender = model_s, receiver = model_r));
		
		return model_r;
	}
	
	var createmachine_return:id;
	var createmachine_param:(nodeManager:id, typeofmachine:int, param:any);
	state _CREATEMACHINE {
		entry{
			//NOTE : That the create machine right now uses the P Send.
			_SENDRELIABLE(createmachine_param.nodeManager, Req_CreatePMachine, (creator = receivePort, typeofmachine = createmachine_param.typeofmachine, constructorparam = createmachine_param.param));
		}
        on Resp_CreatePMachine do PopState;
	}

	
	action PopState {
		createmachine_return = payload.receiver;
		return;
	}


}

machine Coordinator 
{

	var data: map[int,int];
	var replicas: seq[id];
	var numReplicas: int;
	var i: int;
	var pendingWriteReq: (client: id, key: int, val: int);
	var replica: id;
	var currSeqNum:int;
	var timer: mid;
	var client:id;
	var key: int;
	var readResult: (bool, int);
	var creatorMachine:id;
	var temp_NM:id;
	
	state Init {
		entry {
			
			numReplicas = (int)payload;
			assert (numReplicas > 0);
			i = 0;
			while (i < numReplicas) {
				temp_NM = _CREATENODE();
				createmachine_param = (nodeManager = temp_NM, typeofmachine = 2, param = receivePort);
				call(_CREATEMACHINE);
				replica = createmachine_return;
				replicas.insert(i, replica);
				i = i + 1;
			}
			currSeqNum = 0;
			//new Termination(this, replicas);
			timer = new Timer(this);
			raise(Unit);
		}
		on Unit goto Loop;
	}
	
	state DoRead {
		entry {
			client = payload.client;
			key = payload.key;
			call(PerformRead);
			if(readResult[0])
				raise(READ_FAIL, readResult[1]);
			else
				raise(READ_SUCCESS, readResult[1]);
		}
		on READ_FAIL goto Loop
		{
			_SEND(client, READ_FAIL, payload);
		};
		on READ_SUCCESS goto Loop
		{	
			_SEND(client, READ_SUCCESS, payload);
		};
	}
	
	model fun ChooseReplica()
	{
			if(*) 
				_SEND(replicas[0], READ_REQ_REPLICA, key);
			else
				_SEND(replicas[sizeof(replicas) - 1], READ_REQ_REPLICA, key);
				
	}
	
	state PerformRead {
		entry{ ChooseReplica(); }
		on REP_READ_FAIL do ReturnResult;
		on REP_READ_SUCCESS do ReturnResult;
		
	}
	
	action ReturnResult {
		if(trigger == REP_READ_FAIL)
			readResult = (true, -1);
		else
			readResult = (false, (int)payload);
		
		return;
	}
	action DoWrite {
		pendingWriteReq = payload;
		currSeqNum = currSeqNum + 1;
		i = 0;
		while (i < sizeof(replicas)) {
			_SEND(replicas[i], REQ_REPLICA, (seqNum=currSeqNum, key=pendingWriteReq.key, val=pendingWriteReq.val));
			i = i + 1;
		}
		send(timer, StartTimer, 100);
		raise(Unit);
	}

	state Loop {
		on WRITE_REQ do DoWrite;
		on Unit goto CountVote;
		on READ_REQ goto DoRead;
		ignore RESP_REPLICA_COMMIT, RESP_REPLICA_ABORT;
	}

	fun DoGlobalAbort() {
		i = 0;
		while (i < sizeof(replicas)) {
			_SEND(replicas[i], GLOBAL_ABORT, currSeqNum);
			i = i + 1;
		}
		_SEND(pendingWriteReq.client, WRITE_FAIL, null);
	}

	state CountVote {
		entry {
			if (i == 0) {
				while (i < sizeof(replicas)) {
					_SEND(replicas[i], GLOBAL_COMMIT, currSeqNum);
					i = i + 1;
				}
				data.update(pendingWriteReq.key, pendingWriteReq.val);
				//invoke Termination(MONITOR_UPDATE, (m = this, key = pendingWriteReq.key, val = pendingWriteReq.val));
				_SEND(pendingWriteReq.client, WRITE_SUCCESS, null);
				send(timer, CancelTimer);
				raise(Unit);
			}
		}
		defer WRITE_REQ, READ_REQ;
		on RESP_REPLICA_COMMIT goto CountVote {
			if (currSeqNum == (int)payload) {
				i = i - 1;
			}
		};
		on RESP_REPLICA_ABORT do HandleAbort;
		on Timeout goto Loop {
			DoGlobalAbort();
		};
		on Unit goto WaitForCancelTimerResponse;
	}

	action HandleAbort {
		if (currSeqNum == (int)payload) {
			DoGlobalAbort();
			send(timer, CancelTimer);
			raise(Unit);
		}
	}

	state WaitForCancelTimerResponse {
		defer WRITE_REQ, READ_REQ;
		ignore RESP_REPLICA_COMMIT, RESP_REPLICA_ABORT;
		on Timeout, CancelTimerSuccess goto Loop;
		on CancelTimerFailure goto WaitForTimeout;
	}

	state WaitForTimeout {
		defer WRITE_REQ, READ_REQ;
		ignore RESP_REPLICA_COMMIT, RESP_REPLICA_ABORT;
		on Timeout goto Loop;
	}

	//necessary variable
	var sendPort:id;
    var receivePort:id;
    var initMessage:(nodemanager:id, param:any, sender:id, receiver:id);

    //common start state
    start state BootingState {
		entry {
			initMessage = ((nodemanager:id, param:any, sender:id, receiver:id))payload;
			sendPort = initMessage.sender;
            receivePort = initMessage.receiver;
            send(receivePort, hostM, this);
            raise(StartE, initMessage.param);
		}
		on StartE goto Init;
	}

	//send to the sender machine
	fun _SEND(target:id, e:eid, p:any) {
		send(sendPort, sendMessage, (source = this, target = target, e = e, p = p));
	}

    fun _SENDRELIABLE(target:id, e:eid, p:any) {
        send(sendPort, sendRelMessage, (source = this, target = target, e = e, p = p));
    }
	// This function sets up the entire VM and sets up the nodeManager.
	var model_s:id;
	var model_r:id;
	var model_h:id;
	model fun _CREATENODE() : id {
		//set up the VM
		model_s = new SenderMachine((nodemanager = null, param = 3));
		model_r = new ReceiverMachine((nodemanager = null, param = null));
        model_h = new NodeManager((nodemanager = null, param = null, sender = model_s, receiver = model_r));
		
		return model_r;
	}
	
	var createmachine_return:id;
	var createmachine_param:(nodeManager:id, typeofmachine:int, param:any);
	state _CREATEMACHINE {
		entry{
			//NOTE : That the create machine right now uses the P Send.
			_SENDRELIABLE(createmachine_param.nodeManager, Req_CreatePMachine, (creator = receivePort, typeofmachine = createmachine_param.typeofmachine, constructorparam = createmachine_param.param));
		}
        on Resp_CreatePMachine do PopState;
	}

	
	action PopState {
		createmachine_return = payload.receiver;
		return;
	}


}

machine Client 
{

    var coordinator: id;
	var mydata : int;
	var counter : int;

	state Init {
		entry {
			coordinator = ((id,int))payload[0];
			mydata = ((id,int))payload[1];
			counter = 0;
			new ReadWrite(this);
			raise(Unit);
		}
		on Unit goto DoWrite;
	}
	
	
	state DoWrite {
	    entry {
			mydata = mydata + 1; 
			counter = counter + 1;
			if(counter == 2)
				raise(goEnd);
			_SEND(coordinator, WRITE_REQ, (client=receivePort, key=mydata, val=mydata));
		}
		on WRITE_FAIL goto DoRead
		{
			invoke ReadWrite(MONITOR_WRITE_FAILURE, (m = this, key=mydata, val = mydata))
		};
		on WRITE_SUCCESS goto DoRead
		{
			invoke ReadWrite(MONITOR_WRITE_SUCCESS, (m = this, key=mydata, val = mydata))
		};
		on goEnd goto End;
	}

	state DoRead {
	    entry {
			_SEND(coordinator, READ_REQ, (client=receivePort, key=mydata));
		}
		on READ_FAIL goto DoWrite
		{
			invoke ReadWrite(MONITOR_READ_FAILURE, this);
		};
		on READ_SUCCESS goto DoWrite
		{
			invoke ReadWrite(MONITOR_READ_SUCCESS, (m = this, key = mydata, val = payload));
		};
	}

	state End {  }


	//necessary variable
	var sendPort:id;
    var receivePort:id;
    var initMessage:(nodemanager:id, param:any, sender:id, receiver:id);

    //common start state
    start state BootingState {
		entry {
			initMessage = ((nodemanager:id, param:any, sender:id, receiver:id))payload;
			sendPort = initMessage.sender;
            receivePort = initMessage.receiver;
            send(receivePort, hostM, this);
            raise(StartE, initMessage.param);
		}
		on StartE goto Init;
	}

	//send to the sender machine
	fun _SEND(target:id, e:eid, p:any) {
		send(sendPort, sendMessage, (source = this, target = target, e = e, p = p));
	}

    fun _SENDRELIABLE(target:id, e:eid, p:any) {
        send(sendPort, sendRelMessage, (source = this, target = target, e = e, p = p));
    }
	// This function sets up the entire VM and sets up the nodeManager.
	var model_s:id;
	var model_r:id;
	var model_h:id;
	model fun _CREATENODE() : id {
		//set up the VM
		model_s = new SenderMachine((nodemanager = null, param = 3));
		model_r = new ReceiverMachine((nodemanager = null, param = null));
        model_h = new NodeManager((nodemanager = null, param = null, sender = model_s, receiver = model_r));
		
		return model_r;
	}
	
	var createmachine_return:id;
	var createmachine_param:(nodeManager:id, typeofmachine:int, param:any);
	state _CREATEMACHINE {
		entry{
			//NOTE : That the create machine right now uses the P Send.
			_SENDRELIABLE(createmachine_param.nodeManager, Req_CreatePMachine, (creator = receivePort, typeofmachine = createmachine_param.typeofmachine, constructorparam = createmachine_param.param));
		}
        on Resp_CreatePMachine do PopState;
	}

	
	action PopState {
		createmachine_return = payload.receiver;
		return;
	}


}

main machine GodMachine 
{

    var coordinator: id;
	var temp_NM : id;

    start state Init {
	    entry {

			//Let me create my own sender/receiver
			sendPort = new SenderMachine((nodemanager = this, param = 3));
            receivePort = new ReceiverMachine((nodemanager = this, param = null));
            send(receivePort, hostM, this);

			temp_NM = _CREATENODE();
			createmachine_param = (nodeManager = temp_NM, typeofmachine = 1, param = 1);
			call(_CREATEMACHINE); // create coordinator
			coordinator = createmachine_return;
			//temp_NM = _CREATENODE();
			createmachine_param = (nodeManager = temp_NM, typeofmachine = 3, param = (coordinator, 100));
			call(_CREATEMACHINE);// create client machine
			//temp_NM = _CREATENODE();
			//createmachine_param = (nodeManager = temp_NM, typeofmachine = 3, param = (coordinator, 200));
			//call(_CREATEMACHINE);// create client machine
	    }
	}

    
    var sendPort:id;
    var receivePort:id;

    //send to the sender machine
	fun _SEND(target:id, e:eid, p:any) {
		send(sendPort, sendRelMessage, (source = this, target = target, e = e, p = p));
	}

    fun _SENDRELIABLE(target:id, e:eid, p:any) {
        send(sendPort, sendRelMessage, (source = this, target = target, e = e, p = p));
    }
	// This function sets up the entire VM and sets up the nodeManager.
	var model_s:id;
	var model_r:id;
	var model_h:id;
	model fun _CREATENODE() : id {
		//set up the VM
		model_s = new SenderMachine((nodemanager = null, param = 3));
		model_r = new ReceiverMachine((nodemanager = null, param = null));
        model_h = new NodeManager((nodemanager = null, param = null, sender = model_s, receiver = model_r));
		
		return model_r;
	}
	
	var createmachine_return:id;
	var createmachine_param:(nodeManager:id, typeofmachine:int, param:any);
	state _CREATEMACHINE {
		entry{
			//NOTE : That the create machine right now uses the P Send.
			_SENDRELIABLE(createmachine_param.nodeManager, Req_CreatePMachine, (creator = receivePort, typeofmachine = createmachine_param.typeofmachine, constructorparam = createmachine_param.param));
		}
        on Resp_CreatePMachine do PopState;
	}

	
	action PopState {
		createmachine_return = payload.receiver;
		return;
	}

}
event sendRelMessage:(source:id, target:id, e:eid, p:any);
event sendMessage:(source:id, target:id, e:eid, p:any);
event networkMessage:(iden:(source:id, seqnum:int), msg:(e:eid, p:any));
event hostM:id;
event StartE:any;

machine ReceiverMachine {
	var hostMachine:id;
	var lastReceivedMessage: map[id, int];
	var initMessage:(nodemanager:id, param:any, host:id);
	start state bootingState {
        defer networkMessage;
		entry {
			
		}
        on hostM goto InitHost;
	}
	
    state InitHost {
        entry {
            hostMachine = (id)payload;
        }        
        on networkMessage goto Listening; 
    }
	state Listening {
		entry {
			if(payload.iden.source in lastReceivedMessage)
			{
				if(payload.iden.seqnum > lastReceivedMessage[payload.iden.source])
				{
					send(hostMachine, payload.msg.e, payload.msg.p);
					lastReceivedMessage.update(payload.iden.source, payload.iden.seqnum);
				}
			}
			else
			{
				send(hostMachine, payload.msg.e, payload.msg.p);
				lastReceivedMessage.update(payload.iden.source, payload.iden.seqnum);
			}
		}
		on networkMessage goto Listening;
	}

}

machine SenderMachine {
	var numberofRetry: int; 
	var sendFail:bool;
	var i: int;
	var CurrentSeqNum:int;
	start state bootingState {
		entry {
			numberofRetry = 1;//((nodemanager:id, param:int))payload.param;
			sendFail = false;
			CurrentSeqNum = 0;
		}
		on sendMessage goto Listening;
        on sendRelMessage goto Listening;
	}
	
	state Listening {
		entry {
			
			if(trigger == sendRelMessage)
            {
                send(payload.target, networkMessage, (iden = (source = payload.source, seqnum = CurrentSeqNum),msg = (e = payload.e, p = payload.p)));
            }
            else
            {
                i = numberofRetry;
			    while(i != 0 && !sendFail)
			    {
				    sendFail = sendRPC(payload.target, networkMessage, (iden = (source = payload.source, seqnum = CurrentSeqNum),msg = (e = payload.e, p = payload.p)));
				    i = i - 1;
			    }
                
            }
		}
		on sendRelMessage goto Listening;
		on sendMessage goto Listening;
		exit {
			sendFail = false;
			CurrentSeqNum = CurrentSeqNum + 1;
		}
	}
	
	model fun sendRPC(target:id, e:eid, p:any) : bool {
		
		if(*)
		{
			return false;
		}
		else
		{
			send(target, e, p);
			if(*)
				return false;
			else
				return true;
		}
	}
}


//The node manager machine is the first machine to be created on a new VM.
//All new machines created on this VM are created by the NodeManager. 
//It should be capable of creating all types of machines

//Creating a new machine involves following stages.
//Stage 1: create the sender machine which acts the sendPort for the machine to be created.
//Stage 2: create the host machine and make it point to the sender machine, it sends all its messages to the
// sender machine.
//Stage 3: create the receiver machine and it points to the host machine. Receiver machine forwards all the
// messages to the host machine.
// Receiver machine is the external point for contact for the host machine and hence is returned by the 
// NodeManager as a response to createMachine request.

/// Replace this with proper enum types but for the time-being the mapping between int -> machineType is


// 1 -> coordinatorMachine
// 2 -> replicaMachine
// 3 -> clientMachine


//Events
event Req_CreatePMachine:(creator:id, typeofmachine: int, constructorparam: any);
event Resp_CreatePMachine : (receiver:id);

machine NodeManager
{

	
	var sender:id;
	var receiver:id;
	
	state Init {
        on Req_CreatePMachine goto CreateNewMachine;
    }
	state CreateNewMachine {
		entry {

			sender = new SenderMachine((nodemanager = this, param = 3));
            receiver = new ReceiverMachine((nodemanager = this, param = null));
			_CREATELOCALMACHINE(payload.typeofmachine, payload.constructorparam, sender, receiver);
			_SENDRELIABLE(payload.creator, Resp_CreatePMachine, (receiver = receiver));
			

		}
		
		on Req_CreatePMachine goto CreateNewMachine;
	}
	
	fun _CREATELOCALMACHINE(typeofmachine:int, p:any, sender:id, receiver:id) {
		if(typeofmachine == 1)
		{new Coordinator((nodemanager = this, param = p, sender = sender, receiver = receiver));}
		else if(typeofmachine == 2)
			{new Replica((nodemanager = this, param = p, sender = sender, receiver = receiver));}
		else if(typeofmachine == 3)
			{new Client((nodemanager = this, param = p, sender = sender, receiver = receiver));}
		else
		{
			assert(false);
		}
	}

	//necessary variable
	var sendPort:id;
    var receivePort:id;
    var initMessage:(nodemanager:id, param:any, sender:id, receiver:id);

    //common start state
    start state BootingState {
		entry {
			initMessage = ((nodemanager:id, param:any, sender:id, receiver:id))payload;
			sendPort = initMessage.sender;
            receivePort = initMessage.receiver;
            send(receivePort, hostM, this);
            raise(StartE, initMessage.param);
		}
		on StartE goto Init;
	}

	//send to the sender machine
	fun _SEND(target:id, e:eid, p:any) {
		send(sendPort, sendMessage, (source = this, target = target, e = e, p = p));
	}

    fun _SENDRELIABLE(target:id, e:eid, p:any) {
        send(sendPort, sendRelMessage, (source = this, target = target, e = e, p = p));
    }
	// This function sets up the entire VM and sets up the nodeManager.
	var model_s:id;
	var model_r:id;
	var model_h:id;
	model fun _CREATENODE() : id {
		//set up the VM
		model_s = new SenderMachine((nodemanager = null, param = 3));
		model_r = new ReceiverMachine((nodemanager = null, param = null));
        model_h = new NodeManager((nodemanager = null, param = null, sender = model_s, receiver = model_r));
		
		return model_r;
	}
	
	var createmachine_return:id;
	var createmachine_param:(nodeManager:id, typeofmachine:int, param:any);
	state _CREATEMACHINE {
		entry{
			//NOTE : That the create machine right now uses the P Send.
			_SENDRELIABLE(createmachine_param.nodeManager, Req_CreatePMachine, (creator = receivePort, typeofmachine = createmachine_param.typeofmachine, constructorparam = createmachine_param.param));
		}
        on Resp_CreatePMachine do PopState;
	}

	
	action PopState {
		createmachine_return = payload.receiver;
		return;
	}


}

//Monitors


// ReadWrite monitor keeps track of the property that every successful write should be followed by
// successful read and failed write should be followed by a failed read.
// This monitor is created local to each client.

monitor ReadWrite {
	var client : id;
	var datakey:int;
	var dataval:int;
	action DoWriteSuccess {
		if(payload.m == client)
			datakey = payload.key; dataval = payload.val;
	}
	
	action DoWriteFailure {
		if(payload.m == client)
			datakey = -1; dataval = -1;
	}
	action CheckReadSuccess {
		if(payload.m == client)
		{assert(datakey == payload.key && dataval == payload.val);}
			
	}
	action CheckReadFailure {
		if(payload == client)
			assert(datakey == -1 && dataval == -1);
	}
	start state Init {
		entry {
			client = (id) payload;
		}
		on MONITOR_WRITE_SUCCESS do DoWriteSuccess;
		on MONITOR_WRITE_FAILURE do DoWriteFailure;
		on MONITOR_READ_SUCCESS do CheckReadSuccess;
		on MONITOR_READ_FAILURE do CheckReadFailure;
	}
}




