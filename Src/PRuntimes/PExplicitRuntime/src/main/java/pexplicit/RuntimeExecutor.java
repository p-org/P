package pexplicit;

import pexplicit.runtime.PExplicitGlobal;
import pexplicit.runtime.STATUS;
import pexplicit.runtime.logger.PExplicitLogger;
import pexplicit.runtime.logger.StatWriter;
import pexplicit.runtime.scheduler.Scheduler;
import pexplicit.runtime.scheduler.explicit.ExplicitSearchScheduler;
import pexplicit.runtime.scheduler.replay.ReplayScheduler;
import pexplicit.utils.exceptions.BugFoundException;
import pexplicit.utils.exceptions.MemoutException;
import pexplicit.utils.monitor.MemoryMonitor;
import pexplicit.utils.monitor.TimeMonitor;
import pexplicit.utils.monitor.TimedCall;

import java.time.Duration;
import java.time.Instant;
import java.util.concurrent.*;
import java.util.*;

/**
 * Represents the runtime executor that executes the analysis engine
 */
public class RuntimeExecutor {
    private static ThreadPoolExecutor executor;
    private static ArrayList<Future<Integer>> futures = new ArrayList<>();
    private static ArrayList<ExplicitSearchScheduler> schedulers = new ArrayList<>();
    private static List<Thread> threads = new ArrayList<>();

