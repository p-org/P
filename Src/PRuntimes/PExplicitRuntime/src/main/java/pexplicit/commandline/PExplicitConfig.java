package pexplicit.commandline;

import lombok.Getter;
import lombok.Setter;
import pexplicit.runtime.machine.buffer.BufferSemantics;
import pexplicit.runtime.scheduler.explicit.StateCachingMode;

/**
 * Represents the configuration for PExplicit runtime.
 */
@Getter
public class PExplicitConfig {
    // default name of the test driver
    final String testDriverDefault = "DefaultImpl";
    // max internal steps before throwing an exception
    final int maxInternalSteps = 100;
    // name of the test driver
    @Setter
    String testDriver = testDriverDefault;
    // name of the project
    @Setter
    String projectName = "default";
    // name of the output folder
    @Setter
    String outputFolder = "output";
    // time limit in seconds (0 means infinite)
    @Setter
    double timeLimit = 0;
    // memory limit in megabytes (0 means infinite)
    @Setter
    double memLimit = (Runtime.getRuntime().maxMemory() / 2.0 / 1024.0 / 1024.0);
    // level of verbosity for the logging
    @Setter
    int verbosity = 0;
    // strategy of exploration
    @Setter
    String strategy = "dfs";
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
    // max number of logs (i.e., internal steps) within a single schedule step
    @Setter
    int maxStepLogBound = 1000;
    // buffer semantics
    @Setter
    BufferSemantics bufferSemantics = BufferSemantics.SenderQueue;
    // state caching mode
    @Setter
    StateCachingMode stateCachingMode = StateCachingMode.Fingerprint;
    // use stateful backtracking
    @Setter
    boolean statefulBacktrackEnabled = true;

    public void setToDfs() {
        this.setStrategy("dfs");
    }

    public void setToReplay() {
        this.setStrategy("replay");
    }
}
