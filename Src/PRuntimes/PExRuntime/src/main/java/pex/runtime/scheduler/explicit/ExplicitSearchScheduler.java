package pex.runtime.scheduler.explicit;

import com.google.common.hash.Hashing;
import lombok.Getter;
import pex.runtime.PExGlobal;
import pex.runtime.STATUS;
import pex.runtime.machine.PMachine;
import pex.runtime.machine.PMachineId;
import pex.runtime.scheduler.Scheduler;
import pex.runtime.scheduler.choice.ScheduleChoice;
import pex.runtime.scheduler.choice.SearchUnit;
import pex.runtime.scheduler.explicit.strategy.*;
import pex.utils.exceptions.PExRuntimeException;
import pex.utils.misc.Assert;
import pex.utils.monitor.MemoryMonitor;
import pex.utils.monitor.TimeMonitor;
import pex.values.ComputeHash;
import pex.values.PValue;

import java.util.ArrayList;
import java.util.Collections;
import java.util.List;
import java.util.concurrent.TimeoutException;

/**
 * Represents the scheduler for performing explicit-state model checking
 */
public class ExplicitSearchScheduler extends Scheduler {
    /**
     * Search strategy orchestrator
     */
    @Getter
    private final transient SearchStrategy searchStrategy;
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
    @Getter
    private final SchedulerStatistics stats = new SchedulerStatistics();


    /**
     * Constructor.
     */
    public ExplicitSearchScheduler(int schedulerId) {
        super(schedulerId);
        switch (PExGlobal.getConfig().getSearchStrategyMode()) {
            case DepthFirst:
                searchStrategy = new SearchStrategyDfs();
                break;
            case Random:
                searchStrategy = new SearchStrategyRandom();
                break;
            case Astar:
                searchStrategy = new SearchStrategyAStar();
                break;
            default:
                throw new RuntimeException("Unrecognized search strategy: " + PExGlobal.getConfig().getSearchStrategyMode());
        }
    }

