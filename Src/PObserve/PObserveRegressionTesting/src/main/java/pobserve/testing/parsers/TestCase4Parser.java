package pobserve.testing.parsers;

import java.util.stream.Stream;

import pobserve.commons.Parser;
import pobserve.commons.PObserveEvent;

import pobserve.testing.spec.PGenerated.Java.PEvents;
import pobserve.testing.spec.PGenerated.Java.PTypes;
import pobserve.runtime.events.PEvent;

/**
 * This contains the parser for test case 4 (IncreasingInts).
 */
public class TestCase4Parser implements Parser<PEvent<?>> {
    private static PObserveEvent<PEvent<?>> parseLogLine(String line) {
        String[] parts = line.split(",");

        long timestamp = Long.parseLong(parts[0].trim());
        long key = Long.parseLong(parts[1].trim());
        long value = Integer.parseInt(parts[2].trim());

        PEvents.eNewIncreasingInt event = new PEvents.eNewIncreasingInt(
                new PTypes.PTuple_tmstm_key_value(timestamp, key, value));

        return new PObserveEvent<PEvent<?>>(Long.toString(key), timestamp, event, line);
    }

    @Override
    public Stream<PObserveEvent<PEvent<?>>> apply(Object obj) {
        if (obj instanceof String) {
            return Stream.of(parseLogLine((String) obj));
        }
        throw new IllegalArgumentException("Unsupported type for apply method: " + obj.getClass().getName());
    }
}
