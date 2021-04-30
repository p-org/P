package psymbolic.runtime;

import psymbolic.valuesummary.GuardedValue;
import psymbolic.valuesummary.ListVS;
import psymbolic.valuesummary.PrimVS;
import psymbolic.valuesummary.ValueSummary;
import psymbolic.valuesummary.bdd.Bdd;

import java.io.FileWriter;
import java.io.IOException;
import java.util.*;

public class Schedule {

    private Bdd filter = Bdd.constTrue();

    public VectorClockManager getNewVectorClockManager() {
        return new VectorClockManager(false);
    }

    /** The vector clock manager */
    public final VectorClockManager vcManager = getNewVectorClockManager();

    public Bdd getFilter() {
        return filter;
    }

    public void setFilter(Bdd set) {
        filter = set;
    }

    public class Choice {
        PrimVS<Machine> senderChoice = new PrimVS<>();
        PrimVS<Boolean> boolChoice = new PrimVS<>();
        PrimVS<Integer> intChoice = new PrimVS<>();
        PrimVS<ValueSummary> elementChoice = new PrimVS<>();
        Event eventChosen = new Event();

        public Choice() {
        }

        public Choice(PrimVS<Machine> senderChoice, PrimVS<Boolean> boolChoice, PrimVS<Integer> intChoice,
                      Event eventChosen) {
            this.senderChoice = senderChoice;
            this.boolChoice = boolChoice;
            this.intChoice = intChoice;
            this.eventChosen = eventChosen;
        }

        public Bdd getUniverse() {
            return senderChoice.getUniverse().or(boolChoice.getUniverse().or(intChoice.getUniverse()));
        }

        public boolean isEmpty() {
            return getUniverse().isConstFalse();
        }

        public Choice guard(Bdd pc) {
            Choice c = new Choice();
            c.senderChoice = senderChoice.guard(pc);
            c.boolChoice = boolChoice.guard(pc);
            c.intChoice = intChoice.guard(pc);
            c.elementChoice = elementChoice.guard(pc);
            c.eventChosen = eventChosen.guard(pc);
            return c;
        }

        public void addSenderChoice(PrimVS<Machine> choice) {
            senderChoice = choice;
            List<Event> toMerge = new ArrayList<>();
            for (GuardedValue<Machine> guardedValue : choice.getGuardedValues()) {
                toMerge.add(guardedValue.value.sendEffects.peek(guardedValue.guard));
            }
            eventChosen = new Event();
            eventChosen = eventChosen.merge(toMerge);
        }

        public void addBoolChoice(PrimVS<Boolean> choice) {
            boolChoice = choice;
        }

        public void addIntChoice(PrimVS<Integer> choice) {
            intChoice = choice;
        }

        public void addElementChoice(PrimVS<ValueSummary> choice) {
            elementChoice = choice;
        }

