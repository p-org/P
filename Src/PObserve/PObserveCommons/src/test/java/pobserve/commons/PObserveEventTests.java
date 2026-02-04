package pobserve.commons;

import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

import java.time.Instant;
import java.time.temporal.ChronoUnit;

public class PObserveEventTests {
    @Test
    public void testSameBasicEvents() {
        Instant now = Instant.now();
        PObserveEvent<Integer> event1 = new PObserveEvent<>("key1", now, 1);
        PObserveEvent<Integer> event2 = new PObserveEvent<>("key1", now, 1);

        performSamenessTests(event1, event2);
    }

    @Test
    public void testSameLogLines() {
        Instant now = Instant.now();
        PObserveEvent<Integer> event1 = new PObserveEvent<>("key1", now, 1, "log line 1");
        PObserveEvent<Integer> event2 = new PObserveEvent<>("key1", now, 1, "log line 1");

        performSamenessTests(event1, event2);
    }

    @Test
    public void testSameCustomPayloads() {
        Instant now = Instant.now();
        PObserveEvent<Integer> event1 = new PObserveEvent<>("key1", now, 1, "log line 1", "payload");
        PObserveEvent<Integer> event2 = new PObserveEvent<>("key1", now, 1, "log line 1", "payload");

        performSamenessTests(event1, event2);
    }

    @Test
    public void testDifferentKeys() {
        Instant now = Instant.now();
        PObserveEvent<Integer> event1 = new PObserveEvent<>("key1", now, 1);
        PObserveEvent<Integer> event2 = new PObserveEvent<>("key2", now, 1);

        performDifferenceTests(event1, event2);
    }

    @Test
    public void testDifferentInstants() {
        Instant now = Instant.now();
        PObserveEvent<Integer> event1 = new PObserveEvent<>("key1", now, 1);
        PObserveEvent<Integer> event2 = new PObserveEvent<>("key1", now.plus(1, ChronoUnit.MICROS), 1);

        performDifferenceTests(event1, event2);
    }

    @Test
    public void testDifferentPayloads() {
        Instant now = Instant.now();
        PObserveEvent<Integer> event1 = new PObserveEvent<>("key1", now, 1);
        PObserveEvent<Integer> event2 = new PObserveEvent<>("key1", now, 2);

        performDifferenceTests(event1, event2);
    }

    @Test
    public void testDifferentLogLines() {
        Instant now = Instant.now();
        PObserveEvent<Integer> event1 = new PObserveEvent<>("key1", now, 1, "line 1");
        PObserveEvent<Integer> event2 = new PObserveEvent<>("key1", now, 1, "line 2");

        performDifferenceTests(event1, event2);
    }

    @Test
    public void testDifferentCustomPayloads() {
        Instant now = Instant.now();
        PObserveEvent<Integer> event1 = new PObserveEvent<>("key1", now, 1, "line 1", "payload 1");
        PObserveEvent<Integer> event2 = new PObserveEvent<>("key1", now, 1, "line 1", "payload 2");

        performDifferenceTests(event1, event2);
    }

    private void performSamenessTests(PObserveEvent<?> event1, PObserveEvent<?> event2) {
        Assertions.assertTrue(event1.equals(event2));
        Assertions.assertEquals(event1.hashCode(), event2.hashCode());
    }

    private void performDifferenceTests(PObserveEvent<?> event1, PObserveEvent<?> event2) {
        Assertions.assertTrue(event1.equals(event1));
        Assertions.assertTrue(event2.equals(event2));
        Assertions.assertFalse(event1.equals(event2));
        Assertions.assertFalse(event2.equals(event1));

        Assertions.assertEquals(event1.hashCode(), event1.hashCode());
        Assertions.assertEquals(event2.hashCode(), event2.hashCode());
        Assertions.assertNotEquals(event1.hashCode(), event2.hashCode());
    }
}
