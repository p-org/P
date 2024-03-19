package pcover.runtime.machine.events;

import lombok.Getter;
import pcover.runtime.machine.PMachine;
import pcover.utils.exceptions.NotImplementedException;
import pcover.values.PEvent;
import pcover.values.PValue;

/**
 * Represents a message containing details about the event, target machine, and event payload.
 */
@Getter
public class Message extends PValue<Message> {
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
    public Message(PEvent event, PMachine target, PValue<?> payload) {
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
    public Message clone() {
        return new Message(this.event, this.target, this.payload);
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
