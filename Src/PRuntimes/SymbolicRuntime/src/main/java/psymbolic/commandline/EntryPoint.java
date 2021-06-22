package psymbolic.commandline;

import psymbolic.runtime.scheduler.IterativeBoundedScheduler;
import psymbolic.runtime.scheduler.ReplayScheduler;
import psymbolic.runtime.logger.PSymLogger;
import psymbolic.runtime.logger.TraceSymLogger;
import psymbolic.valuesummary.bdd.BDDEngine;
import psymbolic.valuesummary.Guard;

import java.time.Duration;
import java.time.Instant;

public class EntryPoint {

    public static Instant start = Instant.now();

    public static void run(Program p, PSymConfiguration config) {
        BDDEngine.reset();
        PSymLogger.ResetAllConfigurations();
        IterativeBoundedScheduler scheduler = new IterativeBoundedScheduler(config);
        p.setScheduler(scheduler);
        start = Instant.now();
        try {
            PSymLogger.SearchMode();
            scheduler.doSearch(p);
        } catch (BugFoundException e) {
            PSymLogger.ErrorReproMode();
            Guard pc = e.pathConstraint;
            ReplayScheduler replay = new ReplayScheduler(config, scheduler.getSchedule(), pc);
            p.setScheduler(replay);
            replay.doSearch(p);
            e.printStackTrace();
            throw new BugFoundException("Found bug: " + e.getLocalizedMessage(), pc);
        } finally {
            Instant end = Instant.now();
            TraceSymLogger.enable();
            TraceSymLogger.finished(scheduler.getDepth());
            TraceSymLogger.logMessage("Took " + Duration.between(start, end).getSeconds() + " seconds");
        }
    }

}
