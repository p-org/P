package pexplicit.runtime;

import lombok.Getter;
import lombok.Setter;
import pexplicit.commandline.PExplicitConfig;
import pexplicit.runtime.logger.PExplicitLogger;
import pexplicit.runtime.machine.PMachine;
import pexplicit.runtime.machine.PMachineId;
import pexplicit.runtime.scheduler.Schedule;
import pexplicit.runtime.scheduler.Scheduler;

import java.util.*;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.atomic.AtomicLong;

import org.jetbrains.annotations.Async;

import pexplicit.runtime.scheduler.replay.ReplayScheduler;

/**
 * Represents global data structures represented with a singleton class
 */
public class PExplicitGlobal {

    @Getter
    private static final int verbosity = (new PExplicitConfig ()).getVerbosity();

    @Getter
    private static final int maxThreads = PExplicitConfig.getNumThreads();




    private static AtomicLong threadsBlocking = new AtomicLong(0);
    //  @Getter
    //  @Setter
    // private static Map< >
    @Getter
    private static Map<Long, Integer> tID_to_localtID = new ConcurrentHashMap<>();
    /**
     * PModel
     **/
    @Getter
    @Setter
    private static PModel model = null;
    /**
     * Global configuration
     **/
    @Getter
    @Setter
    private static PExplicitConfig config = null;

    /**
     * Mapping from machine type to list of all machine instances
     */
    //  @Getter
    // private static final Map<Class<? extends PMachine>, List<PMachine>> machineListByType = new HashMap<>(); // This is per thread; so make this map of tiD to same Map
    /**
     * Scheduler
     **/
    // @Getter
    // @Setter
    // private static Scheduler scheduler = null; // Remove this!

    // @Getter
    // @Setter
    // private static int buggytID = -1;
    @Getter
    @Setter
    private static Scheduler buggyScheduler = null;




    @Setter
    private static Map<Integer, Scheduler> schedulers = new ConcurrentHashMap<>();

    @Getter
    @Setter
    private static ReplayScheduler repScheduler = null;

    /**
     * Status of the run
     **/
    @Getter
    @Setter
    private static STATUS status = STATUS.INCOMPLETE;
    /**
     * Results of the run
     **/
    @Getter
    @Setter
    private static String result = null;

    // Method to get the current value of threadSafeLong
    public static long getThreadsBlocking() {
        return threadsBlocking.get();
    }

    // Method to increment threadSafeLong
    public static void incrementThreadsBlocking() {
        threadsBlocking.incrementAndGet();
    }

    public static void decrementThreadsBlocking() {
        threadsBlocking.decrementAndGet();
    }

    // Method to add to tID_to_localtID
    public static void addTotIDtolocaltID(long tID, Integer localtID) {
        tID_to_localtID.put(tID, localtID);
    }



    public static Scheduler getScheduler() {
        if (repScheduler != null)
            return repScheduler;
        int localtID = tID_to_localtID.get(Thread.currentThread().getId());
        return schedulers.get(localtID);
    }

    public static void putSchedulers( Integer ltID, Scheduler sch ) {
        schedulers.put(ltID, sch);
    }

  


}

