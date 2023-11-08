package psym.runtime.statistics;

import java.io.Serializable;
import java.math.BigDecimal;
import java.math.RoundingMode;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Collections;
import java.util.List;
import lombok.Getter;
import lombok.Setter;
import psym.runtime.PSymGlobal;
import psym.runtime.logger.CoverageWriter;
import psym.runtime.logger.StatWriter;
import psym.runtime.scheduler.search.choiceorchestration.ChoiceLearningRewardMode;
import psym.runtime.scheduler.search.choiceorchestration.ChoiceQTable;

/** Class to track all coverage statistics */
public class CoverageStats implements Serializable {
  /** Track of number of choices explored versus remaining in aggregate at each depth */
  private final List<CoverageDepthStats> perDepthStats = new ArrayList<>();
  /** Estimated state-space coverage */
  private BigDecimal estimatedCoverage = new BigDecimal(0);

  @Getter @Setter
  /** Track of path coverage during depth-first iterative search */
  private List<CoverageChoiceDepthStats> perChoiceDepthStats = new ArrayList<>();

  public CoverageStats() {}

  public static BigDecimal getMaxCoverage() {
    return BigDecimal.ONE;
  }

  public static String getMaxCoverageGoal() {
    return "∞ 9s";
  }

  public int getNumScheduleChoicesExplored() {
    int res = 0;
    for (CoverageDepthStats val : perDepthStats) {
      res += val.numScheduleExplored;
    }
    return res;
  }

  public int getNumDataChoicesExplored() {
    int res = 0;
    for (CoverageDepthStats val : perDepthStats) {
      res += val.numDataExplored;
    }
    return res;
  }

  public int getNumScheduleChoicesRemaining() {
    int res = 0;
    for (CoverageDepthStats val : perDepthStats) {
      res += val.numScheduleRemaining;
    }
    return res;
  }

  public int getNumDataChoicesRemaining() {
    int res = 0;
    for (CoverageDepthStats val : perDepthStats) {
      res += val.numDataRemaining;
    }
    return res;
  }

  /**
   * Update running path coverage at a given choice depth
   *
   * @param choiceDepth Choice depth to update at
   * @param numExplored Number of choices explored in current schedule at choiceDepth
   * @param numRemaining Number of choices remaining in current schedule at choiceDepth
   * @param isNewChoice Whether or not this is a new choice
   */
  public void updatePathCoverage(
      int choiceDepth,
      int numExplored,
      int numRemaining,
      boolean isNewChoice,
      ChoiceQTable.ChoiceQTableKey chosenActions) {
    CoverageChoiceDepthStats prefix;
    if (choiceDepth == 0) prefix = new CoverageChoiceDepthStats();
    else prefix = perChoiceDepthStats.get(choiceDepth - 1);
    perChoiceDepthStats
        .get(choiceDepth)
        .update(prefix, numExplored, numRemaining, isNewChoice, chosenActions);
  }

  /**
   * Update all coverage statistics at the current step
   *
   * @param depth Scheduler depth/step
   * @param choiceDepth Current choice depth
   * @param numExplored Number of choices explored in current schedule at choiceDepth
   * @param numRemaining Number of choices remaining in current schedule at choiceDepth
   * @param isData Is true if the choice is a data choice
   * @param isNewChoice Whether or not this is a new choice
   */
  public void updateDepthCoverage(
      int depth,
      int choiceDepth,
      int numExplored,
      int numRemaining,
      boolean isData,
      boolean isNewChoice,
      ChoiceQTable.ChoiceQTableKey chosenActions) {
    // TODO: add synchronized to avoid race conditions when developing multi-threaded version
    while (depth >= perDepthStats.size()) {
      perDepthStats.add(new CoverageDepthStats());
    }
    while (choiceDepth >= perChoiceDepthStats.size()) {
      perChoiceDepthStats.add(new CoverageChoiceDepthStats());
    }

    if (isData) {
      perDepthStats.get(depth).numDataExplored += numExplored;
      if (isNewChoice) {
        perDepthStats.get(depth).numDataRemaining += numRemaining;
      } else {
        perDepthStats.get(depth).numDataRemaining -= numExplored;
      }
    } else {
      perDepthStats.get(depth).numScheduleExplored += numExplored;
      if (isNewChoice) {
        perDepthStats.get(depth).numScheduleRemaining += numRemaining;
      } else {
        perDepthStats.get(depth).numScheduleRemaining -= numExplored;
      }
    }
    updatePathCoverage(choiceDepth, numExplored, numRemaining, isNewChoice, chosenActions);
  }

