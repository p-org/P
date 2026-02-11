// ============================================================================
// BEST PRACTICES: Test Driver with Proper Machine Wiring
// ============================================================================
//
// This test driver demonstrates the correct pattern for initializing a system
// with circular dependencies (Coordinator needs component list, Workers need
// Coordinator reference).
//
// KEY PRINCIPLES:
// 1. Create machines in dependency order
// 2. Use setup events for post-creation wiring
// 3. NEVER use protocol events for initialization
// 4. Build complete component lists AFTER all machines exist
// 5. Keep scenario machines minimal — just configuration
// ============================================================================

machine TestSetup {
    start state Init {
        entry {
            var coordinator: machine;
            var workers: seq[machine];
            var allComponents: seq[machine];
            var i: int;
            var worker: machine;

            // STEP 1: Create the Coordinator first (with just config).
            // Can't pass allComponents yet — Workers don't exist.
            coordinator = new Coordinator((numWorkers = 3));
            allComponents += (0, coordinator);

            // STEP 2: Create Workers (they get the Coordinator reference at creation).
            i = 0;
            while (i < 3) {
                worker = new Worker((coordinator = coordinator, workerId = i));
                workers += (i, worker);
                allComponents += (sizeof(allComponents), worker);
                i = i + 1;
            }

            // STEP 3: CRITICAL — Send setup event with COMPLETE component list.
            // Now that all machines exist, wire the Coordinator with the full list.
            // This uses the dedicated eSetupComponentList event, NOT a protocol event.
            send coordinator, eSetupComponentList, allComponents;

            // STEP 4: Trigger the actual test scenario.
            send coordinator, eRequest, (sender = coordinator, data = 42);
        }
    }
}

// ============================================================================
// ANTI-PATTERN: What NOT to do
// ============================================================================
//
// DON'T do this:
//   send coordinator, eNotifyAll, (value = 0);  // Misusing protocol event for setup!
//
// The eNotifyAll event is a protocol event meant for broadcasting during operation.
// Using it to pass initialization data is a SEMANTIC MISMATCH that causes:
// 1. UnhandledEventException if the receiver state doesn't expect it
// 2. Safety specs observing eNotifyAll may see spurious initialization events
// 3. Confusing test traces that mix setup with protocol behavior
//
// INSTEAD, define a dedicated setup event (eSetupComponentList) and use it.
// ============================================================================
