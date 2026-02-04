package pobserve.junit;

import pobserve.commons.PObserveEvent;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.MethodSource;
import pobserve.runtime.Monitor;
import pobserve.runtime.events.PEvent;
import pobserve.runtime.exceptions.PAssertionFailureException;

import java.lang.reflect.Field;
import java.util.ArrayList;
import java.util.Map;
import java.util.Objects;
import java.util.function.Supplier;
import java.util.Collections;
import java.util.List;
import java.util.Random;

import java.util.stream.Collectors;
import java.util.stream.Stream;

import static org.junit.jupiter.api.Assertions.*;



public class EventSequencerTests {

    public static class PEvent_idx extends PEvent<Long> {
        private long l;

        public PEvent_idx(long l) {
            this.l = l;
        }

        @Override
        public Long getPayload() {
            return l;
        }
    }

    static Stream<EventSequencer> testSpecEventSequencers() {
        Supplier<?> supplier = new TestPMachines.TestSpec.Supplier();
        List<Supplier<?>> monitorSuppliers = new ArrayList<>();
        monitorSuppliers.add(supplier);
        return Stream.of(
                new WindowedEventSequencer(10, monitorSuppliers),
                new TotalEventSequencer(monitorSuppliers)
        );
    }

    static Stream<EventSequencer> emptyEventSequencers() {
        List<Supplier<?>> monitorSuppliers = new ArrayList<>();
        return Stream.of(
                new WindowedEventSequencer(10, monitorSuppliers),
                new TotalEventSequencer(monitorSuppliers)
        );
    }

    @ParameterizedTest
    @MethodSource("testSpecEventSequencers")
    public void testEventProcessing(EventSequencer sequencer) throws Exception {
        long delayCounter = 10;

        /* We begin with some events, out of order but all within `delayMillis` of each other. */

        ArrayList<PObserveEvent<PEvent_idx>> inputs = new ArrayList<>();
        for (long batchId = 0; batchId < 100; batchId++) {
            ArrayList<PObserveEvent<PEvent_idx>> batch = new ArrayList<>();
            for (long i = 0; i < delayCounter; i++) {
                long ts = batchId * delayCounter + i;
                batch.add(new PObserveEvent<>("key", ts, new PEvent_idx(ts)));
            }
            Collections.shuffle(batch);
            inputs.addAll(batch);
        }

        /* Next, we process each "almost ordered" input into the sequencer, inducing a bit of latency by parking the
         * thread for a few ms, so we can ensure events are asynchronously processed in batches in this test rather
         * than "all at once".
         */

        inputs.forEach((pe) -> {
            sequencer.accept(pe);
            synchronized (this) {
                try {
                    wait(1);
                } catch (InterruptedException e) {
                }
            }
        });

        /* Shut down the sequencer, ensuring that any pending events are drained out too. */

        try {
            assertTrue(sequencer.shutdown());
        } catch (InterruptedException e) {
            assertNull(e);
        }

        /* What we get out of the sequencer must be what we put into it, but completely ordered this time! */

        Field privateField = EventSequencer.class.getDeclaredField("keyMonitor");
        privateField.setAccessible(true);
        Map<String, List<Monitor<?>>> value = (Map<String, List<Monitor<?>>>) privateField.get(sequencer);
        ArrayList<PEvent<?>> outputs = ((TestPMachines.TestSpec) value.get("key").get(0)).getOutput();
        ArrayList<PEvent<?>> expected = new ArrayList<>();
        List<PObserveEvent<?>> sortedInput = inputs.stream().sorted().collect(Collectors.toList());
        for (PObserveEvent<?> p : sortedInput) {
            expected.add((PEvent<?>) p.getEvent());
        }

        assertEquals(expected.size(), outputs.size());
        assertArrayEquals(expected.toArray(), outputs.toArray());
    }