    private static void runWithTimeout(long timeLimit)
            throws TimeoutException,
            InterruptedException,
            RuntimeException {
        try { 
            if (timeLimit > 0) {
                for (Future<Integer> future : futures) {
                    future.get(timeLimit, TimeUnit.SECONDS);
                }
            } else {
                for (Future<Integer> future : futures) {
                    future.get();
                }
            }
        } catch (TimeoutException e) {
            throw e;
        } catch (BugFoundException e) { // Dont merge with TimeoutException catch block, easier for seeing race conditions
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
        for (int i = 0; i < PExplicitGlobal.getMaxThreads(); i++) {
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

        ThreadFactory threadFactory = new ThreadFactory() {
            @Override
            public Thread newThread(Runnable r) {
                Thread thread = new Thread(r);
                threads.add(thread);
                return thread;
            }
        };

        executor = (ThreadPoolExecutor) Executors.newFixedThreadPool(PExplicitGlobal.getMaxThreads(), threadFactory);

        double preSearchTime =
                TimeMonitor.findInterval(TimeMonitor.getStart());
        StatWriter.log("project-name", String.format("%s", PExplicitGlobal.getConfig().getProjectName()));
        StatWriter.log("strategy", String.format("%s", PExplicitGlobal.getConfig().getSearchStrategyMode()));
        StatWriter.log("time-limit-seconds", String.format("%.1f", PExplicitGlobal.getConfig().getTimeLimit()));
        StatWriter.log("memory-limit-MB", String.format("%.1f", PExplicitGlobal.getConfig().getMemLimit()));
        StatWriter.log("time-pre-seconds", String.format("%.1f", preSearchTime));
    }

    private static void process(boolean resume) throws Exception {
        
        ArrayList<TimedCall> timedCalls = new ArrayList<>();
        try {
            // create and add first task through scheduler 0
            schedulers.get(0).getSearchStrategy().createFirstTask();

            
            for (int i = 0; i < PExplicitGlobal.getMaxThreads(); i++) {
                timedCalls.add(new TimedCall(schedulers.get(i), resume, i));
            }


            for (int i = 0; i < PExplicitGlobal.getMaxThreads(); i++) {
                Future<Integer> future = executor.submit(timedCalls.get(i));
                futures.add(future);
            }

            Thread.sleep(1000); // Sleep for 1 second, so that threads can pick up the task from the executor object

            // Get the number of pending tasks
            int pendingTasks = executor.getQueue().size();
            PExplicitLogger.logInfo("Number of pending tasks: " + pendingTasks);

            TimeMonitor.startInterval();

            runWithTimeout((long) PExplicitGlobal.getConfig().getTimeLimit());

        } 
        catch (MemoutException | OutOfMemoryError e) {
            for (Thread thread : threads) {
                if (thread != Thread.currentThread()) {
                    thread.interrupt();
                }
            }
        }
        // catch (TimeoutException e) {
        //     PExplicitGlobal.setStatus(STATUS.TIMEOUT);
        //     throw new Exception("TIMEOUT", e);
        // } 
        // catch (MemoutException | OutOfMemoryError e) {
        //     PExplicitGlobal.setStatus(STATUS.MEMOUT);
        //     throw new Exception("MEMOUT", e);
        // } 
        catch (BugFoundException e) {


            for (Thread thread : threads) {
                if (thread != Thread.currentThread()) {
                    thread.interrupt();
                }
            }

            // (schedulers.get( PExplicitGlobal.getTID_to_localtID().get( Thread.currentThread().getId() ))).updateResult(); // Update result field before setting status

            // PExplicitGlobal.setStatus(STATUS.BUG_FOUND);
            // PExplicitGlobal.setResult("cex"); 


            PExplicitLogger.logStackTrace(e);


            ReplayScheduler replayer = new ReplayScheduler( e.getBuggySchedule() );
            
            PExplicitGlobal.setRepScheduler(replayer); 
            PExplicitGlobal.addTotIDtolocaltID(Thread.currentThread().getId(), e.getBuggyLocalTID());

            try {
                    replayer.run();
            } catch (NullPointerException | StackOverflowError | ClassCastException replayException) {
                PExplicitLogger.logStackTrace((Exception) replayException);
                throw new BugFoundException(replayException.getMessage(), replayException);
            } catch (BugFoundException replayException) {
                PExplicitLogger.logStackTrace(replayException); // This should be throw exception again!
                throw replayException;
            } catch (Exception replayException) {
                PExplicitLogger.logStackTrace(replayException);
                throw new Exception("Error when replaying the bug", replayException);
            }
            throw new Exception("Failed to replay bug", e);
        } 
        // catch (InterruptedException e) {
        //     PExplicitGlobal.setStatus(STATUS.INTERRUPTED);
        //     throw new Exception("INTERRUPTED", e);
        // } 
        // catch (RuntimeException e) {
        //     PExplicitGlobal.setStatus(STATUS.ERROR);
        //     throw new Exception("ERROR", e);
        // } 
        finally {        
            executor.shutdownNow(); // forcibly shutdown EVERY thread
            
            Boolean isMemOutException = false;
            Boolean NoException = true;
            Boolean isBugFoundException = false;
            Boolean AllTimeOutException = true;
            int buggyScheduleIndex = -1;
            int memOutScheduleIndex = -1;

            for (int i = 0; i < timedCalls.size() ; i++) {
                Throwable ePerThread = (timedCalls.get(i)).getExceptionThrown();
                if (ePerThread != null && !(ePerThread instanceof InterruptedException)) 
                    /*  Interrupted exception is thrown because 'sleep' of other threads is interrupted somewhere - possibly by timeout feature 
                    or blocking on no current jobs available, so dont want this to go into `etc error case' */
                    NoException = false;
                if (!(ePerThread instanceof TimeoutException))
                    AllTimeOutException = false;
                if (ePerThread instanceof MemoutException || ePerThread instanceof OutOfMemoryError) {
                    isMemOutException = true;
                    memOutScheduleIndex = i;
                }
                else if ( ePerThread instanceof BugFoundException) {
                    isBugFoundException = true;
                    buggyScheduleIndex = i;
                }
            }
            
            if (NoException) { // Correct Case (all threads report correct)
                PExplicitGlobal.setStatus(STATUS.VERIFIED_UPTO_MAX_STEPS);
                PExplicitGlobal.setResult("Correct");
                printStats();
                PExplicitLogger.logEndOfRun(schedulers.get(0), Duration.between(TimeMonitor.getStart(), Instant.now()).getSeconds()); // PIN:
            }
            else if (isMemOutException) {  // Memory Error (atleast one thread throws MemOut Exception)
                PExplicitGlobal.setStatus(STATUS.MEMOUT);
                PExplicitGlobal.setResult("MemOut");
                printStats();
                PExplicitLogger.logEndOfRun(schedulers.get(memOutScheduleIndex), Duration.between(TimeMonitor.getStart(), Instant.now()).getSeconds());
                throw new Exception("MEMOUT"); 
            }
            else if (isBugFoundException) {  // Bug Found Exception (atleast one thread throws BugFound Exception AND no thread throws MemOut Exception) 
                PExplicitGlobal.setStatus(STATUS.BUG_FOUND);
                PExplicitGlobal.setResult("cex");
                printStats();
                PExplicitLogger.logEndOfRun(schedulers.get(buggyScheduleIndex), Duration.between(TimeMonitor.getStart(), Instant.now()).getSeconds());
                throw new BugFoundException();
            }
            else if (AllTimeOutException) {  // All threads timeout
                PExplicitGlobal.setStatus(STATUS.TIMEOUT);
                PExplicitGlobal.setResult("TimeOut");
                printStats();
                PExplicitLogger.logEndOfRun(schedulers.get(0), Duration.between(TimeMonitor.getStart(), Instant.now()).getSeconds());
                throw new Exception("TIMEOUT"); 
            }
            else {  // Some other case
                PExplicitGlobal.setStatus(STATUS.ERROR);
                PExplicitGlobal.setResult("error");
                printStats();
                throw new Exception("ERROR"); 
            }

        }
    }

    public static void run() throws Exception {
        for (int i = 0; i < PExplicitGlobal.getMaxThreads(); i++) {
            ExplicitSearchScheduler localCopy = new ExplicitSearchScheduler();
            schedulers.add(localCopy);
        }

        preprocess();

        process(false);

    }
}