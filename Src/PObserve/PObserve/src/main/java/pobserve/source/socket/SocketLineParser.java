package pobserve.source.socket;

import pobserve.commons.PObserveEvent;
import pobserve.commons.Parser;
import pobserve.commons.exceptions.PObserveLogParsingException;
import pobserve.logger.PObserveLogger;
import pobserve.metrics.MetricConstants;
import pobserve.report.PObserveError;
import pobserve.report.TrackErrors;

import java.util.stream.Stream;

import static pobserve.metrics.PObserveMetrics.getPObserveMetrics;

/**
 * Parser for socket input lines.
 * This class adapts the PObserve parser to handle individual lines from socket input.
 */
public class SocketLineParser {
    private final Parser<?> parser;

    /**
     * Creates a new socket line parser.
     *
     * @param parser The parser to use for parsing log lines
     */
    public SocketLineParser(Parser<?> parser) {
        this.parser = parser;
    }

    /**
     * Parses a log line and returns a PObserveEvent.
     *
     * @param logLine The log line to parse
     * @return PObserveEvent or null if parsing fails
     */
    public PObserveEvent<?> parseLine(String logLine) {
        try {
            // Apply the parser to the log line and get a stream of events
            Stream<?> eventStream = parser.apply(logLine);

            // Take the first event (if any)
            return eventStream
                    .findFirst()
                    .map(event -> (PObserveEvent<?>) event)
                    .orElse(null);
        } catch (Exception e) {
            getPObserveMetrics().addMetric(MetricConstants.TOTAL_PARSER_ERRORS, 1);
            PObserveLogger.error("Parser Exception::");
            PObserveLogger.error("Exception occurred while parsing log line: " + logLine);
            TrackErrors.addError(new PObserveError(new PObserveLogParsingException((String) logLine)));
            return null;
        }
    }
}
