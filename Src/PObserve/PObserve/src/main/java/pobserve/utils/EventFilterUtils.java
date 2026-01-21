package pobserve.utils;

import pobserve.commons.PObserveEvent;
import pobserve.runtime.Monitor;
import pobserve.runtime.events.PEvent;

import java.util.List;
import java.util.function.Consumer;

import static pobserve.config.PObserveConfig.getPObserveConfig;

/**
 * Utility class for filtering PObserve events.
 * Used by both file-based and socket-based processing.
 */
public class EventFilterUtils {

    /**
     * Filters the PObserve events that are observed by the specification.
     * Events are filtered based on:
     * 1. If the event is null, it's filtered out
     * 2. If the event type is not in the list of event types monitored by the specification, it's filtered out
     * 3. If keys filtering is enabled and the event's partition key is not in the list of keys, it's filtered out
     *
     * @param pObserveEvent The PObserveEvent to check
     * @return true if the event is observed by the specification, false otherwise
     */
    public static boolean filterBasedOnSpecObservation(PObserveEvent<?> pObserveEvent) {
        // Get a fresh instance of the consumer to check its event types
        Consumer<PEvent<?>> consumer = getPObserveConfig().getSpecificationSupplier().get();

        // Get the event types that the specification is interested in
        List<Class<? extends PEvent<?>>> eventTypes = ((Monitor<?>) consumer).getEventTypes();

        // Check if the event is observed by the specification
        return pObserveEvent.getEvent() != null &&
               eventTypes.contains(pObserveEvent.getEvent().getClass()) &&
               (getPObserveConfig().getKeys().size() == 0 ||
                getPObserveConfig().getKeys().contains(pObserveEvent.getPartitionKey()));
    }
}
