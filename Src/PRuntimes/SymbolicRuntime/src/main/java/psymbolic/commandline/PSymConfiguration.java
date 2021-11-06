package psymbolic.commandline;

import lombok.Getter;
import lombok.Setter;

/**
 * Represents the configuration of the P Symbolic tool
 */
public class PSymConfiguration {

    // name of the main machine
    @Getter @Setter
    private String mainMachine = "Main";

    @Getter
    // max depth bound after which the search will stop automatically
    private final int maxDepthBound = 1000;

    @Getter @Setter
    // max depth bound provided by the user
    private int depthBound = maxDepthBound;

    // max input choice bound at each depth after which the search will truncate the choices
    private final int maxInputChoiceBound = 100;

    @Getter @Setter
    // max input choice bound provided by the user
    private int inputChoiceBound = maxInputChoiceBound;

    // max scheduling choice bound at each depth after which the search will truncate the scheduling choices
    private final int maxSchedChoiceBound = 100;

    @Getter @Setter
    // max input choice bound provided by the user
    private int schedChoiceBound = maxSchedChoiceBound;

    @Getter
    // max internal steps before throwing an exception
    private int maxInternalSteps = 1000;

    @Getter @Setter
    // intersect with receiver queue semantics
    private boolean addReceiverQueueSemantics = false;

    @Getter @Setter
    // use symbolic sleep sets
    private boolean useSleepSets = false;

    @Getter @Setter
    // turn all sender queues into bags -- currently not implemented
    private boolean useBagSemantics = false;

    @Getter @Setter
    // apply DPOR
    private boolean dpor = false;

    @Getter @Setter
    // level of verbosity for the logging
    private int verbosity = 1;

    @Getter @Setter
    // whether to collect stats or not
    private boolean collectStats = false;
}
