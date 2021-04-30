package psymbolic;

import org.junit.jupiter.api.Test;
import psymbolic.runtime.*;
import psymbolic.valuesummary.PrimVS;
import psymbolic.valuesummary.VectorClockVS;
import psymbolic.valuesummary.bdd.Bdd;

import java.util.Random;

public class TestBuffers {

    public enum TestEvents implements EventName {
        Event0, Event1, Event2, Event3
    }

    Random random = new Random();

    public EventName randomEvent() {
        switch (random.nextInt(5)) {
            case 0: return TestEvents.Event0;
            case 1: return TestEvents.Event1;
            case 2: return TestEvents.Event2;
            case 3: return TestEvents.Event3;
            default: return EventName.Init.instance;
        }
    }

    public void testBuffer(EffectCollection buffer) {
        int iters = 10;
        for (int i = 0; i < iters; i++) {
            buffer.add(new Event(randomEvent(), new VectorClockVS(Bdd.constFalse()), new PrimVS<>()));
        }
        for (int i = 0; i < 10; i++) {
            buffer.remove(Bdd.constTrue());
        }
        assert(buffer.isEmpty());
    }

    @Test
    public void testQueue() {
        testBuffer(new EffectQueue(null));
    }

    @Test
    public void testBag() {
        testBuffer(new EffectBag(null));
    }
}
