package punit.exceptions;

import java.util.Optional;

public class PAssertMismatchExeception extends RuntimeException {

    public static Optional<PAssertMismatchExeception> fromTestResults(
            Optional<Throwable> actual, Optional<Class<? extends Throwable>> expected) {
        // No exception was expected and none was thrown!
        if (actual.isEmpty() && expected.isEmpty()) {
            return Optional.empty();
        }
        // An exception was expected and one of that class was thrown!
        if (actual.map(t -> t.getClass()).equals(expected)) {
            return Optional.empty();
        }

        // Otherwise, we have a mismatch.
        return Optional.of(new PAssertMismatchExeception(actual, expected));
    }

    private Optional<Throwable> actual;
    private Optional<Class<? extends Throwable>> expected;

    public Optional<Class<? extends Throwable>> getExpected() {
        return expected;
    }

    public Optional<Throwable> getActual() {
        return actual;
    }

    private PAssertMismatchExeception(Optional<Throwable> actual, Optional<Class<? extends Throwable>> expected) {
        if (actual.isEmpty() && expected.isEmpty()) {
            throw new RuntimeException("No throwable was expected, and none was thrown!");
        }

        this.actual = actual;
        this.expected = expected;
    }

    @Override
    public String getMessage() {
        // 1. We expected something to be thrown but nothing aws.
        if (expected.isPresent() && actual.isEmpty()) {
            return String.format("Expected an exception of type %s to be thrown but one never was.",
                    expected.get().getName());
        }

        // 2. We did not expect an exception but something was thrown.
        if (expected.isEmpty() && actual.isPresent()) {
            Throwable t = actual.get();
            return String.format("An exception of type %s was thrown (msg: %s)",
                    t.getClass().getName(), t.getMessage());
        }

        // 3. The wrong thing was thrown.
        if (expected.isPresent() && actual.isPresent()) {
            Class<? extends Throwable> expectedClass = expected.get();
            Throwable actuallyThrown = actual.get();
            if (actuallyThrown.getClass() != expectedClass) {
                return String.format(
                        "Expected an exception of type %s but one was thrown of type %s (msg: %s)",
                        expectedClass.getName(), actuallyThrown.getClass().getName(), actuallyThrown.getMessage());
            }
        }
        throw new RuntimeException(String.format("Unexpected state: actual=%s, expected=%s", actual, expected));
    }
}
