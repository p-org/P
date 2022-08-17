package sample.samplespec;

import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;
import prt.State;
import prt.events.PEvent;

import java.math.BigInteger;
import java.util.List;
import java.util.function.Supplier;

public class RingSpec extends prt.Monitor {

    public static class Supplier implements java.util.function.Supplier<RingSpec> {
        @Override
        public RingSpec get() {
            RingSpec ret = new RingSpec();
            ret.ready();
            return ret;
        }
    }

    Logger logger = LogManager.getLogger(this.getClass());

    private BigInteger val;

    enum States { Init };

    public RingSpec() {
        super();

        addState(new State.Builder<>(States.Init)
                .isInitialState(true)
                .withEvent(PEvents.addEvent.class, e -> {
                    BigInteger nextVal = val.add(BigInteger.valueOf(e.i));

                    if (!nextVal.equals(BigInteger.valueOf(e.total))) {
                        throw new prt.exceptions.PAssertionFailureException("Sum failed");
                    }

                    logger.info(val + " + " + e.i + " = " + nextVal);
                    val = nextVal;
                })
                .withEvent(PEvents.mulEvent.class, e -> {
                    BigInteger nextVal = val.multiply(BigInteger.valueOf(e.i));

                    if (!nextVal.equals(BigInteger.valueOf(e.total))) {
                        throw new prt.exceptions.PAssertionFailureException("Mult failed");
                    }

                    logger.info(val + " * " + e.i + " = " + nextVal);
                    val = nextVal;
                })
                .build());

        val = BigInteger.valueOf(0);
    }

    @Override
    public List<Class<? extends PEvent<?>>> getEventTypes() {
        return List.of(PEvents.addEvent.class, PEvents.mulEvent.class);
    }

}
