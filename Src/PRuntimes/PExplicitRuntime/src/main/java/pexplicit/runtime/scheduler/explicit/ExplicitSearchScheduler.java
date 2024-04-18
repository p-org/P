package pexplicit.runtime.scheduler.explicit;

import lombok.Getter;
import lombok.Setter;
import org.apache.commons.lang3.StringUtils;
import pexplicit.runtime.PExplicitGlobal;
import pexplicit.runtime.STATUS;
import pexplicit.runtime.logger.PExplicitLogger;
import pexplicit.runtime.logger.ScratchLogger;
import pexplicit.runtime.logger.StatWriter;
import pexplicit.runtime.machine.PMachine;
import pexplicit.runtime.machine.events.PMessage;
import pexplicit.runtime.scheduler.Choice;
import pexplicit.runtime.scheduler.Scheduler;
import pexplicit.utils.misc.Assert;
import pexplicit.utils.monitor.MemoryMonitor;
import pexplicit.utils.monitor.TimeMonitor;
import pexplicit.values.ComputeHash;
import pexplicit.values.PValue;

import java.time.Duration;
import java.time.Instant;
import java.util.*;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.TimeoutException;

/**
 * Represents the scheduler for performing explicit-state model checking
 */
public class ExplicitSearchScheduler extends Scheduler {
    /**
     * Backtrack choice number
     */
    @Getter
    private transient int backtrackChoiceNumber = 0;

    /**
     * Whether done with all iterations
     */
    private transient boolean isDoneIterating = false;

    /**
     * Whether to skip liveness check (because of early schedule termination due to state caching)
     */
    private transient boolean skipLiveness = false;

    /**
     * Time of last status report
     */
    @Getter
    @Setter
    private transient Instant lastReportTime = Instant.now();

    /**
     * Map from state hash to iteration when first visited
     */
    private transient Map<Object, Integer> stateCache = new HashMap<>();

    /**
     * Constructor.
     */
    public ExplicitSearchScheduler() {
        super();
    }

    /**
     * Run the scheduler to perform explicit-state search.
     *
     * @throws TimeoutException Throws timeout exception if timeout is reached
     */
    @Override
    public void run() throws TimeoutException {
        // log run test
        PExplicitLogger.logRunTest();

        PExplicitGlobal.setResult("incomplete");
        if (PExplicitGlobal.getConfig().getVerbosity() == 0) {
            printProgressHeader(true);
        }
        isDoneIterating = false;
        while (!isDoneIterating) {
            SearchStatistics.iteration++;
            PExplicitLogger.logStartIteration(SearchStatistics.iteration, schedule.getStepNumber());
            start();
            runIteration();
            postProcessIteration();
        }
    }

    /**
     * Run an iteration.
     *
     * @throws TimeoutException Throws timeout exception if timeout is reached.
     */
    @Override
    protected void runIteration() throws TimeoutException {
        isDoneStepping = false;
        scheduleTerminated = false;
        skipLiveness = false;
        while (!isDoneStepping) {
            printProgress(false);
            runStep();
        }
        printProgress(false);

        SearchStatistics.totalSteps += schedule.getStepNumber();
        if (SearchStatistics.minSteps == -1 || schedule.getStepNumber() < SearchStatistics.minSteps) {
            SearchStatistics.minSteps = schedule.getStepNumber();
        }
        if (SearchStatistics.maxSteps == -1 || schedule.getStepNumber() > SearchStatistics.maxSteps) {
            SearchStatistics.maxSteps = schedule.getStepNumber();
        }

        if (scheduleTerminated) {
            // schedule terminated, check for deadlock
            checkDeadlock();
        }
        if (!skipLiveness) {
            // check for liveness
            checkLiveness(scheduleTerminated);
        }

        Assert.fromModel(
                !PExplicitGlobal.getConfig().isFailOnMaxStepBound() || (schedule.getStepNumber() < PExplicitGlobal.getConfig().getMaxStepBound()),
                "Step bound of " + PExplicitGlobal.getConfig().getMaxStepBound() + " reached.");

        if (PExplicitGlobal.getConfig().getMaxSchedules() > 0) {
            if (SearchStatistics.iteration >= PExplicitGlobal.getConfig().getMaxSchedules()) {
                isDoneIterating = true;
                PExplicitGlobal.setStatus(STATUS.SCHEDULEOUT);
            }
        }
    }

