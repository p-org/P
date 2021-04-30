package psymbolic.valuesummary;

import java.util.HashMap;
import java.util.Map;

/** Class containing static methods that are useful for Boolean primitive value summaries */
public final class BoolUtils {
    private BoolUtils() {}

    /** Create a BooleanVS that is true under when the guard is true
     *
     * @param guard the guard under which the BooleanVS should be true
     * @return BooleanVS
     */
    public static PrimitiveVS<Boolean> fromTrueGuard(Guard guard) {
        if (guard.isFalse()) {
            return new PrimitiveVS<>(false);
        }

        if (guard.isTrue()) {
            return new PrimitiveVS<>(true);
        }

        final Map<Boolean, Guard> entries = new HashMap<>();
        entries.put(true, guard);
        entries.put(false, guard.not());
        return new PrimitiveVS<>(entries);
    }

    /** Get the condition under which a Boolean value summary is true
     *
     * @param primVS A Boolean value summary
     * @return Guard that is true when the value summary is true
     */
    public static Guard trueCond(PrimitiveVS<Boolean> primVS) {
        return primVS.getGuard(true);
    }

    /** Get the condition under which a Boolean value summary is false
     *
     * @param primVS A Boolean value summary
     * @return Guard that is true when the value summary is false
     */
    public static Guard falseCond(PrimitiveVS<Boolean> primVS) {
        return primVS.getGuard(false);
    }

    /** Get the conjunction of two Boolean value summaries
     *
     * @param a The first conjunct's Boolean value summary
     * @param b The second conjunct's Boolean value summary
     * @return Boolean value summary for the arguments' conjunction
     */
    public static PrimitiveVS<Boolean> and(PrimitiveVS<Boolean> a, PrimitiveVS<Boolean> b) {
        return a.apply2(b, (x, y) -> x && y);
    }

    /** Get the conjunction of a Boolean value summary and a boolean
     *
     * @param a The first conjunct's Boolean value summary
     * @param b The second boolean's value
     * @return Boolean value summary for the arguments' conjunction
     */
    public static PrimitiveVS<Boolean> and(PrimitiveVS<Boolean> a, boolean b) {
        return a.apply(x -> x && b);
    }

    /** Get the conjunction of a boolean and a Boolean value summary
     *
     * @param a The first boolean's value
     * @param b The second conjunct's Boolean value summary
     * @return Boolean value summary for the arguments' conjunction
     */
    public static PrimitiveVS<Boolean> and(boolean a, PrimitiveVS<Boolean> b) {
        return and(b, a);
    }

    /** Get the disjunction of two Boolean value summaries
     *
     * @param a The first disjunct's Boolean value summary
     * @param b The second disjunct's Boolean value summary
     * @return Boolean value summary for the arguments' disjunction
     */
    public static PrimitiveVS<Boolean> or(PrimitiveVS<Boolean> a, PrimitiveVS<Boolean> b) {
        return a.apply2(b, (x, y) -> x || y);
    }

    /** Get whether or not a Boolean value summary is always false
     *
     * @param b The Boolean value summary
     * @return Whether or not the provided value summary is always false
     */
    public static boolean isFalse(PrimitiveVS<Boolean> b) {
        return falseCond(b).isConstTrue();
    }

    /** Get whether or not a Boolean value summary is ever true
     *
     * @param b The Boolean value summary
     * @return Whether or not the provided value summary can be true
     */
    public static boolean isEverTrue(PrimitiveVS<Boolean> b) {
        return !trueCond(b).isConstFalse();
    }

    /** Get whether or not a Boolean value summary is ever false
     *
     * @param b The Boolean value summary
     * @return Whether or not the provided value summary can be false
     */
    public static boolean isEverFalse(PrimitiveVS<Boolean> b) { return !falseCond(b).isConstFalse(); }

}
