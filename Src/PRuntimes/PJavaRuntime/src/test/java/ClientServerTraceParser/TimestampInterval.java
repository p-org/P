package ClientServerTraceParser;

import java.util.List;
import java.util.Objects;

/**
 * A bound of wall-clock time that a PObserveEvent could have executed at, defined
 * as the half-interval [min, max).
 */
public class TimestampInterval {
    long min;
    long max;

    /**
     * Constructs an instantaneous time interval on [t, t).
     * @param t The scalar time.
     */
    public TimestampInterval(long t) {
        this(t, t);
    }

    /**
     * Constructs a time interval [b, e).
     * @param b The earliest valid time.
     * @param e The latest valid time.
     */
    public TimestampInterval(long b, long e) {
        min = b;
        max = e;
        if (b > e) {
            throw new RuntimeException("Undefined interval" + this.toString());
        }
    }

    public long getMin() {
        return min;
    }

    public long getMax() {
        return max;
    }


    public TimestampInterval add(TimestampInterval other) {
        Objects.requireNonNull(other);
        return new TimestampInterval(
                this.min + other.min,
                this.max + other.max
        );
    }

    public TimestampInterval sub(TimestampInterval other) {
        Objects.requireNonNull(other);
        return new TimestampInterval(
                this.min - other.min,
                this.max - other.max
        );
    }

    public TimestampInterval mul(TimestampInterval other) {
        Objects.requireNonNull(other);
        long new_min = Long.MAX_VALUE;
        long new_max = Long.MIN_VALUE;

        for (long p : List.of(
                this.min*other.min,
                this.min*other.max,
                this.max*other.min,
                this.max*other.max)) {
            new_min = Math.min(p, new_min);
            new_max = Math.min(p, new_max);
        }
        return new TimestampInterval(new_min, new_max);
    }

    public TimestampInterval div(TimestampInterval other) {
        Objects.requireNonNull(other);
        TimestampInterval denom;
        if (other.min != 0 && other.max == 0) {
            denom = new TimestampInterval(Long.MIN_VALUE, 1 / other.min);
        } else if (other.min == 0 && other.max != 0) {
            denom = new TimestampInterval(other.max, Long.MAX_VALUE);
        } else if (other.min == 0 && other.max == 0) {
            denom = new TimestampInterval(Long.MIN_VALUE, Long.MAX_VALUE);
        } else {
            denom = new TimestampInterval(1 / other.min, 1 / other.max);
        }
        return this.mul(denom);
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;
        TimestampInterval that = (TimestampInterval) o;
        return min == that.min && max == that.max;
    }

    @Override
    public int hashCode() {
        return Objects.hash(min, max);
    }

    @Override
    public String toString() {
        if (min == max) {
            return Long.toString(min);
        }
        return String.format("[%ld, %ld)", min, max);
    }
}
