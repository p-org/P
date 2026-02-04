// Define custom types
type tLogEventEventPairs = (timestamp: int, key: int, eventNumber: int, status: string);
type tEntriesFinishedEventPairs = (timestamp: int, key: int);

// Define events
event eEventStartEventPairs : tLogEventEventPairs;
event eEventEndEventPairs : tLogEventEventPairs;
event eEntriesFinishedEventPairs : tEntriesFinishedEventPairs; // Updated event type

spec EventPairs observes eEventStartEventPairs, eEventEndEventPairs, eEntriesFinishedEventPairs {
    var eventStatus : map[int, map[int, string]]; // Nested map to track status by key and event number
    

    start state InitialState {
        on eEventStartEventPairs do ValidateStart;
        on eEventEndEventPairs do ValidateEnd;
        on eEntriesFinishedEventPairs do ValidateFinalCount;
    }

    fun ValidateStart(logEntry: tLogEventEventPairs) {
        var tempMap: map[int, string];

        if (!(logEntry.key in keys(eventStatus))) {
            tempMap[logEntry.eventNumber] = "start";
            eventStatus[logEntry.key] = tempMap;
        }

        else {
            if (logEntry.eventNumber in eventStatus[logEntry.key]) {
                assert false, format("Invalid 'start' status for key {0}, event {1}. Current status: {2}", logEntry.key, logEntry.eventNumber, eventStatus[logEntry.key][logEntry.eventNumber]);
            }
            eventStatus[logEntry.key][logEntry.eventNumber] = "start";
        }
    }

    fun ValidateEnd(logEntry: tLogEventEventPairs) {
        if (!(logEntry.eventNumber in eventStatus[logEntry.key])) {
            assert false, format("Invalid 'end' status for key {0}, event {1}. Event not started", logEntry.key, logEntry.eventNumber);
        } else {
            assert eventStatus[logEntry.key][logEntry.eventNumber] == "start", format("Invalid 'end' status for key {0}, event {1}. Current status: {2}", logEntry.key, logEntry.eventNumber, eventStatus[logEntry.key][logEntry.eventNumber]);
            eventStatus[logEntry.key][logEntry.eventNumber] = "end";

        }
    }

    fun ValidateFinalCount(logEntry: tEntriesFinishedEventPairs) {
        var i: int;
        if (logEntry.key in eventStatus) {
            foreach(i in keys(eventStatus[logEntry.key])) {
                assert eventStatus[logEntry.key][i] == "end", format("For key {0}, event {1} has not ended. Current status: {2}", logEntry.key, i, eventStatus[logEntry.key][i]);
            }
        }
    }
}
