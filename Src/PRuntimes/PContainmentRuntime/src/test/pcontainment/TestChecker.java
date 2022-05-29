package pcontainment;

import com.microsoft.z3.BoolExpr;
import com.microsoft.z3.Expr;
import com.microsoft.z3.IntExpr;
import org.junit.jupiter.api.Test;
import p.runtime.values.PInt;
import pcontainment.runtime.*;
import pcontainment.runtime.Message;
import pcontainment.runtime.machine.*;
import pcontainment.runtime.machine.eventhandlers.EventHandler;
import pcontainment.runtime.machine.eventhandlers.EventHandlerReturnReason;

import java.time.Duration;
import java.time.Instant;
import java.util.*;

public class TestChecker {

    public static int branchFactor = 2;
    public static int iterations = 1;

    public static Event add = new Event("Add");
    public static Event query = new Event("Query");
    public static Event sum = new Event("Sum");

    private static final State add1 = new State("add1") {};
    private static final State add2 = new State("add2") {};
    private static final List<State> adderStates = Arrays.asList(add1, add2);
    private static final Map<Event, List<EventHandler>> handlers = new HashMap<>();

    static class Adder extends Machine {

        public Adder(String name, int instanceId) {
            super(name, instanceId, add1, adderStates, handlers);
            this.addHandler(Event.createMachine, new Init(Event.createMachine, add1));
            this.addHandler(add, new Add1Handler(add, add1));
            this.addHandler(add, new Add2Handler(add, add2));
            this.addHandler(query, new QueryAdd1(query, add1));
            this.addHandler(query, new QueryAdd2(query, add2));
            getChecker().declLocal("sum", new PInt(0));
        };

        //public IntExpr getsumAtDepth(int depth) {
        //    return getChecker().mkIntConst("sum_" + depth);
        //}

    };

    static class Client extends Machine {

        public Client(String name, int instanceId) {
            super(name, instanceId, null, null, null);
        };

    };

    static class Init extends EventHandler {

        public Init(Event eventType, State state) { super(eventType, state); }

        @Override
        public Map<BoolExpr, Triple<Integer, Locals, EventHandlerReturnReason>> getEncoding(int sends, Locals locals,
                                                                                  Machine target, Payloads payloads) {
            Checker c = target.getChecker();
            Adder castTarget = (Adder) target;
            int numSends = sends;
            target.setStarted(true);
            Map<BoolExpr, Triple<Integer, Locals, EventHandlerReturnReason>> retMap = new HashMap<>();
            IntExpr tmp = c.mkFreshIntConst();
            Locals retLocals = locals.immutablePut("sum", tmp);
            BoolExpr body = c.mkEq(tmp, c.mkInt(0));
            EventHandlerReturnReason ret0 = new EventHandlerReturnReason.NormalReturn();
            retMap.put(body, new Triple<>(numSends, retLocals, ret0));
            return retMap;
        }
    }

    static class Add1Handler extends EventHandler {
        public Add1Handler(Event eventType, State state) {
            super(eventType, state);
        }

        @Override
        public Map<BoolExpr, Triple<Integer, Locals, EventHandlerReturnReason>> getEncoding(int sends, Locals locals,
                                                                                  Machine target, Payloads payloads) {
            Checker c = target.getChecker();
            int numSends = sends;
            Map<BoolExpr, Triple<Integer, Locals, EventHandlerReturnReason>> retMap = new HashMap<>();
            for (int i = 0; i < branchFactor; i++) {
                IntExpr tmp = c.mkFreshIntConst();
                BoolExpr body = c.mkEq(tmp,
                        c.mkPlus(c.mkInt(1 + 2 * i), (IntExpr) locals.get("sum")));
                Locals retLocals = locals.immutablePut("sum", tmp);
                EventHandlerReturnReason ret = new EventHandlerReturnReason.Goto(add2);
                retMap.put(body, new Triple<>(numSends, retLocals, ret));
            }
            return retMap;
        }
    }

    static class Add2Handler extends EventHandler {
        public Add2Handler(Event eventType, State state) {
            super(eventType, state);
        }

        @Override
        public Map<BoolExpr, Triple<Integer, Locals, EventHandlerReturnReason>> getEncoding(int sends, Locals locals,
                                                                                  Machine target, Payloads payloads) {
            Checker c = target.getChecker();
            int numSends = sends;
            Map<BoolExpr, Triple<Integer, Locals, EventHandlerReturnReason>> retMap = new HashMap<>();
            for (int i = 0; i < branchFactor; i++) {
                IntExpr tmp = c.mkFreshIntConst();
                BoolExpr body = c.mkEq(tmp,
                        c.mkPlus(c.mkInt(2 + 2 * i), (IntExpr) locals.get("sum")));
                Locals retLocals = locals.immutablePut("sum", tmp);
                EventHandlerReturnReason ret = new EventHandlerReturnReason.Goto(add1);
                retMap.put(body, new Triple<>(numSends, retLocals, ret));
            }
            return retMap;
        }
    }

