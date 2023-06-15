package psym.runtime.scheduler;

import lombok.Getter;
import lombok.Setter;
import psym.commandline.Assert;
import psym.commandline.PSymConfiguration;
import psym.commandline.Program;
import psym.runtime.*;
import psym.runtime.logger.PSymLogger;
import psym.runtime.logger.SearchLogger;
import psym.runtime.logger.StatWriter;
import psym.runtime.logger.TraceLogger;
import psym.runtime.machine.Machine;
import psym.runtime.machine.Monitor;
import psym.runtime.machine.State;
import psym.runtime.statistics.SearchStats;
import psym.runtime.statistics.SolverStats;
import psym.utils.GlobalData;
import psym.utils.MemoryMonitor;
import psym.utils.StateCachingMode;
import psym.utils.TimeMonitor;
import psym.valuesummary.*;
import psym.valuesummary.solvers.SolverEngine;

import java.time.Duration;
import java.time.Instant;
import java.util.*;
import java.util.concurrent.TimeoutException;
import java.util.function.Function;
import java.util.stream.Collectors;


public class Scheduler implements SymbolicSearch {
    @Getter
    /** List of all machines along any path constraints */
    final List<Machine> machines;
    /**
     * Set of machines along current schedule
     */
    final SortedSet<Machine> currentMachines;
    /**
     * Program
     */
    @Getter
    private final Program program;
    /**
     * The scheduling choices made
     */
    public Schedule schedule;
    /**
     * Result of the search
     */
    public String result;
    /**
     * Whether final result is set or not
     */
    public boolean isFinalResult = false;
    /**
     * Search statistics
     */
    protected final SearchStats searchStats = new SearchStats();
    /**
     * How many instances of each Machine there are
     */
    protected Map<Class<? extends Machine>, PrimitiveVS<Integer>> machineCounters;
    /**
     * Whether or not search is done
     */
    protected boolean done = false;
    /**
     * Iteration number
     */
    @Getter
    int iter = 0;
    /**
     * Start iteration number
     */
    @Getter
    int start_iter = 0;
    @Setter
    transient PSymConfiguration configuration;
    /**
     * List of monitors instances
     */
    List<Monitor> monitors;
    /**
     * Choice depth
     */
    int choiceDepth = 0;
    /**
     * Backtrack choice depth
     */
    int backtrackDepth = 0;
    /**
     * Starting choice depth from previous iteration, i.e., corresponding to srcState
     */
    int preChoiceDepth = Integer.MAX_VALUE;
    /**
     * Source state at the beginning of each schedule step
     */
    transient Map<Machine, List<ValueSummary>> srcState = new HashMap<>();
    /**
     * The machine to start with
     */
    private Machine start;
    /**
     * The map from events to listening monitors
     */
    private Map<Event, List<Monitor>> listeners;
    /**
     * Current depth of exploration
     */
    private int depth = 0;
    /**
     * Flag whether current step is a create or sync machine step
     */
    private Boolean stickyStep = false;
    /**
     * Flag whether current execution finished
     */
    private Boolean executionFinished = false;
    /**
     * Map of distinct concrete state to number of times state is visited
     */
    transient private Set<Object> distinctStates = new HashSet<>();
    /**
     * Guard corresponding on distinct states at a step
     */
    transient private Guard distinctStateGuard = null;
    /**
     * Total number of states
     */
    private int totalStateCount = 0;
    /**
     * Total number of distinct states
     */
    private int totalDistinctStateCount = 0;

    /**
     * Make a new Scheduler
     *
     * @param machines The machines initially in the Scheduler
     */
    public Scheduler(PSymConfiguration config, Program p, Machine... machines) {
        setConfiguration(config);
        program = p;
        this.schedule = getNewSchedule();
        this.machines = new ArrayList<>();
        this.currentMachines = new TreeSet<>();
        this.machineCounters = new HashMap<>();

        for (Machine machine : machines) {
            this.machines.add(machine);
            this.currentMachines.add(machine);
            if (this.machineCounters.containsKey(machine.getClass())) {
                this.machineCounters.put(machine.getClass(),
                        IntegerVS.add(this.machineCounters.get(machine.getClass()), 1));
            } else {
                this.machineCounters.put(machine.getClass(), new PrimitiveVS<>(1));
            }
            TraceLogger.onCreateMachine(Guard.constTrue(), machine);
            machine.setScheduler(this);
            schedule.makeMachine(machine, Guard.constTrue());
        }
    }

    public static void cleanup() {
        SolverEngine.cleanupEngine();
    }

    public int getTotalStates() {
        return totalStateCount;
    }

    public int getTotalDistinctStates() {
        return totalDistinctStateCount;
    }

