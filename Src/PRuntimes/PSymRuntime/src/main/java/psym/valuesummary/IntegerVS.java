package psym.valuesummary;

/** Class containing static methods that are useful for Integer primitive value summaries */
public class IntegerVS {
  /**
   * Add two Integer primitive value summaries
   *
   * @param a First value summary
   * @param b Second value summary
   * @return The value summary representing the arguments' sum
   */
  public static PrimitiveVS<Integer> add(PrimitiveVS<Integer> a, PrimitiveVS<Integer> b) {
    return a.apply(b, Integer::sum);
  }

  /**
   * Add a concrete int to an Integer primitive value summary
   *
   * @param a First value summary
   * @param i Second value summary
   * @return The value summary representing the arguments' sum
   */
  public static PrimitiveVS<Integer> add(PrimitiveVS<Integer> a, int i) {
    return a.apply(x -> x + i);
  }

  /**
   * Subtract two Integer primitive value summaries
   *
   * @param a Value summary of first Integer
   * @param b Value summary of Integer to be subtracted
   * @return The value summary representing the arguments' difference
   */
  public static PrimitiveVS<Integer> subtract(PrimitiveVS<Integer> a, PrimitiveVS<Integer> b) {
    return a.apply(b, (x, y) -> x - y);
  }

  /**
   * Subtract a concrete int from an Integer primitive value summary
   *
   * @param a Value summary of first Integer
   * @param i Value of int to be subtracted
   * @return The value summary representing the arguments' difference
   */
  public static PrimitiveVS<Integer> subtract(PrimitiveVS<Integer> a, int i) {
    return a.apply(x -> x - i);
  }

  /**
   * Detect whether one Integer value summary is less than another
   *
   * @param a Value summary of first Integer
   * @param b Value summary of second Integer
   * @return The value summary representing whether the first argument is less than the second
   */
  public static PrimitiveVS<Boolean> lessThan(PrimitiveVS<Integer> a, PrimitiveVS<Integer> b) {
    return a.apply(b, (x, y) -> x < y);
  }

  /**
   * Detect whether an int is less than an Integer value summary
   *
   * @param a Value of the int
   * @param b Value summary of second Integer
   * @return The value summary representing whether the first argument is less than the second
   */
  public static PrimitiveVS<Boolean> lessThan(int a, PrimitiveVS<Integer> b) {
    return b.apply(x -> a < x);
  }

  /**
   * Detect whether an Integer value summary is less than an int
   *
   * @param a Value summary of first Integer
   * @param b Value of the int
   * @return The value summary representing whether the first argument is less than the second
   */
  public static PrimitiveVS<Boolean> lessThan(PrimitiveVS<Integer> a, int b) {
    return a.apply(x -> x < b);
  }

  /**
   * Compare two Integer value summaries
   *
   * @param a Value summary of first Integer
   * @param b Value summary of second Integer
   * @return The value summary representing the comparison result, with negative indicating a < b,
   *     positive indicating b < a, and 0 indicating a = b.
   */
  public static PrimitiveVS<Integer> compare(PrimitiveVS<Integer> a, PrimitiveVS<Integer> b) {
    return a.apply(b, Integer::compareTo);
  }

  /**
   * Get the maximum value that an Integer value summary may take on
   *
   * @return The maximum possible value
   */
  public static Integer maxValue(PrimitiveVS<Integer> a) {
    return a.getValues().stream().max(Integer::compare).orElse(null);
  }

  /**
   * Return true iff an Integer value summary may take a positive value
   *
   * @return The maximum possible value
   */
  public static Boolean hasPositiveValue(PrimitiveVS<Integer> a) {
    for (Integer val : a.getValues()) {
      if (val > 0) {
        return true;
      }
    }
    return false;
  }

  /**
   * Get the minimum value that an Integer value summary may take on
   *
   * @return The minimum possible value
   */
  public static Integer minValue(PrimitiveVS<Integer> a) {
    return a.getValues().stream().min(Integer::compare).orElse(null);
  }

  /**
   * Detect whether one Integer value summary is equal to another
   *
   * @param a Value summary of first Integer
   * @param b Value summary of second Integer
   * @return The value summary representing whether the first argument is equal to the second
   */
  public static PrimitiveVS<Boolean> equalTo(PrimitiveVS<Integer> a, PrimitiveVS<Integer> b) {
    return a.apply(b, Integer::equals);
  }
}
