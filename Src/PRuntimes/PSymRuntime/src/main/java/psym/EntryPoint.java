package psym;

import java.time.Duration;
import java.time.Instant;
import java.util.concurrent.*;
import psym.runtime.Concretizer;
import psym.runtime.PSymGlobal;
import psym.runtime.Program;
import psym.runtime.logger.*;
import psym.runtime.scheduler.SearchScheduler;
import psym.runtime.scheduler.explicit.ExplicitSearchScheduler;
import psym.runtime.scheduler.replay.ReplayScheduler;
import psym.runtime.scheduler.symbolic.SymbolicSearchScheduler;
import psym.runtime.scheduler.symmetry.SymmetryMode;
import psym.runtime.scheduler.symmetry.SymmetryTracker;
import psym.utils.exception.BugFoundException;
import psym.utils.exception.LivenessException;
import psym.utils.exception.MemoutException;
import psym.utils.monitor.MemoryMonitor;
import psym.utils.monitor.TimeMonitor;
import psym.utils.monitor.TimedCall;
import psym.valuesummary.Guard;
import psym.valuesummary.solvers.SolverEngine;

public class EntryPoint {
  private static ExecutorService executor;
  private static Future<Integer> future;
  private static String status;
  private static SearchScheduler scheduler;
  private static String mode;

  private static void runWithTimeout(long timeLimit)
      throws TimeoutException,
          InterruptedException,
          RuntimeException {
    try {
      if (timeLimit > 0) {
        future.get(timeLimit, TimeUnit.SECONDS);
      } else {
        future.get();
      }
    } catch (TimeoutException e) {
      throw e;
    } catch (BugFoundException e) {
      throw e;
    } catch (OutOfMemoryError e) {
      throw new MemoutException(e.getMessage(), MemoryMonitor.getMemSpent(), e);
    } catch (ExecutionException e) {
      if (e.getCause() instanceof MemoutException) {
        throw (MemoutException) e.getCause();
      } else if (e.getCause() instanceof BugFoundException) {
        throw (BugFoundException) e.getCause();
      } else if (e.getCause() instanceof TimeoutException) {
        throw (TimeoutException) e.getCause();
      } else {
        throw new RuntimeException("RuntimeException", e);
      }
    } catch (InterruptedException e) {
      throw e;
    }
  }

  private static void print_stats() {
    double searchTime = TimeMonitor.getInstance().stopInterval();
    TimeMonitor.getInstance().startInterval();
    scheduler.print_search_stats();
    if (scheduler.isFinalResult && scheduler.result.equals("correct for any depth")) {
      status = "proved";
    }
    StatWriter.log("time-search-seconds", String.format("%.1f", searchTime));
    if (PSymGlobal.getConfiguration().isExplicit()) {
      ((ExplicitSearchScheduler) scheduler).reportEstimatedCoverage();
    }
    StatWriter.log("status", String.format("%s", status));
  }

  private static void preprocess() {
    PSymLogger.info(String.format(".. Test case :: " + PSymGlobal.getConfiguration().getTestDriver()));
    PSymLogger.info(
        String.format(
            "... Checker is using '%s' strategy (seed:%s)",
                PSymGlobal.getConfiguration().getStrategy(), PSymGlobal.getConfiguration().getRandomSeed()));
    PSymLogger.info("--------------------");

    executor = Executors.newSingleThreadExecutor();
    status = "error";
    if (PSymGlobal.getConfiguration().isSymbolic()) {
      mode = "symbolic";
    } else {
      mode = "single";
    }
    if (PSymGlobal.getConfiguration().getSymmetryMode() != SymmetryMode.None) {
      SymmetryTracker.setScheduler(scheduler);
    }

    double preSearchTime =
        TimeMonitor.getInstance().findInterval(TimeMonitor.getInstance().getStart());
    StatWriter.log("project-name", String.format("%s", PSymGlobal.getConfiguration().getProjectName()));
    StatWriter.log("mode", String.format("%s", mode));
    StatWriter.log("solver", String.format("%s", PSymGlobal.getConfiguration().getSolverType().toString()));
    StatWriter.log("expr-type", String.format("%s", PSymGlobal.getConfiguration().getExprLibType().toString()));
    StatWriter.log("time-limit-seconds", String.format("%.1f", PSymGlobal.getConfiguration().getTimeLimit()));
    StatWriter.log("memory-limit-MB", String.format("%.1f", PSymGlobal.getConfiguration().getMemLimit()));
    StatWriter.log("time-pre-seconds", String.format("%.1f", preSearchTime));
    Concretizer.print = (PSymGlobal.getConfiguration().getVerbosity() > 8);
  }