    @ParameterizedTest
    @MethodSource("emptyEventSequencers")
    public void testOutOfOrderExceptionThrowing(EventSequencer sequencer) {
        /* We try to make sequencer accept events out of order more than `delayMillis` from each other,
        * the eventSequencer should throw UnorderableEventException */

        long delayMillis = 10;

        sequencer.accept(new PObserveEvent<>("key", (long)21, new PEvent_idx((long)21)));
        sequencer.accept(new PObserveEvent<>("key", (long)22, new PEvent_idx((long)22)));
        synchronized (this) {
            try {
                wait(10);
            } catch (InterruptedException e) {
            }
        }
        sequencer.accept(new PObserveEvent<>("key", (long)11, new PEvent_idx((long)11)));
        synchronized (this) {
            try {
                wait(10);
            } catch (InterruptedException e) {
            }
        }
        if (sequencer.canThrowOutOfOrderException()) {
            assertThrows(UnorderableEventException.class, () -> {
                sequencer.accept(new PObserveEvent<>("key", (long) 0, new PEvent_idx((long) 0)));
                synchronized (this) {
                    try {
                        wait(10);
                    } catch (InterruptedException e) {
                    }
                }
            });
        } else {
            try {
                assertTrue(sequencer.shutdown());
            } catch (InterruptedException e) {
                assertNull(e);
            }
        }
    }

    @ParameterizedTest
    @MethodSource("testSpecEventSequencers")
    public void testKeySeperation(EventSequencer sequencer) throws IllegalAccessException, NoSuchFieldException {
        long delayMillis = 10;
        Random random = new Random();

        /* We begin with some events, out of order but all within `delayMillis` of each other.
         * Each event will be randomly assigned to a number between 0 and 4 as key.
         */

        ArrayList<PObserveEvent<PEvent_idx>> inputs = new ArrayList<>();
        for (long batchId = 0; batchId < 100; batchId++) {
            ArrayList<PObserveEvent<PEvent_idx>> batch = new ArrayList<>();
            for (long i = 0; i < delayMillis; i++) {
                long ts = batchId * delayMillis + i;
                batch.add(new PObserveEvent<>(String.valueOf(random.nextInt(5)), ts, new PEvent_idx(ts)));
            }
            Collections.shuffle(batch);
            inputs.addAll(batch);
        }

        /* Next, we process each "almost ordered" input into the sequencer, inducing a bit of latency by parking the
         * thread for a few ms, so we can ensure events are asynchronously processed in batches in this test rather
         * than "all at once".
         */

        inputs.forEach((pe) -> {
            sequencer.accept(pe);
            synchronized (this) {
                try {
                    wait(1);
                } catch (InterruptedException e) {
                }
            }
        });

        /* Shut down the sequencer, ensuring that any pending events are drained out too. */

        try {
            assertTrue(sequencer.shutdown());
        } catch (InterruptedException e) {
            assertNull(e);
        }

        /* What we get out of the sequencer must be what we put into it, but completely ordered this time! */

        Field privateField = EventSequencer.class.getDeclaredField("keyMonitor");
        privateField.setAccessible(true);
        Map<String, List<Monitor<?>>> map = (Map<String, List<Monitor<?>>>) privateField.get(sequencer);
        List<PObserveEvent<?>> sortedInput = inputs.stream().sorted().collect(Collectors.toList());

        /* Checking to see if events are sorted by keys and fed into the monitor that matches the key*/

        for (String key : map.keySet()) {
            ArrayList<PEvent<?>> outputs = ((TestPMachines.TestSpec) map.get(String.valueOf(key)).get(0)).getOutput();
            ArrayList<PEvent<?>> expected = new ArrayList<>();
            for (PObserveEvent<?> p : sortedInput) {
                if (Objects.equals(p.getPartitionKey(), String.valueOf(key))) {
                    expected.add((PEvent<?>) p.getEvent());
                }
            }
            assertEquals(expected.size(), outputs.size());
            assertArrayEquals(expected.toArray(), outputs.toArray());
        }
    }

