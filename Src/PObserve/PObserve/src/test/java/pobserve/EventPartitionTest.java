package pobserve;

import pobserve.commons.PObserveEvent;
import pobserve.executor.PartitionEventStream;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.Random;
import org.junit.jupiter.api.Test;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;

public class EventPartitionTest {
    private boolean isCorrectlyPartitioned(Map<String, List<PObserveEvent>> eventMap) {
        return eventMap.keySet().stream().noneMatch(key -> eventMap.get(key).stream().anyMatch(event -> !event.getPartitionKey().equals(key)));
    }

    @Test
    public void testPartitioningWithSingleKey() {
        String key = "key1";
        List<PObserveEvent> events = new ArrayList<>();
        for (int i = 0; i < 1000; i++) {
            events.add(new PObserveEvent(key, System.currentTimeMillis()));
        }
        Map<String, List<PObserveEvent>> eventMap = PartitionEventStream.partitionByKey(events.stream());
        assertEquals(eventMap.size(), 1);
        assertTrue(isCorrectlyPartitioned(eventMap));
    }

    @Test
    public void testPartitioningWithMultipleKeys() {
        List<PObserveEvent> events = new ArrayList<>();
        Random random = new Random();
        for (int i = 0; i < 1000; i++) {
            String k = (i < 10) ? "key" + i : "key" + random.nextInt(10);
            events.add(new PObserveEvent(k, System.currentTimeMillis()));
        }
        Map<String, List<PObserveEvent>> eventMap = PartitionEventStream.partitionByKey(events.stream());
        assertEquals(eventMap.size(), 10);
        assertTrue(isCorrectlyPartitioned(eventMap));
    }

    @Test
    public void testPartitioningWithEmptyStream() {
        List<PObserveEvent> events = new ArrayList<>();
        Map<String, List<PObserveEvent>> eventMap = PartitionEventStream.partitionByKey(events.stream());
        assertEquals(eventMap.size(), 0);
    }


}
