package psymbolic.commandline;

import lombok.Getter;
import lombok.Setter;
import psymbolic.runtime.scheduler.choiceorchestration.ChoiceOrchestrationMode;
import psymbolic.runtime.scheduler.taskorchestration.TaskOrchestrationMode;
import psymbolic.valuesummary.solvers.SolverType;
import psymbolic.valuesummary.solvers.sat.expr.ExprLibType;

import java.io.Serializable;

/**
 * Represents the configuration of the P Symbolic tool
 */
public class PSymConfiguration implements Serializable {
    // mode of exploration
    @Getter @Setter
    String mode = "default";

    // time limit in seconds (0 means infinite)
    @Getter @Setter
    double timeLimit = 60;

    // memory limit in megabytes (0 means infinite)
    @Getter @Setter
    double memLimit = (Runtime.getRuntime().maxMemory() / 1000000);

    // random seed
    @Getter @Setter
    long randomSeed = System.currentTimeMillis();

    // default name of the test driver
    @Getter
    final String testDriverDefault = "DefaultTestDriver";

    // name of the test driver
    @Getter @Setter
    String testDriver = testDriverDefault;

    // default name of the project
    @Getter
    final String projectNameDefault = "test";

    // name of the project
    @Getter @Setter
    String projectName = projectNameDefault;

    // name of the output folder
    @Getter @Setter
    String outputFolder = "output";

    // name of the cex file to read the replayer state
    @Getter @Setter
    String readReplayerFromFile = "";

    // max steps/depth bound provided by the user
    @Getter @Setter
    int maxStepBound = 1000;

    // max number of executions bound provided by the user
    @Getter @Setter
    int maxExecutions = 0;

    // max scheduling choice bound provided by the user
    @Getter @Setter
    int schedChoiceBound = 1;

    // max data choice bound provided by the user
    @Getter @Setter
    int dataChoiceBound = 1;

    // use state caching
    @Getter @Setter
    boolean useStateCaching = true;

    // use backtracking
    @Getter @Setter
    boolean useBacktrack = true;

    // mode of choice orchestration
    @Getter @Setter
    ChoiceOrchestrationMode choiceOrchestration = ChoiceOrchestrationMode.Random;

    // mode of task orchestration
    @Getter @Setter
    TaskOrchestrationMode taskOrchestration = TaskOrchestrationMode.CoverageAStar;

    // max number of children tasks per execution
    @Getter @Setter
    int maxBacktrackTasksPerExecution = 2;

    // type of solver engine
    @Getter @Setter
    SolverType solverType = SolverType.BDD;

    // type of expression engine
    @Getter @Setter
    ExprLibType exprLibType = ExprLibType.Bdd;

    // name of the file to read the program state
    @Getter @Setter
    String readFromFile = "";

    // whether or not to write the program state(s) to file
    @Getter @Setter
    boolean writeToFile = false;

    // use filters
    @Getter @Setter
    boolean useFilters = true;

    // intersect with receiver queue semantics
    @Getter @Setter
    boolean useReceiverQueueSemantics = false;

    // use symbolic sleep sets
    @Getter @Setter
    boolean useSleepSets = false;

    // turn all sender queues into bags -- currently not implemented
    @Getter @Setter
    boolean useBagSemantics = false;

    // apply DPOR
    @Getter @Setter
    boolean dpor = false;

    // level of stats collection
    @Getter @Setter
    int collectStats = 1;

    // level of verbosity for the logging
    @Getter @Setter
    int verbosity = 0;

    // max internal steps before throwing an exception
    @Getter
    final int maxInternalSteps = 1000;

    public boolean isSymbolic() {
        return (getSchedChoiceBound() != 1 || getDataChoiceBound() != 1);
    }

    public void setToDefault() {
        this.setMode("default");
    }

    public void setToBmc() {
        this.setMode("bmc");
        this.setSchedChoiceBound(0);
        this.setDataChoiceBound(0);
        this.setUseStateCaching(false);
    }

    public void setToRandom() {
        this.setMode("random");
        this.setSchedChoiceBound(1);
        this.setDataChoiceBound(1);
        this.setChoiceOrchestration(ChoiceOrchestrationMode.Random);
        this.setTaskOrchestration(TaskOrchestrationMode.Random);
    }

    public void setToFuzz() {
        this.setMode("fuzz");
        this.setSchedChoiceBound(1);
        this.setDataChoiceBound(1);
        this.setUseStateCaching(false);
        this.setUseBacktrack(false);
        this.setChoiceOrchestration(ChoiceOrchestrationMode.Random);
        this.setTaskOrchestration(TaskOrchestrationMode.Random);
    }

    public void setToDebug() {
        this.setMode("debug");
    }


}
