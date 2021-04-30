package psymbolic.runtime;

import psymbolic.run.Assert;
import psymbolic.valuesummary.*;
import psymbolic.valuesummary.bdd.Bdd;

import java.util.HashSet;
import java.util.Map;
import java.util.Set;

public abstract class Machine extends HasId {
    private Scheduler scheduler;
    private final State startState;
    private final Set<State> states;
    private PrimVS<Boolean> started = new PrimVS<>(false);
    int update = 0;

    private PrimVS<State> state;
    private ListVS<PrimVS<State>> stack;
    public final EffectCollection sendEffects;
    public final DeferQueue deferredQueue;

    private VectorClockVS clock;

    public VectorClockVS getClock() { return this.clock; }

    public void incrementClock(Bdd pc) {
        if (scheduler.schedule.vcManager.isEnabled())
            clock = clock.increment(scheduler.schedule.vcManager.getIdx(new PrimVS<>(this).guard(pc)));
    }

    public void setScheduler(Scheduler scheduler) { this.scheduler = scheduler; }

    public Scheduler getScheduler() { return scheduler; }

    public PrimVS<Boolean> hasStarted() {
        return started;
    }

    public PrimVS<State> getState() {
        return state;
    }

    public ListVS<PrimVS<State>> getStack() { return stack; }

    public void reset() {
        started = new PrimVS<>(false);
        state = new PrimVS<>(startState);
        stack = new ListVS(Bdd.constTrue());
        clock = new VectorClockVS(Bdd.constTrue());
        while (!sendEffects.isEmpty()) {
            Bdd cond = sendEffects.enabledCond(x -> new PrimVS<>(true)).getGuard(true);
            sendEffects.remove(sendEffects.enabledCond(x -> new PrimVS<>(true)).getGuard(true));
        }
        while (!deferredQueue.isEmpty()) {
            deferredQueue.dequeueEntry(deferredQueue.enabledCond(x -> new PrimVS<>(true)).getGuard(true));
        }
    }

    public Machine(String name, int id, BufferSemantics semantics, State startState, State... states) {
        super(name, id);

        EffectCollection buffer;
        switch (semantics) {
            case bag:
                buffer = new EffectBag(this);
                break;
            default:
                buffer = new EffectQueue(this);
                break;
        }

        this.startState = startState;
        this.sendEffects = buffer;
        this.deferredQueue = new DeferQueue();

        this.clock = new VectorClockVS(Bdd.constTrue());

        this.state = new PrimVS<>(startState);
        stack = new ListVS(Bdd.constTrue());

        startState.addHandlers(
                new EventHandler(EventName.Init.instance) {
                    @Override
                    public void handleEvent(Bdd pc, UnionVS payload, Machine machine, Outcome outcome) {
                        assert(!BoolUtils.isEverTrue(hasStarted().guard(pc)));
                        machine.start(pc, payload);
                    }
                }
        );

        this.states = new HashSet<>();
        for (State state : states) {
            this.states.add(state);
        }
    }

    public void start(Bdd pc, UnionVS payload) {
        ScheduleLogger.onMachineStart(pc, this);
        update++;
        this.state = this.state.guard(pc.not()).merge(new PrimVS<>(startState).guard(pc));
        this.started = this.started.update(pc, new PrimVS<>(true));

        Outcome initOutcome = new Outcome();
        startState.entry(pc, this, initOutcome, payload);

        runOutcomesToCompletion(pc, initOutcome);
    }

