package pex.runtime;

import lombok.Getter;
import lombok.Setter;
import pex.commandline.PExConfig;
import pex.runtime.machine.PMachine;
import pex.runtime.machine.PMachineId;
import pex.runtime.scheduler.Scheduler;
import pex.runtime.scheduler.explicit.choiceselector.ChoiceSelector;
import pex.runtime.scheduler.explicit.choiceselector.ChoiceSelectorQL;
import pex.runtime.scheduler.explicit.choiceselector.ChoiceSelectorRandom;
import pex.runtime.scheduler.explicit.strategy.SearchStrategyMode;

import java.util.*;

/**
 * Represents global data structures represented with a singleton class
 */
public class PExGlobal {
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
    private static PExConfig config = null;
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
     * Choice orchestrator
     */
    @Getter
    private static ChoiceSelector choiceSelector = null;

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

    /**
     * Set choice orchestrator
     */
    public static void setChoiceSelector() {
        switch (config.getChoiceSelectorMode()) {
            case Random:
                choiceSelector = new ChoiceSelectorRandom();
                break;
            case QL:
                choiceSelector = new ChoiceSelectorQL();
                break;
            default:
                throw new RuntimeException("Unrecognized choice orchestrator: " + config.getChoiceSelectorMode());
        }
    }
}