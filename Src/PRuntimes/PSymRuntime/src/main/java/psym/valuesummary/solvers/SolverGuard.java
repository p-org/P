package psym.valuesummary.solvers;

import com.google.common.collect.ImmutableList;
import java.io.Serializable;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.HashMap;
import java.util.List;
import psym.runtime.statistics.SolverStats;

/** Represents the generic solver based implementation of Guard */
public class SolverGuard implements Serializable {
  private static final List<SolverGuard> varList = new ArrayList<>();
  private static final List<SolverGuard> guardList = new ArrayList<>();
  private static final HashMap<Object, SolverGuard> table = new HashMap<Object, SolverGuard>();
  private static boolean resume = false;
  private final SolverGuardType type;
  private final String name;
  private final ImmutableList<SolverGuard> children;
  private final int id;
  private transient Object formula;
  private SolverTrueStatus statusTrue;
  private SolverFalseStatus statusFalse;

  /**
   * Creates a new solver guard
   *
   * @param formula formula represented in solver backend
   * @param type type of the solver guard
   * @param children list of children
   */
  public SolverGuard(
      Object formula, SolverGuardType type, String name, ImmutableList<SolverGuard> children) {
    this.formula = formula;
    this.type = type;
    this.name = name;
    this.children = children;
    this.statusTrue = SolverTrueStatus.Unknown;
    this.statusFalse = SolverFalseStatus.Unknown;
    this.id = guardList.size();
    table.put(formula, this);
    guardList.add(this);
  }

  /** Global reset for the solver guard class */
  public static void reset() {
    table.clear();
  }

  /** Resume solver guard implementation to the new solver backend */
  public static void resumeSolverGuard() {
    // reset the old table
    table.clear();

    // recreate all vars first (in order)
    for (SolverGuard oldGuard : varList) {
      recreateSolverGuard(oldGuard);
    }

    resume = true;
  }

  /** (Experimental) Simplify the solver guard */
  public static void simplifySolverGuard() {
    // reset the old table
    table.clear();

    // recreate all vars first (in order)
    for (SolverGuard oldGuard : varList) {
      simplifySolverGuard(oldGuard);
    }

    // recreate remaining guards
    for (SolverGuard oldGuard : guardList) {
      simplifySolverGuard(oldGuard);
    }
  }

  /**
   * (Experimental) Simplify a solver guard by calling simplify in the solver backend
   *
   * @param original the original solver guard
   */
  private static void simplifySolverGuard(SolverGuard original) {
    // return if already cached in new table
    if (table.containsKey(original.formula)) {
      original.formula = table.get(original.formula).formula;
      return;
    }

    original.formula = SolverEngine.getSolver().simplify(original.formula);

    // cache result
    table.put(original.formula, original);
  }

  /** Switch solver guard implementation to the new solver backend */
  public static void switchSolverGuard() {
    // reset the old table
    table.clear();

    // recreate all vars first (in order)
    for (SolverGuard oldGuard : varList) {
      recreateSolverGuard(oldGuard);
    }

    // recreate remaining guards
    for (SolverGuard oldGuard : guardList) {
      recreateSolverGuard(oldGuard);
    }
  }

  /**
   * Port a solver guard to the new solver backend
   *
   * @param original original solver guard
   */
  private static void recreateSolverGuard(SolverGuard original) {
    // return if already cached in new table
    if (original.formula != null && table.containsKey(original.formula)) {
      original.formula = table.get(original.formula).formula;
      return;
    }

    // process children first
    for (SolverGuard child : original.children) {
      recreateSolverGuard(child);
    }

    // recreate new solver object
    switch (original.type) {
      case TRUE:
        original.formula = SolverEngine.getSolver().constTrue();
        break;
      case FALSE:
        original.formula = SolverEngine.getSolver().constFalse();
        break;
      case VARIABLE:
        original.formula = SolverEngine.getSolver().newVar(original.name);
        break;
      case NOT:
        assert (original.children.size() == 1);
        original.formula = SolverEngine.getSolver().not(original.children.get(0).formula);
        break;
      case AND:
        assert (original.children.size() == 2);
        original.formula =
            SolverEngine.getSolver()
                .and(original.children.get(0).formula, original.children.get(1).formula);
        break;
      case OR:
        assert (original.children.size() == 2);
        original.formula =
            SolverEngine.getSolver()
                .or(original.children.get(0).formula, original.children.get(1).formula);
        break;
      default:
        throw new RuntimeException(
            "Unexpected solver guard of type " + original.type + " : " + original);
    }

    //        System.out.println("Recreated solver guard: " + original);
    //        System.out.println("\thashcode: " +
    // SolverEngine.getSolver().hashCode(original.formula));

    // cache result
    table.put(original.formula, original);
  }