  /**
   * Increment path coverage after a schedule has ended
   *
   * @param choiceDepth Highest choice depth at which the last schedule ended
   */
  public void updateIterationCoverage(
      int choiceDepth, int startDepth, ChoiceLearningRewardMode rewardMode) {
    BigDecimal iterationCoverage = getPathCoverageAtDepth(choiceDepth);
    estimatedCoverage = estimatedCoverage.add(iterationCoverage);
    //        assert (estimatedCoverage.compareTo(BigDecimal.ONE) <= 0): "Error in path coverage
    // estimation";
    if (rewardMode != ChoiceLearningRewardMode.None) {
      for (int i = startDepth; i <= choiceDepth; i++) {
        CoverageChoiceDepthStats stats = perChoiceDepthStats.get(i);
        if (stats != null) {
          PSymGlobal.getChoiceLearningStats()
              .rewardIteration(
                  stats.getStateActions(), iterationCoverage.doubleValue(), rewardMode);
        }
      }
    }
  }

  /** Reset coverage statistics after a resumed run */
  public void resetCoverage() {
    estimatedCoverage = new BigDecimal(0);
    perDepthStats.clear();
  }

  /** Reset running path coverage at a given choice depth */
  public void resetPathCoverage(int choiceDepth) {
    if (choiceDepth < perChoiceDepthStats.size()) {
      perChoiceDepthStats.get(choiceDepth).reset();
    }
  }

  public String getCoverageGoalAchieved() {
    String coverageString = String.format("%.22f", getEstimatedCoverage(22));
    String coverageGoal = "?";
    if (coverageString.startsWith("1.")) {
      return getMaxCoverageGoal();
    } else if (coverageString.startsWith("0.")) {
      int numNines = 0;
      for (int i = 2; i < coverageString.length(); i++) {
        if (coverageString.charAt(i) != '9') {
          break;
        }
        numNines++;
      }
      coverageGoal = String.format("%d 9s", numNines);
    }
    return coverageGoal;
  }

  public BigDecimal getEstimatedCoverage(int scale) {
    return estimatedCoverage.setScale(scale, RoundingMode.FLOOR);
  }

  /**
   * Get path coverage of a schedule after a schedule has ended
   *
   * @param choiceDepth Highest choice depth at which the last schedule ended
   */
  public BigDecimal getPathCoverageAtDepth(int choiceDepth) {
    assert (choiceDepth < perChoiceDepthStats.size());
    return perChoiceDepthStats.get(choiceDepth).pathCoverage;
  }

  /**
   * Prints a coverage report in coverage log file based on number of choices explored versus
   * remaining at each depth
   */
  public void logPerDepthCoverage() {
    for (int d = 0; d < perDepthStats.size(); d++) {
      CoverageDepthStats val = perDepthStats.get(d);
      if (val.isNotEmpty()) CoverageWriter.log(d, val);
    }
  }

  /** Prints a coverage report based on number of choices explored versus remaining at each depth */
  public void reportChoiceCoverage() {
    CoverageWriter.info("-----------------");
    CoverageWriter.info("Coverage Report::");
    CoverageWriter.info("-----------------");
    CoverageWriter.info(
        String.format(
            "  Covered choices:   %5s scheduling, %5s data",
            getNumScheduleChoicesExplored(), getNumDataChoicesExplored()));
    CoverageWriter.info(
        String.format(
            "  Remaining choices: %5s scheduling, %5s data",
            getNumScheduleChoicesRemaining(), getNumDataChoicesRemaining()));

    String s = "";
    CoverageWriter.info("\t-------------------------------------");
    s += "\t  Depth  ";
    s += "  Covered        Remaining";
    s += String.format("\n\t%5s  %5s   %5s  ", "", "sch", "data");
    s += String.format(" %5s   %5s ", "sch", "data");
    CoverageWriter.info(s);
    CoverageWriter.info("\t-------------------------------------");
    for (int d = 0; d < perDepthStats.size(); d++) {
      CoverageDepthStats val = perDepthStats.get(d);
      if (val.isNotEmpty()) {
        s = "";
        s += String.format("\t%5s ", d);
        s +=
            String.format(
                " %5s   %5s  ",
                (val.numScheduleExplored == 0 ? "" : val.numScheduleExplored),
                (val.numDataExplored == 0 ? "" : val.numDataExplored));
        s +=
            String.format(
                " %5s   %5s ",
                (val.numScheduleRemaining == 0 ? "" : val.numScheduleRemaining),
                (val.numDataRemaining == 0 ? "" : val.numDataRemaining));
        CoverageWriter.info(s);
      }
    }

    // print schedule statistics
    StatWriter.log(
        "#-choices-covered",
        String.format(
            "%d scheduling, %d data",
            getNumScheduleChoicesExplored(), getNumDataChoicesExplored()));
    StatWriter.log(
        "#-choices-remaining",
        String.format(
            "%d scheduling, %d data",
            getNumScheduleChoicesRemaining(), getNumDataChoicesRemaining()));
  }

