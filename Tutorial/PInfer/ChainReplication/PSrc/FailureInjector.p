/*
The failure injector machine randomly selects a replica machine and enqueues the special event "halt".
*/

event eDelayNodeFailure;
// event: event sent by the failure injector to shutdown a node
event eShutDown: Replicate;
event eStartInjectFailure;

machine FailureInjector {
    var nFailures: int;
    var nodes: set[Replicate];

    start state Init {
        entry (config: (nodes: set[Replicate], nFailures: int)) {
            nFailures = config.nFailures;
            nodes = config.nodes;
            assert nFailures <= sizeof(nodes);
        }
        
        on eStartInjectFailure do {
            goto FailOneNode;
        }
    }

    state FailOneNode {
        entry {
            var fail: Replicate;
            
            if (nFailures == 0) {
                raise halt;
            } else {
                if($) {
                    fail = choose(nodes);
                    send fail, eShutDown, fail;
                    nodes -= (fail);
                    nFailures = nFailures - 1;
                } else {
                    send this, eDelayNodeFailure;
                }
            }
        }

        on eDelayNodeFailure goto FailOneNode;
    }
}

