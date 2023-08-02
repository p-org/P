package psym.valuesummary;

import java.util.HashMap;
import java.util.Map;

/** Class containing static methods that are useful for Boolean primitive value summaries */
public final class BooleanVS {
  private BooleanVS() {}

  /**
   * Create a PrimitiveVS representing boolean `true` under the `guard`
   *
   * @param guard the guard under which the Boolean VS should be true
   * @return Primitive Boolean VS
   */
  public static PrimitiveVS<Boolean> trueUnderGuard(Guard guard) {
    if (guard.isFalse()) {
      return new PrimitiveVS<>(false);
    }

    if (guard.isTrue()) {
      return new PrimitiveVS<>(true);
    }

    // return a value summary which is true under the guard
    final Map<Boolean, Guard> values = new HashMap<>();
    values.put(true, guard);
    values.put(false, guard.not());
    return new PrimitiveVS<>(values);
  }

  /**
   * Get the condition/guard under which a Boolean value summary is true
   *
   * @param primVS A primitive boolean value summary
   * @return Guard under which the primitive value summary has value `true`
   */
  public static Guard getTrueGuard(PrimitiveVS<Boolean> primVS) {
    return primVS.getGuardFor(true);
  }

  /**
   * Get the condition/guard under which a Boolean value summary is false
   *
   * @param primVS A primitive boolean value summary
   * @return Guard under which the primitive value summary has value `false`
   */
  public static Guard getFalseGuard(PrimitiveVS<Boolean> primVS) {
    return primVS.getGuardFor(false);
  }

  /**
   * Get the conjunction of two Boolean value summaries
   *
   * @param a The first Boolean value summary
   * @param b The second Boolean value summary
   * @return Boolean value summary for the arguments' conjunction
   */
  public static PrimitiveVS<Boolean> and(PrimitiveVS<Boolean> a, PrimitiveVS<Boolean> b) {
    return a.apply(b, (x, y) -> x && y);
  }

  /**
   * Get the conjunction of a Boolean value summary and a boolean value
   *
   * @param a The first conjunct's Boolean value summary
   * @param b The second boolean's value
   * @return Boolean value summary for the arguments' conjunction
   */
  public static PrimitiveVS<Boolean> and(PrimitiveVS<Boolean> a, boolean b) {
    return a.apply(x -> x && b);
  }

  /**
   * Get the conjunction of a boolean and a Boolean value summary
   *
   * @param a The first boolean's value
   * @param b The second conjunct's Boolean value summary
   * @return Boolean value summary for the arguments' conjunction
   */
  public static PrimitiveVS<Boolean> and(boolean a, PrimitiveVS<Boolean> b) {
    return and(b, a);
  }

  /**
   * Get the disjunction of two Boolean value summaries
   *
   * @param a The first Boolean value summary
   * @param b The second Boolean value summary
   * @return Boolean value summary for the arguments' disjunction
   */
  public static PrimitiveVS<Boolean> or(PrimitiveVS<Boolean> a, PrimitiveVS<Boolean> b) {
    return a.apply(b, (x, y) -> x || y);
  }

  /**
   * Get whether a Boolean value summary is always false
   *
   * @param b The Boolean value summary
   * @return Whether the provided value summary is always false
   */
  public static boolean isFalse(PrimitiveVS<Boolean> b) {
    return getFalseGuard(b).isTrue();
  }

  /**
   * Get whether a Boolean value summary is ever true
   *
   * @param b The Boolean value summary
   * @return Whether the provided value summary can be true
   */
  public static boolean isEverTrue(PrimitiveVS<Boolean> b) {
    return !getTrueGuard(b).isFalse();
  }

  /**
   * Get whether a Boolean value summary is ever false
   *
   * @param b The Boolean value summary
   * @return Whether or not the provided value summary can be false
   */
  public static boolean isEverFalse(PrimitiveVS<Boolean> b) {
    return !getFalseGuard(b).isFalse();
  }
}
