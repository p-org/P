package pobserve.source.socket.utils;

import pobserve.logger.PObserveLogger;
import pobserve.metrics.MetricConstants;
import pobserve.report.PObserveError;
import pobserve.report.TrackErrors;

import java.util.List;

import static pobserve.metrics.PObserveMetrics.getPObserveMetrics;

/**
 * Utility class for handling errors in the PObserve socket server.
 * Centralizes error handling logic to reduce duplication.
 */
public class ErrorHandler {

    /**
     * Converts stack trace to string for logging
     *
     * @param e The exception
     * @return String representation of stack trace
     */
    public static String getStackTraceAsString(Exception e) {
        StringBuilder sb = new StringBuilder();
        for (StackTraceElement element : e.getStackTrace()) {
            sb.append(element.toString()).append("\n");
        }
        return sb.toString();
    }

    /**
     * Logs an error and updates metrics and error tracking.
     *
     * @param e The exception to handle
     * @param metricKey The metric key to increment, or null to use TOTAL_UNKNOWN_ERRORS
     * @param exceptionList Optional list to add the exception to for later reporting
     */
    public static void handleError(Exception e, String metricKey, List<Exception> exceptionList) {
        // Log the error
        PObserveLogger.error(e.getMessage() + "\n" + getStackTraceAsString(e));

        // Update metrics
        String metric = (metricKey != null) ? metricKey : MetricConstants.TOTAL_UNKNOWN_ERRORS;
        getPObserveMetrics().addMetric(metric, 1);

        // Update error tracking
        TrackErrors.addError(new PObserveError(e));

        // Add to exception list if provided
        if (exceptionList != null) {
            exceptionList.add(e);
        }
    }

    /**
     * Logs an error with associated replay events and updates metrics and error tracking.
     *
     * @param e The exception to handle
     * @param metricKey The metric key to increment, or null to use TOTAL_UNKNOWN_ERRORS
     * @param exceptionList Optional list to add the exception to for later reporting
     * @param replayEvents The replay events object to associate with the error
     */
    public static void handleErrorWithReplay(Exception e, String metricKey, List<Exception> exceptionList,
            pobserve.executor.PObserveReplayEvents replayEvents) {
        // Log the error
        PObserveLogger.error(e.getMessage() + "\n" + getStackTraceAsString(e));

        // Update metrics
        String metric = (metricKey != null) ? metricKey : MetricConstants.TOTAL_UNKNOWN_ERRORS;
        getPObserveMetrics().addMetric(metric, 1);

        // Update error tracking with replay events
        TrackErrors.addError(new PObserveError(e, replayEvents));

        // Add to exception list if provided
        if (exceptionList != null) {
            exceptionList.add(e);
        }
    }
}
