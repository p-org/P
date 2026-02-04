package pobserve;

import pobserve.commons.PObserveEvent;
import pobserve.executor.CheckPartitionedEventStream;

import java.util.ArrayList;
import java.util.Collections;
import java.util.List;
import java.util.Random;
import java.util.stream.Collectors;
import org.junit.jupiter.api.Test;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;

public class EventSortTest {

    @Test
    public void testSortingWithDifferentTimestamps() {
        List<PObserveEvent> events = new ArrayList<>();
        long timestamp = System.currentTimeMillis();
        for (int i = 0; i < 1000; i++) {
            events.add(new PObserveEvent("key", timestamp));
            timestamp += 1;
        }
        Collections.shuffle(events);
        List<PObserveEvent> sortedEvents = CheckPartitionedEventStream.sortByTimeStamp(events.stream()).collect(Collectors.toList());
        for (int i = 1; i < sortedEvents.size(); i++) {
            assertTrue(sortedEvents.get(i).getTimestamp().compareTo(sortedEvents.get(i - 1).getTimestamp()) > 0);
        }
    }

    @Test
    public void testSortingWithDuplicateTimestamp() {
        List<PObserveEvent> events = new ArrayList<>();
        long timestamp = System.currentTimeMillis();
        Random random = new Random();
        for (int i = 0; i < 1000; i++) {
            events.add(new PObserveEvent("key", timestamp));
            if (random.nextBoolean()) {
                timestamp += 1;
            }
        }
        Collections.shuffle(events);
        List<PObserveEvent> sortedEvents = CheckPartitionedEventStream.sortByTimeStamp(events.stream()).collect(Collectors.toList());
        for (int i = 1; i < sortedEvents.size(); i++) {
            assertTrue(sortedEvents.get(i).getTimestamp().compareTo(sortedEvents.get(i - 1).getTimestamp()) >= 0);
        }
    }

    @Test
    public void testSortingWithEmptyStream() {
        List<PObserveEvent> events = new ArrayList<>();
        List<PObserveEvent> sortedEvents = CheckPartitionedEventStream.sortByTimeStamp(events.stream()).collect(Collectors.toList());
        assertEquals(sortedEvents.size(), 0);
    }
}
