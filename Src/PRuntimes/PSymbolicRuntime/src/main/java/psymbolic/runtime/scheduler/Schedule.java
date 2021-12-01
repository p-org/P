package psymbolic.runtime.scheduler;

import lombok.Getter;
import psymbolic.runtime.machine.Machine;
import psymbolic.valuesummary.*;
import psymbolic.runtime.Concretizer;
import psymbolic.runtime.Message;

import java.util.*;
import java.util.stream.Collectors;

public class Schedule {

    private final boolean useSleepSets;

    private Guard filter = Guard.constTrue();

    public void restrictFilter(Guard c) { filter = filter.and(c); }
    public Guard getFilter() { return filter; }
    public void resetFilter() { filter = Guard.constTrue(); }

    public Choice newChoice() {
        return new Choice();
    }

    SetVS<VectorClockVS> sleepSet = new SetVS<>(Guard.constTrue());

    private static List<VectorClockVS> fullUniverseClocks (VectorClockVS vc) {
        List<GuardedValue<List<Object>>> concreteVals = Concretizer.getConcreteValues(vc.getUniverse(), x -> false, Concretizer::concretize, vc);
        List<VectorClockVS> res = new ArrayList<>();
        for (GuardedValue<List<Object>> gv : concreteVals) { 
            if (gv.getValue().isEmpty()) continue;
            List<Object> list = gv.getValue();
            ListVS newList = new ListVS<>(Guard.constTrue());
            for (Object i : list) {
                newList = newList.add(new PrimitiveVS<>((Integer) i));
            }
            res.add(new VectorClockVS(newList));
        }
        return res;
    }

    private static VectorClockVS getVectorClockSent (PrimitiveVS<Machine> sender) {
        Message m = new Message().restrict(Guard.constFalse());
        for (GuardedValue<Machine> gv : sender.getGuardedValues()) {
            m = m.merge(gv.getValue().sendBuffer.peek(gv.getGuard()));
        }
        return m.getVectorClock();
    }

    public List<PrimitiveVS> filterSleep (List<PrimitiveVS> candidates) {
        if (!useSleepSets) return candidates;
        List<PrimitiveVS> filtered = candidates.stream().map(x -> x.restrict(sleepSet.contains(getVectorClockSent(x)).getGuardFor(true).not())).collect(Collectors.toList());
        return filtered;
    }

    public void unblock(VectorClockVS vc) {
        VectorClockVS toRemove = vc.restrict(sleepSet.getUniverse());
        sleepSet.remove(toRemove.restrict(sleepSet.contains(toRemove).getGuardFor(true)));
    }

    public class Choice {
        @Getter
        PrimitiveVS<Machine> repeatSender = new PrimitiveVS<>();
        @Getter
        PrimitiveVS<Boolean> repeatBool = new PrimitiveVS<>();
        @Getter
        PrimitiveVS<Integer> repeatInt = new PrimitiveVS<>();
        @Getter
        PrimitiveVS<ValueSummary> repeatElement = new PrimitiveVS<>();
        @Getter
        List<PrimitiveVS<Machine>> backtrackSender = new ArrayList<>();
        @Getter
        List<PrimitiveVS<Boolean>> backtrackBool = new ArrayList();
        @Getter
        List<PrimitiveVS<Integer>> backtrackInt = new ArrayList<>();
        @Getter
        List<PrimitiveVS<ValueSummary>> backtrackElement = new ArrayList<>();
        @Getter
        Guard handledUniverse = Guard.constFalse();

        public Choice() {
        }

        public Guard getRepeatUniverse() {
            return repeatSender.getUniverse().or(repeatBool.getUniverse().or(repeatInt.getUniverse().or(repeatElement.getUniverse())));
        }

        public Guard getBacktrackUniverse() {
            Guard senderUniverse = Guard.constFalse();
            for (PrimitiveVS<Machine> machine : backtrackSender) {
                senderUniverse = senderUniverse.or(machine.getUniverse());
            }
            for (PrimitiveVS<Boolean> bool : backtrackBool) {
                senderUniverse = senderUniverse.or(bool.getUniverse());
            }
            for (PrimitiveVS<Integer> integer : backtrackInt) {
                senderUniverse = senderUniverse.or(integer.getUniverse());
            }
            for (PrimitiveVS<ValueSummary> element : backtrackElement) {
                senderUniverse = senderUniverse.or(element.getUniverse());
            }
            return senderUniverse;
        }

