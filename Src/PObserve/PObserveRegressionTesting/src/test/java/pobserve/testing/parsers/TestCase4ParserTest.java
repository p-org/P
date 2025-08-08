package pobserve.testing.parsers;

import static org.junit.jupiter.api.Assertions.assertEquals;

import java.util.List;
import java.util.stream.Collectors;
import java.util.stream.Stream;

import org.junit.jupiter.api.Test;

import pobserve.commons.PObserveEvent;

import pobserve.runtime.events.PEvent;


class TestCase4ParserTest {
    @Test
    void testParseLogLine() {
        TestCase4Parser parser = new TestCase4Parser();

        String logLine = "1697156606966, 1, 166";

        Stream<PObserveEvent<PEvent<?>>> result = parser.apply(logLine);
        List<PObserveEvent<PEvent<?>>> resultList = result.collect(Collectors.toList());
        PObserveEvent<PEvent<?>> event = resultList.get(0);

        assertEquals(event.getLogLine(), "1697156606966, 1, 166");
        assertEquals(event.getTimestamp().toEpochMilli(), Long.valueOf("1697156606966"));
        assertEquals(event.getPartitionKey(), "1");
    }
}
