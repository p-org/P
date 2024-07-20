package pexplicit;

import pexplicit.runtime.PExplicitGlobal;
import pexplicit.runtime.STATUS;
import pexplicit.runtime.logger.PExplicitLogger;
import pexplicit.runtime.logger.ScratchLogger;
import pexplicit.runtime.logger.StatWriter;
import pexplicit.runtime.scheduler.Schedule;
import pexplicit.runtime.scheduler.explicit.ExplicitSearchScheduler;
import pexplicit.runtime.scheduler.explicit.SearchStatistics;
import pexplicit.runtime.scheduler.explicit.strategy.SearchStrategyMode;
import pexplicit.runtime.scheduler.explicit.strategy.SearchTask;
import pexplicit.runtime.scheduler.replay.ReplayScheduler;
import pexplicit.utils.exceptions.BugFoundException;
import pexplicit.utils.exceptions.MemoutException;
import pexplicit.utils.monitor.MemoryMonitor;
import pexplicit.utils.monitor.TimeMonitor;
import pexplicit.utils.monitor.TimedCall;

import java.time.Duration;
import java.time.Instant;
import java.util.concurrent.*;

/**
 * Represents the runtime executor that executes the analysis engine
 */
public class RuntimeExecutor {
    private static ExecutorService executor;
    private static Future<Integer> future;
    private static ExplicitSearchScheduler scheduler;

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
        } catch (TimeoutException | BugFoundException e) {
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

    private static void printStats() {
        double timeUsed = (Duration.between(TimeMonitor.getStart(), Instant.now()).toMillis() / 1000.0);
        double memoryUsed = MemoryMonitor.getMemSpent();

        StatWriter.log("time-seconds", String.format("%.1f", timeUsed));
        StatWriter.log("memory-max-MB", String.format("%.1f", MemoryMonitor.getMaxMemSpent()));
        StatWriter.log("memory-current-MB", String.format("%.1f", memoryUsed));

        if (PExplicitGlobal.getConfig().getSearchStrategyMode() != SearchStrategyMode.Replay) {
            StatWriter.log("max-depth-explored", String.format("%d", SearchStatistics.maxSteps));
            scheduler.recordStats();
            if (PExplicitGlobal.getResult().equals("correct for any depth")) {
                PExplicitGlobal.setStatus(STATUS.VERIFIED);
            } else if (PExplicitGlobal.getResult().startsWith("correct up to step")) {
                PExplicitGlobal.setStatus(STATUS.VERIFIED_UPTO_MAX_STEPS);
            }
        }

        StatWriter.log("result", PExplicitGlobal.getResult());
        StatWriter.log("status", String.format("%s", PExplicitGlobal.getStatus()));
    }

    private static void preprocess() {
        PExplicitLogger.logInfo(String.format(".. Test case :: " + PExplicitGlobal.getConfig().getTestDriver()));
        PExplicitLogger.logInfo(String.format("... Checker is using '%s' strategy (seed:%s)",
                PExplicitGlobal.getConfig().getSearchStrategyMode(), PExplicitGlobal.getConfig().getRandomSeed()));

        PExplicitGlobal.setResult("error");

        StatWriter.log("project-name", String.format("%s", PExplicitGlobal.getConfig().getProjectName()));
        StatWriter.log("strategy", String.format("%s", PExplicitGlobal.getConfig().getSearchStrategyMode()));
        StatWriter.log("time-limit-seconds", String.format("%.1f", PExplicitGlobal.getConfig().getTimeLimit()));
        StatWriter.log("memory-limit-MB", String.format("%.1f", PExplicitGlobal.getConfig().getMemLimit()));
    }

    private static void process(boolean resume) throws Exception {
        executor = Executors.newSingleThreadExecutor();
        try {
            TimedCall timedCall = new TimedCall(scheduler, resume);
            future = executor.submit(timedCall);
            runWithTimeout((long) PExplicitGlobal.getConfig().getTimeLimit());
        } catch (TimeoutException e) {
            PExplicitGlobal.setStatus(STATUS.TIMEOUT);
            throw new Exception("TIMEOUT", e);
        } catch (MemoutException | OutOfMemoryError e) {
            PExplicitGlobal.setStatus(STATUS.MEMOUT);
            throw new Exception("MEMOUT", e);
        } catch (BugFoundException e) {
            PExplicitGlobal.setStatus(STATUS.BUG_FOUND);
            PExplicitGlobal.setResult(String.format("found cex of length %d", scheduler.getStepNumber()));
            PExplicitLogger.logStackTrace(e);

            String schFile = PExplicitGlobal.getConfig().getOutputFolder() + "/" + PExplicitGlobal.getConfig().getProjectName() + "_0_0.schedule";
            PExplicitLogger.logInfo(String.format("Writing buggy trace in %s", schFile));
            scheduler.schedule.writeToFile(schFile);

            ReplayScheduler replayer = new ReplayScheduler(scheduler.schedule);
            PExplicitGlobal.setScheduler(replayer);
            try {
                replayer.run();
            } catch (NullPointerException | StackOverflowError | ClassCastException replayException) {
                PExplicitLogger.logStackTrace((Exception) replayException);
                throw new BugFoundException(replayException.getMessage(), replayException);
            } catch (BugFoundException replayException) {
                PExplicitLogger.logStackTrace(replayException);
                throw replayException;
            } catch (Exception replayException) {
                PExplicitLogger.logStackTrace(replayException);
                throw new Exception("Error when replaying the bug", replayException);
            }
            throw new Exception("Failed to replay bug", e);
        } catch (InterruptedException e) {
            PExplicitGlobal.setStatus(STATUS.INTERRUPTED);
            throw new Exception("INTERRUPTED", e);
        } catch (RuntimeException e) {
            PExplicitGlobal.setStatus(STATUS.ERROR);
            throw new Exception("ERROR", e);
        } finally {
            future.cancel(true);
            executor.shutdownNow();
            scheduler.updateResult();
            printStats();
            PExplicitLogger.logEndOfRun(scheduler, Duration.between(TimeMonitor.getStart(), Instant.now()).getSeconds());
            SearchTask.Cleanup();
        }
    }

    public static void runSearch() throws Exception {
        SearchTask.Initialize();
        ScratchLogger.Initialize();

        scheduler = new ExplicitSearchScheduler();
        PExplicitGlobal.setScheduler(scheduler);

        preprocess();
        process(false);
    }

    private static void replaySchedule(String fileName) throws Exception {
        PExplicitLogger.logInfo(String.format("... Reading buggy trace from %s", fileName));

        ReplayScheduler replayer = new ReplayScheduler(Schedule.readFromFile(fileName));
        PExplicitGlobal.setScheduler(replayer);
        try {
            replayer.run();
        } catch (NullPointerException | StackOverflowError | ClassCastException replayException) {
            PExplicitGlobal.setStatus(STATUS.BUG_FOUND);
            PExplicitGlobal.setResult(String.format("found cex of length %d", replayer.getStepNumber()));
            PExplicitLogger.logStackTrace((Exception) replayException);
            throw new BugFoundException(replayException.getMessage(), replayException);
        } catch (BugFoundException replayException) {
            PExplicitGlobal.setStatus(STATUS.BUG_FOUND);
            PExplicitGlobal.setResult(String.format("found cex of length %d", replayer.getStepNumber()));
            PExplicitLogger.logStackTrace(replayException);
            throw replayException;
        } catch (Exception replayException) {
            PExplicitLogger.logStackTrace(replayException);
            throw new Exception("Error when replaying the bug", replayException);
        } finally {
            printStats();
            PExplicitLogger.logEndOfRun(null, Duration.between(TimeMonitor.getStart(), Instant.now()).getSeconds());
        }
    }

    public static void replay() throws Exception {
        preprocess();
        replaySchedule(PExplicitGlobal.getConfig().getReplayFile());
    }

    public static void run() throws Exception {
        // initialize stats writer
        StatWriter.Initialize();

        if (PExplicitGlobal.getConfig().getSearchStrategyMode() == SearchStrategyMode.Replay) {
            replay();
        } else {
            runSearch();
        }
    }
}