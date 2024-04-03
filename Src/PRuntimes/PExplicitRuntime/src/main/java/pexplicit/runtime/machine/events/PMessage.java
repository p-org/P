package pexplicit.runtime.machine.events;

import lombok.Getter;
import pexplicit.runtime.machine.PMachine;
import pexplicit.utils.exceptions.NotImplementedException;
import pexplicit.values.ComputeHash;
import pexplicit.values.PEvent;
import pexplicit.values.PTuple;
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

    @Override
    public PMessage clone() {
        return new PMessage(this.event, this.target, this.payload);
    }

    @Override
    public int hashCode() {
        return ComputeHash.getHashCode(target, event, payload);
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this) return true;

        if (!(obj instanceof PMessage)) {
            return false;
        }

        PMessage other = (PMessage) obj;
        if (this.target != other.target) {
            return false;
        }
        if (this.event != other.event) {
            return false;
        }
        if (this.payload != other.payload) {
            return false;
        }

        return true;
    }

    @Override
    public String toString() {
        StringBuilder sb = new StringBuilder();
        sb.append(String.format("%s@%s", event, target));
        if (payload != null) {
            sb.append(String.format(" :payload %s", payload));
        }
        return sb.toString();
    }
}
