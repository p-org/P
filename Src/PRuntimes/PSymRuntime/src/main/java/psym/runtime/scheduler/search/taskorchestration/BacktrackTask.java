package psym.runtime.scheduler.search.taskorchestration;

import java.io.Serializable;
import java.math.BigDecimal;
import java.math.RoundingMode;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

import lombok.Getter;
import lombok.Setter;
import psym.runtime.scheduler.Schedule;
import psym.runtime.statistics.CoverageStats;

public class BacktrackTask implements Serializable {
  @Setter private static TaskOrchestrationMode orchestration;
  private static TaskOrchestrator taskOrchestrator = null;
  @Getter private Map<Integer, Schedule.Choice> prefixChoices = new HashMap<>();
  @Getter private List<Schedule.Choice> suffixChoices = new ArrayList<>();

  @Getter
  private final Map<Integer, CoverageStats.CoverageChoiceDepthStats> prefixPerChoiceDepthStats =
      new HashMap<>();

  @Getter
  private final List<CoverageStats.CoverageChoiceDepthStats> suffixPerChoiceDepthStats =
          new ArrayList<>();

  @Getter private final int id;
  @Getter private final List<BacktrackTask> children = new ArrayList<>();
  @Getter private BigDecimal coverage = new BigDecimal(0);
  @Getter @Setter private BigDecimal prefixCoverage = new BigDecimal(0);
  @Getter private BigDecimal estimatedCoverage = new BigDecimal(0);
  @Getter @Setter private int depth = -1;
  @Getter @Setter private int backtrackChoiceDepth = -1;
  @Getter private int numBacktracks = 0;
  @Getter private int numDataBacktracks = 0;
  @Getter @Setter private BacktrackTask parentTask = null;
  @Getter private boolean completed = false;

  public BacktrackTask(int id) { this.id = id; }

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

  public void cleanup() {
    numBacktracks = 0;
    numDataBacktracks = 0;
    suffixPerChoiceDepthStats.clear();
    suffixChoices.clear();
  }

  public void addPrefixChoice(int cdepth, Schedule.Choice choice) {
    // TODO: check if we need copy here
    assert (!choice.isBacktrackNonEmpty());
    prefixChoices.put(cdepth, choice);
  }

  public void addPrefixCoverageStats(int cdepth, CoverageStats.CoverageChoiceDepthStats stats) {
    // TODO: check if we need copy here
    prefixPerChoiceDepthStats.put(cdepth, stats);
  }

  public void addSuffixChoice(Schedule.Choice choice) {
    // TODO: check if we need copy here
    suffixChoices.add(choice.getCopy());
    if (choice.isBacktrackNonEmpty()) {
      numBacktracks++;
      if (choice.isDataBacktrackNonEmpty()) {
        numDataBacktracks++;
      }
    }
  }

  public void addSuffixCoverageStats(CoverageStats.CoverageChoiceDepthStats stats) {
    // TODO: check if we need copy here
    suffixPerChoiceDepthStats.add(stats.getCopy());
  }

  public List<Schedule.Choice> getAllChoices() {
    List<Schedule.Choice> result = new ArrayList<>(suffixChoices);
    BacktrackTask task = this;
    int i = backtrackChoiceDepth-1;
    while(i >= 0) {
      Schedule.Choice c = task.getPrefixChoices().get(i);
      if (c == null) {
        assert (!task.isInitialTask());
        task = task.getParentTask();
      } else {
        result.add(0, c);
        i--;
      }
    }
    assert(result.size() == (suffixChoices.size() + backtrackChoiceDepth));
    return result;
  }

  public List<CoverageStats.CoverageChoiceDepthStats> getAllPerChoiceDepthStats() {
    List<CoverageStats.CoverageChoiceDepthStats> result = new ArrayList<>(suffixPerChoiceDepthStats);
    BacktrackTask task = this;
    int i = backtrackChoiceDepth-1;
    while(i >= 0) {
      CoverageStats.CoverageChoiceDepthStats c = task.getPrefixPerChoiceDepthStats().get(i);
      if (c == null) {
        assert (!task.isInitialTask());
        task = task.getParentTask();
      } else {
        result.add(0, c);
        i--;
      }
    }
    assert(result.size() == (suffixPerChoiceDepthStats.size() + backtrackChoiceDepth));
    return result;
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
    if (obj == this) return true;
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
        estimatedCoverage =
            estimatedCoverage.divide(BigDecimal.valueOf(count), 200, RoundingMode.FLOOR);
      }
    }
  }

  /** Set priority of the task with given orchestration mode */
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
    assert (inputCoverage.doubleValue() <= prefixCoverage.doubleValue())
        : String.format(
            "Error in coverage estimation: path coverage (%.5f) should be <= prefix coverage (%.5f)",
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
          assert (parentTask != null);
          for (BacktrackTask t : parentTask.getChildren()) {
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
}
