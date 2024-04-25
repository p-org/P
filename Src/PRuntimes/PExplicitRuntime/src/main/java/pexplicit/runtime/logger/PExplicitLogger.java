package pexplicit.runtime.logger;

import lombok.Setter;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;
import org.apache.logging.log4j.core.LoggerContext;
import org.apache.logging.log4j.core.appender.ConsoleAppender;
import org.apache.logging.log4j.core.layout.PatternLayout;
import pexplicit.runtime.PExplicitGlobal;
import pexplicit.runtime.STATUS;
import pexplicit.runtime.machine.PMachine;
import pexplicit.runtime.machine.PMonitor;
import pexplicit.runtime.machine.State;
import pexplicit.runtime.machine.events.PContinuation;
import pexplicit.runtime.scheduler.explicit.ExplicitSearchScheduler;
import pexplicit.runtime.scheduler.explicit.SearchStatistics;
import pexplicit.runtime.scheduler.explicit.StateCachingMode;
import pexplicit.runtime.scheduler.explicit.strategy.SearchTask;
import pexplicit.runtime.scheduler.replay.ReplayScheduler;
import pexplicit.utils.monitor.MemoryMonitor;
import pexplicit.values.PEvent;
import pexplicit.values.PMessage;
import pexplicit.values.PValue;

import java.io.PrintWriter;
import java.io.StringWriter;
import java.util.List;

/**
 * Represents the main PExplicit logger
 */
public class PExplicitLogger {
    static Logger log = null;
    static LoggerContext context = null;
    @Setter
    static int verbosity;

    /**
     * Initializes the logger with the given verbosity level.
     *
     * @param verb Verbosity level
     */
    public static void Initialize(int verb) {
        verbosity = verb;
        log = Log4JConfig.getContext().getLogger(PExplicitLogger.class.getName());
        org.apache.logging.log4j.core.Logger coreLogger =
                (org.apache.logging.log4j.core.Logger) LogManager.getLogger(PExplicitLogger.class.getName());
        context = coreLogger.getContext();

        PatternLayout layout = Log4JConfig.getPatternLayout();
        ConsoleAppender consoleAppender = ConsoleAppender.createDefaultAppenderForLayout(layout);
        consoleAppender.start();

        context.getConfiguration().addLoggerAppender(coreLogger, consoleAppender);

        // initialize all loggers and writers
        StatWriter.Initialize();
        ScratchLogger.Initialize();
        ScheduleWriter.Initialize();
        TextWriter.Initialize();
    }

    public static void logInfo(String message) {
        log.info(message);
    }

    /**
     * Logs the given message based on the current verbosity level.
     *
     * @param message Message to print
     */
    public static void logVerbose(String message) {
        if (verbosity > 3) {
            log.info(message);
        }
    }

    /**
     * Logs message at the end of a run.
     *
     * @param scheduler Explicit state search scheduler
     * @param timeSpent Time spent in seconds
     */
    public static void logEndOfRun(ExplicitSearchScheduler scheduler, long timeSpent) {
        if (verbosity == 0) {
            log.info("");
        }
        log.info("--------------------");
        log.info("... Checking statistics:");
        if (PExplicitGlobal.getStatus() == STATUS.BUG_FOUND) {
            log.info("..... Found 1 bug.");
        } else {
            log.info("..... Found 0 bugs.");
        }
        log.info("... Scheduling statistics:");
        if (PExplicitGlobal.getConfig().getStateCachingMode() != StateCachingMode.None) {
            log.info(String.format("..... Explored %d distinct states", SearchStatistics.totalDistinctStates));
        }
        log.info(String.format("..... Explored %d distinct schedules", SearchStatistics.iteration));
        log.info(String.format("..... Finished %d search tasks (%d pending)",
                scheduler.getSearchStrategy().getFinishedTasks().size(), scheduler.getSearchStrategy().getPendingTasks().size()));
        log.info(String.format("..... Number of steps explored: %d (min), %d (avg), %d (max).",
                SearchStatistics.minSteps, (SearchStatistics.totalSteps / SearchStatistics.iteration), SearchStatistics.maxSteps));
        log.info(String.format("... Elapsed %d seconds and used %.1f GB", timeSpent, MemoryMonitor.getMaxMemSpent() / 1000.0));
        log.info(String.format(".. Result: " + PExplicitGlobal.getResult()));
        log.info(". Done");
    }

