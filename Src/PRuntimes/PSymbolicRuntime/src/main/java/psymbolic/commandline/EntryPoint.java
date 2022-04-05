package psymbolic.commandline;

import psymbolic.runtime.logger.SearchLogger;
import psymbolic.runtime.scheduler.IterativeBoundedScheduler;
import psymbolic.runtime.scheduler.ReplayScheduler;
import psymbolic.runtime.scheduler.DPORScheduler;
import psymbolic.runtime.logger.PSymLogger;
import psymbolic.runtime.logger.TraceLogger;
import psymbolic.valuesummary.Guard;
import psymbolic.runtime.logger.StatLogger;

import java.time.Duration;
import java.time.Instant;
import java.util.concurrent.*;

public class EntryPoint {
    public static Instant start = Instant.now();

    private static void runWithTimeout(Future<Integer> future, long timeLimit) throws TimeoutException, MemoutException, BugFoundException, InterruptedException {
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

    private static void print_stats(String status, PSymConfiguration config, IterativeBoundedScheduler scheduler) {
        if (config.getCollectStats() != 0) {
            System.out.println("--------------------");
            System.out.println("Stats::");
            System.out.println(String.format("project-name:\t%s", config.getProjectName()));
            System.out.println(String.format("solver:\t%s", config.getSolverType().toString()));
            StatLogger.log(String.format("expr-type:\t%s", config.getExprLibType().toString()));
            StatLogger.log(String.format("time-limit-seconds:\t%.1f", config.getTimeLimit()));
            StatLogger.log(String.format("memory-limit-MB:\t%.1f", config.getMemLimit()));
            StatLogger.log(String.format("status:\t%s", status));
            scheduler.print_stats();
            System.out.println("--------------------");
        }
    }

    public static void run(Program p, PSymConfiguration config) throws Exception {
        PSymLogger.ResetAllConfigurations(config.getVerbosity(), config.getProjectName());
        IterativeBoundedScheduler scheduler = new IterativeBoundedScheduler(config);
        if (config.isDpor()) scheduler = new DPORScheduler(config);
        p.setScheduler(scheduler);
        if (config.getCollectStats() != 0) {
            StatLogger.log(String.format("project-name:\t%s", config.getProjectName()));
            StatLogger.log(String.format("solver:\t%s", config.getSolverType().toString()));
            StatLogger.log(String.format("expr-type:\t%s", config.getExprLibType().toString()));
            StatLogger.log(String.format("time-limit-seconds:\t%.1f", config.getTimeLimit()));
            StatLogger.log(String.format("memory-limit-MB:\t%.1f", config.getMemLimit()));
        }
        start = Instant.now();
        String status = "error";

        ExecutorService executor = Executors.newSingleThreadExecutor();
        TimedCall timedCall = new TimedCall(scheduler, p);
        Future<Integer> future = executor.submit(timedCall);
        try {
            runWithTimeout(future, (long)config.getTimeLimit());
            status = "success";
        } catch (TimeoutException e) {
            status = "timeout";
            throw new Exception("TIMEOUT");
        } catch (MemoutException e) {
            status = "memout";
            throw new Exception("MEMOUT");
        } catch (BugFoundException e) {
            status = "cex";
            print_stats(status, config, scheduler);

//            TraceLogger.setVerbosity(2);
            SearchLogger.disable();
            Guard pc = e.pathConstraint;
            ReplayScheduler replay = new ReplayScheduler(config, scheduler.getSchedule(), pc);
            p.setScheduler(replay);
            replay.doSearch(p);
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
            TraceLogger.finished(scheduler.getDepth(), Duration.between(start, end).getSeconds());
            print_stats(status, config, scheduler);
        }
    }

}
