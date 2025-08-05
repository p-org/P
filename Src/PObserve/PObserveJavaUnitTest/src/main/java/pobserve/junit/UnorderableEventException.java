package pobserve.junit;

import java.time.Instant;

public class UnorderableEventException extends RuntimeException {
    public UnorderableEventException(Instant ts, Instant maxAlreadyProcessed) {
        super(String.format(
                "Timestamp %s can't be ordered; less than already-processed ts %s", ts.toString(), maxAlreadyProcessed.toString()));
    }
}
