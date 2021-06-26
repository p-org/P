package psymbolic.runtime.scheduler;

import psymbolic.runtime.machine.Machine;
import psymbolic.valuesummary.Guard;
import psymbolic.valuesummary.ListVS;
import psymbolic.valuesummary.PrimitiveVS;
import psymbolic.valuesummary.ValueSummary;

import java.io.FileWriter;
import java.io.IOException;
import java.util.*;

public class Schedule {

    private Guard filter = Guard.constTrue();

    public void restrictFilter(Guard c) { filter = filter.and(c); }
    public Guard getFilter() { return filter; }
    public void resetFilter() { filter = Guard.constTrue(); }

    public class Choice {
        PrimitiveVS<Machine> senderChoice = new PrimitiveVS<>();
        PrimitiveVS<Boolean> boolChoice = new PrimitiveVS<>();
        PrimitiveVS<Integer> intChoice = new PrimitiveVS<>();
        PrimitiveVS<ValueSummary> elementChoice = new PrimitiveVS<>();

        public Choice() {
        }

        public Guard getUniverse() {
            return senderChoice.getUniverse().or(boolChoice.getUniverse().or(intChoice.getUniverse().or(elementChoice.getUniverse())));
        }

        public boolean isEmpty() {
            return getUniverse().isFalse();
        }

        public Choice restrict(Guard pc) {
            Choice c = new Choice();
            c.senderChoice = senderChoice.restrict(pc);
            c.boolChoice = boolChoice.restrict(pc);
            c.intChoice = intChoice.restrict(pc);
            c.elementChoice = elementChoice.restrict(pc);
            return c;
        }

        public void addSenderChoice(PrimitiveVS<Machine> choice) {
            senderChoice = choice;
        }

        public void addBoolChoice(PrimitiveVS<Boolean> choice) {
            boolChoice = choice;
        }

        public void addIntChoice(PrimitiveVS<Integer> choice) {
            intChoice = choice;
        }

        public void addElementChoice(PrimitiveVS<ValueSummary> choice) {
            elementChoice = choice;
        }

        public void clear() {
            senderChoice = new PrimitiveVS<>();
            boolChoice = new PrimitiveVS<>();
            intChoice = new PrimitiveVS<>();
            elementChoice = new PrimitiveVS<>();
        }
    }

    private List<Choice> fullChoice = new ArrayList<>();
    private List<Choice> repeatChoice = new ArrayList<>();
    private List<Choice> backtrackChoice = new ArrayList<>();
    int previousTransitionOverlap = 0;
    List<Integer> transitionCount = new ArrayList<>();
    int totalTransitionCountAllIterations = 0;
    int totalNewTransitionCountAllIterations = 0;

    public void addTransition(int depth) {
        if (depth >= transitionCount.size()) {
            transitionCount.add(1);
        } else {
            transitionCount.set(depth, transitionCount.get(depth) + 1);
        }
    }

    public void resetTransitionCount() {
        previousTransitionOverlap = 0;
        int scheduleChoices = 0;
        for (Choice choice : repeatChoice) {
            if (!choice.senderChoice.isEmptyVS()) scheduleChoices++;
        }
        for (int i = 0; i < scheduleChoices; i++) {
            if (!repeatChoice.get(i).isEmpty()) {
                previousTransitionOverlap += transitionCount.get(i);
            }
        }
        transitionCount.clear();
    }

    public int getNumBacktracks() {
        int count = 0;
        for (Choice backtrack : backtrackChoice) {
            if (!backtrack.isEmpty()) count++;
        }
        return count;
    }


    public void addSenderChoice(PrimitiveVS<Machine> choice, int depth) {
        if (depth >= fullChoice.size()) {
            fullChoice.add(new Choice());
        }
        fullChoice.get(depth).addSenderChoice(choice);
    }

    public void addBoolChoice(PrimitiveVS<Boolean> choice, int depth) {
        if (depth >= fullChoice.size()) {
            fullChoice.add(new Choice());
        }
        fullChoice.get(depth).addBoolChoice(choice);
    }

    public void addIntChoice(PrimitiveVS<Integer> choice, int depth) {
        if (depth >= fullChoice.size()) {
            fullChoice.add(new Choice());
        }
        fullChoice.get(depth).addIntChoice(choice);
    }

    public void addElementChoice(PrimitiveVS<ValueSummary> choice, int depth) {
        if (depth >= fullChoice.size()) {
            fullChoice.add(new Choice());
        }
        fullChoice.get(depth).addElementChoice(choice);
    }

    public void addRepeatSender(PrimitiveVS<Machine> choice, int depth) {
        // filter = filter.and(choice.getUniverse());
        if (depth >= repeatChoice.size()) {
            repeatChoice.add(new Choice());
        }
        repeatChoice.get(depth).addSenderChoice(choice);
    }

