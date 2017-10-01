event ACQUIRE_REQ: machine;
event ACQUIRE_RESP: any;
event RELEASE: any;

machine Lock {
	var data_v: any;

	start state Unheld {
		entry (v: any) {
			data_v = v move;
		}
		on ACQUIRE_REQ goto Held;
	}

	state Held {
		entry (client: machine) {
			var v: any;
			data_v = v swap;
			send client, ACQUIRE_RESP, v move;
		}
		defer ACQUIRE_REQ;
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
		var data_v: seq[int];
		var i: int;
		i = 0;
		while (i < iter) {
			send lock, ACQUIRE_REQ, this;
			receive {
				case ACQUIRE_RESP: (v: any) {
					v = data_v swap;
					Process(data_v swap);
					v = data_v swap;
					send lock, RELEASE, v move;
				}
			}
			i = i + 1;
		}
	}

	fun Process(data_v: seq[int]) {
		data_v += (0, 1);
	}
}

machine Main {
    start state S {
	    entry {
			var lock: machine;
			var client1: machine;
			var client2: machine;
			var data_v:seq[int];

			lock = new Lock(data_v);
			client1 = new Client(lock, 3);
			client2 = new Client(lock, 2);
		}
	}
}
