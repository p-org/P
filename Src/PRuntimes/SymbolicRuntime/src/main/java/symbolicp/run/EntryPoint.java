package symbolicp.run;
import symbolicp.bdd.Bdd;
import symbolicp.bdd.BugFoundException;
import symbolicp.runtime.*;
import symbolicp.*;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.time.Duration;
import java.time.Instant;
import java.util.List;
import java.util.stream.Collectors;
import java.util.stream.Stream;

public class EntryPoint {

    public static int prog = 0;
    public static Instant start = Instant.now();

    public static void run(Program p, String name, int depth, int maxInternalSteps) {
        Bdd.reset();
        BoundedScheduler scheduler = new TransDPORScheduler(name, 1, 1000, 1000);
        p.setScheduler(scheduler);
        scheduler.setErrorDepth(depth);
        scheduler.setMaxInternalSteps(maxInternalSteps);
        start = Instant.now();
        try {
            scheduler.doSearch(p.getStart());
            ScheduleLogger.enable();
            ScheduleLogger.finished(scheduler.getDepth());
        } catch (BugFoundException e) {
            Bdd pc = e.pathConstraint;
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