    /**
     * Run a step in the current iteration.
     *
     * @throws TimeoutException Throws timeout exception if timeout is reached.
     */
    @Override
    protected void runStep() throws TimeoutException {
        // check for timeout/memout
        TimeMonitor.checkTimeout();
        MemoryMonitor.checkMemout();

        if (PExplicitGlobal.getConfig().getStateCachingMode() != StateCachingMode.None) {
            // perform state caching only if beyond backtrack choice number
            if (schedule.getChoiceNumber() > backtrackChoiceNumber) {
                // increment state count
                SearchStatistics.totalStates++;

                // get state key
                Object stateKey = getCurrentStateKey();

                // check if state key is present in state cache
                Integer visitedAtIteration = stateCache.get(stateKey);

                if (visitedAtIteration == null) {
                    // not present, add to state cache
                    stateCache.put(stateKey, SearchStatistics.iteration);
                    // increment distinct state count
                    SearchStatistics.totalDistinctStates++;
                } else {
                    // present in state cache
                    if (visitedAtIteration == SearchStatistics.iteration) {
                        // cycle detected since revisited same state at a different step in the same schedule
                        Assert.cycle(false, "Cycle detected: Infinite loop found due to revisiting a state multiple times in the same schedule");
                    } else {
                        // done with this schedule
                        scheduleTerminated = false;
                        skipLiveness = true;
                        isDoneStepping = true;
                        PExplicitLogger.logFinishedIteration(schedule.getStepNumber());
                        return;
                    }
                }
            }
        }

        // reset number of logs in current step
        stepNumLogs = 0;

        // get a scheduling choice as sender machine
        PMachine sender = getNextScheduleChoice();

        if (sender == null) {
            // done with this schedule
            scheduleTerminated = true;
            skipLiveness = false;
            isDoneStepping = true;
            PExplicitLogger.logFinishedIteration(schedule.getStepNumber());
            return;
        }

        // execute a step from message in the sender queue
        executeStep(sender);
    }

    /**
     * Get the hashing key corresponding to the current protocol state
     * @return
     */
    Object getCurrentStateKey() {
        Object stateKey = null;
        if (PExplicitGlobal.getConfig().getStateCachingMode() == StateCachingMode.Fingerprint) {
            // use fingerprinting by hashing values from each machine vars
            int fingerprint = ComputeHash.getHashCode(schedule.getMachineSet());
            stateKey = fingerprint;
        } else {
            // use exact values from each machine vars
            List<List<Object>> machineValues = new ArrayList<>();
            for (PMachine machine: schedule.getMachineSet()) {
                machineValues.add(machine.getLocalVarValues());
            }
            stateKey = machineValues;
        }
        return stateKey;
    }

    /**
     * Reset the scheduler.
     */
    @Override
    protected void reset() {
        super.reset();
    }

    /**
     * Get the next schedule choice.
     *
     * @return Machine as scheduling choice.
     */
    @Override
    public PMachine getNextScheduleChoice() {
        PMachine result;

        if (schedule.getChoiceNumber() < backtrackChoiceNumber) {
            // pick the current schedule choice
            result = schedule.getCurrentScheduleChoice(schedule.getChoiceNumber());
            PExplicitLogger.logRepeatScheduleChoice(result, schedule.getStepNumber(), schedule.getChoiceNumber());

            schedule.setChoiceNumber(schedule.getChoiceNumber() + 1);
            return result;
        }

        // get existing unexplored choices, if any
        List<PMachine> choices = schedule.getUnexploredScheduleChoices(schedule.getChoiceNumber());

        if (choices.isEmpty()) {
            // no existing unexplored choices, so try generating new choices
            choices = getNewScheduleChoices();
            if (choices.size() > 1) {
                // log new choice
                PExplicitLogger.logNewScheduleChoice(choices, schedule.getStepNumber(), schedule.getChoiceNumber());
            }

            if (choices.isEmpty()) {
                // no unexplored choices remaining
                schedule.setChoiceNumber(schedule.getChoiceNumber() + 1);
                return null;
            }
        }

        // pick the first choice
        result = choices.get(0);
        schedule.setCurrentScheduleChoice(result, schedule.getChoiceNumber());
        PExplicitLogger.logCurrentScheduleChoice(result, schedule.getStepNumber(), schedule.getChoiceNumber());

        // remove the first choice from unexplored choices
        choices.remove(0);

        // set unexplored choices
        schedule.setUnexploredScheduleChoices(choices, schedule.getChoiceNumber());

        // increment choice number
        schedule.setChoiceNumber(schedule.getChoiceNumber() + 1);

        return result;
    }

