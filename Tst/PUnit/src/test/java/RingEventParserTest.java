import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import prt.events.PEvent;
import sample.samplespec.PEvents;
import sample.samplespec.PTypes;
import sample.samplespec.RingEventParser;

import java.util.List;
import java.util.stream.Collectors;

import static org.junit.jupiter.api.Assertions.assertEquals;

public class RingEventParserTest {
    @Test
    @DisplayName("Can decode a valid add event")
    public void testDecodeAdd() {
        RingEventParser p = new RingEventParser();
        String line = "ADD:42,100";

        List<PEvent<?>> events = p.apply(line).collect(Collectors.toList());
        assertEquals(1, events.size());
        assertEquals(new PEvents.addEvent(new PTypes.PTuple_i_total(42, 100)), events.get(0));
    }


    @Test
    @DisplayName("Can decode a valid mul event")
    public void testDecodeMul() {
        RingEventParser p = new RingEventParser();
        String line = "MUL:42,100";

        List<PEvent<?>> events = p.apply(line).collect(Collectors.toList());
        assertEquals(1, events.size());
        assertEquals(new PEvents.mulEvent(new PTypes.PTuple_i_total(42, 100)), events.get(0));
    }

    @Test
    @DisplayName("Can process an unrelated event")
    public void testDecodeNonEvent() {
        RingEventParser p = new RingEventParser();
        String line = "{\"@timestamp\":\"2022-07-05T17:47:14.675Z\",\"ecs.version\":\"1.2.0\",\"log.level\":\"INFO\",\"message\":\"42\",\"process.thread.name\":\"main\",\"log.logger\":\"sampleimpl.Ring\",\"marker\":\"MOD\"}";

        List<PEvent<?>> events = p.apply(line).collect(Collectors.toList());
        assertEquals(0, events.size());
    }
}
