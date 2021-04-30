package psymbolic.valuesummary;

import lombok.Getter;

/**
 * Represents the guarded value in a value summary where the value is of Type T
 * @param <T> Type of the value in the guarded value
 */
public class GuardedValue<T> {
    @Getter
    private final Guard guard;
    @Getter
    private final T value;

    public GuardedValue(T value, Guard guard) {
        this.value = value;
        this.guard = guard;
    }
}