  private static void postprocess(boolean printStats) {
    Instant end = Instant.now();
    if (printStats) {
      print_stats();
    }
    if (scheduler instanceof ExplicitSearchScheduler) {
      ExplicitSearchScheduler explicitSearchScheduler = (ExplicitSearchScheduler) scheduler;
      PSymLogger.finishedExplicit(
              explicitSearchScheduler.getIter(),
              explicitSearchScheduler.getIter() - explicitSearchScheduler.getStart_iter(),
              Duration.between(TimeMonitor.getInstance().getStart(), end).getSeconds(),
              scheduler.result);
    } else {
      assert (scheduler instanceof SymbolicSearchScheduler);
      PSymLogger.finishedSymbolic(
              Duration.between(TimeMonitor.getInstance().getStart(), end).getSeconds(),
              scheduler.result);
    }
  }

  private static void process(boolean resume) throws Exception {
    try {
      TimedCall timedCall = new TimedCall(scheduler, resume);
      future = executor.submit(timedCall);
      TimeMonitor.getInstance().startInterval();
      runWithTimeout((long) PSymGlobal.getConfiguration().getTimeLimit());
      status = "completed";
    } catch (TimeoutException e) {
      status = "timeout";
      throw new Exception("TIMEOUT", e);
    } catch (MemoutException | OutOfMemoryError e) {
      status = "memout";
      throw new Exception("MEMOUT", e);
    } catch (BugFoundException e) {
      status = "cex";
      scheduler.result = "found cex of length " + scheduler.getDepth();
      scheduler.isFinalResult = true;
      postprocess(true);
      PSymLogger.info(e.toString());
      if (PSymGlobal.getConfiguration().getVerbosity() > 0) {
        PSymGlobal.printStackTrace(e, false);
      }

      PSymLogger.setVerbosity(1);
      TraceLogger.setVerbosity(1);
      SearchLogger.disable();
      CoverageWriter.disable();
      Guard pc = e.pathConstraint;

      ReplayScheduler replayScheduler =
          new ReplayScheduler(
              scheduler.getProgram(),
              scheduler.getSchedule(),
              pc,
              scheduler.getDepth());
      PSymGlobal.setScheduler(replayScheduler);
      replay(replayScheduler);
    } catch (InterruptedException e) {
      status = "interrupted";
      throw new Exception("INTERRUPTED", e);
    } catch (RuntimeException e) {
      status = "error";
      throw new Exception("ERROR", e);
    } finally {
      //            GlobalData.getChoiceLearningStats().printQTable();
      future.cancel(true);
      executor.shutdownNow();
      TraceLogger.setVerbosity(0);
      postprocess(!status.equals("cex"));
    }
  }

  public static void run(Program p) throws Exception {
    if (PSymGlobal.getConfiguration().isSymbolic()) {
      scheduler = new SymbolicSearchScheduler(p);
    } else {
      scheduler = new ExplicitSearchScheduler(p);
    }
    PSymGlobal.setScheduler(scheduler);

    preprocess();
    process(false);
  }

  public static void resume() throws Exception {
    assert (PSymGlobal.getConfiguration().isExplicit());
    scheduler = ExplicitSearchScheduler.readFromFile(PSymGlobal.getConfiguration().getReadFromFile());
    PSymGlobal.setScheduler(scheduler);
    TraceLogger.setVerbosity(PSymGlobal.getConfiguration().getVerbosity());
    SolverEngine.resumeEngine();

    preprocess();
    process(true);
  }

  private static void replay(ReplayScheduler replayScheduler)
      throws RuntimeException, TimeoutException {
    try {
      ScheduleWriter.Initialize(PSymGlobal.getConfiguration().getProjectName(), PSymGlobal.getConfiguration().getOutputFolder());
      replayScheduler.doSearch();
      status = "error";
      throw new RuntimeException("ERROR: Failed to replay counterexample");
    } catch (BugFoundException e) {
      PSymLogger.info(e.toString());
      PSymGlobal.printStackTrace(e, true);
      PSymLogger.info("Checker found a bug.");
      PSymLogger.info("... Emitting traces:");
      PSymLogger.info(String.format("..... Writing %s", ScheduleWriter.getFileName()));
      throw new BugFoundException(
          "Found bug: " + e.getLocalizedMessage(),
          replayScheduler.getPathConstraint(),
          e);
    }
  }

  public static void replayBug(ReplayScheduler replayScheduler)
      throws RuntimeException, TimeoutException {
    SolverEngine.resumeEngine();
    if (PSymGlobal.getConfiguration().getVerbosity() == 0) {
      PSymLogger.setVerbosity(1);
      TraceLogger.setVerbosity(1);
    }
    TraceLogger.enable();
    PSymGlobal.setScheduler(replayScheduler);

    PSymLogger.info(String.format(".. Test case :: " + PSymGlobal.getConfiguration().getTestDriver()));
    PSymLogger.info("... Checker is using 'replay' strategy");

    replay(replayScheduler);
  }

  public static void writeToFile() throws Exception {
    assert (PSymGlobal.getConfiguration().isExplicit());
    ((ExplicitSearchScheduler) scheduler).writeToFile();
  }
}
