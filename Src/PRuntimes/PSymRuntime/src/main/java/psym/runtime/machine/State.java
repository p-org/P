package psym.runtime.machine;

import java.io.Serializable;
import java.util.Objects;
import psym.runtime.GlobalData;
import psym.runtime.logger.TraceLogger;
import psym.runtime.machine.eventhandlers.EventHandler;
import psym.runtime.machine.eventhandlers.EventHandlerReturnReason;
import psym.runtime.machine.eventhandlers.IgnoreEventHandler;
import psym.runtime.machine.events.Event;
import psym.runtime.machine.events.Message;
import psym.runtime.machine.events.StateEvents;
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

  public void entry(Guard pc, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {}

  public void exit(Guard pc, Machine machine) {}

  private String getStateKey() {
    return String.format("%s_%s", name, machineName);
  }

  private StateEvents getStateEvents() {
    String key = getStateKey();
    if (!GlobalData.getAllStateEvents().containsKey(key))
      GlobalData.getAllStateEvents().put(key, new StateEvents());
    return GlobalData.getAllStateEvents().get(key);
  }

  public void addHandlers(EventHandler... eventHandlers) {
    for (EventHandler handler : eventHandlers) {
      getStateEvents().eventHandlers.put(handler.event, handler);
      if (handler instanceof IgnoreEventHandler) {
        getStateEvents().ignored.add(handler.event);
      }
    }
  }

  public Boolean isIgnored(Event event) {
    return getStateEvents().ignored.contains(event);
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
