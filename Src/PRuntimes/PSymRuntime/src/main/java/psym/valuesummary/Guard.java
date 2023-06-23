package psym.valuesummary;

import java.io.Serializable;
import java.util.List;
import psym.valuesummary.solvers.SolverGuard;

/**
 * Represents the Schedule, Control, Input (SCI) restrict in the guarded value of a value summary
 * Currently, the guards are implemented using BDDs.
 */
public class Guard implements Serializable {
  /** Represents the boolean formula for the restrict */
  private final SolverGuard guard;

  public Guard(SolverGuard guard) {
    this.guard = guard;
  }

  /**
   * Create a constant false restrict
   *
   * @return Guard representing constant false
   */
  public static Guard constFalse() {
    return new Guard(SolverGuard.constFalse());
  }

  /**
   * Create a constant true restrict
   *
   * @return Guard representing constant true
   */
  public static Guard constTrue() {
    return new Guard(SolverGuard.constTrue());
  }

  /**
   * Perform `or` of a list of Guards
   *
   * @param bddGuards all the Guards to be `OR`ed
   * @return `OR`ed Guard
   */
  public static Guard orMany(List<Guard> bddGuards) {
    return bddGuards.stream().reduce(Guard.constFalse(), Guard::or);
  }

  public static Guard newVar() {
    return new Guard(SolverGuard.newVar());
  }

  /**
   * ValueSummaryChecks whether the logical restrict evaluates to true
   *
   * @return True iff the restrict evaluates to true
   */
  public boolean isTrue() {
    return guard.isTrue();
  }

  /**
   * ValueSummaryChecks whether the logical restrict evaluates to false
   *
   * @return True iff the restrict evaluates to false
   */
  public boolean isFalse() {
    return guard.isFalse();
  }

  /**
   * Performs logical `and` of two guards
   *
   * @param other the other restrict
   * @return restrict that is the `and` of two guards
   */
  public Guard and(Guard other) {
    return new Guard(guard.and(other.guard));
  }

  /**
   * Performs logical `or` of two guards
   *
   * @param other the other restrict
   * @return restrict that is the `or` of two guards
   */
  public Guard or(Guard other) {
    return new Guard(guard.or(other.guard));
  }

  /**
   * Performs the logical `implies` this -> other
   *
   * @param other the other restrict
   * @return
   */
  public Guard implies(Guard other) {
    return new Guard(guard.implies(other.guard));
  }

  /**
   * Perform logical `negation` of the restrict
   *
   * @return negated restrict `not`
   */
  public Guard not() {
    return new Guard(guard.not());
  }

  /**
   * Perform ITE of the given Guard `cond`
   *
   * @param thenCase then Guard
   * @param elseCase else Guard
   * @return resultant ITE Guard
   */
  public Guard ifThenElse(Guard thenCase, Guard elseCase) {
    return new Guard(guard.ifThenElse(thenCase.guard, elseCase.guard));
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
    return guard.equals(guard1.guard);
  }

  @Override
  public int hashCode() {
    return guard.hashCode();
  }
}
