package psymbolic.runtime.scheduler;

import lombok.Setter;
import psymbolic.commandline.*;
import psymbolic.runtime.*;
import psymbolic.runtime.logger.TraceLogger;
import psymbolic.runtime.logger.SearchLogger;
import psymbolic.runtime.machine.Machine;
import psymbolic.runtime.machine.Monitor;
import psymbolic.runtime.statistics.SearchStats;
import psymbolic.runtime.statistics.SolverStats;
import psymbolic.valuesummary.*;
import psymbolic.valuesummary.solvers.SolverEngine;
import psymbolic.runtime.machine.buffer.*;
import psymbolic.runtime.logger.StatLogger;
import psymbolic.valuesummary.solvers.SolverType;

import java.time.Duration;
import java.time.Instant;
import java.util.*;
import java.util.function.Function;
import java.util.stream.Collectors;


public class Scheduler implements SymbolicSearch {

    protected SearchStats searchStats = new SearchStats();

    /** The scheduling choices made */
    public final Schedule schedule;

    /** The result of the current run */
    public String result;

    @Setter
    PSymConfiguration configuration;

    /** List of all machines along any path constraints */
    final List<Machine> machines;

    /** How many instances of each Machine there are */
    final Map<Class<? extends Machine>, PrimitiveVS<Integer>> machineCounters;

    /** The machine to start with */
    private Machine start;

    /** The map from events to listening monitors */
    private Map<Event, List<Monitor>> listeners;

    /** List of monitors instances */
    List<Monitor> monitors;

    /** Vector clock manager */
    private VectorClockManager vcManager;

    /** Use the interleave map (if false) or not (if true) */
    private boolean useFilters() { return configuration.isUseFilters(); }

    /** Get whether to intersect with receiver queue semantics
     * @return whether to intersect with receiver queue semantics
     */
    public boolean useReceiverSemantics() { return configuration.isUseReceiverQueueSemantics(); }

    /** Get whether to use bag semantics
     * @return whether to use bag semantics
     */
    public boolean useBagSemantics() { return configuration.isUseBagSemantics(); }

    /** Get whether to use sleep sets
     * @return whether to use sleep sets
     */
    public boolean useSleepSets() { return configuration.isUseSleepSets(); }

    /** Current depth of exploration */
    private int depth = 0;
    /** Whether or not search is done */
    private boolean done = false;

    int choiceDepth = 0;

    /** Start depth at which create machine events are already explored */
    int startDepth = Integer.MAX_VALUE;

    private List<List<List<ValueSummary>>> prevStates = new ArrayList<>();


    List<List<ValueSummary>> srcState = new ArrayList<>();

    private List<Integer> totalStates = new ArrayList<>();

    public int getTotalStates() {
        int result = 0;
        for (Integer i: totalStates) {
            result += i;
        }
        return result;
    }

    /** Reset scheduler state
     */
    public void reset() {
        depth = 0;
        choiceDepth = 0;
        done = false;
        machineCounters.clear();
        machines.clear();
        totalStates.clear();
    }

    /** Restore scheduler state
     */
    public void restore(int d, int cd) {
        depth = d;
        choiceDepth = cd;
        done = false;
    }

    /** Return scheduler's VC manager
        @return the scheduler's current vector clock manager
     */
    public VectorClockManager getVcManager() {
        return vcManager;
    }

    /** Find out whether symbolic execution is finished
     * @return Whether or not there are more steps to run
     */
    public boolean isDone() {
        return done || depth == configuration.getDepthBound();
    }

    /** Get current depth
     * @return current depth
     */
    public int getDepth() { return depth; }

    /** Get current choice depth
     * @return current choice depth
     */
    public int getChoiceDepth() { return choiceDepth; }

    /** Make new schedule
     * @return A new Schedule instance */
    public Schedule getNewSchedule() {
        return new Schedule(this.useSleepSets());
    }

    /** Get the schedule
     * @return The schedule
     */
    public Schedule getSchedule() { return schedule; }

