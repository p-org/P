package pexplicit.runtime.machine.events;

import lombok.Getter;
import pexplicit.runtime.machine.PMachine;
import pexplicit.utils.exceptions.NotImplementedException;
import pexplicit.values.PEvent;
import pexplicit.values.PValue;

/**
 * Represents a message containing details about the event, target machine, and event payload.
 */
@Getter
public class PMessage extends PValue<PMessage> {
    private final PEvent event;
    private final PMachine target;
    private final PValue<?> payload;

    /**
     * Constructor
     *
     * @param event   Event
     * @param target  Target machine
     * @param payload Event payload
     */
    public PMessage(PEvent event, PMachine target, PValue<?> payload) {
        this.event = event;
        this.target = target;
        this.payload = payload;
    }

    /**
     * Clone a message
     *
     * @return Cloned message
     */
    @Override
    public PMessage clone() {
        return new PMessage(this.event, this.target, this.payload);
    }

    /**
     * TODO
     *
     * @return
     */
    @Override
    public int hashCode() {
        throw new NotImplementedException();
    }

    /**
     * TODO
     *
     * @return
     */
    @Override
    public boolean equals(Object other) {
        throw new NotImplementedException();
    }

    /**
     * TODO
     *
     * @return
     */
    @Override
    public String toString() {
        throw new NotImplementedException();
    }
}
