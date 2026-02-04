package pobserve.source.socket;

import pobserve.commons.PObserveEvent;
import pobserve.config.PObserveConfig;
import pobserve.executor.PObserveExecutor;
import pobserve.executor.PObserveReplayEvents;
import pobserve.logger.PObserveLogger;
import pobserve.metrics.MetricConstants;
import pobserve.metrics.PObserveMetrics;
import pobserve.report.TrackErrors;
import pobserve.runtime.Monitor;
import pobserve.runtime.events.PEvent;
import pobserve.source.socket.models.Response;
import pobserve.source.socket.utils.ErrorHandler;
import pobserve.utils.EventFilterUtils;
import pobserve.utils.SerializationUtils;

import java.time.Instant;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.function.Consumer;
import java.util.function.Supplier;

import static pobserve.config.PObserveConfig.getPObserveConfig;
import static pobserve.metrics.PObserveMetrics.getPObserveMetrics;

/**
 * Processes socket commands and interacts with the PObserve execution engine.
 */
public class SocketCommandProcessor {
    // Command constants
    public static final String CMD_START = "START";
    public static final String CMD_RESET = "RESET";
    public static final String CMD_ACCEPT = "ACCEPT";
    public static final String CMD_STOP = "STOP";

    private PObserveExecutor executor;
    private SocketLineParser lineParser;
    private boolean initialized = false;

    // Track errors for reporting
    private final List<Exception> capturedExceptions = new ArrayList<>();

    // Keep track of the consumer, supplier, and replay window
    private Supplier<Consumer<PEvent<?>>> consumerSupplier;
    private Consumer<PEvent<?>> consumer;
    private PObserveReplayEvents replayEvents;
    private Instant prevTimestamp = Instant.ofEpochMilli(-1L);

    /**
     * Handles the START command.
     *
     * @return Response object
     */
    public Response handleStartCommand() {
        try {
            PObserveLogger.info("Starting monitoring session");
            getPObserveMetrics().setStartTime(System.currentTimeMillis());

            // Clear any previous captured exceptions
            capturedExceptions.clear();
            prevTimestamp = Instant.ofEpochMilli(-1L);

            // Initialize parser and executor on first start
            if (!initialized) {
                PObserveConfig.validateAndLoadPObserveConfig();
                lineParser = new SocketLineParser(getPObserveConfig().getParserSupplier());
                executor = new PObserveExecutor();
                initialized = true;
            }

            // Store the supplier reference and initialize the consumer and replay events
            consumerSupplier = getPObserveConfig().getSpecificationSupplier();
            consumer = consumerSupplier.get();
            replayEvents = new PObserveReplayEvents(getPObserveConfig().getReplayWindowSize(), "socket");

            return new Response("SUCCESS", "Monitoring started", null);
        } catch (Exception e) {
            ErrorHandler.handleError(e, MetricConstants.TOTAL_UNKNOWN_ERRORS, capturedExceptions);
            return new Response("ERROR", e.getMessage(), null);
        }
    }

    /**
     * Handles the RESET command.
     *
     * @return Response object
     */
    public Response handleResetCommand() {
        try {
            if (!initialized) {
                return new Response("ERROR", "Monitoring not started. Please issue START command first.", null);
            }

            PObserveLogger.info("Resetting monitoring state");

            // Reset timestamp
            prevTimestamp = Instant.ofEpochMilli(-1L);

            // Reset metrics by resetting each counter to 0
            PObserveMetrics metrics = getPObserveMetrics();
            metrics.getMetricsMap().forEach((name, value) -> value.set(0));
            metrics.setStartTime(System.currentTimeMillis());

            // Reset error tracking
            TrackErrors.reset();
            capturedExceptions.clear();

            // Reinitialize executor
            executor = new PObserveExecutor();

            // Reinitialize consumer and replay events for direct event processing
            consumer = consumerSupplier.get();
            replayEvents = new PObserveReplayEvents(getPObserveConfig().getReplayWindowSize(), "socket");

            return new Response("SUCCESS", "Monitoring state reset", null);
        } catch (Exception e) {
            ErrorHandler.handleError(e, MetricConstants.TOTAL_UNKNOWN_ERRORS, capturedExceptions);
            return new Response("ERROR", e.getMessage(), null);
        }
    }