    /**
     * Reset scheduler state
     */
    public void reset() {
        depth = 0;
        choiceDepth = 0;
        preChoiceDepth = Integer.MAX_VALUE;
        done = false;
        machineCounters.clear();
//        machines.clear();
        currentMachines.clear();
        srcState.clear();
        schedule.setSchedulerDepth(getDepth());
        schedule.setSchedulerChoiceDepth(getChoiceDepth());
        schedule.setSchedulerState(srcState, machineCounters);
        GlobalData.getSymmetryTracker().reset();
    }

    /**
     * Reinitialize scheduler
     */
    public void reinitialize() {
        // set all transient data structures
        srcState = new HashMap<>();
        distinctStates = new HashSet<>();
        distinctStateGuard = null;
        for (Machine machine : schedule.getMachines()) {
            machine.setScheduler(this);
        }
    }

    /**
     * Restore scheduler state
     */
    public void restore(int d, int cd) {
        depth = d;
        choiceDepth = cd;
        preChoiceDepth = Integer.MAX_VALUE;
        done = false;
    }

    /**
     * Find out whether symbolic execution is done
     *
     * @return Whether or not there are more steps to run
     */
    public boolean isDone() {
        return done || depth == configuration.getMaxStepBound();
    }

    /**
     * Find out whether current execution finished completely
     *
     * @return Whether or not current execution finished
     */
    public boolean isFinishedExecution() {
        return executionFinished || depth == configuration.getMaxStepBound();
    }

    /**
     * Get current depth
     *
     * @return current depth
     */
    public int getDepth() {
        return depth;
    }

    /**
     * Get current choice depth
     *
     * @return current choice depth
     */
    public int getChoiceDepth() {
        return choiceDepth;
    }

    /**
     * Make new schedule
     *
     * @return A new Schedule instance
     */
    public Schedule getNewSchedule() {
        return new Schedule();
    }

    /**
     * Get the schedule
     *
     * @return The schedule
     */
    public Schedule getSchedule() {
        return schedule;
    }

    public List<PrimitiveVS> getNextIntegerChoices(PrimitiveVS<Integer> bound, Guard pc) {
        List<PrimitiveVS> choices = new ArrayList<>();
        Guard zeroGuard = bound.getGuardFor(0);
        if (!zeroGuard.isFalse()) {
            bound = bound.updateUnderGuard(zeroGuard, new PrimitiveVS<Integer>(1));
        }
        for (int i = 0; i < IntegerVS.maxValue(bound); i++) {
            Guard cond = IntegerVS.lessThan(i, bound).getGuardFor(true);
            choices.add(new PrimitiveVS<>(i).restrict(cond).restrict(pc));
        }
        return choices;
    }

    public PrimitiveVS<Integer> getNextInteger(List<PrimitiveVS> candidateIntegers) {
        PrimitiveVS<Integer> choices = (PrimitiveVS<Integer>) NondetUtil.getNondetChoice(candidateIntegers);
        schedule.addRepeatInt(choices, choiceDepth);
        choiceDepth++;
        return choices;
    }

    @Override
    public PrimitiveVS<Integer> getNextInteger(PrimitiveVS<Integer> bound, Guard pc) {
        return getNextInteger(getNextIntegerChoices(bound, pc));
    }

    public List<PrimitiveVS> getNextBooleanChoices(Guard pc) {
        List<PrimitiveVS> choices = new ArrayList<>();
        choices.add(new PrimitiveVS<>(true).restrict(pc));
        choices.add(new PrimitiveVS<>(false).restrict(pc));
        return choices;
    }

    public PrimitiveVS<Boolean> getNextBoolean(List<PrimitiveVS> candidateBooleans) {
        PrimitiveVS<Boolean> choices = (PrimitiveVS<Boolean>) NondetUtil.getNondetChoice(candidateBooleans);
        schedule.addRepeatBool(choices, choiceDepth);
        choiceDepth++;
        return choices;
    }

    @Override
    public PrimitiveVS<Boolean> getNextBoolean(Guard pc) {
        return getNextBoolean(getNextBooleanChoices(pc));
    }

    public List<ValueSummary> getNextElementChoices(ListVS candidates, Guard pc) {
        PrimitiveVS<Integer> size = candidates.size();
        PrimitiveVS<Integer> index = new PrimitiveVS<>(0).restrict(size.getUniverse());
        List<ValueSummary> list = new ArrayList<>();
        while (BooleanVS.isEverTrue(IntegerVS.lessThan(index, size))) {
            Guard cond = BooleanVS.getTrueGuard(IntegerVS.lessThan(index, size));
            if (cond.isTrue()) {
                list.add(candidates.get(index).restrict(pc));
            } else {
                list.add(candidates.restrict(cond).get(index).restrict(pc));
            }
            index = IntegerVS.add(index, 1);
        }
        return list;
    }

