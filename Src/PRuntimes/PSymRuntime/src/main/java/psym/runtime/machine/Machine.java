package psym.runtime.machine;

import psym.commandline.Assert;
import psym.runtime.*;
import psym.runtime.logger.TraceLogger;
import psym.runtime.machine.buffer.*;
import psym.runtime.machine.eventhandlers.EventHandler;
import psym.runtime.machine.eventhandlers.EventHandlerReturnReason;
import psym.runtime.scheduler.Scheduler;
import psym.utils.GlobalData;
import psym.valuesummary.*;
import psym.valuesummary.Guard;

import java.io.Serializable;
import java.util.*;

import lombok.Getter;
import psym.valuesummary.util.SerializableBiFunction;
import psym.valuesummary.util.SerializableFunction;
import psym.valuesummary.util.SerializableRunnable;

public abstract class Machine implements Serializable, Comparable<Machine> {
    private static int numMachines = 0;

    @Getter
    private final String name;
    @Getter
    private final int instanceId;
    @Getter
    transient private Scheduler scheduler;
    private final State startState;
    private final Set<State> states;
    private PrimitiveVS<Boolean> started = new PrimitiveVS<>(false);
    private PrimitiveVS<Boolean> halted = new PrimitiveVS<>(false);
    private PrimitiveVS<State> currentState;
    public EventBuffer sendBuffer;
    public final DeferQueue deferredQueue;
    // note: will not work for receives in functions outside the machine
    private PrimitiveVS<SerializableFunction<Guard, SerializableBiFunction<EventHandlerReturnReason, Message, Guard>>> receives = new PrimitiveVS<>();
    public final Map<String, SerializableFunction<Guard, SerializableBiFunction<EventHandlerReturnReason, Message, Guard>>> continuations = new HashMap<>();
    public final Set<SerializableRunnable> clearContinuationVars = new HashSet<>();

    public void setScheduler(Scheduler scheduler) {
        this.scheduler = scheduler;
    }

    public void receive(String continuationName, Guard pc) {
        PrimitiveVS<SerializableFunction<Guard, SerializableBiFunction<EventHandlerReturnReason, Message, Guard>>> handler = new PrimitiveVS<>(continuations.get(continuationName)).restrict(pc);
        receives = receives.merge(handler);
    }

    public PrimitiveVS<Boolean> hasStarted() {
        return started;
    }

    public PrimitiveVS<Boolean> hasHalted() {
        return halted;
    }

    public Guard getBlockedOnReceiveGuard() { return receives.getUniverse(); }

    public PrimitiveVS<State> getCurrentState() {
        return currentState;
    }

    public void reset() {
        this.currentState = new PrimitiveVS<>(startState);
        this.sendBuffer = new EventQueue(this);
        while (!deferredQueue.isEmpty()) {
            deferredQueue.dequeueEntry(deferredQueue.satisfiesPredUnderGuard(x -> new PrimitiveVS<>(true)).getGuardFor(true));
        }
        this.receives = new PrimitiveVS<>();
        for (Runnable r : clearContinuationVars) { r.run(); }
        this.started = new PrimitiveVS<>(false);
        this.halted = new PrimitiveVS<>(false);
    }

    public List<ValueSummary> getLocalState() {
        List<ValueSummary> localState = new ArrayList<>();
        localState.add(this.currentState);
        localState.add(this.sendBuffer.getEvents());
        localState.add(this.deferredQueue.getEvents());
        localState.add(this.receives);
        localState.add(this.started);
        localState.add(this.halted);
        return localState;
    }

    public int setLocalState(List<ValueSummary> localState) {
        int idx = 0;
        this.currentState = (PrimitiveVS<State>) localState.get(idx++);
        this.sendBuffer.setEvents(localState.get(idx++));
        this.deferredQueue.setEvents(localState.get(idx++));
        this.receives = (PrimitiveVS<SerializableFunction<Guard, SerializableBiFunction<EventHandlerReturnReason, Message, Guard>>>) localState.get(idx++);
        this.started = (PrimitiveVS<Boolean>) localState.get(idx++);
        this.halted = (PrimitiveVS<Boolean>) localState.get(idx++);
        return idx;
    }

