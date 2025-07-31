package pobserve.commons;

import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

import java.time.Instant;
import java.util.List;
import java.util.stream.Collectors;
import java.util.stream.Stream;

public class ParserTests {
    @Test
    public void testSimpleParser() {
        Parser<String> parser = new SimpleParser();
        runTest(parser, "\n");
    }

    @Test
    public void testOverriddenParser() {
        Parser<String> parser = new OverriddenParser();
        runTest(parser, "\n\n");
    }

    private void runTest(Parser<String> parser, String expectedDelimiter) {
        parser.setConfiguration("Config String");
        Assertions.assertEquals(expectedDelimiter, parser.getLogDelimiter());

        List<PObserveEvent<String>> events = parser.apply("Key|Value").collect(Collectors.toList());

        Assertions.assertEquals(1, events.size());

        PObserveEvent<String> event = events.get(0);

        Assertions.assertEquals("Key", event.getPartitionKey());
        Assertions.assertEquals("Value", event.getEvent());
    }

    static class SimpleParser implements Parser<String> {
        @Override
        public Stream<PObserveEvent<String>> apply(Object o) {
            String log = o.toString();
            String[] parts = log.split("\\|");
            PObserveEvent<String> event = new PObserveEvent<>(parts[0], Instant.now(), parts[1]);
            return Stream.of(event);
        }
    }

    static class OverriddenParser extends SimpleParser {
        private boolean configurationCalled;

        @Override
        public String getLogDelimiter() {
            return "\n\n";
        }

        @Override
        public void setConfiguration(String configuration) {
            configurationCalled = true;
        }

        @Override
        public Stream<PObserveEvent<String>> apply(Object o) {
            Assertions.assertTrue(configurationCalled);
            return super.apply(o);
        }
    }
}
