package psymbolic.runtime.scheduler;

import psymbolic.runtime.Event;

import java.util.HashMap;
import java.util.HashSet;
import java.util.Map;
import java.util.Set;

public class InterleaveMap {
    private static Map<Event, Set<Event>> interleaveMap = new HashMap<>();
    private static boolean initialized = false;

    public static Map<Event, Set<Event>> getMap() {
        if (!initialized) {
            Set<Event> acquireSet = new HashSet<>();
            Set<Event> releaseSet = new HashSet<>();
            Set<Event> prepareSet = new HashSet<>();
            Set<Event> prepareResponseSet = new HashSet<>();


            Event acquire = new Event("eAcquireLock");
            Event release = new Event("eReleaseLock");
            Event prepare = new Event("ePrepareReq");
            Event prepareResponse = new Event("ePrepareResp");

            acquireSet.add(release);
            releaseSet.add(acquire);

            prepareSet.add(prepareResponse);
            prepareResponseSet.add(prepare);

            interleaveMap.put(acquire, acquireSet);
            interleaveMap.put(release, releaseSet);
            interleaveMap.put(prepare, prepareSet);
            interleaveMap.put(prepareResponse, prepareResponseSet);

            initialized = true;
        }
        return interleaveMap;
    }
}
