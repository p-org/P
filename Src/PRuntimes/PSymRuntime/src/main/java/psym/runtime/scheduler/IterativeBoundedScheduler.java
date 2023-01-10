package psym.runtime.scheduler;

import org.apache.commons.lang3.StringUtils;
import psym.commandline.Assert;
import psym.commandline.PSymConfiguration;
import psym.commandline.Program;
import psym.runtime.logger.*;
import psym.runtime.machine.Machine;
import psym.runtime.scheduler.choiceorchestration.*;
import psym.runtime.scheduler.taskorchestration.*;
import psym.runtime.statistics.SearchStats;
import psym.utils.*;
import psym.valuesummary.*;
import psym.runtime.machine.buffer.*;

import java.io.*;
import java.math.BigDecimal;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.util.*;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.TimeoutException;
import java.util.function.BiConsumer;
import java.util.function.Consumer;
import java.util.function.Function;
import java.util.function.Supplier;
import java.util.stream.Collectors;

/**
 * Represents the iterative bounded scheduler
 */
public class IterativeBoundedScheduler extends Scheduler {

    /** List of all backtrack tasks */
    private final List<BacktrackTask> allTasks = new ArrayList<>();
    /** Priority queue of all backtrack tasks that are pending */
    private final Set<Integer> pendingTasks = new HashSet<>();
    /** List of all backtrack tasks that finished */
    private final List<Integer> finishedTasks = new ArrayList<>();
    /** Task id of the latest backtrack task */
    private int latestTaskId = 0;
    private int numPendingBacktracks = 0;
    private int numPendingDataBacktracks = 0;
    private boolean isDoneIterating = false;
    private final ChoiceOrchestrator choiceOrchestrator;

    public IterativeBoundedScheduler(PSymConfiguration config, Program p) {
        super(config, p);
        switch (config.getChoiceOrchestration()) {
            case None:
                choiceOrchestrator = new ChoiceOrchestratorNone();
                break;
            case Random:
                choiceOrchestrator = new ChoiceOrchestratorRandom();
                break;
            case QLearning:
                choiceOrchestrator = new ChoiceOrchestratorQLearning();
                break;
            case EpsilonGreedy:
                choiceOrchestrator = new ChoiceOrchestratorEpsilonGreedy();
                break;
            default:
                throw new RuntimeException("Unrecognized choice orchestration mode: " + config.getChoiceOrchestration());
        }
    }

    private void resetBacktrackTasks() {
        pendingTasks.clear();
        numPendingBacktracks = 0;
        numPendingDataBacktracks = 0;
        BacktrackTask.initialize(configuration.getTaskOrchestration());
    }

    private void isValidTaskId(int taskId) {
        assert(taskId < allTasks.size());
    }

    private BacktrackTask getTask(int taskId) {
        isValidTaskId(taskId);
        return allTasks.get(taskId);
    }


    @Override
    public void print_stats() {
        super.print_stats();

        // print statistics
        StatWriter.log("#-tasks-finished", String.format("%d", finishedTasks.size()));
        StatWriter.log("#-tasks-remaining", String.format("%d",  (allTasks.size() - finishedTasks.size())));
        StatWriter.log("#-backtracks", String.format("%d", getTotalNumBacktracks()));
        StatWriter.log("%-backtracks-data", String.format("%.2f", getTotalDataBacktracksPercent()));
        StatWriter.log("#-executions", String.format("%d", (iter - start_iter)));
    }

    @Override
    public void reset_stats() {
        super.reset_stats();
    }

    /**
     * Estimates and prints a coverage percentage based on number of choices explored versus remaining at each depth
     */
    public void reportEstimatedCoverage() {
        GlobalData.getCoverage().reportChoiceCoverage();

        SearchLogger.log("\n--------------------");
        SearchLogger.log(String.format("Estimated Coverage:: %.10f %%", GlobalData.getCoverage().getEstimatedCoverage()));
        if (configuration.isUseStateCaching()) {
            SearchLogger.log(String.format("Distinct States Explored:: %d", getTotalDistinctStates()));
        }
        StatWriter.log("coverage-%", String.format("%.20f", GlobalData.getCoverage().getEstimatedCoverage(20)));
    }

