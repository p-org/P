Programmers can write safety and liveness specifications in P as monitors or `spec` machines.
`spec` machines are monitor state machines or observer state machines that observe a set of events during the execution of the system and
assert the desired correctness specifications based on these observations.

!!! info "Machines vs Spec Machine"

    Syntactically, machines and spec machines in P **are very similar in terms of the state machine structure**. But, they have some key differences:

    - `spec` machines in P are **observer machines** (imagine runtime monitors), they observe a set of events in the execution of the system and based on these observed events (may keep track on local state) assert the desired global safety and liveness specifications.
    - Since `spec` machines are observer machines, they **cannot have any side effects** on the system behavior and hence, `spec` machines cannot perform `send`, `receive`, `new`, and `annouce`.
    - `spec` machines are **global machines**, in other words, there is only a single instance of each monitor created at the start of the execution of the system. We currently do not support dynamic creation of monitors. Hence, `spec` machines cannot use `this` expression.
    - `spec` machines are **synchronously composed** with the system that it is monitored. The way this is achieved is: each time there is a `send` or `announce` of an event during the execution of a system, all the monitors or specifications that are observing that event are executed synchronously at that point. Another way to imagine this is: just before `send` or `annouce` of an event, we deliver this event to all the monitors that are observing the event and **synchronously** execute the monitor at that point.
    - Finally, `spec` machines can have `hot` and `cold` annotations on their states to model liveness specifications.

???+ note "P Spec Machine Grammar"

    `specMachineDecl : spec iden observes eventsList statemachineBody ;`

    As mentioned above, syntactically, the P `spec` machines are very similar to the [P state machines](statemachines.md). The main difference being the `observes` annotation that specifies the list of events being observed (monitored) and also the `hot` and `cold` annotations on the states of a liveness specification.

**Syntax:** `spec iden observes eventsList statemachineBody ;`

`iden` is the name of the spec machine, `eventsList` is the comma separated list of events observed by the spec machine and the `statemachineBody` is the implementation of the specification and its grammar is similar to the [P state machine](statemachines.md).

#### Safety specification

``` kotlin linenums="1"
/*******************************************************************
ReqIdsAreMonotonicallyIncreasing observes the eRequest event and
checks that the payload (Id) associated with the requests sent
by all concurrent clients in the system is always globally
monotonically increasing by 1
*******************************************************************/
spec ReqIdsAreMonotonicallyIncreasing observes eRequest {
    // keep track of the Id in the previous request
    var previousId : int;
    start state Init {
        on eRequest do (req: tRequest) {
            assert req.rId > previousId,
            format ("Request Ids not monotonically increasing, got {0},
            previously seen Id was {1}", req.rId, previousId);
            previousId = req.rId;
        }
    }
}
```

The above specification checks a very simple global invariant that all `eRequest` events that are being sent by clients in the system have a globally monotonically increasing `rId`.

#### Liveness specification

``` kotlin linenums="1" hl_lines="13"
/**************************************************************************
GuaranteedProgress observes the eRequest and eResponse events,
it asserts that every request is always responded by a successful response.
***************************************************************************/
spec GuaranteedProgress observes eRequest, eResponse {
    // keep track of the pending requests
    var pendingReqs: set[int];
    start state NopendingRequests {
        on eRequest goto PendingReqs with (req: tRequest){
            pendingReqs += (req.rId);
        }
    }
    hot state PendingReqs {
        on eResponse do (resp: tResponse) {
            assert resp.rId in pendingReqs,
                format ("unexpected rId: {0} received, expected one of {1}", resp.rId, pendingReqs);
            if(resp.status == SUCCESS)
            {
                pendingReqs -= (resp.rId);
                if(sizeof(pendingReqs) == 0) // requests already responded
                    goto NopendingRequests;
            }
        }
        on eRequest goto PendingReqs with (req: tRequest){
            pendingReqs += (req.rId);
        }
    }
}
```

The above specification checks the global liveness property that every event `eRequest` is eventually followed by a corresponding successful `eResponse` event. The key idea is that the system satisfies a liveness specification if at the end of the execution the monitor is not in a **hot** state (line 13). The programmers can use `hot` annotation on states to mark them as intermediate or error states. Hence, properties like `eventually something holds` or `every event X is eventually followed by Y` or `eventually the system enters a convergence state`, all such properties can be specified by marking the intermediate state as `hot` states and the checker checks that all the executions of the system eventually end in a non-hot state. If there exists an execution that fails to come out of a hot state eventually then it is flagged as a potential liveness violation.

Details about the importance of liveness specifications is described [here](../advanced/importanceliveness.md). For several examples of liveness properties, please check the specifications in the tutorial examples.

