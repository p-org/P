package pobserve.testing.parsers;

import java.util.stream.Stream;

import pobserve.commons.Parser;
import pobserve.commons.PObserveEvent;

import pobserve.testing.spec.PGenerated.Java.PEvents;
import pobserve.testing.spec.PGenerated.Java.PTypes;
import pobserve.runtime.events.PEvent;

public class TestCase6Parser implements Parser<PEvent<?>> {
    private static PObserveEvent<PEvent<?>> parseConfigParams(String line) {
        String[] parts = line.split(",");

        long timestamp = Long.parseLong(parts[1].trim());
        long key = Long.parseLong(parts[2].trim());
        long node_1_ones = Long.parseLong(parts[3].trim());
        long node_2_ones = Long.parseLong(parts[4].trim());
        long n = Long.parseLong(parts[5].trim());

        PEvents.eConfigReceivedDualNodesBinary event = new PEvents.eConfigReceivedDualNodesBinary(
                new PTypes.PTuple_tmstm_key_nd1cn_nd2cn_ttlvl(timestamp, key, node_1_ones, node_2_ones, n));

        return new PObserveEvent<PEvent<?>>(Long.toString(key), timestamp, event, line);
    }

    private static PObserveEvent<PEvent<?>> parseLogEntry(String line) {
        String[] parts = line.split(",");

        long timestamp = Long.parseLong(parts[0].trim());
        long key = Long.parseLong(parts[1].trim());
        int node = Integer.parseInt(parts[2].trim());
        int value = Integer.parseInt(parts[3].trim());

        PEvents.eNewLogEntryDualNodesBinary event = new PEvents.eNewLogEntryDualNodesBinary(
                new PTypes.PTuple_tmstm_key_node_value(timestamp, key, node, value));

        return new PObserveEvent<PEvent<?>>(Long.toString(key), timestamp, event, line);
    }

    private static PObserveEvent<PEvent<?>> parseEntriesFinished(String line) {
        String[] parts = line.split(",");

        long timestamp = Long.parseLong(parts[1].trim());
        long key = Long.parseLong(parts[2].trim());

        PEvents.eEntriesFinishedDualNodesBinary event = new PEvents.eEntriesFinishedDualNodesBinary(
                new PTypes.PTuple_tmstm_key(timestamp, key));

        return new PObserveEvent<PEvent<?>>(Long.toString(key), timestamp, event, line);
    }

    @Override
    public Stream<PObserveEvent<PEvent<?>>> apply(Object obj) {
        if (obj instanceof String) {
            String t = (String) obj;
            if (t.startsWith("params")) {
                return Stream.of(parseConfigParams(t));
            } else if (t.startsWith("finished")) {
                return Stream.of(parseEntriesFinished(t));
            }
            return Stream.of(parseLogEntry(t));
        }
        throw new IllegalArgumentException("Unsupported type for apply method: " + obj.getClass().getName());
    }
}
