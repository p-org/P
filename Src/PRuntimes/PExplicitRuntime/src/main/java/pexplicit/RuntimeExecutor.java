package pexplicit;

import pexplicit.runtime.PExplicitGlobal;
import pexplicit.runtime.logger.PExplicitLogger;
import pexplicit.runtime.logger.StatWriter;
import pexplicit.runtime.scheduler.explicit.ExplicitSearchScheduler;
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
        double searchTime = TimeMonitor.stopInterval();
        scheduler.recordStats();
        if (PExplicitGlobal.getResult().equals("correct for any depth")) {
            PExplicitGlobal.setStatus("verified");
        }
        StatWriter.log("time-search-seconds", String.format("%.1f", searchTime));
    }

    private static void preprocess() {
        PExplicitLogger.info(String.format(".. Test case :: " + PExplicitGlobal.getConfig().getTestDriver()));
        PExplicitLogger.info(
                String.format(
                        "... Checker is using '%s' strategy (seed:%s)",
                        PExplicitGlobal.getConfig().getStrategy(), PExplicitGlobal.getConfig().getRandomSeed()));
        PExplicitLogger.info("--------------------");

        executor = Executors.newSingleThreadExecutor();

        PExplicitGlobal.setResult("error");

        double preSearchTime =
                TimeMonitor.findInterval(TimeMonitor.getStart());
        StatWriter.log("project-name", String.format("%s", PExplicitGlobal.getConfig().getProjectName()));
        StatWriter.log("strategy", String.format("%s", PExplicitGlobal.getConfig().getStrategy()));
        StatWriter.log("time-limit-seconds", String.format("%.1f", PExplicitGlobal.getConfig().getTimeLimit()));
        StatWriter.log("memory-limit-MB", String.format("%.1f", PExplicitGlobal.getConfig().getMemLimit()));
        StatWriter.log("time-pre-seconds", String.format("%.1f", preSearchTime));
    }

    private static void postprocess(boolean printStats) {
        if (!PExplicitGlobal.getStatus().equals("cex")) {
            scheduler.updateResult();
        }

        Instant end = Instant.now();
        if (printStats) {
            printStats();
        }
        PExplicitLogger.logEndOfRun(
                scheduler.getIteration(),
                scheduler.getIteration(),
                Duration.between(TimeMonitor.getStart(), end).getSeconds(),
                PExplicitGlobal.getResult());
    }

    private static void process(boolean resume) throws Exception {
        try {
            TimedCall timedCall = new TimedCall(scheduler, resume);
            future = executor.submit(timedCall);
            TimeMonitor.startInterval();
            runWithTimeout((long) PExplicitGlobal.getConfig().getTimeLimit());
            PExplicitGlobal.setStatus("completed");
        } catch (TimeoutException e) {
            PExplicitGlobal.setStatus("timeout");
            throw new Exception("TIMEOUT", e);
        } catch (MemoutException | OutOfMemoryError e) {
            PExplicitGlobal.setStatus("memout");
            throw new Exception("MEMOUT", e);
        } catch (BugFoundException e) {
            PExplicitGlobal.setStatus("cex");
            PExplicitGlobal.setResult(String.format("found cex of length %d", scheduler.schedule.getStepNumber()));

            postprocess(true);
            PExplicitLogger.info(e.toString());
            if (PExplicitGlobal.getConfig().getVerbosity() > 0) {
                PExplicitLogger.printStackTrace(e, false);
            }
            throw e;
        } catch (InterruptedException e) {
            PExplicitGlobal.setStatus("interrupted");
            throw new Exception("INTERRUPTED", e);
        } catch (RuntimeException e) {
            PExplicitGlobal.setStatus("error");
            throw new Exception("ERROR", e);
        } finally {
            future.cancel(true);
            executor.shutdownNow();
            postprocess(!PExplicitGlobal.getStatus().equals("cex"));
        }
    }

    public static void run() throws Exception {
        scheduler = new ExplicitSearchScheduler();
        PExplicitGlobal.setScheduler(scheduler);

        preprocess();
        process(false);
    }

}