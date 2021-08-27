/*
The failure injector machine randomly selects a participant machine and enqueues a special event "halt"
On dequeueing a halt event, the P machine destroyes itself safely.
This is one way of modeling node failures in P.
Note that as the model-checker explores all possible interleavings. The failure injecture is exhaustive and can add a failure at all possible interleaving points.
*/
machine FailureInjector {
	start state Init {
		entry (participants: seq[machine]){
		    send choose(participants), halt;
		}
	}
}