    public PrimitiveVS<ValueSummary> getNextElementHelper(List<ValueSummary> candidates) {
        PrimitiveVS<ValueSummary> choices = NondetUtil.getNondetChoice(candidates.stream().map(x -> new PrimitiveVS(x).restrict(x.getUniverse())).collect(Collectors.toList()));
        schedule.addRepeatElement(choices, choiceDepth);
        choiceDepth++;
        return choices;
    }

    public ValueSummary getNextElementFlattener(PrimitiveVS<ValueSummary> choices) {
        ValueSummary flattened = null;
        List<ValueSummary> toMerge = new ArrayList<>();
        for (GuardedValue<ValueSummary> guardedValue : choices.getGuardedValues()) {
            if (flattened == null) {
                flattened = guardedValue.getValue().restrict(guardedValue.getGuard());
            } else {
                toMerge.add(guardedValue.getValue().restrict(guardedValue.getGuard()));
            }
        }
        if (flattened == null) {
            flattened = new PrimitiveVS<>();
        } else {
            flattened = flattened.merge(toMerge);
        }
        return flattened;
    }

    @Override
    public ValueSummary getNextElement(ListVS<? extends ValueSummary> s, Guard pc) {
        return getNextElementFlattener(getNextElementHelper(getNextElementChoices(s, pc)));
    }

    @Override
    public ValueSummary getNextElement(SetVS<? extends ValueSummary> s, Guard pc) {
        return getNextElement(s.getElements(), pc);
    }

    @Override
    public ValueSummary getNextElement(MapVS<?, ? extends ValueSummary, ? extends ValueSummary> s, Guard pc) {
        return getNextElement(s.getKeys(), pc);
    }

    /**
     * Start execution with the specified machine
     *
     * @param machine Machine to start execution with
     */
    public void startWith(Machine machine) {
        if (this.machineCounters.containsKey(machine.getClass())) {
            this.machineCounters.put(machine.getClass(),
                    IntegerVS.add(this.machineCounters.get(machine.getClass()), 1));
        } else {
            this.machineCounters.put(machine.getClass(), new PrimitiveVS<>(1));
        }

        machines.add(machine);
        currentMachines.add(machine);
        start = machine;
        TraceLogger.onCreateMachine(Guard.constTrue(), machine);
        machine.setScheduler(this);
        schedule.makeMachine(machine, Guard.constTrue());

        performEffect(
                new Message(
                        Event.createMachine,
                        new PrimitiveVS<>(machine),
                        null
                )
        );
    }

    public void replayStartWith(Machine machine) {
        PrimitiveVS<Machine> machineVS;
        if (this.machineCounters.containsKey(machine.getClass())) {
            machineVS = schedule.getMachine(machine.getClass(), this.machineCounters.get(machine.getClass()));
            this.machineCounters.put(machine.getClass(),
                    IntegerVS.add(this.machineCounters.get(machine.getClass()), 1));
        } else {
            machineVS = schedule.getMachine(machine.getClass(), new PrimitiveVS<>(0));
            this.machineCounters.put(machine.getClass(), new PrimitiveVS<>(1));
        }

        TraceLogger.onCreateMachine(machineVS.getUniverse(), machine);
        machine.setScheduler(this);

        performEffect(
                new Message(
                        Event.createMachine,
                        machineVS,
                        null
                )
        );
    }

    public void initializeSearch() {
        assert (getDepth() == 0);

        if (configuration.isChoiceOrchestrationLearning()) {
            GlobalData.getChoiceLearningStats().setProgramStateHash(this, configuration.getChoiceLearningStateMode(), null);
        }
        listeners = program.getListeners();
        monitors = new ArrayList<>(program.getMonitors());
        for (Machine m : program.getMonitors()) {
            startWith(m);
        }
        Machine target = program.getStart();
        startWith(target);
        start = target;
    }

    public void restoreState(Schedule.ChoiceState state) {
        assert (state != null);
        currentMachines.clear();
        for (Map.Entry<Machine, List<ValueSummary>> entry : state.getMachineStates().entrySet()) {
            entry.getKey().setLocalState(entry.getValue());
            currentMachines.add(entry.getKey());
        }
        for (Machine m : machines) {
            if (!state.getMachineStates().containsKey(m)) {
                m.reset();
            }
        }
        assert (machines.size() >= currentMachines.size());
        machineCounters = state.getMachineCounters();
    }

    @Override
    public void doSearch() throws TimeoutException, InterruptedException {
        initializeSearch();
        performSearch();
    }