    void recordResult() {
        SearchStats.TotalStats totalStats = searchStats.getSearchTotal();
        result = "";
        if (start_iter != 0) {
            result += "(resumed run) ";
        }
        if (totalStats.isCompleted()) {
            if (getTotalNumBacktracks() == 0) {
                result += "correct for any depth";
            } else {
                result += "partially correct with " + getTotalNumBacktracks() + " backtracks remaining";
            }
        } else {
            int safeDepth = configuration.getMaxStepBound();
            if (totalStats.getDepthStats().getDepth() < safeDepth) {
                safeDepth = totalStats.getDepthStats().getDepth();
            }
            if (getTotalNumBacktracks() == 0) {
                result += "correct up to step " + safeDepth;
            } else {
                result += "partially correct up to step " + (configuration.getMaxStepBound()-1) + " with " + getTotalNumBacktracks() + " backtracks remaining";
            }
        }
    }

    /**
     * Write scheduler state to a file
     * @param writeFileName Output file name
     * @throws Exception Throw error if writing fails
     */
    public void writeToFile(String writeFileName) throws Exception {
        try {
            FileOutputStream fos = new FileOutputStream(writeFileName);
            ObjectOutputStream oos = new ObjectOutputStream(fos);
            oos.writeObject(this);
            oos.writeObject(GlobalData.getInstance());
            if (configuration.getVerbosity() > 0) {
                long szBytes = Files.size(Paths.get(writeFileName));
                PSymLogger.info(String.format("  %,.1f MB  written in %s", (szBytes / 1024.0 / 1024.0), writeFileName));
            }
        } catch (IOException e) {
            e.printStackTrace();
            throw new Exception("Failed to write state in file " + writeFileName);
        }
    }

    /**
     * Write each backtracking point state individually
     * @param prefix Output file name prefix
     * @throws Exception Throw error if writing fails
     */
    public void writeBacktracksToFiles(String prefix) throws Exception {
        for (int i = 0; i < schedule.size(); i++) {
            Schedule.Choice choice = schedule.getChoice(i);
            // if choice at this depth is non-empty
            if (!choice.isBacktrackEmpty()) {
                writeBacktrackToFile(prefix, i);
            }
        }
        while (setNextBacktrackTask() != null) {
            for (int i = 0; i < schedule.size(); i++) {
                Schedule.Choice choice = schedule.getChoice(i);
                // if choice at this depth is non-empty
                if (!choice.isBacktrackEmpty()) {
                    writeBacktrackToFile(prefix, i);
                }
            }
        }
    }

    /**
     * Write backtracking point state individually at a given depth
     * @param prefix Output file name prefix
     * @param choiceDepth Backtracking point choice depth
     * @throws Exception Throw error if writing fails
     */
    public void writeBacktrackToFile(String prefix, int choiceDepth) throws Exception {
        // create a copy of original choices
        List<Schedule.Choice> originalChoices = new ArrayList<>();
        for (int i = 0; i < schedule.size(); i++) {
            originalChoices.add(schedule.getChoice(i).getCopy());
        }

        // clear backtracks at all predecessor depths
        for (int i = 0; i < choiceDepth; i++) {
            schedule.getChoice(i).clearBacktrack();
        }
        // clear the complete choice information (including repeats and backtracks) at all successor depths
        for (int i = choiceDepth+1; i < schedule.size(); i++) {
            schedule.clearChoice(i);
        }
        int depth = schedule.getChoice(choiceDepth).getSchedulerDepth();
        long pid = ProcessHandle.current().pid();
        String writeFileName = prefix + "_d" + depth + "_cd" + choiceDepth + "_task" + latestTaskId + "_pid" + pid + ".out";
        // write to file
        writeToFile(writeFileName);
        BacktrackWriter.log(writeFileName, GlobalData.getCoverage().getPathCoverageAtDepth(choiceDepth), depth, choiceDepth);

        // restore schedule to original choices
        schedule.setChoices(originalChoices);
    }

    /**
     * Read scheduler state from a file
     * @param readFromFile Name of the input file containing the scheduler state
     * @return A scheduler object
     * @throws Exception Throw error if reading fails
     */
    public static IterativeBoundedScheduler readFromFile(String readFromFile) throws Exception {
        IterativeBoundedScheduler result;
        try {
            PSymLogger.info("... Reading program state from file " + readFromFile);
            FileInputStream fis;
            fis = new FileInputStream(readFromFile);
            ObjectInputStream ois = new ObjectInputStream(fis);
            result = (IterativeBoundedScheduler) ois.readObject();
            GlobalData.setInstance((GlobalData) ois.readObject());
            result.reinitialize();
            PSymLogger.info("... Successfully read.");
        } catch (IOException | ClassNotFoundException e) {
            e.printStackTrace();
            throw new Exception("... Failed to read program state from file " + readFromFile);
        }
        return result;
    }

