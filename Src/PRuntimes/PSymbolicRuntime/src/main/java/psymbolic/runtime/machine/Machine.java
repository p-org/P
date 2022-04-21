package psymbolic.runtime.machine;

import psymbolic.commandline.Assert;
import psymbolic.runtime.*;
import psymbolic.runtime.logger.TraceLogger;
import psymbolic.runtime.machine.buffer.*;
import psymbolic.runtime.machine.eventhandlers.EventHandler;
import psymbolic.runtime.machine.eventhandlers.EventHandlerReturnReason;
import psymbolic.runtime.scheduler.Scheduler;
import psymbolic.valuesummary.*;
import psymbolic.valuesummary.Guard;

import java.io.Serializable;
import java.util.*;
import java.util.function.BiFunction;
import java.util.function.Function;

import lombok.Getter;
import lombok.Setter;

public abstract class Machine implements Serializable {

    private EventBufferSemantics semantics;
    private String name;
    @Getter
    private int instanceId;
    @Getter
    private Scheduler scheduler;
    private final State startState;
    private final Set<State> states;
    private PrimitiveVS<Boolean> started = new PrimitiveVS<>(false);
    private PrimitiveVS<Boolean> halted = new PrimitiveVS<>(false);
    private PrimitiveVS<State> currentState;
    public EventBuffer sendBuffer;
    public final DeferQueue deferredQueue;
    // note: will not work for receives in functions outside the machine
    private PrimitiveVS<Function<Guard, BiFunction<EventHandlerReturnReason, Message, Guard>>> receives = new PrimitiveVS<>();
    public final Map<String, Function<Guard, BiFunction<EventHandlerReturnReason, Message, Guard>>> continuations = new HashMap<>();
    public final Set<Runnable> clearContinuationVars = new HashSet<>();
    // the vector clock for the machine
    private VectorClockVS clock = new VectorClockVS(Guard.constTrue());

    public VectorClockVS getClock() { return clock; }

    public void setScheduler(Scheduler scheduler) {
        this.scheduler = scheduler;
    }

    public void receive(String continuationName, Guard pc) {
        PrimitiveVS<Function<Guard, BiFunction<EventHandlerReturnReason, Message, Guard>>> handler = new PrimitiveVS<>(continuations.get(continuationName)).restrict(pc);
        receives = receives.merge(handler);
    }

    public PrimitiveVS<Boolean> hasStarted() {
        return started;
    }

    public PrimitiveVS<Boolean> hasHalted() {
        return halted;
    }

    public void incrementClock(Guard pc) {
        if (scheduler.getVcManager().isEnabled())
            clock = clock.increment(scheduler.getVcManager().getIdx(new PrimitiveVS<>(this).restrict(pc)));
    }

    public Guard getBlockedOnReceiveGuard() { return receives.getUniverse(); }

    public PrimitiveVS<State> getCurrentState() {
        return currentState;
    }

    public void reset() {
        started = new PrimitiveVS<>(false);
        currentState = new PrimitiveVS<>(startState);
        if (semantics == EventBufferSemantics.bag) {
            this.sendBuffer = new EventBag(this);
        } else if (semantics == EventBufferSemantics.receiver) {
            this.sendBuffer = new ReceiveEventQueue(this);
        } else {
            this.sendBuffer = new EventQueue(this);
        }
        while (!deferredQueue.isEmpty()) {
            deferredQueue.dequeueEntry(deferredQueue.satisfiesPredUnderGuard(x -> new PrimitiveVS<>(true)).getGuardFor(true));
        }
        receives = new PrimitiveVS<>();
        for (Runnable r : clearContinuationVars) { r.run(); }
        clock = new VectorClockVS(Guard.constTrue());
    }

    public void setSemantics(EventBufferSemantics semantics) {
        if (this.sendBuffer != null && !this.sendBuffer.isEmpty() && this.semantics != semantics) {
            throw new RuntimeException("Cannot change buffer semantics when buffer not empty");
        }
        this.semantics = semantics;
        if (semantics == EventBufferSemantics.bag) {
            this.sendBuffer = new EventBag(this);
        } else if (semantics == EventBufferSemantics.receiver) {
            this.sendBuffer = new ReceiveEventQueue(this);
        } else {
            this.sendBuffer = new EventQueue(this);
        }
    }