  /**
   * Estimates a coverage percentage based on number of choices explored versus remaining at each
   * depth Estimation is done assuming a perfect choice tree (no sharing of states)
   */
  @Deprecated
  private BigDecimal computeEstimatedCoverage() {
    if (perDepthStats.size() == 0) {
      return new BigDecimal(0);
    }

    BigDecimal[] depthMultiplier = new BigDecimal[perDepthStats.size()];
    for (int d = perDepthStats.size() - 1; d >= 0; d--) {
      CoverageDepthStats val = perDepthStats.get(d);
      int numScheduleExplored = val.numScheduleExplored;
      int numDataExplored = val.numDataExplored;
      int numExplored =
          Collections.max(
              Arrays.asList(
                  1, numScheduleExplored, numDataExplored, numScheduleExplored * numDataExplored));
      depthMultiplier[d] = BigDecimal.valueOf(numExplored);
      if (d != (perDepthStats.size() - 1))
        depthMultiplier[d] = depthMultiplier[d].add(depthMultiplier[d + 1]);
    }

    BigDecimal[] perDepthRemaining = new BigDecimal[perDepthStats.size()];
    for (int d = perDepthStats.size() - 1; d >= 0; d--) {
      CoverageDepthStats val = perDepthStats.get(d);
      int numScheduleExplored = val.numScheduleExplored;
      int numDataExplored = val.numDataExplored;
      int numScheduleRemaining = val.numScheduleRemaining;
      int numDataRemaining = val.numDataRemaining;
      int numExplored =
          Collections.max(
              Arrays.asList(
                  1, numScheduleExplored, numDataExplored, numScheduleExplored * numDataExplored));
      int numRemaining =
          Collections.max(
              Arrays.asList(
                  numScheduleRemaining, numDataRemaining, numScheduleRemaining * numDataRemaining));
      perDepthRemaining[d] =
          depthMultiplier[d]
              .multiply(BigDecimal.valueOf(numRemaining))
              .divide(BigDecimal.valueOf(numExplored), 2, RoundingMode.CEILING);
    }

    BigDecimal estimatedExplored = depthMultiplier[0];
    BigDecimal estimatedRemaining = BigDecimal.valueOf(0);
    for (int d = perDepthStats.size() - 1; d >= 0; d--) {
      estimatedRemaining = estimatedRemaining.add(perDepthRemaining[d]);
    }

    return estimatedExplored
        .multiply(BigDecimal.valueOf(100.0))
        .divide(estimatedExplored.add(estimatedRemaining), 2, RoundingMode.FLOOR);
  }

  @Getter
  public static class CoverageDepthStats implements Serializable {
    int numScheduleExplored;
    int numDataExplored;
    int numScheduleRemaining;
    int numDataRemaining;

    CoverageDepthStats() {
      numScheduleExplored = 0;
      numDataExplored = 0;
      numScheduleRemaining = 0;
      numDataRemaining = 0;
    }

    boolean isNotEmpty() {
      return (numScheduleExplored + numDataExplored + numScheduleRemaining + numDataRemaining) != 0;
    }
  }

  public static class CoverageChoiceDepthStats implements Serializable {
    BigDecimal pathCoverage;
    int numTotal;
    @Getter ChoiceQTable.ChoiceQTableKey stateActions;

    CoverageChoiceDepthStats() {
      this(new BigDecimal(1), 0, new ChoiceQTable.ChoiceQTableKey());
    }

    private CoverageChoiceDepthStats(BigDecimal inputPathCoverage, int inputNumTotal) {
      this(inputPathCoverage, inputNumTotal, new ChoiceQTable.ChoiceQTableKey());
    }

    private CoverageChoiceDepthStats(
        BigDecimal inputPathCoverage,
        int inputNumTotal,
        ChoiceQTable.ChoiceQTableKey inputStateActions) {
      this.pathCoverage = inputPathCoverage;
      this.numTotal = inputNumTotal;
      this.stateActions = inputStateActions;
    }

    void update(
        CoverageChoiceDepthStats prefix,
        int numExplored,
        int numRemaining,
        boolean isNewChoice,
        ChoiceQTable.ChoiceQTableKey chosenActions) {
      pathCoverage = prefix.pathCoverage;
      if (isNewChoice) {
        assert (numRemaining >= 0);
        numTotal = numExplored + numRemaining;
      }
      if (numTotal != 0) {
        assert (numExplored <= numTotal);
        pathCoverage =
            prefix.pathCoverage.multiply(
                BigDecimal.valueOf(numExplored)
                    .divide(BigDecimal.valueOf(numTotal), 20, RoundingMode.FLOOR));
      }
      this.stateActions = chosenActions;
    }

    public void reset() {
      pathCoverage = new BigDecimal(1);
      numTotal = 0;
      if (stateActions != null) {
        stateActions.clear();
      }
    }

    public CoverageChoiceDepthStats getCopy() {
      return new CoverageChoiceDepthStats(this.pathCoverage, this.numTotal);
    }
  }
}
