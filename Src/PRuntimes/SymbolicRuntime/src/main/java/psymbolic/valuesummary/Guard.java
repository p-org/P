package psymbolic.valuesummary;

import psymbolic.valuesummary.bdd.BddGuard;

import java.util.List;
import java.util.Objects;

/**
 * Represents the Schedule, Control, Input (SCI) guard in the guarded value of a value summary
 * Currently, the guards are implemented using BDDs.
 */
public class Guard {
    /**
     * Represents the boolean formula for the guard
     */
    private final BddGuard guard;

    public Guard(BddGuard guard) {
        this.guard = guard;
    }

    /**
     * Create a constant false guard
     * @return Guard representing constant false
     */
    public static Guard constFalse() {
        return new Guard(BddGuard.constFalse());
    }

    /**
     * Create a constant true guard
     * @return Guard representing constant true
     */
    public static Guard constTrue() {
        return new Guard(BddGuard.constTrue());
    }

    /** Checks whether the logical guard evaluates to true
     *
     * @return True iff the guard evaluates to true
     */
    public boolean isTrue() {
        return guard.isTrue();
    }

    /** Checks whether the logical guard evaluates to false
     *
     * @return True iff the guard evaluates to false
     */
    public boolean isFalse() {
        return guard.isFalse();
    }

    /**
     * Performs logical `and` of two guards
     * @param other the other guard
     * @return guard that is the `and` of two guards
     */
    public Guard and(Guard other) {
        return new Guard(guard.and(other.guard));
    }

    /**
     * Performs logical `or` of two guards
     * @param other the other guard
     * @return guard that is the `or` of two guards
     */
    public Guard or(Guard other) {
        return new Guard(guard.or(other.guard));
    }

    /**
     * Performs the logical `implies` this -> other
     * @param other the other guard
     * @return
     */
    public Guard implies(Guard other) {
        return new Guard(guard.implies(other.guard));
    }

    /**
     * Perform logical `negation` of the guard
     * @return negated guard `not`
     */
    public Guard not() {
        return new Guard(guard.not());
    }

    /**
     * Perform `or` of a list of Guards
     * @param bddGuards all the Guards to be `OR`ed
     * @return `OR`ed Guard
     */
    public static Guard orMany(List<Guard> bddGuards) {
        return bddGuards.stream().reduce(Guard.constFalse(), Guard::or);
    }

    /**
     * Perform ITE of the given Guard `cond`
     * @param thenCase then Guard
     * @param elseCase else Guard
     * @return resultant ITE Guard
     */
    public Guard ifThenElse(Guard thenCase, Guard elseCase) {
        return new Guard(guard.ifThenElse(thenCase.guard, elseCase.guard));
    }

    /**
     * This function should not be invoked from outside,
     * TODO: Need to fix this part.
     * @return
     */
    public static Guard newVar() {
        return new Guard(BddGuard.newVar());
    }

    @Override
    public String toString() {
        return guard.toString();
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (!(o instanceof Guard)) return false;
        Guard guard1 = (Guard) o;
        return Objects.equals(guard, guard1.guard);
    }

    @Override
    public int hashCode() {
        return Objects.hash(guard);
    }
}
