package pobserve.testing.parsers;

import java.util.stream.Stream;

import pobserve.commons.Parser;
import pobserve.commons.PObserveEvent;

import pobserve.testing.spec.PGenerated.Java.PEvents;
import pobserve.testing.spec.PGenerated.Java.PTypes;
import pobserve.runtime.events.PEvent;

public class TestCase7Parser implements Parser<PEvent<?>> {
    private static PObserveEvent<PEvent<?>> parseLogEntry(String line) {
        String[] parts = line.split(",");

        long timestamp = Long.parseLong(parts[0].trim());
        long key = Long.parseLong(parts[1].trim());
        long eventNum = Integer.parseInt(parts[2].trim());
        String status = parts[3].trim();

        if (status.equals("start")) {
            PEvents.eEventStartEventPairs event = new PEvents.eEventStartEventPairs(
                    new PTypes.PTuple_tmstm_key_evntn_stts(timestamp, key, eventNum, status));
            return new PObserveEvent<PEvent<?>>(Long.toString(key), timestamp, event, line);
        } else {
            PEvents.eEventEndEventPairs event = new PEvents.eEventEndEventPairs(
                    new PTypes.PTuple_tmstm_key_evntn_stts(timestamp, key, eventNum, status));
            return new PObserveEvent<PEvent<?>>(Long.toString(key), timestamp, event, line);
        }
    }

    private static PObserveEvent<PEvent<?>> parseEntriesFinished(String line) {
        String[] parts = line.split(",");

        long timestamp = Long.parseLong(parts[0].trim());
        long key = Long.parseLong(parts[1].trim());

        PEvents.eEntriesFinishedEventPairs event = new PEvents.eEntriesFinishedEventPairs(
                new PTypes.PTuple_tmstm_key(timestamp, key));

        return new PObserveEvent<PEvent<?>>(Long.toString(key), timestamp, event, line);
    }

    @Override
    public Stream<PObserveEvent<PEvent<?>>> apply(Object obj) {
        if (obj instanceof String) {
            String t = (String) obj;
            if (t.toLowerCase().contains("finished")) {
                return Stream.of(parseEntriesFinished(t));
            }
            return Stream.of(parseLogEntry(t));
        }
        throw new IllegalArgumentException("Unsupported type for apply method: " + obj.getClass().getName());
    }
}
