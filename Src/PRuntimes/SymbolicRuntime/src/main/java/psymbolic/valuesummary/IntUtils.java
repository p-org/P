package psymbolic.valuesummary;

/** Class containing static methods that are useful for Integer primitive value summaries */
public class IntUtils {
    /** Add two Integer primitive value summaries
     *
     * @param a First summand value summary
     * @param b Second summand value summary
     * @return The value summary representing the arguments' sum
     */
    public static PrimitiveVS<Integer> add(PrimitiveVS<Integer> a, PrimitiveVS<Integer> b) {
        return a.apply2(b, (x, y) -> x + y);
    }

    /** Add a concrete int to an Integer primitive value summary
     *
     * @param a First summand value summary
     * @param i Second summand
     * @return The value summary representing the arguments' sum
     */
    public static PrimitiveVS<Integer> add(PrimitiveVS<Integer> a, int i) {
        return a.apply(x -> x + i);
    }

    /** Subtract two Integer primitive value summaries
     *
     * @param a Value summary of first Integer
     * @param b Value summary of Integer to be subtracted
     * @return The value summary representing the arguments' difference
     */
    public static PrimitiveVS<Integer> subtract(PrimitiveVS<Integer> a, PrimitiveVS<Integer> b) {
        return a.apply2(b, (x, y) -> x - y);
    }

    /** Subtract a concrete int from an Integer primitive value summary
     *
     * @param a Value summary of first Integer
     * @param i Value of int to be subtracted
     * @return The value summary representing the arguments' difference
     */
    public static PrimitiveVS<Integer> subtract(PrimitiveVS<Integer> a, int i) {
        return a.apply(x -> x - i);
    }

    /** Detect whether one Integer value summary is less than another
     *
     * @param a Value summary of first Integer
     * @param b Value summary of second Integer
     * @return The value summary representing whether the first argument is less than the second
     */
    public static PrimitiveVS<Boolean> lessThan(PrimitiveVS<Integer> a, PrimitiveVS<Integer> b) {
        return a.apply2(b, (x, y) -> x < y);
    }

    /** Detect whether an int is less than an Integer value summary
     *
     * @param a Value of the int
     * @param b Value summary of second Integer
     * @return The value summary representing whether the first argument is less than the second
     */
    public static PrimitiveVS<Boolean> lessThan(int a, PrimitiveVS<Integer> b) {
        return b.apply(x -> a < x);
    }

    /** Detect whether an Integer value summary is less than an int
     *
     * @param a Value summary of first Integer
     * @param b Value of the int
     * @return The value summary representing whether the first argument is less than the second
     */
    public static PrimitiveVS<Boolean> lessThan(PrimitiveVS<Integer> a, int b) {
        return a.apply(x -> x < b);
    }

    /** Compare two Integer value summaries
     *
     * @param a Value summary of first Integer
     * @param b Value summary of second Integer
     * @return The value summary representing the comparison result, with negative indicating a < b,
     *         positive indicating b < a, and 0 indicating a = b.
     */
    public static PrimitiveVS<Integer> compare(PrimitiveVS<Integer> a, PrimitiveVS<Integer> b) {
        return a.apply2(b, (x, y) -> x.compareTo(y));
    }

    /** Get the maximum value that an Integer value summary may take on
     *
     * @return The maximum possible value
     */
    public static Integer maxValue(PrimitiveVS<Integer> a) {
        Integer max = null;
        for (int val : a.getValues()) {
            if (max == null) max = val;
            else if (max < val) max = val;
        }
        return max;
    }

    /** Get the minimum value that an Integer value summary may take on
     *
     * @return The minimum possible value
     */
    public static Integer minValue(PrimitiveVS<Integer> a) {
        Integer min = null;
        for (int val : a.getValues()) {
            if (min == null) min = val;
            else if (min > val) min = val;
        }
        return min;
    }

    /** Detect whether one Integer value summary is equal to another
     *
     * @param a Value summary of first Integer
     * @param b Value summary of second Integer
     * @return The value summary representing whether the first argument is equal to the second
     */
    public static PrimitiveVS<Boolean> equalTo(PrimitiveVS<Integer> a, PrimitiveVS<Integer> b) {
        return a.apply2(b, Integer::equals);
    }
}
