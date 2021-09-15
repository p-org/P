/*
The failure injector machine randomly selects a replica machine and enqueues the special event "halt".
*/
machine FailureInjector {
	start state Init {
		entry (config: (nodes: set[machine], nFailures: int)) {
            var fail: machine;
            assert config.nFailures < sizeof(config.nodes);

            while(config.nFailures > 0)
            {
                fail = choose(config.nodes);
                send fail, halt;
                config.nodes -= (fail);
                config.nFailures = config.nFailures - 1;
            }
		}
	}
}

// function to create the failure injection
fun CreateFailureInjector(config: (nodes: set[machine], nFailures: int)) {
    new FailureInjector(config);
}

// failure injector module
module FailureInjector = { FailureInjector };