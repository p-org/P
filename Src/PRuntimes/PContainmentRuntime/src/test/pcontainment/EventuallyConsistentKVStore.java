package pcontainment;

import com.microsoft.z3.*;
import org.junit.jupiter.api.Test;
import p.runtime.values.PInt;
import pcontainment.runtime.Event;
import pcontainment.runtime.Message;
import pcontainment.runtime.Payloads;
import pcontainment.runtime.machine.Locals;
import pcontainment.runtime.machine.Machine;
import pcontainment.runtime.machine.State;
import pcontainment.runtime.machine.SymbolicMachineIdentifier;
import pcontainment.runtime.machine.eventhandlers.EventHandler;
import pcontainment.runtime.machine.eventhandlers.EventHandlerReturnReason;

import java.time.Duration;
import java.time.Instant;
import java.util.*;

public class EventuallyConsistentKVStore {

    static Event eReadRequest = new Event("eReadRequest");
    static Event eReadResponse = new Event("eReadResponse");
    static Event eWriteRequest = new Event("eWriteRequest");
    static Event eWriteResponse = new Event("eWriteResponse");

    public static int SUCCESS = 0;
    public static int TIMEOUT = 1;
    public static int FAILURE = 2;

    public static final State kvStoreInit = new State("init") {};

    static class kvStoreInit extends EventHandler {

        public kvStoreInit(Event eventType, State state) { super(eventType, state); }

        @Override
        public Map<BoolExpr, Triple<Integer, Locals, EventHandlerReturnReason>> getEncoding(int sends, Locals locals,
                                                                                            Machine target, Payloads payloads) {
            Checker c = target.getChecker();
            int numSends = sends;
            target.setStarted(true);
            BoolExpr body = c.mkBool(true);
            c.declLocal("kvStore", c.mkMap());
            c.declLocal("keys", c.mkSeq());
            c.declLocal("pending_keys", c.mkSeq());
            c.declLocal("pending_vals", c.mkSeq());
            c.declLocal("pending_length", c.mkInt(0));
            Map<BoolExpr, Triple<Integer, Locals, EventHandlerReturnReason>> retMap = new HashMap<>();
            retMap.put(body, new Triple<>(numSends, locals, new EventHandlerReturnReason.NormalReturn()));
            return retMap;
        }
    }

    static class storeHandler extends EventHandler {
        public storeHandler(Event eventType, State state) { super(eventType, state); }

        @Override
        public Map<BoolExpr, Triple<Integer, Locals, EventHandlerReturnReason>> getEncoding(int sends, Locals locals,
                                                                                            Machine target,
                                                                                            Payloads payloads) {
            Checker c = target.getChecker();
            Map<BoolExpr, Triple<Integer, Locals, EventHandlerReturnReason>> retMap = new HashMap<>();
            // success
            int successSends = sends;
            // add to pending
            Locals updatedLocals = locals.immutableAssign("pending_keys",
                    c.mkAdd((SeqExpr<IntSort>) locals.get("pending_keys"), (IntExpr) payloads.get("key")));
            updatedLocals  = updatedLocals.immutableAssign("pending_vals",
                    c.mkAdd((SeqExpr<IntSort>) locals.get("pending_vals"), (IntExpr) payloads.get("val")));
            updatedLocals = locals.immutableAssign("pending_length",
                            c.mkPlus((IntExpr) updatedLocals.get("pending_length"), c.mkInt(1)));
            Message success = new Message(eWriteResponse,
                            new SymbolicMachineIdentifier((IntExpr) payloads.get("client")),
                            new Payloads("status", c.mkInt(SUCCESS)));
            retMap.put(c.send(successSends++, success), new Triple<>(successSends, updatedLocals, new EventHandlerReturnReason.NormalReturn()));
            // failure
            int failureSends = sends;
            Message failure = new Message(eWriteResponse,
                    new SymbolicMachineIdentifier((IntExpr) payloads.get("client")),
                    new Payloads("status", c.mkInt(FAILURE)));
            retMap.put(c.send(failureSends++, failure), new Triple<>(failureSends, locals, new EventHandlerReturnReason.NormalReturn()));
            // timeout
            int timeoutSends = sends;
            Message timeout = new Message(eWriteResponse,
                    new SymbolicMachineIdentifier((IntExpr) payloads.get("client")),
                    new Payloads("status", c.mkInt(TIMEOUT)));
            retMap.put(c.send(timeoutSends++, timeout), new Triple<>(timeoutSends, locals, new EventHandlerReturnReason.NormalReturn()));
            return retMap;
        }
    }