    public void addRepeatBool(PrimitiveVS<Boolean> choice, int depth) {
        if (depth >= repeatChoice.size()) {
            repeatChoice.add(new Choice());
        }
        repeatChoice.get(depth).addBoolChoice(choice);
    }

    public void addRepeatInt(PrimitiveVS<Integer> choice, int depth) {
        if (depth >= repeatChoice.size()) {
            repeatChoice.add(new Choice());
        }
        repeatChoice.get(depth).addIntChoice(choice);
    }

    public void addRepeatElement(PrimitiveVS<ValueSummary> choice, int depth) {
        if (depth >= repeatChoice.size()) {
            repeatChoice.add(new Choice());
        }
        repeatChoice.get(depth).addElementChoice(choice);
    }

    public void addBacktrackSender(PrimitiveVS<Machine> choice, int depth) {
        if (depth >= backtrackChoice.size()) {
            backtrackChoice.add(new Choice());
        }
        backtrackChoice.get(depth).addSenderChoice(choice);
    }

    public void addBacktrackBool(PrimitiveVS<Boolean> choice, int depth) {
        if (depth >= backtrackChoice.size()) {
            backtrackChoice.add(new Choice());
        }
        backtrackChoice.get(depth).addBoolChoice(choice);
    }

    public void addBacktrackInt(PrimitiveVS<Integer> choice, int depth) {
        if (depth >= backtrackChoice.size()) {
            backtrackChoice.add(new Choice());
        }
        backtrackChoice.get(depth).addIntChoice(choice);
    }

    public void addBacktrackElement(PrimitiveVS<ValueSummary> choice, int depth) {
        if (depth >= backtrackChoice.size()) {
            backtrackChoice.add(new Choice());
        }
        backtrackChoice.get(depth).addElementChoice(choice);
    }

    public Choice getFullChoice (int depth)  { return fullChoice.get(depth); }

    public PrimitiveVS<Machine> getSenderChoice(int depth) {
        return getFullChoice(depth).senderChoice;
    }

    public PrimitiveVS<Boolean> getBoolChoice(int depth) {
        return getFullChoice(depth).boolChoice;
    }

    public PrimitiveVS<Integer> getIntChoice(int depth) {
        return getFullChoice(depth).intChoice;
    }

    public ValueSummary getElementChoice(int depth) {
        return getFullChoice(depth).elementChoice;
    }

    public Choice getRepeatChoice (int depth)  { return repeatChoice.get(depth); }

    public PrimitiveVS<Machine> getRepeatSender(int depth) {
        return getRepeatChoice(depth).senderChoice;
    }

    public PrimitiveVS<Boolean> getRepeatBool(int depth) {
        return getRepeatChoice(depth).boolChoice;
    }

    public PrimitiveVS<Integer> getRepeatInt(int depth) { return getRepeatChoice(depth).intChoice; }

    public PrimitiveVS<ValueSummary> getRepeatElement(int depth) { return repeatChoice.get(depth).elementChoice; }

    public Choice getBacktrackChoice (int depth)  { return backtrackChoice.get(depth); }

    public PrimitiveVS<Machine> getBacktrackSender(int depth) {
        return getBacktrackChoice(depth).senderChoice;
    }

    public PrimitiveVS<Boolean> getBacktrackBool(int depth) {
        return getBacktrackChoice(depth).boolChoice;
    }

    public PrimitiveVS<Integer> getBacktrackInt(int depth) {
        return getBacktrackChoice(depth).intChoice;
    }

    public PrimitiveVS<ValueSummary> getBacktrackElement(int depth) { return getBacktrackChoice(depth).elementChoice; }

    public void clearChoice(int depth) {
        getFullChoice(depth).clear();
    }

    public void clearRepeat(int depth) {
        Choice choice = repeatChoice.get(depth);
        getRepeatChoice(depth).clear();
    }

    public void clearBacktrack(int depth) {
        getBacktrackChoice(depth).clear();
    }

    public int size() {
        return fullChoice.size();
    }

    private Map<Class<? extends Machine>, ListVS<PrimitiveVS<Machine>>> createdMachines = new HashMap<>();
    private Set<Machine> machines = new HashSet<>();

    private Guard pc = Guard.constTrue();

    public Schedule() {
    }

    private Schedule(List<Choice> fullChoice,
                     List<Choice> repeatChoice,
                     List<Choice> backtrackChoice,
                     Map<Class<? extends Machine>, ListVS<PrimitiveVS<Machine>>> createdMachines,
                     Set<Machine> machines,
                     Guard pc) {
        this.fullChoice = new ArrayList<>(fullChoice);
        this.repeatChoice = new ArrayList<>(repeatChoice);
        this.backtrackChoice = new ArrayList<>(backtrackChoice);
        this.createdMachines = new HashMap<>(createdMachines);
        this.machines = new HashSet<>(machines);
        this.pc = pc;
    }

