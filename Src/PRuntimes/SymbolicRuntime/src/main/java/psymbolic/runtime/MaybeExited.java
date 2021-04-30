package psymbolic.runtime;

import psymbolic.valuesummary.bdd.Bdd;

public class MaybeExited<T> {
    final T value;
    final Bdd newPc;

    public MaybeExited(T value, Bdd newPc) {
        this.value = value;
        this.newPc = newPc;
    }

    public T getValue() {
        return value;
    }

    public Bdd getNewPc() {
        return newPc;
    }
}
