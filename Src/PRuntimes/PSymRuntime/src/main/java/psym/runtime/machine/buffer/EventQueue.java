package psym.runtime.machine.buffer;

import java.io.Serializable;
import java.util.function.Function;
import psym.runtime.logger.ScheduleWriter;
import psym.runtime.logger.TextWriter;
import psym.runtime.logger.TraceLogger;
import psym.runtime.machine.Machine;
import psym.runtime.machine.events.Event;
import psym.runtime.machine.events.Message;
import psym.runtime.scheduler.Scheduler;
import psym.runtime.scheduler.replay.ReplayScheduler;
import psym.utils.exception.BugFoundException;
import psym.valuesummary.*;

public class EventQueue extends SymbolicQueue implements EventBuffer, Serializable {

  private final Machine sender;

  public EventQueue(Machine sender) {
    super(sender);
    this.sender = sender;
  }

  public void send(
      Guard pc, PrimitiveVS<Machine> dest, PrimitiveVS<Event> eventName, UnionVS payload) {
    Guard destIsNull = dest.symbolicEquals(null, pc).getGuardFor(true);
    if (!destIsNull.isFalse()) {
      throw new BugFoundException(
              "Machine in send cannot be null",
              destIsNull
      );
    }
    Message event = new Message(eventName, dest, payload).restrict(pc);
    TraceLogger.send(event);
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
    addEvent(event);
    //        scheduler.performEffect(event);
    return machine;
  }

  public void unblock(Message event) {
    TraceLogger.unblock(event);
    if (sender.getScheduler() instanceof ReplayScheduler) {
      ScheduleWriter.logUnblock(sender, event);
      TextWriter.logUnblock(sender, event);
    }
  }

  private void addEvent(Message event) {
    if (sender.getScheduler() instanceof ReplayScheduler) {
      ScheduleWriter.logEnqueue(sender, event);
      TextWriter.logEnqueue(sender, event);
    }
    super.add(event);
  }


}
