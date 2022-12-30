package psym.commandline;

import psym.runtime.Concretizer;
import psym.runtime.logger.*;
import psym.runtime.scheduler.IterativeBoundedScheduler;
import psym.runtime.scheduler.ReplayScheduler;
import psym.utils.BugFoundException;
import psym.utils.LivenessException;
import psym.utils.TimeMonitor;
import psym.valuesummary.Guard;
import psym.valuesummary.solvers.SolverEngine;

import java.time.Duration;
import java.time.Instant;
import java.util.concurrent.*;

public class EntryPoint {
    private static ExecutorService executor;
    private static Future<Integer> future;
    private static String status;
    private static PSymConfiguration configuration;
    private static IterativeBoundedScheduler scheduler;
    private static String mode;

    private static void runWithTimeout(long timeLimit) throws TimeoutException, MemoutException, BugFoundException, InterruptedException {
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
        } catch (ExecutionException e) {
            if (e.getCause() instanceof MemoutException) {
                throw (MemoutException)e.getCause();
            } else if (e.getCause() instanceof BugFoundException) {
                throw (BugFoundException)e.getCause();
            } else if (e.getCause() instanceof TimeoutException) {
                throw (TimeoutException)e.getCause();
            } else {
                e.getCause().printStackTrace();
                e.printStackTrace();
                throw new RuntimeException("RuntimeException");
            }
        } catch (InterruptedException e) {
            throw e;
        }
    }

    private static void print_stats() {
        double searchTime = TimeMonitor.getInstance().stopInterval();
        TimeMonitor.getInstance().startInterval();
        StatWriter.log("status", String.format("%s", status));
        scheduler.print_stats();
        StatWriter.log("time-search-seconds", String.format("%.1f", searchTime));
        scheduler.reportEstimatedCoverage();
    }

    private static void preprocess() {
        PSymLogger.info(String.format("... Method " +  configuration.getTestDriver()));
        PSymLogger.info(String.format("... Project %s is using '%s' strategy (seed:%s)",
                                            configuration.getProjectName(),
                                            configuration.getMode(),
                                            configuration.getRandomSeed()));
        PSymLogger.info("--------------------");

        executor = Executors.newSingleThreadExecutor();
        status = "error";
        if (configuration.isSymbolic()) {
            mode = "symbolic";
        } else {
            mode = "single";
        }
        double preSearchTime = TimeMonitor.getInstance().findInterval(TimeMonitor.getInstance().getStart());
        StatWriter.log("project-name", String.format("%s", configuration.getProjectName()));
        StatWriter.log("mode", String.format("%s", mode));
        StatWriter.log("solver", String.format("%s", configuration.getSolverType().toString()));
        StatWriter.log("expr-type", String.format("%s", configuration.getExprLibType().toString()));
        StatWriter.log("time-limit-seconds", String.format("%.1f", configuration.getTimeLimit()));
        StatWriter.log("memory-limit-MB", String.format("%.1f", configuration.getMemLimit()));
        StatWriter.log("time-pre-seconds", String.format("%.1f", preSearchTime));
        Concretizer.print = (configuration.getVerbosity() > 8);
    }

    private static void postprocess() {
        Instant end = Instant.now();
        print_stats();
        PSymLogger.finished(scheduler.getIter(), scheduler.getIter()- scheduler.getStart_iter(), Duration.between(TimeMonitor.getInstance().getStart(), end).getSeconds(), scheduler.result, mode);
    }

    private static void process(boolean resume) throws Exception {
        try {
            TimedCall timedCall = new TimedCall(scheduler, resume);
            future = executor.submit(timedCall);
            TimeMonitor.getInstance().startInterval();
            runWithTimeout((long)configuration.getTimeLimit());
            status = "success";
        } catch (TimeoutException e) {
            status = "timeout";
            throw new Exception("TIMEOUT");
        } catch (MemoutException e) {
            status = "memout";
            throw new Exception("MEMOUT");
        } catch (BugFoundException e) {
            status = "cex";
            scheduler.result = "found cex of length " + scheduler.getDepth();
            postprocess();

            PSymLogger.setVerbosity(1);
            TraceLogger.setVerbosity(1);
            SearchLogger.disable();
            CoverageWriter.disable();
            Guard pc = e.pathConstraint;

            ReplayScheduler replayScheduler = new ReplayScheduler(configuration, scheduler.getProgram(),
                    scheduler.getSchedule(), pc, scheduler.getDepth(), (e instanceof LivenessException));
            scheduler.getProgram().setProgramScheduler(replayScheduler);
            String writeFileName = configuration.getOutputFolder() + "/cex.schedule";
            replayScheduler.writeToFile(writeFileName);
            replay(replayScheduler);
        } catch (InterruptedException e) {
            status = "interrupted";
            throw new Exception("INTERRUPTED");
        } catch (RuntimeException e) {
            status = "error";
            throw new Exception("ERROR");
        } finally {
//            GlobalData.getChoiceLearningStats().printQTable();
            future.cancel(true);
            executor.shutdownNow();
            TraceLogger.setVerbosity(0);
            postprocess();
        }
    }

    public static void run(IterativeBoundedScheduler sch, PSymConfiguration config) throws Exception {
        scheduler = sch;
        configuration = config;
        scheduler.getProgram().setProgramScheduler(scheduler);

        preprocess();
        process(false);
    }

    public static void resume(IterativeBoundedScheduler sch, PSymConfiguration config) throws Exception {
        scheduler = sch;
        configuration = config;
        scheduler.getProgram().setProgramScheduler(scheduler);

        scheduler.setConfiguration(configuration);
        TraceLogger.setVerbosity(config.getVerbosity());
        SolverEngine.resumeEngine();

        preprocess();
        process(true);
    }

    private static void replay(ReplayScheduler replayScheduler) throws RuntimeException, InterruptedException, TimeoutException {
        try {
            replayScheduler.doSearch();
            throw new RuntimeException("ERROR: Failed to replay counterexample");
        } catch (BugFoundException bugFoundException) {
            bugFoundException.printStackTrace();
            throw new BugFoundException("Found bug: " + bugFoundException.getLocalizedMessage(), replayScheduler.getPathConstraint());
        }
    }

    public static void replayBug(ReplayScheduler replayScheduler, PSymConfiguration config) throws RuntimeException, InterruptedException, TimeoutException {
        SolverEngine.resumeEngine();
        if (config.getVerbosity() == 0) {
            PSymLogger.setVerbosity(1);
            TraceLogger.setVerbosity(1);
        }
        TraceLogger.enable();
        config.setUseReceiverQueueSemantics(false);
        config.setCollectStats(0);
        replayScheduler.setConfiguration(config);
        replayScheduler.getProgram().setProgramScheduler(replayScheduler);
        replay(replayScheduler);
    }

    public static void writeToFile() throws Exception {
        if (configuration.getVerbosity() > 0) {
            PSymLogger.info(String.format("Writing 1 current and %d backtrack states in %s/", scheduler.getTotalNumBacktracks(), configuration.getOutputFolder()));
        }
        long pid = ProcessHandle.current().pid();
        String writeFileName = configuration.getOutputFolder() + "/current" + "_pid" + pid + ".out";
        scheduler.writeToFile(writeFileName);
        scheduler.writeBacktracksToFiles(configuration.getOutputFolder() + "/backtrack");
        if (configuration.getVerbosity() > 0) {
            PSymLogger.info("--------------------");
        }
    }
}
