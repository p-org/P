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

    /**
     * Cancels all running futures
     */
    private static void cancelAllThreads() {
        futures.stream()
            .filter(f -> !f.isDone() && !f.isCancelled())
            .forEach(f -> f.cancel(true));
    }

    /**
     * Runs the analysis with timeout handling
     * 
     * @throws Exception if any error occurs during execution
     */
    private static void runWithTimeout() throws Exception {
        PExGlobal.setResult("incomplete");
        PExGlobal.printProgressHeader();

        double timeLimit = PExGlobal.getConfig().getTimeLimit();
        Set<Integer> completedTasks = new HashSet<>();
        Exception resultException = null;

        // Create first search task
        PExGlobal.getSearchSchedulers().get(1).getSearchStrategy().createFirstTask();

        // Submit all tasks to executor
        submitTasks();

        // Monitor task execution
        while (true) {
            // Check if time limit exceeded
            if (timeLimit > 0 && TimeMonitor.getRuntime() > timeLimit) {
                cancelAllThreads();
                resultException = new TimeoutException(
                    String.format("Max time limit reached. Runtime: %.1f seconds", TimeMonitor.getRuntime()));
                break;
            }

            // Check task completion status
            boolean allDone = checkAndProcessCompletedTasks(completedTasks, resultException);
            if (allDone || resultException != null) {
                break;
            }

            // Sleep briefly and update progress
            TimeUnit.MILLISECONDS.sleep(100);
            PExGlobal.printProgress(false);
        }
        
        PExGlobal.printProgress(true);
        PExGlobal.printProgressFooter();

        if (resultException != null) {
            throw resultException;
        }
    }
    
    /**
     * Submit all search tasks to the executor
     */
    private static void submitTasks() {
        for (int i = 0; i < PExGlobal.getConfig().getNumThreads(); i++) {
            TimedCall timedCall = new TimedCall(PExGlobal.getSearchSchedulers().get(i + 1));
            Future<Integer> future = executor.submit(timedCall);
            futures.add(future);
        }
    }
    
    /**
     * Check and process completed tasks
     * 
     * @param completedTasks Set of tasks that have completed execution
     * @param resultException Exception to store any error encountered
     * @return true if all tasks are completed
     */
    private static boolean checkAndProcessCompletedTasks(Set<Integer> completedTasks, Exception resultException) {
        for (int i = 0; i < futures.size(); i++) {
            if (completedTasks.contains(i)) {
                continue;
            }
                
            Future<Integer> future = futures.get(i);
            if (future.isDone() || future.isCancelled()) {
                completedTasks.add(i);
                try {
                    future.get();
                } catch (InterruptedException | CancellationException e) {
                    cancelAllThreads();
                    return true;
                } catch (OutOfMemoryError e) {
                    cancelAllThreads();
                    resultException = new MemoutException(e.getMessage(), MemoryMonitor.getMemSpent(), e);
                    return true;
                } catch (ExecutionException e) {
                    cancelAllThreads();
                    
                    // Handle different exception types
                    if (e.getCause() instanceof MemoutException) {
                        resultException = (MemoutException) e.getCause();
                    } else if (e.getCause() instanceof BugFoundException) {
                        resultException = (BugFoundException) e.getCause();
                    } else if (e.getCause() instanceof TimeoutException) {
                        resultException = (TimeoutException) e.getCause();
                    } else {
                        resultException = new RuntimeException("Unexpected runtime exception", e);
                    }
                    return true;
                }
            }
        }
            
        return completedTasks.size() == PExGlobal.getConfig().getNumThreads();
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

    /**
     * Process the main execution and handle exceptions
     * 
     * @throws Exception if any error occurs during processing
     */
    private static void process() throws Exception {
        try {
            runWithTimeout();
        } catch (TimeoutException e) {
            handleTimeoutException(e);
        } catch (MemoutException | OutOfMemoryError e) {
            handleMemoryException(e);
        } catch (BugFoundException e) {
            handleBugFoundException(e);
        } catch (InterruptedException e) {
            handleInterruptedException(e);
        } catch (RuntimeException e) {
            handleRuntimeException(e);
        } finally {
            cleanupResources();
        }
    }
    
    /**
     * Handle timeout exception
     * 
     * @param e Timeout exception
     * @throws Exception with timeout message
     */
    private static void handleTimeoutException(TimeoutException e) throws Exception {
        PExGlobal.setStatus(STATUS.TIMEOUT);
        throw new Exception("TIMEOUT", e);
    }
    
    /**
     * Handle memory related exceptions
     * 
     * @param e Memory related exception or error
     * @throws Exception with memory error message
     */
    private static void handleMemoryException(Throwable e) throws Exception {
        PExGlobal.setStatus(STATUS.MEMOUT);
        throw new Exception("MEMOUT", e);
    }
    
    /**
     * Handle bug found exception
     * 
     * @param e Bug found exception
     * @throws Exception with bug details
     */
    private static void handleBugFoundException(BugFoundException e) throws Exception {
        PExGlobal.setStatus(STATUS.BUG_FOUND);
        
        // Set result with bug information
        PExGlobal.setResult(String.format("found cex of length %,d", e.getScheduler().getStepNumber()));
        if (e instanceof TooManyChoicesException) {
            PExGlobal.setResult(PExGlobal.getResult() + " (too many choices)");
        }
        
        // Log bug information
        e.getScheduler().getLogger().logStackTrace(e);
        PExLogger.logBugFoundInfo(e.getScheduler());

        // Save bug trace
        String scheduleFile = PExGlobal.getConfig().getOutputFolder() + "/" 
            + PExGlobal.getConfig().getProjectName() + "_0_0.schedule";
        PExLogger.logInfo(String.format("... Writing buggy trace in %s", scheduleFile));
        e.getScheduler().getSchedule().writeToFile(scheduleFile);

        // Try to replay the bug
        replayBug(e.getScheduler().getSchedule(), e);
        
        throw new Exception("Failed to replay bug", e);
    }
    
    /**
     * Replay a bug using its schedule
     * 
     * @param schedule Bug schedule to replay
     * @param originalException The original exception that triggered replay
     * @throws Exception if replay fails
     */
    private static void replayBug(Schedule schedule, Exception originalException) throws Exception {
        BugFoundException bugException = null;
        if (originalException instanceof BugFoundException) {
            bugException = (BugFoundException) originalException;
        }
        
        ReplayScheduler replayer = new ReplayScheduler(schedule, bugException);
        PExGlobal.setReplayScheduler(replayer);
        PExLogger.logReplayerInfo(replayer);
        
        try {
            replayer.run();
        } catch (NullPointerException | StackOverflowError | ClassCastException e) {
            logReplayException(replayer, (Exception) wrapThrowable(e));
            throw new BugFoundException(e.getMessage(), e);
        } catch (BugFoundException e) {
            logReplayException(replayer, e);
            throw e;
        } catch (Exception e) {
            logReplayException(replayer, e);
            throw new Exception("Error when replaying the bug", e);
        }
    }
    
    /**
     * Wrap a Throwable in an Exception if necessary
     * 
     * @param t The Throwable to wrap
     * @return An Exception containing the Throwable
     */
    private static Throwable wrapThrowable(Throwable t) {
        if (t instanceof Exception) {
            return t;
        }
        return new Exception(t.getMessage(), t);
    }
    
    /**
     * Log replay exceptions
     * 
     * @param replayer The replay scheduler
     * @param e The exception to log
     */
    private static void logReplayException(ReplayScheduler replayer, Exception e) {
        replayer.getLogger().logStackTrace(e);
        PExLogger.logTrace(e);
    }
    
    /**
     * Handle interrupted exception
     * 
     * @param e Interrupted exception
     * @throws Exception with interrupted message
     */
    private static void handleInterruptedException(InterruptedException e) throws Exception {
        PExGlobal.setStatus(STATUS.INTERRUPTED);
        throw new Exception("INTERRUPTED", e);
    }
    
    /**
     * Handle general runtime exception
     * 
     * @param e Runtime exception
     * @throws Exception with error message
     */
    private static void handleRuntimeException(RuntimeException e) throws Exception {
        PExGlobal.setStatus(STATUS.ERROR);
        throw new Exception("ERROR", e);
    }
    
    /**
     * Clean up resources after execution
     */
    private static void cleanupResources() {
        cancelAllThreads();
        executor.shutdownNow();
        PExGlobal.updateResult();
        printStats();
        PExLogger.logEndOfRun(Duration.between(TimeMonitor.getStart(), Instant.now()).getSeconds());
        SearchTask.Cleanup();
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

    /**
     * Replay a schedule from a file
     * 
     * @param fileName File containing the schedule to replay
     * @throws Exception if replay fails
     */
    private static void replaySchedule(String fileName) throws Exception {
        PExLogger.logInfo(String.format("... Reading buggy trace from %s", fileName));

        ReplayScheduler replayer = new ReplayScheduler(Schedule.readFromFile(fileName), null);
        PExGlobal.setReplayScheduler(replayer);
        PExLogger.logReplayerInfo(replayer);
        
        try {
            replayer.run();
        } catch (NullPointerException | StackOverflowError | ClassCastException e) {
            PExGlobal.setStatus(STATUS.BUG_FOUND);
            PExGlobal.setResult(String.format("found cex of length %,d", replayer.getStepNumber()));
            
            Exception wrappedException = (Exception) wrapThrowable(e);
            replayer.getLogger().logStackTrace(wrappedException);
            PExLogger.logTrace(wrappedException);
            
            throw new BugFoundException(e.getMessage(), e);
        } catch (BugFoundException e) {
            PExGlobal.setStatus(STATUS.BUG_FOUND);
            PExGlobal.setResult(String.format("found cex of length %,d", replayer.getStepNumber()));
            
            if (e instanceof TooManyChoicesException) {
                PExGlobal.setResult(PExGlobal.getResult() + " (too many choices)");
            }
            
            replayer.getLogger().logStackTrace(e);
            PExLogger.logTrace(e);
            throw e;
        } catch (Exception e) {
            replayer.getLogger().logStackTrace(e);
            PExLogger.logTrace(e);
            throw new Exception("Error when replaying the bug", e);
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
