package psymbolic.runtime.statistics;

import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.Setter;
import lombok.var;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;

/**
 * Represents the search statistics during exploration
 */
public class SearchStats {
    /**
     * Represents the search statistics for one iteration
     */
    @Getter
    public class IterationStats {

        // iteration number
        private int iteration;

        // per depth statistics during this iteration
        private HashMap<Integer, DepthStats> perDepthStats;

        public void addDepthStatistics(int depth, DepthStats stats) {
            perDepthStats.put(depth, stats);
        }

        public IterationStats(int iterationNumber)
        {
            iteration = iterationNumber;
            perDepthStats = new HashMap<>();
        }

        public DepthStats getIterationTotal()
        {
            int maxDepth = 0;
            int totalTransitions = 0;
            int totalMergedTransitions = 0;
            int totalTransitionsExplored = 0;
            for(var entry: perDepthStats.entrySet())
            {
                if(entry.getKey() > maxDepth)
                {
                    maxDepth = entry.getKey();
                    totalTransitions += entry.getValue().numOfTransitions;
                    totalMergedTransitions += entry.getValue().numOfMergedTransitions;
                    totalTransitionsExplored += entry.getValue().numOfTransitionsExplored;
                }
            }

            return new DepthStats(maxDepth, totalTransitions, totalMergedTransitions, totalTransitionsExplored);
        }
    }

    /**
     * Represents the statistics at each depth per iteration
     */
    @AllArgsConstructor
    @Getter @Setter
    public static class DepthStats {
        // depth
        private int depth;

        // number of transitions that can be taken at this depth
        private int numOfTransitions;

        // number of transitions that can be taken at this depth (after merging messages with the same target)
        private int numOfMergedTransitions;

        /*
        number of transitions explored at this depth, this is the number of event handler invocations and takes into
        account the merging of messages to the same target machine
         */
        private int numOfTransitionsExplored;
    }

    // per iteration statistics map
    @Getter
    private List<IterationStats> iterationStats = new ArrayList<>();

    public void addDepthStatistics(int depth, DepthStats depthStats)
    {
        iterationStats.get(iterationStats.size()-1).addDepthStatistics(depth, depthStats);
    }

    public void startNewIteration(int iteration)
    {
        iterationStats.add(new IterationStats(iteration));
    }

    public DepthStats getSearchTotal()
    {
        int maxDepth = 0;
        int totalTransitions = 0;
        int totalMergedTransitions = 0;
        int totalTransitionsExplored = 0;
        for(var entry: iterationStats)
        {
            if(entry.getIterationTotal().getDepth() > maxDepth)
            {
                maxDepth = entry.getIterationTotal().getDepth();
                totalTransitions += entry.getIterationTotal().getNumOfTransitions();
                totalMergedTransitions += entry.getIterationTotal().getNumOfMergedTransitions();
                totalTransitionsExplored += entry.getIterationTotal().getNumOfTransitionsExplored();
            }
        }

        return new DepthStats(maxDepth, totalTransitions, totalMergedTransitions, totalTransitionsExplored);
    }

}