    public Machine(String name, int id, EventBufferSemantics semantics, State startState, State... states) {
        this.name = name;
        this.instanceId = id;

        this.semantics = semantics;
        EventBuffer buffer;
        if (semantics == EventBufferSemantics.bag) {
            buffer = new EventBag(this);
        } else {
            buffer = new EventQueue(this);
        }

        this.startState = startState;
        this.sendBuffer = buffer;
        this.deferredQueue = new DeferQueue();
        this.currentState = new PrimitiveVS<>(startState);

        startState.addHandlers(
                new EventHandler(Event.createMachine) {
                    @Override
                    public void handleEvent(Guard pc, Machine target, UnionVS payload, EventHandlerReturnReason eventHandlerReturnReason) {
                        assert(!BooleanVS.isEverTrue(hasStarted().restrict(pc)));
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
        int steps = 0;
        // Outer loop: process sequences of 'goto's, 'raise's, 'push's, 'pop's, and events from the deferred queue.
        while (!eventHandlerReturnReason.isNormalReturn()) {
            // TODO: Determine if this can be safely optimized into a concrete boolean
            Guard performedTransition = Guard.constFalse();

            if (!eventHandlerReturnReason.getRaiseCond().isFalse()) {
              Message m = eventHandlerReturnReason.getMessageSummary();
              Guard receiveGuard = getBlockedOnReceiveGuard().and(pc).and(this.currentState.apply(m.getEvent(), (x, msg) -> x.isIgnored(msg)).getGuardFor(false));
              if (!receiveGuard.isFalse()) {
                  PrimitiveVS<Function<Guard, BiFunction<EventHandlerReturnReason, Message, Guard>>> runNow = receives.restrict(receiveGuard);
                  EventHandlerReturnReason nextEventHandlerReturnReason = new EventHandlerReturnReason();
                  nextEventHandlerReturnReason.raiseGuardedMessage(m.restrict(receiveGuard.not()));
                  Guard deferred = Guard.constFalse();
                  for (GuardedValue<Function<Guard, BiFunction<EventHandlerReturnReason, Message, Guard>>> receiver : runNow.getGuardedValues()) {
                      deferred = deferred.or(receiver.getValue().apply(receiver.getGuard()).apply(nextEventHandlerReturnReason, m.restrict(receiver.getGuard())));
                  }
                  receives = receives.restrict(receiveGuard.not().or(deferred));
                  eventHandlerReturnReason = nextEventHandlerReturnReason;
              } else {
                  // clean up receives
                  for (Runnable r : clearContinuationVars) { r.run(); }
              }
            }

            // Inner loop: process sequences of 'goto's and 'raise's.
            while (!eventHandlerReturnReason.isNormalReturn()) {
                Assert.prop(scheduler.getMaxInternalSteps() < 0 || steps < scheduler.getMaxInternalSteps(), scheduler,
                        pc.and(eventHandlerReturnReason.getGotoCond().or(eventHandlerReturnReason.getRaiseCond())));
                steps++;
                EventHandlerReturnReason nextEventHandlerReturnReason = new EventHandlerReturnReason();
                // goto
                if (!eventHandlerReturnReason.getGotoCond().isFalse()) {
                    performedTransition = performedTransition.or(eventHandlerReturnReason.getGotoCond());
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

            // Process events from the deferred queue
            pc = performedTransition.and(deferredQueue.isEnabledUnderGuard());
            if (!pc.isFalse()) {
                EventHandlerReturnReason deferredRaiseEventHandlerReturnReason = new EventHandlerReturnReason();
                Message deferredMessage = deferredQueue.dequeueEntry(pc);
                deferredRaiseEventHandlerReturnReason.raiseGuardedMessage(deferredMessage);
                eventHandlerReturnReason = deferredRaiseEventHandlerReturnReason;
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

    public void processEventToCompletion(Guard pc, Message message) {
        final EventHandlerReturnReason eventRaiseEventHandlerReturnReason = new EventHandlerReturnReason();
        eventRaiseEventHandlerReturnReason.raiseGuardedMessage(message);
        if (scheduler.getVcManager().isEnabled()) {
            this.incrementClock(pc);
            clock = clock.update(message.getVectorClock());
        }
        runOutcomesToCompletion(pc, eventRaiseEventHandlerReturnReason);
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
        return this.name.equals(((Machine) obj).name) && this.instanceId == (((Machine) obj).instanceId);
    }

    @Override
    public int hashCode() {
        if (name == null)
            return instanceId;
        return name.hashCode()^instanceId;
    }
}
