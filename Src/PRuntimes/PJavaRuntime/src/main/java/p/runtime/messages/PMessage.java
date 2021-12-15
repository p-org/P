package p.runtime.messages;

import p.runtime.events.PEvent;
import p.runtime.values.PMachineRef;
import p.runtime.values.PValue;

public class PMessage {

    private final PEvent event;
    private final PValue<?> payload;
    private final PMachineRef source;
    private final PMachineRef target;

    /*** Constructors ***/
    public PMessage(PEvent event) {
        this.event = event;
        this.payload = null;
        source = null;
        target = null;
    }

    public PMessage(PEvent event, PValue<?> payload, PMachineRef source, PMachineRef target) {
        this.event = event;
        this.payload = payload;
        this.source = source;
        this.target = target;
    }
    /*** Getters ***/
    public PEvent getEvent() { return event; }
    public PValue<?> getPayload() { return payload; }
    public PMachineRef getSource() { return source; }
    public PMachineRef getTarget() { return target; }

    /*** Builder ***/
    public static class Builder {
        private PEvent event;
        private PValue<?> payload;
        private PMachineRef source;
        private PMachineRef target;

        public Builder(PEvent event) {
            this.event = event;
        }

        public Builder payload(PValue<?> payload) {
            this.payload = payload;
            return this;
        }

        public Builder source(PMachineRef source) {
            this.source = source;
            return this;
        }

        public Builder target(PMachineRef target) {
            this.target = target;
            return this;
        }

        public PMessage build() {
            return new PMessage(event, payload, source, target);
        }
    }
}
