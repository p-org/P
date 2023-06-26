package psym.valuesummary.solvers;

import java.util.Objects;
import lombok.Getter;
import lombok.Setter;
import psym.runtime.logger.SearchLogger;
import psym.valuesummary.solvers.bdd.PJBDDImpl;
import psym.valuesummary.solvers.sat.expr.ExprLibType;

/** Represents the generic backend engine */
public class SolverEngine {
  @Getter @Setter private static SolverLib solver;
  @Getter @Setter private static SolverType solverType = SolverType.BDD;
  @Getter @Setter private static ExprLibType exprLibType = ExprLibType.Bdd;

  public static void resumeEngine() {
    if (SearchLogger.getVerbosity() > 1) {
      SearchLogger.log("Resuming solver engine:");
      SearchLogger.log(
          String.format("  %-20s->%-20s", getSolverType().toString(), getExprLibType().toString()));
    }
    setSolver(getSolverType(), getExprLibType());
    SolverGuard.resumeSolverGuard();
  }

  public static void resetEngine(SolverType type, ExprLibType etype) {
    if (SearchLogger.getVerbosity() > 1) {
      SearchLogger.log("Setting solver engine to " + type.toString() + " + " + etype.toString());
    }
    setSolver(type, etype);
    SolverGuard.reset();
  }

  public static void cleanupEngine() {
    solver.cleanup();
  }

  public static void setSolver(SolverType type, ExprLibType etype) {
    setSolverType(type);
    setExprLibType(etype);
    if (Objects.requireNonNull(type) == SolverType.BDD) {
      solver = new PJBDDImpl(false);
    } else {
      assert false
          : String.format(
              "Unrecognized solver or expression type: solver type %s with expression type %s",
              type, etype);
    }
  }

  public static int getVarCount() {
    return solver.getVarCount();
  }

  public static int getGuardCount() {
    return SolverGuard.getGuardCount();
    //        return solverImpl.getVarCount();
  }

  public static String getStats() {
    return solver.getStats();
  }
}