        public void clear() {
            senderChoice = new PrimVS<>();
            boolChoice = new PrimVS<>();
            intChoice = new PrimVS<>();
            elementChoice = new PrimVS<>();
            eventChosen = new Event();
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


    public void addSenderChoice(PrimVS<Machine> choice, int depth) {
        if (depth >= fullChoice.size()) {
            fullChoice.add(new Choice());
        }
        fullChoice.get(depth).addSenderChoice(choice);
    }

    public void addBoolChoice(PrimVS<Boolean> choice, int depth) {
        if (depth >= fullChoice.size()) {
            fullChoice.add(new Choice());
        }
        fullChoice.get(depth).addBoolChoice(choice);
    }

    public void addIntChoice(PrimVS<Integer> choice, int depth) {
        if (depth >= fullChoice.size()) {
            fullChoice.add(new Choice());
        }
        fullChoice.get(depth).addIntChoice(choice);
    }

    public void addElementChoice(PrimVS<ValueSummary> choice, int depth) {
        if (depth >= fullChoice.size()) {
            fullChoice.add(new Choice());
        }
        fullChoice.get(depth).addElementChoice(choice);
    }

    public void addRepeatSender(PrimVS<Machine> choice, int depth) {
        // filter = filter.and(choice.getUniverse());
        if (depth >= repeatChoice.size()) {
            repeatChoice.add(new Choice());
        }
        repeatChoice.get(depth).addSenderChoice(choice.guard(filter));
    }

    public void addRepeatBool(PrimVS<Boolean> choice, int depth) {
        if (depth >= repeatChoice.size()) {
            repeatChoice.add(new Choice().guard(filter));
        }
        repeatChoice.get(depth).addBoolChoice(choice.guard(filter));
    }

    public void addRepeatInt(PrimVS<Integer> choice, int depth) {
        if (depth >= repeatChoice.size()) {
            repeatChoice.add(new Choice());
        }
        repeatChoice.get(depth).addIntChoice(choice.guard(filter));
    }

    public void addRepeatElement(PrimVS<ValueSummary> choice, int depth) {
        filter = filter.and(choice.getUniverse());
        if (depth >= repeatChoice.size()) {
            repeatChoice.add(new Choice());
        }
        repeatChoice.get(depth).addElementChoice(choice.guard(filter));
    }

    public void addBacktrackSender(PrimVS<Machine> choice, int depth) {
        if (depth >= backtrackChoice.size()) {
            backtrackChoice.add(new Choice());
        }
        backtrackChoice.get(depth).addSenderChoice(choice);
    }

    public void addBacktrackBool(PrimVS<Boolean> choice, int depth) {
        if (depth >= backtrackChoice.size()) {
            backtrackChoice.add(new Choice());
        }
        backtrackChoice.get(depth).addBoolChoice(choice);
    }

    public void addBacktrackInt(PrimVS<Integer> choice, int depth) {
        if (depth >= backtrackChoice.size()) {
            backtrackChoice.add(new Choice());
        }
        backtrackChoice.get(depth).addIntChoice(choice);
    }

    public void addBacktrackElement(PrimVS<ValueSummary> choice, int depth) {
        if (depth >= backtrackChoice.size()) {
            backtrackChoice.add(new Choice());
        }
        backtrackChoice.get(depth).addElementChoice(choice);
    }

    public Choice getFullChoice (int depth)  { return fullChoice.get(depth); }

    public PrimVS<Machine> getSenderChoice(int depth) {
        return getFullChoice(depth).senderChoice;
    }

    public PrimVS<Boolean> getBoolChoice(int depth) {
        return getFullChoice(depth).boolChoice;
    }

    public PrimVS<Integer> getIntChoice(int depth) {
        return getFullChoice(depth).intChoice;
    }

    public ValueSummary getElementChoice(int depth) {
        return getFullChoice(depth).elementChoice;
    }

    public Choice getRepeatChoice (int depth)  { return repeatChoice.get(depth); }

    public PrimVS<Machine> getRepeatSender(int depth) {
        return getRepeatChoice(depth).senderChoice;
    }

    public PrimVS<Boolean> getRepeatBool(int depth) {
        return getRepeatChoice(depth).boolChoice;
    }

    public PrimVS<Integer> getRepeatInt(int depth) { return getRepeatChoice(depth).intChoice; }

    public PrimVS<ValueSummary> getRepeatElement(int depth) { return repeatChoice.get(depth).elementChoice; }

    public Choice getBacktrackChoice (int depth)  { return backtrackChoice.get(depth); }

    public PrimVS<Machine> getBacktrackSender(int depth) {
        return getBacktrackChoice(depth).senderChoice;
    }

    public PrimVS<Boolean> getBacktrackBool(int depth) {
        return getBacktrackChoice(depth).boolChoice;
    }

    public PrimVS<Integer> getBacktrackInt(int depth) {
        return getBacktrackChoice(depth).intChoice;
    }

    public PrimVS<ValueSummary> getBacktrackElement(int depth) { return getBacktrackChoice(depth).elementChoice; }

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

    private Map<Class<? extends Machine>, ListVS<PrimVS<Machine>>> createdMachines = new HashMap<>();
    private Set<Machine> machines = new HashSet<>();

    private Bdd pc = Bdd.constTrue();

    public Schedule() {
    }

    private Schedule(List<Choice> fullChoice,
                     List<Choice> repeatChoice,
                     List<Choice> backtrackChoice,
                     Map<Class<? extends Machine>, ListVS<PrimVS<Machine>>> createdMachines,
                     Set<Machine> machines,
                     Bdd pc) {
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

    public Schedule guard(Bdd pc) {
        List<Choice> newFullChoice = new ArrayList<>();
        List<Choice> newRepeatChoice = new ArrayList<>();
        List<Choice> newBacktrackChoice = new ArrayList<>();
        for (Choice c : fullChoice) {
            newFullChoice.add(c.guard(pc));
        }
        for (Choice c : repeatChoice) {
            newRepeatChoice.add(c.guard(pc));
        }
        for (Choice c : backtrackChoice) {
            newBacktrackChoice.add(c.guard(pc));
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

    public void guardRepeat(Bdd pc) {
        for (int i = 0; i < repeatChoice.size(); i++) {
            repeatChoice.set(i, repeatChoice.get(i).guard(pc));
        }
    }

    public void makeMachine(Machine m, Bdd pc) {
        PrimVS<Machine> toAdd = new PrimVS<>(m).guard(pc);
        if (createdMachines.containsKey(m.getClass())) {
            createdMachines.put(m.getClass(), createdMachines.get(m.getClass()).add(toAdd));
        } else {
            createdMachines.put(m.getClass(), new ListVS<PrimVS<Machine>>(Bdd.constTrue()).add(toAdd));
        }
        machines.add(m);
        vcManager.addMachine(toAdd.getUniverse(), m);
    }

    public boolean hasMachine(Class<? extends Machine> type, PrimVS<Integer> idx, Bdd otherPc) {
        if (!createdMachines.containsKey(type)) return false;
        // TODO: may need fixing
        //ScheduleLogger.log("has machine of type");
        //ScheduleLogger.log(idx + " in range? " + createdMachines.get(type).inRange(idx).getGuard(false));
        if (!createdMachines.get(type).inRange(idx).getGuard(false).isConstFalse()) return false;
        PrimVS<Machine> machines = createdMachines.get(type).get(idx);
        return !machines.guard(pc).guard(otherPc).getUniverse().isConstFalse();
    }

    public PrimVS<Machine> getMachine(Class<? extends Machine> type, PrimVS<Integer> idx) {
        PrimVS<Machine> machines = createdMachines.get(type).get(idx);
        return machines.guard(pc);
    }

    public Schedule getSingleSchedule() {
        Bdd pc = Bdd.constTrue();
        for (Choice choice : repeatChoice) {
            Choice guarded = choice.guard(pc);
            PrimVS<Machine> sender = guarded.senderChoice;
            if (sender.getGuardedValues().size() > 0) {
                pc = pc.and(sender.getGuardedValues().get(0).guard);
            } else {
                PrimVS<Boolean> boolChoice = guarded.boolChoice;
                if (boolChoice.getGuardedValues().size() > 0) {
                    pc = pc.and(boolChoice.getGuardedValues().get(0).guard);
                } else {
                    PrimVS<Integer> intChoice = guarded.intChoice;
                    if (intChoice.getGuardedValues().size() > 0) {
                        pc = pc.and(intChoice.getGuardedValues().get(0).guard);
                    }
                }
            }
        }
        return this.guard(pc).removeEmptyRepeat();
    }

    public Bdd getLengthCond(int size) {
        if (size == 0) return Bdd.constFalse();
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
