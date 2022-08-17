package sample.samplespec;

import prt.events.PEvent;

import java.util.HashMap;
import java.util.Map;
import java.util.function.Function;
import java.util.function.Supplier;
import java.util.stream.Stream;

public class RingEventParser implements Function<String, Stream<? extends PEvent<?>>> {

    public static class Supplier implements java.util.function.Supplier<RingEventParser> {
        public RingEventParser get() {
            return new RingEventParser();
        }
    }

    private static final HashMap<String, Function<String, ? extends PEvent<?>>> handlers = new HashMap<>(Map.of(
            "ADD", RingEventParser::payloadToAddEvent,
            "MUL", RingEventParser::payloadToMulEvent
    ));

    private static PEvents.addEvent payloadToAddEvent(String payload) {
        String[] tokens = payload.split(",");
        return new PEvents.addEvent(
                new PTypes.PTuple_i_total(
                    Integer.valueOf(tokens[0]),
                    Integer.valueOf(tokens[1])));
    }

    private static PEvents.mulEvent payloadToMulEvent(String payload) {
        String[] tokens = payload.split(",");
        return new PEvents.mulEvent(
                new PTypes.PTuple_i_total(
                        Integer.valueOf(tokens[0]),
                        Integer.valueOf(tokens[1])));
    }


    @Override
    public Stream<PEvent<?>> apply(String line) {
        String[] tokens = line.split(":");
        if (tokens.length != 2) {
            return Stream.of();
        }

        String event = tokens[0];
        String payload = tokens[1];

        if (handlers.containsKey(event)) {
            return Stream.of(handlers.get(event).apply(payload));
        }
        return Stream.of();
    }
}
