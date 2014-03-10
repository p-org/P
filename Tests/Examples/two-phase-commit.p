event START_2PC;
event WRITE_REQ_REPLICA:(idx:int,val:int);
event WRITE_RESP_REPLICA_COMMIT;
event WRITE_RESP_REPLICA_ABORT;
event READ_REQ_REPLICA:int;
event READ_RESP_REPLICA:int;
event GLOBAL_ABORT;
event GLOBAL_COMMIT;
event WRITE_REQ:(client:mid, idx:int, val:int);
event READ_REQ:(client:mid, idx:int);
event WRITE_RESP;
event READ_RESP:int;
event Unit;

machine Replica {
	var coordinator: id;
    var data: map[int,int];
    start state _Init {
	    entry {
		    coordinator = (id)payload;
			raise(Unit);
		}
		on Unit goto Init;
	}

	state Init {
		on WRITE_REQ_REPLICA goto DoWrite;
		on READ_REQ_REPLICA goto DoRead;
	}

	state DoRead {
		entry {
			send(coordinator, READ_RESP_REPLICA, data[payload]);
			raise(Unit);
		}
		on Unit goto Init;
	}

	var shouldCommit:bool;
	state DoWrite {
		entry {
			shouldCommit = ShouldCommitWrite();
			if (shouldCommit) {
				send(coordinator, WRITE_RESP_REPLICA_COMMIT);
			} else {
				send(coordinator, WRITE_RESP_REPLICA_ABORT);
			}
			raise(Unit);
		}
		on Unit goto WaitForGlobalCommitOrAbort;
	}

	state WaitForGlobalCommitOrAbort {
		on GLOBAL_ABORT goto Init;
		on GLOBAL_COMMIT goto Init;
	}

	model fun ShouldCommitWrite(): bool 
	{
		return *;
	}
}

machine Coordinator {
    var log: seq[eid];
	var replicas: seq[id];
	var numReplicas: int;
	var i: int;
	var pendingWriteReq: (client: mid, idx: int, val: int);
	var pendingReadReq: (client: mid, idx: int);

	start state _Init {
		entry {
			numReplicas = (int)payload;
			assert (numReplicas > 0);
			i = 0;
			while (i < numReplicas) {
				replicas[i] = new Replica(this);
				i = i + 1;
			}
			raise(Unit);
		}
		on Unit goto Init;
	}

	state Init {
		on WRITE_REQ goto DoWrite {
			pendingWriteReq = ((client: mid, idx: int, val: int))payload;
		};
		on READ_REQ goto DoRead {
			pendingReadReq = ((client: mid, idx: int))payload;
		};
	}

	state DoWrite {
		entry {
			log[sizeof(log)] = START_2PC;
			i = 0;
			while (i < sizeof(replicas)) {
				send(replicas[i], WRITE_REQ_REPLICA, (idx=pendingWriteReq.idx, val=pendingWriteReq.val));
				i = i + 1;
			}
			raise(Unit);
		}
		on Unit goto CountVote;
	}

	state CountVote {
		entry {
			if (i == 0) {
				log[sizeof(log)] = GLOBAL_COMMIT;
				while (i < sizeof(replicas)) {
					send(replicas[i], GLOBAL_COMMIT);
					i = i + 1;
				}
				send(pendingWriteReq.client, WRITE_RESP);
				raise(Unit);
			}
		}
		on WRITE_RESP_REPLICA_COMMIT goto CountVote {
			i = i - 1;
		};
		on WRITE_RESP_REPLICA_ABORT goto DoWrite {
			log[sizeof(log)] = GLOBAL_ABORT;
			i = 0;
			while (i < sizeof(replicas)) {
				send(replicas[i], GLOBAL_ABORT);
				i = i + 1;
			}
		};
		on Unit goto Init;
	}

	state DoRead {
		entry {
			send(replicas[0], READ_REQ_REPLICA, pendingReadReq.idx);
		}
		on READ_RESP_REPLICA goto Init {
			send(pendingReadReq.client, READ_RESP, payload);
		};
	}
}

model machine Client {
    var coordinator: mid;
    start state Init {
	    entry {
	        coordinator = (mid)payload;
			raise(Unit);
		}
		on Unit goto DoWrite;
	}
	var idx: int;
	var val: int;
	state DoWrite {
	    entry {
			idx = ChooseIndex();
			val = ChooseValue();
			send(coordinator, WRITE_REQ, (this, idx, val));
		}
		on WRITE_RESP goto DoRead;
	}

	state DoRead {
	    entry {
			send(coordinator, READ_REQ, (this, idx));
		}
		on READ_RESP goto DoWrite {
			assert ((int)payload == val);
		};
	}

	fun ChooseIndex(): int
	{
		return 0;
	}

	fun ChooseValue(): int
	{
		return 0;
	}
}

main model machine TwoPhaseCommit {
    var coordinator: id;
	var client: mid;
    start state Init {
	    entry {
	        coordinator = new Coordinator(3);
			client = new Client(coordinator);
		}
	}
}