        public boolean isRepeatEmpty() {
            return getRepeatUniverse().isFalse();
        }

        public boolean isBacktrackEmpty() {
            return getBacktrackSender().isEmpty() && getBacktrackBool().isEmpty() && getBacktrackInt().isEmpty() && getBacktrackElement().isEmpty();
        }

        public Choice restrict(Guard pc) {
            Choice c = newChoice();
            c.repeatSender = repeatSender.restrict(pc);
            c.repeatBool = repeatBool.restrict(pc);
            c.repeatInt = repeatInt.restrict(pc);
            c.repeatElement = repeatElement.restrict(pc);
            c.backtrackSender = backtrackSender.stream().map(x -> x.restrict(pc)).filter(x -> !x.isEmptyVS()).collect(Collectors.toList());
            c.backtrackBool = backtrackBool.stream().map(x -> x.restrict(pc)).filter(x -> !x.isEmptyVS()).collect(Collectors.toList());
            c.backtrackInt = backtrackInt.stream().map(x -> x.restrict(pc)).filter(x -> !x.isEmptyVS()).collect(Collectors.toList());
            c.backtrackElement = backtrackElement.stream().map(x -> x.restrict(pc)).filter(x -> !x.isEmptyVS()).collect(Collectors.toList());
            return c;
        }

        public void updateHandledUniverse(Guard update) {
            handledUniverse = handledUniverse.or(update);
        }

        public void addRepeatSender(PrimitiveVS<Machine> choice) {
            repeatSender = choice;
            if (!useSleepSets) return;
            Guard seen = Guard.constFalse();
            Guard choiceUniverse = choice.getUniverse();
            for (GuardedValue<Machine> gv : choice.getGuardedValues()) {
                Guard initCond = gv.getValue().sendBuffer.hasCreateMachineUnderGuard().getGuardFor(true);
                List<VectorClockVS> clks = fullUniverseClocks(gv.getValue().sendBuffer.peek(gv.getGuard().and(initCond.not())).getVectorClock());
                seen = seen.or(gv.getGuard());
                for (VectorClockVS clk : clks) {
                    // remove from sleep set:
                    Guard toRemoveGuard = sleepSet.contains(clk.restrict(sleepSet.getUniverse())).getGuardFor(true);
                    sleepSet = sleepSet.remove(clk.restrict(toRemoveGuard));
                    // add to sleep set:
                    sleepSet = sleepSet.add(clk.restrict(seen.not().and(choiceUniverse)));
                }
            }
        }

        public void addRepeatBool(PrimitiveVS<Boolean> choice) {
            repeatBool = choice;
        }

        public void addRepeatInt(PrimitiveVS<Integer> choice) {
            repeatInt = choice;
        }

        public void addRepeatElement(PrimitiveVS<ValueSummary> choice) {
            repeatElement = choice;
        }

        public void clearRepeatSender() {
            repeatSender = new PrimitiveVS<>();
        }

        public void clearRepeat() {
            repeatSender = new PrimitiveVS<>();
            repeatBool = new PrimitiveVS<>();
            repeatInt = new PrimitiveVS<>();
            repeatElement = new PrimitiveVS<>();
        }

        public void addBacktrackSender(PrimitiveVS<Machine> choice) { if (!choice.isEmptyVS()) backtrackSender.add(choice); }

        public void addBacktrackBool(PrimitiveVS<Boolean> choice) { if (!choice.isEmptyVS())  backtrackBool.add(choice); }

        public void addBacktrackInt(PrimitiveVS<Integer> choice) { if (!choice.isEmptyVS()) backtrackInt.add(choice); }

        public void addBacktrackElement(PrimitiveVS<ValueSummary> choice) { if (!choice.isEmptyVS()) backtrackElement.add(choice); }

        public void clearBacktrack() {
            backtrackSender = new ArrayList<>();
            backtrackBool = new ArrayList<>();
            backtrackInt = new ArrayList<>();
            backtrackElement = new ArrayList<>();
        }

        public void clear() {
            clearRepeat();
            clearBacktrack();
            handledUniverse = Guard.constFalse();
        }

    }

