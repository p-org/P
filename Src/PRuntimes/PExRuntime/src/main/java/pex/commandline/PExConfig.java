package pex.commandline;

import lombok.Getter;
import lombok.Setter;
import pex.runtime.machine.buffer.BufferSemantics;
import pex.runtime.scheduler.explicit.StateCachingMode;
import pex.runtime.scheduler.explicit.StatefulBacktrackingMode;
import pex.runtime.scheduler.explicit.choiceselector.ChoiceSelectorMode;
import pex.runtime.scheduler.explicit.strategy.SearchStrategyMode;

/**
 * Represents the configuration for PEx runtime.
 */
@Getter
public class PExConfig {
    // default name of the test driver
    final String testDriverDefault = "DefaultImpl";
    // name of the test driver
    @Setter
    String testDriver = testDriverDefault;
    // name of the project
    @Setter
    String projectName = "default";
    // name of the output folder
    @Setter
    String outputFolder = "output";
    // number of threads
    @Setter
    int numThreads = 1;
    // time limit in seconds (0 means infinite)
    @Setter
    double timeLimit = 0;
    // memory limit in megabytes (0 means infinite)
    @Setter
    double memLimit = (Runtime.getRuntime().maxMemory() / 2.0 / 1024.0 / 1024.0);
    // level of verbosity for the logging
    @Setter
    int verbosity = 0;
    // max number of schedules bound provided by the user
    @Setter
    int maxSchedules = 1;
    // max steps/depth bound provided by the user
    @Setter
    int maxStepBound = 10000;
    // fail on reaching the maximum scheduling step bound
    @Setter
    boolean failOnMaxStepBound = false;
    // random seed
    @Setter
    long randomSeed = System.currentTimeMillis();
    // name of the replay file
    @Setter
    String replayFile = "";
    // max number of logs (i.e., internal steps) within a single schedule step
    @Setter
    int maxStepLogBound = 1000;
    // buffer semantics
    @Setter
    BufferSemantics bufferSemantics = BufferSemantics.SenderQueue;
    // state caching mode
    @Setter
    StateCachingMode stateCachingMode = StateCachingMode.Murmur3_128;
    // stateful backtracking mode
    @Setter
    StatefulBacktrackingMode statefulBacktrackingMode = StatefulBacktrackingMode.IntraTask;
    // search strategy mode
    @Setter
    SearchStrategyMode searchStrategyMode = SearchStrategyMode.Random;
    // choice selector mode
    @Setter
    ChoiceSelectorMode choiceSelectorMode = ChoiceSelectorMode.Random;
    // max number of schedules per search task
    @Setter
    int maxSchedulesPerTask = 500;
    //max number of children search tasks
    @Setter
    int maxChildrenPerTask = 2;
    //max number of choices per choose(.) operation per call
    @Setter
    int maxChoiceBoundPerCall = 10000;
    //max number of choices per choose(.) operation in total
    @Setter
    int maxChoiceBoundTotal = 0;
}