    @ParameterizedTest
    @MethodSource("testSpecEventSequencers")
    public void testSameTimestamp(EventSequencer sequencer) throws NoSuchFieldException, IllegalAccessException {
        Random random = new Random();
        long delayCounter = 10;

        /* We begin with some events, all events in the same milliseconds have same timestamp
         */

        ArrayList<PObserveEvent<PEvent_idx>> inputs = new ArrayList<>();
        for (long batchId = 0; batchId < 100; batchId++) {
            ArrayList<PObserveEvent<PEvent_idx>> batch = new ArrayList<>();
            for (long i = 0; i < delayCounter; i++) {
                batch.add(new PObserveEvent<>(String.valueOf(random.nextInt(5)), batchId,
                        new PEvent_idx(i + batchId * 10)));
            }
            inputs.addAll(batch);
        }

        /* Next, we process each input into the sequencer, inducing a bit of latency by parking the
         * thread for a few ms, so we can ensure events are asynchronously processed in batches in this test rather
         * than "all at once".
         */

        inputs.forEach((pe) -> {
            sequencer.accept(pe);
            synchronized (this) {
                try {
                    wait(1);
                } catch (InterruptedException e) {
                }
            }
        });

        /* Shut down the sequencer, ensuring that any pending events are drained out too. */

        try {
            assertTrue(sequencer.shutdown());
        } catch (InterruptedException e) {
            assertNull(e);
        }

        /* What we get out of the sequencer must be what we put into it, but came out the same order as they went in */

        Field privateField = EventSequencer.class.getDeclaredField("keyMonitor");
        privateField.setAccessible(true);
        Map<String, List<Monitor<?>>> map = (Map<String, List<Monitor<?>>>) privateField.get(sequencer);
        List<PObserveEvent<?>> sortedInput = inputs.stream().sorted().collect(Collectors.toList());

        /* Checking to see if events are sorted by keys and fed into the monitor that matches the key*/

        for (String key : map.keySet()) {
            ArrayList<PEvent<?>> outputs = ((TestPMachines.TestSpec) map.get(String.valueOf(key)).get(0)).getOutput();
            ArrayList<PEvent<?>> expected = new ArrayList<>();
            for (PObserveEvent<?> p : sortedInput) {
                if (Objects.equals(p.getPartitionKey(), String.valueOf(key))) {
                    expected.add((PEvent<?>) p.getEvent());
                }
            }
            assertEquals(expected.size(), outputs.size());
            assertArrayEquals(expected.toArray(), outputs.toArray());
        }
    }

    @ParameterizedTest
    @MethodSource("testSpecEventSequencers")
    public void exceptionMessageShouldContainEventReplay(EventSequencer sequencer) {
        // given

        long delayMillis = 10;
        Random random = new Random();

        /* We begin with some events, out of order but all within `delayMillis` of each other.
         * Each event will be randomly assigned to a number between 0 and 4 as key.
         */

        ArrayList<PObserveEvent<PEvent_idx>> inputs = new ArrayList<>();
        for (long batchId = 0; batchId < 100; batchId++) {
            ArrayList<PObserveEvent<PEvent_idx>> batch = new ArrayList<>();
            for (long i = 0; i < delayMillis; i++) {
                long ts = batchId * delayMillis + i;
                batch.add(new PObserveEvent<>(String.valueOf(random.nextInt(5)), ts, new PEvent_idx(ts)));
            }
            Collections.shuffle(batch);
            inputs.addAll(batch);
        }

        /* Injecting an error at random place */

        long index = random.nextInt(inputs.size() + 1);
        String errorKey = String.valueOf(random.nextInt(5));
        PObserveEvent<PEvent_idx> errorEvent = new PObserveEvent<>(errorKey, index, new PEvent_idx(-1));
        System.out.println("injecting error at index: " + index + ", key: " + errorKey);
        inputs.set((int) index, errorEvent);

        /* Next, we process each "almost ordered" input into the sequencer, inducing a bit of latency by parking the
         * thread for a few ms, so we can ensure events are asynchronously processed in batches in this test rather
         * than "all at once".
         */

        inputs.forEach((pe) -> {
            sequencer.accept(pe);
            synchronized (this) {
                try {
                    wait(1);
                } catch (InterruptedException ignored) {
                }
            }
        });

        /* Shut down the sequencer, ensuring that any pending events are drained out too. */

        String output = "";
        try {
            assertTrue(sequencer.shutdown());
        } catch (InterruptedException e) {
            assertNull(e);
        } catch (PAssertionFailureException e) {
            System.out.println("Caught injected error");
            output = e.getMessage();
        }

        // then

        /* PAssertionFailureException message must be the same as the expected error event replay */

        List<PObserveEvent<?>> sortedInput = inputs.stream().sorted().collect(Collectors.toList());
        StringBuilder expected = new StringBuilder();
        expected.append("Assertion failure: [FAILED EVENT] ").append(errorEvent).append("\nSpec Error: Testing error injection")
                .append("\nEvent replay for key ").append(errorKey).append(": \n");
        for (PObserveEvent<?> p : sortedInput) {
            if (Objects.equals(p.getPartitionKey(), errorKey)) {
                expected.append(p).append(System.lineSeparator());
                if (p == errorEvent) {
                    break;
                }
            }
        }

        assertEquals(expected.toString(), output);
    }

