package prt;

import java.util.Objects;

/**
 * Represents a P event
 * @param <P> is the type of the payload associated with this event
 */
public abstract class PEvent<P> {
    public abstract P getPayload();

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;
        PEvent<?> that = (PEvent<?>) o;
        return prt.values.Equality.deepEquals(this.getPayload(), that.getPayload());
    }

    @Override
    public int hashCode() {
        return Objects.hash(this.getPayload());
    }
}