package psym.commandline;

import lombok.Getter;
import lombok.Setter;
import psym.runtime.scheduler.choiceorchestration.ChoiceLearningRewardMode;
import psym.runtime.scheduler.choiceorchestration.ChoiceLearningStateMode;
import psym.runtime.scheduler.choiceorchestration.ChoiceOrchestrationMode;
import psym.runtime.scheduler.taskorchestration.TaskOrchestrationMode;
import psym.utils.StateHashingMode;
import psym.valuesummary.solvers.SolverType;
import psym.valuesummary.solvers.sat.expr.ExprLibType;

import java.io.Serializable;

/**
 * Represents the configuration of the P Symbolic tool
 */
public class PSymConfiguration implements Serializable {
    // name of the psym configuration file
    @Getter @Setter
    String configFile = "";

    // strategy of exploration
    @Getter @Setter
    String strategy = "learn";

    // default name of the test driver
    @Getter
    final String testDriverDefault = "DefaultImpl";

    // name of the test driver
    @Getter @Setter
    String testDriver = testDriverDefault;

    // time limit in seconds (0 means infinite)
    @Getter @Setter
    double timeLimit = 0;

    // memory limit in megabytes (0 means infinite)
    @Getter @Setter
    double memLimit = (Runtime.getRuntime().maxMemory() / 1024 / 1024);

    // name of the output folder
    @Getter @Setter
    String outputFolder = "output";

    // max number of executions bound provided by the user
    @Getter @Setter
    int maxExecutions = 1;

    // max steps/depth bound provided by the user
    @Getter @Setter
    int maxStepBound = 10000;

    // fail on reaching the maximum scheduling step bound
    @Getter @Setter
    boolean failOnMaxStepBound = false;

    // random seed
    @Getter @Setter
    long randomSeed = System.currentTimeMillis();

    // name of the project
    @Getter @Setter
    String projectName = "default";

    // name of the cex file to read the replayer state
    @Getter @Setter
    String readReplayerFromFile = "";

    // max scheduling choice bound provided by the user
    @Getter @Setter
    int schedChoiceBound = 1;

    // max data choice bound provided by the user
    @Getter @Setter
    int dataChoiceBound = 1;

    // mode of state hashing
    @Getter @Setter
    StateHashingMode stateHashingMode = StateHashingMode.Exact;

    // use symmetry
    @Getter @Setter
    boolean useSymmetry = false;

    // use backtracking
    @Getter @Setter
    boolean useBacktrack = true;

    // mode of choice orchestration
    @Getter @Setter
    ChoiceOrchestrationMode choiceOrchestration = ChoiceOrchestrationMode.EpsilonGreedy;

    // mode of choice learning state mode
    @Getter @Setter
    ChoiceLearningStateMode choiceLearningStateMode = ChoiceLearningStateMode.LastStep;

    // mode of choice learning reward mode
    @Getter @Setter
    ChoiceLearningRewardMode choiceLearningRewardMode = ChoiceLearningRewardMode.Coverage;

    // mode of task orchestration
    @Getter @Setter
    TaskOrchestrationMode taskOrchestration = TaskOrchestrationMode.CoverageEpsilonGreedy;

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
    boolean useFilters = false;

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

    public boolean isIterative() {
        return (getSchedChoiceBound() != 0 || getDataChoiceBound() != 0);
    }

    public boolean isChoiceOrchestrationLearning() {
        return  (getChoiceOrchestration() == ChoiceOrchestrationMode.QLearning) ||
                (getChoiceOrchestration() == ChoiceOrchestrationMode.EpsilonGreedy);
    }

    public void setToRandom() {
        this.setStrategy("random");
        this.setSchedChoiceBound(1);
        this.setDataChoiceBound(1);
        this.setChoiceOrchestration(ChoiceOrchestrationMode.Random);
        this.setTaskOrchestration(TaskOrchestrationMode.Random);
    }

    public void setToDfs() {
        this.setStrategy("dfs");
        this.setSchedChoiceBound(1);
        this.setDataChoiceBound(1);
        this.setChoiceOrchestration(ChoiceOrchestrationMode.Random);
        this.setTaskOrchestration(TaskOrchestrationMode.DepthFirst);
    }

    public void setToAllLearn() {
        this.setStrategy("learn");
        this.setSchedChoiceBound(1);
        this.setDataChoiceBound(1);
        this.setChoiceOrchestration(ChoiceOrchestrationMode.EpsilonGreedy);
        this.setTaskOrchestration(TaskOrchestrationMode.CoverageEpsilonGreedy);
    }

    public void setToChoiceLearn() {
        this.setStrategy("learn");
        this.setSchedChoiceBound(1);
        this.setDataChoiceBound(1);
        this.setChoiceOrchestration(ChoiceOrchestrationMode.EpsilonGreedy);
        this.setTaskOrchestration(TaskOrchestrationMode.Random);
    }

    public void setToBacktrackLearn() {
        this.setStrategy("learn");
        this.setSchedChoiceBound(1);
        this.setDataChoiceBound(1);
        this.setChoiceOrchestration(ChoiceOrchestrationMode.Random);
        this.setTaskOrchestration(TaskOrchestrationMode.CoverageEpsilonGreedy);
    }

    public void setToSymex() {
        this.setStrategy("symex");
        this.setSchedChoiceBound(0);
        this.setDataChoiceBound(0);
        this.setStateHashingMode(StateHashingMode.None);
        this.setChoiceOrchestration(ChoiceOrchestrationMode.None);
        this.setTaskOrchestration(TaskOrchestrationMode.DepthFirst);
    }

    public void setToFuzz() {
        this.setStrategy("fuzz");
        this.setSchedChoiceBound(1);
        this.setDataChoiceBound(1);
        this.setStateHashingMode(StateHashingMode.None);
        this.setUseBacktrack(false);
        this.setChoiceOrchestration(ChoiceOrchestrationMode.Random);
        this.setTaskOrchestration(TaskOrchestrationMode.Random);
    }

    public void setToCoverage() {
        this.setStrategy("coverage");
        this.setSchedChoiceBound(1);
        this.setDataChoiceBound(1);
        this.setChoiceOrchestration(ChoiceOrchestrationMode.Random);
        this.setTaskOrchestration(TaskOrchestrationMode.CoverageAStar);
    }

    public void setToDebug() {
        this.setStrategy("debug");
    }


}
