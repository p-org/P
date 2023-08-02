package psym.valuesummary.util;

import java.util.ArrayList;
import java.util.List;
import psym.valuesummary.*;

/** This class implements different checks for invariants on Guards and ValueSummaries */
public class ValueSummaryChecks {

  /**
   * Do the provided Guards implement a disjoint union?
   *
   * @param bdds The Guards
   */
  public static boolean disjointUnion(Iterable<Guard> bdds) {
    Guard acc = Guard.constFalse();
    for (Guard bdd : bdds) {
      if (!acc.and(bdd).isFalse()) return false;
      // System.out.println(acc + " and " + bdd + " are disjoint");
      acc = acc.or(bdd);
    }
    return true;
  }

  /**
   * Are the provided Guards the same universe?
   *
   * @param a The first Guard
   * @param b The second Guard
   */
  public static boolean hasSameUniverse(Guard a, Guard b) {
    return a.implies(b).isTrue() && b.implies(a).isTrue();
  }

  /**
   * Are the provided ValueSummaries equal under the given guard?
   *
   * @param a The first ValueSummary
   * @param b The second ValueSummary
   * @param guard The guard
   * @return Whether or not they are equal under the given guard
   */
  public static boolean equalUnder(ValueSummary a, ValueSummary b, Guard guard) {
    if (!a.getClass().equals(b.getClass())) return false;
    return !BooleanVS.isEverFalse(
        a.restrict(guard).symbolicEquals(b.restrict(guard), guard).restrict(guard));
  }

  /**
   * Is the provided PrimVS such that its guarded values have disjoint guards?
   *
   * @param vs The PrimVS
   */
  public static boolean disjointGuards(PrimitiveVS<?> vs) {
    List<Guard> guards = new ArrayList<>();
    for (GuardedValue<?> gv : vs.getGuardedValues()) {
      guards.add(gv.getGuard());
    }
    return disjointUnion(guards);
  }

  /**
   * Is the provided Guard inside another?
   *
   * @param a The Guard
   * @param b The enclosing Guard
   * @return Whether or not the Guard is included in the other
   */
  public static boolean includedIn(Guard a, Guard b) {
    return a.implies(b).isTrue();
  }

  public static boolean noRepeats(List a) {
    boolean res = true;
    for (int i = 0; i < a.size(); i++) {
      Object itm = a.get(i);
      for (int j = 0; j < a.size(); j++) {
        if (i != j) res = res && !(itm.equals(a.get(j)));
      }
    }
    return res;
  }

  /**
   * Will print the provided string if the condition doesn't hold and throw an exception
   *
   * @param msg What to print
   * @param cond What to assert
   */
  public static void check(String msg, boolean cond) {
    if (!cond) {
      throw new CheckViolatedException(msg);
    }
  }

  private static class CheckViolatedException extends RuntimeException {
    public CheckViolatedException(String msg) {
      super(msg);
    }
  }
}
