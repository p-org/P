package pex;

import pex.runtime.PExGlobal;
import pex.runtime.STATUS;
import pex.runtime.logger.PExLogger;
import pex.runtime.logger.ScratchLogger;
import pex.runtime.logger.StatWriter;
import pex.runtime.scheduler.Schedule;
import pex.runtime.scheduler.explicit.ExplicitSearchScheduler;
import pex.runtime.scheduler.explicit.strategy.SearchStrategyMode;
import pex.runtime.scheduler.explicit.strategy.SearchTask;
import pex.runtime.scheduler.replay.ReplayScheduler;
import pex.utils.exceptions.BugFoundException;
import pex.utils.exceptions.MemoutException;
import pex.utils.exceptions.TooManyChoicesException;
import pex.utils.monitor.MemoryMonitor;
import pex.utils.monitor.TimeMonitor;
import pex.utils.monitor.TimedCall;

import java.time.Duration;
import java.time.Instant;
import java.util.ArrayList;
import java.util.HashSet;
import java.util.List;
import java.util.Set;
import java.util.concurrent.*;

/**
 * Represents the runtime executor that executes the analysis engine
 */
public class RuntimeExecutor {
    private static final List<Future<Integer>> futures = new ArrayList<>();
    private static ExecutorService executor;

    private static void cancelAllThreads() {
        for (Future<Integer> f : futures) {
            if (!f.isDone() && !f.isCancelled()) {
                f.cancel(true);
            }
        }
    }

    private static void runWithTimeout() throws Exception {
        PExGlobal.setResult("incomplete");
        PExGlobal.printProgressHeader();

        double timeLimit = PExGlobal.getConfig().getTimeLimit();
        Set<Integer> done = new HashSet<>();
        Exception resultException = null;

        PExGlobal.getSearchSchedulers().get(1).getSearchStrategy().createFirstTask();

        for (int i = 0; i < PExGlobal.getConfig().getNumThreads(); i++) {
            TimedCall timedCall = new TimedCall(PExGlobal.getSearchSchedulers().get(i + 1));
            Future<Integer> f = executor.submit(timedCall);
            futures.add(f);
        }

        while (true) {
            if (timeLimit > 0) {
                double elapsedTime = TimeMonitor.getRuntime();
                if (elapsedTime > timeLimit) {
                    cancelAllThreads();
                    resultException = new TimeoutException(String.format("Max time limit reached. Runtime: %.1f seconds", elapsedTime));
                }
            }

            for (int i = 0; i < futures.size(); i++) {
                if (!done.contains(i)) {
                    Future<Integer> f = futures.get(i);
                    if (f.isDone() || f.isCancelled()) {
                        done.add(i);
                        try {
                            f.get();
                        } catch (InterruptedException | CancellationException e) {
                            cancelAllThreads();
                        } catch (OutOfMemoryError e) {
                            cancelAllThreads();
                            resultException = new MemoutException(e.getMessage(), MemoryMonitor.getMemSpent(), e);
                        } catch (ExecutionException e) {
                            if (e.getCause() instanceof MemoutException) {
                                cancelAllThreads();
                                resultException = (MemoutException) e.getCause();
                            } else if (e.getCause() instanceof BugFoundException) {
                                cancelAllThreads();
                                resultException = (BugFoundException) e.getCause();
                            } else if (e.getCause() instanceof TimeoutException) {
                                cancelAllThreads();
                                resultException = (TimeoutException) e.getCause();
                            } else {
                                cancelAllThreads();
                                resultException = new RuntimeException("RuntimeException", e);
                            }
                        }
                    }
                }
            }

            if (done.size() == PExGlobal.getConfig().getNumThreads()) {
                break;
            }

            TimeUnit.MILLISECONDS.sleep(100);
            PExGlobal.printProgress(false);
        }
        PExGlobal.printProgress(true);
        PExGlobal.printProgressFooter();

        if (resultException != null) {
            throw resultException;
        }
    }

