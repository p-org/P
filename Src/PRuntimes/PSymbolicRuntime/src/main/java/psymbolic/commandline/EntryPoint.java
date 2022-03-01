package psymbolic.commandline;

import psymbolic.runtime.logger.SearchLogger;
import psymbolic.runtime.scheduler.IterativeBoundedScheduler;
import psymbolic.runtime.scheduler.ReplayScheduler;
import psymbolic.runtime.scheduler.DPORScheduler;
import psymbolic.runtime.logger.PSymLogger;
import psymbolic.runtime.logger.TraceLogger;
import psymbolic.valuesummary.solvers.SolverEngine;
import psymbolic.valuesummary.Guard;
import psymbolic.runtime.logger.StatLogger;

import java.time.Duration;
import java.time.Instant;

public class EntryPoint {

    public static Instant start = Instant.now();

    public static void run(Program p, PSymConfiguration config) {
        PSymLogger.ResetAllConfigurations(config.getVerbosity(), config.getProjectName());
        IterativeBoundedScheduler scheduler = new IterativeBoundedScheduler(config);
        if (config.isDpor()) scheduler = new DPORScheduler(config);
        p.setScheduler(scheduler);
        if (config.isCollectStats()) {
            StatLogger.log(String.format("project-name:\t%s", config.getProjectName()));
            StatLogger.log(String.format("solver:\t%s", config.getSolverType().toString()));
        }
        start = Instant.now();
        String status = "error";
        try {
            scheduler.doSearch(p);
            status = "success";
        } catch (BugFoundException e) {
            status = "cex";
            TraceLogger.setVerbosity(2);
            SearchLogger.disable();
            Guard pc = e.pathConstraint;
            ReplayScheduler replay = new ReplayScheduler(config, scheduler.getSchedule(), pc);
            p.setScheduler(replay);
            //replay.doSearch(p);
            e.printStackTrace();
            throw new BugFoundException("Found bug: " + e.getLocalizedMessage(), pc);
        } finally {
            TraceLogger.setVerbosity(2);
            Instant end = Instant.now();
            TraceLogger.finished(scheduler.getDepth());
            TraceLogger.logMessage("Took " + Duration.between(start, end).getSeconds() + " seconds");
            
            if (config.isCollectStats()) {
	            System.out.println("--------------------");
	            System.out.println("Stats::");
                StatLogger.log(String.format("status:\t%s", status));
	            StatLogger.log(String.format("time-seconds:\t%.1f", Duration.between(start, end).toMillis()/1000.0));
	            scheduler.print_stats();
	            System.out.println("--------------------");
            }
        }
    }

}
