package pcontainment.runtime.machine;

import com.microsoft.z3.BoolExpr;
import pcontainment.Checker;
import pcontainment.commandline.Assert;
import pcontainment.runtime.*;
import pcontainment.runtime.logger.TraceLogger;
import pcontainment.runtime.machine.eventhandlers.EventHandler;
import pcontainment.runtime.machine.eventhandlers.EventHandlerReturnReason;
import pcontainment.valuesummary.*;
import pcontainment.valuesummary.Guard;

import java.util.*;
import java.util.concurrent.LinkedBlockingDeque;
import java.util.concurrent.LinkedBlockingQueue;
import java.util.function.BiFunction;
import java.util.function.Function;

import lombok.Getter;

public abstract class Machine {
    private String name;
    @Getter
    private Checker checker;
    @Getter
    private final int instanceId;
    @Getter
    private boolean started = false;
    @Getter
    private boolean halted = false;
    private State currentState;
    @Getter
    private boolean blockedOnReceive = false;
    private final Deque<Observation> trace = new LinkedBlockingDeque<>();
    private final Map<Event, > states = new HashSet<>();

    public final Queue<Message> deferredQueue = new LinkedBlockingQueue<>();

    public Machine(String name, int instanceId, State startState) {
        this.name = name;
        this.instanceId = instanceId;
        currentState = startState;
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
        if (m.getTarget() == instanceId) {
            addReceive(m);
        } else {
            addSend(m);
        }
    }

    public void check() {
        Observation o = trace.getFirst();
        if (!o.isStarted())
            processEvent(o.receive);
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

    public void processEvent(Message receive) {
        List<EventHandler> handlers = getHandlersFor(receive.getEvent());
        for (EventHandler handler : handlers) {
            checker.addHandlerEncoding(runToCompletion(handler));
        }
        checker.runHandlers();
    }

    @Override
    public String toString() {
        return String.format("%s(%d)", name, instanceId);
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this)
            return true;
        else if (!(obj instanceof Machine)) {
            return false;
        }
        return this.instanceId == (((Machine) obj).instanceId);
    }

    @Override
    public int hashCode() {
        return name.hashCode()^instanceId;
    }
}
