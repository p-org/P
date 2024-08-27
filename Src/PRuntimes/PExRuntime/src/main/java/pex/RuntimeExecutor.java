package pex;

import pex.runtime.PExGlobal;
import pex.runtime.STATUS;
import pex.runtime.logger.PExLogger;
import pex.runtime.logger.ScratchLogger;
import pex.runtime.logger.StatWriter;
import pex.runtime.scheduler.Schedule;
import pex.runtime.scheduler.explicit.ExplicitSearchScheduler;
import pex.runtime.scheduler.explicit.SearchStatistics;
import pex.runtime.scheduler.explicit.strategy.SearchStrategyMode;
import pex.runtime.scheduler.explicit.strategy.SearchTask;
import pex.runtime.scheduler.replay.ReplayScheduler;
import pex.utils.exceptions.BugFoundException;
import pex.utils.exceptions.MemoutException;
import pex.utils.monitor.MemoryMonitor;
import pex.utils.monitor.TimeMonitor;
import pex.utils.monitor.TimedCall;

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

        if (PExGlobal.getConfig().getSearchStrategyMode() != SearchStrategyMode.Replay) {
            StatWriter.log("max-depth-explored", String.format("%d", SearchStatistics.maxSteps));
            PExGlobal.recordStats();
            if (PExGlobal.getResult().equals("correct for any depth")) {
                PExGlobal.setStatus(STATUS.VERIFIED);
            } else if (PExGlobal.getResult().startsWith("correct up to step")) {
                PExGlobal.setStatus(STATUS.VERIFIED_UPTO_MAX_STEPS);
            }
        }

        StatWriter.log("result", PExGlobal.getResult());
        StatWriter.log("status", String.format("%s", PExGlobal.getStatus()));
    }

    private static void preprocess() {
        PExLogger.logInfo(String.format(".. Test case :: " + PExGlobal.getConfig().getTestDriver()));
        PExLogger.logInfo(String.format("... Checker is using '%s' strategy (seed:%s)",
                PExGlobal.getConfig().getSearchStrategyMode(), PExGlobal.getConfig().getRandomSeed()));

        PExGlobal.setResult("error");

        StatWriter.log("project-name", String.format("%s", PExGlobal.getConfig().getProjectName()));
        StatWriter.log("mode", String.format("%s", PExGlobal.getConfig().getSearchStrategyMode()));
        StatWriter.log("time-limit-seconds", String.format("%.1f", PExGlobal.getConfig().getTimeLimit()));
        StatWriter.log("memory-limit-MB", String.format("%.1f", PExGlobal.getConfig().getMemLimit()));
    }

    private static void process(boolean resume) throws Exception {
        executor = Executors.newSingleThreadExecutor();
        try {
            TimedCall timedCall = new TimedCall(scheduler, resume);
            future = executor.submit(timedCall);
            runWithTimeout((long) PExGlobal.getConfig().getTimeLimit());
        } catch (TimeoutException e) {
            PExGlobal.setStatus(STATUS.TIMEOUT);
            throw new Exception("TIMEOUT", e);
        } catch (MemoutException | OutOfMemoryError e) {
            PExGlobal.setStatus(STATUS.MEMOUT);
            throw new Exception("MEMOUT", e);
        } catch (BugFoundException e) {
            PExGlobal.setStatus(STATUS.BUG_FOUND);
            PExGlobal.setResult(String.format("found cex of length %d", scheduler.getStepNumber()));
            PExLogger.logStackTrace(e);

            String schFile = PExGlobal.getConfig().getOutputFolder() + "/" + PExGlobal.getConfig().getProjectName() + "_0_0.schedule";
            PExLogger.logInfo(String.format("Writing buggy trace in %s", schFile));
            scheduler.getSchedule().writeToFile(schFile);

            ReplayScheduler replayer = new ReplayScheduler(scheduler.getSchedule());
            PExGlobal.setReplayScheduler(replayer);
            try {
                replayer.run();
            } catch (NullPointerException | StackOverflowError | ClassCastException replayException) {
                PExLogger.logStackTrace((Exception) replayException);
                throw new BugFoundException(replayException.getMessage(), replayException);
            } catch (BugFoundException replayException) {
                PExLogger.logStackTrace(replayException);
                throw replayException;
            } catch (Exception replayException) {
                PExLogger.logStackTrace(replayException);
                throw new Exception("Error when replaying the bug", replayException);
            }
            throw new Exception("Failed to replay bug", e);
        } catch (InterruptedException e) {
            PExGlobal.setStatus(STATUS.INTERRUPTED);
            throw new Exception("INTERRUPTED", e);
        } catch (RuntimeException e) {
            PExGlobal.setStatus(STATUS.ERROR);
            throw new Exception("ERROR", e);
        } finally {
            future.cancel(true);
            executor.shutdownNow();
            PExGlobal.updateResult();
            printStats();
            PExLogger.logEndOfRun(scheduler, Duration.between(TimeMonitor.getStart(), Instant.now()).getSeconds());
            SearchTask.Cleanup();
        }
    }

    public static void runSearch() throws Exception {
        SearchTask.Initialize();
        ScratchLogger.Initialize();

        scheduler = new ExplicitSearchScheduler(1);
        PExGlobal.addSearchScheduler(scheduler);

        preprocess();
        process(false);
    }

    private static void replaySchedule(String fileName) throws Exception {
        PExLogger.logInfo(String.format("... Reading buggy trace from %s", fileName));

        ReplayScheduler replayer = new ReplayScheduler(Schedule.readFromFile(fileName));
        PExGlobal.setReplayScheduler(replayer);
        try {
            replayer.run();
        } catch (NullPointerException | StackOverflowError | ClassCastException replayException) {
            PExGlobal.setStatus(STATUS.BUG_FOUND);
            PExGlobal.setResult(String.format("found cex of length %d", replayer.getStepNumber()));
            PExLogger.logStackTrace((Exception) replayException);
            throw new BugFoundException(replayException.getMessage(), replayException);
        } catch (BugFoundException replayException) {
            PExGlobal.setStatus(STATUS.BUG_FOUND);
            PExGlobal.setResult(String.format("found cex of length %d", replayer.getStepNumber()));
            PExLogger.logStackTrace(replayException);
            throw replayException;
        } catch (Exception replayException) {
            PExLogger.logStackTrace(replayException);
            throw new Exception("Error when replaying the bug", replayException);
        } finally {
            printStats();
            PExLogger.logEndOfRun(null, Duration.between(TimeMonitor.getStart(), Instant.now()).getSeconds());
        }
    }

    public static void replay() throws Exception {
        preprocess();
        replaySchedule(PExGlobal.getConfig().getReplayFile());
    }

    public static void run() throws Exception {
        if (PExGlobal.getConfig().getSearchStrategyMode() == SearchStrategyMode.Replay) {
            replay();
        } else {
            runSearch();
        }
    }
}