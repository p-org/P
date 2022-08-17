import org.apache.logging.log4j.Logger;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import prt.Monitor;
import prt.events.PEvent;
import punit.ObservableAppender;
import punit.annotations.PAssertExpected;
import punit.annotations.PSpecTest;
import sample.sampleimpl.Ring;
import sample.samplespec.PEvents;
import sample.samplespec.PTypes;
import sample.samplespec.RingEventParser;
import sample.samplespec.RingSpec;

import java.util.function.Function;
import java.util.stream.Stream;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;

public class RingTest {
    @Test
    @DisplayName("Can add to a Ring")
    public void testSingleRingAdd() {
        Ring r = new Ring();
        r.Add(42);
    }

    @Test
    @DisplayName("Can multiply to a ring specification, manually")
    public void testRingSpecMul() {
        Monitor spec = new RingSpec();
        spec.ready();

        spec.accept(new PEvents.mulEvent(new PTypes.PTuple_i_total(42, 0)));
    }

    @Test
    @DisplayName("Test no overflow, manually")
    public void testSpecOverflow() {
        Monitor spec = new RingSpec();
        spec.ready();

        spec.accept(new PEvents.addEvent(new PTypes.PTuple_i_total(32, 32)));
        spec.accept(new PEvents.addEvent(new PTypes.PTuple_i_total(10, 42)));
    }



    @PSpecTest(
            // Test instances of this class...
            impl = Ring.class,
            // ...by parsing impl's log lines with this parser...
            parser = RingEventParser.Supplier.class,
            // ... against instances of this P specification!
            spec = RingSpec.Supplier.class
    )
    @DisplayName("A simple unit test for an algebraic ring.")
    public void testRing() {
        Ring r = new Ring();
        r.Add(32);
        r.Mul(2);
    }



    @PSpecTest(impl = Ring.class, parser = RingEventParser.Supplier.class, spec = RingSpec.Supplier.class)
    @PAssertExpected
    @DisplayName("Tests overflowing the Ring's state")
    public void testRingOverflow() {
        Ring r = new Ring();
        r.Add(32);
        r.Mul(7);
    }
}