    /**
     * Run the scheduler to perform explicit-state search.
     *
     * @throws TimeoutException Throws timeout exception if timeout is reached
     */
    @Override
    public void run() throws TimeoutException {
        // log run test
        logger.logRunTest();

        searchStrategy.createFirstTask();

        while (true) {
            logger.logStartTask(searchStrategy.getCurrTask());
            isDoneIterating = false;
            while (!isDoneIterating) {
                stats.numSchedules++;
                searchStrategy.incrementCurrTaskNumSchedules();
                logger.logStartIteration(searchStrategy.getCurrTask(), schedulerId, stats.numSchedules, stepNumber);
                if (stepNumber == 0) {
                    start();
                }
                runIteration();
                postProcessIteration();
            }
            logger.logEndTask(searchStrategy.getCurrTask(), searchStrategy.getCurrTaskNumSchedules());
            addRemainingChoicesAsChildrenTasks();
            endCurrTask();

            if (PExGlobal.getPendingTasks().isEmpty() || PExGlobal.getStatus() == STATUS.SCHEDULEOUT) {
                // all tasks completed or schedule limit reached
                break;
            }

            // schedule limit not reached and there are pending tasks
            // set the next task
            SearchTask nextTask = setNextTask();
            assert (nextTask != null);
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
            runStep();
        }

        stats.totalSteps += stepNumber;
        if (stats.minSteps == -1 || stepNumber < stats.minSteps) {
            stats.minSteps = stepNumber;
        }
        if (stats.maxSteps == -1 || stepNumber > stats.maxSteps) {
            stats.maxSteps = stepNumber;
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
                !PExGlobal.getConfig().isFailOnMaxStepBound() || (stepNumber < PExGlobal.getConfig().getMaxStepBound()),
                "Step bound of " + PExGlobal.getConfig().getMaxStepBound() + " reached.");

        if (PExGlobal.getConfig().getMaxSchedules() > 0) {
            if (PExGlobal.getTotalSchedules() >= PExGlobal.getConfig().getMaxSchedules()) {
                isDoneIterating = true;
                PExGlobal.setStatus(STATUS.SCHEDULEOUT);
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

        // check if we can skip the remaining schedule
        if (skipRemainingSchedule()) {
            scheduleTerminated = false;
            skipLiveness = true;
            isDoneStepping = true;
            logger.logFinishedIteration(stepNumber);
            return;
        }

        if (PExGlobal.getConfig().getStatefulBacktrackingMode() != StatefulBacktrackingMode.None
                && stepNumber != 0) {
            schedule.setStepBeginState(stepState.copyState());
        }

        // update timeline
        Object timeline = stepState.getTimeline();
        if (!PExGlobal.getTimelines().contains(timeline)) {
            // add new timeline
            logger.logNewTimeline(this);
            PExGlobal.getTimelines().add(timeline);
        }

        // get a scheduling choice as sender machine
        PMachine sender = getNextScheduleChoice();

        if (sender == null) {
            // done with this schedule
            scheduleTerminated = true;
            skipLiveness = false;
            isDoneStepping = true;
            logger.logFinishedIteration(stepNumber);
            return;
        }

        // execute a step from message in the sender queue
        executeStep(sender);
    }

    /**
     * Check if the remaining schedule can be skipped if current state is already in state cache
     *
     * @return true if remaining schedule can be skipped
     */
    boolean skipRemainingSchedule() {
        if (PExGlobal.getConfig().getStateCachingMode() == StateCachingMode.None) {
            return false;
        }

        // perform state caching only if beyond backtrack choice number
        if (isStickyStep || choiceNumber <= backtrackChoiceNumber) {
            return false;
        }

        // increment state count
        stats.totalStates++;

        // get state key
        Object stateKey = getCurrentStateKey();

        // check if state key is present in state cache
        String visitedAt = PExGlobal.getStateCache().get(stateKey);
        String stateVal = String.format("%d-%d", schedulerId, stats.numSchedules);

        if (visitedAt == null) {
            // not present, add to state cache
            PExGlobal.getStateCache().put(stateKey, stateVal);
            // log new state
            logger.logNewState(stepNumber, choiceNumber, stateKey, stepState.getMachines());
        } else {
            // present in state cache

            // check if possible cycle
            if (visitedAt == stateVal) {
                if (PExGlobal.getConfig().isFailOnMaxStepBound()) {
                    // cycle detected since revisited same state at a different step in the same schedule
                    Assert.cycle("Cycle detected: Infinite loop found due to revisiting a state multiple times in the same schedule");
                } else {
                    // do nothing, allow cycle to run till max bound reached
                }
            } else {
                // done with this schedule
                return true;
            }
        }
        return false;
    }

    /**
     * Get the hashing key corresponding to the current protocol state
     *
     * @return
     */
    Object getCurrentStateKey() {
        Object stateKey;
        switch (PExGlobal.getConfig().getStateCachingMode()) {
            case HashCode -> stateKey = ComputeHash.getHashCode(stepState.getMachines());
            case SipHash24 -> stateKey = ComputeHash.getHashCode(stepState.getMachines(), Hashing.sipHash24());
            case Murmur3_128 ->
                    stateKey = ComputeHash.getHashCode(stepState.getMachines(), Hashing.murmur3_128((int) PExGlobal.getConfig().getRandomSeed()));
            case Sha256 -> stateKey = ComputeHash.getHashCode(stepState.getMachines(), Hashing.sha256());
            case Exact -> stateKey = ComputeHash.getExactString(stepState.getMachines());
            default ->
                    throw new PExRuntimeException(String.format("Unexpected state caching mode: %s", PExGlobal.getConfig().getStateCachingMode()));
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

        if (choiceNumber < backtrackChoiceNumber) {
            // pick the current schedule choice
            PMachineId pid = schedule.getCurrentScheduleChoice(choiceNumber);
            result = getMachine(pid);
            logger.logRepeatScheduleChoice(result, stepNumber, choiceNumber);

            // increment choice number
            choiceNumber++;
            return result;
        }

        // get existing unexplored choices, if any
        List<PMachineId> choices = searchStrategy.getCurrTask().getScheduleSearchUnit(choiceNumber);

        if (choices.isEmpty()) {
            // no existing unexplored choices, so try generating new choices
            choices = getNewScheduleChoices();
            if (choices.size() > 1) {
                // log new choice
                logger.logNewScheduleChoice(choices, stepNumber, choiceNumber);
            }

            if (choices.isEmpty()) {
                // no unexplored choices remaining

                // increment choice number
                choiceNumber++;
                return null;
            }
        }

        // pick a choice
        int selected = PExGlobal.getChoiceSelector().selectChoice(this, choices);
        result = getMachine(choices.get(selected));
        logger.logCurrentScheduleChoice(result, stepNumber, choiceNumber);

        // remove the selected choice from unexplored choices
        choices.remove(selected);

        // add choice to schedule
        schedule.setScheduleChoice(stepNumber, choiceNumber, result.getPid());

        // update search unit in search task
        if (choices.isEmpty()) {
            searchStrategy.getCurrTask().clearSearchUnit(choiceNumber);
        } else {
            searchStrategy.getCurrTask().setScheduleSearchUnit(choiceNumber, choices);
        }

        // increment choice number
        choiceNumber++;
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

        if (choiceNumber < backtrackChoiceNumber) {
            // pick the current data choice
            result = schedule.getCurrentDataChoice(choiceNumber);
            assert (input_choices.contains(result));
            logger.logRepeatDataChoice(result, stepNumber, choiceNumber);

            // increment choice number
            choiceNumber++;
            return result;
        }

        // get existing unexplored choices, if any
        List<PValue<?>> choices = searchStrategy.getCurrTask().getDataSearchUnit(choiceNumber);
        assert (input_choices.containsAll(choices));

        if (choices.isEmpty()) {
            // no existing unexplored choices, so try generating new choices
            choices = input_choices;
            if (choices.size() > 1) {
                // log new choice
                logger.logNewDataChoice(choices, stepNumber, choiceNumber);
            }

            if (choices.isEmpty()) {
                // no unexplored choices remaining

                // increment choice number
                choiceNumber++;
                return null;
            }
        }

        // pick a choice
        int selected = PExGlobal.getChoiceSelector().selectChoice(this, choices);
        result = choices.get(selected);
        logger.logCurrentDataChoice(result, stepNumber, choiceNumber);

        // remove the selected choice from unexplored choices
        choices.remove(selected);

        // add choice to schedule
        schedule.setDataChoice(stepNumber, choiceNumber, result);

        // update search unit in search task
        if (choices.isEmpty()) {
            searchStrategy.getCurrTask().clearSearchUnit(choiceNumber);
        } else {
            searchStrategy.getCurrTask().setDataSearchUnit(choiceNumber, choices);
        }

        // increment choice number
        choiceNumber++;
        return result;
    }

    private void postProcessIteration() {
        int maxSchedulesPerTask = PExGlobal.getConfig().getMaxSchedulesPerTask();
        if (maxSchedulesPerTask > 0 && searchStrategy.getCurrTaskNumSchedules() >= maxSchedulesPerTask) {
            isDoneIterating = true;
        }

        if (!isDoneIterating) {
            postIterationCleanup();
        }
    }

    private void addRemainingChoicesAsChildrenTasks() {
        SearchTask parentTask = searchStrategy.getCurrTask();
        int numChildrenAdded = 0;
        for (int i : parentTask.getSearchUnitKeys(false)) {
            SearchUnit unit = parentTask.getSearchUnit(i);
            // if search unit at this depth is non-empty
            if (!unit.getUnexplored().isEmpty()) {
                if (PExGlobal.getConfig().getMaxChildrenPerTask() > 0 && numChildrenAdded == (PExGlobal.getConfig().getMaxChildrenPerTask() - 1)) {
                    setChildTask(unit, i, parentTask, false);
                    break;
                }
                // top search task should be always exact
                setChildTask(unit, i, parentTask, true);
                numChildrenAdded++;
            }
        }

        logger.logNewTasks(parentTask.getChildren());
    }

    private void endCurrTask() {
        SearchTask currTask = searchStrategy.getCurrTask();
        currTask.cleanup();
        PExGlobal.getFinishedTasks().add(currTask);
    }

    private void setChildTask(SearchUnit unit, int choiceNum, SearchTask parentTask, boolean isExact) {
        SearchTask newTask = searchStrategy.createTask(parentTask);

        int maxChoiceNum = choiceNum;

        newTask.addSuffixSearchUnit(choiceNum, unit);

        if (!isExact) {
            for (int i : parentTask.getSearchUnitKeys(false)) {
                if (i > choiceNum) {
                    if (i > maxChoiceNum) {
                        maxChoiceNum = i;
                    }
                    newTask.addSuffixSearchUnit(i, parentTask.getSearchUnit(i));
                }
            }
        }

        for (int i = 0; i <= maxChoiceNum; i++) {
            newTask.addPrefixChoice(schedule.getChoice(i));
        }

        newTask.writeToFile();
        parentTask.addChild(newTask);
        PExGlobal.getPendingTasks().add(newTask);
        searchStrategy.addNewTask(newTask);
    }

    /**
     * Set next backtrack task with given orchestration mode
     */
    public SearchTask setNextTask() {
        SearchTask nextTask = searchStrategy.setNextTask();
        if (nextTask != null) {
            logger.logNextTask(nextTask);
            schedule.setChoices(nextTask.getPrefixChoices());
            postIterationCleanup();
        }
        return nextTask;
    }

    private void postIterationCleanup() {
        SearchTask task = searchStrategy.getCurrTask();
        for (int cIdx : task.getSearchUnitKeys(true)) {
            SearchUnit unit = task.getSearchUnit(cIdx);
            if (unit.getUnexplored().isEmpty()) {
                task.clearSearchUnit(cIdx);
                continue;
            }

            backtrackChoiceNumber = cIdx;
            int newStepNumber = 0;
            ScheduleChoice scheduleChoice = null;
            if (PExGlobal.getConfig().getStatefulBacktrackingMode() != StatefulBacktrackingMode.None) {
                scheduleChoice = schedule.getScheduleChoiceAt(cIdx);
                if (scheduleChoice != null && scheduleChoice.getChoiceState() != null) {
                    newStepNumber = scheduleChoice.getStepNumber();
                }
            }
            if (newStepNumber == 0) {
                reset();
                stepState.resetToZero(getMachineSet());
            } else {
                stepNumber = newStepNumber;
                choiceNumber = scheduleChoice.getChoiceNumber();
                stepState.setTo(getMachineSet(), scheduleChoice.getChoiceState());
                assert (!getMachine(scheduleChoice.getCurrent()).getSendBuffer().isEmpty());
            }
            schedule.removeChoicesAfter(backtrackChoiceNumber);
            logger.logBacktrack(newStepNumber, cIdx, unit);
            return;
        }
        schedule.clear();
        isDoneIterating = true;
    }

    private List<PMachineId> getNewScheduleChoices() {
        // prioritize create machine events
        for (PMachine machine : stepState.getMachines()) {
            if (machine.getSendBuffer().nextIsCreateMachineMsg()) {
                return new ArrayList<>(Collections.singletonList(machine.getPid()));
            }
        }

        // now there are no create machine events remaining
        List<PMachineId> choices = new ArrayList<>();

        for (PMachine machine : stepState.getMachines()) {
            if (machine.getSendBuffer().nextHasTargetRunning()) {
                choices.add(machine.getPid());
            }
        }

        return choices;
    }
}