    public void performSearch() throws TimeoutException {
        schedule.setNumBacktracksInSchedule();
        while (!isDone()) {
            // ScheduleLogger.log("step " + depth + ", true queries " + Guard.trueQueries + ", false queries " + Guard.falseQueries);
            Assert.prop(getDepth() < configuration.getMaxStepBound(), "Maximum allowed depth " + configuration.getMaxStepBound() + " exceeded", schedule.getLengthCond(schedule.size()));
            step();
        }
        Assert.prop(!configuration.isFailOnMaxStepBound() || (getDepth() < configuration.getMaxStepBound()), "Scheduling steps bound of " + configuration.getMaxStepBound() + " reached.", schedule.getLengthCond(schedule.size()));
        schedule.setNumBacktracksInSchedule();
        if (done) {
            searchStats.setIterationCompleted();
        }
    }

    protected void checkLiveness(boolean forceCheck) {
        if (forceCheck || isFinishedExecution()) {
            for (Monitor m : monitors) {
                PrimitiveVS<State> monitorState = m.getCurrentState().restrict(schedule.getFilter());
                for (GuardedValue<State> entry : monitorState.getGuardedValues()) {
                    State s = entry.getValue();
                    if (s.isHotState()) {
                        Guard g = entry.getGuard();
                        if (executionFinished) {
                            Assert.liveness(g.isFalse(), String.format(
                                    "Monitor %s detected liveness bug in hot state %s at the end of program execution",
                                    m, s), g);
                        } else {
                            Assert.liveness(g.isFalse(), String.format(
                                    "Monitor %s detected potential liveness bug in hot state %s",
                                    m, s), g);
                        }
                    }
                }
            }
        }
    }

    // print statistics
    public void print_stats(SearchStats.TotalStats totalStats) {
        SearchLogger.log("\n--------------------");
        Instant end = Instant.now();
        double timeUsed = (Duration.between(TimeMonitor.getInstance().getStart(), end).toMillis() / 1000.0);
        double memoryUsed = MemoryMonitor.getMemSpent();

        // print basic statistics
        StatWriter.log("result", String.format("%s", result));
        StatWriter.log("time-seconds", String.format("%.1f", timeUsed));
        StatWriter.log("memory-max-MB", String.format("%.1f", MemoryMonitor.getMaxMemSpent()));
        StatWriter.log("memory-current-MB", String.format("%.1f", memoryUsed));
        StatWriter.log("max-depth-explored", String.format("%d", totalStats.getDepthStats().getDepth()));
        SearchLogger.log(String.format("Max Depth Explored       %d", totalStats.getDepthStats().getDepth()));

        // print solver statistics
        StatWriter.log("time-create-guards-%", String.format("%.1f", SolverStats.getDoublePercent(SolverStats.timeTotalCreateGuards / 1000.0, timeUsed)));
        StatWriter.log("time-solve-guards-%", String.format("%.1f", SolverStats.getDoublePercent(SolverStats.timeTotalSolveGuards / 1000.0, timeUsed)));
        StatWriter.log("time-create-guards-seconds", String.format("%.1f", SolverStats.timeTotalCreateGuards / 1000.0));
        StatWriter.log("time-solve-guards-seconds", String.format("%.1f", SolverStats.timeTotalSolveGuards / 1000.0));
        StatWriter.log("time-create-guards-max-seconds", String.format("%.3f", SolverStats.timeMaxCreateGuards / 1000.0));
        StatWriter.log("time-solve-guards-max-seconds", String.format("%.3f", SolverStats.timeMaxSolveGuards / 1000.0));
        StatWriter.logSolverStats();

        // print search statistics
        StatWriter.log("#-states", String.format("%d", getTotalStates()));
        StatWriter.log("#-distinct-states", String.format("%d", getTotalDistinctStates()));
        StatWriter.log("#-events", String.format("%d", totalStats.getDepthStats().getNumOfTransitions()));
        StatWriter.log("#-events-merged", String.format("%d", totalStats.getDepthStats().getNumOfMergedTransitions()));
        StatWriter.log("#-events-explored", String.format("%d", totalStats.getDepthStats().getNumOfTransitionsExplored()));

        // print learn statistics
        StatWriter.log("learn-#-qstates", String.format("%d", GlobalData.getChoiceLearningStats().numQStates()));
        StatWriter.log("learn-#-qvalues", String.format("%d", GlobalData.getChoiceLearningStats().numQValues()));
    }

    public void reset_stats() {
        searchStats.reset_stats();
        distinctStates.clear();
        totalStateCount = 0;
        totalDistinctStateCount = 0;
        GlobalData.getCoverage().resetCoverage();
        if (configuration.isChoiceOrchestrationLearning()) {
            GlobalData.getChoiceLearningStats().setProgramStateHash(this, configuration.getChoiceLearningStateMode(), null);
        }
    }