  /**
   * Get the solver guard
   *
   * @param formula formula in solver backend
   * @param type type of solver guard
   * @param children solver guard children
   * @return a cached solver guard or create a new one
   */
  private static SolverGuard getSolverGuard(
      Object formula, SolverGuardType type, String name, ImmutableList<SolverGuard> children) {
    if (table.containsKey(formula)) {
      return table.get(formula);
    }
    return new SolverGuard(formula, type, name, children);
  }

  /**
   * Total number of solver guards stored
   *
   * @return the number of solver guards
   */
  public static int getGuardCount() {
    return table.size();
  }

  /**
   * Get solver guard representing logical `true`
   *
   * @return solver guard representing logical `true`
   */
  private static SolverGuard createTrue() {
    SolverGuard g =
        getSolverGuard(
            SolverEngine.getSolver().constTrue(), SolverGuardType.TRUE, "true", ImmutableList.of());
    g.statusTrue = SolverTrueStatus.True;
    g.statusFalse = SolverFalseStatus.NotFalse;
    return g;
  }

  /**
   * Get solver guard representing logical `false`
   *
   * @return solver guard representing logical `false`
   */
  private static SolverGuard createFalse() {
    SolverGuard g =
        getSolverGuard(
            SolverEngine.getSolver().constFalse(),
            SolverGuardType.FALSE,
            "false",
            ImmutableList.of());
    g.statusTrue = SolverTrueStatus.NotTrue;
    g.statusFalse = SolverFalseStatus.False;
    return g;
  }

  /**
   * Get solver guard representing logical `true`
   *
   * @return solver guard representing logical `true`
   */
  public static SolverGuard constTrue() {
    return createTrue();
  }

  /**
   * Get solver guard representing logical `false`
   *
   * @return solver guard representing logical `false`
   */
  public static SolverGuard constFalse() {
    return createFalse();
  }

  /**
   * Get solver guard representing a new Boolean variable
   *
   * @return solver guard representing a new Boolean variable
   */
  public static SolverGuard newVar() {
    String name = "x" + varList.size();
    SolverGuard g =
        getSolverGuard(
            SolverEngine.getSolver().newVar(name),
            SolverGuardType.VARIABLE,
            name,
            ImmutableList.of());
    g.statusTrue = SolverTrueStatus.NotTrue;
    g.statusFalse = SolverFalseStatus.NotFalse;
    varList.add(g);
    return g;
  }

  /** Sanity check if the list of input solver guards are already stored */
  private static void checkInput(List<SolverGuard> inputs) {
    for (SolverGuard input : inputs) {
      if (resume) {
        recreateSolverGuard(input);
      } else {
        if (input.formula == null || !table.containsKey(input.formula)) {
          System.out.println("\tMissing SolverGuard: " + input);
          System.out.println("\tSolverGuard: " + input);
          System.out.println("\thashcode: " + SolverEngine.getSolver().hashCode(input.formula));
          assert (false);
        }
      }
    }
  }

  /**
   * Get solver guard representing logical `or` on this and a list of solver guards
   *
   * @param others list of solver guards to `or` this with
   * @return solver guard representing logical `or` on this and a list of solver guards
   */
  private static SolverGuard orMany(List<SolverGuard> others) {
    return others.stream().reduce(SolverGuard.constFalse(), SolverGuard::or);
  }

  /**
   * Check if the solver guard is logical `true`
   *
   * @return true iff solver guard logically evaluates to `true`
   */
  public boolean isTrue() {
    switch (statusTrue) {
      case True:
        return true;
      case NotTrue:
        return false;
      default:
        checkInput(List.of(this));
        //                Instant start = Instant.now();
        boolean isSatNeg = SolverEngine.getSolver().isSat(SolverEngine.getSolver().not(formula));
        //                SolverStats.updateSolveGuardTime((Duration.between(start,
        // Instant.now()).toMillis()));
        if (!isSatNeg) {
          statusTrue = SolverTrueStatus.True;
          statusFalse = SolverFalseStatus.NotFalse;
          formula = SolverEngine.getSolver().constTrue();
          return true;
        } else {
          statusTrue = SolverTrueStatus.NotTrue;
          return false;
        }
    }
  }

  /**
   * Check if the solver guard is logical `false`
   *
   * @return true iff solver guard logically evaluates to `false`
   */
  public boolean isFalse() {
    switch (statusFalse) {
      case False:
        return true;
      case NotFalse:
        return false;
      default:
        checkInput(List.of(this));
        //                Instant start = Instant.now();
        boolean isSat = SolverEngine.getSolver().isSat(formula);
        //                SolverStats.updateSolveGuardTime((Duration.between(start,
        // Instant.now()).toMillis()));
        if (!isSat) {
          statusTrue = SolverTrueStatus.NotTrue;
          statusFalse = SolverFalseStatus.False;
          formula = SolverEngine.getSolver().constFalse();
          return true;
        } else {
          statusFalse = SolverFalseStatus.NotFalse;
          return false;
        }
    }
  }

