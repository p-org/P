package psymbolic.runtime.scheduler;

import lombok.Getter;
import lombok.Setter;
import psymbolic.runtime.scheduler.taskorchestration.*;
import psymbolic.runtime.statistics.CoverageStats;

import java.io.Serializable;
import java.math.BigDecimal;
import java.math.RoundingMode;
import java.util.ArrayList;
import java.util.List;

public class BacktrackTask implements Serializable {
    @Setter
    private static TaskOrchestrationMode orchestration;
    private static TaskOrchestrator taskOrchestrator = null;
    @Getter
    private int id;
    @Getter
    private BigDecimal coverage = new BigDecimal(0);
    @Getter @Setter
    private BigDecimal prefixCoverage = new BigDecimal(0);

    @Getter
    private BigDecimal estimatedCoverage = new BigDecimal(0);
    @Getter @Setter
    private int depth = -1;
    @Getter @Setter
    private int choiceDepth = -1;
    @Getter
    final private List<Schedule.Choice> choices = new ArrayList<>();
    @Getter
    private int numBacktracks = 0;
    @Getter
    private int numDataBacktracks = 0;
    @Getter @Setter
    private BacktrackTask parentTask = null;
    @Getter
    private List<BacktrackTask> children = new ArrayList<>();
    @Getter
    private boolean completed = false;

    @Getter
    final private List<CoverageStats.CoverageChoiceDepthStats> perChoiceDepthStats = new ArrayList<>();

    public BacktrackTask(int id) {
        this.id = id;
    }

    public void cleanup() {
        choices.clear();
        numBacktracks = 0;
        numDataBacktracks = 0;
        perChoiceDepthStats.clear();
    }

    public void setChoices(List<Schedule.Choice> inputChoices) {
        assert(choices.isEmpty());
        for (Schedule.Choice choice: inputChoices) {
            choices.add(choice.getCopy());
            if (!choice.isBacktrackEmpty()) {
                numBacktracks++;
                if (!choice.isDataBacktrackEmpty()) {
                    numDataBacktracks++;
                }
            }
        }
    }

    public void setPerChoiceDepthStats(List<CoverageStats.CoverageChoiceDepthStats> inputStats) {
        assert(perChoiceDepthStats.isEmpty());
        for (CoverageStats.CoverageChoiceDepthStats stat: inputStats) {
            perChoiceDepthStats.add(stat.getCopy());
        }
    }

    public boolean isInitialTask() {
        return id == 0;
    }

    public void addChild(BacktrackTask task) {
        children.add(task);
    }

    @Override
    public String toString() {
        return String.format("task%d", id);
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this)
            return true;
        else if (!(obj instanceof BacktrackTask)) {
            return false;
        }
        return this.id == ((BacktrackTask) obj).id;
    }

    @Override
    public int hashCode() {
        return id;
    }

    private void setCoverageEstimate() {
        if (isInitialTask()) {
            estimatedCoverage = coverage;
        } else {
            assert (parentTask != null);
            estimatedCoverage = parentTask.getCoverage();
            int count = 1;
            for (BacktrackTask t : parentTask.getChildren()) {
                if (t.completed) {
                    estimatedCoverage = estimatedCoverage.add(t.getCoverage());
                    count += 1;
                }
            }
            if (count != 1) {
                estimatedCoverage = estimatedCoverage.divide(BigDecimal.valueOf(count), 200, RoundingMode.FLOOR);
            }
        }
    }

    /**
     * Set priority of the task with given orchestration mode
     */
    public void setPriority() {
        switch (orchestration) {
            case DepthFirst:
                throw new RuntimeException("Unexpected orchestration mode: " + orchestration);
            case Random:
            case CoverageAStar:
            case CoverageEpsilonGreedy:
                // do nothing
                break;
            case CoverageEstimate:
                setCoverageEstimate();
                break;
            default:
                throw new RuntimeException("Unrecognized orchestration mode: " + orchestration);
        }
        taskOrchestrator.addPriority(this);
    }

    public void postProcess(BigDecimal inputCoverage) {
        assert(inputCoverage.doubleValue() <= prefixCoverage.doubleValue()):
                String.format("Error in coverage estimation: path coverage (%.5f) should be <= prefix coverage (%.5f)",
                        inputCoverage, prefixCoverage);
        coverage = inputCoverage;
        estimatedCoverage = inputCoverage;
        completed = true;

        switch (orchestration) {
            case DepthFirst:
                throw new RuntimeException("Unexpected orchestration mode: " + orchestration);
            case Random:
            case CoverageAStar:
            case CoverageEpsilonGreedy:
                // do nothing
                break;
            case CoverageEstimate:
                if (!isInitialTask()) {
                    assert(parentTask != null);
                    for (BacktrackTask t: parentTask.getChildren()) {
                        if (!t.completed) {
                            t.setCoverageEstimate();
                            taskOrchestrator.addPriority(t);
                        }
                    }
                }
                break;
            default:
                throw new RuntimeException("Unrecognized orchestration mode: " + orchestration);
        }
    }

    public static void initialize(TaskOrchestrationMode orch) {
        orchestration = orch;
        switch (orchestration) {
            case DepthFirst:
                // do nothing
                break;
            case Random:
                taskOrchestrator = new TaskOrchestratorRandom();
                break;
            case CoverageAStar:
                taskOrchestrator = new TaskOrchestratorCoverageAStar();
                break;
            case CoverageEstimate:
                taskOrchestrator = new TaskOrchestratorCoverageEstimate();
                break;
            case CoverageEpsilonGreedy:
                taskOrchestrator = new TaskOrchestratorCoverageEpsilonGreedy();
                break;
            default:
                throw new RuntimeException("Unrecognized orchestration mode: " + orchestration);
        }
    }

    public static BacktrackTask getNextTask() throws InterruptedException {
        BacktrackTask result;
        switch (orchestration) {
            case DepthFirst:
                throw new RuntimeException("Unexpected orchestration mode: " + orchestration);
            case Random:
            case CoverageAStar:
            case CoverageEstimate:
            case CoverageEpsilonGreedy:
                result = taskOrchestrator.getNext();
                break;
            default:
                throw new RuntimeException("Unrecognized orchestration mode: " + orchestration);
        }
        taskOrchestrator.remove(result);
        return result;
    }

}
