package psymbolic.run;

import psymbolic.runtime.BoundedScheduler;
import psymbolic.runtime.CompilerLogger;
import psymbolic.runtime.ReplayScheduler;
import psymbolic.runtime.ScheduleLogger;
import psymbolic.valuesummary.bdd.Bdd;

import java.time.Duration;
import java.time.Instant;

public class EntryPoint {

    public static int prog = 0;
    public static Instant start = Instant.now();

    public static void run(Program p, String name, int depth, int maxInternalSteps) {
        Bdd.reset();
        BoundedScheduler scheduler = new BoundedScheduler(name, 25, 1000, 1000);
        p.setScheduler(scheduler);
        scheduler.setErrorDepth(depth);
        scheduler.setMaxInternalSteps(maxInternalSteps);
        start = Instant.now();
        try {
            scheduler.doSearch(p);
            ScheduleLogger.enable();
            ScheduleLogger.finished(scheduler.getDepth());
        } catch (BugFoundException e) {
            Bdd pc = e.pathConstraint;
            ReplayScheduler replay = new ReplayScheduler(name, scheduler.getSchedule(), pc);
            p.setScheduler(replay);
            replay.setMaxInternalSteps(maxInternalSteps);
            replay.doSearch(p);
            e.printStackTrace();
            throw new BugFoundException("Found bug: " + e.getLocalizedMessage(), pc);
        } finally {
            Instant end = Instant.now();
            ScheduleLogger.enable();
            CompilerLogger.log("Took " + Duration.between(start, end).getSeconds() + " seconds");
            prog++;
        }
    }

}