    static class readHandler extends EventHandler {
        public readHandler(Event eventType, State state) { super(eventType, state); }

        @Override
        public Map<BoolExpr, Triple<Integer, Locals, EventHandlerReturnReason>> getEncoding(int sends, Locals locals,
                                                                                            Machine target,
                                                                                            Payloads payloads) {
            Checker c = target.getChecker();
            Map<BoolExpr, Triple<Integer, Locals, EventHandlerReturnReason>> retMap = new HashMap<>();
            String localNameForKey = "kvStore_key=" + payloads.get("key");
            String localNameForKeyExists = "kvStore_key=" + payloads.get("key") + "_exists";
            List<BoolExpr> branches = new ArrayList<>();
            Locals updatedLocals = locals;
            // nondet update
            BoolExpr updateCond = c.mkAnd(c.mkGt((IntExpr) updatedLocals.get("pending_length"), c.mkInt(0)),
                                            c.mkFreshBoolConst());
            IntExpr tmp = c.mkFreshIntConst();
            BoolExpr tmpCond = c.mkAnd(c.mkOr(c.mkGt(tmp, c.mkInt(0)), c.mkEq(tmp, c.mkInt(0))),
                                c.mkGt((IntExpr) updatedLocals.get("pending_length"), tmp));
            IntExpr key = (IntExpr) c.mkGet((SeqExpr<IntSort>) updatedLocals.get("pending_keys"), tmp);
            IntExpr val = (IntExpr) c.mkGet((SeqExpr<IntSort>) updatedLocals.get("pending_vals"), tmp);
            // remove pending!!
            updatedLocals = updatedLocals.immutableAssign("pending_keys",
                                c.mkSubseq((SeqExpr<?>) updatedLocals.get("pending_keys"),
                                        (IntExpr) c.mkPlus(tmp, c.mkInt(1))));
            updatedLocals = updatedLocals.immutableAssign("pending_vals",
                    c.mkSubseq((SeqExpr<?>) updatedLocals.get("pending_vals"),
                            (IntExpr) c.mkPlus(tmp, c.mkInt(1))));
            // update map
            updatedLocals = updatedLocals.immutableAssign("kvStore",
                    c.mkAdd((ArrayExpr<IntSort, IntSort>) updatedLocals.get("kvStore"), key, val));
            updatedLocals = updatedLocals.immutableAssign("keys",
                    c.mkAdd((SeqExpr<IntSort>) updatedLocals.get("keys"), key));
            // timeout
            branches.add(c.send(sends, new Message(eReadResponse,
                                new SymbolicMachineIdentifier((IntExpr) payloads.get("client")),
                                new Payloads("val", c.mkInt(0), "status", c.mkInt(TIMEOUT)))));
            // no timeout
            BoolExpr containsKey = c.mkContains((SeqExpr<IntSort>) updatedLocals.get("keys"), key);
            branches.add(c.mkAnd(containsKey, c.send(sends, new Message(eReadResponse,
                    new SymbolicMachineIdentifier((IntExpr) payloads.get("client")),
                    new Payloads("val", c.mkGet(((ArrayExpr<IntSort, IntSort>) updatedLocals.get("kvStore")), key), "status", c.mkInt(SUCCESS))))));
            branches.add(c.mkAnd(c.mkNot(containsKey), c.send(sends, new Message(eReadResponse,
                    new SymbolicMachineIdentifier((IntExpr) payloads.get("client")),
                    new Payloads("val", c.mkInt(0), "status", c.mkInt(FAILURE))))));
            sends++;
            retMap.put(c.mkBool(true), new Triple<>(sends, updatedLocals,
                    new EventHandlerReturnReason.NormalReturn()));
            return retMap;
        }
    }

