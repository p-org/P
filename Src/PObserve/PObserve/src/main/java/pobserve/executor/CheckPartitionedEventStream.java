package pobserve.executor;

import pobserve.commons.PObserveEvent;
import pobserve.commons.exceptions.PObserveEventOutOfOrderException;
import pobserve.metrics.MetricConstants;
import pobserve.report.PObserveError;
import pobserve.report.TrackErrors;
import pobserve.runtime.events.PEvent;
import pobserve.runtime.exceptions.PAssertionFailureException;

import java.time.Instant;
import java.util.Comparator;
import java.util.function.Consumer;
import java.util.stream.Stream;

import static pobserve.config.PObserveConfig.getPObserveConfig;
import static pobserve.metrics.PObserveMetrics.getPObserveMetrics;

/**
 * CheckPartitionedEventStream class helps run specification check on sorted PObserve event stream
 */
public class CheckPartitionedEventStream {
    /**
     * Sorted PObserve event stream based on event timestamp
     * @param eventStream PObserve event stream
     * @return sorted PObserve event stream
     */
    public static Stream<PObserveEvent> sortByTimeStamp(Stream<PObserveEvent> eventStream) {
        return eventStream.sorted(Comparator.comparing(PObserveEvent::getTimestamp));
    }

    /**
     * Runs PObserve monitor on sorted PObserve event stream to check for specification violation
     * @param key PObserve partition key
     * @param events PObserve event stream
     */
    public static void check(String key, Stream<PObserveEvent> events) {
        Consumer<PEvent<?>> consumer = getPObserveConfig().getSpecificationSupplier().get();
        PObserveReplayEvents replayEvents = new PObserveReplayEvents(getPObserveConfig().getReplayWindowSize(), key);
        var sortedEventStream = events; // lets assume the stream is sorted
        if (!getPObserveConfig().isAssumeInputFilesAreSorted()) {
            // create a sorted event stream
            sortedEventStream = sortByTimeStamp(events);
        }

        // invoke the monitor on each event in order
        var prevTimestamp = new Object() {
            Instant timestamp = Instant.ofEpochMilli(-1L);
        };
        try {
            sortedEventStream.forEachOrdered(ev -> {
                replayEvents.addEvent(ev);
                if (ev.getTimestamp().compareTo(prevTimestamp.timestamp) < 0) {
                    String errorMsg = "Timestamp of last processed event: " + prevTimestamp.timestamp.toString() + "\n";
                    errorMsg += "Timestamp of current event: " + ev.getTimestamp().toString() + "\n";
                    errorMsg += "Error event details: " + ev;
                    throw new PObserveEventOutOfOrderException("Encountered an out of order event::\n" + errorMsg);
                }
                prevTimestamp.timestamp = ev.getTimestamp();
                long startTime = System.currentTimeMillis();
                consumer.accept((PEvent<?>) ev.getEvent());
                long endTime = System.currentTimeMillis();
                getPObserveMetrics().updateEventMetrics(ev.getEvent().getClass().getSimpleName(), (endTime - startTime));
            });
            getPObserveMetrics().addMetric(MetricConstants.TOTAL_VERIFIED_KEYS, 1);
        } catch (PAssertionFailureException e) {
            getPObserveMetrics().addMetric(MetricConstants.TOTAL_SPEC_ERRORS, 1);
            TrackErrors.addError(new PObserveError(e, replayEvents));
        } catch (PObserveEventOutOfOrderException e) {
            getPObserveMetrics().addMetric(MetricConstants.TOTAL_EVENT_OUT_OF_ORDER_ERRORS, 1);
            TrackErrors.addError(new PObserveError(e, replayEvents));
        } catch (Exception e) {
            getPObserveMetrics().addMetric(MetricConstants.TOTAL_UNKNOWN_ERRORS, 1);
            TrackErrors.addError(new PObserveError(e, replayEvents));
        }
    }
}
