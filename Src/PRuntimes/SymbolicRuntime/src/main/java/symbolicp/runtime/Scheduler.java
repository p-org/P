package symbolicp.runtime;

import symbolicp.bdd.Bdd;
import symbolicp.run.Assert;
import symbolicp.vs.*;

import java.util.*;
import java.util.function.Function;
import java.util.stream.Collectors;

public class Scheduler implements SymbolicSearch {

    /** The scheduling choices made */
    public final Schedule schedule;

    /** Program name */
    public final String name;

    /** List of all machines along any path constraints */
    final List<Machine> machines;

    /** How many instances of each Machine there are */
    final Map<Class<? extends Machine>, PrimVS<Integer>> machineCounters;

    /** The machine to start with */
    private Machine start;

    /** Current depth of exploration */
    private int depth = 0;
    /** Whether or not search is done */
    private boolean done = false;

    int choiceDepth = 0;

    /** Maximum number of internal steps allowed */
    private int maxInternalSteps = -1;
    /** Maximum depth to explore */
    private int maxDepth = -1;
    /** Maximum depth to explore before considering it an error */
    private int errorDepth = -1;

    /** Reset scheduler state
     */
    public void reset() {
        depth = 0;
        choiceDepth = 0;
        done = false;
        machineCounters.clear();
        machines.clear();
    }

    /** Find out whether symbolic execution is finished
     * @return Whether or not there are more steps to run
     */
    public boolean isDone() {
        return done || depth == maxDepth;
    }

    /** Get the machine that execution started with
     * @return The starting machine
     */
    public Machine getStartMachine() {
        return start;
    }

    /** Get current depth
     * @return current depth
     */
    public int getDepth() { return depth; }

    /** Make new schedule
     * @return A new Schedule instance */
    public Schedule getNewSchedule() {
        return new Schedule();
    }

    /** Get the schedule
     * @return The schedule
     */
    public Schedule getSchedule() { return schedule; }

    /** Make a new Scheduler
     * @param machines The machines initially in the Scheduler
     */
    public Scheduler(String name, Machine... machines) {
        //ScheduleLogger.disable();
        schedule = getNewSchedule();
        this.machines = new ArrayList<>();
        this.machineCounters = new HashMap<>();
        this.name = name;

        for (Machine machine : machines) {
            this.machines.add(machine);
            if (this.machineCounters.containsKey(machine.getClass())) {
                this.machineCounters.put(machine.getClass(),
                        IntUtils.add(this.machineCounters.get(machine.getClass()), 1));
            } else {
                this.machineCounters.put(machine.getClass(), new PrimVS<>(1));
            }
            ScheduleLogger.onCreateMachine(Bdd.constTrue(), machine);
            machine.setScheduler(this);
            schedule.makeMachine(machine, Bdd.constTrue());
        }
    }

    public void setMaxInternalSteps(int maxSteps) { this.maxInternalSteps = maxSteps; }

    public int getMaxInternalSteps() { return maxInternalSteps; }

    @Override
    public void setMaxDepth(int maxDepth) {
        this.maxDepth = maxDepth;
    }

    public List<PrimVS> getNextIntegerChoices(PrimVS<Integer> bound, Bdd pc) {
        List<PrimVS> choices = new ArrayList<>();
        for (int i = 0; i < IntUtils.maxValue(bound); i++) {
            Bdd cond = IntUtils.lessThan(i, bound).getGuard(true);
            choices.add(new PrimVS<>(i).guard(cond));
        }
        return choices;
    }

    public PrimVS<Integer> getNextInteger(List<PrimVS> candidateIntegers) {
        PrimVS<Integer> choices = (PrimVS<Integer>) NondetUtil.getNondetChoice(candidateIntegers);
        schedule.addRepeatInt(choices, choiceDepth);
        schedule.addIntChoice(choices, choiceDepth);
        return choices;
    }

    @Override
    public PrimVS<Integer> getNextInteger(PrimVS<Integer> bound, Bdd pc) {
        return getNextInteger(getNextIntegerChoices(bound, pc));
    }

    public List<PrimVS> getNextBooleanChoices(Bdd pc) {
        List<PrimVS> choices = new ArrayList<>();
        choices.add(new PrimVS<>(true).guard(pc));
        choices.add(new PrimVS<>(false).guard(pc));
        return choices;
    }