    public List<PrimitiveVS> getNextSenderChoices() {
        // prioritize the create actions
        for (Machine machine : machines) {
            if (!machine.sendBuffer.isEmpty()) {
                Guard initCond = machine.sendBuffer.hasCreateMachineUnderGuard().getGuardFor(true);
                if (!initCond.isFalse()) {
                    PrimitiveVS<Machine> ret = new PrimitiveVS<>(machine).restrict(initCond);
                    return new ArrayList<>(Collections.singletonList(ret));
                }
            }
        }

        // prioritize the sync actions i.e. events that are marked as synchronous
        for (Machine machine : machines) {
            if (!machine.sendBuffer.isEmpty()) {
                Guard syncCond = machine.sendBuffer.hasSyncEventUnderGuard().getGuardFor(true);
                if (!syncCond.isFalse()) {
                    PrimitiveVS<Machine> ret = new PrimitiveVS<>(machine).restrict(syncCond);
                    return new ArrayList<>(Collections.singletonList(ret));
                }
            }
        }

        // now there are no create machine and sync event actions remaining
        List<GuardedValue<Machine>> guardedMachines = new ArrayList<>();

        for (Machine machine : machines) {
            if (!machine.sendBuffer.isEmpty()) {
                Guard canRun = machine.sendBuffer.satisfiesPredUnderGuard(x -> x.canRun()).getGuardFor(true);
                if (!canRun.isFalse()) {
                    guardedMachines.add(new GuardedValue(machine, canRun));
                }
            }
        }

        executionFinished = guardedMachines.stream().map(x -> x.getGuard().and(schedule.getFilter())).allMatch(x -> x.isFalse());

        if (configuration.getStateCachingMode() != StateCachingMode.None) {
            if (distinctStateGuard != null) {
                guardedMachines = filterDistinct(guardedMachines);
            }
        }

        List<PrimitiveVS> candidateSenders = new ArrayList<>();
        for (GuardedValue<Machine> guardedValue : guardedMachines) {
            candidateSenders.add(new PrimitiveVS<>(guardedValue.getValue()).restrict(guardedValue.getGuard()));
        }
        return candidateSenders;
    }

    private Message peekBuffer(Machine m, Guard g) {
        return m.sendBuffer.peek(g);
    }

    private Message rmBuffer(Machine m, Guard g) {
        return m.sendBuffer.remove(g);
    }

    private List<GuardedValue<Machine>> filterDistinct(List<GuardedValue<Machine>> choices) {
        assert (distinctStateGuard != null);
        List<GuardedValue<Machine>> filtered = new ArrayList<>();
        for (GuardedValue<Machine> choice : choices) {
            Machine m = choice.getValue();
            Guard g = choice.getGuard();
            Guard gNew = g.and(distinctStateGuard);
            if (!gNew.isFalse())
                filtered.add(new GuardedValue(m, gNew));
        }
        return filtered;
    }

    public PrimitiveVS<Machine> getNextSender(List<PrimitiveVS> candidateSenders) {
        PrimitiveVS<Machine> choices = (PrimitiveVS<Machine>) NondetUtil.getNondetChoice(candidateSenders);
        schedule.addRepeatSender(choices, choiceDepth);
        choiceDepth++;
        return choices;
    }

    public PrimitiveVS<Machine> getNextSender() {
        return getNextSender(getNextSenderChoices());
    }

    private void storeSrcState() {
        if (!srcState.isEmpty())
            return;
        for (Machine machine : currentMachines) {
            List<ValueSummary> machineLocalState = machine.getLocalState();
            srcState.put(machine, machineLocalState);
        }
    }

    private String globalStateString() {
        StringBuilder out = new StringBuilder();
        out.append("Src State:").append(System.lineSeparator());
        for (Machine machine : currentMachines) {
            List<ValueSummary> machineLocalState = machine.getLocalState();
            out.append(String.format("  Machine: %s", machine)).append(System.lineSeparator());
            for (ValueSummary vs : machineLocalState) {
                out.append(String.format("    %s", vs.toStringDetailed())).append(System.lineSeparator());
            }
        }
        return out.toString();
    }

    private String getConcreteStateString(List<List<Object>> concreteState) {
        StringBuilder out = new StringBuilder();
        out.append(String.format("#%d[", concreteState.size()));
//        out.append(System.lineSeparator());
        int i = 0;
        for (Machine m : currentMachines) {
            out.append("  ");
            out.append(m.toString());
            out.append(" -> ");
            out.append(concreteState.get(i).toString());
            i++;
//            out.append(System.lineSeparator());
        }
        out.append("]");
        return out.toString();
    }

