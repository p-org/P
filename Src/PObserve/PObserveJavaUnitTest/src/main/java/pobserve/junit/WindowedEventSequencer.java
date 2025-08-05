package pobserve.junit;

import org.apache.logging.log4j.Logger;
import org.apache.logging.log4j.core.LoggerContext;

import pobserve.commons.PObserveEvent;
import pobserve.runtime.Monitor;
import pobserve.runtime.events.PEvent;

import java.time.Instant;
import java.util.ArrayList;
import java.util.List;
import java.util.Optional;
import java.util.concurrent.ScheduledThreadPoolExecutor;
import java.util.concurrent.TimeUnit;
import java.util.function.Supplier;
import java.util.stream.Collectors;

/**
 * An EventSequencer orders PObserveEvents by some maximum out-of-order delay, in a manner
 * similar to PObserve's Sorter. It consumes PObserveEvents and delays propagating them
 */
public class WindowedEventSequencer extends EventSequencer {
    private final Logger logger = LoggerContext.getContext().getLogger(this.getClass());

    /**
     * How long should an event be retained prior to processing?
     */
    private final long delayMillis;

    /**
     * The executor to handle processing events in batches of `delayMillis`.
     */
    private final ScheduledThreadPoolExecutor executor;
    /**
     * the greatest timestamp that has been pushed into the priority queue.
     */
    private Instant largestEnqueued;
    /**
     * The greatest timestamp that has been popped off the priority queue.
     */
    private Instant largestDequeued;
    /**
     * Is an exception pending to be thrown in the main thread?
     */
    private Optional<RuntimeException> pendingException;


    /**
     * Constructs a new EventSequencer.
     *
     * @param delayMillis The maximum out-of-orderliness
     * @param monitorSuppliers The monitors to be called by the sequencer
     */
    public WindowedEventSequencer(long delayMillis, List<Supplier<?>> monitorSuppliers) {
        super(monitorSuppliers);
        this.delayMillis = delayMillis;

        this.largestDequeued = Instant.ofEpochMilli(-1L);
        this.largestEnqueued = Instant.ofEpochMilli(-1L);

        this.executor = new ScheduledThreadPoolExecutor(1);
        this.pendingException = Optional.empty();

        this.executor.scheduleAtFixedRate(this::processAllPending, 0, delayMillis, TimeUnit.MILLISECONDS);
        logger.info(String.format("Calling handler loop every %dms.", delayMillis));
    }

    @Override
    public boolean canThrowOutOfOrderException() {
        return true;
    }

    /**
     * Returns, in order, all PObserveEvents such that the timestamp is lower than the last event seen,
     * taking into account the value of `delayMillis`.  Called on a regular basis by the executor.
     * May also set `pendingException` if the maximum out-of-orderliness is exceeded.`
     */
    private void processAllPending() {

        synchronized (this) {
            int processed = 0;
            if (pendingException.isPresent()) {
                return;
            }

            PObserveEvent<? extends PEvent<?>> candidate = queuedEvents.isEmpty() ? null : queuedEvents.get(0);

            while (candidate != null) {
                if (candidate.getTimestamp().compareTo(largestDequeued) < 0) {

                    // 1. Have we already processed an event with a lower time stamp? Uh oh!

                    pendingException = Optional.of(
                            new UnorderableEventException(candidate.getTimestamp(), largestDequeued));
                    break;
                }
                if ((largestEnqueued.toEpochMilli() - candidate.getTimestamp().toEpochMilli() < delayMillis)) {

                    // 2. Is the next event still potentially concurrent with others still to come?
                    // If so, push it off for later.

                    break;

                } else {

                    // 3. We have enqueued events sufficient far ahead that no further events should commute.

                    largestDequeued = candidate.getTimestamp();
                    candidate = queuedEvents.get(0);

                    // 4. Feed the event into matching key monitor

                    String key = candidate.getPartitionKey();
                    List<Monitor<?>> monitors = keyMonitor.get(key);
                    for (Monitor<?> monitor : monitors) {
                        checkNAccept(monitor, candidate);
                    }

                    processed++;
                    queuedEvents.remove(0);
                    candidate = queuedEvents.get(0);
                }
            }
            if (processed > 0) {
                logger.debug(String.format("Processed %d events.  Pending interval: (%d, %d].", processed,
                        largestDequeued, largestEnqueued));
            }
        }
    }

    /**
     * Enqueues an event to be processed either after an event with timestamp `pe.getTimestamp() + delayMillis` is
     * observed, or when shutdown occurs (in which case all pending events are processed)
     *
     * @param pe the input argument
     */
    @Override
    public void accept(PObserveEvent<? extends PEvent<?>> pe) {

        synchronized (this) {
            if (executor.isShutdown()) {
                throw new RuntimeException("Executor is shut down; cannot process any more events!");
            }
            else if (pendingException.isPresent()) {
                throw pendingException.get();
            }
            else if (pe.getTimestamp().toEpochMilli() < largestDequeued.toEpochMilli() - delayMillis) {
                throw new UnorderableEventException(pe.getTimestamp(), largestDequeued);
            }

            if (pe.getTimestamp().compareTo(largestEnqueued) > 0) {
                largestEnqueued = pe.getTimestamp();
            }
            queuedEvents.add((PObserveEvent<PEvent<?>>) pe);
            queuedEvents.sort(new PObserveEventComparator<>());
            String key = pe.getPartitionKey();
            if (keyMonitor.get(pe.getPartitionKey()) == null) {
                List<Monitor<?>> tempMonitors = new ArrayList<>();
                monitorSuppliers.forEach(supplier -> {
                    tempMonitors.add((Monitor<?>) supplier.get());
                });
                keyMonitor.put(key, tempMonitors);
            }
        }
    }

    /**
     * Shuts down the executor, allowing all pending operations to be processed.
     *
     * @return whether the executor successfully terminated.
     */
    public boolean shutdown() throws InterruptedException {
        boolean ret;

        // Shut down the executor, so we can't consume any _new_ events.
        synchronized (this) {
            executor.shutdown();
            ret = executor.awaitTermination(delayMillis, TimeUnit.MILLISECONDS);

            // After the executor is shut down, if an exception was thrown since the final call
            // to `processAllPending`, throw it now.
            if (pendingException.isPresent()) {
                throw pendingException.get();
            }
        }

        // Finally, whatever is remaining in the priority queue needs to be ordered and sent downstream.
        List<PObserveEvent<? extends PEvent<?>>> remainings =
                queuedEvents.stream().sorted().collect(Collectors.toList());

        for (PObserveEvent<? extends PEvent<?>> remaining : remainings) {
            String key = remaining.getPartitionKey();
            List<Monitor<?>> monitors = keyMonitor.get(key);
            for (Monitor<?> monitor : monitors) {
                checkNAccept(monitor, remaining);
            }
        }

        logger.debug(String.format("Processed %d events and shutting down.",
                remainings.size()));

        logger.info("PObserve successfully checked all events. Total number of events checked: " + totalEventsChecked);
        return ret;
    }

}