    public Machine(String name, int id, State startState, State... states) {
        this.name = name;
//        this.instanceId = id;
        this.instanceId = numMachines++;

        EventBuffer buffer;
        buffer = new EventQueue(this);

        this.startState = startState;
        this.sendBuffer = buffer;
        this.deferredQueue = new DeferQueue();
        this.currentState = new PrimitiveVS<>(startState);

        startState.addHandlers(
                new EventHandler(Event.createMachine) {
                    @Override
                    public void handleEvent(Guard pc, Machine target, UnionVS payload, EventHandlerReturnReason eventHandlerReturnReason) {
                        assert(!BooleanVS.isEverTrue(target.hasStarted().restrict(pc)));
                        target.start(pc, payload);
                    }
                }
        );

        this.states = new HashSet<>();
        Collections.addAll(this.states, states);
    }

    public void start(Guard pc, UnionVS payload) {
        TraceLogger.onMachineStart(pc, this);
        this.currentState = this.currentState.restrict(pc.not()).merge(new PrimitiveVS<>(startState).restrict(pc));
        this.started = this.started.updateUnderGuard(pc, new PrimitiveVS<>(true));

        EventHandlerReturnReason initEventHandlerReturnReason = new EventHandlerReturnReason();
        startState.entry(pc, this, initEventHandlerReturnReason, payload);

        runOutcomesToCompletion(pc, initEventHandlerReturnReason);
    }

    public void halt(Guard pc) {
        this.halted = this.halted.updateUnderGuard(pc, new PrimitiveVS<>(true));
    }

    void runOutcomesToCompletion(Guard pc, EventHandlerReturnReason eventHandlerReturnReason) {
        pc = pc.and(hasHalted().getGuardFor(false));
        if (pc.isFalse()) {
            return;
        }

        int steps = 0;
        // Outer loop: process sequences of 'goto's, 'raise's, 'push's, 'pop's, and events from the deferred queue.
        while (!eventHandlerReturnReason.isNormalReturn()) {
            boolean runDeferred = false;
            Guard deferred = Guard.constFalse();
            if (!eventHandlerReturnReason.getRaiseCond().isFalse()) {
              Message m = eventHandlerReturnReason.getMessageSummary();
              Guard haltGuard = m.getHaltEventGuard().and(pc);
              if (!haltGuard.isFalse()) {
                  EventHandlerReturnReason nextEventHandlerReturnReason = new EventHandlerReturnReason();
                  nextEventHandlerReturnReason.raiseGuardedMessage(m.restrict(haltGuard.not()));
                  processEvent(haltGuard, nextEventHandlerReturnReason, m.restrict(haltGuard));
                  eventHandlerReturnReason = nextEventHandlerReturnReason;
                  receives = receives.restrict(haltGuard.not());
                  continue;
              }
              Guard receiveGuard = getBlockedOnReceiveGuard().and(pc).and(this.currentState.apply(m.getEvent(), (x, msg) -> x.isIgnored(msg)).getGuardFor(false));
              if (!receiveGuard.isFalse()) {
                  PrimitiveVS<SerializableFunction<Guard, SerializableBiFunction<EventHandlerReturnReason, Message, Guard>>> runNow = receives.restrict(receiveGuard);
                  EventHandlerReturnReason nextEventHandlerReturnReason = new EventHandlerReturnReason();
                  nextEventHandlerReturnReason.raiseGuardedMessage(m.restrict(receiveGuard.not()));
                  PrimitiveVS<SerializableFunction<Guard, SerializableBiFunction<EventHandlerReturnReason, Message, Guard>>> oldReceives = new PrimitiveVS<>(receives);
                  receives = receives.restrict(receiveGuard.not());
                  for (GuardedValue<SerializableFunction<Guard, SerializableBiFunction<EventHandlerReturnReason, Message, Guard>>> receiver : runNow.getGuardedValues()) {
                      deferred = deferred.or(receiver.getValue().apply(receiver.getGuard()).apply(nextEventHandlerReturnReason, m.restrict(receiver.getGuard())));
                  }
                  oldReceives = oldReceives.restrict(receiveGuard.not().or(deferred));
                  receives = receives.merge(oldReceives);
                  eventHandlerReturnReason = nextEventHandlerReturnReason;
                  runDeferred = true;
              } else {
                  // clean up receives
                  for (Runnable r : clearContinuationVars) { r.run(); }
              }
            }

            // Inner loop: process sequences of 'goto's and 'raise's.
            while (!eventHandlerReturnReason.isNormalReturn()) {
                Assert.prop(scheduler.getMaxInternalSteps() < 0 || steps < scheduler.getMaxInternalSteps(),
                        String.format("Possible infinite loop found in machine %s", this),
                        pc.and(eventHandlerReturnReason.getGotoCond().or(eventHandlerReturnReason.getRaiseCond())));
                steps++;
                EventHandlerReturnReason nextEventHandlerReturnReason = new EventHandlerReturnReason();
                // goto
                if (!eventHandlerReturnReason.getGotoCond().isFalse()) {
                    processStateTransition(
                            eventHandlerReturnReason.getGotoCond(),
                            nextEventHandlerReturnReason,
                            eventHandlerReturnReason.getGotoStateSummary(),
                            eventHandlerReturnReason.getPayloads()
                    );
                }
                // raise
                if (!eventHandlerReturnReason.getRaiseCond().isFalse()) {
                    processEvent(eventHandlerReturnReason.getRaiseCond(), nextEventHandlerReturnReason, eventHandlerReturnReason.getMessageSummary());
                }

                eventHandlerReturnReason = nextEventHandlerReturnReason;
            }

            if (runDeferred) {
                runDeferredEvents(pc.and(deferred.not()));
            }
        }
    }

