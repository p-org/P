package psym.valuesummary;

import java.io.Serializable;

/**
 * Represents the guarded value in a value summary where the value is of Type T
 *
 * @param <T> Type of the value in the guarded value
 */
public class GuardedValue<T> implements Serializable {
    private final Guard guard;
    private final T value;

    public GuardedValue(T value, Guard guard) {
        this.value = value;
        this.guard = guard;
    }

    public Guard getGuard() {
        return guard;
    }

    public T getValue() {
        return value;
    }
}