    /**
     * Enumerate concrete states from explicit
     *
     * @return number of concrete states represented by the symbolic state
     */
    public int[] enumerateConcreteStatesFromExplicit(StateCachingMode mode) {
        if (configuration.getVerbosity() > 5) {
            PSymLogger.info(globalStateString());
        }

        if (stickyStep || (choiceDepth <= backtrackDepth) || (mode == StateCachingMode.None)) {
            distinctStateGuard = Guard.constTrue();
            return new int[]{0, 0};
        }

        List<List<Object>> globalStateConcrete = new ArrayList<>();
        for (Machine m : currentMachines) {
            assert (srcState.containsKey(m));
            List<ValueSummary> machineStateSymbolic = srcState.get(m);
            List<Object> machineStateConcrete = new ArrayList<>();
            for (int j = 0; j < machineStateSymbolic.size(); j++) {
                Object varValue = null;
                if (mode == StateCachingMode.Fast) {
                    varValue = machineStateSymbolic.get(j).getConcreteHash();
                } else {
                    GuardedValue<?> guardedValue = Concretizer.concretize(machineStateSymbolic.get(j));
                    if (guardedValue != null) {
                        varValue = guardedValue.getValue();
                    }
                }
                machineStateConcrete.add(varValue);
            }
            globalStateConcrete.add(machineStateConcrete);
        }

        String concreteState = globalStateConcrete.toString();
        totalStateCount += 1;
        if (distinctStates.contains(concreteState)) {
            if (configuration.getVerbosity() > 5) {
                PSymLogger.info("Repeated State: " + getConcreteStateString(globalStateConcrete));
            }
            distinctStateGuard = Guard.constFalse();
            return new int[]{1, 0};
        } else {
            if (configuration.getVerbosity() > 4) {
                PSymLogger.info("New State:      " + getConcreteStateString(globalStateConcrete));
            }
            distinctStates.add(concreteState);
            totalDistinctStateCount += 1;
            distinctStateGuard = Guard.constTrue();
            return new int[]{1, 1};
        }
    }

    /**
     * Enumerate concrete states from symbolic
     *
     * @return number of concrete states represented by the symbolic state
     */
    public int[] enumerateConcreteStatesFromSymbolic(Function<ValueSummary, GuardedValue<?>> concretizer) {
        Guard iterPc = Guard.constTrue();
        Guard alreadySeen = Guard.constFalse();
        int numConcreteStates = 0;
        int numDistinctConcreteStates = 0;

        distinctStateGuard = Guard.constFalse();
        if (stickyStep || (choiceDepth <= backtrackDepth)) {
            distinctStateGuard = Guard.constTrue();
            return new int[]{0, 0};
        }

        if (configuration.getVerbosity() > 5) {
            PSymLogger.info(globalStateString());
        }

        while (!iterPc.isFalse()) {
            Guard concreteStateGuard = Guard.constTrue();
            List<List<Object>> globalStateConcrete = new ArrayList<>();
            int i = 0;
            for (Machine m : currentMachines) {
                if (!srcState.containsKey(m))
                    continue;
                List<ValueSummary> machineStateSymbolic = srcState.get(m);
                List<Object> machineStateConcrete = new ArrayList<>();
                for (int j = 0; j < machineStateSymbolic.size(); j++) {
                    GuardedValue<?> guardedValue = concretizer.apply(machineStateSymbolic.get(j).restrict(iterPc));
                    if (guardedValue == null) {
                        if (i == 0 && j == 0) {
                            return new int[]{numConcreteStates, numDistinctConcreteStates};
                        }
                        machineStateConcrete.add(null);
                    } else {
                        iterPc = iterPc.and(guardedValue.getGuard());
                        machineStateConcrete.add(guardedValue.getValue());
                        concreteStateGuard = concreteStateGuard.and(guardedValue.getGuard());
                    }
                }
                if (!machineStateConcrete.isEmpty()) {
                    globalStateConcrete.add(machineStateConcrete);
                }
                i++;
            }

            if (!globalStateConcrete.isEmpty()) {
                totalStateCount += 1;
                numConcreteStates += 1;
                String concreteState = globalStateConcrete.toString();
                if (distinctStates.contains(concreteState)) {
                    if (configuration.getVerbosity() > 5) {
                        PSymLogger.info("Repeated State: " + getConcreteStateString(globalStateConcrete));
                    }
                } else {
                    totalDistinctStateCount += 1;
                    numDistinctConcreteStates += 1;
                    distinctStates.add(concreteState);
                    if (configuration.getStateCachingMode() != StateCachingMode.None) {
                        distinctStateGuard = distinctStateGuard.or(concreteStateGuard);
                    }
                    if (configuration.getVerbosity() > 4) {
                        PSymLogger.info("New State:      " + getConcreteStateString(globalStateConcrete));
                    }
                }
            }
            alreadySeen = alreadySeen.or(iterPc);
            iterPc = alreadySeen.not();
        }
        return new int[]{numConcreteStates, numDistinctConcreteStates};
    }

