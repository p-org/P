package pobserve.executor;
import pobserve.commons.PObserveEvent;

import java.util.concurrent.LinkedBlockingQueue;
import lombok.Getter;

/**
 * PObserveReplayEvents class keeps track of replay events in a PObserve job
 */
@Getter
public class PObserveReplayEvents {
    private LinkedBlockingQueue<PObserveEvent> replayEventQueue;
    private Long errorTimeStamp;
    private String key;

    public PObserveReplayEvents(int windowSize, String key) {
        this.key = key;
        this.replayEventQueue = new LinkedBlockingQueue<>(windowSize);
        this.errorTimeStamp = 0L;
    }

    public void addEvent(PObserveEvent event) {
        try {
            if (this.replayEventQueue.remainingCapacity() == 0) {
                this.replayEventQueue.poll();
            }
            this.replayEventQueue.put(event);
            this.errorTimeStamp = event.getTimestamp().toEpochMilli();
        } catch (InterruptedException e) {
            throw new RuntimeException(e);
        }
    }


}
