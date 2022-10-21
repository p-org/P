package psymbolic.commandline;

import psymbolic.runtime.Concretizer;
import psymbolic.runtime.logger.*;
import psymbolic.runtime.scheduler.IterativeBoundedScheduler;
import psymbolic.runtime.scheduler.ReplayScheduler;
import psymbolic.utils.TimeMonitor;
import psymbolic.valuesummary.Guard;
import psymbolic.valuesummary.solvers.SolverEngine;

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
        if (configuration.getCollectStats() != 0) {
            SearchLogger.log("--------------------");
            SearchLogger.log("Statistics Report::");
            SearchLogger.log("--------------------");
            SearchLogger.log("project-name", String.format("%s", configuration.getProjectName()));
            SearchLogger.log("mode", String.format("%s", mode));
            SearchLogger.log("solver", String.format("%s", configuration.getSolverType().toString()));
            SearchLogger.log("expr-type", String.format("%s", configuration.getExprLibType().toString()));
            SearchLogger.log("time-limit-seconds", String.format("%.1f", configuration.getTimeLimit()));
            SearchLogger.log("memory-limit-MB", String.format("%.1f", configuration.getMemLimit()));
            StatWriter.log("status", String.format("%s", status));
            scheduler.print_stats();
            StatWriter.log("time-search-seconds", String.format("%.1f", searchTime));
        }
        scheduler.reportEstimatedCoverage();
    }

    private static void preprocess() {
        executor = Executors.newSingleThreadExecutor();
        status = "error";
        if (configuration.isSymbolic()) {
            mode = "symbolic";
        } else {
            mode = "concrete";
        }
        if (configuration.getCollectStats() != 0) {
            double preSearchTime = TimeMonitor.getInstance().findInterval(TimeMonitor.getInstance().getStart());
            StatWriter.log("project-name", String.format("%s", configuration.getProjectName()), false);
            StatWriter.log("mode", String.format("%s", mode), false);
            StatWriter.log("solver", String.format("%s", configuration.getSolverType().toString()), false);
            StatWriter.log("expr-type", String.format("%s", configuration.getExprLibType().toString()), false);
            StatWriter.log("time-limit-seconds", String.format("%.1f", configuration.getTimeLimit()), false);
            StatWriter.log("memory-limit-MB", String.format("%.1f", configuration.getMemLimit()), false);
            StatWriter.log("time-pre-seconds", String.format("%.1f", preSearchTime), false);
        }
        Concretizer.print = (configuration.getVerbosity() > 6);
    }

    private static void postprocess() {
        Instant end = Instant.now();
        print_stats();
        PSymLogger.finished(scheduler.getIter(), scheduler.getIter()- scheduler.getStart_iter(), Duration.between(TimeMonitor.getInstance().getStart(), end).getSeconds(), scheduler.result, mode);
    }

    private static void process() throws Exception {
        try {
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

//            TraceLogger.setVerbosity(2);
            SearchLogger.disable();
            CoverageWriter.disable();
            Guard pc = e.pathConstraint;

            ReplayScheduler replay = new ReplayScheduler(configuration, scheduler.getProgram(), scheduler.getSchedule(), pc, scheduler.getDepth());
            scheduler.getProgram().setScheduler(replay);
            replay.doSearch();
            e.printStackTrace();
            throw new BugFoundException("Found bug: " + e.getLocalizedMessage(), pc);
        } catch (InterruptedException e) {
            status = "interrupted";
            throw new Exception("INTERRUPTED");
        } catch (RuntimeException e) {
            status = "error";
            throw new Exception("ERROR");
        } finally {
            future.cancel(true);
            executor.shutdownNow();
            TraceLogger.setVerbosity(0);
            postprocess();
        }
    }

    public static void run(IterativeBoundedScheduler sch, PSymConfiguration config) throws Exception {
        scheduler = sch;
        configuration = config;
        scheduler.getProgram().setScheduler(scheduler);

        preprocess();
        TimedCall timedCall = new TimedCall(scheduler, false);
        future = executor.submit(timedCall);
        process();
    }

    public static void resume(IterativeBoundedScheduler sch, PSymConfiguration config) throws Exception {
        scheduler = sch;
        configuration = config;
        scheduler.getProgram().setScheduler(scheduler);

        scheduler.setConfiguration(configuration);
        TraceLogger.setVerbosity(config.getVerbosity());
        SolverEngine.resumeEngine();

        preprocess();
        TimedCall timedCall = new TimedCall(scheduler, true);
        future = executor.submit(timedCall);
        process();
    }

    public static void writeToFile() throws Exception {
        if (configuration.getCollectStats() != 0) {
            PSymLogger.info(String.format("Writing 1 current and %d backtrack states in %s/", scheduler.getNumBacktracks(), configuration.getOutputFolder()));
        }
        long pid = ProcessHandle.current().pid();
        String writeFileName = configuration.getOutputFolder() + "/current" + "_pid" + pid + ".out";
        scheduler.writeToFile(writeFileName);
        scheduler.writeBacktracksToFiles(configuration.getOutputFolder() + "/backtrack");
        if (configuration.getCollectStats() != 0) {
            PSymLogger.info("--------------------");
        }
    }
}
