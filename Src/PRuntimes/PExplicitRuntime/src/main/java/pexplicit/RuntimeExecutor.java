package pexplicit;

import pexplicit.runtime.PExplicitGlobal;
import pexplicit.runtime.STATUS;
import pexplicit.runtime.logger.PExplicitLogger;
import pexplicit.runtime.logger.StatWriter;
import pexplicit.runtime.scheduler.explicit.ExplicitSearchScheduler;
import pexplicit.runtime.scheduler.explicit.strategy.SearchStrategy;
import pexplicit.runtime.scheduler.replay.ReplayScheduler;
import pexplicit.utils.exceptions.BugFoundException;
import pexplicit.utils.exceptions.MemoutException;
import pexplicit.utils.monitor.MemoryMonitor;
import pexplicit.utils.monitor.TimeMonitor;
import pexplicit.utils.monitor.TimedCall;
import pexplicit.runtime.scheduler.Scheduler;

import java.time.Duration;
import java.time.Instant;
import java.util.ArrayList;
import java.util.concurrent.*;

import pexplicit.commandline.PExplicitConfig;

/**
 * Represents the runtime executor that executes the analysis engine
 */
public class RuntimeExecutor {
    private static ThreadPoolExecutor executor;
    private static ArrayList<Future<Integer>> futures = new ArrayList<>();
    private static ArrayList<ExplicitSearchScheduler> schedulers = new ArrayList<>();

    private static void runWithTimeout(long timeLimit)
            throws TimeoutException,
            InterruptedException,
            RuntimeException {
        try { // PIN: If thread gets exception, need to kill the other threads.
            if (timeLimit > 0) {
                // PExplicitLogger.logInfo("Check0.1.3.1");
                
                for (Future<Integer> future: futures) {
                    // Future<Integer> future = futures.get(i);
                    future.get(timeLimit, TimeUnit.SECONDS);
                }

                // PExplicitLogger.logInfo("Check0.1.3.2");
                
                

            } else {
                
                for (int i = 0; i < PExplicitGlobal.getMaxThreads(); i++) {
                    Future<Integer> future = futures.get(i);
                    future.get();
                }
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
        for (int i = 0; i < PExplicitGlobal.getMaxThreads(); i++){
            ExplicitSearchScheduler scheduler = schedulers.get(i);
            scheduler.recordStats();
        }
            
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

        executor = (ThreadPoolExecutor) Executors.newFixedThreadPool(PExplicitGlobal.getMaxThreads());

        // PExplicitGlobal.setResult("error");

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
            ArrayList<TimedCall> timedCalls = new ArrayList<>();
            
            // PExplicitLogger.logInfo("Check0.1.1");
            
            SearchStrategy.createFirstTask();
            
            for (int i = 0; i < PExplicitGlobal.getMaxThreads(); i++) {
                timedCalls.add( new TimedCall(schedulers.get(i), resume, i));
            }

            for (int i = 0; i < PExplicitGlobal.getMaxThreads(); i++) {
                Future<Integer> future = executor.submit(timedCalls.get(i));
                futures.add(future);
            }

            // Thread.sleep(1000);

            // Get the number of pending tasks
            int pendingTasks = executor.getQueue().size();
            PExplicitLogger.logInfo("Number of pending tasks: " + pendingTasks);


            // PExplicitLogger.logInfo("Check0.1.2");

            TimeMonitor.startInterval();

            // PExplicitLogger.logInfo("Check0.1.3");

            runWithTimeout((long) PExplicitGlobal.getConfig().getTimeLimit());

            // PExplicitLogger.logInfo("Check0.1.4");
        } catch (TimeoutException e) {
            PExplicitGlobal.setStatus(STATUS.TIMEOUT);
            throw new Exception("TIMEOUT", e);
        } catch (MemoutException | OutOfMemoryError e) {
            PExplicitGlobal.setStatus(STATUS.MEMOUT);
            throw new Exception("MEMOUT", e);
        } catch (BugFoundException e) {
            PExplicitGlobal.setStatus(STATUS.BUG_FOUND);
            
            

            // for (int i = 0; i < PExplicitGlobal.getMaxThreads(); i++)
            //     PExplicitGlobal.setResult(String.format("found cex of length %d", (schedulers.get(i)).getStepNumber()));
            // Terminate all schedulers at this point, and that scheduler which found this exception stores result.

            PExplicitLogger.logStackTrace(e);

            ArrayList<ReplayScheduler> replayers = new ArrayList<>();
            for (int i = 0; i < PExplicitGlobal.getMaxThreads() ; i++)
                replayers.add(new ReplayScheduler((schedulers.get(i)).schedule));

            ArrayList<Scheduler> localSchedulers = PExplicitGlobal.getSchedulers(); 
            for (int i = 0; i < PExplicitGlobal.getMaxThreads(); i++)
                localSchedulers.set(i,replayers.get(i));
            
            try {
                for (int i = 0; i < PExplicitGlobal.getMaxThreads() ; i++)
                    (replayers.get(i)).run();
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
            for (int i = 0; i < PExplicitGlobal.getMaxThreads(); i++) {
                Future<Integer> future = futures.get(i);
                future.cancel(true);
            }
            executor.shutdownNow();
            for (int i = 0; i < PExplicitGlobal.getMaxThreads() ; i++)
                (schedulers.get(i)).updateResult();
            printStats();
            for (int i = 0; i < PExplicitGlobal.getMaxThreads() ; i++)
            PExplicitLogger.logEndOfRun(schedulers.get(i), Duration.between(TimeMonitor.getStart(), Instant.now()).getSeconds());
        }
    }

    public static void run() throws Exception {
        ArrayList<Scheduler> localSchedulers = PExplicitGlobal.getSchedulers();
        for (int i = 0; i < PExplicitGlobal.getMaxThreads(); i++) {
            ExplicitSearchScheduler localCopy = new ExplicitSearchScheduler();
            schedulers.add(localCopy);
            localSchedulers.add(localCopy);
        }

        // PExplicitLogger.logInfo("Check0.0");

        preprocess();

        // PExplicitLogger.logInfo("Check0.1");

        process(false);

        // PExplicitLogger.logInfo("Check0.2");

    }

}