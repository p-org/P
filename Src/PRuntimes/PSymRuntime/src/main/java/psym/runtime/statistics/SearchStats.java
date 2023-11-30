package psym.runtime.statistics;

import java.io.Serializable;
import java.util.HashMap;
import java.util.Map;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.Setter;

/** Represents the search statistics during exploration */
public class SearchStats implements Serializable {
  // per schedule statistics map
  @Getter private final HashMap<Integer, IterationStats> iterationStats = new HashMap<>();
  @Setter private int current_iter = 1;
  private int lastCompletedIteration = 1;

  public void addDepthStatistics(int depth, DepthStats depthStats) {
    if (iterationStats.containsKey(current_iter)) {
      iterationStats.get(current_iter).addDepthStatistics(depth, depthStats);
    }
  }

  public void setIterationCompleted() {
    if (iterationStats.containsKey(current_iter)) {
      iterationStats.get(current_iter).setCompleted(true);
      lastCompletedIteration = current_iter;
    }
  }

  public void startNewIteration(int schedule, int backtrack) {
    iterationStats.put(schedule, new IterationStats(schedule, backtrack));
    current_iter = schedule;
  }

  public TotalStats getSearchTotal() {
    boolean completed = true;

    int maxDepth = 0;
    int totalStates = 0;
    int totalTransitions = 0;
    int totalMergedTransitions = 0;
    int totalTransitionsExplored = 0;

    for (int i = 1; i <= lastCompletedIteration; i++) {
      if (!iterationStats.containsKey(i)) continue;
      IterationStats entry = iterationStats.get(i);
      if (!entry.isCompleted()) {
        completed = false;
      }

      DepthStats d = entry.getIterationNewTotal();
      if (d.getDepth() == 0) continue;
      if (d.getDepth() > maxDepth) {
        maxDepth = d.getDepth();
      }
      totalStates += d.numOfStates;
      totalTransitions += d.getNumOfTransitions();
      totalMergedTransitions += d.getNumOfMergedTransitions();
      totalTransitionsExplored += d.getNumOfTransitionsExplored();
    }
    DepthStats totalDepthStats =
        new DepthStats(
            maxDepth,
            totalStates,
            totalTransitions,
            totalMergedTransitions,
            totalTransitionsExplored);
    return new TotalStats(totalDepthStats, completed);
  }

  public void reset_stats() {
    iterationStats.clear();
  }

  /** Represents the statistics at each depth per schedule */
  @AllArgsConstructor
  @Getter
  @Setter
  public static class DepthStats implements Serializable {
    // depth
    private int depth;

    // number of states that are explored at this depth
    private int numOfStates;

    // number of transitions that can be taken at this depth
    private int numOfTransitions;

    // number of transitions that can be taken at this depth (after merging messages with the same
    // target)
    private int numOfMergedTransitions;

    /*
    number of transitions explored at this depth, this is the number of event handler invocations and takes into
    account the merging of messages to the same target machine
     */
    private int numOfTransitionsExplored;
  }

  /** Represents the overall search statistics */
  @AllArgsConstructor
  @Getter
  public static class TotalStats {
    // total depth stats
    private DepthStats depthStats;

    // has the search completed
    private boolean completed;
  }

  /** Represents the search statistics for one schedule */
  @Getter
  public static class IterationStats implements Serializable {

    // schedule number
    private final int schedule;

    // schedule number
    @Getter private final int startDepth;
    // per depth statistics during this schedule
    private final HashMap<Integer, DepthStats> perDepthStats;
    // has the schedule completed for any depth
    @Getter @Setter private boolean completed;

    public IterationStats(int iterationNumber, int startDepth) {
      schedule = iterationNumber;
      this.startDepth = startDepth;
      perDepthStats = new HashMap<>();
      completed = false;
    }

    public void addDepthStatistics(int depth, DepthStats stats) {
      perDepthStats.put(depth, stats);
    }

    public DepthStats getIterationTotal() {
      int maxDepth = 0;
      int totalStates = 0;
      int totalTransitions = 0;
      int totalMergedTransitions = 0;
      int totalTransitionsExplored = 0;
      for (Map.Entry<Integer, DepthStats> entry : perDepthStats.entrySet()) {
        if (entry.getKey() > maxDepth) {
          maxDepth = entry.getKey();
        }
        totalStates += entry.getValue().numOfStates;
        totalTransitions += entry.getValue().numOfTransitions;
        totalMergedTransitions += entry.getValue().numOfMergedTransitions;
        totalTransitionsExplored += entry.getValue().numOfTransitionsExplored;
      }

      return new DepthStats(
          maxDepth,
          totalStates,
          totalTransitions,
          totalMergedTransitions,
          totalTransitionsExplored);
    }

    public DepthStats getIterationNewTotal() {
      int maxDepth = 0;
      int totalStates = 0;
      int totalTransitions = 0;
      int totalMergedTransitions = 0;
      int totalTransitionsExplored = 0;
      for (Map.Entry<Integer, DepthStats> entry : perDepthStats.entrySet()) {
        if (entry.getKey() >= startDepth) {
          if (entry.getKey() > maxDepth) {
            maxDepth = entry.getKey();
          }
          totalStates += entry.getValue().numOfStates;
          totalTransitions += entry.getValue().numOfTransitions;
          totalMergedTransitions += entry.getValue().numOfMergedTransitions;
          totalTransitionsExplored += entry.getValue().numOfTransitionsExplored;
        }
      }

      return new DepthStats(
          maxDepth,
          totalStates,
          totalTransitions,
          totalMergedTransitions,
          totalTransitionsExplored);
    }
  }
}
