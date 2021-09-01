/*
The failure injector machine randomly selects a participant machine and enqueues a special event "halt"
On dequeueing a halt event, the P machine destroyes itself safely.
This is one way of modeling node failures in P.
Note that as the model-checker explores all possible interleavings. The failure injecture is exhaustive and can add a failure at all possible interleaving points.
*/
machine FailureInjector {
	start state Init {
		entry (config: (participants: set[Participant], nFailures: int)) {
            var fail: Participant;
            assert config.nFailures < sizeof(config.participants);

            while(config.nFailures > 0)
            {
                fail = choose(config.participants);
                send fail, halt;
                config.participants -= (fail);
                config.nFailures = config.nFailures - 1;
            }
		}
	}
}