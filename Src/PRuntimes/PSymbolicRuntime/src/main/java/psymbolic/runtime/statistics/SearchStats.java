package psymbolic.runtime.statistics;

import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.Setter;

import java.io.Serializable;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

/**
 * Represents the search statistics during exploration
 */
public class SearchStats implements Serializable {
    /**
     * Represents the search statistics for one iteration
     */
    @Getter
    public class IterationStats implements Serializable {

        // iteration number
        private int iteration;

        // iteration number
        @Getter
        private int startDepth;

        // number of backtracks remaining
        @Getter @Setter
        private int numBacktracks;

        // has the iteration completed for any depth
        @Getter @Setter
        private boolean completed;

        // per depth statistics during this iteration
        private HashMap<Integer, DepthStats> perDepthStats;

        public void addDepthStatistics(int depth, DepthStats stats) {
            perDepthStats.put(depth, stats);
        }

        public IterationStats(int iterationNumber, int startDepth)
        {
            iteration = iterationNumber;
            this.startDepth = startDepth;
            perDepthStats = new HashMap<>();
            numBacktracks = 0;
            completed = false;
        }

        public DepthStats getIterationTotal()
        {
            int maxDepth = 0;
            int totalStates = 0;
            int totalTransitions = 0;
            int totalMergedTransitions = 0;
            int totalTransitionsExplored = 0;
            for(Map.Entry<Integer, DepthStats> entry: perDepthStats.entrySet())
            {
                if (entry.getKey() > maxDepth) {
                    maxDepth = entry.getKey();
                }
                totalStates += entry.getValue().numOfStates;
                totalTransitions += entry.getValue().numOfTransitions;
                totalMergedTransitions += entry.getValue().numOfMergedTransitions;
                totalTransitionsExplored += entry.getValue().numOfTransitionsExplored;
            }

            return new DepthStats(maxDepth, totalStates, totalTransitions, totalMergedTransitions, totalTransitionsExplored);
        }

        public DepthStats getIterationNewTotal()
        {
            int maxDepth = 0;
            int totalStates = 0;
            int totalTransitions = 0;
            int totalMergedTransitions = 0;
            int totalTransitionsExplored = 0;
            for(Map.Entry<Integer, DepthStats> entry: perDepthStats.entrySet())
            {
                if(entry.getKey() >= startDepth) {
                    if (entry.getKey() > maxDepth) {
                        maxDepth = entry.getKey();
                    }
                    totalStates += entry.getValue().numOfStates;
                    totalTransitions += entry.getValue().numOfTransitions;
                    totalMergedTransitions += entry.getValue().numOfMergedTransitions;
                    totalTransitionsExplored += entry.getValue().numOfTransitionsExplored;
                }
            }

            return new DepthStats(maxDepth, totalStates, totalTransitions, totalMergedTransitions, totalTransitionsExplored);
        }
    }

    /**
     * Represents the statistics at each depth per iteration
     */
    @AllArgsConstructor
    @Getter @Setter
    public static class DepthStats implements Serializable {
        // depth
        private int depth;

        // number of states that are explored at this depth
        private int numOfStates;

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
        if (!iterationStats.isEmpty()) {
            iterationStats.get(iterationStats.size() - 1).addDepthStatistics(depth, depthStats);
        }
    }

    public int getIterationBacktracks()
    {
        return iterationStats.get(iterationStats.size()-1).getNumBacktracks();
    }

    public void setIterationBacktracks(int numBacktracks)
    {
        if (!iterationStats.isEmpty()) {
            iterationStats.get(iterationStats.size() - 1).setNumBacktracks(numBacktracks);
        }
    }

    public void setIterationCompleted()
    {
        iterationStats.get(iterationStats.size()-1).setCompleted(true);
    }

    public void startNewIteration(int iteration, int backtrack)
    {
        iterationStats.add(new IterationStats(iteration, backtrack));
    }


    /**
     * Represents the overall search statistics
     */
    @AllArgsConstructor
    @Getter
    public static class TotalStats {
        // total depth stats
        private DepthStats depthStats;

        // has the search completed
        private boolean completed;

        // number of backtracks remaining
        private int numBacktracks;
    }

    public TotalStats getSearchTotal()
    {
        boolean completed = true;

        int maxDepth = 0;
        int totalStates = 0;
        int totalTransitions = 0;
        int totalMergedTransitions = 0;
        int totalTransitionsExplored = 0;
        for(IterationStats entry: iterationStats)
        {
            DepthStats d = entry.getIterationNewTotal();
            if (d.getDepth() > maxDepth) {
                maxDepth = d.getDepth();
            }
            totalStates += d.numOfStates;
            totalTransitions += d.getNumOfTransitions();
            totalMergedTransitions += d.getNumOfMergedTransitions();
            totalTransitionsExplored += d.getNumOfTransitionsExplored();

            if (!entry.isCompleted()) {
                completed = false;
            }
        }
        DepthStats totalDepthStats = new DepthStats(maxDepth, totalStates, totalTransitions, totalMergedTransitions, totalTransitionsExplored);
        return new TotalStats(totalDepthStats, completed, getIterationBacktracks());
    }

}
