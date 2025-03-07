package ClientServerTraceParser;

import prt.events.PEvent;

import java.util.Objects;

/**
 * This is meant to stub out a PObserve event that will terminate at a sink such as the Java
 * P runtime.  For monitors and the runtime in general, the only thing of interest to us is
 * the `PEvent` field.
 */
public class PObserveEvent<P> {
    private final TimestampInterval ts;
    private final PEvent<P> pEvent;
    public TimestampInterval getTs() {
        return this.ts;
    }

    public PEvent<P> getpEvent() {
        return this.pEvent;
    }

    public PObserveEvent(TimestampInterval ts, PEvent<P> pEvent) {
        this.ts = ts;
        this.pEvent = pEvent;
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;
        PObserveEvent<?> that = (PObserveEvent<?>) o;
        return Objects.equals(ts, that.ts) && prt.values.Equality.deepEquals(this.pEvent, that.pEvent);
    }

    @Override
    public int hashCode() {
        return Objects.hash(ts, pEvent);
    }

    @Override
    public String toString() {
        return "PObserveEvent{" +
                "ts=" + ts +
                ", pEvent=" + pEvent +
                '}';
    }

}