    /**
     * Handles the ACCEPT command.
     *
     * @param logLine The log line to process
     * @return Response object
     */
    public Response handleAcceptCommand(String logLine) {
        try {
            if (!initialized) {
                return new Response("ERROR", "Monitoring not started. Please issue START command first.", null);
            }

            // Parse log line and convert to PObserveEvent
            PObserveEvent<?> event = lineParser.parseLine(logLine);
            if (event == null) {
                return new Response("ERROR", "Failed to parse log line", null);
            }

            // Filter events based on spec observation, just like in file-based processing
            if (!EventFilterUtils.filterBasedOnSpecObservation(event)) {
                // If the event is not observed by the specification, ignore it but don't report an error
                PObserveLogger.info("Ignoring event not observed by specification: " + event.getEvent().getClass().getSimpleName());
                return new Response("SUCCESS", "Log line processed (event ignored - not observed by specification)", null);
            }

            try {
                // Store a reference to the current timestamp
                Instant originalTimestamp = prevTimestamp;

                // Create a backup of the current consumer state
                Monitor<?> consumerBackup = createConsumerBackup();

                try {
                    // Process the event (this contains the code from lines 234-248)
                    processEvent(event);

                    return new Response("SUCCESS", "Log line processed", null);
                } catch (Exception e) {
                    // Restore state in case of error
                    restoreFromBackup(consumerBackup, originalTimestamp);

                    // Re-throw the exception
                    throw e;
                }
            } catch (pobserve.runtime.exceptions.PAssertionFailureException e) {
                ErrorHandler.handleErrorWithReplay(e, MetricConstants.TOTAL_SPEC_ERRORS, capturedExceptions, replayEvents);
                return new Response("ERROR", e.getMessage(), null);
            } catch (pobserve.commons.exceptions.PObserveEventOutOfOrderException e) {
                ErrorHandler.handleErrorWithReplay(e, MetricConstants.TOTAL_EVENT_OUT_OF_ORDER_ERRORS, capturedExceptions, replayEvents);
                return new Response("ERROR", e.getMessage(), null);
            } catch (Exception e) {
                ErrorHandler.handleErrorWithReplay(e, MetricConstants.TOTAL_UNKNOWN_ERRORS, capturedExceptions, replayEvents);
                return new Response("ERROR", e.getMessage(), null);
            }
        } catch (Exception e) {
            // This catches exceptions from parseLine or other parts outside the event processing
            ErrorHandler.handleError(e, MetricConstants.TOTAL_UNKNOWN_ERRORS, capturedExceptions);
            return new Response("ERROR", e.getMessage(), null);
        }
    }

    /**
     * Creates a backup of the consumer state if possible.
     *
     * @return A backup of the consumer state or null if backup creation failed
     */
    private Monitor<?> createConsumerBackup() {
        if (!(consumer instanceof Monitor<?>)) {
            return null;
        }

        try {

            Monitor<?> consumerBackup = SerializationUtils.cloneMonitor((Monitor<?>) consumer);
            PObserveLogger.info("Successfully created backup of monitor state");
            return consumerBackup;
        } catch (Exception e) {
            PObserveLogger.error("Unable to create backup of monitor state: " + e.getMessage());
            // Log more details about the errors to help diagnose serialization issues
            if (e.getCause() != null) {
                PObserveLogger.error("Caused by: " + e.getCause().toString());
            }
            if (consumer != null) {
                PObserveLogger.info("Consumer class: " + consumer.getClass().getName());
                PObserveLogger.info("Consumer classloader: " + (consumer.getClass().getClassLoader() != null ?
                    consumer.getClass().getClassLoader().toString() : "null"));
            }
            // Continue without backup
            return null;
        }
    }