    void processStateTransition(
            Guard pc,
            EventHandlerReturnReason eventHandlerReturnReason, // 'out' parameter
            PrimitiveVS<State> newState,
            Map<State, UnionVS> payloads
    ) {
        TraceLogger.onProcessStateTransition(pc, this, newState);

        if (this.currentState == null) {
            this.currentState = newState;
        } else {
            PrimitiveVS<State> guardedState = this.currentState.restrict(pc);
            for (GuardedValue<State> entry : guardedState.getGuardedValues()) {
                entry.getValue().exit(entry.getGuard(), this);
            }

            this.currentState = newState.merge(this.currentState.restrict(pc.not()));
        }

        for (GuardedValue<State> entry : newState.getGuardedValues()) {
            State state = entry.getValue();
            Guard transitionCond = entry.getGuard();
            UnionVS payload = payloads.get(state);
            state.entry(transitionCond, this, eventHandlerReturnReason, payload);
        }
    }

    void processEvent(
            Guard pc,
            EventHandlerReturnReason eventHandlerReturnReason,
            Message message
    ) {
        // assert(event.getMachine().guard(pc).getValues().size() <= 1);
        TraceLogger.onProcessEvent(pc, this, message);
        PrimitiveVS<State> guardedState = this.currentState.restrict(pc);
        for (GuardedValue<State> entry : guardedState.getGuardedValues()) {
            Guard state_pc = entry.getGuard();
            if (state_pc.and(pc).isFalse()) continue;
            entry.getValue().handleEvent(message.restrict(state_pc), this, eventHandlerReturnReason);
        }
    }

    /**
     * Run events from the deferred queue
     * @param pc Guard under which to run
     */
    void runDeferredEvents(Guard pc) {
        if (pc.isFalse()) {
            return;
        }
        List<Guard> deferredMessageGuards = new ArrayList<>();
        List<Message> deferredMessages = new ArrayList<>();
        while (true) {
            Guard deferredPc = pc.and(deferredQueue.isEnabledUnderGuard());
            if (!deferredPc.isFalse()) {
                Message deferredMessage = deferredQueue.dequeueEntry(deferredPc);
                deferredMessageGuards.add(deferredPc);
                deferredMessages.add(deferredMessage);
            } else {
                break;
            }
        }
        for (int i = 0; i < deferredMessageGuards.size(); i++) {
            EventHandlerReturnReason deferredRaiseEventHandlerReturnReason = new EventHandlerReturnReason();
            deferredRaiseEventHandlerReturnReason.raiseGuardedMessage(deferredMessages.get(i));
            runOutcomesToCompletion(deferredMessageGuards.get(i), deferredRaiseEventHandlerReturnReason);
        }
    }

    public void processEventToCompletion(Guard pc, Message message) {
        final EventHandlerReturnReason eventRaiseEventHandlerReturnReason = new EventHandlerReturnReason();
        eventRaiseEventHandlerReturnReason.raiseGuardedMessage(message);

        // Process events from the deferred queue first
        runDeferredEvents(pc.and(getBlockedOnReceiveGuard().not()));

        runOutcomesToCompletion(pc, eventRaiseEventHandlerReturnReason);

        // Process events from the deferred queue again
        runDeferredEvents(pc.and(getBlockedOnReceiveGuard().not()));
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
        if (this.name == null)
            return (this.name == ((Machine) obj).name) && this.instanceId == (((Machine) obj).instanceId);
        return this.name.equals(((Machine) obj).name) && this.instanceId == (((Machine) obj).instanceId);
    }

    @Override
    public int hashCode() {
        if (name == null)
            return instanceId;
        return name.hashCode()^instanceId;
    }

    @Override
    public int compareTo(Machine rhs) {
        return instanceId - rhs.getInstanceId();
    }
}