    private static final List<State> kvStoreStates = Arrays.asList(kvStoreInit);
    private static final Map<Event, List<EventHandler>> kvStoreHandlers = new HashMap<>();

    static class KVStore extends Machine {
        public KVStore(String name, int instanceId) {
            super(name, instanceId, kvStoreInit, kvStoreStates, kvStoreHandlers);
            this.addHandler(Event.createMachine, new kvStoreInit(Event.createMachine, kvStoreInit));
            this.addHandler(eWriteRequest, new storeHandler(eWriteRequest, kvStoreInit));
            this.addHandler(eReadRequest, new readHandler(eReadRequest, kvStoreInit));
        }
    }

    private static final List<State> clientStates = new ArrayList<>();
    private static final Map<Event, List<EventHandler>> clientHandlers = new HashMap<>();
    public static final State clientInit = new State("init") {};

    static class Client extends Machine {
        public Client(String name, int instanceId) {
            super(name, instanceId, clientInit, clientStates, clientHandlers);
        }
    }

    public static Random r = new Random();
    public static Map<PInt, PInt> map = new HashMap<>();

    public static void doTransaction(List<Message> trace, Client client,  KVStore kvStore) {
        if (r.nextBoolean()) {
            PInt key = new PInt(r.nextInt());
            PInt val = new PInt(r.nextInt());
            // write
            trace.add(new Message(eWriteRequest, kvStore.getId(),
                    new Payloads("client", client.getId(), "key", key, "val", val)));
            if (r.nextBoolean()) {
                // success
                map.put(key, val);
                trace.add(new Message(eWriteResponse, client.getId(),
                        new Payloads("status", new PInt(SUCCESS))));
            } else {
                // failure
                trace.add(new Message(eWriteResponse, client.getId(),
                        new Payloads("status", new PInt(FAILURE))));
            }
        } else {
            // read
            if (r.nextBoolean() && map.size() > 0) {
                // success
                List<Map.Entry<PInt, PInt>> entries = new ArrayList<>(map.entrySet());
                Map.Entry<PInt, PInt> entry = entries.get(r.nextInt(entries.size()));
                trace.add(new Message(eReadRequest, kvStore.getId(),
                        new Payloads("key", entry.getKey(), "client", client.getId())));
                trace.add(new Message(eReadResponse, client.getId(),
                        new Payloads("val", entry.getValue(), "status", new PInt(SUCCESS))));
            } else {
                // failure
                trace.add(new Message(eReadRequest, kvStore.getId(),
                        new Payloads("key", new PInt(r.nextInt()), "client", client.getId())));
                trace.add(new Message(eReadResponse, client.getId(),
                        new Payloads("val", new PInt(-1), "status", new PInt(FAILURE))));
            }
        }
    }

    public static List<Message> getTrace(int transactions, Client client,  KVStore kvStore) {
        List<Message> trace = new ArrayList<>();
        trace.add(new Message(Event.createMachine, kvStore.getId(), new Payloads()));
        for (int i = 0; i < transactions; i++) {
            doTransaction(trace, client, kvStore);
        }
        return trace;
    }

    public void query(Machine m) {
        Instant queryStartTime = Instant.now();
        m.check();
        Instant queryEndTime = Instant.now();
        Duration duration = Duration.between(queryStartTime, queryEndTime);
        System.out.println("Query solved in " + duration.getSeconds() + "s");
    }

    @Test
    public void testKVStore() {
        KVStore kvStore = new KVStore("store", 0);
        Client client = new Client("clt", 0);
        Instant startTime = Instant.now();
        List<Message> trace = getTrace(100, client, kvStore);
        for (Message msg : trace) {
            kvStore.observeMessage(msg);
            if (!msg.getTargetId().equals(kvStore.getId())) {
                kvStore.encode();
                query(kvStore);
            }
        }
        Duration duration = Duration.between(startTime, Instant.now());
        System.out.println("Solved in " + duration.getSeconds() + "s");
    }
}