    private void setBacktrackTasks() {
        BacktrackTask parentTask;
        if (latestTaskId == 0) {
            assert(allTasks.isEmpty());
            BacktrackTask.setOrchestration(configuration.getTaskOrchestration());
            parentTask = new BacktrackTask(0);
            parentTask.setPrefixCoverage(new BigDecimal(1));
            allTasks.add(parentTask);
        } else {
            parentTask = getTask(latestTaskId);
        }
        parentTask.postProcess(GlobalData.getCoverage().getPathCoverageAtDepth(getChoiceDepth()-1));
        finishedTasks.add(parentTask.getId());
        if (configuration.getVerbosity() > 1) {
            PSymLogger.info(String.format("  Finished %s [depth: %d, parent: %s]",
                    parentTask, parentTask.getDepth(), parentTask.getParentTask()));
        }

        int numBacktracksAdded = 0;
        for (int i = 0; i < schedule.size(); i++) {
            Schedule.Choice choice = schedule.getChoice(i);
            // if choice at this depth is non-empty
            if (!choice.isBacktrackEmpty()) {
                if (configuration.getMaxBacktrackTasksPerExecution() > 0 &&
                        numBacktracksAdded == (configuration.getMaxBacktrackTasksPerExecution()-1)) {
                    setBacktrackTaskAtDepthCombined(parentTask, i);
                    numBacktracksAdded++;
                    break;
                } else {
                    // top backtrack should be never combined
                    setBacktrackTaskAtDepthExact(parentTask, i);
                    numBacktracksAdded++;
                }
            }
        }

        if (configuration.getVerbosity() > 1) {
            PSymLogger.info(String.format("    Added %d new tasks", parentTask.getChildren().size()));
            if (configuration.getVerbosity() > 2) {
                for (BacktrackTask t : parentTask.getChildren()) {
                    PSymLogger.info(String.format("      %s [depth: %d]", t, t.getDepth()));
                }
            }
        }
    }

    private void setBacktrackTaskAtDepthExact(BacktrackTask parentTask, int choiceDepth) {
        setBacktrackTaskAtDepth(parentTask, choiceDepth, true);
    }

    private void setBacktrackTaskAtDepthCombined(BacktrackTask parentTask, int choiceDepth) {
        setBacktrackTaskAtDepth(parentTask, choiceDepth, false);
    }

    private void setBacktrackTaskAtDepth(BacktrackTask parentTask, int choiceDepth, boolean isExact) {
        // create a copy of original choices
        List<Schedule.Choice> originalChoices = new ArrayList<>();
        for (int i = 0; i < schedule.size(); i++) {
            originalChoices.add(schedule.getChoice(i).getCopy());
        }

        // clear backtracks at all predecessor depths
        for (int i = 0; i < choiceDepth; i++) {
            schedule.getChoice(i).clearBacktrack();
        }
        if (isExact) {
            // clear the complete choice information (including repeats and backtracks) at all successor depths
            for (int i = choiceDepth + 1; i < schedule.size(); i++) {
                schedule.clearChoice(i);
            }
        }

        BigDecimal prefixCoverage = GlobalData.getCoverage().getPathCoverageAtDepth(choiceDepth);

        BacktrackTask newTask = new BacktrackTask(allTasks.size());
        newTask.setPrefixCoverage(prefixCoverage);
        newTask.setDepth(schedule.getChoice(choiceDepth).getSchedulerDepth());
        newTask.setChoiceDepth(choiceDepth);
        newTask.setChoices(schedule.getChoices());
        newTask.setPerChoiceDepthStats(GlobalData.getCoverage().getPerChoiceDepthStats());
        newTask.setParentTask(parentTask);
        newTask.setPriority();
        allTasks.add(newTask);
        parentTask.addChild(newTask);
        addPendingTask(newTask);

        // restore schedule to original choices
        schedule.setChoices(originalChoices);
    }

