package psym.runtime.machine;

import java.io.Serializable;
import java.util.Objects;
import psym.runtime.PSymGlobal;
import psym.runtime.logger.ScheduleWriter;
import psym.runtime.logger.TextWriter;
import psym.runtime.logger.TraceLogger;
import psym.runtime.machine.eventhandlers.DeferEventHandler;
import psym.runtime.machine.eventhandlers.EventHandler;
import psym.runtime.machine.eventhandlers.EventHandlerReturnReason;
import psym.runtime.machine.eventhandlers.IgnoreEventHandler;
import psym.runtime.machine.events.Event;
import psym.runtime.machine.events.Message;
import psym.runtime.machine.events.StateEvents;
import psym.runtime.scheduler.replay.ReplayScheduler;
import psym.utils.Assert;
import psym.valuesummary.*;
import psym.valuesummary.util.ValueSummaryChecks;

public abstract class State implements Serializable {
  public final String name;
  public final String machineName;
  public final StateTemperature temperature;

  public State(
      String name,
      String machineName,
      StateTemperature temperature,
      EventHandler... eventHandlers) {
    this.name = name;
    this.machineName = machineName;
    this.temperature = temperature;
  }

  public void entry(Guard pc, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
    if (machine.getScheduler() instanceof ReplayScheduler) {
      TextWriter.logStateEntry(machine, this);
    }
  }

  public void exit(Guard pc, Machine machine) {
    if (machine.getScheduler() instanceof ReplayScheduler) {
      TextWriter.logStateExit(machine, this);
    }
  }

  private String getStateKey() {
    return String.format("%s_%s", name, machineName);
  }

  private StateEvents getStateEvents() {
    String key = getStateKey();
    if (!PSymGlobal.getAllStateEvents().containsKey(key))
      PSymGlobal.getAllStateEvents().put(key, new StateEvents());
    return PSymGlobal.getAllStateEvents().get(key);
  }

  public void addHandlers(EventHandler... eventHandlers) {
    for (EventHandler handler : eventHandlers) {
      getStateEvents().eventHandlers.put(handler.event, handler);
      if (handler instanceof IgnoreEventHandler) {
        getStateEvents().ignored.add(handler.event);
      } else if (handler instanceof DeferEventHandler) {
        getStateEvents().deferred.add(handler.event);
      }
    }
  }

  public Boolean isIgnored(Event event) {
    return getStateEvents().ignored.contains(event);
  }

  public Boolean isDeferred(Event event) {
    return getStateEvents().deferred.contains(event);
  }

  public PrimitiveVS<Boolean> hasHandler(Message message) {
    Guard has = Guard.constFalse();
    for (GuardedValue<Event> entry : message.getEvent().getGuardedValues()) {
      if (getStateEvents().eventHandlers.containsKey(entry.getValue())) {
        has = has.or(entry.getGuard());
      }
    }
    return BooleanVS.trueUnderGuard(has).restrict(message.getUniverse());
  }

  public void handleEvent(Message message, Machine machine, EventHandlerReturnReason outcome) {
    for (GuardedValue<Event> entry : message.getEvent().getGuardedValues()) {
      Event event = entry.getValue();
      Guard eventPc = entry.getGuard();
      assert (message.restrict(eventPc).getEvent().getGuardedValues().size() == 1);
      PrimitiveVS<State> current = new PrimitiveVS<>(this).restrict(eventPc);
      TraceLogger.handle(machine, this, message.restrict(entry.getGuard()));

      if (PSymGlobal.getScheduler() instanceof ReplayScheduler) {
        // exclude announce event
        if (!(machine instanceof Monitor)) {
          // exclude ignored and deferred events
          if (!isIgnored(event) && !isDeferred(event)) {
            // exclude raise event from user
            if (!message.getTarget().getGuardedValues().isEmpty()) {
              assert (message.getTarget().getGuardedValues().get(0).getValue() == machine);
              ScheduleWriter.logDequeue(machine, this, event);
              TextWriter.logDequeue(machine, this, event, message.getPayload());
            }
          }
        }
      }

      Guard handledPc = Guard.constFalse();
      for (GuardedValue<State> guardedValue : current.getGuardedValues()) {
        if (guardedValue.getValue().getStateEvents().eventHandlers.containsKey(event)) {
          guardedValue
              .getValue()
              .getStateEvents()
              .eventHandlers
              .get(event)
              .handleEvent(
                  eventPc.and(guardedValue.getGuard()),
                  machine,
                  message.restrict(guardedValue.getGuard()).getPayload(),
                  outcome);
          handledPc = handledPc.or(guardedValue.getGuard());
        }
      }
      if (event.equals(Event.haltEvent)) {
        machine.halt(eventPc.and(handledPc.not()));
      } else if (!ValueSummaryChecks.hasSameUniverse(handledPc, eventPc)) {
        Assert.prop(
            false,
            String.format(
                "%s received event %s that cannot be handled in state %s",
                machine, event, this.name),
            eventPc);
      }
    }
  }

  public boolean isHotState() {
    return temperature == StateTemperature.Hot;
  }

  @Override
  public boolean equals(Object obj) {
    if (obj == this) return true;
    else if (!(obj instanceof State)) {
      return false;
    }
    return this.name.equals(((State) obj).name)
        && this.machineName.equals(((State) obj).machineName)
        && this.temperature.equals(((State) obj).temperature);
  }

  @Override
  public int hashCode() {
    return Objects.hash(name, machineName, temperature);
  }

  @Override
  public String toString() {
    return name;
  }
}