    private List<Choice> choices = new ArrayList<>();

    public List<Choice> getChoices() { return choices; }

    public Choice getChoice(int d) {
        return choices.get(d);
    }

    public void clearChoice(int d) {
        choices.get(d).clear();
    }

    public int getNumBacktracks() {
        int count = 0;
        for (Choice backtrack : choices) {
            if (!backtrack.isBacktrackEmpty()) count++;
        }
        return count;
    }

    public void addRepeatSender(PrimitiveVS<Machine> choice, int depth) {
        if (depth >= choices.size()) {
            choices.add(newChoice());
        }
        choices.get(depth).addRepeatSender(choice);
    }

    public void addRepeatBool(PrimitiveVS<Boolean> choice, int depth) {
        if (depth >= choices.size()) {
            choices.add(newChoice());
        }
        choices.get(depth).addRepeatBool(choice);
    }

    public void addRepeatInt(PrimitiveVS<Integer> choice, int depth) {
        if (depth >= choices.size()) {
            choices.add(newChoice());
        }
        choices.get(depth).addRepeatInt(choice);
    }

    public void addRepeatElement(PrimitiveVS<ValueSummary> choice, int depth) {
        if (depth >= choices.size()) {
            choices.add(newChoice());
        }
        choices.get(depth).addRepeatElement(choice);
    }

    public void addBacktrackSender(List<PrimitiveVS<Machine>> machines, int depth) {
        if (depth >= choices.size()) {
            choices.add(newChoice());
        }
        for (PrimitiveVS<Machine> choice : machines) {
            choices.get(depth).addBacktrackSender(choice);
        }
    }

    public void addBacktrackBool(List<PrimitiveVS<Boolean>> bools, int depth) {
        if (depth >= choices.size()) {
            choices.add(newChoice());
        }
        for (PrimitiveVS<Boolean> choice : bools) {
            choices.get(depth).addBacktrackBool(choice);
        }
    }

    public void addBacktrackInt(List<PrimitiveVS<Integer>> ints, int depth) {
        if (depth >= choices.size()) {
            choices.add(newChoice());
        }
        for (PrimitiveVS<Integer> choice : ints) {
            choices.get(depth).addBacktrackInt(choice);
        }
    }

    public void addBacktrackElement(List<PrimitiveVS<ValueSummary>> elements, int depth) {
        if (depth >= choices.size()) {
            choices.add(newChoice());
        }
        for (PrimitiveVS<ValueSummary> choice : elements) {
            choices.get(depth).addBacktrackElement(choice);
        }
    }

    public PrimitiveVS<Machine> getRepeatSender(int depth) {
        return choices.get(depth).getRepeatSender();
    }

    public PrimitiveVS<Boolean> getRepeatBool(int depth) {
        return choices.get(depth).getRepeatBool();
    }

    public PrimitiveVS<Integer> getRepeatInt(int depth) {
        return choices.get(depth).getRepeatInt();
    }

    public PrimitiveVS<ValueSummary> getRepeatElement(int depth) {
        return choices.get(depth).getRepeatElement();
    }

    public List<PrimitiveVS<Machine>> getBacktrackSender(int depth) {
        return choices.get(depth).getBacktrackSender();
    }

    public List<PrimitiveVS<Boolean>> getBacktrackBool(int depth) {
        return choices.get(depth).getBacktrackBool();
    }

    public List<PrimitiveVS<Integer>> getBacktrackInt(int depth) {
        return choices.get(depth).getBacktrackInt();
    }

    public List<PrimitiveVS<ValueSummary>> getBacktrackElement(int depth) {
        return choices.get(depth).getBacktrackElement();
    }

    public void clearRepeat(int depth) {
        choices.get(depth).clearRepeat();
    }

    public void clearBacktrack(int depth) {
        choices.get(depth).clearBacktrack();
    }

    public void restrictFilterForDepth(int depth) {
        Choice choice = choices.get(depth);
        Guard repeat = choice.getRepeatUniverse();
        Guard backtrackOrHandled = choice.getBacktrackUniverse().or(choice.getHandledUniverse());
        restrictFilter(backtrackOrHandled.not().or(repeat.and(backtrackOrHandled)));
    }

    public int size() {
        return choices.size();
    }