    private void addPendingTask(BacktrackTask task) {
        pendingTasks.add(task.getId());
        numPendingBacktracks += task.getNumBacktracks();
        numPendingDataBacktracks += task.getNumDataBacktracks();
    }

    private void removePendingTask(BacktrackTask task) {
        pendingTasks.remove(task.getId());
        numPendingBacktracks -= task.getNumBacktracks();
        numPendingDataBacktracks -= task.getNumDataBacktracks();
    }

    /**
     * Set next backtrack task with given orchestration mode
     */
    public BacktrackTask setNextBacktrackTask() throws InterruptedException {
        if (pendingTasks.isEmpty())
            return null;
        BacktrackTask latestTask = BacktrackTask.getNextTask();
        latestTaskId = latestTask.getId();
        assert(!latestTask.isCompleted());
        removePendingTask(latestTask);

        schedule.getChoices().clear();
        GlobalData.getCoverage().getPerChoiceDepthStats().clear();
        assert(!latestTask.isInitialTask());
        latestTask.getParentTask().cleanup();

        schedule.setChoices(latestTask.getChoices());
        GlobalData.getCoverage().setPerChoiceDepthStats(latestTask.getPerChoiceDepthStats());
        return latestTask;
    }

    public int getTotalNumBacktracks() {
        int count = schedule.getNumBacktracksInSchedule();
        count += numPendingBacktracks;
        return count;
    }

    public double getTotalDataBacktracksPercent() {
        int totalBacktracks = getTotalNumBacktracks();
        if (totalBacktracks == 0) {
            return 0.0;
        }
        int count = schedule.getNumDataBacktracksInSchedule();
        count += numPendingDataBacktracks;
        return (count * 100.0) / totalBacktracks;
    }

    private void printProgressHeader(boolean consolePrint) {
        StringBuilder s = new StringBuilder(100);
        s.append(StringUtils.center("Time", 12));
        s.append(StringUtils.center("Memory", 12));
        s.append(StringUtils.center("Coverage", 18));
        s.append(StringUtils.center("Iteration", 12));
        s.append(StringUtils.center("Remaining", 24));
        s.append(StringUtils.center("Depth", 10));
        if (configuration.isUseStateCaching()) {
            s.append(StringUtils.center("States", 12));
        }

        if (consolePrint) {
            System.out.println(s);
        } else {
            PSymLogger.info(s.toString());
        }
    }

    private void printProgress(boolean forcePrint) {
        boolean consolePrint = (configuration.getVerbosity() == 0);
        if (!consolePrint) {
            if (!forcePrint) {
                return;
            }
        }

        long runtime = (long) (TimeMonitor.getInstance().getRuntime() * 1000);
        String runtimeHms =  String.format("%02d:%02d:%02d", TimeUnit.MILLISECONDS.toHours(runtime),
                TimeUnit.MILLISECONDS.toMinutes(runtime) % TimeUnit.HOURS.toMinutes(1),
                TimeUnit.MILLISECONDS.toSeconds(runtime) % TimeUnit.MINUTES.toSeconds(1));

        StringBuilder s = new StringBuilder(100);
        if (consolePrint) {
            s.append('\r');
        } else {
            PSymLogger.info("--------------------");
            printProgressHeader(false);
        }
        s.append(StringUtils.center(String.format("%s", runtimeHms), 12));
        s.append(StringUtils.center(String.format("%.1f GB", MemoryMonitor.getMemSpent() / 1024), 12));
        s.append(StringUtils.center(String.format("%.10f %%", GlobalData.getCoverage().getEstimatedCoverage()), 18));
        s.append(StringUtils.center(String.format("%d", (iter - start_iter)), 12));
        s.append(StringUtils.center(String.format("%d (%.0f %% data)", getTotalNumBacktracks(), getTotalDataBacktracksPercent()), 24));
        s.append(StringUtils.center(String.format("%d", getDepth()), 10));
        if (configuration.isUseStateCaching()) {
            s.append(StringUtils.center(String.format("%d", getTotalDistinctStates()), 12));
        }

        if (consolePrint) {
            System.out.print(s);
        } else {
            SearchLogger.log(s.toString());
        }
    }