    /**
     * Get the next data choice.
     *
     * @return PValue as data choice
     */
    @Override
    public PValue<?> getNextDataChoice(List<PValue<?>> input_choices) {
        PValue<?> result;

        if (schedule.getChoiceNumber() < backtrackChoiceNumber) {
            // pick the current data choice
            result = schedule.getCurrentDataChoice(schedule.getChoiceNumber());
            assert (input_choices.contains(result));
            PExplicitLogger.logRepeatDataChoice(result, schedule.getStepNumber(), schedule.getChoiceNumber());

            schedule.setChoiceNumber(schedule.getChoiceNumber() + 1);
            return result;
        }

        // get existing unexplored choices, if any
        List<PValue<?>> choices = schedule.getUnexploredDataChoices(schedule.getChoiceNumber());
        assert (input_choices.containsAll(choices));

        if (choices.isEmpty()) {
            // no existing unexplored choices, so try generating new choices
            choices = input_choices;
            if (choices.size() > 1) {
                // log new choice
                PExplicitLogger.logNewDataChoice(choices, schedule.getStepNumber(), schedule.getChoiceNumber());
            }

            if (choices.isEmpty()) {
                // no unexplored choices remaining
                schedule.setChoiceNumber(schedule.getChoiceNumber() + 1);
                return null;
            }
        }

        // pick the first choice
        result = choices.get(0);
        schedule.setCurrentDataChoice(result, schedule.getChoiceNumber());
        PExplicitLogger.logCurrentDataChoice(result, schedule.getStepNumber(), schedule.getChoiceNumber());

        // remove the first choice from unexplored choices
        choices.remove(0);

        // set unexplored choices
        schedule.setUnexploredDataChoices(choices, schedule.getChoiceNumber());

        // increment choice number
        schedule.setChoiceNumber(schedule.getChoiceNumber() + 1);

        return result;
    }

    private void postProcessIteration() {
        if (!isDoneIterating) {
            postIterationCleanup();
        }
    }

    private void postIterationCleanup() {
        for (int cIdx = schedule.size() - 1; cIdx >= 0; cIdx--) {
            Choice choice = schedule.getChoice(cIdx);
            schedule.clearCurrent(cIdx);
            if (choice.isUnexploredNonEmpty()) {
                PExplicitLogger.logBacktrack(cIdx);
                backtrackChoiceNumber = cIdx;
                return;
            } else {
                schedule.clearChoice(cIdx);
            }
        }
        isDoneIterating = true;
    }

    private List<PMachine> getNewScheduleChoices() {
        // prioritize create machine events
        for (PMachine machine : schedule.getMachineSet()) {
            if (machine.getSendBuffer().nextIsCreateMachineMsg()) {
                return new ArrayList<>(Collections.singletonList(machine));
            }
        }

        // now there are no create machine events remaining
        List<PMachine> choices = new ArrayList<>();

        for (PMachine machine : schedule.getMachineSet()) {
            if (machine.getSendBuffer().nextHasTargetRunning()) {
                choices.add(machine);
            }
        }

        return choices;
    }

    public void updateResult() {
        if (PExplicitGlobal.getStatus() == STATUS.BUG_FOUND) {
            return;
        }

        String result = "";
        int maxStepBound = PExplicitGlobal.getConfig().getMaxStepBound();
        int numUnexplored = schedule.getNumUnexploredChoices();
        if (SearchStatistics.maxSteps < maxStepBound) {
            if (numUnexplored == 0) {
                result += "correct for any depth";
            } else {
                result += String.format("partially correct with %d choices remaining", numUnexplored);
            }
        } else {
            if (numUnexplored == 0) {
                result += String.format("correct up to step %d", SearchStatistics.maxSteps);
            } else {
                result += String.format("partially correct up to step %d with %d choices remaining", SearchStatistics.maxSteps, numUnexplored);
            }

        }
        PExplicitGlobal.setResult(result);
    }