    public Set<Machine> getMachines() {
        return machines;
    }

    public Schedule guard(Guard pc) {
        List<Choice> newFullChoice = new ArrayList<>();
        List<Choice> newRepeatChoice = new ArrayList<>();
        List<Choice> newBacktrackChoice = new ArrayList<>();
        for (Choice c : fullChoice) {
            newFullChoice.add(c.restrict(pc));
        }
        for (Choice c : repeatChoice) {
            newRepeatChoice.add(c.restrict(pc));
        }
        for (Choice c : backtrackChoice) {
            newBacktrackChoice.add(c.restrict(pc));
        }
        return new Schedule(newFullChoice, newRepeatChoice, newBacktrackChoice, createdMachines, machines, pc);
    }

    public Schedule removeEmptyRepeat() {
        List<Choice> newFullChoice = new ArrayList<>();
        List<Choice> newRepeatChoice = new ArrayList<>();
        List<Choice> newBacktrackChoice = new ArrayList<>();
        for (int i = 0; i < size(); i++) {
            if (!getRepeatChoice(i).isEmpty()) {
                newFullChoice.add(getFullChoice(i));
                newRepeatChoice.add(getRepeatChoice(i));
                newBacktrackChoice.add(getBacktrackChoice(i));
            }
        }
        return new Schedule(newFullChoice, newRepeatChoice, newBacktrackChoice, createdMachines, machines, pc);
    }

    public void guardRepeat(Guard pc) {
        for (int i = 0; i < repeatChoice.size(); i++) {
            repeatChoice.set(i, repeatChoice.get(i).restrict(pc));
        }
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
        for (Choice choice : repeatChoice) {
            Choice guarded = choice.restrict(pc);
            PrimitiveVS<Machine> sender = guarded.senderChoice;
            if (sender.getGuardedValues().size() > 0) {
                pc = pc.and(sender.getGuardedValues().get(0).getGuard());
            } else {
                PrimitiveVS<Boolean> boolChoice = guarded.boolChoice;
                if (boolChoice.getGuardedValues().size() > 0) {
                    pc = pc.and(boolChoice.getGuardedValues().get(0).getGuard());
                } else {
                    PrimitiveVS<Integer> intChoice = guarded.intChoice;
                    if (intChoice.getGuardedValues().size() > 0) {
                        pc = pc.and(intChoice.getGuardedValues().get(0).getGuard());
                    }
                    else {
                        PrimitiveVS<ValueSummary> elementChoice = guarded.elementChoice;
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
        return repeatChoice.get(size - 1).getUniverse();
    }

    public void printSchedule(FileWriter writer, int iter) {
        int i = 0;
        int transitionCountTotal = transitionCount.stream().reduce(0, (x, y) -> x + y);
        totalTransitionCountAllIterations += transitionCountTotal;
        totalNewTransitionCountAllIterations += transitionCountTotal - previousTransitionOverlap;
        try {
            writer.append("Iter " + iter + ": ");
            writer.append(System.lineSeparator());
            writer.append("Running Total Transitions:" + totalTransitionCountAllIterations);
            writer.append(System.lineSeparator());
            writer.append("Running Total New Transitions: " + totalNewTransitionCountAllIterations);
            writer.append(System.lineSeparator());
            writer.append("Total Transitions:" + transitionCountTotal);
            writer.append(System.lineSeparator());
            writer.append("Total New Transitions: " + (transitionCountTotal - previousTransitionOverlap));
            /*
            int choice = 0;
            for (Choice rc : repeatChoice) {
                writer.append(System.lineSeparator());
                writer.append("Choice " + choice++ +": ");
                if (!rc.boolChoice.isEmptyVS()) {
                    writer.append(rc.boolChoice.toString());
                }
                if (!rc.intChoice.isEmptyVS()) {
                    writer.append(rc.intChoice.toString());
                }
                if (!rc.senderChoice.isEmptyVS()) {
                    writer.append(rc.senderChoice.toString());
                }
            }
            */
            /*
            choice = 0;
            writer.append(System.lineSeparator());
            writer.append("Counts " + iter + ": ");
            for (Choice rc : repeatChoice) {
                writer.append(System.lineSeparator());
                writer.append("Choice " + choice++ + ": ");
                if (!rc.boolChoice.isEmptyVS()) {
                    writer.append(rc.boolChoice.getValues().size() + "");
                }
                if (!rc.intChoice.isEmptyVS()) {
                    writer.append(rc.intChoice.getValues().size() + "");
                }
                if (!rc.senderChoice.isEmptyVS()) {
                    writer.append(rc.senderChoice.getValues().size() + "");
                }
            } */
        } catch (IOException e) {
            e.printStackTrace();
        }
    }
}
