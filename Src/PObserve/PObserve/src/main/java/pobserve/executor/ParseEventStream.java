package pobserve.executor;

import pobserve.commons.PObserveEvent;
import pobserve.commons.exceptions.PObserveLogParsingException;
import pobserve.logger.PObserveLogger;
import pobserve.metrics.MetricConstants;
import pobserve.report.PObserveError;
import pobserve.report.TrackErrors;
import pobserve.utils.EventFilterUtils;

import java.util.stream.Stream;

import static pobserve.config.PObserveConfig.getPObserveConfig;
import static pobserve.metrics.PObserveMetrics.getPObserveMetrics;

/**
 * ParseEventStream class helps parse logs and filter PObserve events
 */
public class ParseEventStream {

    /**
     * Parses log lines to PObserve events
     * @param inputStream stream of log lines
     * @return stream of PObserve events
     * @throws Exception when parsing fails for any of the log lines
     */
    public static Stream<? extends PObserveEvent<?>> parseToPObserveEvents(Stream<Object> inputStream) throws Exception {
        try {
            return inputStream.flatMap(logLine -> {
                try {
                    return ParseLogLine(logLine).filter(event -> {
                        boolean keep = EventFilterUtils.filterBasedOnSpecObservation(event);
                        if (keep) {
                            getPObserveMetrics().addMetric(MetricConstants.TOTAL_EVENTS_READ, 1);
                        }
                        return keep;
                    });
                } catch (PObserveLogParsingException e) {
                    throw new RuntimeException(e);
                }
            });
        } catch (RuntimeException ignored) {
            return Stream.empty();
        }
    }

    /**
     * Parses a log line to get PObserve events
     * @param log log line object
     * @return stream of PObserve events generated from the log line
     * @throws PObserveLogParsingException when there is an exception while parsing the log line
     */
    private static Stream<? extends PObserveEvent<?>> ParseLogLine(Object log) throws PObserveLogParsingException {
        Stream<? extends PObserveEvent<?>> parsedEvents = Stream.empty();
        try {
            parsedEvents = getPObserveConfig().getParserSupplier().apply(log);
        } catch (Exception e) {
            getPObserveMetrics().addMetric(MetricConstants.TOTAL_PARSER_ERRORS, 1);
            PObserveLogger.error("Parser Exception::");
            PObserveLogger.error("Exception occurred while parsing log line: " + log);
            TrackErrors.addError(new PObserveError(new PObserveLogParsingException((String) log)));
            throw new PObserveLogParsingException((String) log);
        }
        return parsedEvents;
    }
}
