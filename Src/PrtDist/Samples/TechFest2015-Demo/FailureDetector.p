include "Timer.p"

event PING: machine;
event PONG: machine;

event ROUND_DONE;
event REGISTER_CLIENT: machine;
event UNREGISTER_CLIENT: machine;
event NODE_DOWN: machine;
event TIMER_CANCELED;

machine FailureDetector {
	var nodes: seq[machine];
    var clients: map[machine, bool];
	var attempts: int;
	var alive: map[machine, bool];
	var responses: map[machine, bool];
    var timer: machine;
	
    start state Init {
        entry (payload: seq[machine]) {
  	        nodes = payload as seq[machine];
			InitializeAliveSet();
			timer = new Timer(this);
	        raise UNIT;   	   
        }
		on REGISTER_CLIENT do (payload: machine) { clients[payload] = true; };
		on UNREGISTER_CLIENT do (payload: machine) { if (payload in clients) clients -= payload; };
        on UNIT push SendPing;
    }
    state SendPing {
        entry {
		    SendPingsToAliveSet();
			send timer, START, 100;
	    }
        on PONG do (payload: machine){ 
		    if (payload in alive) {
				 responses[payload] = true; 
				 if (sizeof(responses) == sizeof(alive)) {
			         send timer, CANCEL;
					 raise TIMER_CANCELED;
			     }
			}
		};
		on TIMEOUT do { 
			attempts = attempts + 1;
		    if (sizeof(responses) < sizeof(alive) && attempts < 2) {
				raise UNIT;
			}
			Notify();
			raise ROUND_DONE;
		};
		on ROUND_DONE goto Reset;
		on UNIT goto SendPing;
		on TIMER_CANCELED push WaitForCancelResponse;
     }
	 state WaitForCancelResponse {
	     defer TIMEOUT, PONG;
	     on CANCEL_SUCCESS do { raise ROUND_DONE; };
		 on CANCEL_FAILURE do { pop; };
	 }
	 state Reset {
         entry {
			 attempts = 0;
			 responses = default(map[machine, bool]);
			 send timer, START, 1000;
		 }
		 on TIMEOUT goto SendPing;
		 ignore PONG;
	 }

	 fun InitializeAliveSet() {
		var i: int;
		i = 0;
		while (i < sizeof(nodes)) {
			alive += (nodes[i], true);
			i = i + 1;
		}
	 }
	 fun SendPingsToAliveSet() {
		var i: int;
		i = 0;
		while (i < sizeof(nodes)) {
		    if (nodes[i] in alive && !(nodes[i] in responses)) {
				monitor M_PING, nodes[i];
				_SEND(nodes[i], PING, this);
			}
		    i = i + 1;
		}
	 }
	fun Notify() {
	     var i, j: int;
		 i = 0;
		 while (i < sizeof(nodes)) {
		     if (nodes[i] in alive && !(nodes[i] in responses)) {
		         alive -= nodes[i];
				 j = 0;
				 while (j < sizeof(clients)) {
				     _SEND(keys(clients)[j], NODE_DOWN, nodes[i]);
				     j = j + 1;
				 }
			 }
			 i = i + 1;
		 }
	 }
}

machine Node {
	start state WaitPing {
        on PING do (payload: machine) {
			monitor M_PONG, this;
		    _SEND(payload as machine, PONG, this);
		};
    }
}