    /** Make a new Scheduler
     * @param machines The machines initially in the Scheduler
     */
    public Scheduler(PSymConfiguration config, Machine... machines) {
        setConfiguration(config);
        this.schedule = getNewSchedule();
        this.machines = new ArrayList<>();
        this.machineCounters = new HashMap<>();
        this.vcManager = new VectorClockManager(useReceiverSemantics() || configuration.isDpor() || useSleepSets());

        for (Machine machine : machines) {
            this.machines.add(machine);
            if (this.machineCounters.containsKey(machine.getClass())) {
                this.machineCounters.put(machine.getClass(),
                        IntegerVS.add(this.machineCounters.get(machine.getClass()), 1));
            } else {
                this.machineCounters.put(machine.getClass(), new PrimitiveVS<>(1));
            }
            TraceLogger.onCreateMachine(Guard.constTrue(), machine);
            machine.setScheduler(this);
            schedule.makeMachine(machine, Guard.constTrue());
            vcManager.addMachine(Guard.constTrue(), machine);
        }
    }

    public List<PrimitiveVS> getNextIntegerChoices(PrimitiveVS<Integer> bound, Guard pc) {
        List<PrimitiveVS> choices = new ArrayList<>();
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
            list.add(candidates.get(index).restrict(pc));
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

    /** Start execution with the specified machine
     * @param machine Machine to start execution with */
    public void startWith(Machine machine) {
        if (this.machineCounters.containsKey(machine.getClass())) {
            this.machineCounters.put(machine.getClass(),
                    IntegerVS.add(this.machineCounters.get(machine.getClass()), 1));
        } else {
            this.machineCounters.put(machine.getClass(), new PrimitiveVS<>(1));
        }

        machines.add(machine);
        start = machine;
        TraceLogger.onCreateMachine(Guard.constTrue(), machine);
        machine.setScheduler(this);
        schedule.makeMachine(machine, Guard.constTrue());
        if (vcManager.hasIdx(new PrimitiveVS<>(machine)).isFalse())
            vcManager.addMachine(Guard.constTrue(), machine);

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

    public void initializeSearch(Program p) {
        listeners = p.getMonitorMap();
        monitors = new ArrayList<>(p.getMonitorList());
        for (Machine m : p.getMonitorList()) {
            startWith(m);
        }
        Machine target = p.getStart();
        startWith(target);
        start = target;
    }

    public void restoreState(List<List<ValueSummary>> state) {
        int idx = 0;
        for (Machine machine : machines) {
            List<ValueSummary> machineLocalState = state.get(idx++);
            machine.setLocalState(machineLocalState);
        }
        for (Monitor machine : monitors) {
            List<ValueSummary> machineLocalState = state.get(idx++);
            machine.setLocalState(machineLocalState);
        }
    }

    public void restoreStringState(List<List<String>> state) {
        int idx = 0;
        for (Machine machine : machines) {
            List<String> machineStringState = state.get(idx++);
            List<ValueSummary> machineLocalState = new ArrayList<>();
            for (String s: machineStringState) {
                machineLocalState.add((ValueSummary) SerializeObject.objectFromString(s));
            }
            machine.setLocalState(machineLocalState);
        }
        for (Monitor machine : monitors) {
            List<String> machineStringState = state.get(idx++);
            List<ValueSummary> machineLocalState = new ArrayList<>();
            for (String s: machineStringState) {
                machineLocalState.add((ValueSummary) SerializeObject.objectFromString(s));
            }
            machine.setLocalState(machineLocalState);
        }
    }

    @Override
    public void doSearch(Program p) {
        initializeSearch(p);
        while (!isDone()) {
            // ScheduleLogger.log("step " + depth + ", true queries " + Guard.trueQueries + ", false queries " + Guard.falseQueries);
            Assert.prop(depth < configuration.getMaxDepthBound(), "Maximum allowed depth " + configuration.getMaxDepthBound() + " exceeded", this, schedule.getLengthCond(schedule.size()));
            step();
        }
    }

    @Override
    public void resumeSearch(Program p) {
        while (!isDone()) {
            // ScheduleLogger.log("step " + depth + ", true queries " + Guard.trueQueries + ", false queries " + Guard.falseQueries);
            Assert.prop(depth < configuration.getMaxDepthBound(), "Maximum allowed depth " + configuration.getMaxDepthBound() + " exceeded", this, schedule.getLengthCond(schedule.size()));
            step();
        }
    }

    public void print_stats() {
        // print statistics
        StatLogger.log(String.format("result:\t%s", result));
        if (configuration.getCollectStats() != 0) {
            Instant end = Instant.now();
            Runtime runtime = Runtime.getRuntime();
            double timeUsed = (Duration.between(EntryPoint.start, end).toMillis() / 1000.0);
            double memoryUsed = ((runtime.totalMemory() - runtime.freeMemory()) / 1000000.0);
            StatLogger.log(String.format("time-seconds:\t%.1f", timeUsed));
            StatLogger.log(String.format("memory-max-MB:\t%.1f", SolverStats.maxMemSpent));
            StatLogger.log(String.format("memory-current-MB:\t%.1f", memoryUsed));
            StatLogger.log(String.format("time-create-guards-%%:\t%.1f", SolverStats.getDoublePercent(SolverStats.timeTotalCreateGuards/1000.0, timeUsed)));
            StatLogger.log(String.format("time-solve-guards-%%:\t%.1f", SolverStats.getDoublePercent(SolverStats.timeTotalSolveGuards/1000.0, timeUsed)));
            StatLogger.log(String.format("time-create-guards-max-seconds:\t%.3f", SolverStats.timeMaxCreateGuards/1000.0));
            StatLogger.log(String.format("time-solve-guards-max-seconds:\t%.3f", SolverStats.timeMaxSolveGuards/1000.0));
            StatLogger.log(String.format("depth:\t%d", getDepth()));
            StatLogger.logSolverStats();

            if (configuration.getCollectStats() > 2) {
                StatLogger.log(String.format("#-states:\t%d", getTotalStates()));
                StatLogger.log(String.format("#-events:\t%d", searchStats.getSearchTotal().getNumOfTransitions()));
                StatLogger.log(String.format("#-events-merged:\t%d", searchStats.getSearchTotal().getNumOfMergedTransitions()));
                StatLogger.log(String.format("#-events-explored:\t%d", searchStats.getSearchTotal().getNumOfTransitionsExplored()));
            }
        }
    }
    
    public List<PrimitiveVS> getNextSenderChoices() {
        // prioritize the create actions
        for (Machine machine : machines) {
            if (!machine.sendBuffer.isEmpty()) {
                Guard initCond = machine.sendBuffer.hasCreateMachineUnderGuard().getGuardFor(true);
                if (!initCond.isFalse()) {
                    PrimitiveVS<Machine> ret = new PrimitiveVS<>(machine).restrict(initCond);
                    return new ArrayList<>(Arrays.asList(ret));
                }
            }
        }

        if (startDepth > getDepth()) {
            startDepth = getDepth();
            TraceLogger.logMessage("Increasing start depth to " + startDepth);
        }

        // prioritize the sync actions i.e. events that are marked as synchronous
        for (Machine machine : machines) {
            if (!machine.sendBuffer.isEmpty()) {
                Guard syncCond = machine.sendBuffer.hasSyncEventUnderGuard().getGuardFor(true);
                if (!syncCond.isFalse()) {
                    PrimitiveVS<Machine> ret = new PrimitiveVS<>(machine).restrict(syncCond);
                    return new ArrayList<>(Arrays.asList(ret));
                }
            }
        }

        // now there are no create machine and sync event actions remaining
        List<GuardedValue<Machine>> guardedMachines = new ArrayList<>();

        for (Machine machine : machines) {
            if (!machine.sendBuffer.isEmpty()) {
                Guard canRun = machine.hasHalted().getGuardFor(true).not();
                canRun = canRun.and(machine.sendBuffer.satisfiesPredUnderGuard(x -> x.canRun()).getGuardFor(true));
                if (!canRun.isFalse()) {
                    guardedMachines.add(new GuardedValue(machine, canRun));
 //                   candidateSenders.add(new PrimitiveVS<>(machine).restrict(canRun));
                }
            }
        }
  //      return candidateSenders;

        if (useReceiverSemantics()) {
            guardedMachines = filter(guardedMachines, ReceiverQueueOrder.getInstance());
        }

        if (useFilters()) {
            guardedMachines = filter(guardedMachines, InterleaveOrder.getInstance());
        }

        List<PrimitiveVS> candidateSenders = new ArrayList<>();
        for (GuardedValue<Machine> guardedValue : guardedMachines) {
            candidateSenders.add(new PrimitiveVS<>(guardedValue.getValue()).restrict(guardedValue.getGuard()));
        }
        candidateSenders = getSchedule().filterSleep(candidateSenders);
        return candidateSenders;
    }

    private Message peekBuffer(Machine m, Guard g) {
        return m.sendBuffer.peek(g);
    }

    private Message rmBuffer(Machine m, Guard g) {
        return m.sendBuffer.remove(g);
    }

    private List<GuardedValue<Machine>> filter(List<GuardedValue<Machine>> choices, MessageOrder order) {
        Map<Machine, Guard> filteredMap = new HashMap<>();
        Map<Machine, Message> firstElement = new HashMap<>();
        for (GuardedValue<Machine> choice : choices) {
            Machine currentMachine = choice.getValue();
            Message current = peekBuffer(currentMachine, choice.getGuard());
            Guard add = choice.getGuard();
            List<Message> remove = new ArrayList<>();
            Map<Machine, Guard> newFilteredMap = new HashMap<>();
            for (Machine oldMachine : filteredMap.keySet()) {
                Message old = firstElement.get(oldMachine);
                add = add.and(order.lessThan(old, current).not());
            }
            for (Machine oldMachine : filteredMap.keySet()) {
                Message old = firstElement.get(oldMachine);
                Guard remCond = order.lessThan(current, old).and(add);
                newFilteredMap.put(oldMachine, filteredMap.get(oldMachine).and(remCond.not()));
                firstElement.put(oldMachine, firstElement.get(oldMachine).restrict(remCond.not()));
            }
            newFilteredMap.put(currentMachine, add);
            firstElement.put(currentMachine, current.restrict(add));
            filteredMap = newFilteredMap;
        }
        List<GuardedValue<Machine>> filtered = new ArrayList<>();
        for (Map.Entry<Machine,Guard> entry : filteredMap.entrySet()) {
            if (!entry.getValue().isFalse())
                filtered.add(new GuardedValue(entry.getKey(), entry.getValue()));
        }
        return filtered;
    }

/*
    private PrimitiveVS<Boolean> shouldInterleave(List<Message> candidateMessages, Message m) {
        if (alwaysInterleaveNonAsync) return new PrimitiveVS<>(true);
        PrimitiveVS<Event> event = m.getEvent();
        PrimitiveVS<Set<Event>> doNotInterleave = new PrimitiveVS<>();
        List<PrimitiveVS<Set<Event>>> toMerge = new ArrayList<>();
        for (GuardedValue<Event> e : event.getGuardedValues()) {
            if (interleaveMap.containsKey(e.getValue())) {
                toMerge.add(new PrimitiveVS<>(interleaveMap.get(e.getValue())));
            }
        }
        doNotInterleave = doNotInterleave.merge(toMerge);

        Guard equal = Guard.constFalse();
        for (Message other : candidateMessages) {
            for (GuardedValue<Event> e : other.getEvent().getGuardedValues()) {
                for (GuardedValue<Set<Event>> notToInterleave : doNotInterleave.getGuardedValues()) {
                    for (Event replacement : notToInterleave.getValue()) {
                        if (e.getValue().equals(replacement)) {
                            equal = equal.or(e.getGuard().and(notToInterleave.getGuard()));
                        }
                    }
                }
            }
        }
        return new PrimitiveVS<>(true).restrict(equal.not());
    }
*/

    public PrimitiveVS<Machine> getNextSender(List<PrimitiveVS> candidateSenders) {
        PrimitiveVS<Machine> choices = (PrimitiveVS<Machine>) NondetUtil.getNondetChoice(candidateSenders);
        schedule.addRepeatSender(choices, choiceDepth);
        choiceDepth++;
        return choices;
    }

    public PrimitiveVS<Machine> getNextSender() {
        return getNextSender(getNextSenderChoices());
    }

    private void recordResult() {
        int numBacktracks = schedule.getNumBacktracks();
        if (numBacktracks > 0) {
            result = "partially safe with " + numBacktracks + " backtracks remaining";
        } else {
            if (done) {
                result = "safe for any depth";
            } else {
                result = "safe until depth " + getDepth();
            }
        }
    }

    public void step() {
        srcState = new ArrayList<>();
        int numStates = 1;
        for (Machine machine : machines) {
            List<ValueSummary> machineLocalState = machine.getLocalState();
            srcState.add(machineLocalState);
        }
        for (Monitor machine : monitors) {
            List<ValueSummary> machineLocalState = machine.getLocalState();
            srcState.add(machineLocalState);
        }
        if (configuration.getCollectStats() > 2) {
            List<ValueSummary> flatState = new ArrayList<>();
            for (List<ValueSummary> machineState: srcState) {
                for (ValueSummary vs: machineState) {
                    flatState.add(vs);
                }
            }
            numStates = Concretizer.getNumConcreteValues(false, Guard.constTrue(), flatState.toArray(new ValueSummary[0]));
        }
        prevStates.add(srcState);
        totalStates.add(numStates);

        schedule.setSchedulerDepth(getDepth());
        schedule.setSchedulerChoiceDepth(getChoiceDepth());
        schedule.setSchedulerState(srcState);
//        schedule.setSchedulerStringState(srcState);

        PrimitiveVS<Machine> choices = getNextSender();

        if (choices.isEmptyVS()) {
//            TraceLogger.finished(depth);
            done = true;
        }

        recordResult();

        if (done) {
            return;
        }

        Message effect = null;
        List<Message> effects = new ArrayList<>();
        int numMessages = 0;
        for (GuardedValue<Machine> sender : choices.getGuardedValues()) {
            Machine machine = sender.getValue();
            Guard guard = sender.getGuard();
            Message removed = rmBuffer(machine, guard);
            if (configuration.getVerbosity() > 3) {
                System.out.println("\tMachine " + machine.toString());
                System.out.println("\t  state   " + machine.getCurrentState().toStringDetailed());
                System.out.println("\t  message " + removed.toString());
                System.out.println("\t  target " + removed.getTarget().toString());
            }
            if (configuration.getCollectStats() > 2) {
                numMessages += Concretizer.getNumConcreteValues(false, Guard.constTrue(), removed);
            }
            if (effect == null) {
                effect = removed;
            } else {
                effects.add(removed);
            }
        }
        assert effect != null;
        effect = effect.merge(effects);
        TraceLogger.schedule(depth, effect, choices);

        performEffect(effect);

        List<List<ValueSummary>> newStates = new ArrayList<>();
        int numStates = 1;
        for (Machine machine : machines) {
            List<ValueSummary> machineLocalState = machine.getLocalState();
            newStates.add(machineLocalState);
        }
        if (configuration.getCollectStats() > 2) {
            List<ValueSummary> flatState = new ArrayList<>();
            for (List<ValueSummary> machineState: newStates) {
                for (ValueSummary vs: machineState) {
                    flatState.add(vs);
                }
            }
            numStates = Concretizer.getNumConcreteValues(false, Guard.constTrue(), flatState.toArray(new ValueSummary[0]));
        }

        prevStates.add(newStates);
        totalStates.add(numStates);

>>>>>>> e2a21d64e (Restore PSym to without fp)

=======
>>>>>>> 7627dce6d (Adds setting machine's state)
        // simplify engine
//        SolverEngine.simplifyEngineAuto();

        // switch engine
//        SolverEngine.switchEngineAuto();

        // performing node clean-up
        SolverEngine.cleanupEngine();
        System.gc();

        if (configuration.getCollectStats() != 0) {
            double timeUsed = SolverStats.getTime();
            double memoryUsed = SolverStats.getMemory();
            if (configuration.getCollectStats() > 1) {
                System.out.println("--------------------");
                System.out.println("Resource Stats::");
                System.out.println(String.format("time-seconds:\t%.1f", timeUsed));
                System.out.println(String.format("memory-max-MB:\t%.1f", SolverStats.maxMemSpent));
                System.out.println(String.format("memory-current-MB:\t%.1f", memoryUsed));
                System.out.println("--------------------");
                System.out.println("Solver Stats::");
                System.out.println(String.format("time-create-guards-%%:\t%.1f", SolverStats.getDoublePercent(SolverStats.timeTotalCreateGuards/1000.0, timeUsed)));
                System.out.println(String.format("time-solve-guards-%%:\t%.1f", SolverStats.getDoublePercent(SolverStats.timeTotalSolveGuards/1000.0, timeUsed)));
                System.out.println(String.format("time-create-guards-max-seconds:\t%.3f", SolverStats.timeMaxCreateGuards/1000.0));
                System.out.println(String.format("time-solve-guards-max-seconds:\t%.3f", SolverStats.timeMaxSolveGuards/1000.0));
                System.out.println(SolverStats.prettyPrint());
                System.out.println("--------------------");
                System.out.println("Detailed Solver Stats::\n" + SolverEngine.getStats());
                System.out.println("--------------------");
            }
        }

        // add depth statistics
        if (configuration.getCollectStats() > 2) {
          SearchStats.DepthStats depthStats = new SearchStats.DepthStats(depth, numMessages, Concretizer.getNumConcreteValues(false, Guard.constTrue(), effect), Concretizer.getNumConcreteValues(false, Guard.constTrue(), effect.getTarget(), effect.getEvent()));
          searchStats.addDepthStatistics(depth, depthStats);
          SearchLogger.logDepthStats(depthStats);
          System.out.println("--------------------");
          System.out.println("Collect Stats::");
          System.out.println("Total States:: " + numStates + ", Running Total States::" + getTotalStates());
          System.out.println("Total transitions:: " + depthStats.getNumOfTransitions() + ", Total Merged Transitions (merged same target):: " + depthStats.getNumOfMergedTransitions() + ", Total Transitions Explored:: " + depthStats.getNumOfTransitionsExplored());
          System.out.println("Running Total Transitions:: " + searchStats.getSearchTotal().getNumOfTransitions() + ", Running Total Merged Transitions:: " + searchStats.getSearchTotal().getNumOfMergedTransitions() + ", Running Total Transitions Explored:: " + searchStats.getSearchTotal().getNumOfTransitionsExplored());
          System.out.println("--------------------");
        }

        depth++;
    }

    public PrimitiveVS<Machine> allocateMachine(Guard pc, Class<? extends Machine> machineType,
                                           Function<Integer, ? extends Machine> constructor) {
        if (!machineCounters.containsKey(machineType)) {
            machineCounters.put(machineType, new PrimitiveVS<>(0));
        }
        PrimitiveVS<Integer> guardedCount = machineCounters.get(machineType).restrict(pc);

        Machine newMachine;
        newMachine = constructor.apply(IntegerVS.maxValue(guardedCount));

        if (!machines.contains(newMachine)) {
            machines.add(newMachine);
            vcManager.addMachine(pc, newMachine);
        }

        TraceLogger.onCreateMachine(pc, newMachine);
        newMachine.setScheduler(this);
        if (useBagSemantics()) {
            newMachine.setSemantics(EventBufferSemantics.bag);
        }
        else if (useReceiverSemantics()) {
            newMachine.setSemantics(EventBufferSemantics.receiver);
        }
        schedule.makeMachine(newMachine, pc);

        guardedCount = IntegerVS.add(guardedCount, 1);
        PrimitiveVS<Integer> mergedCount = machineCounters.get(machineType).updateUnderGuard(pc, guardedCount);
        machineCounters.put(machineType, mergedCount);
        return new PrimitiveVS<>(newMachine).restrict(pc);
    }

    void runMonitors(Message event) {
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
        for (Monitor m: monitors) {
            Guard constraint = monitorConstraints.get(m);
            if (!constraint.isFalse()) {
                m.processEventToCompletion(constraint, event.restrict(constraint));
            }
        }
    }

    void performEffect(Message event) {
        runMonitors(event);
        for (GuardedValue<Machine> target : event.getTarget().getGuardedValues()) {
            target.getValue().processEventToCompletion(target.getGuard(), event.restrict(target.getGuard()));
        }
    }

    public void announce(PrimitiveVS<Event> names, UnionVS payload) {
        Message event = new Message(names, new PrimitiveVS<>(), payload);
        runMonitors(event);
    }

    public int getMaxInternalSteps() {
        return configuration.getMaxInternalSteps();
    }
}
