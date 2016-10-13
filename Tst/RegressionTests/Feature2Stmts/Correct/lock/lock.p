event ACQUIRE_REQ: machine;
event ACQUIRE_RESP: any;
event RELEASE: any;

machine Lock {
	var data: any;

	start state Unheld {
		entry (v: any) {
			data = v xfer;
		}
		on ACQUIRE_REQ goto Held;
	}

	state Held {
		entry (client: machine) {
			var v: any;
			data = v swap;
			send client, ACQUIRE_RESP, v xfer;
		}
		on RELEASE goto Unheld;
	}
}

machine Client {
	var lock: machine;
	var iter: int;

	start state Init {
		entry (x: (machine, int)) {
			lock = x.0;
			iter = x.1;
			Work();
		}
	}

	fun Work() {
		var data: seq[int];
		var i: int;
		i = 0;
		while (i < iter) {
			send lock, ACQUIRE_REQ, this;
			receive {
				case ACQUIRE_RESP: (v: any) {
					v = data swap;
					Process(data swap);
					v = data swap;
					send lock, RELEASE, v xfer;
				}
			}
			i = i + 1;
		}
	}

	fun Process(data: seq[int]) {
		data += (0, 1);
	}
}

machine Main {
    start state S {
	    entry {
			var lock: machine;
			var client1: machine;
			var client2: machine;
			var data:seq[int];

			lock = new Lock(data);
			client1 = new Client(lock, 3);
			client2 = new Client(lock, 2);
		}
	}
}
