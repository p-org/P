package pcontainment;

import org.junit.jupiter.api.Test;
import pcontainment.runtime.*;
import pcontainment.runtime.Message;
import pcontainment.runtime.machine.buffer.EventBag;
import pcontainment.runtime.machine.buffer.EventBuffer;
import pcontainment.runtime.machine.buffer.EventQueue;
import pcontainment.valuesummary.Guard;
import pcontainment.valuesummary.PrimitiveVS;


import java.util.Random;

public class TestBuffers {

    public Event Event0 = new Event("Event0");
    public Event Event1 = new Event("Event1");
    public Event Event2 = new Event("Event2");
    public Event Event3 = new Event("Event3");
    public Event Event4 = new Event("Event4");

    Random random = new Random();

    public Event randomEvent() {
        switch (random.nextInt(5)) {
            case 0: return Event0;
            case 1: return Event1;
            case 2: return Event2;
            case 3: return Event3;
            default: return Event.createMachine;
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
        assert(buffer.isEmpty());
    }

    @Test
    public void testQueue() {
        testBuffer(new EventQueue(null));
    }

    @Test
    public void testBag() {
        testBuffer(new EventBag((null)));
    }
}
