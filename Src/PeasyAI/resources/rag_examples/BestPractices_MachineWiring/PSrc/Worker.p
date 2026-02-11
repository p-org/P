// ============================================================================
// BEST PRACTICES: Worker with Proper Event Handling
// ============================================================================
//
// Demonstrates:
// 1. Handling broadcast events from coordinator (eNotifyAll)
// 2. Ignoring events in states where they're not relevant
// 3. Proper initialization via constructor payload
// ============================================================================

machine Worker {
    var coordinator: machine;
    var workerId: int;

    start state Init {
        entry InitEntry;
        // BEST PRACTICE: Ignore broadcast events that may arrive during init.
        ignore eNotifyAll;
    }

    state Working {
        on eRequest do HandleRequest;
        // BEST PRACTICE: Handle broadcast events from coordinator.
        // Even if the Worker doesn't need to act on the notification,
        // it MUST either handle or ignore it to prevent UnhandledEventException.
        on eNotifyAll do HandleNotification;
    }

    state Idle {
        on eRequest do HandleRequest;
        // BEST PRACTICE: Consistent event handling across all states.
        ignore eNotifyAll;
    }

    fun InitEntry(config: tWorkerConfig) {
        coordinator = config.coordinator;
        workerId = config.workerId;
        goto Idle;
    }

    fun HandleRequest(req: (sender: machine, data: int)) {
        // Process work and send response back
        send coordinator, eResponse, (receiver = this, result = req.data * 2);
        goto Working;
    }

    fun HandleNotification(notif: (value: int)) {
        // React to coordinator broadcast
        goto Idle;
    }
}
