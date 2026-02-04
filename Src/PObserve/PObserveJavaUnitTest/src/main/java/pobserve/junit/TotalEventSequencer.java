package pobserve.junit;

import org.apache.logging.log4j.Logger;
import org.apache.logging.log4j.core.LoggerContext;

import pobserve.commons.PObserveEvent;
import pobserve.runtime.Monitor;
import pobserve.runtime.events.PEvent;

import java.util.ArrayList;
import java.util.List;
import java.util.function.Supplier;

/**
 * An EventSequencer orders PObserveEvents by capturing them, sorting them, and then processing them at the end
 */
public class TotalEventSequencer extends EventSequencer {
    private final Logger logger = LoggerContext.getContext().getLogger(this.getClass());


    /**
     * Constructs a new TotalEventSequencer.
     *
     * @param monitorSuppliers list of monitor suppliers
     */
    public TotalEventSequencer(List<Supplier<?>> monitorSuppliers) {
        super(monitorSuppliers);

    }

    @Override
    public boolean canThrowOutOfOrderException() {
        return false;
    }

    /**
     * Enqueues an event to be processed when shutdown occurs
     * Enqueues an event to be processed when shutdown occurs
     *
     * @param pe the input argument
     */
    @Override
    public void accept(PObserveEvent<? extends PEvent<?>> pe) {
        synchronized (this) {
            if (isActive) {
                queuedEvents.add((PObserveEvent<PEvent<?>>) pe);
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
    }

    /**
     * Shuts down the executor, allowing all pending operations to be processed.
     *
     * @return whether the executor successfully terminated.
     */
    public boolean shutdown() {
        // Shut down the executor, so we can't consume any _new_ events.
        synchronized (this) {
            isActive = false;
        }

        // Important to use a stable sort
        queuedEvents.sort(new PObserveEventComparator<>());
        for (PObserveEvent<? extends PEvent<?>> remaining : queuedEvents) {
            String key = remaining.getPartitionKey();
            List<Monitor<?>> monitors = keyMonitor.get(key);
            for (Monitor<?> monitor : monitors) {
                checkNAccept(monitor, remaining);
            }
        }

        logger.debug(String.format("Processed %d events and shutting down.",
                queuedEvents.size()));

        logger.info("PObserve successfully checked all events. Total number of events checked: " + totalEventsChecked);
        return true;
    }
}
