package psymbolic.runtime.scheduler;

import lombok.Getter;
import psymbolic.commandline.PSymConfiguration;
import psymbolic.commandline.Program;
import psymbolic.runtime.logger.SearchLogger;
import psymbolic.runtime.logger.StatLogger;
import psymbolic.runtime.logger.TraceLogger;
import psymbolic.runtime.machine.Machine;
import psymbolic.valuesummary.*;
import psymbolic.runtime.machine.buffer.*;

import java.util.*;
import java.util.function.BiConsumer;
import java.util.function.Consumer;
import java.util.function.Function;
import java.util.function.Supplier;
import java.util.stream.Collectors;

/**
 * Represents the iterative bounded scheduler
 */
public class IterativeBoundedScheduler extends Scheduler {

    @Getter
    int iter = 0;
    private int backtrack = 0;
    private Program program;

    private boolean isDoneIterating = false;

    public IterativeBoundedScheduler(PSymConfiguration config) {
        super(config);
    }

    @Override
    public void print_stats() {
        super.print_stats();
        // print statistics
        if (configuration.getCollectStats() != 0) {
            StatLogger.log(String.format("#-choices-covered:\t%d scheduling, %d data",
                                                schedule.getNumScheduleChoicesExplored(),
                                                schedule.getNumDataChoicesExplored()));
            StatLogger.log(String.format("#-choices-remaining:\t%d scheduling, %d data",
                                                schedule.getNumScheduleChoicesRemaining(),
                                                schedule.getNumDataChoicesRemaining()));
            StatLogger.log(String.format("#-backtracks:\t%d", schedule.getNumBacktracks()));
            StatLogger.log(String.format("#-iterations:\t%d", iter));
        }
    }

    public void reportSearchSummary() {
        TraceLogger.log("--------------------");
        TraceLogger.log("Coverage Report::");
        TraceLogger.log("--------------------");
        TraceLogger.log(String.format("  Covered choices:\t%5s scheduling, %5s data",
                                        schedule.getNumScheduleChoicesExplored(),
                                        schedule.getNumDataChoicesExplored()));
        TraceLogger.log(String.format("  Remaining choices:\t%5s scheduling, %5s data",
                                        schedule.getNumScheduleChoicesRemaining(),
                                        schedule.getNumDataChoicesRemaining() ));

        Map<Integer, List<Integer>> perDepth = new HashMap<>();
        for (int d = 0; d < schedule.size(); d++) {
            Schedule.Choice choice = schedule.getChoice(d);
            int depth = choice.getSchedulerDepth();
            int numScheduleExplored = choice.getNumScheduleChoicesExplored();
            int numDataExplored = choice.getNumDataChoicesExplored();
            int numScheduleRemaining = choice.getNumScheduleChoicesRemaining();
            int numDataRemaining = choice.getNumDataChoicesRemaining();

            if ((numScheduleExplored + numDataExplored + numScheduleRemaining + numDataRemaining) == 0) {
                continue;
            }

            if (!perDepth.containsKey(depth)) {
                perDepth.put(depth, Arrays.asList(0, 0, 0, 0));
            }
            List<Integer> val = perDepth.get(depth);
            val.set(0, val.get(0) + numScheduleExplored);
            val.set(1, val.get(1) + numDataExplored);
            val.set(2, val.get(2) + numScheduleRemaining);
            val.set(3, val.get(3) + numDataRemaining);
        }
        String s = "";
        TraceLogger.log("\t-------------------------------------");
        s += String.format("\t   Step  ");
        s += String.format("  Covered        Remaining");
        s += String.format("\n\t%5s  %5s   %5s  ", "", "sch", "data");
        s += String.format(" %5s   %5s ", "sch", "data");
        TraceLogger.log(s);
        TraceLogger.log("\t-------------------------------------");
        for (Map.Entry entry : perDepth.entrySet()) {
            int depth = (int) entry.getKey();
            List<Integer> val = (List<Integer>) entry.getValue();
            s = "";
            s += String.format("\t%5s ", depth);
            s += String.format(" %5s   %5s  ",
                    (val.get(0)==0?"":val.get(0)),
                    (val.get(1)==0?"":val.get(1)));
            s += String.format(" %5s   %5s ",
                    (val.get(2)==0?"":val.get(2)),
                    (val.get(3)==0?"":val.get(3)));
            TraceLogger.log(s);
        }
    }

