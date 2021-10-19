package psymbolic.runtime.scheduler;

import psymbolic.runtime.Event;
import psymbolic.runtime.Message;
import psymbolic.valuesummary.Guard;
import psymbolic.valuesummary.PrimitiveVS;

public class InterleaveOrder implements MessageOrder {

    private InterleaveOrder() {}

    public static InterleaveOrder getInstance() { return new InterleaveOrder(); }

    public Guard lessThan (Message m0, Message m1) {
        PrimitiveVS<Event> e0 = m0.getEvent();
        PrimitiveVS<Event> e1 = m1.getEvent();
        PrimitiveVS<Event> acquire = new PrimitiveVS<>(new Event("eAcquireLock"));
        PrimitiveVS<Event> release = new PrimitiveVS<>(new Event("eReleaseLock"));
        PrimitiveVS<Event> prepare = new PrimitiveVS<>(new Event("ePrepareReq"));
        PrimitiveVS<Event> prepareResponse = new PrimitiveVS<>(new Event("ePrepareResp"));
        Guard acquireCond0 = e0.symbolicEquals(acquire, e0.getUniverse()).getGuardFor(true);
        Guard acquireCond1 = e1.symbolicEquals(acquire, e1.getUniverse()).getGuardFor(true);
        Guard releaseCond0 = e0.symbolicEquals(release, e0.getUniverse()).getGuardFor(true);
        Guard releaseCond1 = e1.symbolicEquals(release, e1.getUniverse()).getGuardFor(true);
        Guard prepareCond0 = e0.symbolicEquals(prepare, e0.getUniverse()).getGuardFor(true);
        Guard prepareCond1 = e1.symbolicEquals(prepare, e1.getUniverse()).getGuardFor(true);
        Guard prepareResponseCond0 = e0.symbolicEquals(prepareResponse, e0.getUniverse()).getGuardFor(true);
        Guard prepareResponseCond1 = e1.symbolicEquals(prepareResponse, e1.getUniverse()).getGuardFor(true);
        return acquireCond0.and(releaseCond1).or(acquireCond1.and(releaseCond0)).or(
               prepareCond0.and(prepareResponseCond1).or(prepareCond1.and(prepareResponseCond0)));
    }

/*
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
            prepareSet.add(prepare);

            prepareResponseSet.add(prepare);

            interleaveMap.put(acquire, acquireSet);
            interleaveMap.put(release, releaseSet);
            interleaveMap.put(prepare, prepareSet);
            interleaveMap.put(prepareResponse, prepareResponseSet);

            initialized = true;
        }
        return interleaveMap;
    }
*/
}
