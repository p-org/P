type tTime = int;
type tGlobalQuery = (localClock: LocalClock);
type tGlobalResponse = (target: LocalClock, trueTime: tTime);
type tLocalResponse = (target: Client, trueTime: tTime, earliest: tTime, latest: tTime);

event eGlobalQuery: tGlobalQuery;
event eLocalQuery;
event eGlobalResponse: tGlobalResponse;
event eLocalResponse: tLocalResponse;

machine GlobalClock {
    var time: tTime;

    start state Init {
        entry {
            time = 0;
            goto Serving;
        }
    }

    state Serving {
        on eGlobalQuery do (payload: tGlobalQuery) {
            time = time + choose(10) + 1;
            send payload.localClock, eGlobalResponse, (target=payload.localClock, trueTime=time);
        }
    }
}

machine LocalClock {
    var currEarlyBound: tTime;
    var currLateBound: tTime;
    var maxUncertainty: tTime;
    var globalClock: GlobalClock;
    var client: Client;

    start state Init {
        entry (setup: (clock: GlobalClock, client: Client)) {
            currEarlyBound = 0;
            currLateBound = 0;
            maxUncertainty = 5;
            globalClock = setup.clock;
            client = setup.client;
            goto Serving;
        }
    }

    state Serving {
        on eLocalQuery do {
            send globalClock, eGlobalQuery, (localClock=this,);
        }

        on eGlobalResponse do (payload: tGlobalResponse) {
            var early: tTime;
            var late: tTime;
            early = currEarlyBound + choose(payload.trueTime - currEarlyBound + 1);
            late = payload.trueTime + choose(maxUncertainty);
            send client, eLocalResponse, (target=client, trueTime=payload.trueTime, earliest=early, latest=late);
            currEarlyBound = early;
            currLateBound = late;
        }
    }
}

machine Client {
    var localClock: LocalClock;
    var num_requests: int;

    start state Init {
        entry (setup: (globalClock: GlobalClock, n: int)) {
            localClock = new LocalClock((clock=setup.globalClock, client=this));
            num_requests = setup.n;
            goto Requesting;
        }
    }

    state Requesting {
        entry {
            send localClock, eLocalQuery;
            num_requests = num_requests - 1;
        }

        on eLocalResponse do (payload: tLocalResponse) {
            assert(payload.earliest <= payload.latest);
            if (num_requests > 0) {
                goto Requesting;
            }
            goto Done;
        }
    }

    state Done {
        ignore eLocalResponse;
    }
}

spec Correctness observes eLocalResponse {
    var responses: seq[tLocalResponse];

    start state Init {
        entry {
            responses = default(seq[tLocalResponse]);
            goto Observing;
        }
    }

    state Observing {
        on eLocalResponse do (payload: tLocalResponse) {
            var resp: tLocalResponse;
            // check invariant 1
            foreach (resp in responses) {
                if (payload.target == resp.target) {
                    print format("{0} vs {1}", payload, resp);
                    if (payload.trueTime > resp.trueTime) {
                        assert (payload.earliest >= resp.earliest);
                    } else {
                        assert (resp.earliest >= payload.earliest);
                    }
                }
            }
            // check invariant 2
            foreach (resp in responses) {
                if (payload.trueTime > resp.trueTime) {
                    assert (payload.latest > resp.earliest);
                } else {
                    assert (resp.latest > payload.earliest);
                }
            }
            responses += (sizeof(responses), payload);
        }
    }
}

module ClockBound = {GlobalClock, LocalClock, Client};