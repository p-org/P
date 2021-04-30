package psymbolic.valuesummary;

import psymbolic.valuesummary.bdd.Bdd;

import java.util.HashMap;
import java.util.Map;

/** Class containing static methods that are useful for Boolean primitive value summaries */
public final class BoolUtils {
    private BoolUtils() {}

    /** Make a Boolean value summary that is true under the conditions that the provided guard is true
     *
     * @param guard A Bdd that is true when the Boolean value summary should be true
     * @return Boolean value summary
     */
    public static PrimVS<Boolean> fromTrueGuard(Bdd guard) {
        if (guard.isConstFalse()) {
            return new PrimVS<>(false);
        }

        if (guard.isConstTrue()) {
            return new PrimVS<>(true);
        }

        final Map<Boolean, Bdd> entries = new HashMap<>();
        entries.put(true, guard);
        entries.put(false, guard.not());
        return new PrimVS<>(entries);
    }

    /** Get the condition under which a Boolean value summary is true
     *
     * @param primVS A Boolean value summary
     * @return Bdd that is true when the value summary is true
     */
    public static Bdd trueCond(PrimVS<Boolean> primVS) {
        return primVS.getGuard(true);
    }

    /** Get the condition under which a Boolean value summary is false
     *
     * @param primVS A Boolean value summary
     * @return Bdd that is true when the value summary is false
     */
    public static Bdd falseCond(PrimVS<Boolean> primVS) {
        return primVS.getGuard(false);
    }

    /** Get the conjunction of two Boolean value summaries
     *
     * @param a The first conjunct's Boolean value summary
     * @param b The second conjunct's Boolean value summary
     * @return Boolean value summary for the arguments' conjunction
     */
    public static PrimVS<Boolean> and(PrimVS<Boolean> a, PrimVS<Boolean> b) {
        return a.apply2(b, (x, y) -> x && y);
    }

    /** Get the conjunction of a Boolean value summary and a boolean
     *
     * @param a The first conjunct's Boolean value summary
     * @param b The second boolean's value
     * @return Boolean value summary for the arguments' conjunction
     */
    public static PrimVS<Boolean> and(PrimVS<Boolean> a, boolean b) {
        return a.apply(x -> x && b);
    }

    /** Get the conjunction of a boolean and a Boolean value summary
     *
     * @param a The first boolean's value
     * @param b The second conjunct's Boolean value summary
     * @return Boolean value summary for the arguments' conjunction
     */
    public static PrimVS<Boolean> and(boolean a, PrimVS<Boolean> b) {
        return and(b, a);
    }

    /** Get the disjunction of two Boolean value summaries
     *
     * @param a The first disjunct's Boolean value summary
     * @param b The second disjunct's Boolean value summary
     * @return Boolean value summary for the arguments' disjunction
     */
    public static PrimVS<Boolean> or(PrimVS<Boolean> a, PrimVS<Boolean> b) {
        return a.apply2(b, (x, y) -> x || y);
    }

    /** Get whether or not a Boolean value summary is always false
     *
     * @param b The Boolean value summary
     * @return Whether or not the provided value summary is always false
     */
    public static boolean isFalse(PrimVS<Boolean> b) {
        return falseCond(b).isConstTrue();
    }

    /** Get whether or not a Boolean value summary is ever true
     *
     * @param b The Boolean value summary
     * @return Whether or not the provided value summary can be true
     */
    public static boolean isEverTrue(PrimVS<Boolean> b) {
        return !trueCond(b).isConstFalse();
    }

    /** Get whether or not a Boolean value summary is ever false
     *
     * @param b The Boolean value summary
     * @return Whether or not the provided value summary can be false
     */
    public static boolean isEverFalse(PrimVS<Boolean> b) { return !falseCond(b).isConstFalse(); }

}
