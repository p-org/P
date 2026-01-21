package pobserve.executor;

import pobserve.commons.PObserveEvent;

import java.util.List;
import java.util.concurrent.ConcurrentMap;
import java.util.stream.Collectors;
import java.util.stream.Stream;

/**
 * PartitionEventStream class helps partition a PObserve event stream
 */
public class PartitionEventStream {
    /**
     * Partitions PObserve event stream based on partitionKey
     * @param parsedEvents stream of PObserve events
     * @return Map of partitioned PObserve events (key: "key", value: stream of PObserve events with partitionKey "key")
     */
    public static ConcurrentMap<String, List<PObserveEvent>> partitionByKey(Stream<? extends PObserveEvent> parsedEvents) {
        return parsedEvents.collect(Collectors.groupingByConcurrent((PObserveEvent::getPartitionKey)));
    }
}