    /**
     * Print error trace
     *
     * @param e Exception object
     */
    public static void logStackTrace(Exception e) {
        StringWriter sw = new StringWriter();
        PrintWriter pw = new PrintWriter(sw);
        e.printStackTrace(pw);
        log.info("--------------------");
        log.info(sw.toString());
    }

    /**
     * Log at the start of a search task
     *
     * @param task Search task
     */
    public static void logStartTask(SearchTask task) {
        if (verbosity > 0) {
            log.info("=====================");
            log.info(String.format("Starting %s", task.toStringDetailed()));
        }
    }

    /**
     * Log at the end of a search task
     *
     * @param task Search task
     */
    public static void logEndTask(SearchTask task, int numSchedules) {
        if (verbosity > 0) {
            log.info(String.format("  Finished %s after exploring %d schedules", task, numSchedules));
        }
    }

    public static void logNewTasks(List<SearchTask> tasks) {
        if (verbosity > 0) {
            log.info(String.format("    Added %d new tasks", tasks.size()));
        }
        if (verbosity > 1) {
            for (SearchTask task : tasks) {
                log.info(String.format("      %s", task.toStringDetailed()));
            }
        }
    }

    /**
     * Log at the start of an iteration
     *
     * @param iter Iteration number
     * @param step Starting step number
     */
    public static void logStartIteration(SearchTask task, int iter, int step) {
        if (verbosity > 0) {
            log.info("--------------------");
            log.info(String.format("[%s] Starting schedule %s from step %s", task, iter, step));
        }
    }

    public static void logStartStep(int step, PMachine sender, PMessage msg) {
        if (verbosity > 0) {
            log.info(String.format(
                    "  Step %d: %s sent %s to %s",
                    step, sender, msg.getEvent(), msg.getTarget()));
            if (verbosity > 5) {
                log.info(String.format("    payload: %s", msg.getPayload()));
            }
        }
    }

    /**
     * Log at the end of an iteration
     *
     * @param step Step number
     */
    public static void logFinishedIteration(int step) {
        if (verbosity > 0) {
            log.info(String.format("  Schedule finished at step %d", step));
        }
    }

    /**
     * Log when backtracking to a new choice number
     *
     * @param choiceNum Choice number to which backtracking to
     */
    public static void logBacktrack(int choiceNum, int stepNum) {
        if (verbosity > 1) {
            log.info(String.format("  Backtracking to choice @%d::%d", stepNum, choiceNum));
        }
    }

    public static void logNewScheduleChoice(List<PMachine> choices, int step, int idx) {
        if (verbosity > 1) {
            log.info(String.format("    @%d::%d new schedule choice: %s", step, idx, choices));
        }
    }

    public static void logNewDataChoice(List<PValue<?>> choices, int step, int idx) {
        if (verbosity > 1) {
            log.info(String.format("    @%d::%d new data choice: %s", step, idx, choices));
        }
    }

    public static void logRepeatScheduleChoice(PMachine choice, int step, int idx) {
        if (verbosity > 2) {
            log.info(String.format("    @%d::%d %s (repeat)", step, idx, choice));
        }
    }

    public static void logCurrentScheduleChoice(PMachine choice, int step, int idx) {
        if (verbosity > 2) {
            log.info(String.format("    @%d::%d %s", step, idx, choice));
        }
    }

    public static void logRepeatDataChoice(PValue<?> choice, int step, int idx) {
        if (verbosity > 2) {
            log.info(String.format("    @%d::%d %s (repeat)", step, idx, choice));
        }
    }

    public static void logCurrentDataChoice(PValue<?> choice, int step, int idx) {
        if (verbosity > 2) {
            log.info(String.format("    @%d::%d %s", step, idx, choice));
        }
    }

    private static boolean isReplaying() {
        return (PExplicitGlobal.getScheduler() instanceof ReplayScheduler);
    }

    private static boolean typedLogEnabled() {
        return (verbosity > 4) || isReplaying();
    }

    private static void typedLog(LogType type, String message) {
        if (isReplaying()) {
            TextWriter.typedLog(type, message);
        } else {
            log.info(String.format("    <%s> %s", type, message));
        }
    }

    public static void logRunTest() {
        if (typedLogEnabled()) {
            typedLog(LogType.TestLog, String.format("Running test %s.",
                    PExplicitGlobal.getConfig().getTestDriver()));
        }
    }