    private void summarizeIteration() {
        recordResult();
        if (configuration.getCollectStats() > 2) {
            SearchLogger.logIterationStats(searchStats.getIterationStats().get(iter));
        }
        if (configuration.getIterationBound() > 0) {
            isDoneIterating = (iter >= configuration.getIterationBound());
        }
        if (!isDoneIterating) {
            postIterationCleanup();
        }
    }

    @Override
    public void doSearch(Program p) {
        program = p;
        result = "incomplete";
        initializeSearch(program);
        while (!isDoneIterating) {
            iter++;
            SearchLogger.logStartExecution(iter, getDepth());
            searchStats.startNewIteration(iter, backtrack);
            super.resumeSearch(program);
            summarizeIteration();
        }
    }

    @Override
    public void resumeSearch(Program p) {
        program = p;
        isDoneIterating = false;
        boolean initialRun = true;
        while (!isDoneIterating) {
            if (initialRun) {
                SearchLogger.logResumeExecution(iter, getDepth());
                initialRun = false;
            } else {
                iter++;
                SearchLogger.logStartExecution(iter, getDepth());
                searchStats.startNewIteration(iter, backtrack);
            }
            super.resumeSearch(p);
            summarizeIteration();
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
                TraceLogger.logMessage("backtrack to " + d);
                backtrack = d;
                TraceLogger.logMessage("pending backtracks: " + schedule.getNumBacktracks());
                if (newDepth == 0) {
                    reset();
                    initializeSearch(program);
                } else {
                    restore(newDepth, choice.getSchedulerChoiceDepth());
                }
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

    private PrimitiveVS getNext(int depth, int bound, Function<Integer, PrimitiveVS> getRepeat, Function<Integer, List> getBacktrack,
                           Consumer<Integer> clearBacktrack, BiConsumer<PrimitiveVS, Integer> addRepeat,
                           BiConsumer<List, Integer> addBacktrack, Supplier<List> getChoices,
                           Function<List, PrimitiveVS> generateNext) {
        List<PrimitiveVS> choices = new ArrayList();

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
                TraceLogger.logMessage("new choice at depth " + depth);
            choices = getChoices.get();
            choices = choices.stream().map(x -> x.restrict(schedule.getFilter())).filter(x -> !(x.getUniverse().isFalse())).collect(Collectors.toList());
        }

        if (choices.size() > 1) {
            if (configuration.isUseRandom()) {
                Collections.shuffle(choices, new Random(configuration.getRandomSeed()));
            }
//            Collections.sort(choices, new SortVS());
//            TraceLogger.log("\t#Choices = " + choices.size());
//            for (PrimitiveVS vs: choices) {
//                TraceLogger.log("\t\t" + vs);
//            }
        }

        List<PrimitiveVS> chosen = new ArrayList();
        List<PrimitiveVS> backtrack = new ArrayList();
        for (int i = 0; i < choices.size(); i++) {
            if (i < bound) chosen.add(choices.get(i));
            else {
                backtrack.add(choices.get(i));
            }
        }
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
                super::getNextSender);
        choiceDepth = depth + 1;
        return res;
    }

    @Override
    public PrimitiveVS<Boolean> getNextBoolean(Guard pc) {
        int depth = choiceDepth;
        PrimitiveVS<Boolean> res = getNext(depth, configuration.getInputChoiceBound(), schedule::getRepeatBool, schedule::getBacktrackBool,
                schedule::clearBacktrack, schedule::addRepeatBool, schedule::addBacktrackBool,
                () -> super.getNextBooleanChoices(pc), super::getNextBoolean);
        choiceDepth = depth + 1;
        return res;
    }

    @Override
    public PrimitiveVS<Integer> getNextInteger(PrimitiveVS<Integer> bound, Guard pc) {
        int depth = choiceDepth;
        PrimitiveVS<Integer> res = getNext(depth, configuration.getInputChoiceBound(), schedule::getRepeatInt, schedule::getBacktrackInt,
                schedule::clearBacktrack, schedule::addRepeatInt, schedule::addBacktrackInt,
                () -> super.getNextIntegerChoices(bound, pc), super::getNextInteger);
        choiceDepth = depth + 1;
        return res;
    }

    @Override
    public ValueSummary getNextElement(ListVS<? extends ValueSummary> candidates, Guard pc) {
        int depth = choiceDepth;
        PrimitiveVS<ValueSummary> res = getNext(depth, configuration.getInputChoiceBound(), schedule::getRepeatElement, schedule::getBacktrackElement,
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
