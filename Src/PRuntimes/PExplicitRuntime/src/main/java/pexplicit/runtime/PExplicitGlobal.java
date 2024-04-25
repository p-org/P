package pexplicit.runtime;

import lombok.Getter;
import lombok.Setter;
import pexplicit.commandline.PExplicitConfig;
import pexplicit.runtime.machine.PMachine;
import pexplicit.runtime.scheduler.Scheduler;
import pexplicit.runtime.scheduler.explicit.strategy.SearchStrategy;
import pexplicit.runtime.scheduler.explicit.strategy.SearchStrategyMode;

import java.util.*;

/**
 * Represents global data structures represented with a singleton class
 */
public class PExplicitGlobal {
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
    @Getter
    @Setter
    private static Scheduler scheduler = null;
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
     * Result of the run
     **/
    @Getter
    @Setter
    private static String result = "error";

    /**
     * Get a machine of a given type and index if exists, else return null.
     *
     * @param type Machine type
     * @param idx  Machine index
     * @return Machine
     */
    public static PMachine getGlobalMachine(Class<? extends PMachine> type, int idx) {
        List<PMachine> machinesOfType = machineListByType.get(type);
        if (machinesOfType == null) {
            return null;
        }
        if (idx >= machinesOfType.size()) {
            return null;
        }
        PMachine result = machineListByType.get(type).get(idx);
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