  /**
   * Get solver guard representing logical `not` on this
   *
   * @return solver guard representing logical `not` on this
   */
  public SolverGuard not() {
    checkInput(List.of(this));
    SolverStats.notOperations++;
    //        Instant start = Instant.now();
    SolverGuard result =
        getSolverGuard(
            SolverEngine.getSolver().not(formula), SolverGuardType.NOT, "", ImmutableList.of(this));
    //        SolverStats.updateCreateGuardTime((Duration.between(start,
    // Instant.now()).toMillis()));
    return result;
  }

  /**
   * Get solver guard representing logical `and` on this and other
   *
   * @param other solver guard to `and` this with
   * @return solver guard representing logical `and` on this and other
   */
  public SolverGuard and(SolverGuard other) {
    checkInput(Arrays.asList(this, other));
    SolverStats.andOperations++;
    //        Instant start = Instant.now();
    SolverGuard result =
        getSolverGuard(
            SolverEngine.getSolver().and(formula, other.formula),
            SolverGuardType.AND,
            "",
            ImmutableList.of(this, other));
    //        SolverStats.updateCreateGuardTime((Duration.between(start,
    // Instant.now()).toMillis()));
    return result;
  }

  /**
   * Get solver guard representing logical `or` on this and other
   *
   * @param other solver guard to `or` this with
   * @return solver guard representing logical `or` on this and other
   */
  public SolverGuard or(SolverGuard other) {
    checkInput(Arrays.asList(this, other));
    SolverStats.orOperations++;
    //        Instant start = Instant.now();
    SolverGuard result =
        getSolverGuard(
            SolverEngine.getSolver().or(formula, other.formula),
            SolverGuardType.OR,
            "",
            ImmutableList.of(this, other));
    //        SolverStats.updateCreateGuardTime((Duration.between(start,
    // Instant.now()).toMillis()));
    return result;
  }

  /**
   * Get solver guard representing logical implication
   *
   * @param other right-hand side of the implication
   * @return solver guard representing logical implication (this => other)
   */
  public SolverGuard implies(SolverGuard other) {
    checkInput(Arrays.asList(this, other));
    return (this.not()).or(other);
  }

  /**
   * Get solver guard representing if-then-else
   *
   * @param thenCase solver guard for then case
   * @param elseCase solver guard for else case
   * @return solver guard representing if (this) then thenCase else elseCase
   */
  public SolverGuard ifThenElse(SolverGuard thenCase, SolverGuard elseCase) {
    checkInput(Arrays.asList(this, thenCase, elseCase));
    return (this.and(thenCase)).or((this.not()).and(elseCase));
  }

  /**
   * Pretty print the solver guard
   *
   * @return a string
   */
  @Override
  public String toString() {
    StringBuilder result = new StringBuilder();
    switch (this.type) {
      case TRUE:
        return "true";
      case FALSE:
        return "false";
      case VARIABLE:
        return name;
      case NOT:
        result.append("(not ");
        break;
      case AND:
        result.append("(and ");
        break;
      case OR:
        result.append("(or ");
        break;
    }
    for (int i = 0; i < children.size(); i++) {
      result.append(children.get(i).toString());
      if (i != children.size() - 1) {
        result.append(" ");
        if (result.length() > 80) {
          return result.substring(0, 80) + "...)";
        }
      }
    }
    result.append(")");
    return result.toString();
  }

  /**
   * Pretty print the solver guard formula
   *
   * @return a string
   */
  public String toSolverString() {
    return SolverEngine.getSolver().toString(formula);
  }

  /**
   * Check if this equals o
   *
   * @param o right-hand side of the equality
   * @return true iff this and o are equal
   */
  @Override
  public boolean equals(Object o) {
    if (this == o) return true;
    if (!(o instanceof SolverGuard)) return false;
    SolverGuard that = (SolverGuard) o;
    if (formula == null || that.formula == null) {
      return (id == that.id)
          && (type == that.type)
          && name.equals(that.name)
          && (children == that.children || children.equals(that.children));
    }
    return SolverEngine.getSolver().areEqual(formula, that.formula);
    //        return SolverEngine.getSolver().areEqual(formula, that.formula) &&
    // statusTrue.equals(that.statusTrue) && statusFalse.equals(that.statusFalse);
  }

  /**
   * Hash code of the solver guard
   *
   * @return integer representing the hash of solver guard
   */
  @Override
  public int hashCode() {
    if (formula == null) return id;
    return SolverEngine.getSolver().hashCode(formula);
  }
}