    public void step() throws TimeoutException {
        srcState.clear();

        int numStates = 0;
        int numStatesDistinct = 0;
        int numMessages = 0;
        int numMessagesMerged = 0;
        int numMessagesExplored = 0;

        if (configuration.getCollectStats() > 3 || (configuration.getStateCachingMode() != StateCachingMode.None)) {
            storeSrcState();
            int[] numConcrete;
            if (configuration.isSymbolic()) {
                numConcrete = enumerateConcreteStatesFromSymbolic(Concretizer::concretize);
            } else {
                numConcrete = enumerateConcreteStatesFromExplicit(configuration.getStateCachingMode());
            }
            numStates = numConcrete[0];
            numStatesDistinct = numConcrete[1];
        }

        if (configuration.isUseBacktrack()) {
            storeSrcState();
            schedule.setSchedulerDepth(getDepth());
            schedule.setSchedulerChoiceDepth(getChoiceDepth());
            schedule.setSchedulerState(srcState, machineCounters);
        }

        preChoiceDepth = choiceDepth;


        // remove messages with halted target
        for (Machine machine : machines) {
            while (!machine.sendBuffer.isEmpty()) {
                Guard targetHalted = machine.sendBuffer.satisfiesPredUnderGuard(x -> x.targetHalted()).getGuardFor(true);
                if (!targetHalted.isFalse()) {
                    rmBuffer(machine, targetHalted);
                    continue;
                }
                break;
            }
        }

        PrimitiveVS<Machine> choices = getNextSender();

        if (choices.isEmptyVS()) {
            done = true;
            SearchLogger.finishedExecution(depth);
        }

        if (done) {
            return;
        }

        TimeMonitor.getInstance().checkTimeout();

        if (configuration.isChoiceOrchestrationLearning()) {
            GlobalData.getChoiceLearningStats().setProgramStateHash(
                    this,
                    configuration.getChoiceLearningStateMode(),
                    choices);
        }

        Message effect = null;
        List<Message> effects = new ArrayList<>();

        for (GuardedValue<Machine> sender : choices.getGuardedValues()) {
            Machine machine = sender.getValue();
            Guard guard = sender.getGuard();
            Message removed = rmBuffer(machine, guard);
            if (configuration.getVerbosity() > 5) {
                System.out.println("  Machine " + machine);
                System.out.println("    state   " + machine.getCurrentState().toStringDetailed());
                System.out.println("    message " + removed.toString());
                System.out.println("    target " + removed.getTarget().toString());
            }
            if (configuration.getCollectStats() > 3) {
                numMessages += Concretizer.getNumConcreteValues(Guard.constTrue(), removed);
            }
            if (effect == null) {
                effect = removed;
            } else {
                effects.add(removed);
            }
        }

        if (configuration.getCollectStats() > 3) {
            numMessagesMerged = Concretizer.getNumConcreteValues(Guard.constTrue(), effect);
            numMessagesExplored = Concretizer.getNumConcreteValues(Guard.constTrue(), effect.getTarget(), effect.getEvent());
        }

        assert effect != null;
        effect = effect.merge(effects);

        stickyStep = false;
        if (effects.isEmpty()) {
            if (!effect.isCreateMachine().getGuardFor(true).isFalse() ||
                    !effect.isSyncEvent().getGuardFor(true).isFalse()) {
                stickyStep = true;
            }
        }
        if (!stickyStep) {
            depth++;
        }

        TraceLogger.schedule(depth, effect, choices);

        performEffect(effect);

        // simplify engine
//        SolverEngine.simplifyEngineAuto();

        // switch engine
//        SolverEngine.switchEngineAuto();

        double memoryUsed = MemoryMonitor.getMemSpent();
        if (memoryUsed > (0.8 * SolverStats.memLimit)) {
            Scheduler.cleanup();
        }
        SolverStats.checkResourceLimits();

        // record depth statistics
        SearchStats.DepthStats depthStats = new SearchStats.DepthStats(depth, numStates, numMessages, numMessagesMerged, numMessagesExplored);
        searchStats.addDepthStatistics(depth, depthStats);

        // log statistics
        if (configuration.getVerbosity() > 3) {
            double timeUsed = TimeMonitor.getInstance().getRuntime();
            if (configuration.getVerbosity() > 4) {
                SearchLogger.log("--------------------");
                SearchLogger.log("Resource Stats::");
                SearchLogger.log("time-seconds", String.format("%.1f", timeUsed));
                SearchLogger.log("memory-max-MB", String.format("%.1f", MemoryMonitor.getMaxMemSpent()));
                SearchLogger.log("memory-current-MB", String.format("%.1f", memoryUsed));
                SearchLogger.log("--------------------");
                SearchLogger.log("Solver Stats::");
                SearchLogger.log("time-create-guards-%", String.format("%.1f", SolverStats.getDoublePercent(SolverStats.timeTotalCreateGuards / 1000.0, timeUsed)));
                SearchLogger.log("time-solve-guards-%", String.format("%.1f", SolverStats.getDoublePercent(SolverStats.timeTotalSolveGuards / 1000.0, timeUsed)));
                SearchLogger.log("time-create-guards-max-seconds", String.format("%.3f", SolverStats.timeMaxCreateGuards / 1000.0));
                SearchLogger.log("time-solve-guards-max-seconds", String.format("%.3f", SolverStats.timeMaxSolveGuards / 1000.0));
                SolverStats.logSolverStats();
                SearchLogger.log("--------------------");
                SearchLogger.log("Detailed Solver Stats::");
                SearchLogger.log(SolverEngine.getStats());
                SearchLogger.log("--------------------");
            }
        }

        // log depth statistics
        if (configuration.getVerbosity() > 4) {
            SearchLogger.logDepthStats(depthStats);
            System.out.println("--------------------");
            System.out.println("Collect Stats::");
            System.out.println("Total States:: " + numStates + ", Running Total States::" + getTotalStates());
            System.out.println("Total Distinct States:: " + numStatesDistinct + ", Running Total Distinct States::" + getTotalDistinctStates());
            System.out.println("Total transitions:: " + depthStats.getNumOfTransitions() + ", Total Merged Transitions (merged same target):: " + depthStats.getNumOfMergedTransitions() + ", Total Transitions Explored:: " + depthStats.getNumOfTransitionsExplored());
            System.out.println("Running Total Transitions:: " + searchStats.getSearchTotal().getDepthStats().getNumOfTransitions() + ", Running Total Merged Transitions:: " + searchStats.getSearchTotal().getDepthStats().getNumOfMergedTransitions() + ", Running Total Transitions Explored:: " + searchStats.getSearchTotal().getDepthStats().getNumOfTransitionsExplored());
            System.out.println("--------------------");
        }
    }

