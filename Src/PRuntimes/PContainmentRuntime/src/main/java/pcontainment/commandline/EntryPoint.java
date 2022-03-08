package pcontainment.commandline;

import pcontainment.runtime.logger.SearchLogger;
import pcontainment.runtime.scheduler.IterativeBoundedScheduler;
import pcontainment.runtime.scheduler.ReplayScheduler;
import pcontainment.runtime.scheduler.DPORScheduler;
import pcontainment.runtime.logger.PSymLogger;
import pcontainment.runtime.logger.TraceLogger;
import pcontainment.valuesummary.bdd.BDDEngine;
import pcontainment.valuesummary.Guard;

import java.time.Duration;
import java.time.Instant;

public class EntryPoint {

    public static Instant start = Instant.now();

    public static void run(Program p, PSymConfiguration config) {
        BDDEngine.reset();
        PSymLogger.ResetAllConfigurations(config.getVerbosity());
        IterativeBoundedScheduler scheduler = new IterativeBoundedScheduler(config);
        if (config.isDpor()) scheduler = new DPORScheduler(config);
        p.setScheduler(scheduler);
        start = Instant.now();
        try {
            scheduler.doSearch(p);
        } catch (BugFoundException e) {
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
        }
    }

}