    void runOutcomesToCompletion(Bdd pc, Outcome outcome) {
        int steps = 0;
        // Outer loop: process sequences of 'goto's, 'raise's, 'push's, 'pop's, and events from the deferred queue.
        while (!outcome.isEmpty()) {
            // TODO: Determine if this can be safely optimized into a concrete boolean
            Bdd performedTransition = Bdd.constFalse();

            // Inner loop: process sequences of 'goto's and 'raise's.
            while (!outcome.isEmpty()) {
                Assert.prop(scheduler.getMaxInternalSteps() < 0 || steps < scheduler.getMaxInternalSteps(), scheduler,
                        pc.and(outcome.getGotoCond().or(outcome.getPopCond()).or(outcome.getPushCond()).or(outcome.getRaiseCond())));
                steps++;
                Outcome nextOutcome = new Outcome();
                // goto
                if (!outcome.getGotoCond().isConstFalse()) {
                    performedTransition = performedTransition.or(outcome.getGotoCond());
                    processStateTransition(
                            outcome.getGotoCond(),
                            nextOutcome,
                            outcome.getGotoStateSummary(),
                            outcome.getPayloads()
                    );
                }
                // raise
                if (!outcome.getRaiseCond().isConstFalse()) {
                    processEvent(outcome.getRaiseCond(), nextOutcome, outcome.getEventSummary());
                }
                // push
                if (!outcome.getPushCond().isConstFalse()) {
                    processPushTransition(
                            outcome.getPushCond(),
                            nextOutcome,
                            outcome.getPushStateSummary(),
                            outcome.getPayloads()
                    );
                }
                // pop
                if (!outcome.getPopCond().isConstFalse()) {
                    popFromStack(outcome.getPopCond());
                }

                outcome = nextOutcome;
            }

            // Process events from the deferred queue
            pc = performedTransition.and(deferredQueue.enabledCond());
            if (!pc.isConstFalse()) {
                Outcome deferredRaiseOutcome = new Outcome();
                Event deferredEvent = deferredQueue.dequeueEntry(pc);
                deferredRaiseOutcome.addGuardedRaiseEvent(deferredEvent);
                outcome = deferredRaiseOutcome;
            }
        }
    }

    void pushOnStack(PrimVS<State> pushState) {
        stack = stack.add(pushState);
    }

    public void popFromStack(Bdd pc) {
        ListVS<PrimVS<State>> guardedStack = stack.guard(pc);
        PrimVS<State> newState = guardedStack.get(IntUtils.subtract(guardedStack.size(), 1));
        ListVS<PrimVS<State>> newStack = guardedStack.removeAt(IntUtils.subtract(guardedStack.size(), 1));
        state = state.update(pc, newState);
        stack = stack.update(pc, newStack);
        ScheduleLogger.log("after pop, stack size is " + stack.size() + ", state is " + state);
    }

    void processPushTransition(Bdd pc,
                               Outcome outcome, // 'out' parameter
                               PrimVS<State> newState,
                               Map<State, UnionVS> payloads) {
        // TODO: logging
        ScheduleLogger.push(state.guard(pc));
        pushOnStack(state.guard(pc)); // push current state on stack
        processStateTransition(pc, outcome, newState, payloads);
    }

    void processStateTransition(
            Bdd pc,
            Outcome outcome, // 'out' parameter
            PrimVS<State> newState,
            Map<State, UnionVS> payloads
    ) {
        ScheduleLogger.onProcessStateTransition(pc, this, newState);

        if (this.state == null) {
            this.state = newState;
        } else {
            PrimVS<State> guardedState = this.state.guard(pc);
            for (GuardedValue<State> entry : guardedState.getGuardedValues()) {
                entry.value.exit(entry.guard, this);
            }

            this.state = newState.merge(this.state.guard(pc.not()));
        }

        for (GuardedValue<State> entry : newState.getGuardedValues()) {
            State state = entry.value;
            Bdd transitionCond = entry.guard;
            UnionVS payload = payloads.get(state);
            state.entry(transitionCond, this, outcome, payload);
        }
    }

    void processEvent(
            Bdd pc,
            Outcome outcome,
            Event event
    ) {
        // assert(event.getMachine().guard(pc).getValues().size() <= 1);
        ScheduleLogger.onProcessEvent(pc, this, event);
        PrimVS<State> guardedState = this.state.guard(pc);
        for (GuardedValue<State> entry : guardedState.getGuardedValues()) {
            Bdd state_pc = entry.guard;
            if (state_pc.and(pc).isConstFalse()) continue;
            entry.value.handleEvent(event.guard(state_pc), this, outcome);
        }
    }

    void processEventToCompletion(Bdd pc, Event event) {
        final Outcome eventRaiseOutcome = new Outcome();
        eventRaiseOutcome.addGuardedRaiseEvent(event);
        if (scheduler.schedule.vcManager.isEnabled()) {
            this.incrementClock(pc);
            clock = clock.update(event.getVectorClock());
        }
        runOutcomesToCompletion(pc, eventRaiseOutcome);
    }

    @Override
    public String toString() {
        return name + "#" + id;
    }
}