    private static void printStats() {
        double timeUsed = (Duration.between(TimeMonitor.getStart(), Instant.now()).toMillis() / 1000.0);
        double memoryUsed = MemoryMonitor.getMemSpent();

        StatWriter.log("time-seconds", String.format("%.1f", timeUsed));
        StatWriter.log("memory-max-MB", String.format("%.1f", MemoryMonitor.getMaxMemSpent()));
        StatWriter.log("memory-current-MB", String.format("%.1f", memoryUsed));

        if (PExGlobal.getConfig().getSearchStrategyMode() != SearchStrategyMode.Replay) {
            StatWriter.log("max-depth-explored", String.format("%d", PExGlobal.getMaxSteps()));
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
        PExLogger.logInfo(String.format("... Checker is using '%s' strategy with %d threads (seed:%s)",
                PExGlobal.getConfig().getSearchStrategyMode(), PExGlobal.getConfig().getNumThreads(), PExGlobal.getConfig().getRandomSeed()));

        PExGlobal.setResult("error");

        StatWriter.log("project-name", String.format("%s", PExGlobal.getConfig().getProjectName()));
        StatWriter.log("mode", String.format("%s", PExGlobal.getConfig().getSearchStrategyMode()));
        StatWriter.log("time-limit-seconds", String.format("%.1f", PExGlobal.getConfig().getTimeLimit()));
        StatWriter.log("memory-limit-MB", String.format("%.1f", PExGlobal.getConfig().getMemLimit()));
    }

    private static void process() throws Exception {
        try {
            runWithTimeout();
        } catch (TimeoutException e) {
            PExGlobal.setStatus(STATUS.TIMEOUT);
            throw new Exception("TIMEOUT", e);
        } catch (MemoutException | OutOfMemoryError e) {
            PExGlobal.setStatus(STATUS.MEMOUT);
            throw new Exception("MEMOUT", e);
        } catch (BugFoundException e) {
            PExGlobal.setStatus(STATUS.BUG_FOUND);
            PExGlobal.setResult(String.format("found cex of length %d", e.getScheduler().getStepNumber()));
            if (e instanceof TooManyChoicesException) {
                PExGlobal.setResult(PExGlobal.getResult() + " (too many choices)");
            }
            e.getScheduler().getLogger().logStackTrace(e);
            PExLogger.logBugFoundInfo(e.getScheduler());

            String schFile = PExGlobal.getConfig().getOutputFolder() + "/" + PExGlobal.getConfig().getProjectName() + "_0_0.schedule";
            PExLogger.logInfo(String.format("Writing buggy trace in %s", schFile));
            e.getScheduler().getSchedule().writeToFile(schFile);

            ReplayScheduler replayer = new ReplayScheduler(e.getScheduler().getSchedule());
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
            cancelAllThreads();
            executor.shutdownNow();
            PExGlobal.updateResult();
            printStats();
            PExLogger.logEndOfRun(Duration.between(TimeMonitor.getStart(), Instant.now()).getSeconds());
            SearchTask.Cleanup();
        }
    }

    public static void runSearch() throws Exception {
        SearchTask.Initialize();
        ScratchLogger.Initialize();

        preprocess();

        executor = Executors.newFixedThreadPool(PExGlobal.getConfig().getNumThreads());

        for (int i = 0; i < PExGlobal.getConfig().getNumThreads(); i++) {
            ExplicitSearchScheduler scheduler = new ExplicitSearchScheduler(i + 1);
            PExGlobal.addSearchScheduler(scheduler);
        }

        process();
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
            if (replayException instanceof TooManyChoicesException) {
                PExGlobal.setResult(PExGlobal.getResult() + " (too many choices)");
            }
            PExLogger.logStackTrace(replayException);
            throw replayException;
        } catch (Exception replayException) {
            PExLogger.logStackTrace(replayException);
            throw new Exception("Error when replaying the bug", replayException);
        } finally {
            printStats();
            PExLogger.logEndOfRun(Duration.between(TimeMonitor.getStart(), Instant.now()).getSeconds());
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