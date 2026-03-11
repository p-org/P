// ============================================================================
// BEST PRACTICES: Types, Events, and Setup Events
// ============================================================================
//
// This file demonstrates best practices for defining types, events, and
// setup/configuration events in P programs.
//
// KEY PRINCIPLES:
// 1. Separate protocol events from setup events
// 2. Use setup events for post-creation machine wiring
// 3. Define clear payload types for each event
// ============================================================================

// --- Protocol events (used during normal operation) ---
event eRequest: (sender: machine, data: int);
event eResponse: (receiver: machine, result: int);
event eNotifyAll: (value: int);

// --- Setup/configuration events (used ONLY during initialization) ---
// BEST PRACTICE: Define dedicated setup events for post-creation wiring.
// NEVER reuse protocol events for setup purposes.
//
// Common patterns:
// 1. Broadcast list setup:  event eSetupComponents: seq[machine]
// 2. Back-reference setup:  event eSetupOwner: machine
// 3. Peer list setup:       event eSetupPeers: seq[machine]
// 4. Full config setup:     event eSetupConfig: tConfigType

event eSetupComponentList: seq[machine];
event eSetupCoordinatorRef: machine;

// --- Configuration types ---
type tWorkerConfig = (coordinator: machine, workerId: int);
type tCoordinatorConfig = (numWorkers: int);
