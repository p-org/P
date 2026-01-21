package pobserve.junit;

import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.TestInfo;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import pobserve.commons.PObserveEvent;
import pobserve.commons.Parser;
import pobserve.runtime.events.PEvent;
import pobserve.runtime.exceptions.PAssertionFailureException;

import java.util.List;

import java.util.function.Supplier;
import java.util.stream.Stream;



public class PObserveLogAppender {

    private static final Logger log = LoggerFactory.getLogger(PObserveLogAppender.class);
    private final Parser<? extends PEvent<?>> parser;
    private final EventSequencer sequencer;

    private final String[] delimiterLines;
    private final StringBuilder logAccumalator;
    private int delimiterLinesMatched;

    /**
     * Constructs a new PObserveLogAppender.
     *
     * @param parser custom parser for user specific log
     * @param monitorSuppliers suppliers for PSpec monitors
     */
    public PObserveLogAppender(Parser<? extends PEvent<?>> parser, List<Supplier<?>> monitorSuppliers) {
        this(parser, new WindowedEventSequencer(10L, monitorSuppliers));
    }

    /**
     * Constructs a new PObserveLogAppender.
     *
     * @param parser custom parser for user specific log
     * @param eventSequencer the event sequencer
     */
    public PObserveLogAppender(
            Parser<? extends PEvent<?>> parser,
            EventSequencer eventSequencer) {
        this.parser = parser;
        this.sequencer = eventSequencer;
        String logDelimiter = this.parser.getLogDelimiter();
        if (logDelimiter == null || logDelimiter.isEmpty() || logDelimiter.equals("\n")) {
            this.delimiterLines = new String[0];
            this.logAccumalator = null;
        } else {
            if (logDelimiter.startsWith("\n") && logDelimiter.endsWith("\n")) {
                logDelimiter = logDelimiter.substring(1, logDelimiter.length() - 1);
            }
            this.delimiterLines = logDelimiter.split("\n");
            this.logAccumalator = new StringBuilder();
        }
        delimiterLinesMatched = 0;
    }

    public void append(String line) {
        if (delimiterLines.length > 0) {
            // See if the current line matches the next possible delimiter
            if (line.equals(delimiterLines[delimiterLinesMatched])) {
                delimiterLinesMatched++;
                // Only process the log if all of the delimiters have been matched
                if (delimiterLinesMatched == delimiterLines.length) {
                    Stream<? extends PObserveEvent<? extends PEvent<?>>> events;
                    String accumulatedString = logAccumalator.toString();
                    logAccumalator.setLength(0);
                    delimiterLinesMatched = 0;

                    synchronized (parser) {
                        events = parser.apply(accumulatedString);
                    }
                    events.forEach(sequencer::accept);
                }
            } else {
                // We have not matched the next delimiter

                // Any partially matched delimiter lines need to be added to the accumulator
                for (int delimiterIndex = 0; delimiterIndex < delimiterLinesMatched; delimiterIndex++) {
                    logAccumalator.append(delimiterLines[delimiterIndex]);
                    logAccumalator.append("\n");
                }

                // The existing line needs to be evaluated against delimiters again
                delimiterLinesMatched = 0;
                if (line.equals(delimiterLines[0])) {
                    delimiterLinesMatched++;
                } else{
                    // If the line doesn't match a delimiter fragment, append it the accumulator
                    logAccumalator.append(line);
                    logAccumalator.append("\n");
                }
            }
        } else {
            Stream<? extends PObserveEvent<? extends PEvent<?>>> events;
            synchronized (parser) {
                events = parser.apply(line);
            }
            events.forEach(sequencer::accept);
        }
    }

    public void close() {
        try {
            sequencer.shutdown();
        } catch (InterruptedException e) {
            throw new RuntimeException(e);
        }
    }

    public void close(TestInfo testInfo) {
        try {
            if (testInfo.getTags().contains("ExpectFail")) {
                Assertions.assertThrows(PAssertionFailureException.class, () -> sequencer.shutdown());
            } else {
                sequencer.shutdown();
            }

        } catch (InterruptedException e) {
        }
    }
}
