package psym.runtime.machine.buffer;

import java.io.Serializable;
import java.util.function.Function;

import psym.runtime.logger.ScheduleWriter;
import psym.runtime.logger.TraceLogger;
import psym.runtime.machine.Machine;
import psym.runtime.machine.events.Event;
import psym.runtime.machine.events.Message;
import psym.runtime.scheduler.Scheduler;
import psym.runtime.scheduler.replay.ReplayScheduler;
import psym.valuesummary.*;

public class EventQueue extends SymbolicQueue implements EventBuffer, Serializable {

  private final Machine sender;

  public EventQueue(Machine sender) {
    super();
    this.sender = sender;
  }

  public void send(
      Guard pc, PrimitiveVS<Machine> dest, PrimitiveVS<Event> eventName, UnionVS payload) {
    if (eventName.getGuardedValues().size() > 1) {
      throw new RuntimeException(
          String.format("Handling multiple events together is not supported, in %s", eventName));
    }
    TraceLogger.send(new Message(eventName, dest, payload).restrict(pc));
    Message event = new Message(eventName, dest, payload).restrict(pc);
    if (sender.getScheduler() instanceof ReplayScheduler) {
      ScheduleWriter.logSend(sender, event);
    }
    addEvent(event);
    sender.getScheduler().runMonitors(event);
  }

  public PrimitiveVS<Machine> create(
      Guard pc,
      Scheduler scheduler,
      Class<? extends Machine> machineType,
      UnionVS payload,
      Function<Integer, ? extends Machine> constructor) {
    PrimitiveVS<Machine> machine = scheduler.allocateMachine(pc, machineType, constructor);
    if (payload != null) payload = payload.restrict(pc);
    Message event = new Message(Event.createMachine, machine, payload).restrict(pc);
    if (sender.getScheduler() instanceof ReplayScheduler) {
      ScheduleWriter.logSend(sender, event);
    }
    addEvent(event);
    //        scheduler.performEffect(event);
    return machine;
  }

  public void unblock(Message event) {
    TraceLogger.unblock(event);
    if (sender.getScheduler() instanceof ReplayScheduler) {
      ScheduleWriter.logUnblock(sender, event);
    }
  }

  private void addEvent(Message event) {
    if (sender.getScheduler().getConfiguration().isReceiverQueue()) {
      for (GuardedValue<Machine> target : event.getTarget().getGuardedValues()) {
        target.getValue().getReceiverQueue().add(event.restrict(target.getGuard()));
      }
    } else {
      super.add(event);
    }
  }


}
