package psymbolic.valuesummary;

import psymbolic.valuesummary.bdd.Bdd;

public class GuardedValue<T> {
    public final T value;
    public final Bdd guard;

    public GuardedValue(T value, Bdd guard) {
        this.value = value;
        this.guard = guard;
    }
}
