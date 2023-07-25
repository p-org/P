package psym.runtime.statistics;

import java.util.concurrent.TimeoutException;
import psym.runtime.logger.SearchLogger;
import psym.utils.exception.MemoutException;
import psym.utils.monitor.MemoryMonitor;
import psym.utils.monitor.TimeMonitor;
import psym.valuesummary.solvers.SolverEngine;

public class SolverStats {
  public static int andOperations = 0;
  public static int orOperations = 0;
  public static int notOperations = 0;
  public static int isSatOperations = 0;
  public static int isSatResult = 0;
  public static double timeTotalCreateGuards = 0; // total time in milliseconds to create guards
  public static double timeMaxCreateGuards = 0; // max time in milliseconds to create guards
  public static double timeTotalSolveGuards = 0; // total time in milliseconds to solve guards
  public static double timeMaxSolveGuards = 0; // max time in milliseconds to solve guards

  public static void updateCreateGuardTime(long timeSpent) throws TimeoutException, MemoutException {
    timeTotalCreateGuards += timeSpent;
    if (timeMaxCreateGuards < timeSpent) timeMaxCreateGuards = timeSpent;

    // check if reached time or memory limit
    checkResourceLimits();
  }

  public static void updateSolveGuardTime(long timeSpent) throws TimeoutException, MemoutException {
    timeTotalSolveGuards += timeSpent;
    if (timeMaxSolveGuards < timeSpent) timeMaxSolveGuards = timeSpent;
    // check if reached time or memory limit
    checkResourceLimits();
  }

  public static void checkResourceLimits() throws TimeoutException, MemoutException {
    TimeMonitor.getInstance().checkTimeout();
    MemoryMonitor.checkMemout();
  }

  public static double getDoublePercent(double spent, double total) {
    return (spent == 0 ? 0.0 : (spent * 100.0 / total));
  }

  public static double isSatPercent(int isSatOps, int isSatRes) {
    return (isSatOps == 0 ? 0.0 : (isSatRes * 100.0 / isSatOps));
  }

  public static void logSolverStats() {
    SearchLogger.log("#-vars", String.format("%d", SolverEngine.getVarCount()));
    SearchLogger.log("#-guards", String.format("%d", SolverEngine.getGuardCount()));
    SearchLogger.log("#-expr", String.format("%d", SolverEngine.getSolver().getExprCount()));
    SearchLogger.log("#-and-ops", String.format("%d", andOperations));
    SearchLogger.log("#-or-ops", String.format("%d", orOperations));
    SearchLogger.log("#-not-ops", String.format("%d", notOperations));
    SearchLogger.log(
        "solver-#-nodes", String.format("%d", SolverEngine.getSolver().getNodeCount()));
    SearchLogger.log("solver-#-sat-ops", String.format("%d", isSatOperations));
    SearchLogger.log("solver-#-sat-ops-sat", String.format("%d", isSatResult));
    SearchLogger.log(
        "solver-%-sat-ops-sat", String.format("%.1f", isSatPercent(isSatOperations, isSatResult)));
  }
}