    static class QueryAdd1 extends EventHandler {
        public QueryAdd1(Event eventType, State state) {
            super(eventType, state);
        }

        @Override
        public Map<BoolExpr, Triple<Integer, Locals, EventHandlerReturnReason>> getEncoding(int sends, Locals locals,
                                                                                            Machine target, Payloads payloads) {
            Checker c = target.getChecker();
            int numSends = sends;
            Map<BoolExpr, Triple<Integer, Locals, EventHandlerReturnReason>> retMap = new HashMap<>();
            BoolExpr body = c.send(numSends, new Message(query,
                    new SymbolicMachineIdentifier((IntExpr) payloads.get("client")),
                    new Payloads("sum", locals.get("sum"))));
            numSends++;
            EventHandlerReturnReason ret = new EventHandlerReturnReason.Goto(add2);
            retMap.put(body, new Triple<>(numSends, locals, ret));
            return retMap;
        }
    }

    static class QueryAdd2 extends EventHandler {
        public QueryAdd2(Event eventType, State state) {
            super(eventType, state);
        }

        @Override
        public Map<BoolExpr, Triple<Integer, Locals, EventHandlerReturnReason>> getEncoding(int sends, Locals locals,
                                                                                            Machine target, Payloads payloads) {
            Checker c = target.getChecker();
            int numSends = sends;
            Map<BoolExpr, Triple<Integer, Locals, EventHandlerReturnReason>> retMap = new HashMap<>();
            BoolExpr body = c.send(numSends, new Message(query,
                    new SymbolicMachineIdentifier((IntExpr) payloads.get("client")),
                    new Payloads("sum", locals.get("sum"))));
            numSends++;
            EventHandlerReturnReason ret = new EventHandlerReturnReason.Goto(add1);
            retMap.put(body, new Triple<>(numSends, locals, ret));
            return retMap;
        }
    }

    Random random = new Random();
    int count = 0;
    boolean add1State = true;

    List<Message> getAddMessage(Machine client, Machine adder) {
        List<Message> obs = new ArrayList<>();
        obs.add(new Message(add, adder.getId(), new Payloads("client", client.getId())));
        if (add1State) {
            count+= 3;
        } else {
            count += 3;
        }
        add1State = !add1State;
        return obs;
    }

    List<Message> getQueryMessage(Machine client, Machine adder) {
        List<Message> obs = new ArrayList<>();
        obs.add(new Message(query, adder.getId(), new Payloads("client", client.getId())));
        obs.add(new Message(sum, client.getId(), new Payloads("sum", new PInt(count))));
        add1State = !add1State;
        return obs;
    }

    List<Message> getRandomObservation(Machine client, Machine adder) {
        if (random.nextBoolean()) {
            return getAddMessage(client, adder);
        } else {
            return getQueryMessage(client, adder);
        }
    }

    public void query(Adder adder) {
        Instant queryStartTime = Instant.now();
        adder.check();
        Instant queryEndTime = Instant.now();
        Duration duration = Duration.between(queryStartTime, queryEndTime);
        System.out.println("Query solved in " + duration.getSeconds() + "s");
    }

    @Test
    public void testChecker() {
        Adder adder = new Adder("adder", 0);
        Client client = new Client("clt", 0);
        List<Message> trace = new ArrayList<>();
        adder.observeMessage(new Message(Event.createMachine, adder.getId(), null));
        Instant startTime = Instant.now();
        Duration duration;
        for (int j = 0; j < 10; j++) {
            Instant iterStartTime = Instant.now();
            // bunch of nondeterminism
            for (int i = 0; i < iterations; i++)
                trace.addAll(getAddMessage(client, adder));
            // then finally resolve it
            trace.addAll(getQueryMessage(client, adder));

            for (int i = 0; i < trace.size(); i++) {
                adder.observeMessage(trace.get(i));
                adder.encode();
            }
            Instant observedTime = Instant.now();
            duration = Duration.between(iterStartTime, observedTime);
            System.out.println("Observed " + trace.size() + " messages in " + duration.getSeconds() + "s");
            //System.out.println("Query " + j + " starting");
            query(adder);
            trace.clear();
        }
        //query(adder);
        duration = Duration.between(startTime, Instant.now());
        System.out.println("Solved in " + duration.getSeconds() + "s");
    }

}