    private void printCurrentStatus() {
        if (configuration.getCollectStats() == 0) {
            return;
        }

        ScratchLogger.log("--------------------");
        ScratchLogger.log(String.format("    Status after %.2f seconds:", TimeMonitor.getInstance().getRuntime()));
        ScratchLogger.log(String.format("      Coverage:         %.10f %%", GlobalData.getCoverage().getEstimatedCoverage()));
        ScratchLogger.log(String.format("      Iterations:       %d", (iter - start_iter)));
        ScratchLogger.log(String.format("      Memory:           %.2f MB", MemoryMonitor.getMemSpent()));
        ScratchLogger.log(String.format("      Finished:         %d", finishedTasks.size()));
        ScratchLogger.log(String.format("      Remaining:        %d", getTotalNumBacktracks()));
        ScratchLogger.log(String.format("      Depth:            %d", getDepth()));
        ScratchLogger.log(String.format("      States:           %d", getTotalStates()));
        ScratchLogger.log(String.format("      DistinctStates:   %d", getTotalDistinctStates()));
    }

    public void postIterationCleanup() {
        schedule.resetFilter();
        for (int d = schedule.size() - 1; d >= 0; d--) {
            Schedule.Choice choice = schedule.getChoice(d);
            choice.updateHandledUniverse(choice.getRepeatUniverse());
            schedule.clearRepeat(d);
            if (!choice.isBacktrackEmpty()) {
                int newDepth = 0;
                if (configuration.isUseBacktrack()) {
                    newDepth = choice.getSchedulerDepth();
                }
                if (newDepth == 0) {
                    for (Machine machine : machines) {
                        machine.reset();
                    }
                } else {
                    restoreState(choice.getChoiceState());
                    schedule.setFilter(choice.getFilter());
                }
                SearchLogger.logMessage("backtrack to " + d);
                backtrackDepth = d;
                if (newDepth == 0) {
                    reset();
                    initializeSearch();
                } else {
                    restore(newDepth, choice.getSchedulerChoiceDepth());
                }
                return;
            } else {
                schedule.clearChoice(d);
                GlobalData.getCoverage().resetPathCoverage(d);
            }
        }
        isDoneIterating = true;
    }

    private void summarizeIteration() throws InterruptedException {
        recordResult();
        if (configuration.getVerbosity() > 3) {
            SearchLogger.logIterationStats(searchStats.getIterationStats().get(iter));
        }
        if (configuration.getMaxExecutions() > 0) {
            isDoneIterating = ((iter - start_iter) >= configuration.getMaxExecutions());
        }
        GlobalData.getCoverage().updateIterationCoverage(getChoiceDepth()-1);
//        GlobalData.getChoiceLearningStats().printQTable();
        if (configuration.getTaskOrchestration() != TaskOrchestrationMode.DepthFirst) {
            setBacktrackTasks();
            BacktrackTask nextTask = setNextBacktrackTask();
            if (nextTask != null) {
                if (configuration.getVerbosity() > 1) {
                    PSymLogger.info(String.format("    Next is %s [depth: %d, parent: %s]",
                            nextTask, nextTask.getDepth(), nextTask.getParentTask()));
                }
            }
        }
        printProgress(true);
        printCurrentStatus();
        if (!isDoneIterating) {
            postIterationCleanup();
//            if ((iter % 100) == 0) {
//                if (configuration.isSymbolic() && SolverEngine.getSolverType() == SolverType.BDD) {
//                    SolverEngine.resumeEngine();
//                }
//            }
        }
    }

    @Override
    public void reinitialize() {
        super.reinitialize();
    }

    @Override
    public void startWith(Machine machine) {
        super.startWith(machine);
        /*
        if (iter == 0) {
            super.startWith(machine);
        } else {
            super.replayStartWith(machine);
        }
         */
    }

    @Override
    public void performSearch() throws TimeoutException {
        schedule.setNumBacktracksInSchedule();
        while (!isDone()) {
            printProgress(false);
            printCurrentStatus();

            // ScheduleLogger.log("step " + depth + ", true queries " + Guard.trueQueries + ", false queries " + Guard.falseQueries);
            Assert.prop(getDepth() < configuration.getMaxStepBound(), "Maximum allowed depth " + configuration.getMaxStepBound() + " exceeded", schedule.getLengthCond(schedule.size()));
            super.step();
        }
        schedule.setNumBacktracksInSchedule();
        if (done) {
            searchStats.setIterationCompleted();
        } else {
//            cleanup();
        }
    }

