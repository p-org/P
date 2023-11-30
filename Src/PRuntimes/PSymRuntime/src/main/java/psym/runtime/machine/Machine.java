package psym.runtime.machine;

import java.io.Serializable;
import java.util.*;
import lombok.Getter;
import org.apache.commons.lang3.tuple.ImmutablePair;
import psym.runtime.PSymGlobal;
import psym.runtime.logger.TextWriter;
import psym.runtime.logger.TraceLogger;
import psym.runtime.machine.buffer.*;
import psym.runtime.machine.eventhandlers.EventHandler;
import psym.runtime.machine.eventhandlers.EventHandlerReturnReason;
import psym.runtime.machine.events.Event;
import psym.runtime.machine.events.Message;
import psym.runtime.scheduler.Scheduler;
import psym.runtime.scheduler.replay.ReplayScheduler;
import psym.runtime.scheduler.search.choiceorchestration.ChoiceLearningStateMode;
import psym.utils.Assert;
import psym.utils.serialize.SerializableBiFunction;
import psym.utils.serialize.SerializableFunction;
import psym.utils.serialize.SerializableRunnable;
import psym.valuesummary.*;

public abstract class Machine implements Serializable, Comparable<Machine> {
  @Getter
  private static final int mainMachineId = 2;
  @Getter private static final Map<String, Machine> nameToMachine = new HashMap<>();
  protected static int globalMachineId = mainMachineId;
  public final Map<
          String,
          SerializableFunction<
              Guard, SerializableBiFunction<EventHandlerReturnReason, Message, Guard>>>
      continuations = new HashMap<>();
  public final Set<SerializableRunnable> clearContinuationVars = new HashSet<>();
  @Getter protected final String name;
  private final State startState;
  private final Set<State> states;
  @Getter protected int instanceId;
  @Getter private EventQueue sendBuffer;
  @Getter private DeferQueue deferredQueue;
  @Getter private transient Scheduler scheduler;
  private PrimitiveVS<Boolean> started = new PrimitiveVS<>(false);
  private PrimitiveVS<Boolean> halted = new PrimitiveVS<>(false);
  private PrimitiveVS<State> currentState;
  // note: will not work for receives in functions outside the machine
  private PrimitiveVS<
          SerializableFunction<
              Guard, SerializableBiFunction<EventHandlerReturnReason, Message, Guard>>>
      receives = new PrimitiveVS<>();

  @Getter private Set<Event> observedEvents;
  @Getter private Set<ImmutablePair<Event, Event>> happensBeforePairs;

  public Machine(String name, int id, State startState, State... states) {
    this.name = name;
    //        this.instanceId = id;
    this.instanceId = globalMachineId++;
    nameToMachine.put(toString(), this);

    this.startState = startState;
    this.sendBuffer = new EventQueue(this);
    this.deferredQueue = new DeferQueue(this);
    this.currentState = new PrimitiveVS<>(startState);

    startState.addHandlers(
        new EventHandler(Event.createMachine) {
          @Override
          public void handleEvent(
              Guard pc,
              Machine target,
              UnionVS payload,
              EventHandlerReturnReason eventHandlerReturnReason) {
            assert (!BooleanVS.isEverTrue(target.hasStarted().restrict(pc)));
            target.start(pc, payload);
          }
        });

    this.states = new HashSet<>();
    Collections.addAll(this.states, states);

    this.observedEvents = new HashSet<>();
    this.happensBeforePairs = new HashSet<>();
  }

  public void setScheduler(Scheduler scheduler) {
    this.scheduler = scheduler;
  }

  public SymbolicQueue getEventBuffer() {
    return sendBuffer;
  }

  public void receive(String continuationName, Guard pc) {
    PrimitiveVS<
            SerializableFunction<
                Guard, SerializableBiFunction<EventHandlerReturnReason, Message, Guard>>>
        handler = new PrimitiveVS<>(continuations.get(continuationName)).restrict(pc);
    receives = receives.merge(handler);
  }

  public PrimitiveVS<Boolean> hasStarted() {
    return started;
  }

