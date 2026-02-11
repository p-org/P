// ============================================================================
// BEST PRACTICES: Coordinator with Broadcast Pattern
// ============================================================================
//
// Demonstrates:
// 1. How to properly receive a component list via setup event
// 2. How to broadcast to all components
// 3. How to handle/ignore broadcast responses in all states
// ============================================================================

machine Coordinator {
    var workers: seq[machine];
    var allComponents: seq[machine];
    var numWorkers: int;

    start state Init {
        entry InitEntry;
        // BEST PRACTICE: Accept setup events in the start state.
        // These arrive from the test driver after all machines are created.
        on eSetupComponentList do HandleSetupComponents;
        // BEST PRACTICE: Ignore protocol events that may arrive before setup.
        ignore eResponse, eNotifyAll;
    }

    state Ready {
        on eRequest do HandleRequest;
        on eResponse do HandleResponse;
        // BEST PRACTICE: When you broadcast eNotifyAll to all components,
        // you might also be in the component list — handle or ignore your own broadcast.
        ignore eNotifyAll;
    }

    state Done {
        // BEST PRACTICE: Terminal states should ignore all events still in flight.
        ignore eRequest, eResponse, eNotifyAll, eSetupComponentList;
    }

    fun InitEntry(config: tCoordinatorConfig) {
        numWorkers = config.numWorkers;
        // Don't goto Ready yet — wait for setup event with component list.
    }

    fun HandleSetupComponents(components: seq[machine]) {
        allComponents = components;
        goto Ready;
    }

    fun HandleRequest(req: (sender: machine, data: int)) {
        // Process request and notify all components
        BroadcastNotification(req.data);
    }

    fun HandleResponse(resp: (receiver: machine, result: int)) {
        // Process response from worker
    }

    // BEST PRACTICE: Factor broadcast logic into a helper function.
    fun BroadcastNotification(value: int) {
        var i: int;
        i = 0;
        while (i < sizeof(allComponents)) {
            send allComponents[i], eNotifyAll, (value = value);
            i = i + 1;
        }
    }
}
