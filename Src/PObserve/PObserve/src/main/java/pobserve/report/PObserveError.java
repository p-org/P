package pobserve.report;

import pobserve.executor.PObserveReplayEvents;

import lombok.Getter;
import lombok.Setter;

@Getter
@Setter
public class PObserveError {
    private Exception exception;

    private PObserveReplayEvents replayEvents;

    public PObserveError(Exception ex) {
        exception = ex;
        replayEvents = null;
    }

    public PObserveError(Exception ex, PObserveReplayEvents replay) {
        exception = ex;
        replayEvents = replay;
    }
}
