package pexplicit;

import pexplicit.runtime.PExplicitGlobal;
import pexplicit.runtime.STATUS;
import pexplicit.runtime.logger.PExplicitLogger;
import pexplicit.runtime.logger.StatWriter;
import pexplicit.runtime.scheduler.Schedule;
import pexplicit.runtime.scheduler.explicit.ExplicitSearchScheduler;
import pexplicit.runtime.scheduler.replay.ReceiverQueueReplayer;
import pexplicit.runtime.scheduler.replay.SenderQueueReplayer;
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
            PExplicitGlobal.setStatus(STATUS.VERIFIED);
        } else if (PExplicitGlobal.getResult().startsWith("correct up to step")) {
            PExplicitGlobal.setStatus(STATUS.VERIFIED_UPTO_MAX_STEPS);
        }
        StatWriter.log("time-search-seconds", String.format("%.1f", searchTime));
    }

    private static void preprocess() {
        PExplicitLogger.logInfo(String.format(".. Test case :: " + PExplicitGlobal.getConfig().getTestDriver()));
        PExplicitLogger.logInfo(String.format("... Checker is using '%s' strategy (seed:%s)",
                PExplicitGlobal.getConfig().getSearchStrategyMode(), PExplicitGlobal.getConfig().getRandomSeed()));

        executor = Executors.newSingleThreadExecutor();

        PExplicitGlobal.setResult("error");

        double preSearchTime =
                TimeMonitor.findInterval(TimeMonitor.getStart());
        StatWriter.log("project-name", String.format("%s", PExplicitGlobal.getConfig().getProjectName()));
        StatWriter.log("strategy", String.format("%s", PExplicitGlobal.getConfig().getSearchStrategyMode()));
        StatWriter.log("time-limit-seconds", String.format("%.1f", PExplicitGlobal.getConfig().getTimeLimit()));
        StatWriter.log("memory-limit-MB", String.format("%.1f", PExplicitGlobal.getConfig().getMemLimit()));
        StatWriter.log("time-pre-seconds", String.format("%.1f", preSearchTime));
    }

    private static void process(boolean resume) throws Exception {
        try {
            TimedCall timedCall = new TimedCall(scheduler, resume);
            future = executor.submit(timedCall);
            TimeMonitor.startInterval();
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

            SenderQueueReplayer senderQueueReplayer = new SenderQueueReplayer(scheduler.schedule);
            PExplicitGlobal.setScheduler(senderQueueReplayer);
            try {
                senderQueueReplayer.run();
            } catch (NullPointerException | StackOverflowError | ClassCastException | BugFoundException senderQueueException) {
                ReceiverQueueReplayer receiverQueueReplayer = new ReceiverQueueReplayer(senderQueueReplayer.getReceiverSemanticsSchedule());
                PExplicitGlobal.setScheduler(receiverQueueReplayer);
                try {
                    receiverQueueReplayer.run();
                } catch (NullPointerException | StackOverflowError | ClassCastException receiverQueueException) {
                    PExplicitLogger.logStackTrace((Exception) receiverQueueException);
                    throw new BugFoundException(receiverQueueException.getMessage(), receiverQueueException);
                } catch (BugFoundException receiverQueueException) {
                    PExplicitLogger.logStackTrace(receiverQueueException);
                    throw receiverQueueException;
                } catch (Exception receiverQueueException) {
                    PExplicitLogger.logStackTrace(receiverQueueException);
                    throw new Exception("Error when replaying the bug in receiver queue semantics", receiverQueueException);
                }
                throw new Exception("Failed to replay bug in receiver queue semantics", e);
            } catch (Exception senderQueueException) {
                PExplicitLogger.logStackTrace(senderQueueException);
                throw new Exception("Error when replaying the bug in sender queue semantics", senderQueueException);
            }
            throw new Exception("Failed to replay bug in sender queue semantics", e);
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
        }
    }

    public static void run() throws Exception {
        scheduler = new ExplicitSearchScheduler();
        PExplicitGlobal.setScheduler(scheduler);

        preprocess();
        process(false);
    }

}