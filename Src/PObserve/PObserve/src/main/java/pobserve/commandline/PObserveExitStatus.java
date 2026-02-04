package pobserve.commandline;

import lombok.Getter;

public enum PObserveExitStatus {
    SUCCESS(0),
    CMDLINEERROR(1),
    PASSERT(2),
    INTERNALERROR(3),

    PARSELOGERROR(4);

    @Getter
    private final int value;

    PObserveExitStatus(final int er) {
        value = er;
    }
}