    public void recordStats() {
        double timeUsed = (Duration.between(TimeMonitor.getStart(), Instant.now()).toMillis() / 1000.0);
        double memoryUsed = MemoryMonitor.getMemSpent();

        printProgress(true);

        // print basic statistics
        StatWriter.log("time-seconds", String.format("%.1f", timeUsed));
        StatWriter.log("memory-max-MB", String.format("%.1f", MemoryMonitor.getMaxMemSpent()));
        StatWriter.log("memory-current-MB", String.format("%.1f", memoryUsed));
        StatWriter.log("#-schedules", String.format("%d", SearchStatistics.iteration));
        if (PExplicitGlobal.getConfig().getStateCachingMode() != StateCachingMode.None) {
            StatWriter.log("#-states", String.format("%d", SearchStatistics.totalStates));
            StatWriter.log("#-distinct-states", String.format("%d", SearchStatistics.totalDistinctStates));
        }
        StatWriter.log("steps-min", String.format("%d", SearchStatistics.minSteps));
        StatWriter.log("steps-max", String.format("%d", SearchStatistics.maxSteps));
        StatWriter.log("steps-avg", String.format("%d", SearchStatistics.totalSteps/SearchStatistics.iteration));
        StatWriter.log("#-choices-unexplored", String.format("%d", schedule.getNumUnexploredChoices()));
        StatWriter.log("%-choices-unexplored-data", String.format("%.1f", schedule.getUnexploredDataChoicesPercent()));
    }

    private void printCurrentStatus(double newRuntime) {

        String s = "--------------------" +
                 String.format("\n    Status after %.2f seconds:", newRuntime) +
                 String.format("\n      Memory:           %.2f MB", MemoryMonitor.getMemSpent()) +
                 String.format("\n      Depth:            %d", schedule.getStepNumber()) +
                 String.format("\n      Schedules:        %d", SearchStatistics.iteration) +
                 String.format("\n      Unexplored:       %d", schedule.getNumUnexploredChoices());
        if (PExplicitGlobal.getConfig().getStateCachingMode() != StateCachingMode.None) {
            s += String.format("\n      DistinctStates:   %d", SearchStatistics.totalDistinctStates);
        }

        ScratchLogger.log(s);
    }

    private void printProgressHeader(boolean consolePrint) {
        StringBuilder s = new StringBuilder(100);
        s.append(StringUtils.center("Time", 11));
        s.append(StringUtils.center("Memory", 9));
        s.append(StringUtils.center("Step", 7));

        s.append(StringUtils.center("Schedule", 12));
        s.append(StringUtils.center("Unexplored", 24));

        if (PExplicitGlobal.getConfig().getStateCachingMode() != StateCachingMode.None) {
            s.append(StringUtils.center("States", 12));
        }

        if (consolePrint) {
            System.out.println("--------------------");
            System.out.println(s);
        } else {
            PExplicitLogger.logVerbose("--------------------");
            PExplicitLogger.logVerbose(s.toString());
        }
    }

    protected void printProgress(boolean forcePrint) {
        if (forcePrint || (TimeMonitor.findInterval(getLastReportTime()) > 5)) {
            setLastReportTime(Instant.now());
            double newRuntime = TimeMonitor.getRuntime();
            printCurrentStatus(newRuntime);
            boolean consolePrint = (PExplicitGlobal.getConfig().getVerbosity() == 0);
            if (consolePrint || forcePrint) {
                long runtime = (long) (newRuntime * 1000);
                String runtimeHms =
                        String.format(
                                "%02d:%02d:%02d",
                                TimeUnit.MILLISECONDS.toHours(runtime),
                                TimeUnit.MILLISECONDS.toMinutes(runtime) % TimeUnit.HOURS.toMinutes(1),
                                TimeUnit.MILLISECONDS.toSeconds(runtime) % TimeUnit.MINUTES.toSeconds(1));

                StringBuilder s = new StringBuilder(100);
                if (consolePrint) {
                    s.append('\r');
                } else {
                    printProgressHeader(false);
                }
                s.append(StringUtils.center(String.format("%s", runtimeHms), 11));
                s.append(
                        StringUtils.center(String.format("%.1f GB", MemoryMonitor.getMemSpent() / 1024), 9));
                s.append(StringUtils.center(String.format("%d", schedule.getStepNumber()), 7));

                s.append(StringUtils.center(String.format("%d", SearchStatistics.iteration), 12));
                s.append(
                        StringUtils.center(
                                String.format(
                                        "%d (%.0f %% data)", schedule.getNumUnexploredChoices(), schedule.getUnexploredDataChoicesPercent()),
                                24));

                if (PExplicitGlobal.getConfig().getStateCachingMode() != StateCachingMode.None) {
                    s.append(StringUtils.center(String.format("%d", SearchStatistics.totalDistinctStates), 12));
                }

                if (consolePrint) {
                    System.out.print(s);
                } else {
                    PExplicitLogger.logVerbose(s.toString());
                }
            }
        }
    }
}