    @ParameterizedTest
    @MethodSource("testSpecEventSequencers")
    public void exceptionMessageShouldContainEventReplayGivenEmptyKey(EventSequencer sequencer) throws NoSuchFieldException, IllegalAccessException {
        // given

        long delayMillis = 10;
        Random random = new Random();

        /* We begin with some events, out of order but all within `delayMillis` of each other.
         * We set the key to be empty string.
         */

        ArrayList<PObserveEvent<PEvent_idx>> inputs = new ArrayList<>();
        for (long batchId = 0; batchId < 100; batchId++) {
            ArrayList<PObserveEvent<PEvent_idx>> batch = new ArrayList<>();
            for (long i = 0; i < delayMillis; i++) {
                long ts = batchId * delayMillis + i;
                batch.add(new PObserveEvent<>("", ts, new PEvent_idx(ts)));
            }
            Collections.shuffle(batch);
            inputs.addAll(batch);
        }

        /* Injecting an error at random index */

        long index = random.nextInt(inputs.size() + 1);
        String errorKey = "";
        PObserveEvent<PEvent_idx> errorEvent = new PObserveEvent<>(errorKey, index, new PEvent_idx(-1));
        System.out.println("injecting error at index: " + index + ", key: " + errorKey);
        inputs.set((int) index, errorEvent);

        /* We process each "almost ordered" input into the sequencer, inducing a bit of latency by parking the
         * thread for a few ms, so we can ensure events are asynchronously processed in batches in this test rather
         * than "all at once".
         */

        inputs.forEach((pe) -> {
            sequencer.accept(pe);
            synchronized (this) {
                try {
                    wait(1);
                } catch (InterruptedException ignored) {
                }
            }
        });

        /* Shutting down the sequencer, ensuring that any pending events are drained out too. */

        String output = "";
        try {
            assertTrue(sequencer.shutdown());
        } catch (InterruptedException e) {
            assertNull(e);
        } catch (PAssertionFailureException e) {
            System.out.println("Caught injected error");
            output = e.getMessage();
        }

        // then

        /* Exception error event replay must be the same as the sequencer output */

        List<PObserveEvent<?>> sortedInput = inputs.stream().sorted().collect(Collectors.toList());
        StringBuilder expected = new StringBuilder();
        expected.append("Assertion failure: [FAILED EVENT] ").append(errorEvent).append("\nSpec Error: Testing error injection")
                .append("\nEvent replay for key ").append(errorKey).append(": \n");
        for (PObserveEvent<?> p : sortedInput) {
            if (Objects.equals(p.getPartitionKey(), errorKey)) {
                expected.append(p).append(System.lineSeparator());
                if (p == errorEvent) {
                    break;
                }
            }
        }

        assertEquals(expected.toString(), output);
    }
}
