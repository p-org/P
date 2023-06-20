package psym.commandline;

import lombok.Getter;
import lombok.Setter;
import psym.runtime.scheduler.explicit.choiceorchestration.ChoiceLearningRewardMode;
import psym.runtime.scheduler.explicit.choiceorchestration.ChoiceLearningStateMode;
import psym.runtime.scheduler.explicit.choiceorchestration.ChoiceOrchestrationMode;
import psym.runtime.scheduler.symmetry.SymmetryMode;
import psym.runtime.scheduler.explicit.taskorchestration.TaskOrchestrationMode;
import psym.runtime.scheduler.explicit.StateCachingMode;
import psym.valuesummary.solvers.SolverType;
import psym.valuesummary.solvers.sat.expr.ExprLibType;

import java.io.Serializable;

/**
 * Represents the configuration of the P Symbolic tool
 */
public class PSymConfiguration implements Serializable {

    // default name of the test driver
    @Getter
    final String testDriverDefault = "DefaultImpl";
    // max internal steps before throwing an exception
    @Getter
    final int maxInternalSteps = 1000;
    // name of the test driver
    @Getter
    @Setter
    String testDriver = testDriverDefault;
    // name of the project
    @Getter
    @Setter
    String projectName = "default";
    // name of the output folder
    @Getter
    @Setter
    String outputFolder = "output";
    // time limit in seconds (0 means infinite)
    @Getter
    @Setter
    double timeLimit = 0;
    // memory limit in megabytes (0 means infinite)
    @Getter
    @Setter
    double memLimit = (Runtime.getRuntime().maxMemory() / 1024 / 1024);
    // level of verbosity for the logging
    @Getter
    @Setter
    int verbosity = 0;
    // strategy of exploration
    @Getter
    @Setter
    String strategy = "learn";
    // max number of executions bound provided by the user
    @Getter
    @Setter
    int maxExecutions = 1;
    // max steps/depth bound provided by the user
    @Getter
    @Setter
    int maxStepBound = 10000;
    // fail on reaching the maximum scheduling step bound
    @Getter
    @Setter
    boolean failOnMaxStepBound = false;
    // name of the cex file to read the replayer state
    @Getter
    @Setter
    String readReplayerFromFile = "";
    // random seed
    @Getter
    @Setter
    long randomSeed = System.currentTimeMillis();
    // name of the psym configuration file
    @Getter
    @Setter
    String configFile = "";
    // mode of state hashing
    @Getter
    @Setter
    StateCachingMode stateCachingMode = StateCachingMode.Fast;
    // symmetry mode
    @Getter
    @Setter
    SymmetryMode symmetryMode = SymmetryMode.None;
    // use backtracking
    @Getter
    @Setter
    boolean useBacktrack = true;
    // max number of children tasks per execution
    @Getter
    @Setter
    int maxBacktrackTasksPerExecution = 2;
    // mode of choice orchestration
    @Getter
    @Setter
    ChoiceOrchestrationMode choiceOrchestration = ChoiceOrchestrationMode.EpsilonGreedy;
    // mode of choice learning state mode
    @Getter
    @Setter
    ChoiceLearningStateMode choiceLearningStateMode = ChoiceLearningStateMode.LastStep;
    // mode of choice learning reward mode
    @Getter
    @Setter
    ChoiceLearningRewardMode choiceLearningRewardMode = ChoiceLearningRewardMode.Coverage;
    // mode of task orchestration
    @Getter
    @Setter
    TaskOrchestrationMode taskOrchestration = TaskOrchestrationMode.CoverageEpsilonGreedy;
    // type of solver engine
    @Getter
    @Setter
    SolverType solverType = SolverType.BDD;
    // type of expression engine
    @Getter
    @Setter
    ExprLibType exprLibType = ExprLibType.Bdd;
    // name of the file to read the program state
    @Getter
    @Setter
    String readFromFile = "";
    // whether or not to write the program state(s) to file
    @Getter
    @Setter
    boolean writeToFile = false;
    // level of stats collection
    @Getter
    @Setter
    int collectStats = 1;

    public boolean isSymbolic() {
        return (strategy.equals("symex"));
    }

    public boolean isIterative() {
        return !isSymbolic();
    }

    public boolean isChoiceOrchestrationLearning() {
        return (getChoiceOrchestration() == ChoiceOrchestrationMode.QLearning) ||
                (getChoiceOrchestration() == ChoiceOrchestrationMode.EpsilonGreedy);
    }

    public void setToRandom() {
        this.setStrategy("random");
        this.setChoiceOrchestration(ChoiceOrchestrationMode.Random);
        this.setTaskOrchestration(TaskOrchestrationMode.Random);
    }

    public void setToDfs() {
        this.setStrategy("dfs");
        this.setChoiceOrchestration(ChoiceOrchestrationMode.Random);
        this.setTaskOrchestration(TaskOrchestrationMode.DepthFirst);
    }

    public void setToAllLearn() {
        this.setStrategy("learn");
        this.setChoiceOrchestration(ChoiceOrchestrationMode.EpsilonGreedy);
        this.setTaskOrchestration(TaskOrchestrationMode.CoverageEpsilonGreedy);
    }

    public void setToChoiceLearn() {
        this.setStrategy("learn");
        this.setChoiceOrchestration(ChoiceOrchestrationMode.EpsilonGreedy);
        this.setTaskOrchestration(TaskOrchestrationMode.Random);
    }

    public void setToBacktrackLearn() {
        this.setStrategy("learn");
        this.setChoiceOrchestration(ChoiceOrchestrationMode.Random);
        this.setTaskOrchestration(TaskOrchestrationMode.CoverageEpsilonGreedy);
    }

    public void setToSymex() {
        this.setStrategy("symex");
        this.setStateCachingMode(StateCachingMode.None);
        this.setChoiceOrchestration(ChoiceOrchestrationMode.None);
        this.setTaskOrchestration(TaskOrchestrationMode.DepthFirst);
    }

    public void setToFuzz() {
        this.setStrategy("fuzz");
        this.setStateCachingMode(StateCachingMode.None);
        this.setUseBacktrack(false);
        this.setChoiceOrchestration(ChoiceOrchestrationMode.Random);
        this.setTaskOrchestration(TaskOrchestrationMode.Random);
    }

    public void setToCoverage() {
        this.setStrategy("coverage");
        this.setChoiceOrchestration(ChoiceOrchestrationMode.Random);
        this.setTaskOrchestration(TaskOrchestrationMode.CoverageAStar);
    }

    public void setToReplay() {
        this.setStrategy("replay");
        this.setCollectStats(0);
        this.setStateCachingMode(StateCachingMode.None);
        this.setUseBacktrack(false);
        this.setSymmetryMode(SymmetryMode.None);
    }

    public void setToDebug() {
        this.setStrategy("debug");
    }


}
