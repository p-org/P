package pexplicit.runtime;

import lombok.Getter;
import lombok.Setter;
import pexplicit.commandline.PExplicitConfig;
import pexplicit.runtime.logger.PExplicitLogger;
import pexplicit.runtime.machine.PMachine;
import pexplicit.runtime.machine.PMachineId;
import pexplicit.runtime.scheduler.Scheduler;

import java.util.*;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.atomic.AtomicLong;

/**
 * Represents global data structures represented with a singleton class
 */
public class PExplicitGlobal {

    @Getter
    private static final int verbosity = (new PExplicitConfig ()).getVerbosity();

    @Getter
    private static final int maxThreads = PExplicitConfig.getNumThreads();
    /**
     * Mapping from machine type to list of all machine instances
     */
    @Getter
    private static final Map<Integer, Map<Class<? extends PMachine>, List<PMachine>>> machineListByTypePerThread = new ConcurrentHashMap<>(); // This is per thread; so make this map of tiD to same Map
    /**
     * Set of machines
     */
    @Getter
    private static final Map<Integer, SortedSet<PMachine>> machineSetPerThread = new ConcurrentHashMap<>();
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

    @Getter
    @Setter
    private static ArrayList<Scheduler> schedulers = new ArrayList<>();
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

    public static Map<Class<? extends PMachine>, List<PMachine>> getMachineListByType() {
        int localtID = tID_to_localtID.get(Thread.currentThread().getId());
        if (!machineListByTypePerThread.containsKey(localtID)) {
            machineListByTypePerThread.put(localtID, new HashMap<>()); // Initialize with an empty HashMap if key doesn't exist
        }
        return machineListByTypePerThread.get(localtID);
    }

    public static void putMachineListByType(Map<Class<? extends PMachine>, List<PMachine>> machineListByType) {
        int localtID = tID_to_localtID.get(Thread.currentThread().getId());
        machineListByTypePerThread.put(localtID, machineListByType);
    }

    public static SortedSet<PMachine> getMachineSet() {
        int localtID = tID_to_localtID.get(Thread.currentThread().getId());
        if (!machineSetPerThread.containsKey(localtID)) {
            machineSetPerThread.put(localtID, new TreeSet<>());
        }
        return machineSetPerThread.get(localtID);
    }

    public static Scheduler getScheduler() {
        int localtID = tID_to_localtID.get(Thread.currentThread().getId());
        return schedulers.get(localtID);
    }

    /**
     * Get a machine of a given type and index if exists, else return null.
     *
     * @param pid Machine pid
     * @return Machine
     */
    public static PMachine getGlobalMachine(PMachineId pid) {
        Map<Class<? extends PMachine>, List<PMachine>> machineListByType = getMachineListByType();
        List<PMachine> machinesOfType = machineListByType.get(pid.getType());
        if (machinesOfType == null) {
            return null;
        }
        if (pid.getTypeId() >= machinesOfType.size()) {
            return null;
        }
        PMachine result = machineListByType.get(pid.getType()).get(pid.getTypeId());
        assert (getMachineSet().contains(result));
        return result;
    }

    /**
     * Add a machine.
     *
     * @param machine      Machine to add
     * @param machineCount Machine type count
     */
    public static void addGlobalMachine(PMachine machine, int machineCount) {
        Map<Class<? extends PMachine>, List<PMachine>> machineListByType = getMachineListByType();
        if (!machineListByType.containsKey(machine.getClass())) {
            machineListByType.put(machine.getClass(), new ArrayList<>());
            putMachineListByType(machineListByType); 
        }
        assert (machineCount == machineListByType.get(machine.getClass()).size());
        machineListByType.get(machine.getClass()).add(machine);
        getMachineSet().add(machine);
        assert (machineListByType.get(machine.getClass()).get(machineCount) == machine);
    }
}