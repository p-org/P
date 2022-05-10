package psymbolic.commandline;

import lombok.Getter;
import lombok.Setter;
import psymbolic.valuesummary.solvers.SolverType;
import psymbolic.valuesummary.solvers.sat.expr.ExprLibType;

import java.io.Serializable;

/**
 * Represents the configuration of the P Symbolic tool
 */
public class PSymConfiguration implements Serializable {

    // name of the main machine
    @Getter @Setter
    private String mainMachine = "Main";

    // name of the target project
    @Getter @Setter
    private String projectName = "test";

    @Getter
    // max depth bound after which the search will stop automatically
    private final int maxDepthBound = 1000;

    @Getter @Setter
    // max depth bound provided by the user
    private int depthBound = maxDepthBound;

    @Getter @Setter
    // max iterations bound provided by the user
    private int iterationBound = 1;

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
    private boolean useReceiverQueueSemantics = false;

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
    // use filters
    private boolean useFilters = true;

    @Getter @Setter
    // level of verbosity for the logging
    private int verbosity = 1;

    @Getter @Setter
    // level of stats collection
    private int collectStats = 1;

    @Getter @Setter
    // type of solver engine
    private SolverType solverType = SolverType.BDD;

    @Getter @Setter
    // type of solver engine
    private ExprLibType exprLibType = ExprLibType.Bdd;

    @Getter @Setter
    // time limit in seconds (0 means infinite)
    private double timeLimit = 0;

    @Getter @Setter
    // memory limit in megabytes (0 means infinite)
    private double memLimit = (Runtime.getRuntime().maxMemory() / 1000000);

    // name of the file to read the program state
    @Getter @Setter
    private String readFromFile = "";

    @Getter @Setter
    // whether or not to write the program state(s) to file
    private boolean writeToFile = false;

    @Getter @Setter
    // use backtracking
    private boolean useBacktrack = true;

    @Getter @Setter
    // use randomization
    private boolean useRandom = true;

    @Getter @Setter
    // random seed
    private int randomSeed = 0;
}