    public static void logModel(String message) {
        if (verbosity > 0) {
            log.info(message);
        }
        if (typedLogEnabled()) {
            typedLog(LogType.PrintLog, message);
        }
    }

    public static void logBugFound(String message) {
        if (typedLogEnabled()) {
            typedLog(LogType.ErrorLog, message);
            typedLog(LogType.StrategyLog, String.format("Found bug using '%s' strategy.", PExplicitGlobal.getConfig().getSearchStrategyMode()));
            typedLog(LogType.StrategyLog, "Checking statistics:");
            typedLog(LogType.StrategyLog, "Found 1 bug.");
        }
    }

    /**
     * Log when a machine is created
     *
     * @param machine Machine that is created
     * @param creator Machine that created this machine
     */
    public static void logCreateMachine(PMachine machine, PMachine creator) {
        PExplicitGlobal.getScheduler().updateLogNumber();
        if (typedLogEnabled()) {
            typedLog(LogType.CreateLog, String.format("%s was created by %s.", machine, creator));
        }
    }

    public static void logSendEvent(PMachine sender, PMessage message) {
        PExplicitGlobal.getScheduler().updateLogNumber();
        if (typedLogEnabled()) {
            String payloadMsg = "";
            if (message.getPayload() != null) {
                payloadMsg = String.format(" with payload %s", message.getPayload());
            }
            typedLog(LogType.SendLog, String.format("%s in state %s sent event %s%s to %s.",
                    sender, sender.getCurrentState(), message.getEvent(), payloadMsg, message.getTarget()));
        }
    }

    /**
     * Log when a machine enters a new state
     *
     * @param machine Machine that is entering the state
     */
    public static void logStateEntry(PMachine machine) {
        PExplicitGlobal.getScheduler().updateLogNumber();
        if (typedLogEnabled()) {
            typedLog(LogType.StateLog, String.format("%s enters state %s.", machine, machine.getCurrentState()));
        }
    }

    /**
     * Log when a machine exits the current state
     *
     * @param machine Machine that is exiting the state
     */
    public static void logStateExit(PMachine machine) {
        PExplicitGlobal.getScheduler().updateLogNumber();
        if (typedLogEnabled()) {
            typedLog(LogType.StateLog, String.format("%s exits state %s.", machine, machine.getCurrentState()));
        }
    }

    public static void logRaiseEvent(PMachine machine, PEvent event) {
        PExplicitGlobal.getScheduler().updateLogNumber();
        if (typedLogEnabled()) {
            typedLog(LogType.RaiseLog, String.format("%s raised event %s in state %s.", machine, event, machine.getCurrentState()));
        }
    }

    public static void logStateTransition(PMachine machine, State newState) {
        PExplicitGlobal.getScheduler().updateLogNumber();
        if (typedLogEnabled()) {
            typedLog(LogType.GotoLog, String.format("%s is transitioning from state %s to state %s.", machine, machine.getCurrentState(), newState));
        }
    }

    public static void logReceive(PMachine machine, PContinuation continuation) {
        PExplicitGlobal.getScheduler().updateLogNumber();
        if (typedLogEnabled()) {
            typedLog(LogType.ReceiveLog, String.format("%s is waiting to dequeue an event of type %s or %s in state %s.",
                    machine, continuation.getCaseEvents(), PEvent.haltEvent, machine.getCurrentState()));
        }
    }

    public static void logMonitorProcessEvent(PMonitor monitor, PMessage message) {
        PExplicitGlobal.getScheduler().updateLogNumber();
        if (typedLogEnabled()) {
            typedLog(LogType.MonitorLog, String.format("%s is processing event %s in state %s.",
                    monitor, message.getEvent(), monitor.getCurrentState()));
        }
    }

    public static void logDequeueEvent(PMachine machine, PMessage message) {
        PExplicitGlobal.getScheduler().updateLogNumber();
        if (typedLogEnabled()) {
            String payloadMsg = "";
            if (message.getPayload() != null) {
                payloadMsg = String.format(" with payload %s", message.getPayload());
            }
            typedLog(LogType.DequeueLog, String.format("%s dequeued event %s%s in state %s.",
                    machine, message.getEvent(), payloadMsg, machine.getCurrentState()));
        }
    }

    public static void logStartReplay() {
        if (verbosity > 0) {
            log.info("--------------------");
            log.info("Replaying schedule");
        }
    }
}