  public PrimitiveVS<Boolean> hasHalted() {
    return halted;
  }

  public Guard getBlockedOnReceiveGuard() {
    return receives.getUniverse();
  }

  public PrimitiveVS<State> getCurrentState() {
    return currentState;
  }

  public void reset() {
    this.currentState = new PrimitiveVS<>(startState);
    this.sendBuffer = new EventQueue(this);
    this.deferredQueue = new DeferQueue(this);
    this.receives = new PrimitiveVS<>();
    for (Runnable r : clearContinuationVars) {
      r.run();
    }
    this.started = new PrimitiveVS<>(false);
    this.halted = new PrimitiveVS<>(false);
    this.observedEvents.clear();
    this.happensBeforePairs.clear();
  }

  protected List<ValueSummary> getLocalVars() {
    List<ValueSummary> localVars = new ArrayList<>();
    localVars.add(this.currentState);
    localVars.add(this.sendBuffer.getEvents());
    localVars.add(this.deferredQueue.getEvents());
    localVars.add(this.receives);
    localVars.add(this.started);
    localVars.add(this.halted);
    return localVars;
  }

  protected int setLocalVars(List<ValueSummary> localVars) {
    int idx = 0;
    this.currentState = (PrimitiveVS<State>) localVars.get(idx++);
    this.sendBuffer.setEvents(localVars.get(idx++));
    this.deferredQueue.setEvents(localVars.get(idx++));
    this.receives =
        (PrimitiveVS<
                SerializableFunction<
                    Guard, SerializableBiFunction<EventHandlerReturnReason, Message, Guard>>>)
            localVars.get(idx++);
    this.started = (PrimitiveVS<Boolean>) localVars.get(idx++);
    this.halted = (PrimitiveVS<Boolean>) localVars.get(idx++);
    return idx;
  }

  public MachineLocalState getMachineLocalState() {
    MachineLocalState machineLocalState = new MachineLocalState();
    machineLocalState.setLocals(getLocalVars());
    machineLocalState.setObservedEvents(observedEvents);
    machineLocalState.setHappensBeforePairs(happensBeforePairs);
    return machineLocalState;
  }

  public void setMachineLocalState(MachineLocalState localState) {
    setLocalVars(localState.getLocals());
    observedEvents = localState.getObservedEvents();
    happensBeforePairs = localState.getHappensBeforePairs();
  }