    @Override
    public void doSearch() throws TimeoutException, InterruptedException {
        boolean initialRun = true;
        result = "incomplete";
        iter++;
        resetBacktrackTasks();
        SearchLogger.logStartExecution(iter, getDepth());
        initializeSearch();
        if (configuration.getVerbosity() == 0) {
            printProgressHeader(true);
        }
        while (!isDoneIterating) {
            if (initialRun) {
                initialRun = false;
            } else {
                iter++;
                SearchLogger.logStartExecution(iter, getDepth());
            }
            searchStats.startNewIteration(iter, backtrackDepth);
            performSearch();
            checkLiveness(false);
            summarizeIteration();
        }
    }

    public void resumeSearch() throws TimeoutException, InterruptedException {
        boolean initialRun = true;
        isDoneIterating = false;
        start_iter = iter;
        resetBacktrackTasks();
        reset_stats();
        schedule.setNumBacktracksInSchedule();
        boolean resetAfterInitial = isDone();
        if (configuration.getVerbosity() == 0) {
            printProgressHeader(true);
        }
        while (!isDoneIterating) {
            if (initialRun) {
                initialRun = false;
                SearchLogger.logResumeExecution(iter, getDepth());
            } else {
                iter++;
                SearchLogger.logStartExecution(iter, getDepth());
            }
            searchStats.startNewIteration(iter, backtrackDepth);
            performSearch();
            checkLiveness(false);
            summarizeIteration();
            if (resetAfterInitial) {
                resetAfterInitial = false;
                GlobalData.getCoverage().resetCoverage();
            }
        }
    }

    private PrimitiveVS getNext(int depth, int bound, Function<Integer, PrimitiveVS> getRepeat, Function<Integer, List> getBacktrack,
                           Consumer<Integer> clearBacktrack, BiConsumer<PrimitiveVS, Integer> addRepeat,
                           BiConsumer<List, Integer> addBacktrack, Supplier<List> getChoices,
                           Function<List, PrimitiveVS> generateNext, boolean isData) {
        List<ValueSummary> choices = new ArrayList();
        boolean isNewChoice = false;

        if (depth < schedule.size()) {
            PrimitiveVS repeat = getRepeat.apply(depth);
            if (!repeat.getUniverse().isFalse()) {
                schedule.restrictFilterForDepth(depth);
                return repeat;
            }
            // nothing to repeat, so look at backtrack set
            choices = getBacktrack.apply(depth);
            clearBacktrack.accept(depth);
        }

        if (choices.isEmpty()) {
            // no choice to backtrack to, so generate new choices
            if (iter > 0)
                SearchLogger.logMessage("new choice at depth " + depth);
            choices = getChoices.get();
            choices = choices.stream().map(x -> x.restrict(schedule.getFilter())).filter(x -> !(x.getUniverse().isFalse())).collect(Collectors.toList());
            isNewChoice = true;
        }

        if (choices.size() > 1) {
            choiceOrchestrator.reorderChoices(choices, bound, isData);
        }

        List<ValueSummary> chosen = new ArrayList();
        ChoiceQTable.ChoiceQStateKey chosenQStateKey = new ChoiceQTable.ChoiceQStateKey();
        List<ValueSummary> backtrack = new ArrayList();
        for (int i = 0; i < choices.size(); i++) {
            ValueSummary choice = choices.get(i);
            if ((bound <= 0) || (i < bound)) {
                chosen.add(choice);
                chosenQStateKey.add(choice);
            } else {
                backtrack.add(choice);
            }
        }
        ChoiceQTable.ChoiceQTableKey chosenActions = new ChoiceQTable.ChoiceQTableKey(GlobalData.getChoiceLearningStats().getProgramStateHash(), chosenQStateKey);
        GlobalData.getCoverage().updateDepthCoverage(getDepth(), getChoiceDepth(), chosen.size(), backtrack.size(), isData, isNewChoice, chosenActions);

        PrimitiveVS chosenVS = generateNext.apply(chosen);
//        addRepeat.accept(chosenVS, depth);
        addBacktrack.accept(backtrack, depth);
        schedule.restrictFilterForDepth(depth);
        return chosenVS;
    }

