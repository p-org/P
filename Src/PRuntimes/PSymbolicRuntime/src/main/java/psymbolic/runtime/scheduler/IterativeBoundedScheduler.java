package psymbolic.runtime.scheduler;

import psymbolic.commandline.Assert;
import psymbolic.commandline.PSymConfiguration;
import psymbolic.commandline.Program;
import psymbolic.runtime.logger.*;
import psymbolic.runtime.machine.Machine;
import psymbolic.runtime.statistics.SearchStats;
import psymbolic.utils.*;
import psymbolic.valuesummary.*;
import psymbolic.runtime.machine.buffer.*;
import psymbolic.valuesummary.solvers.SolverEngine;
import psymbolic.valuesummary.solvers.SolverGuard;

import java.io.*;
import java.math.BigDecimal;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.util.*;
import java.util.concurrent.PriorityBlockingQueue;
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
    private List<BacktrackTask> allTasks = new ArrayList<>();
    /** Priority queue of all backtrack tasks that are pending */
    private transient PriorityBlockingQueue<Integer> pendingTasks = null;
    /** List of all backtrack tasks that finished */
    private List<Integer> finishedTasks = new ArrayList<>();
    /** Task id of the latest backtrack task */
    private int latestTaskId = 0;

    private boolean isDoneIterating = false;

    public IterativeBoundedScheduler(PSymConfiguration config, Program p) {
        super(config, p);
    }

    private void resetBacktrackTasks() {
        finishedTasks.clear();
        pendingTasks = new PriorityBlockingQueue<Integer>(100, new Comparator<Integer>() {
            public int compare(Integer a, Integer b) {
                return getTask(b).getPriority().compareTo(getTask(a).getPriority());
            }
        });
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
        if (configuration.getCollectStats() != 0) {
            StatWriter.log("#-tasks-finished", String.format("%d", finishedTasks.size()));
            StatWriter.log("#-tasks-remaining", String.format("%d",  (allTasks.size() - finishedTasks.size())));
            StatWriter.log("#-backtracks", String.format("%d", getNumBacktracks()));
            StatWriter.log("#-executions", String.format("%d", (iter - start_iter)));
        }
    }

    @Override
    public void reset_stats() {
        super.reset_stats();
    }

    /**
     * Estimates and prints a coverage percentage based on number of choices explored versus remaining at each depth
     */
    public void reportEstimatedCoverage() {
        if (configuration.getCollectStats() != 0) {
            GlobalData.getCoverage().logPerDepthCoverage();
            GlobalData.getCoverage().reportChoiceCoverage();
        }
        SearchLogger.log("--------------------");
        SearchLogger.log(String.format("Estimated Coverage:: %.5f %%", GlobalData.getCoverage().getEstimatedCoverage()));
        if (configuration.getCollectStats() != 0) {
            StatWriter.log("coverage-%", String.format("%.10f", GlobalData.getCoverage().getEstimatedCoverage(10)), false);
        }
    }

    void recordResult() {
        SearchStats.TotalStats totalStats = searchStats.getSearchTotal();
        result = "";
        if (start_iter != 0) {
            result += "(resumed run) ";
        }
        if (totalStats.isCompleted()) {
            if (totalStats.getNumBacktracks() == 0) {
                result += "safe for any depth";
            } else {
                result += "partially safe with " + totalStats.getNumBacktracks() + " backtracks remaining";
            }
        } else {
            int safeDepth = configuration.getDepthBound();
            if (totalStats.getDepthStats().getDepth() < safeDepth) {
                safeDepth = totalStats.getDepthStats().getDepth();
            }
            if (totalStats.getNumBacktracks() == 0) {
                result += "safe up to step " + safeDepth;
            } else {
                result += "partially safe up to step " + (configuration.getDepthBound()-1) + " with " + totalStats.getNumBacktracks() + " backtracks remaining";
            }
        }
    }

    /**
     * Read scheduler state from a file
     * @param readFromFile Name of the input file containing the scheduler state
     * @return A scheduler object
     * @throws Exception Throw error if reading fails
     */
    public static IterativeBoundedScheduler readFromFile(String readFromFile) throws Exception {
        IterativeBoundedScheduler result = null;
        try {
            PSymLogger.info("Reading program state from file " + readFromFile);
            FileInputStream fis = null;
            fis = new FileInputStream(readFromFile);
            ObjectInputStream ois = new ObjectInputStream(fis);
            result = (IterativeBoundedScheduler) ois.readObject();
            GlobalData.setInstance((GlobalData) ois.readObject());
            PSymLogger.info("Successfully read.");
        } catch (FileNotFoundException e) {
            e.printStackTrace();
            throw new Exception("Failed to read program state from file " + readFromFile);
        } catch (IOException e) {
            e.printStackTrace();
            throw new Exception("Failed to read program state from file " + readFromFile);
        } catch (ClassNotFoundException e) {
            e.printStackTrace();
            throw new Exception("Failed to read program state from file " + readFromFile);
        }
        return result;
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
            if (configuration.getCollectStats() != 0) {
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


    private void summarizeIteration() throws InterruptedException {
        recordResult();
        if (configuration.getCollectStats() > 2) {
            SearchLogger.logIterationStats(searchStats.getIterationStats().get(iter));
        }
        if (configuration.getMaxExecutions() >= 0) {
            isDoneIterating = ((iter - start_iter) >= configuration.getMaxExecutions());
        }
        GlobalData.getCoverage().updateIterationCoverage(getChoiceDepth()-1);
        if (configuration.getOrchestration() != OrchestrationMode.None) {
            setBacktrackTasks();
            BacktrackTask nextTask = setNextBacktrackTask();
            if (nextTask != null) {
                PSymLogger.log(String.format("Next is %s [depth: %d, priority: %.4f, parent: %s]",
                        nextTask, nextTask.getDepth(), nextTask.getPriority(), nextTask.getParentTask()));
            }
        }
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

    private void setBacktrackTasks() {
        BacktrackTask parentTask;
        if (latestTaskId == 0) {
            assert(allTasks.isEmpty());
            parentTask = new BacktrackTask(0);
            parentTask.setPrefixCoverage(new BigDecimal(1));
            parentTask.setPriority(configuration.getOrchestration());
            allTasks.add(parentTask);
        } else {
            parentTask = getTask(latestTaskId);
        }
        parentTask.setCoverage(GlobalData.getCoverage().getIterationCoverage(getChoiceDepth()-1));
        finishedTasks.add(parentTask.getId());
        PSymLogger.log(String.format("Finished %s [depth: %d, priority: %.4f, parent: %s]",
                parentTask, parentTask.getDepth(), parentTask.getPriority(), parentTask.getParentTask()));

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
        PSymLogger.log(String.format("  Added %d new tasks", parentTask.getChildren().size()));
        for (Integer i: parentTask.getChildren()) {
            PSymLogger.log(String.format("    %s [depth: %d, priority: %.4f]", getTask(i), getTask(i).getDepth(), getTask(i).getPriority()));
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
        newTask.setPriority(configuration.getOrchestration());
        allTasks.add(newTask);

        parentTask.addChild(newTask.getId());
        pendingTasks.add(newTask.getId());

        // restore schedule to original choices
        schedule.setChoices(originalChoices);
    }

    /**
     * Set next backtrack task with given orchestration mode
     */
    public BacktrackTask setNextBacktrackTask() throws InterruptedException {
        if (pendingTasks.isEmpty())
            return null;
        latestTaskId = pendingTasks.take();
        BacktrackTask latestTask = getTask(latestTaskId);

        schedule.getChoices().clear();
        GlobalData.getCoverage().getPerChoiceDepthStats().clear();
        assert(!latestTask.isInitialTask());
        latestTask.getParentTask().cleanup();

        schedule.setChoices(latestTask.getChoices());
        GlobalData.getCoverage().setPerChoiceDepthStats(latestTask.getPerChoiceDepthStats());
        return latestTask;
    }

    private void printCurrentStatus() {
        PSymLogger.info("--------------------");
        PSymLogger.info(String.format("    Status after %.2f seconds:", TimeMonitor.getInstance().getRuntime()));
        PSymLogger.info(String.format("      Coverage:         %.5f %%", GlobalData.getCoverage().getEstimatedCoverage()));
        PSymLogger.info(String.format("      Executions:       %d", (iter - start_iter)));
        PSymLogger.info(String.format("      Memory:           %.2f MB", MemoryMonitor.getMemSpent()));
        PSymLogger.info(String.format("      Finished:         %d", finishedTasks.size()));
        PSymLogger.info(String.format("      Remaining:        %d", (allTasks.size() - finishedTasks.size())));
        PSymLogger.info(String.format("      Depth:            %d", getDepth()));
        PSymLogger.info(String.format("      States:           %d", getTotalStates()));
        PSymLogger.info(String.format("      DistinctStates:   %d", getTotalDistinctStates()));
    }

    @Override
    public void doSearch() throws TimeoutException, InterruptedException {
        boolean initialRun = true;
        result = "incomplete";
        iter++;
        resetBacktrackTasks();
        SearchLogger.logStartExecution(iter, getDepth());
        initializeSearch();
        while (!isDoneIterating) {
            if (initialRun) {
                initialRun = false;
            } else {
                iter++;
                SearchLogger.logStartExecution(iter, getDepth());
            }
            searchStats.startNewIteration(iter, backtrackDepth);
            performSearch();
            summarizeIteration();
        }
    }

    @Override
    public void performSearch() throws TimeoutException {
        while (!isDone()) {
            // ScheduleLogger.log("step " + depth + ", true queries " + Guard.trueQueries + ", false queries " + Guard.falseQueries);
            Assert.prop(getDepth() < configuration.getDepthBound(), "Maximum allowed depth " + configuration.getDepthBound() + " exceeded", this, schedule.getLengthCond(schedule.size()));
            super.step();
            if (configuration.getDebugMode().startsWith("exp")) {
                printCurrentStatus();
            }
        }
        searchStats.setIterationStats(getNumBacktracks());
        if (done) {
            searchStats.setIterationCompleted();
        } else {
//            cleanup();
        }
    }

    public int getNumBacktracks() {
        int count = schedule.getNumBacktracksInSchedule();
        count += pendingTasks.size();
        return count;
    }

    public void resumeSearch() throws TimeoutException, InterruptedException {
        boolean initialRun = true;
        isDoneIterating = false;
        start_iter = iter;
        resetBacktrackTasks();
        reset_stats();
        boolean resetAfterInitial = isDone();
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
            summarizeIteration();
            if (resetAfterInitial) {
                resetAfterInitial = false;
                GlobalData.getCoverage().resetCoverage();
            }
        }
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
                if (newDepth < startDepth) {
                    newDepth = 0;
                }
                if (newDepth == 0) {
                    for (Machine machine : schedule.getMachines()) {
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

    private PrimitiveVS getNext(int depth, int bound, Function<Integer, PrimitiveVS> getRepeat, Function<Integer, List> getBacktrack,
                           Consumer<Integer> clearBacktrack, BiConsumer<PrimitiveVS, Integer> addRepeat,
                           BiConsumer<List, Integer> addBacktrack, Supplier<List> getChoices,
                           Function<List, PrimitiveVS> generateNext, boolean isData) {
        List<PrimitiveVS> choices = new ArrayList();
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
            if (configuration.isUseRandom()) {
                Collections.shuffle(choices, new Random(RandomNumberGenerator.getInstance().getRandomLong()));
            }
//            Collections.sort(choices, new SortVS());
//            SearchLogger.log("\t#Choices = " + choices.size());
//            for (PrimitiveVS vs: choices) {
//                SearchLogger.log("\t\t" + vs);
//            }
        }

        List<PrimitiveVS> chosen = new ArrayList();
        List<PrimitiveVS> backtrack = new ArrayList();
        for (int i = 0; i < choices.size(); i++) {
            if (i < bound) {
                chosen.add(choices.get(i));
            } else {
                backtrack.add(choices.get(i));
            }
        }
        GlobalData.getCoverage().updateDepthCoverage(getDepth(), getChoiceDepth(), chosen.size(), backtrack.size(), isData, isNewChoice);

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
        PrimitiveVS<Boolean> res = getNext(depth, configuration.getInputChoiceBound(), schedule::getRepeatBool, schedule::getBacktrackBool,
                schedule::clearBacktrack, schedule::addRepeatBool, schedule::addBacktrackBool,
                () -> super.getNextBooleanChoices(pc), super::getNextBoolean, true);
        choiceDepth = depth + 1;
        return res;
    }

    @Override
    public PrimitiveVS<Integer> getNextInteger(PrimitiveVS<Integer> bound, Guard pc) {
        int depth = choiceDepth;
        PrimitiveVS<Integer> res = getNext(depth, configuration.getInputChoiceBound(), schedule::getRepeatInt, schedule::getBacktrackInt,
                schedule::clearBacktrack, schedule::addRepeatInt, schedule::addBacktrackInt,
                () -> super.getNextIntegerChoices(bound, pc), super::getNextInteger, true);
        choiceDepth = depth + 1;
        return res;
    }

    @Override
    public ValueSummary getNextElement(ListVS<? extends ValueSummary> candidates, Guard pc) {
        int depth = choiceDepth;
        PrimitiveVS<ValueSummary> res = getNext(depth, configuration.getInputChoiceBound(), schedule::getRepeatElement, schedule::getBacktrackElement,
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
            assert(allocated.getValues().size() == 1);
            TraceLogger.onCreateMachine(pc, allocated.getValues().iterator().next());
            allocated.getValues().iterator().next().setScheduler(this);
            machines.add(allocated.getValues().iterator().next());
        }
        else {
            Machine newMachine;
            newMachine = constructor.apply(IntegerVS.maxValue(guardedCount));

            if (!machines.contains(newMachine)) {
                machines.add(newMachine);
            }

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