  public void start(Guard pc, UnionVS payload) {
    TraceLogger.onMachineStart(pc, this);
    this.currentState =
        this.currentState.restrict(pc.not()).merge(new PrimitiveVS<>(startState).restrict(pc));
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
    // Outer loop: process sequences of 'goto's, 'raise's, 'push's, 'pop's, and events from the
    // deferred queue.
    while (eventHandlerReturnReason.isAbnormalReturn()) {
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
        Guard receiveGuard =
            getBlockedOnReceiveGuard()
                .and(pc)
                .and(
                    this.currentState
                        .apply(m.getEvent(), (x, msg) -> x.isIgnored(msg))
                        .getGuardFor(false));
        if (!receiveGuard.isFalse()) {
          PrimitiveVS<
                  SerializableFunction<
                      Guard, SerializableBiFunction<EventHandlerReturnReason, Message, Guard>>>
              runNow = receives.restrict(receiveGuard);
          EventHandlerReturnReason nextEventHandlerReturnReason = new EventHandlerReturnReason();
          nextEventHandlerReturnReason.raiseGuardedMessage(m.restrict(receiveGuard.not()));
          PrimitiveVS<
                  SerializableFunction<
                      Guard, SerializableBiFunction<EventHandlerReturnReason, Message, Guard>>>
              oldReceives = new PrimitiveVS<>(receives);
          receives = receives.restrict(receiveGuard.not());
          for (GuardedValue<
                  SerializableFunction<
                      Guard, SerializableBiFunction<EventHandlerReturnReason, Message, Guard>>>
              receiver : runNow.getGuardedValues()) {
            deferred =
                deferred.or(
                    receiver
                        .getValue()
                        .apply(receiver.getGuard())
                        .apply(nextEventHandlerReturnReason, m.restrict(receiver.getGuard())));
          }
          oldReceives = oldReceives.restrict(receiveGuard.not().or(deferred));
          receives = receives.merge(oldReceives);
          eventHandlerReturnReason = nextEventHandlerReturnReason;
          runDeferred = true;
        } else {
          // clean up receives
          for (Runnable r : clearContinuationVars) {
            r.run();
          }
        }
      }

      // Inner loop: process sequences of 'goto's and 'raise's.
      while (eventHandlerReturnReason.isAbnormalReturn()) {
        Assert.liveness(
            scheduler.getMaxInternalSteps() < 0 || steps < scheduler.getMaxInternalSteps(),
            String.format("Possible infinite loop found in machine %s", this),
            pc.and(
                eventHandlerReturnReason
                    .getGotoCond()
                    .or(eventHandlerReturnReason.getRaiseCond())));
        steps++;
        EventHandlerReturnReason nextEventHandlerReturnReason = new EventHandlerReturnReason();
        // goto
        if (!eventHandlerReturnReason.getGotoCond().isFalse()) {
          processStateTransition(
              eventHandlerReturnReason.getGotoCond(),
              nextEventHandlerReturnReason,
              eventHandlerReturnReason.getGotoStateSummary(),
              eventHandlerReturnReason.getPayloads());
        }
        // raise
        if (!eventHandlerReturnReason.getRaiseCond().isFalse()) {
          processEvent(
              eventHandlerReturnReason.getRaiseCond(),
              nextEventHandlerReturnReason,
              eventHandlerReturnReason.getMessageSummary());
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
      Map<State, UnionVS> payloads) {
    TraceLogger.onProcessStateTransition(pc, this, newState);
    if (this.getScheduler() instanceof ReplayScheduler) {
      TextWriter.logGoto(this, newState);
    }

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

  void processEvent(Guard pc, EventHandlerReturnReason eventHandlerReturnReason, Message message) {
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
   *
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
        Message deferredMessage = deferredQueue.remove(deferredPc);
        deferredMessageGuards.add(deferredPc);
        deferredMessages.add(deferredMessage);
      } else {
        break;
      }
    }
    for (int i = 0; i < deferredMessageGuards.size(); i++) {
      EventHandlerReturnReason deferredRaiseEventHandlerReturnReason =
          new EventHandlerReturnReason();
      deferredRaiseEventHandlerReturnReason.raiseGuardedMessage(deferredMessages.get(i));
      runOutcomesToCompletion(deferredMessageGuards.get(i), deferredRaiseEventHandlerReturnReason);
    }
  }

  private void addObservedEvent(Event newEvent) {
    for (Event happenedBeforeEvent : observedEvents) {
      happensBeforePairs.add(new ImmutablePair<>(happenedBeforeEvent, newEvent));
    }
    observedEvents.add(newEvent);
  }

  private void updateObservedEvents(Message message) {
    for (Event e : message.getEvent().getValues()) {
      addObservedEvent(e);
    }
  }

  public void processEventToCompletion(Guard pc, Message message) {
    if (PSymGlobal.getConfiguration().getChoiceLearningStateMode()
        == ChoiceLearningStateMode.TimelineAbstraction) {
      updateObservedEvents(message);
    }

    final EventHandlerReturnReason eventRaiseEventHandlerReturnReason =
        new EventHandlerReturnReason();
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
    if (obj == this) return true;
    else if (!(obj instanceof Machine)) {
      return false;
    }
    if (this.name == null)
      return (((Machine) obj).name == null) && this.instanceId == (((Machine) obj).instanceId);
    return this.name.equals(((Machine) obj).name)
        && this.instanceId == (((Machine) obj).instanceId);
  }

  @Override
  public int hashCode() {
    if (name == null) return instanceId;
    return name.hashCode() ^ instanceId;
  }

  @Override
  public int compareTo(Machine rhs) {
    return instanceId - rhs.getInstanceId();
  }
}