    public PrimVS<Boolean> getNextBoolean(List<PrimVS> candidateBooleans) {
        PrimVS<Boolean> choices = (PrimVS<Boolean>) NondetUtil.getNondetChoice(candidateBooleans);
        schedule.addRepeatBool(choices, choiceDepth);
        schedule.addBoolChoice(choices, choiceDepth);
        return choices;
    }

    @Override
    public PrimVS<Boolean> getNextBoolean(Bdd pc) {
        return getNextBoolean(getNextBooleanChoices(pc));
    }

    public List<ValueSummary> getNextElementChoices(Set<ValueSummary> candidates, Bdd pc) {
        return candidates.stream().map(x -> x.guard(pc)).collect(Collectors.toList());
    }

    public PrimVS<ValueSummary> getNextElementHelper(List<ValueSummary> candidates) {
        PrimVS<ValueSummary> choices = NondetUtil.getNondetChoice(candidates.stream().map(x -> new PrimVS(x).guard(x.getUniverse())).collect(Collectors.toList()));
        schedule.addRepeatElement(choices, choiceDepth);
        schedule.addElementChoice(choices, choiceDepth);
        return choices;
    }
    public ValueSummary getNextElementFlattener(PrimVS<ValueSummary> choices) {
        ValueSummary flattened = null;
        List<ValueSummary> toMerge = new ArrayList<>();
        for (GuardedValue<ValueSummary> guardedValue : choices.getGuardedValues()) {
            if (flattened == null) {
                guardedValue.value.guard(guardedValue.guard);
            } else {
                toMerge.add(guardedValue.value.guard(guardedValue.guard));
            }
        }
        if (flattened == null) {
            flattened = new PrimVS<>();
        } else {
            flattened = flattened.merge(toMerge);
        }
        return flattened;
    }

    @Override
    public ValueSummary getNextElement(Set<ValueSummary> candidates, Bdd pc) {
        return getNextElementFlattener(getNextElementHelper(getNextElementChoices(candidates, pc)));
    }

    @Override
    public void setErrorDepth(int errorDepth) {
        this.errorDepth = errorDepth;
    }

    /** Start execution with the specified machine
     * @param machine Machine to start execution with */
    public void startWith(Machine machine) {
        if (this.machineCounters.containsKey(machine.getClass())) {
            this.machineCounters.put(machine.getClass(),
                    IntUtils.add(this.machineCounters.get(machine.getClass()), 1));
        } else {
            this.machineCounters.put(machine.getClass(), new PrimVS<>(1));
        }

        machines.add(machine);
        start = machine;
        ScheduleLogger.onCreateMachine(Bdd.constTrue(), machine);
        machine.setScheduler(this);
        schedule.makeMachine(machine, Bdd.constTrue());

        performEffect(
                new Event(
                        EventName.Init.instance,
                        new VectorClockVS(Bdd.constTrue()),
                        new PrimVS<>(machine),
                        null
                )
        );
    }

    public void replayStartWith(Machine machine) {
        PrimVS<Machine> machineVS;
        if (this.machineCounters.containsKey(machine.getClass())) {
            machineVS = schedule.getMachine(machine.getClass(), this.machineCounters.get(machine.getClass()));
            this.machineCounters.put(machine.getClass(),
                    IntUtils.add(this.machineCounters.get(machine.getClass()), 1));
        } else {
            machineVS = schedule.getMachine(machine.getClass(), new PrimVS<>(0));
            this.machineCounters.put(machine.getClass(), new PrimVS<>(1));
        }

        ScheduleLogger.onCreateMachine(machineVS.getUniverse(), machine);
        machine.setScheduler(this);

        performEffect(
                new Event(
                        EventName.Init.instance,
                        new VectorClockVS(machineVS.getUniverse()),
                        machineVS,
                        null
                )
        );
    }

    @Override
    public void doSearch(Machine target) {
        startWith(target);
        while (!isDone()) {
            // ScheduleLogger.log("step " + depth + ", true queries " + Bdd.trueQueries + ", false queries " + Bdd.falseQueries);
            Assert.prop(errorDepth < 0 || depth < errorDepth, "Maximum allowed depth " + errorDepth + " exceeded", this, schedule.getLengthCond(schedule.size()));
            step();
        }
    }

