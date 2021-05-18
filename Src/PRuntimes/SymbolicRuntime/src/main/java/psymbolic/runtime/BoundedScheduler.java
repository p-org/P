package psymbolic.runtime;

import psymbolic.commandline.Program;
import psymbolic.runtime.logger.ScheduleLogger;
import psymbolic.runtime.machine.Machine;
import psymbolic.runtime.machine.Message;
import psymbolic.valuesummary.*;

import java.util.List;
import java.util.function.BiConsumer;
import java.util.function.Consumer;
import java.util.function.Function;
import java.util.function.Supplier;

public class BoundedScheduler extends Scheduler {
    int iter = 0;
    int senderBound;
    int boolBound;
    int intBound;

    private boolean isDoneIterating = false;

    public BoundedScheduler(String name, int senderBound, int boolBound, int intBound) {
        super("bounded_" + name);
        this.senderBound = senderBound;
        this.boolBound = boolBound;
        this.intBound = intBound;
    }

    @Override
    public void doSearch(Program p) {
        while (!isDoneIterating) {
            ScheduleLogger.log("Iteration " + iter);
            super.doSearch(p);
            postIterationCleanup();
            iter++;
        }
    }

    public void postIterationCleanup() {
        for (int d = schedule.size() - 1; d >= 0; d--) {
            Schedule.Choice backtrack = schedule.getBacktrackChoice(d);
            schedule.clearRepeat(d);
            if (!backtrack.isEmpty()) {
                for (Machine machine : schedule.getMachines()) {
                    machine.reset();
                }
                ScheduleLogger.log("backtrack to " + d);
                ScheduleLogger.log("pending backtracks: " + schedule.getNumBacktracks());
                schedule.setFilter(Guard.constTrue()); //backtrack.getUniverse();
                schedule.resetTransitionCount();
                reset();
                return;
            } else {
                schedule.clearChoice(d);
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

    private PrimitiveVS getNext(int depth, int bound, Function<Integer, PrimitiveVS> getRepeat, Function<Integer, PrimitiveVS> getBacktrack,
                           Consumer<Integer> clearBacktrack, BiConsumer<PrimitiveVS, Integer> addRepeat,
                           BiConsumer<PrimitiveVS, Integer> addBacktrack, Supplier<List> getChoices,
                           Function<List, PrimitiveVS> generateNext) {
        PrimitiveVS choices = new PrimitiveVS();
        if (depth < schedule.size()) {
            // ScheduleLogger.log("repeat or backtrack");
            PrimitiveVS repeat = getRepeat.apply(depth);
            if (!repeat.getUniverse().isFalse()) {
                return repeat;
            }
            // ScheduleLogger.log("CHOSE FROM backtrack: " + getBacktrack.apply(depth));
            // nothing to repeat, so look at backtrack set
            choices = getBacktrack.apply(depth);
            clearBacktrack.accept(depth);
        }

        if (choices.isEmptyVS()) {
            // no choice to backtrack to, so generate new choices
            if (iter > 0)
                ScheduleLogger.log("new choice at depth " + depth);
            choices = generateNext.apply(getChoices.get());
        }

        // ScheduleLogger.log("choose from " + choices);
        Guard guard = NondetUtil.chooseGuard(bound, choices);
        PrimitiveVS chosenVS = choices.restrict(guard).restrict(schedule.getFilter());
        PrimitiveVS backtrackVS = choices.restrict(guard.not());
        //("add repeat " + chosenVS);
        addRepeat.accept(chosenVS, depth);
        // ScheduleLogger.log("add backtrack " + backtrackVS);
        if (!backtrackVS.isEmptyVS()) {
            // ScheduleLogger.log("NEED TO BACKTRACK TO " + depth + ", remaining: " + backtrackVS);
        }
        addBacktrack.accept(backtrackVS, depth);
        return chosenVS;
    }

    @Override
    public PrimitiveVS<Machine> getNextSender() {
        int depth = choiceDepth;
        PrimitiveVS<Machine> res = getNext(depth, senderBound, schedule::getRepeatSender, schedule::getBacktrackSender,
                schedule::clearBacktrack, schedule::addRepeatSender, schedule::addBacktrackSender, super::getNextSenderChoices,
                super::getNextSender);
        schedule.setFilter(schedule.getFilter().and(res.getUniverse()));

        /*
        ScheduleLogger.log("choice: " + schedule.getRepeatSender(depth));
        ScheduleLogger.log("backtrack: " + schedule.getBacktrackSender(depth));
        ScheduleLogger.log("full choice: " + schedule.getSenderChoice(depth));
         */
        for (GuardedValue<Machine> sender : schedule.getRepeatSender(depth).getGuardedValues()) {
            Machine machine = sender.getValue();
            Guard guard = machine.sendEffects.satisfiesPredUnderGuard(Message::canRun).getGuardFor(true).and(schedule.getRepeatSender(depth).getUniverse().not());
            if (!guard.isFalse()) {
                machine.sendEffects.remove(guard);
                // ScheduleLogger.log("remove guard from sender " + machine + ": " + guard);
            }
        }
        choiceDepth = depth + 1;
        return res;
    }

    @Override
    public PrimitiveVS<Boolean> getNextBoolean(Guard pc) {
        int depth = choiceDepth;
        PrimitiveVS<Boolean> res = getNext(depth, boolBound, schedule::getRepeatBool, schedule::getBacktrackBool,
                schedule::clearBacktrack, schedule::addRepeatBool, schedule::addBacktrackBool,
                () -> super.getNextBooleanChoices(pc), super::getNextBoolean);
        //ScheduleLogger.log("choice: " + schedule.getBoolChoice(depth));
        choiceDepth = depth + 1;
        return res;
    }

    @Override
    public PrimitiveVS<Integer> getNextInteger(PrimitiveVS<Integer> bound, Guard pc) {
        int depth = choiceDepth;
        PrimitiveVS<Integer> res = getNext(depth, intBound, schedule::getRepeatInt, schedule::getBacktrackInt,
                schedule::clearBacktrack, schedule::addRepeatInt, schedule::addBacktrackInt,
                () -> super.getNextIntegerChoices(bound, pc), super::getNextInteger);
        choiceDepth = depth + 1;
        return res;
    }

    @Override
    public ValueSummary getNextElement(ListVS<? extends ValueSummary> candidates, Guard pc) {
        int depth = choiceDepth;
        PrimitiveVS<ValueSummary> res = getNext(depth, senderBound, schedule::getRepeatElement, schedule::getBacktrackElement,
                schedule::clearBacktrack, schedule::addRepeatElement, schedule::addBacktrackElement,
                () -> super.getNextElementChoices(candidates, pc), super::getNextElementHelper);
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
            ScheduleLogger.onCreateMachine(pc, allocated.getValues().iterator().next());
            allocated.getValues().iterator().next().setScheduler(this);
            machines.add(allocated.getValues().iterator().next());
        }
        else {
            ScheduleLogger.log("NEW MACHINE");
            Machine newMachine;
            newMachine = constructor.apply(IntegerVS.maxValue(guardedCount));

            if (!machines.contains(newMachine)) {
                machines.add(newMachine);
            }

            ScheduleLogger.onCreateMachine(pc, newMachine);
            newMachine.setScheduler(this);
            schedule.makeMachine(newMachine, pc);
            allocated = new PrimitiveVS<>(newMachine).restrict(pc);
        }

        guardedCount = IntegerVS.add(guardedCount, 1);

        PrimitiveVS<Integer> mergedCount = machineCounters.get(machineType).updateUnderGuard(pc, guardedCount);
        machineCounters.put(machineType, mergedCount);
        return allocated;
    }

}