    public Machine setupNewMachine(Guard pc, PrimitiveVS<Integer> guardedCount, Function<Integer, ? extends Machine> constructor) {
        Machine newMachine = constructor.apply(IntegerVS.maxValue(guardedCount));

        if (!machines.contains(newMachine)) {
            machines.add(newMachine);
        }
        currentMachines.add(newMachine);
        assert (machines.size() >= currentMachines.size());

        TraceLogger.onCreateMachine(pc, newMachine);
        newMachine.setScheduler(this);
        schedule.makeMachine(newMachine, pc);
        return newMachine;
    }

    public PrimitiveVS<Machine> allocateMachine(Guard pc, Class<? extends Machine> machineType,
                                                Function<Integer, ? extends Machine> constructor) {
        if (!machineCounters.containsKey(machineType)) {
            machineCounters.put(machineType, new PrimitiveVS<>(0));
        }
        PrimitiveVS<Integer> guardedCount = machineCounters.get(machineType).restrict(pc);
        Machine newMachine = setupNewMachine(pc, guardedCount, constructor);

        guardedCount = IntegerVS.add(guardedCount, 1);
        PrimitiveVS<Integer> mergedCount = machineCounters.get(machineType).updateUnderGuard(pc, guardedCount);
        machineCounters.put(machineType, mergedCount);
        return new PrimitiveVS<>(newMachine).restrict(pc);
    }

    public void runMonitors(Message event) {
        Map<Monitor, Guard> monitorConstraints = new HashMap<>();
        for (Monitor m : monitors) {
            monitorConstraints.put(m, Guard.constFalse());
        }
        for (GuardedValue<Event> e : event.getEvent().getGuardedValues()) {
            List<Monitor> listenersForEvent = listeners.get(e.getValue());
            if (listenersForEvent != null) {
                for (Monitor listener : listenersForEvent) {
                    monitorConstraints.computeIfPresent(listener, (k, v) -> v.or(e.getGuard()));
                }
            }
        }
        for (Monitor m : monitors) {
            Guard constraint = monitorConstraints.get(m);
            if (!constraint.isFalse()) {
                m.processEventToCompletion(constraint, event.restrict(constraint));
            }
        }
    }

    public void performEffect(Message event) {
        for (GuardedValue<Machine> target : event.getTarget().getGuardedValues()) {
            target.getValue().processEventToCompletion(target.getGuard(), event.restrict(target.getGuard()));
        }
    }

    public void announce(PrimitiveVS<Event> names, UnionVS payload) {
        Message event = new Message(names, new PrimitiveVS<>(), payload);
        if (event.hasNullEvent()) {
            throw new RuntimeException(String.format("Machine cannot announce a null event: %s", event));
        }
        runMonitors(event);
    }

    public int getMaxInternalSteps() {
        return configuration.getMaxInternalSteps();
    }
}