    public List<PrimVS> getNextSenderChoices() {
        List<PrimVS> candidateSenders = new ArrayList<>();

        for (Machine machine : machines) {
            if (!machine.sendEffects.isEmpty()) {
                Bdd initCond = machine.sendEffects.enabledCondInit().getGuard(true);
                if (!initCond.isConstFalse()) {
                    PrimVS<Machine> ret = new PrimVS<>(machine).guard(initCond);
                    candidateSenders.add(ret);
                    return candidateSenders;
                }
                Bdd canRun = machine.sendEffects.enabledCond(Event::canRun).getGuard(true);
                if (!canRun.isConstFalse()) {
                    candidateSenders.add(new PrimVS<>(machine).guard(canRun));
                }
            }
        }

        return candidateSenders;
    }

    public PrimVS<Machine> getNextSender(List<PrimVS> candidateSenders) {
        PrimVS<Machine> choices = (PrimVS<Machine>) NondetUtil.getNondetChoice(candidateSenders);
        List<PrimVS<Machine>> choiceList = new ArrayList<>();
        choices.getGuardedValues().forEach(x -> choiceList.add(new PrimVS<>(x.value).guard(x.guard)));
        schedule.addSenderChoice(choices, choiceDepth);
        schedule.addRepeatSender(choices, choiceDepth);
        choiceDepth++;
        return choices;
    }

    public PrimVS<Machine> getNextSender() {
        return getNextSender(getNextSenderChoices());
    }

    public void step() {
        PrimVS<Machine> choices = getNextSender();

        if (choices.isEmptyVS()) {
            ScheduleLogger.finished(depth);
            done = true;
            return;
        }

        /*
        int n = choices.getGuardedValues().size();
        int limit = 5; // n / 2 + 1;
        int i = 0;

        Event effect = null;
        List<Event> effects = new ArrayList<>();
        for (GuardedValue<Machine> sender : choices.getGuardedValues()) {
            Machine machine = sender.value;
            Bdd guard = sender.guard;
            if (i < limit) {
                if (effect == null) {
                    effect = machine.sendEffects.remove(guard);
                } else {
                    effects.add(machine.sendEffects.remove(guard));
                }
            } else {
                ScheduleLogger.log("omitting " + i);
                machine.sendEffects.remove(guard);
            }
            i++;
        }
        */

        Event effect = null;
        List<Event> effects = new ArrayList<>();
        for (GuardedValue<Machine> sender : choices.getGuardedValues()) {
            Machine machine = sender.value;
            Bdd guard = sender.guard;
            if (effect == null) {
                effect = machine.sendEffects.remove(guard);
            } else {
                effects.add(machine.sendEffects.remove(guard));
            }
        }
        effect = effect.merge(effects);
        ScheduleLogger.schedule(depth, effect);
        performEffect(effect);
        depth++;
    }

    public PrimVS<Machine> allocateMachine(Bdd pc, Class<? extends Machine> machineType,
                                           Function<Integer, ? extends Machine> constructor) {
        if (!machineCounters.containsKey(machineType)) {
            machineCounters.put(machineType, new PrimVS<>(0));
        }
        PrimVS<Integer> guardedCount = machineCounters.get(machineType).guard(pc);

        Machine newMachine;
        newMachine = constructor.apply(IntUtils.maxValue(guardedCount));

        if (!machines.contains(newMachine)) {
            machines.add(newMachine);
        }

        ScheduleLogger.onCreateMachine(pc, newMachine);
        newMachine.setScheduler(this);
        schedule.makeMachine(newMachine, pc);

        guardedCount = IntUtils.add(guardedCount, 1);
        PrimVS<Integer> mergedCount = machineCounters.get(machineType).update(pc, guardedCount);
        machineCounters.put(machineType, mergedCount);
        return new PrimVS<>(newMachine).guard(pc);
    }

    void performEffect(Event event) {
        for (GuardedValue<Machine> target : event.getMachine().getGuardedValues()) {
            target.value.processEventToCompletion(target.guard, event.guard(target.guard));
        }
    }

    public void disableLogging() {
        ScheduleLogger.disable();
    }

    public void enableLogging() {
        ScheduleLogger.enable();
    }
}