    private Map<Class<? extends Machine>, ListVS<PrimitiveVS<Machine>>> createdMachines = new HashMap<>();
    private Set<Machine> machines = new HashSet<>();

    private Guard pc = Guard.constTrue();

    public Schedule() { this.useSleepSets = false; }

    public Schedule(boolean useSleepSets) {
        this.useSleepSets = useSleepSets;
    }

    private Schedule(List<Choice> choices,
                     Map<Class<? extends Machine>, ListVS<PrimitiveVS<Machine>>> createdMachines,
                     Set<Machine> machines,
                     Guard pc,
                     boolean useSleepSets) {
        this.choices = new ArrayList<>(choices);
        this.createdMachines = new HashMap<>(createdMachines);
        this.machines = new HashSet<>(machines);
        this.pc = pc;
        this.useSleepSets = useSleepSets;
    }

    public Set<Machine> getMachines() {
        return machines;
    }

    public Schedule guard(Guard pc) {
        List<Choice> newChoices = new ArrayList<>();
        for (Choice c : choices) {
            newChoices.add(c.restrict(pc));
        }
        return new Schedule(newChoices, createdMachines, machines, pc, useSleepSets);
    }

    public Schedule removeEmptyRepeat() {
        List<Choice> newChoices = new ArrayList<>();
        for (int i = 0; i < size(); i++) {
            if (!choices.get(i).isRepeatEmpty()) {
                newChoices.add(choices.get(i));
            }
        }
        return new Schedule(newChoices, createdMachines, machines, pc, useSleepSets);
    }

    public void makeMachine(Machine m, Guard pc) {
        PrimitiveVS<Machine> toAdd = new PrimitiveVS<>(m).restrict(pc);
        if (createdMachines.containsKey(m.getClass())) {
            createdMachines.put(m.getClass(), createdMachines.get(m.getClass()).add(toAdd));
        } else {
            createdMachines.put(m.getClass(), new ListVS<PrimitiveVS<Machine>>(Guard.constTrue()).add(toAdd));
        }
        machines.add(m);
    }

    public boolean hasMachine(Class<? extends Machine> type, PrimitiveVS<Integer> idx, Guard otherPc) {
        if (!createdMachines.containsKey(type)) return false;
        // TODO: may need fixing
        //ScheduleLogger.log("has machine of type");
        //ScheduleLogger.log(idx + " in range? " + createdMachines.get(type).inRange(idx).getGuard(false));
        if (!createdMachines.get(type).inRange(idx).getGuardFor(false).isFalse()) return false;
        PrimitiveVS<Machine> machines = createdMachines.get(type).get(idx);
        return !machines.restrict(pc).restrict(otherPc).getUniverse().isFalse();
    }

    public PrimitiveVS<Machine> getMachine(Class<? extends Machine> type, PrimitiveVS<Integer> idx) {
        PrimitiveVS<Machine> machines = createdMachines.get(type).get(idx);
        return machines.restrict(pc);
    }

    public Schedule getSingleSchedule() {
        Guard pc = Guard.constTrue();
        for (Choice choice : choices) {
            Choice guarded = choice.restrict(pc);
            PrimitiveVS<Machine> sender = guarded.getRepeatSender();
            if (sender.getGuardedValues().size() > 0) {
                pc = pc.and(sender.getGuardedValues().get(0).getGuard());
            } else {
                PrimitiveVS<Boolean> boolChoice = guarded.getRepeatBool();
                if (boolChoice.getGuardedValues().size() > 0) {
                    pc = pc.and(boolChoice.getGuardedValues().get(0).getGuard());
                } else {
                    PrimitiveVS<Integer> intChoice = guarded.getRepeatInt();
                    if (intChoice.getGuardedValues().size() > 0) {
                        pc = pc.and(intChoice.getGuardedValues().get(0).getGuard());
                    }
                    else {
                        PrimitiveVS<ValueSummary> elementChoice = guarded.getRepeatElement();
                        if (elementChoice.getGuardedValues().size() > 0) {
                            pc = pc.and(elementChoice.getGuardedValues().get(0).getGuard());
                        }
                    }
                }
            }
        }
        return this.guard(pc).removeEmptyRepeat();
    }

    public Guard getLengthCond(int size) {
        if (size == 0) return Guard.constFalse();
        return choices.get(size - 1).getRepeatUniverse();
    }

}
