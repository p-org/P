package pobserve.junit;


import pobserve.runtime.events.PEvent;
import pobserve.runtime.exceptions.PAssertionFailureException;
import pobserve.runtime.exceptions.UnhandledEventException;

import java.io.Serializable;
import java.util.*;

/*
 * This is a PMachines class that mocks the generated PSpec.
 * It has an extra function getOutput that can be called in EventSequencerTest
 * to check the received events against the input events.
 */


public class TestPMachines {

    public static class TestSpec extends pobserve.runtime.Monitor<TestSpec.PrtStates> {
        private ArrayList<PEvent<?>> outputs = new ArrayList<>();

        @Override
        public List<Class<? extends PEvent<?>>> getEventTypes() {
            List<Class<? extends PEvent<?>>> types = new ArrayList<>();
            types.add(EventSequencerTests.PEvent_idx.class);
            return types;
        }

        @Override
        public void reInitializeMonitor() {}

        @Override
        public void accept(PEvent<?> p) throws UnhandledEventException {
            outputs.add(p);
            if ((long) p.getPayload() == -1) {
                throw new PAssertionFailureException("Testing error injection");
            }
        }

        public ArrayList<PEvent<?>> getOutput() {
            return outputs;
        }


        public static class Supplier implements java.util.function.Supplier<TestSpec>, Serializable {

            public TestSpec get() {
                TestSpec ret = new TestSpec();
                return ret;
            }
        }

        public enum PrtStates {
            TestState
        }

    }}