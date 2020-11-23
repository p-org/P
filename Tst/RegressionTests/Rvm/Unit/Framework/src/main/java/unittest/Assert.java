package unittest;

import mop.StateNameException;

public class Assert {

    public static void stateNameIs(Instrumented i, String expected) {
        String actual = getStateName(i);
        if (expected == null) {
            throw new AssertionError("The expected state name should never be null.");
        }
        if (actual == null) {
            throw new AssertionError("The actual state name should never be null.");
        }
        if (!expected.equals(actual)) {
            throw new AssertionError("Expected the state name to be '" + expected + "', but was '" + actual + "'.");
        }
    }

    public static void nullPointerException(Runnable r) {
        try {
            r.run();
            throw new AssertionError("Expected a NullPointerException.");
        } catch (NullPointerException e) {
            return;
        } catch (Throwable e) {
            throw new AssertionError("Expected a NullPointerException, but got " + e.toString());
        }
    }

    private static String getStateName(Instrumented i) {
        try {
            i.getState();
            throw new AssertionError("Expected an exception - did the spec contain a getState event?");
        } catch (StateNameException e) {
            return e.getStateName();
        }
    }
}
