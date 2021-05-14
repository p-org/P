package psymbolic.commandline;

import psymbolic.runtime.BoundedScheduler;
import psymbolic.runtime.CompilerLogger;
import psymbolic.runtime.ReplayScheduler;
import psymbolic.runtime.ScheduleLogger;
import psymbolic.valuesummary.bdd.BDDEngine;
import psymbolic.valuesummary.Guard;

import java.time.Duration;
import java.time.Instant;

public class EntryPoint {

    public static int prog = 0;
    public static Instant start = Instant.now();

    public static void run(Program p, String name, int depth, int maxInternalSteps) {
        BDDEngine.reset();
        BoundedScheduler scheduler = new BoundedScheduler(name, 25, 1000, 1000);
        p.setScheduler(scheduler);
        scheduler.setErrorDepth(depth);
        scheduler.setMaxInternalSteps(maxInternalSteps);
        start = Instant.now();
        try {
            scheduler.doSearch(p.getStart());
            ScheduleLogger.enable();
            ScheduleLogger.finished(scheduler.getDepth());
        } catch (BugFoundException e) {
            Guard pc = e.pathConstraint;
            ReplayScheduler replay = new ReplayScheduler(name, scheduler.getSchedule(), pc);
            p.setScheduler(replay);
            replay.setMaxInternalSteps(maxInternalSteps);
            replay.doSearch(scheduler.getStartMachine());
            e.printStackTrace();
            throw new BugFoundException("Found bug", pc);
        } finally {
            Instant end = Instant.now();
            ScheduleLogger.enable();
            CompilerLogger.log("Took " + Duration.between(start, end).getSeconds() + " seconds");
            prog++;
        }
    }

}