    @Override
    public PrimitiveVS<Machine> getNextSender() {
        int depth = choiceDepth;
        PrimitiveVS<Machine> res = getNext(depth, configuration.getSchedChoiceBound(), schedule::getRepeatSender, schedule::getBacktrackSender,
                schedule::clearBacktrack, schedule::addRepeatSender, schedule::addBacktrackSender, super::getNextSenderChoices,
                super::getNextSender, false);

        choiceDepth = depth + 1;
        return res;
    }

    @Override
    public PrimitiveVS<Boolean> getNextBoolean(Guard pc) {
        int depth = choiceDepth;
        PrimitiveVS<Boolean> res = getNext(depth, configuration.getDataChoiceBound(), schedule::getRepeatBool, schedule::getBacktrackBool,
                schedule::clearBacktrack, schedule::addRepeatBool, schedule::addBacktrackBool,
                () -> super.getNextBooleanChoices(pc), super::getNextBoolean, true);
        choiceDepth = depth + 1;
        return res;
    }

    @Override
    public PrimitiveVS<Integer> getNextInteger(PrimitiveVS<Integer> bound, Guard pc) {
        int depth = choiceDepth;
        PrimitiveVS<Integer> res = getNext(depth, configuration.getDataChoiceBound(), schedule::getRepeatInt, schedule::getBacktrackInt,
                schedule::clearBacktrack, schedule::addRepeatInt, schedule::addBacktrackInt,
                () -> super.getNextIntegerChoices(bound, pc), super::getNextInteger, true);
        choiceDepth = depth + 1;
        return res;
    }

    @Override
    public ValueSummary getNextElement(ListVS<? extends ValueSummary> candidates, Guard pc) {
        int depth = choiceDepth;
        PrimitiveVS<ValueSummary> res = getNext(depth, configuration.getDataChoiceBound(), schedule::getRepeatElement, schedule::getBacktrackElement,
                schedule::clearBacktrack, schedule::addRepeatElement, schedule::addBacktrackElement,
                () -> super.getNextElementChoices(candidates, pc), super::getNextElementHelper, true);
        choiceDepth = depth + 1;
        return super.getNextElementFlattener(res);
    }

    @Override
    public PrimitiveVS<Machine> allocateMachine(Guard pc, Class<? extends Machine> machineType,
                                           Function<Integer, ? extends Machine> constructor) {
        if (!machineCounters.containsKey(machineType)) {
            machineCounters.put(machineType, new PrimitiveVS<>(0));
        }
        PrimitiveVS<Integer> guardedCount = machineCounters.get(machineType).restrict(pc);

        PrimitiveVS<Machine> allocated;
        if (schedule.hasMachine(machineType, guardedCount, pc)) {
            assert (iter != 0);
            allocated = schedule.getMachine(machineType, guardedCount).restrict(pc);
            for (GuardedValue gv: allocated.getGuardedValues()) {
                Guard g = gv.getGuard();
                Machine m = (Machine) gv.getValue();
                assert(!BooleanVS.isEverTrue(m.hasStarted().restrict(g)));
                TraceLogger.onCreateMachine(pc.and(g), m);
                if (!machines.contains(m)) {
                    machines.add(m);
                }
                currentMachines.add(m);
                assert(machines.size() >= currentMachines.size());
                m.setScheduler(this);
            }
        } else {
            Machine newMachine;
            newMachine = constructor.apply(IntegerVS.maxValue(guardedCount));

            if (!machines.contains(newMachine)) {
                machines.add(newMachine);
            }
            currentMachines.add(newMachine);
            assert(machines.size() >= currentMachines.size());

            TraceLogger.onCreateMachine(pc, newMachine);
            newMachine.setScheduler(this);
            if (useBagSemantics()) {
                newMachine.setSemantics(EventBufferSemantics.bag);
            }
            if (useReceiverSemantics()) {
                newMachine.setSemantics(EventBufferSemantics.receiver);
            }
            schedule.makeMachine(newMachine, pc);
            getVcManager().addMachine(pc, newMachine);
            allocated = new PrimitiveVS<>(newMachine).restrict(pc);
        }

        guardedCount = IntegerVS.add(guardedCount, 1);

        PrimitiveVS<Integer> mergedCount = machineCounters.get(machineType).updateUnderGuard(pc, guardedCount);
        machineCounters.put(machineType, mergedCount);
        return allocated;
    }

}
