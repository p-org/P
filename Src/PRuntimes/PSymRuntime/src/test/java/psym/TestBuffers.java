package psym;

import java.util.Random;
import org.junit.jupiter.api.Test;
import psym.runtime.machine.buffer.EventBuffer;
import psym.runtime.machine.buffer.EventQueue;
import psym.runtime.machine.events.Event;
import psym.runtime.machine.events.Message;
import psym.valuesummary.Guard;
import psym.valuesummary.PrimitiveVS;

public class TestBuffers {

  public final Event Event0 = new Event("Event0");
  public final Event Event1 = new Event("Event1");
  public final Event Event2 = new Event("Event2");
  public final Event Event3 = new Event("Event3");
  public final Event Event4 = new Event("Event4");

  final Random random = new Random();

  public Event randomEvent() {
    switch (random.nextInt(5)) {
      case 0:
        return Event0;
      case 1:
        return Event1;
      case 2:
        return Event2;
      case 3:
        return Event3;
      default:
        return Event.createMachine;
    }
  }

  public void testBuffer(EventBuffer buffer) {
    int iters = 10;
    for (int i = 0; i < iters; i++) {
      buffer.add(new Message(randomEvent(), new PrimitiveVS<>()));
    }
    for (int i = 0; i < 10; i++) {
      buffer.remove(Guard.constTrue());
    }
    assert (buffer.isEmpty());
  }

  @Test
  public void testQueue() {
    PSym.initializeDefault("output/testCases/testQueue");
    testBuffer(new EventQueue(null));
  }
}
