package psymbolic.commandline;

import psymbolic.runtime.logger.SearchLogger;
import psymbolic.runtime.scheduler.IterativeBoundedScheduler;
import psymbolic.runtime.scheduler.ReplayScheduler;
import psymbolic.runtime.logger.TraceLogger;
import psymbolic.valuesummary.Guard;
import psymbolic.runtime.logger.StatLogger;
import psymbolic.valuesummary.solvers.SolverEngine;

import java.time.Duration;
import java.time.Instant;
import java.util.concurrent.*;

public class EntryPoint {
    public static Instant start = Instant.now();
    private static ExecutorService executor;
    private static Future<Integer> future;
    private static String status;
    private static PSymConfiguration configuration;
    private static Program program;
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
        if (configuration.getCollectStats() != 0) {
            System.out.println("--------------------");
            System.out.println("Statistics::");
            System.out.println(String.format("project-name:\t%s", configuration.getProjectName()));
            System.out.println(String.format("mode:\t%s", mode));
            System.out.println(String.format("solver:\t%s", configuration.getSolverType().toString()));
            System.out.println(String.format("expr-type:\t%s", configuration.getExprLibType().toString()));
            System.out.println(String.format("time-limit-seconds:\t%.1f", configuration.getTimeLimit()));
            System.out.println(String.format("memory-limit-MB:\t%.1f", configuration.getMemLimit()));
            StatLogger.log(String.format("status:\t%s", status));
            scheduler.print_stats();
        }
    }

    private static void preprocess() {
        start = Instant.now();
        executor = Executors.newSingleThreadExecutor();
        status = "error";
        mode = "symbolic";
        if (configuration.getSchedChoiceBound() == 1 && configuration.getInputChoiceBound() == 1) {
            mode = "concrete";
        }
        if (configuration.getCollectStats() != 0) {
            StatLogger.log(String.format("project-name:\t%s", configuration.getProjectName()));
            StatLogger.log(String.format("mode:\t%s", mode));
            StatLogger.log(String.format("solver:\t%s", configuration.getSolverType().toString()));
            StatLogger.log(String.format("expr-type:\t%s", configuration.getExprLibType().toString()));
            StatLogger.log(String.format("time-limit-seconds:\t%.1f", configuration.getTimeLimit()));
            StatLogger.log(String.format("memory-limit-MB:\t%.1f", configuration.getMemLimit()));
        }
    }

    private static void process() throws Exception {
        try {
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
            print_stats();

//            TraceLogger.setVerbosity(2);
            SearchLogger.disable();
            Guard pc = e.pathConstraint;
            ReplayScheduler replay = new ReplayScheduler(configuration, scheduler.getSchedule(), pc);
            program.setScheduler(replay);
            replay.doSearch(program);
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

            Instant end = Instant.now();
            print_stats();

            TraceLogger.finished(scheduler.getIter(), Duration.between(start, end).getSeconds(), scheduler.result, mode);
        }
    }

    public static void run(Program p, IterativeBoundedScheduler sch, PSymConfiguration config) throws Exception {
        program = p;
        scheduler = sch;
        configuration = config;
        program.setScheduler(scheduler);

        preprocess();
        TimedCall timedCall = new TimedCall(scheduler, program, false);
        future = executor.submit(timedCall);
        process();
    }

    public static void resume(Program p, IterativeBoundedScheduler sch, PSymConfiguration config) throws Exception {
        program = p;
        scheduler = sch;
        configuration = config;
        program.setScheduler(scheduler);

        scheduler.setConfiguration(configuration);
        TraceLogger.setVerbosity(config.getVerbosity());
        SolverEngine.resumeEngine();

        preprocess();
        TimedCall timedCall = new TimedCall(scheduler, program, true);
        future = executor.submit(timedCall);
        process();
    }

}
