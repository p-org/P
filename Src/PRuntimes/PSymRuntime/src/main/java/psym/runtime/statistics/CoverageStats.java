package psym.runtime.statistics;

import lombok.Getter;
import lombok.Setter;
import psym.commandline.PSymConfiguration;
import psym.runtime.logger.CoverageWriter;
import psym.runtime.logger.StatWriter;
import psym.runtime.scheduler.choiceorchestration.ChoiceQTable;
import psym.utils.GlobalData;

import java.io.Serializable;
import java.math.BigDecimal;
import java.math.RoundingMode;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Collections;
import java.util.List;

/**
 * Class to track all coverage statistics
 */
public class CoverageStats implements Serializable {
    private static final BigDecimal hundred = BigDecimal.valueOf(100);
    /**
     * Estimated state-space coverage
     */
    private BigDecimal estimatedCoverage = new BigDecimal(0);
    /**
     * Track of number of choices explored versus remaining in aggregate at each depth
     */
    private List<CoverageDepthStats> perDepthStats = new ArrayList<>();
    @Getter @Setter
    /**
     * Track of path coverage during depth-first iterative search
     */
    private List<CoverageChoiceDepthStats> perChoiceDepthStats = new ArrayList<>();

    public CoverageStats() {}

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

        boolean isEmpty() {
            return (numScheduleExplored+numDataExplored+numScheduleRemaining+numDataRemaining) == 0;
        }

    }

    public static class CoverageChoiceDepthStats implements Serializable {
        BigDecimal pathCoverage;
        int numTotal;
        @Getter
        ChoiceQTable.ChoiceQTableKey stateActions;

        CoverageChoiceDepthStats() {
            this(new BigDecimal(1), 0, new ChoiceQTable.ChoiceQTableKey());
        }

        private CoverageChoiceDepthStats(BigDecimal inputPathCoverage, int inputNumTotal, ChoiceQTable.ChoiceQTableKey inputStateActions) {
            this.pathCoverage = inputPathCoverage;
            this.numTotal = inputNumTotal;
            this.stateActions = inputStateActions;
        }

        void update(CoverageChoiceDepthStats prefix, int numExplored, int numRemaining, boolean isNewChoice, ChoiceQTable.ChoiceQTableKey chosenActions) {
            pathCoverage = prefix.pathCoverage;
            if (isNewChoice) {
                assert(numRemaining >= 0);
                numTotal = numExplored + numRemaining;
            }
            if (numTotal != 0) {
                assert(numExplored <= numTotal);
                pathCoverage = prefix.pathCoverage.multiply(BigDecimal.valueOf(numExplored).divide(BigDecimal.valueOf(numTotal), 20, RoundingMode.FLOOR));
            }
            this.stateActions = chosenActions;
        }

        public void reset() {
            pathCoverage = new BigDecimal(1);
            numTotal = 0;
            if (stateActions != null){
                stateActions.clear();
            }
        }

        public CoverageChoiceDepthStats getCopy() {
            return new CoverageChoiceDepthStats(this.pathCoverage, this.numTotal, this.stateActions);
        }
    }

    public int getNumScheduleChoicesExplored() {
        int res = 0;
        for (CoverageDepthStats val: perDepthStats) {
            res += val.numScheduleExplored;
        }
        return res;
    }
    public int getNumDataChoicesExplored() {
        int res = 0;
        for (CoverageDepthStats val: perDepthStats) {
            res += val.numDataExplored;
        }
        return res;
    }
    public int getNumScheduleChoicesRemaining() {
        int res = 0;
        for (CoverageDepthStats val: perDepthStats) {
            res += val.numScheduleRemaining;
        }
        return res;
    }
    public int getNumDataChoicesRemaining() {
        int res = 0;
        for (CoverageDepthStats val: perDepthStats) {
            res += val.numDataRemaining;
        }
        return res;
    }

    /**
     * Update running path coverage at a given choice depth
     * @param choiceDepth Choice depth to update at
     * @param numExplored Number of choices explored in current iteration at choiceDepth
     * @param numRemaining Number of choices remaining in current iteration at choiceDepth
     * @param isNewChoice Whether or not this is a new choice
     */
    public void updatePathCoverage(int choiceDepth, int numExplored, int numRemaining, boolean isNewChoice, ChoiceQTable.ChoiceQTableKey chosenActions) {
        CoverageChoiceDepthStats prefix;
        if (choiceDepth == 0)
            prefix = new CoverageChoiceDepthStats();
        else
            prefix = perChoiceDepthStats.get(choiceDepth-1);
        perChoiceDepthStats.get(choiceDepth).update(prefix, numExplored, numRemaining, isNewChoice, chosenActions);
    }

    /**
     * Update all coverage statistics at the current step
     * @param depth Scheduler depth/step
     * @param choiceDepth Current choice depth
     * @param numExplored Number of choices explored in current iteration at choiceDepth
     * @param numRemaining Number of choices remaining in current iteration at choiceDepth
     * @param isData Is true if the choice is a data choice
     * @param isNewChoice Whether or not this is a new choice
     */
    public void updateDepthCoverage(int depth, int choiceDepth, int numExplored, int numRemaining, boolean isData, boolean isNewChoice, ChoiceQTable.ChoiceQTableKey chosenActions) {
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
     * Increment path coverage after an iteration has ended
     * @param choiceDepth Highest choice depth at which the last iteration ended
     */
    public void updateIterationCoverage(int choiceDepth, boolean rewardEnabled) {
        BigDecimal iterationCoverage = getPathCoverageAtDepth(choiceDepth);
        estimatedCoverage = estimatedCoverage.add(iterationCoverage);
        assert (estimatedCoverage.doubleValue() <= 1.0): "Error in path coverage estimation";
        if (rewardEnabled) {
            for (CoverageChoiceDepthStats stats : perChoiceDepthStats) {
                GlobalData.getChoiceLearningStats().rewardIteration(stats.getStateActions(), iterationCoverage);
            }
        }
    }

    /**
     * Reset coverage statistics after a resumed run
     */
    public void resetCoverage() {
        estimatedCoverage = new BigDecimal(0);
        perDepthStats.clear();
    }

    /**
     * Reset running path coverage at a given choice depth
     */
   public void resetPathCoverage(int choiceDepth) {
       if (choiceDepth < perChoiceDepthStats.size()) {
           perChoiceDepthStats.get(choiceDepth).reset();
       }
   }

    /**
     * Return estimated state-space coverage between 0 - 100%
     */
    public BigDecimal getEstimatedCoverage() {
        return getEstimatedCoverage(10);
    }

    public BigDecimal getEstimatedCoverage(int scale) {
        return estimatedCoverage.multiply(hundred).setScale(scale, RoundingMode.FLOOR);
    }

    /**
     * Get path coverage of an interation after an iteration has ended
     * @param choiceDepth Highest choice depth at which the last iteration ended
     */
    public BigDecimal getPathCoverageAtDepth(int choiceDepth) {
        assert(choiceDepth < perChoiceDepthStats.size());
        return perChoiceDepthStats.get(choiceDepth).pathCoverage;
    }

    /**
     * Prints a coverage report in coverage log file based on number of choices explored versus remaining at each depth
     */
    public void logPerDepthCoverage() {
        for (int d = 0; d < perDepthStats.size(); d++) {
            CoverageDepthStats val = perDepthStats.get(d);
            if (!val.isEmpty())
                CoverageWriter.log(d, val);
        }
    }

    /**
     * Prints a coverage report based on number of choices explored versus remaining at each depth
     */
    public void reportChoiceCoverage() {
        CoverageWriter.info("-----------------");
        CoverageWriter.info("Coverage Report::");
        CoverageWriter.info("-----------------");
        CoverageWriter.info(String.format("  Covered choices:   %5s scheduling, %5s data",
                getNumScheduleChoicesExplored(),
                getNumDataChoicesExplored()));
        CoverageWriter.info(String.format("  Remaining choices: %5s scheduling, %5s data",
                getNumScheduleChoicesRemaining(),
                getNumDataChoicesRemaining() ));

        String s = "";
        CoverageWriter.info("\t-------------------------------------");
        s += String.format("\t  Depth  ");
        s += String.format("  Covered        Remaining");
        s += String.format("\n\t%5s  %5s   %5s  ", "", "sch", "data");
        s += String.format(" %5s   %5s ", "sch", "data");
        CoverageWriter.info(s);
        CoverageWriter.info("\t-------------------------------------");
        for (int d = 0; d< perDepthStats.size(); d++) {
            CoverageDepthStats val = perDepthStats.get(d);
            if (!val.isEmpty()) {
                s = "";
                s += String.format("\t%5s ", d);
                s += String.format(" %5s   %5s  ",
                        (val.numScheduleExplored == 0 ? "" : val.numScheduleExplored),
                        (val.numDataExplored == 0 ? "" : val.numDataExplored));
                s += String.format(" %5s   %5s ",
                        (val.numScheduleRemaining == 0 ? "" : val.numScheduleRemaining),
                        (val.numDataRemaining == 0 ? "" : val.numDataRemaining));
                CoverageWriter.info(s);
            }
        }

        // print schedule statistics
        StatWriter.log("#-choices-covered", String.format("%d scheduling, %d data",
                getNumScheduleChoicesExplored(),
                getNumDataChoicesExplored()));
        StatWriter.log("#-choices-remaining", String.format("%d scheduling, %d data",
                getNumScheduleChoicesRemaining(),
                getNumDataChoicesRemaining()));
    }

    /**
     * Estimates a coverage percentage based on number of choices explored versus remaining at each depth
     * Estimation is done assuming a perfect choice tree (no sharing of states)
     */
    @Deprecated
    private BigDecimal computeEstimatedCoverage() {
        if (perDepthStats.size() == 0) {
            return new BigDecimal(0);
        }

        BigDecimal depthMultiplier[] = new BigDecimal[perDepthStats.size()];
        for (int d = perDepthStats.size()-1; d>=0; d--) {
            CoverageDepthStats val = perDepthStats.get(d);
            int numScheduleExplored = val.numScheduleExplored;
            int numDataExplored = val.numDataExplored;
            int numExplored = Collections.max(Arrays.asList(1, numScheduleExplored, numDataExplored, numScheduleExplored*numDataExplored));
            depthMultiplier[d] = BigDecimal.valueOf(numExplored);
            if (d != (perDepthStats.size()-1))
                depthMultiplier[d] = depthMultiplier[d].add(depthMultiplier[d+1]);
        }

        BigDecimal perDepthRemaining[] = new BigDecimal[perDepthStats.size()];
        for (int d = perDepthStats.size()-1; d>=0; d--) {
            CoverageDepthStats val = perDepthStats.get(d);
            int numScheduleExplored = val.numScheduleExplored;
            int numDataExplored = val.numDataExplored;
            int numScheduleRemaining = val.numScheduleRemaining;
            int numDataRemaining = val.numDataRemaining;
            int numExplored = Collections.max(Arrays.asList(1, numScheduleExplored, numDataExplored, numScheduleExplored*numDataExplored));
            int numRemaining = Collections.max(Arrays.asList(numScheduleRemaining, numDataRemaining, numScheduleRemaining*numDataRemaining));
            perDepthRemaining[d] = depthMultiplier[d].multiply(BigDecimal.valueOf(numRemaining)).divide(BigDecimal.valueOf(numExplored), 2, RoundingMode.CEILING);
        }

        BigDecimal estimatedExplored = depthMultiplier[0];
        BigDecimal estimatedRemaining = BigDecimal.valueOf(0);
        for (int d = perDepthStats.size()-1; d>=0; d--) {
            estimatedRemaining = estimatedRemaining.add(perDepthRemaining[d]);
        }

        BigDecimal estimatedCoverage = estimatedExplored.multiply(BigDecimal.valueOf(100.0)).divide(estimatedExplored.add(estimatedRemaining), 2, RoundingMode.FLOOR);
        return estimatedCoverage;
    }

}
