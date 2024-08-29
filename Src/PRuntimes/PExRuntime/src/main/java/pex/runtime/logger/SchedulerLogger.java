package pex.runtime.logger;

import lombok.Setter;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;
import org.apache.logging.log4j.core.Appender;
import org.apache.logging.log4j.core.LoggerContext;
import org.apache.logging.log4j.core.appender.OutputStreamAppender;
import org.apache.logging.log4j.core.layout.PatternLayout;
import pex.runtime.PExGlobal;
import pex.runtime.machine.PMachine;
import pex.runtime.machine.PMachineId;
import pex.runtime.machine.PMonitor;
import pex.runtime.machine.State;
import pex.runtime.machine.events.PContinuation;
import pex.runtime.scheduler.choice.ScheduleSearchUnit;
import pex.runtime.scheduler.choice.SearchUnit;
import pex.runtime.scheduler.explicit.ExplicitSearchScheduler;
import pex.runtime.scheduler.explicit.strategy.SearchTask;
import pex.runtime.scheduler.replay.ReplayScheduler;
import pex.values.ComputeHash;
import pex.values.PEvent;
import pex.values.PMessage;
import pex.values.PValue;

import java.io.*;
import java.util.List;
import java.util.SortedSet;

/**
 * Represents the logger for scheduler
 */
public class SchedulerLogger {
    Logger log = null;
    LoggerContext context = null;
    @Setter
    int verbosity;

    /**
     * Initializes the logger with the given verbosity level.
     */
    public SchedulerLogger(int schId) {
        verbosity = PExGlobal.getConfig().getVerbosity();
        log = Log4JConfig.getContext().getLogger(SchedulerLogger.class.getName() + schId);
        org.apache.logging.log4j.core.Logger coreLogger =
                (org.apache.logging.log4j.core.Logger) LogManager.getLogger(SchedulerLogger.class.getName() + schId);
        context = coreLogger.getContext();

        try {
            // get new file name
            String fileName = PExGlobal.getConfig().getOutputFolder() + "/threads/" + schId + ".log";
            File file = new File(fileName);
            file.getParentFile().mkdirs();
            file.createNewFile();

            // Define new file printer
            FileOutputStream fout = new FileOutputStream(fileName, false);

            PatternLayout layout = Log4JConfig.getPatternLayout();
            Appender fileAppender =
                    OutputStreamAppender.createAppender(layout, null, fout, fileName, false, true);
            fileAppender.start();

            context.getConfiguration().addLoggerAppender(coreLogger, fileAppender);
        } catch (IOException e) {
            System.out.println("Failed to set printer to the SchedulerLogger!!");
        }
    }

    /**
     * Logs the given message based on the current verbosity level.
     *
     * @param message Message to print
     */
    public void logVerbose(String message) {
        if (verbosity > 3) {
            log.info(message);
        }
    }