    /**
     * Processes the event, including timestamp validation and event acceptance.
     * This method contains the code extracted from lines 234-248 in the original method.
     *
     * @param event The event to process
     * @throws pobserve.commons.exceptions.PObserveEventOutOfOrderException if events are out of order
     */
    private void processEvent(PObserveEvent<?> event) throws Exception {
        // Add event to the replay window for potential error reporting
        replayEvents.addEvent(event);

        // Check if the events are out of order
        if (event.getTimestamp().compareTo(prevTimestamp) < 0) {
            String errorMsg = "Timestamp of last processed event: " + prevTimestamp.toString() + "\n";
            errorMsg += "Timestamp of current event: " + event.getTimestamp().toString() + "\n";
            errorMsg += "Error event details: " + event;
            throw new pobserve.commons.exceptions.PObserveEventOutOfOrderException(
                "Encountered an out of order event::\n" + errorMsg
            );
        }

        // Update prevTimestamp for the next check
        prevTimestamp = event.getTimestamp();

        // Process the event with the current consumer
        long startTime = System.currentTimeMillis();
        consumer.accept((PEvent<?>) event.getEvent());
        long endTime = System.currentTimeMillis();

        // Update metrics
        getPObserveMetrics().updateEventMetrics(
            event.getEvent().getClass().getSimpleName(),
            (endTime - startTime)
        );
    }

    /**
     * Restores the consumer and timestamp from backup after an error.
     *
     * @param consumerBackup The consumer backup to restore from
     * @param originalTimestamp The original timestamp to restore
     */
    private void restoreFromBackup(Monitor<?> consumerBackup, Instant originalTimestamp) {
        // Restore the monitor from backup if available
        if (consumerBackup != null) {
            consumer = (Consumer<PEvent<?>>) consumerBackup;
            PObserveLogger.info("Restored monitor from backup after error");
        } else {
            // If no backup is available, create a fresh consumer
            consumer = consumerSupplier.get();
            PObserveLogger.info("Created fresh consumer after error (no backup was available)");
        }

        // Restore the timestamp
        prevTimestamp = originalTimestamp;
    }

    /**
     * Handles the STOP command.
     *
     * @return Response object with summary
     */
    public Response handleStopCommand() {
        try {
            if (!initialized) {
                return new Response("ERROR", "Monitoring not started. Please issue START command first.", null);
            }

            PObserveLogger.info("Stopping monitoring and generating summary");

            // Set the end time for metrics
            getPObserveMetrics().setEndTime(System.currentTimeMillis());

            // Generate summary data
            Map<String, Object> summaryData = generateSummaryData();

            return new Response("SUCCESS", "Monitoring stopped", summaryData);
        } catch (Exception e) {
            ErrorHandler.handleError(e, MetricConstants.TOTAL_UNKNOWN_ERRORS, capturedExceptions);
            return new Response("ERROR", e.getMessage(), null);
        }
    }

    /**
     * Generates a summary data map for the monitoring session.
     *
     * @return Summary data as a map
     */
    private Map<String, Object> generateSummaryData() {
        PObserveMetrics metrics = getPObserveMetrics();
        int errorCount = metrics.getMetricsMap().get(MetricConstants.TOTAL_SPEC_ERRORS).get()
                + metrics.getMetricsMap().get(MetricConstants.TOTAL_EVENT_OUT_OF_ORDER_ERRORS).get()
                + metrics.getMetricsMap().get(MetricConstants.TOTAL_UNKNOWN_ERRORS).get();

        Map<String, Object> summaryData = new HashMap<>();
        summaryData.put("errorCount", errorCount);

        // Add execution time
        long execTime = metrics.getEndTime() - metrics.getStartTime();
        summaryData.put("executionTimeMs", execTime);

        // Add metrics
        Map<String, Integer> metricsMap = new HashMap<>();
        metrics.getMetricsMap().forEach((key, value) ->
            metricsMap.put(key, value.get())
        );
        summaryData.put("metrics", metricsMap);

        // Add errors if any
        if (!capturedExceptions.isEmpty()) {
            List<String> errors = new ArrayList<>();
            for (Exception e : capturedExceptions) {
                errors.add(e.getMessage());
            }
            summaryData.put("errors", errors);
        }

        return summaryData;
    }

    /**
     * Gets the SocketLineParser instance.
     *
     * @return The line parser
     */
    public SocketLineParser getLineParser() {
        return lineParser;
    }

    /**
     * Checks if the processor has been initialized.
     *
     * @return true if initialized, false otherwise
     */
    public boolean isInitialized() {
        return initialized;
    }
}
