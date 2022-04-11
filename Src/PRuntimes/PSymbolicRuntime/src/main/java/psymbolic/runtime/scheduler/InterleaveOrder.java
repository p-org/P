package psymbolic.runtime.scheduler;

import psymbolic.runtime.Event;
import psymbolic.runtime.Message;
import psymbolic.runtime.machine.*;
import psymbolic.valuesummary.*;

public class InterleaveOrder implements MessageOrder {

    private InterleaveOrder() {}

    public static InterleaveOrder getInstance() { return new InterleaveOrder(); }

    public Guard lessThan (Message m0, Message m1) {
        PrimitiveVS<Event> e0 = m0.getEvent();
        PrimitiveVS<Event> e1 = m1.getEvent();
        PrimitiveVS<Event> readResp = new PrimitiveVS<>(new Event("eReadResp"));
        PrimitiveVS<Event> readReq = new PrimitiveVS<>(new Event("eReadIndexReq"));
        PrimitiveVS<Event> update = new PrimitiveVS<>(new Event("eUpdateIndexReq"));
        PrimitiveVS<Event> acquire = new PrimitiveVS<>(new Event("eAcquireLock"));
        PrimitiveVS<Event> release = new PrimitiveVS<>(new Event("eReleaseLock"));
        PrimitiveVS<Event> prepare = new PrimitiveVS<>(new Event("ePrepareReq"));
        PrimitiveVS<Event> prepareResponse = new PrimitiveVS<>(new Event("ePrepareResp"));

        Guard readRespCond0 = e0.symbolicEquals(readResp, e0.getUniverse()).getGuardFor(true);
        Guard readRespCond1 = e1.symbolicEquals(readResp, e1.getUniverse()).getGuardFor(true);
        Guard readReqCond0 = e0.symbolicEquals(readReq, e0.getUniverse()).getGuardFor(true);
        Guard readReqCond1 = e1.symbolicEquals(readReq, e1.getUniverse()).getGuardFor(true);

        // paxos
        PrimitiveVS<Event> writeResp = new PrimitiveVS<>(new Event("writeResp"));
        PrimitiveVS<Event> write = new PrimitiveVS<>(new Event("write"));
        PrimitiveVS<Event> accepted = new PrimitiveVS<>(new Event("accepted"));
        PrimitiveVS<Event> accept = new PrimitiveVS<>(new Event("accept"));
        PrimitiveVS<Event> read = new PrimitiveVS<>(new Event("read"));
        PrimitiveVS<Event> paxosReadResp = new PrimitiveVS<>(new Event("readResp"));
        PrimitiveVS<Event> agree = new PrimitiveVS<>(new Event("agree"));
        PrimitiveVS<Event> reject = new PrimitiveVS<>(new Event("reject"));
        PrimitiveVS<Event> paxosPrepare = new PrimitiveVS<>(new Event("prepare"));
        Guard writeRespCond0 = e0.symbolicEquals(writeResp, e0.getUniverse()).getGuardFor(true);
        Guard writeRespCond1 = e1.symbolicEquals(writeResp, e1.getUniverse()).getGuardFor(true);
        Guard writeCond0 = e0.symbolicEquals(write, e0.getUniverse()).getGuardFor(true);
        Guard writeCond1 = e1.symbolicEquals(write, e1.getUniverse()).getGuardFor(true);
        Guard acceptedCond0 = e0.symbolicEquals(accepted, e0.getUniverse()).getGuardFor(true);
        Guard acceptedCond1 = e1.symbolicEquals(accepted, e1.getUniverse()).getGuardFor(true);
        Guard acceptCond0 = e0.symbolicEquals(accept, e0.getUniverse()).getGuardFor(true);
        Guard acceptCond1 = e1.symbolicEquals(accept, e1.getUniverse()).getGuardFor(true);
        Guard readCond0 = e0.symbolicEquals(read, e0.getUniverse()).getGuardFor(true);
        Guard readCond1 = e1.symbolicEquals(read, e1.getUniverse()).getGuardFor(true);
        Guard pReadRespCond0 = e0.symbolicEquals(paxosReadResp, e0.getUniverse()).getGuardFor(true);
        Guard pReadRespCond1 = e1.symbolicEquals(paxosReadResp, e1.getUniverse()).getGuardFor(true);
        Guard agreeCond0 = e0.symbolicEquals(agree, e0.getUniverse()).getGuardFor(true);
        Guard agreeCond1 = e0.symbolicEquals(agree, e1.getUniverse()).getGuardFor(true);
        Guard rejectCond0 = e0.symbolicEquals(reject, e0.getUniverse()).getGuardFor(true);
        Guard rejectCond1 = e0.symbolicEquals(reject, e1.getUniverse()).getGuardFor(true);
        Guard pPrepareCond0 = e0.symbolicEquals(paxosPrepare, e0.getUniverse()).getGuardFor(true);
        Guard pPrepareCond1 = e0.symbolicEquals(paxosPrepare, e1.getUniverse()).getGuardFor(true);
        Guard paxosCond = writeRespCond0.and(writeRespCond1).or
                          (writeRespCond0.and(writeCond1)).or
                          (agreeCond0.and(writeCond1)).or(acceptedCond0.and(writeCond1)).or
                          (rejectCond0.and(writeCond1)).or(acceptCond0.and(writeCond1)).or
                          (pPrepareCond0.and(writeCond1)).or
                          (writeCond0.and(agreeCond1)).or(writeCond0.and(acceptedCond0)).or
                          (writeCond0.and(rejectCond1)).or(writeCond0.and(acceptCond1)).or
                          (agreeCond0.and(writeRespCond1)).or(acceptedCond0.and(writeRespCond1)).or
                          (rejectCond0.and(writeRespCond1)).or(acceptCond0.and(writeRespCond1)).or
                          (pPrepareCond0.and(writeRespCond1)).or
                          (agreeCond0.and(pReadRespCond1)).or(acceptedCond0.and(pReadRespCond1)).or
                          (rejectCond0.and(pReadRespCond1)).or(acceptCond0.and(pReadRespCond1)).or
                          (pPrepareCond0.and(pReadRespCond1));

        Guard bothReads = readReqCond0.and(readReqCond1);
        Guard readAtLessElement = Guard.constFalse();
        for (GuardedValue<Machine> gv0 : m0.getTarget().getGuardedValues()) {
            for (GuardedValue<Machine> gv1 : m1.getTarget().getGuardedValues()) {
                if (gv0.getValue().getInstanceId() < gv1.getValue().getInstanceId()) {
                    readAtLessElement = readAtLessElement.or(gv0.getGuard().and(gv1.getGuard()));
                }
            }
        }
        readAtLessElement = readAtLessElement.and(bothReads);

        Guard updateCond0 = e0.symbolicEquals(update, e0.getUniverse()).getGuardFor(true);
        Guard updateCond1 = e1.symbolicEquals(update, e1.getUniverse()).getGuardFor(true);
        Guard acquireCond0 = e0.symbolicEquals(acquire, e0.getUniverse()).getGuardFor(true);
        Guard acquireCond1 = e1.symbolicEquals(acquire, e1.getUniverse()).getGuardFor(true);
        Guard releaseCond0 = e0.symbolicEquals(release, e0.getUniverse()).getGuardFor(true);
        Guard releaseCond1 = e1.symbolicEquals(release, e1.getUniverse()).getGuardFor(true);
        Guard prepareCond0 = e0.symbolicEquals(prepare, e0.getUniverse()).getGuardFor(true);
        Guard prepareCond1 = e1.symbolicEquals(prepare, e1.getUniverse()).getGuardFor(true);
        Guard prepareResponseCond0 = e0.symbolicEquals(prepareResponse, e0.getUniverse()).getGuardFor(true);
        Guard prepareResponseCond1 = e1.symbolicEquals(prepareResponse, e1.getUniverse()).getGuardFor(true);
        return acquireCond0.and(releaseCond1).or(acquireCond1.and(releaseCond0)).or(
               prepareCond0.and(prepareResponseCond1).or(prepareCond1.and(prepareResponseCond0))).or(
               readRespCond0.and(readRespCond1.or(readReqCond1))).or(updateCond0.and(updateCond1)).or(readAtLessElement).or(paxosCond);
               //readRespCond0.and(readRespCond1.or(updateCond1).or(readReqCond1))).or(updateCond0.and(updateCond1));
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
