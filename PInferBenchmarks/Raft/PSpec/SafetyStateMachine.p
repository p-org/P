event eEntryApplied: (logIndex: LogIndex, log: tServerLog);

spec SafetyStateMachine observes eEntryApplied {
    var appliedEntries: map[int, tServerLog];
    start state MonitorEntryApplied {
        entry {
            appliedEntries = default(map[int, tServerLog]);
        }

        on eEntryApplied do (payload: (logIndex: int, log: tServerLog)) {
            if (!(payload.logIndex in keys(appliedEntries))) {
                appliedEntries[payload.logIndex] = payload.log;
            }
            assert appliedEntries[payload.logIndex] == payload.log,
                format("Entry@{0}={1} applied before does not match with {2}", payload.logIndex, appliedEntries[payload.logIndex], payload.log);
        }
    }
} 