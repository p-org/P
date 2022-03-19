package pcontainment.runtime.machine;

import com.microsoft.z3.BoolExpr;
import pcontainment.Checker;
import pcontainment.runtime.*;
import pcontainment.runtime.machine.eventhandlers.EventHandler;
import pcontainment.runtime.machine.eventhandlers.EventHandlerReturnReason;

import java.util.*;
import java.util.concurrent.LinkedBlockingDeque;

import lombok.Getter;

public abstract class Machine {
    private static int machineCount = 0;
    private String name;
    @Getter
    private Checker checker;
    @Getter
    private final int id;
    @Getter
    private boolean started = false;
    @Getter
    private boolean halted = false;
    @Getter
    private final State startState;
    @Getter
    private final List<State> states;
    @Getter
    private BoolExpr blockedOnReceive;
    private final Deque<Observation> trace = new LinkedBlockingDeque<>();
    private final Map<Event, Iterable<EventHandler>> eventHandlers;

    // TODO: handle deferred messages

    public Machine(String name, int instanceId, State startState, List<State> states,
                   Map<Event, Iterable<EventHandler>> handlers) {
        this.name = name;
        this.id = machineCount++;
        this.startState = startState;
        this.states = states;
        this.eventHandlers = handlers;
    }

    private void addReceive(Message r) {
        if (trace.size() > 0) {
            trace.getLast().setPartial(false);
        }
        Observation obs = new Observation(r);
    }

    private void addSend(Message s) {
        if (trace.size() == 0) {
            Observation obs = new Observation(null);
            trace.add(obs);
        }
        trace.getLast().addSend(s);
    }

    public void observeMessage(Message m) {
        if (this.equals(m.getTarget())) {
            addReceive(m);
        } else {
            addSend(m);
        }
    }

    public void check() {
        Observation o = trace.getFirst();
        if (!o.isStarted())
            processEventToCompletion(o.receive);
        int sendLen = o.sends.size();
        while (o.getSendIdx() < sendLen) {
            checker.addConcreteSend(o.sends.get(o.getSendIdx()));
            o.setSendIdx(o.getSendIdx() + 1);
        }
        if (!o.isPartial()) {
            checker.noMoreSends();
        }
        checker.check();
        if (!o.isPartial()) {
            checker.nextDepth();
        }
    }

    public Iterable<EventHandler> getHandlersFor (Event e) {
        eventHandlers.get(e);
    }

    public void processEventToCompletion(Message receive) {
        final EventHandlerReturnReason eventRaiseEventHandlerReturnReason = new EventHandlerReturnReason.Raise(receive);
        checker.runOutcomesToCompletion(this, eventRaiseEventHandlerReturnReason);
    }

    @Override
    public String toString() {
        return String.format("%s(%d)", name, id);
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this)
            return true;
        else if (!(obj instanceof Machine)) {
            return false;
        }
        return this.id == (((Machine) obj).id);
    }

    @Override
    public int hashCode() {
        return ((Integer) id).hashCode();
    }
}
