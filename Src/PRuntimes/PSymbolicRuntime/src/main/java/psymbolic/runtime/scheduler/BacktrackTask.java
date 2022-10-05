package psymbolic.runtime.scheduler;

import lombok.Getter;
import lombok.Setter;
import psymbolic.runtime.statistics.CoverageStats;
import psymbolic.utils.OrchestrationMode;
import psymbolic.utils.RandomNumberGenerator;

import java.io.Serializable;
import java.math.BigDecimal;
import java.util.ArrayList;
import java.util.List;

public class BacktrackTask implements Serializable {
    @Getter
    private int id;
    @Getter @Setter
    private BigDecimal prefixCoverage = new BigDecimal(0);
    @Getter
    private BigDecimal coverage = new BigDecimal(0);
    @Getter @Setter
    private int depth = -1;
    @Getter @Setter
    private int choiceDepth = -1;
    @Getter
    final private List<Schedule.Choice> choices = new ArrayList<>();
    @Getter @Setter
    private BacktrackTask parentTask = null;
    @Getter
    private List<Integer> children = new ArrayList<>();

    @Getter
    final private List<CoverageStats.CoverageChoiceDepthStats> perChoiceDepthStats = new ArrayList<>();

    @Getter
    private BigDecimal priority = new BigDecimal(0);

    public BacktrackTask(int id) {
        this.id = id;
    }

    public void cleanup() {
        choices.clear();
        perChoiceDepthStats.clear();
    }

    public void setChoices(List<Schedule.Choice> inputChoices) {
        assert(choices.isEmpty());
        for (Schedule.Choice choice: inputChoices) {
            choices.add(choice.getCopy());
        }
    }

    public void setPerChoiceDepthStats(List<CoverageStats.CoverageChoiceDepthStats> inputStats) {
        assert(perChoiceDepthStats.isEmpty());
        for (CoverageStats.CoverageChoiceDepthStats stat: inputStats) {
            perChoiceDepthStats.add(stat.getCopy());
        }
    }

    public void setCoverage(BigDecimal inputCoverage) {
        assert(inputCoverage.doubleValue() <= this.prefixCoverage.doubleValue()):
                String.format("Error in coverage estimation: path coverage (%.5f) should be <= prefix coverage (%.5f)",
                        inputCoverage, this.prefixCoverage);
        coverage = inputCoverage;
    }

    public boolean isInitialTask() {
        return this.id == 0;
    }

    public void addChild(int taskId) {
        children.add(taskId);
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

    /**
     * Set priority of the task with given orchestration mode
     * @param orchestration Orchestration mode
     */
    public void setPriority(OrchestrationMode orchestration) {
        switch (orchestration) {
            case None:
                throw new RuntimeException("Unexpected orchestration mode: " + orchestration);
            case Random:
                priority = BigDecimal.valueOf(RandomNumberGenerator.getInstance().getRandomLong());
                break;
            case CoverageAStar:
                priority = this.prefixCoverage;
                break;
            case CoverageEstimate:
                priority = this.prefixCoverage;
                if (!this.isInitialTask()) {
                    assert(parentTask != null);
                    priority = priority.multiply(parentTask.getCoverage());
                }
                break;
            case CoverageParent:
                priority = this.prefixCoverage;
                if (!this.isInitialTask()) {
                    assert(parentTask != null);
                    priority = parentTask.getCoverage();
                }
                break;
            case DepthFirst:
                priority = BigDecimal.valueOf(this.id);
                break;
            default:
                throw new RuntimeException("Unrecognized orchestration mode: " + orchestration);
        }
    }

}
