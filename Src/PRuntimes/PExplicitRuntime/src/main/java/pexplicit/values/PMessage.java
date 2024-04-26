package pexplicit.values;

import lombok.Getter;
import pexplicit.runtime.machine.PMachine;

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
        initialize();
    }

    public PMessage setTarget(PMachine target) {
        return new PMessage(event, target, payload);
    }

    @Override
    public PMessage clone() {
        return new PMessage(this.event, this.target, this.payload);
    }

    @Override
    protected String _asString() {
        StringBuilder sb = new StringBuilder();
        sb.append(String.format("%s@%s", event, target));
        if (payload != null) {
            sb.append(String.format(" :payload %s", payload));
        }
        return sb.toString();
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this) return true;

        if (!(obj instanceof PMessage other)) {
            return false;
        }

        if (this.target != other.target) {
            return false;
        }
        if (this.event != other.event) {
            return false;
        }
        return this.payload == other.payload;
    }
}