    /**
     * Log at the start of a search task
     *
     * @param task Search task
     */
    public void logStartTask(SearchTask task) {
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
    public void logEndTask(SearchTask task, int numSchedules) {
        if (verbosity > 0) {
            log.info(String.format("  Finished %s after exploring %d schedules", task, numSchedules));
        }
    }

    /**
     * Log when the next task is selected
     *
     * @param task Next search task
     */
    public void logNextTask(SearchTask task) {
        if (verbosity > 0) {
            log.info(String.format("  Next task: %s", task.toStringDetailed()));
        }
    }

    public void logNewTasks(List<SearchTask> tasks) {
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
     * Log when serializing a task
     *
     * @param task    Task to serialize
     * @param szBytes Bytes written
     */
    public void logSerializeTask(SearchTask task, long szBytes) {
        if (verbosity > 1) {
            log.info(String.format("      %,.1f MB  written in %s", (szBytes / 1024.0 / 1024.0), task.getSerializeFile()));
        }
    }

    /**
     * Log when deserializing a task
     *
     * @param task Task that is deserialized
     */
    public void logDeserializeTask(SearchTask task) {
        if (verbosity > 1) {
            log.info(String.format("      Reading %s from %s", task, task.getSerializeFile()));
        }
    }

    /**
     * Log at the start of an iteration
     *
     * @param task  Search task
     * @param schId Scheduler id
     * @param iter  Schedule number
     * @param step  Starting step number
     */
    public void logStartIteration(SearchTask task, int schId, int iter, int step) {
        if (verbosity > 0) {
            log.info("--------------------");
            log.info(String.format("[%d::%s] Starting schedule %s from step %s", schId, task, iter, step));
        }
    }

    public void logStartStep(int step, PMachine sender, PMessage msg) {
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
    public void logFinishedIteration(int step) {
        if (verbosity > 0) {
            log.info(String.format("  Schedule finished at step %d", step));
        }
    }

    /**
     * Log when backtracking to a search unit
     *
     * @param stepNum   Step number
     * @param choiceNum Choice number
     * @param unit      Search unit to which backtracking to
     */
    public void logBacktrack(int stepNum, int choiceNum, SearchUnit unit) {
        if (verbosity > 1) {
            log.info(String.format("  Backtracking to %s choice @%d::%d",
                    ((unit instanceof ScheduleSearchUnit) ? "schedule" : "data"),
                    stepNum, choiceNum));
        }
    }

    public void logNewScheduleChoice(List<PMachineId> choices, int step, int idx) {
        if (verbosity > 1) {
            log.info(String.format("    @%d::%d new schedule choice: %s", step, idx, choices));
        }
    }

    public void logNewDataChoice(List<PValue<?>> choices, int step, int idx) {
        if (verbosity > 1) {
            log.info(String.format("    @%d::%d new data choice: %s", step, idx, choices));
        }
    }

    public void logRepeatScheduleChoice(PMachine choice, int step, int idx) {
        if (verbosity > 2) {
            log.info(String.format("    @%d::%d %s (repeat)", step, idx, choice));
        }
    }

    public void logCurrentScheduleChoice(PMachine choice, int step, int idx) {
        if (verbosity > 2) {
            log.info(String.format("    @%d::%d %s", step, idx, choice));
        }
    }

    public void logRepeatDataChoice(PValue<?> choice, int step, int idx) {
        if (verbosity > 2) {
            log.info(String.format("    @%d::%d %s (repeat)", step, idx, choice));
        }
    }

    public void logCurrentDataChoice(PValue<?> choice, int step, int idx) {
        if (verbosity > 2) {
            log.info(String.format("    @%d::%d %s", step, idx, choice));
        }
    }

    public void logNewState(int step, int idx, Object stateKey, SortedSet<PMachine> machines) {
        if (verbosity > 3) {
            log.info(String.format("    @%d::%d new state with key %s", step, idx, stateKey));
            if (verbosity > 6) {
                log.info(String.format("      %s", ComputeHash.getExactString(machines)));
            }
        }
    }

    /**
     * Log a new timeline
     *
     * @param sch Scheduler
     */
    public void logNewTimeline(ExplicitSearchScheduler sch) {
        if (verbosity > 2) {
            log.info(String.format("  new timeline %d", PExGlobal.getTimelines().size()));
            if (verbosity > 4) {
                log.info(String.format("      %s", sch.getStepState().getTimelineString()));
            }
        }
    }

    private boolean isReplaying() {
        return (PExGlobal.getScheduler() instanceof ReplayScheduler);
    }

    private boolean typedLogEnabled() {
        return (verbosity > 5) || isReplaying();
    }

    private void typedLog(LogType type, String message) {
        if (isReplaying()) {
            TextWriter.typedLog(type, message);
        } else {
            log.info(String.format("    <%s> %s", type, message));
        }
    }

    public void logRunTest() {
        if (typedLogEnabled()) {
            typedLog(LogType.TestLog, String.format("Running test %s.",
                    PExGlobal.getConfig().getTestDriver()));
        }
    }

    public void logModel(String message) {
        if (verbosity > 0) {
            log.info(message);
        }
        if (typedLogEnabled()) {
            typedLog(LogType.PrintLog, message);
        }
    }

    public void logBugFound(String message) {
        if (typedLogEnabled()) {
            typedLog(LogType.ErrorLog, message);
            typedLog(LogType.StrategyLog, String.format("Found bug using '%s' strategy.", PExGlobal.getConfig().getSearchStrategyMode()));
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
    public void logCreateMachine(PMachine machine, PMachine creator) {
        PExGlobal.getScheduler().updateLogNumber();
        if (typedLogEnabled()) {
            typedLog(LogType.CreateLog, String.format("%s was created by %s.", machine, creator));
        }
    }

    public void logSendEvent(PMachine sender, PMessage message) {
        PExGlobal.getScheduler().updateLogNumber();
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
    public void logStateEntry(PMachine machine) {
        PExGlobal.getScheduler().updateLogNumber();
        if (typedLogEnabled()) {
            typedLog(LogType.StateLog, String.format("%s enters state %s.", machine, machine.getCurrentState()));
        }
    }

    /**
     * Log when a machine exits the current state
     *
     * @param machine Machine that is exiting the state
     */
    public void logStateExit(PMachine machine) {
        PExGlobal.getScheduler().updateLogNumber();
        if (typedLogEnabled()) {
            typedLog(LogType.StateLog, String.format("%s exits state %s.", machine, machine.getCurrentState()));
        }
    }

    public void logRaiseEvent(PMachine machine, PEvent event) {
        PExGlobal.getScheduler().updateLogNumber();
        if (typedLogEnabled()) {
            typedLog(LogType.RaiseLog, String.format("%s raised event %s in state %s.", machine, event, machine.getCurrentState()));
        }
    }

    public void logStateTransition(PMachine machine, State newState) {
        PExGlobal.getScheduler().updateLogNumber();
        if (typedLogEnabled()) {
            typedLog(LogType.GotoLog, String.format("%s is transitioning from state %s to state %s.", machine, machine.getCurrentState(), newState));
        }
    }

    public void logReceive(PMachine machine, PContinuation continuation) {
        PExGlobal.getScheduler().updateLogNumber();
        if (typedLogEnabled()) {
            typedLog(LogType.ReceiveLog, String.format("%s is waiting to dequeue an event of type %s or %s in state %s.",
                    machine, continuation.getCaseEvents(), PEvent.haltEvent, machine.getCurrentState()));
        }
    }

    public void logMonitorProcessEvent(PMonitor monitor, PMessage message) {
        PExGlobal.getScheduler().updateLogNumber();
        if (typedLogEnabled()) {
            typedLog(LogType.MonitorLog, String.format("%s is processing event %s in state %s.",
                    monitor, message.getEvent(), monitor.getCurrentState()));
        }
    }

    public void logDequeueEvent(PMachine machine, PMessage message) {
        PExGlobal.getScheduler().updateLogNumber();
        if (typedLogEnabled()) {
            String payloadMsg = "";
            if (message.getPayload() != null) {
                payloadMsg = String.format(" with payload %s", message.getPayload());
            }
            typedLog(LogType.DequeueLog, String.format("%s dequeued event %s%s in state %s.",
                    machine, message.getEvent(), payloadMsg, machine.getCurrentState()));
        }
    }

    public void logStartReplay() {
        if (verbosity > 0) {
            log.info("--------------------");
            log.info("Replaying schedule");
        }
    }

    /**
     * Print error trace
     *
     * @param e Exception object
     */
    public void logStackTrace(Exception e) {
        StringWriter sw = new StringWriter();
        PrintWriter pw = new PrintWriter(sw);
        e.printStackTrace(pw);
        log.info("--------------------");
        log.info(sw.toString());
    }
}
