package pobserve.junit;

import org.apache.logging.log4j.Logger;
import org.apache.logging.log4j.core.LoggerContext;

import pobserve.commons.PObserveEvent;
import pobserve.runtime.Monitor;
import pobserve.runtime.events.PEvent;
import pobserve.runtime.exceptions.PAssertionFailureException;
import pobserve.runtime.exceptions.UnhandledEventException;

import java.io.Serializable;
import java.util.ArrayList;
import java.util.Comparator;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.function.Consumer;
import java.util.function.Supplier;

abstract public class EventSequencer implements Consumer<PObserveEvent<? extends PEvent<?>>> {
    public static class PObserveEventComparator<T extends PEvent<?>> implements Comparator<PObserveEvent<T>>,
            Serializable {
        @Override
        public int compare(PObserveEvent<T> event1, PObserveEvent<T> event2) {
            // Compare events based on some criteria, for example, timestamps
            return event1.getTimestamp().compareTo(event2.getTimestamp());
        }
    }

    private final Logger logger = LoggerContext.getContext().getLogger(this.getClass());

    /**
     * This contains all events that may be concurrent with a yet-to-arrive event.
     */
    protected final List<PObserveEvent<PEvent<?>>> queuedEvents;
    /**
     * Whence the sorted events should be sent.
     */
    protected final Map<String, List<Monitor<?>>> keyMonitor;
    /**
     * Provides the monitors specified by user
     */
    protected final List<Supplier<?>> monitorSuppliers;
    /**
     * Stores event replay in their matching key
     */
    protected final Map<String, List<String>> replayWindowMap;
    protected boolean isActive;
    protected int totalEventsChecked;

    /**
     * Construct the common EventSequencer
     *
     * @param monitorSuppliers the monitor suppliers to be called by this sequencer
     */
    public EventSequencer(List<Supplier<?>> monitorSuppliers) {
        this.queuedEvents = new ArrayList<>();
        this.keyMonitor = new HashMap<>();
        this.monitorSuppliers = monitorSuppliers;
        replayWindowMap = new HashMap<>();
        isActive = true;
        totalEventsChecked = 0;
    }

    abstract public boolean canThrowOutOfOrderException();

    abstract public boolean shutdown() throws InterruptedException;

    /**
     * Checks whether the monitor accepts event, and sends the event to monitor.
     *
     * @param monitor the monitor to check against
     * @param event the event to check
     */
    protected void checkNAccept(Monitor<?> monitor, PObserveEvent<? extends PEvent<?>> event) {
        if (monitor == null || event == null) {
            return;
        }
        List<Class<? extends PEvent<?>>> monitoredTypes = monitor.getEventTypes();
        Class<? extends PEvent<?>> eventType = (Class<? extends PEvent<?>>) event.getEvent().getClass();
        String key = event.getPartitionKey();
        if (monitoredTypes.contains(eventType)) {
            String errorMsg = "";
            boolean errorCaught = false;
            StackTraceElement[] stackTrace = null;
            try {
                updateReplayWindow(key, event.toString());
                totalEventsChecked++;
                // Sending event to monitor
                monitor.accept(event.getEvent());
            } catch (UnhandledEventException e) {
                errorCaught = true;
                logger.error("Current state doesn't accept event: " + e.getMessage());
                errorMsg = String.format("[FAILED EVENT] %s%nCurrent state doesn't accept event: %s%n",
                        event, e.getMessage().replace("Assertion failure: ", ""), key, listToString(key));
                stackTrace = e.getStackTrace();
            } catch (PAssertionFailureException e) {
                errorCaught = true;
                errorMsg = String.format("[FAILED EVENT] %s%nSpec Error: %s%n",
                        event, e.getMessage().replace("Assertion failure: ", ""));
                stackTrace = e.getStackTrace();
            } catch (Exception e) {
                errorCaught = true;
                errorMsg = String.format("[FAILED EVENT] %s%nUnknown Error: %s%n",
                        event, e.getMessage().replace("Assertion failure: ", ""));
                stackTrace = e.getStackTrace();
            } finally {
                if (errorCaught) {
                    errorMsg += String.format("Event replay for key %s: %n%s", key, listToString(key));
                    PAssertionFailureException error = new PAssertionFailureException(errorMsg);
                    error.setStackTrace(stackTrace);
                    throw error;
                }
            }
        }
    }

    /**
     *  Stores event string into event replay mapped by their keys
     */
    private void updateReplayWindow(String key, String eventString) {
        List<String> replayWindow;
        if (replayWindowMap.containsKey(key)) {
            replayWindow = replayWindowMap.get(key);
        } else {
            replayWindow = new ArrayList<>();
        }
        if (!replayWindow.contains(eventString)) {
            replayWindow.add(eventString);
        }
        replayWindowMap.put(key, replayWindow);
    }

    /**
     * Converts the error key event replay set to a string
     *
     * @param key Error key name
     */

    private String listToString(String key) {
        StringBuilder res = new StringBuilder();
        for (String eventString: replayWindowMap.get(key)) {
            res.append(eventString).append("\n");
        }
        return res.toString();
    }
}
