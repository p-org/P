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
                // A single unparseable log line must NOT abort verification of the whole
                // stream. The stream is lazy, so throwing here would surface during the
                // downstream terminal operation and escape to PObserveExecutor.run()'s
                // blanket handler, which increments TOTAL_UNKNOWN_ERRORS and aborts the
                // entire run -- masquerading as a "no bugs found" result. ParseLogLine has
                // already recorded this failure (TOTAL_PARSER_ERRORS + TrackErrors), so we
                // skip only this line and continue parsing the rest of the stream.
                return Stream.<PObserveEvent<?>>empty();
            }
        });
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
