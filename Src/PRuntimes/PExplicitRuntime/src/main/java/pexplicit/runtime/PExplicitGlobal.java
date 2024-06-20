package pexplicit.runtime;

import lombok.Getter;
import lombok.Setter;
import pexplicit.commandline.PExplicitConfig;
import pexplicit.runtime.machine.PMachine;
import pexplicit.runtime.machine.PMachineId;
import pexplicit.runtime.scheduler.Scheduler;
import pexplicit.runtime.scheduler.explicit.strategy.SearchStrategyMode;

import java.util.*;
/**
 * Represents global data structures represented with a singleton class
 */
public class PExplicitGlobal {

    //  @Getter
    //  @Setter
    // private static Map< >
    
    @Getter
    private static final int maxThreads = 2;
    
    @Getter
    private static Map <Long, Integer> tID_to_localtID = new HashMap<>();


    // Method to add to tID_to_localtID
    public static void addTotIDtolocaltID(long tID, Integer localtID) {
        tID_to_localtID.put(tID, localtID);
    }

    /**
     * Mapping from machine type to list of all machine instances
     */    
     @Getter
    private static final Map<Class<? extends PMachine>, List<PMachine>> machineListByType = new HashMap<>();
    /**
     * Set of machines
     */
    @Getter
    private static final SortedSet<PMachine> machineSet = new TreeSet<>();
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
     * Scheduler
     **/
    // @Getter
    // @Setter
    // private static Scheduler scheduler = null; // Remove this!

    @Getter
    @Setter
    private static ArrayList<Scheduler> schedulers = new ArrayList<>();    


    public static Scheduler getScheduler() {
        int localtID = tID_to_localtID.get(Thread.currentThread().getId());
        return schedulers.get(localtID);        
    }

    @Getter
    @Setter
    private static SearchStrategyMode searchStrategyMode;
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

    /**
     * Get a machine of a given type and index if exists, else return null.
     *
     * @param pid Machine pid
     * @return Machine
     */
    public static PMachine getGlobalMachine(PMachineId pid) {
        List<PMachine> machinesOfType = machineListByType.get(pid.getType());
        if (machinesOfType == null) {
            return null;
        }
        if (pid.getTypeId() >= machinesOfType.size()) {
            return null;
        }
        PMachine result = machineListByType.get(pid.getType()).get(pid.getTypeId());
        assert (machineSet.contains(result));
        return result;
    }

    /**
     * Add a machine.
     *
     * @param machine      Machine to add
     * @param machineCount Machine type count
     */
    public static void addGlobalMachine(PMachine machine, int machineCount) {
        if (!machineListByType.containsKey(machine.getClass())) {
            machineListByType.put(machine.getClass(), new ArrayList<>());
        }
        assert (machineCount == machineListByType.get(machine.getClass()).size());
        machineListByType.get(machine.getClass()).add(machine);
        machineSet.add(machine);
        assert (machineListByType.get(machine.getClass()).get(machineCount) == machine);
    }
}