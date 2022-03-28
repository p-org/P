package pcontainment;

import com.microsoft.z3.BoolExpr;
import com.microsoft.z3.IntExpr;
import org.junit.jupiter.api.Test;
import p.runtime.values.PInt;
import pcontainment.runtime.*;
import pcontainment.runtime.Message;
import pcontainment.runtime.machine.Machine;
import pcontainment.runtime.machine.State;
import pcontainment.runtime.machine.eventhandlers.EventHandler;
import pcontainment.runtime.machine.eventhandlers.EventHandlerReturnReason;


import java.io.IOException;
import java.util.*;

public class TestChecker {

    public static int branchFactor = 8;
    public static int iterations = 100;

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
            this.addHandler(Event.createMachine, new Init(Event.createMachine));
            this.addHandler(add, new Add1Handler(add));
            this.addHandler(add, new Add2Handler(add));
            this.addHandler(query, new QueryAdd1(query));
            this.addHandler(query, new QueryAdd2(query));

        };

        public IntExpr getsumAtDepth(int depth) {
            return getChecker().mkIntConst("sum_" + depth);
        }
    };

    static class Client extends Machine {

        public Client(String name, int instanceId) {
            super(name, instanceId, null, null, null);
        };

    };

    static class Init extends EventHandler {

        public Init(Event eventType) { super(eventType); }

        @Override
        public Map<BoolExpr, Pair<Integer, EventHandlerReturnReason>> getEncoding(int sends, Machine target, Payloads payloads) {
            Checker c = target.getChecker();
            Adder castTarget = (Adder) target;
            int numSends = sends;
            BoolExpr state = c.getCurrentStateEq(add1);
            target.setStarted(true);
            Map<BoolExpr, Pair<Integer, EventHandlerReturnReason>> retMap = new HashMap<>();
            BoolExpr body0 = c.mkEq(castTarget.getsumAtDepth(c.getDepth()), c.mkInt(0));
            EventHandlerReturnReason ret0 = new EventHandlerReturnReason.NormalReturn();
            retMap.put(c.mkAnd(state, body0), new Pair<>(numSends, ret0));
            return retMap;
        }
    }

    static class Add1Handler extends EventHandler {
        public Add1Handler(Event eventType) {
            super(eventType);
        }

        @Override
        public Map<BoolExpr, Pair<Integer, EventHandlerReturnReason>> getEncoding(int sends, Machine target, Payloads payloads) {
            Checker c = target.getChecker();
            Adder castTarget = (Adder) target;
            int numSends = sends;
            BoolExpr state = c.getCurrentStateEq(add1);
            Map<BoolExpr, Pair<Integer, EventHandlerReturnReason>> retMap = new HashMap<>();
            BoolExpr body0 = c.mkEq(castTarget.getsumAtDepth(c.getDepth()),
                                    c.mkPlus(c.mkInt(1), castTarget.getsumAtDepth(c.getDepth() - 1)));
            EventHandlerReturnReason ret0 = new EventHandlerReturnReason.Goto(add2);
            retMap.put(c.mkAnd(state, body0), new Pair<>(numSends, ret0));
            for (int i = 1; i < branchFactor; i++) {
                BoolExpr body1 = c.mkEq(castTarget.getsumAtDepth(c.getDepth()),
                        c.mkPlus(c.mkInt(1 + 2 * i), castTarget.getsumAtDepth(c.getDepth() - 1)));
                EventHandlerReturnReason ret1 = new EventHandlerReturnReason.Goto(add2);
                retMap.put(c.mkAnd(state, body1), new Pair<>(numSends, ret1));
            }
            return retMap;
        }
    }

    static class Add2Handler extends EventHandler {
        public Add2Handler(Event eventType) {
            super(eventType);
        }

        @Override
        public Map<BoolExpr, Pair<Integer, EventHandlerReturnReason>> getEncoding(int sends, Machine target, Payloads payloads) {
            Checker c = target.getChecker();
            Adder castTarget = (Adder) target;
            int numSends = sends;
            BoolExpr state = c.getCurrentStateEq(add2);
            Map<BoolExpr, Pair<Integer, EventHandlerReturnReason>> retMap = new HashMap<>();
            BoolExpr body0 = c.mkEq(castTarget.getsumAtDepth(c.getDepth()),
                    c.mkPlus(c.mkInt(2), castTarget.getsumAtDepth(c.getDepth() - 1)));
            EventHandlerReturnReason ret0 = new EventHandlerReturnReason.Goto(add1);
            for (int i = 1; i < branchFactor; i++) {
                BoolExpr body1 = c.mkEq(castTarget.getsumAtDepth(c.getDepth()),
                        c.mkPlus(c.mkInt(2 + 2 * i), castTarget.getsumAtDepth(c.getDepth() - 1)));
                EventHandlerReturnReason ret1 = new EventHandlerReturnReason.Goto(add1);
                retMap.put(c.mkAnd(state, body1), new Pair<>(numSends, ret1));
            }
            retMap.put(c.mkAnd(state, body0), new Pair<>(numSends, ret0));
            return retMap;
        }
    }

    static class QueryAdd1 extends EventHandler {
        public QueryAdd1(Event eventType) {
            super(eventType);
        }

        @Override
        public Map<BoolExpr, Pair<Integer, EventHandlerReturnReason>> getEncoding(int sends, Machine target, Payloads payloads) {
            Checker c = target.getChecker();
            Adder castTarget = (Adder) target;
            int numSends = sends;
            BoolExpr state = c.getCurrentStateEq(add1);
            Map<BoolExpr, Pair<Integer, EventHandlerReturnReason>> retMap = new HashMap<>();
            BoolExpr body0 = c.mkEq(castTarget.getsumAtDepth(c.getDepth()), castTarget.getsumAtDepth(c.getDepth() - 1));
            body0 = c.mkAnd(body0, c.send(numSends, new Message(query, (Machine) payloads.get("client"), new Payloads("sum", castTarget.getsumAtDepth(c.getDepth() - 1)))));
            numSends++;
            EventHandlerReturnReason ret0 = new EventHandlerReturnReason.Goto(add2);
            retMap.put(c.mkAnd(state, body0), new Pair<>(numSends, ret0));
            return retMap;
        }
    }

    static class QueryAdd2 extends EventHandler {
        public QueryAdd2(Event eventType) {
            super(eventType);
        }

        @Override
        public Map<BoolExpr, Pair<Integer, EventHandlerReturnReason>> getEncoding(int sends, Machine target, Payloads payloads) {
            Checker c = target.getChecker();
            Adder castTarget = (Adder) target;
            int numSends = sends;
            BoolExpr state = c.getCurrentStateEq(add2);
            Map<BoolExpr, Pair<Integer, EventHandlerReturnReason>> retMap = new HashMap<>();
            BoolExpr body0 = c.mkEq(castTarget.getsumAtDepth(c.getDepth()), castTarget.getsumAtDepth(c.getDepth() - 1));
            body0 = c.mkAnd(body0, c.send(numSends, new Message(query, (Machine) payloads.get("client"), new Payloads("sum", castTarget.getsumAtDepth(c.getDepth() - 1)))));
            numSends++;
            EventHandlerReturnReason ret0 = new EventHandlerReturnReason.Goto(add1);
            retMap.put(c.mkAnd(state, body0), new Pair<>(numSends, ret0));
            return retMap;
        }
    }

    Random random = new Random();
    int count = 0;
    boolean add1State = true;

    List<Message> getAddMessage(Machine client, Machine adder) {
        List<Message> obs = new ArrayList<>();
        obs.add(new Message(add, adder, new Payloads("client", client)));
        if (add1State) {
            count++;
        } else {
            count += 2;
        }
        add1State = !add1State;
        return obs;
    }

    List<Message> getQueryMessage(Machine client, Machine adder) {
        List<Message> obs = new ArrayList<>();
        obs.add(new Message(query, adder, new Payloads("client", client)));
        obs.add(new Message(sum, client, new Payloads("sum", new PInt(count))));
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

    @Test
    public void testChecker() {
        Adder adder = new Adder("adder", 0);
        Client client = new Client("clt", 0);
        List<Message> trace = new ArrayList<>();
        trace.add(new Message(Event.createMachine, adder, null));
        // bunch of nondeterminism
        for (int i = 0; i < iterations; i++)
            trace.addAll(getAddMessage(client, adder));
        // then finally resolve it
        trace.addAll(getQueryMessage(client, adder));

        for (int i = 0; i < trace.size(); i++) {
            adder.observeMessage(trace.get(i));
            adder.encode();
        }
        //adder.check();
        trace.clear();

        // bunch of nondeterminism
        for (int i = 0; i < iterations; i++)
            trace.addAll(getAddMessage(client, adder));
        // then finally resolve it
        trace.addAll(getQueryMessage(client, adder));
        for (int i = 0; i < trace.size(); i++) {
            adder.observeMessage(trace.get(i));
            adder.encode();
        }
        adder.check();

        System.out.println("Observed " + trace.size() + " messages");
    }

}
