package pobserve.junit;

import pobserve.commons.PObserveEvent;
import pobserve.commons.Parser;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;
import pobserve.runtime.events.PEvent;

import java.util.List;
import java.util.stream.Collectors;
import java.util.stream.Stream;

public class PObserveLogAppenderTests {
    // The log lines used by all of the tests are constructed so that for all
    // of the tested delimiters, each set of delimited lines has the same number of lines:
    // For the newline (and null and empty), each group has 1 line
    // For the single line delimiter, each group has 3 lines
    // For the dual line delimiter, each group has 2 lines
    private static List<String> commonLogLines = List.of(
            "Log Line 1",
            "Delimiter - but not really",
            "Optional Delimiter",
            "Delimiter",
            "Log Line 2",
            "Log Line 3",
            "Optional Delimiter",
            "Delimiter",
            "Optional Delimiter",
            "Log Line 7",
            "Optional Delimiter",
            "Delimiter",
            "Log Line 8",
            "Optional Delimiter",
            "Optional Delimiter",
            "Delimiter"
    );

    @Test
    public void testNullDelimiter() throws Exception {
        runTest(null, commonLogLines.size(), 1);
    }

    @Test
    public void testEmptyDelimiter() throws Exception {
        runTest("", commonLogLines.size(), 1);
    }

    @Test
    public void testNewlineDelimiter() throws Exception {
        runTest("\n", commonLogLines.size(), 1);
    }

    @Test
    public void testSingleLineDelimiter() throws Exception {
        runTest("\nDelimiter\n", 4, 3);
    }

    @Test
    public void testTwoLineDelimiter() throws Exception {
        runTest("\nOptional Delimiter\nDelimiter\n", 4, 2);
    }

    private void runTest(String delimiter, int eventsExpected, int linesExpected) throws Exception {
        runGoodParserTest(delimiter, eventsExpected, linesExpected);

        runBadParserTest(delimiter, eventsExpected);
    }

    private void runGoodParserTest(String delimiter, int eventsExpected, int linesExpected) throws Exception {
        TestParser parser = new TestParser(delimiter);
        TestEventSequencer sequencer = new TestEventSequencer(eventsExpected, linesExpected);
        PObserveLogAppender logAppender = new PObserveLogAppender(parser, sequencer);

        commonLogLines.forEach(logAppender::append);

        logAppender.close();
    }

    public void runBadParserTest(String delimiter, int exceptionsExpected) throws Exception {
        BadParser parser = new BadParser(delimiter);
        TestEventSequencer sequencer = new TestEventSequencer(0, 0);
        PObserveLogAppender logAppender = new PObserveLogAppender(parser, sequencer);

        int badParserExceptionCount = 0;
        for (String line: commonLogLines) {
            try {
                logAppender.append(line);
            } catch (BadParserException e) {
                badParserExceptionCount++;
            }
        }

        Assertions.assertEquals(exceptionsExpected, badParserExceptionCount, "Unexpected number of parser exceptions");

        logAppender.close();
    }
}

// Minimal PEvent containing the number of lines for the event
class TestEvent extends PEvent<Integer> {
    private int numLines;

    public TestEvent(int numLines) {
        this.numLines = numLines;
    }

    @Override
    public Integer getPayload() {
        return numLines;
    }
}

// Minimal parser that counts the number of lines passed to it and generates an Event
// containing that information
class TestParser implements Parser<TestEvent> {
    private String delimiter;
    public TestParser(String delimiter) {
        this.delimiter = delimiter;
    }

    @Override
    public Stream<PObserveEvent<TestEvent>> apply(Object o) {
        List<String> lines = ((String)o).lines().collect(Collectors.toList());

        return Stream.of(
                new PObserveEvent<>("", 0L, new TestEvent(lines.size()))
        );
    }

    @Override
    public String getLogDelimiter() {
        return delimiter;
    }
}

class BadParserException extends RuntimeException {
}

class BadParser implements Parser<TestEvent> {
    private String delimiter;

    public BadParser(String delimiter) {
        this.delimiter = delimiter;
    }

    @Override
    public Stream<PObserveEvent<TestEvent>> apply(Object o) {
        throw new BadParserException();
    }

    @Override
    public String getLogDelimiter() {
        return delimiter;
    }
}

// Specialized event sequencer to count the number of events as well as
// verify the number of events and the lines recorded in each event
class TestEventSequencer extends EventSequencer {
    final private int eventsExpected;
    final private int linesExpected;
    private int eventsSeen;

    public TestEventSequencer(int eventsExpected, int linesExpected) {
        super(List.of());
        this.eventsExpected = eventsExpected;
        this.linesExpected = linesExpected;
        this.eventsSeen = 0;
    }

    @Override
    public boolean canThrowOutOfOrderException() {
        return false;
    }

    @Override
    public boolean shutdown() throws InterruptedException {
        Assertions.assertEquals(eventsExpected, eventsSeen, "Unexpected number of events seen");
        return true;
    }

    @Override
    public void accept(PObserveEvent<? extends PEvent<?>> pObserveEvent) {
        int numLines = (Integer)pObserveEvent.getEvent().getPayload();
        Assertions.assertEquals(linesExpected, numLines, "Unexpected number of lines in event");
        eventsSeen++;
    }
}
