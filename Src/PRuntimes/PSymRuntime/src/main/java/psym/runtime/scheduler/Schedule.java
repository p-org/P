package psym.runtime.scheduler;

import java.io.Serializable;
import java.util.*;
import java.util.stream.Collectors;
import lombok.Getter;
import lombok.Setter;
import psym.runtime.PSymGlobal;
import psym.runtime.machine.Machine;
import psym.runtime.machine.MachineLocalState;
import psym.runtime.scheduler.search.symmetry.SymmetryTracker;
import psym.valuesummary.*;

public class Schedule implements Serializable {

  private final ChoiceState schedulerState = new ChoiceState();
  private Guard filter = Guard.constTrue();
  @Setter private int schedulerDepth = 0;
  @Setter private int schedulerChoiceDepth = 0;
  private int numBacktracks = 0;
  private int numDataBacktracks = 0;
  private SymmetryTracker schedulerSymmetry;
  private List<Choice> choices = new ArrayList<>();
  private Map<Class<? extends Machine>, ListVS<PrimitiveVS<Machine>>> createdMachines =
      new HashMap<>();
  private Set<Machine> machines = new HashSet<>();
  private Guard pc = Guard.constTrue();

  public Schedule(SymmetryTracker symmetryTracker) {
    this.schedulerSymmetry = symmetryTracker;
  }

  private Schedule() {}


  private Schedule(
      List<Choice> choices,
      Map<Class<? extends Machine>, ListVS<PrimitiveVS<Machine>>> createdMachines,
      Set<Machine> machines,
      Guard pc,
      SymmetryTracker symmetryTracker) {
    this.choices = new ArrayList<>(choices);
    this.createdMachines = new HashMap<>(createdMachines);
    this.machines = new HashSet<>(machines);
    this.pc = pc;
    this.schedulerSymmetry = symmetryTracker;
  }

  public void restrictFilter(Guard c) {
    filter = filter.and(c);
  }

  public Guard getFilter() {
    return filter;
  }

  public void setFilter(Guard c) {
    filter = c;
  }

  public void resetFilter() {
    filter = Guard.constTrue();
  }

  public Choice newChoice() {
    return new Choice();
  }

  public void setSchedulerState(
      Map<Machine, MachineLocalState> ms, Map<Class<? extends Machine>, PrimitiveVS<Integer>> mc) {
    schedulerState.copy(ms, mc);
  }

  public void setSchedulerSymmetry() {
    schedulerSymmetry = PSymGlobal.getSymmetryTracker();
  }

  public List<Choice> getChoices() {
    return choices;
  }

  public void setChoices(List<Choice> c) {
    choices = c;
  }

  public Choice getChoice(int d) {
    return choices.get(d);
  }

  public void setChoice(int d, Choice choice) {
    choices.set(d, choice);
  }

  public void clearChoice(int d) {
    choices.get(d).clear();
  }

  public void setNumBacktracksInSchedule() {
    numBacktracks = 0;
    numDataBacktracks = 0;
    for (Choice backtrack : choices) {
      if (backtrack.isBacktrackNonEmpty()) {
        numBacktracks++;
        if (backtrack.isDataBacktrackNonEmpty()) {
          numDataBacktracks++;
        }
      }
    }
  }

  public int getNumBacktracksInSchedule() {
    return numBacktracks;
  }

  public int getNumDataBacktracksInSchedule() {
    return numDataBacktracks;
  }

  public void addRepeatSchedulingChoice(PrimitiveVS<Machine> choice, int depth) {
    if (depth >= choices.size()) {
      choices.add(newChoice());
    }
    choices.get(depth).addRepeatSchedulingChoice(choice);
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

  public void addBacktrackSchedulingChoice(List<PrimitiveVS<Machine>> machines, int depth) {
    if (depth >= choices.size()) {
      choices.add(newChoice());
    }
    if (machines.isEmpty()) {
      choices
          .get(depth)
          .storeState(schedulerDepth, schedulerChoiceDepth, null, filter, schedulerSymmetry);
    } else {
      choices
          .get(depth)
          .storeState(
              schedulerDepth, schedulerChoiceDepth, schedulerState, filter, schedulerSymmetry);
      numBacktracks++;
    }
    for (PrimitiveVS<Machine> choice : machines) {
      choices.get(depth).addBacktrackSchedulingChoice(choice);
    }
  }

  public void addBacktrackBool(List<PrimitiveVS<Boolean>> bools, int depth) {
    if (depth >= choices.size()) {
      choices.add(newChoice());
    }
    if (bools.isEmpty()) {
      choices
          .get(depth)
          .storeState(schedulerDepth, schedulerChoiceDepth, null, filter, schedulerSymmetry);
    } else {
      choices
          .get(depth)
          .storeState(
              schedulerDepth, schedulerChoiceDepth, schedulerState, filter, schedulerSymmetry);
      numBacktracks++;
      numDataBacktracks++;
    }
    for (PrimitiveVS<Boolean> choice : bools) {
      choices.get(depth).addBacktrackBool(choice);
    }
  }

  public void addBacktrackInt(List<PrimitiveVS<Integer>> ints, int depth) {
    if (depth >= choices.size()) {
      choices.add(newChoice());
    }
    if (ints.isEmpty()) {
      choices
          .get(depth)
          .storeState(schedulerDepth, schedulerChoiceDepth, null, filter, schedulerSymmetry);
    } else {
      choices
          .get(depth)
          .storeState(
              schedulerDepth, schedulerChoiceDepth, schedulerState, filter, schedulerSymmetry);
      numBacktracks++;
      numDataBacktracks++;
    }
    for (PrimitiveVS<Integer> choice : ints) {
      choices.get(depth).addBacktrackInt(choice);
    }
  }

  public void addBacktrackElement(List<PrimitiveVS<ValueSummary>> elements, int depth) {
    if (depth >= choices.size()) {
      choices.add(newChoice());
    }
    if (elements.isEmpty()) {
      choices
              .get(depth)
              .storeState(schedulerDepth, schedulerChoiceDepth, null, filter, schedulerSymmetry);
    } else {
      choices
              .get(depth)
              .storeState(
                      schedulerDepth, schedulerChoiceDepth, schedulerState, filter, schedulerSymmetry);
      numBacktracks++;
      numDataBacktracks++;
    }
    for (PrimitiveVS<ValueSummary> choice : elements) {
      choices.get(depth).addBacktrackElement(choice);
    }
  }

  public PrimitiveVS<Machine> getRepeatSchedulingChoice(int depth) {
    return choices.get(depth).getRepeatSchedulingChoice();
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

  public List<PrimitiveVS<Machine>> getBacktrackSchedulingChoice(int depth) {
    return choices.get(depth).getBacktrackSchedulingChoice();
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

  public Set<Machine> getMachines() {
    return machines;
  }

  public Schedule guard(Guard pc) {
    List<Choice> newChoices = new ArrayList<>();
    for (Choice c : choices) {
      newChoices.add(c.restrict(pc));
    }
    return new Schedule(newChoices, createdMachines, machines, pc, schedulerSymmetry);
  }

  public void makeMachine(Machine m, Guard pc) {
    PrimitiveVS<Machine> toAdd = new PrimitiveVS<>(m).restrict(pc);
    if (createdMachines.containsKey(m.getClass())) {
      createdMachines.put(m.getClass(), createdMachines.get(m.getClass()).add(toAdd));
    } else {
      createdMachines.put(
          m.getClass(), new ListVS<PrimitiveVS<Machine>>(Guard.constTrue()).add(toAdd));
    }
    machines.add(m);
  }

  public boolean hasMachine(
      Class<? extends Machine> type, PrimitiveVS<Integer> idx, Guard otherPc) {
    if (!createdMachines.containsKey(type)) return false;
    // TODO: may need fixing
    if (!createdMachines.get(type).inRange(idx).getGuardFor(false).isFalse()) return false;
    PrimitiveVS<Machine> machines = createdMachines.get(type).get(idx);
    return !machines.restrict(pc).restrict(otherPc).getUniverse().isFalse();
  }

  public PrimitiveVS<Machine> getMachine(Class<? extends Machine> type, PrimitiveVS<Integer> idx) {
    PrimitiveVS<Machine> machines = createdMachines.get(type).get(idx);
    return machines.restrict(pc);
  }

  public Schedule getSingleSchedule() {
    Schedule result = new Schedule();

    Guard pc = Guard.constTrue();
    pc = pc.and(getFilter());

    int dNew = 0;
    for (int d = 0; d < choices.size(); d++) {
      Choice choice = choices.get(d);
      Choice guarded = choice.restrict(pc);
      PrimitiveVS<Machine> schedulingChoice = guarded.getRepeatSchedulingChoice();
      if (schedulingChoice.getGuardedValues().size() > 0) {
        // add schedule choice
        pc = pc.and(schedulingChoice.getGuardedValues().get(0).getGuard());
        result.addRepeatSchedulingChoice(new PrimitiveVS<>(schedulingChoice.getGuardedValues().get(0).getValue()), dNew++);
      } else {
        // add bool choice
        PrimitiveVS<Boolean> boolChoice = guarded.getRepeatBool();
        if (boolChoice.getGuardedValues().size() > 0) {
          pc = pc.and(boolChoice.getGuardedValues().get(0).getGuard());
          result.addRepeatBool(new PrimitiveVS<>(boolChoice.getGuardedValues().get(0).getValue()), dNew++);
        } else {
          // add int choice
          PrimitiveVS<Integer> intChoice = guarded.getRepeatInt();
          if (intChoice.getGuardedValues().size() > 0) {
            pc = pc.and(intChoice.getGuardedValues().get(0).getGuard());
            result.addRepeatInt(new PrimitiveVS<>(intChoice.getGuardedValues().get(0).getValue()), dNew++);
          } else {
            // add element choice
            PrimitiveVS<ValueSummary> elementChoice = guarded.getRepeatElement();
            if (elementChoice.getGuardedValues().size() > 0) {
              pc = pc.and(elementChoice.getGuardedValues().get(0).getGuard());
              result.addRepeatElement(new PrimitiveVS<>(elementChoice.getGuardedValues().get(0).getValue()), dNew++);
            }
          }
        }
      }
    }

    for (Map.Entry<Class<? extends Machine>, ListVS<PrimitiveVS<Machine>>> entry: createdMachines.entrySet()) {
      ListVS<PrimitiveVS<Machine>> machineListVs = entry.getValue().restrict(pc);
      for (PrimitiveVS<Machine> machineVs: machineListVs.getItems()) {
        for (Machine m: machineVs.getValues()) {
          result.makeMachine(m, Guard.constTrue());
        }
      }
    }

    return result;
  }

  public Guard getLengthCond(int size) {
    if (size == 0) return Guard.constFalse();
    return choices.get(size - 1).getRepeatUniverse();
  }

  public static class ChoiceState implements Serializable {
    @Getter private Map<Machine, MachineLocalState> machineStates;
    @Getter private Map<Class<? extends Machine>, PrimitiveVS<Integer>> machineCounters;

    public ChoiceState() {
      this(new HashMap<>(), new HashMap<>());
    }

    public ChoiceState(
        Map<Machine, MachineLocalState> ms,
        Map<Class<? extends Machine>, PrimitiveVS<Integer>> mc) {
      this.machineStates = new HashMap<>(ms);
      this.machineCounters = new HashMap<>(mc);
    }

    public void copy(
        Map<Machine, MachineLocalState> ms,
        Map<Class<? extends Machine>, PrimitiveVS<Integer>> mc) {
      this.machineStates = new HashMap<>(ms);
      this.machineCounters = new HashMap<>(mc);
    }
  }

  public class Choice implements Serializable {
    @Getter PrimitiveVS<Machine> repeatSchedulingChoice = new PrimitiveVS<>();
    @Getter PrimitiveVS<Boolean> repeatBool = new PrimitiveVS<>();
    @Getter PrimitiveVS<Integer> repeatInt = new PrimitiveVS<>();
    @Getter PrimitiveVS<ValueSummary> repeatElement = new PrimitiveVS<>();
    @Getter List<PrimitiveVS<Machine>> backtrackSchedulingChoice = new ArrayList<>();
    @Getter List<PrimitiveVS<Boolean>> backtrackBool = new ArrayList();
    @Getter List<PrimitiveVS<Integer>> backtrackInt = new ArrayList<>();
    @Getter List<PrimitiveVS<ValueSummary>> backtrackElement = new ArrayList<>();
    @Getter Guard handledUniverse = Guard.constFalse();
    @Getter int schedulerDepth = 0;
    @Getter int schedulerChoiceDepth = 0;
    @Getter ChoiceState choiceState = null;
    @Getter Guard filter = null;
    @Getter SymmetryTracker symmetry = null;

    public Choice() {}

    /**
     * Copy-constructor for Choice
     *
     * @param old The Choice to copy
     */
    public Choice(Choice old) {
      repeatSchedulingChoice = new PrimitiveVS<>(old.repeatSchedulingChoice);
      repeatBool = new PrimitiveVS<>(old.repeatBool);
      repeatInt = new PrimitiveVS<>(old.repeatInt);
      repeatElement = new PrimitiveVS<>(old.repeatElement);
      backtrackSchedulingChoice = new ArrayList<>(old.backtrackSchedulingChoice);
      backtrackBool = new ArrayList<>(old.backtrackBool);
      backtrackInt = new ArrayList<>(old.backtrackInt);
      backtrackElement = new ArrayList<>(old.backtrackElement);
      handledUniverse = old.handledUniverse;
      schedulerDepth = old.schedulerDepth;
      schedulerChoiceDepth = old.schedulerChoiceDepth;
      choiceState = old.choiceState;
      filter = old.filter;
      symmetry = old.symmetry;
    }

    /**
     * Copy the Choice
     *
     * @return A new cloned copy of the Choice
     */
    public Choice getCopy() {
      return new Choice(this);
    }

    public ChoiceState copyState(ChoiceState state) {
      if (state == null) return null;
      return new ChoiceState(state.getMachineStates(), state.getMachineCounters());
    }

    public void storeState(int depth, int cdepth, ChoiceState state, Guard f, SymmetryTracker sym) {
      schedulerDepth = depth;
      schedulerChoiceDepth = cdepth;
      choiceState = copyState(state);
      filter = f;
      symmetry = sym.getCopy();
    }

    public int getNumChoicesExplored() {
      return repeatSchedulingChoice.getValues().size()
          + repeatBool.getValues().size()
          + repeatInt.getValues().size()
          + repeatElement.getValues().size();
    }

    public Guard getRepeatUniverse() {
      return repeatSchedulingChoice
          .getUniverse()
          .or(repeatBool.getUniverse()
            .or(repeatInt.getUniverse()
              .or(repeatElement.getUniverse())));
    }

    public Guard getBacktrackUniverse() {
      Guard backtrackUniverse = Guard.constFalse();
      for (PrimitiveVS<Machine> machine : backtrackSchedulingChoice) {
        backtrackUniverse = backtrackUniverse.or(machine.getUniverse());
      }
      for (PrimitiveVS<Boolean> bool : backtrackBool) {
        backtrackUniverse = backtrackUniverse.or(bool.getUniverse());
      }
      for (PrimitiveVS<Integer> integer : backtrackInt) {
        backtrackUniverse = backtrackUniverse.or(integer.getUniverse());
      }
      for (PrimitiveVS<ValueSummary> element : backtrackElement) {
        backtrackUniverse = backtrackUniverse.or(element.getUniverse());
      }
      return backtrackUniverse;
    }

    public boolean isRepeatEmpty() {
      return getRepeatUniverse().isFalse();
    }

    public boolean isBacktrackNonEmpty() {
      return isScheduleBacktrackNonEmpty() || isDataBacktrackNonEmpty();
    }

    public boolean isScheduleBacktrackNonEmpty() {
      return !getBacktrackSchedulingChoice().isEmpty();
    }

    public boolean isDataBacktrackNonEmpty() {
      return !getBacktrackBool().isEmpty()
          || !getBacktrackInt().isEmpty()
          || !getBacktrackElement().isEmpty();
    }

    public Choice restrict(Guard pc) {
      Choice c = newChoice();
      c.repeatSchedulingChoice = repeatSchedulingChoice.restrict(pc);
      c.repeatBool = repeatBool.restrict(pc);
      c.repeatInt = repeatInt.restrict(pc);
      c.repeatElement = repeatElement.restrict(pc);
      c.backtrackSchedulingChoice =
          backtrackSchedulingChoice.stream()
              .map(x -> x.restrict(pc))
              .filter(x -> !x.isEmptyVS())
              .collect(Collectors.toList());
      c.backtrackBool =
          backtrackBool.stream()
              .map(x -> x.restrict(pc))
              .filter(x -> !x.isEmptyVS())
              .collect(Collectors.toList());
      c.backtrackInt =
          backtrackInt.stream()
              .map(x -> x.restrict(pc))
              .filter(x -> !x.isEmptyVS())
              .collect(Collectors.toList());
      c.backtrackElement =
              backtrackElement.stream()
                      .map(x -> x.restrict(pc))
                      .filter(x -> !x.isEmptyVS())
                      .collect(Collectors.toList());
      c.storeState(
          this.schedulerDepth,
          this.schedulerChoiceDepth,
          this.choiceState,
          this.filter,
          this.symmetry);
      return c;
    }

    public void updateHandledUniverse(Guard update) {
      handledUniverse = handledUniverse.or(update);
    }

    public void addRepeatSchedulingChoice(PrimitiveVS<Machine> choice) {
      repeatSchedulingChoice = choice;
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

    public void clearRepeat() {
      repeatSchedulingChoice = new PrimitiveVS<>();
      repeatBool = new PrimitiveVS<>();
      repeatInt = new PrimitiveVS<>();
      repeatElement = new PrimitiveVS<>();
    }

    public void addBacktrackSchedulingChoice(PrimitiveVS<Machine> choice) {
      if (!choice.isEmptyVS()) backtrackSchedulingChoice.add(choice);
    }

    public void addBacktrackBool(PrimitiveVS<Boolean> choice) {
      if (!choice.isEmptyVS()) backtrackBool.add(choice);
    }

    public void addBacktrackInt(PrimitiveVS<Integer> choice) {
      if (!choice.isEmptyVS()) backtrackInt.add(choice);
    }

    public void addBacktrackElement(PrimitiveVS<ValueSummary> choice) {
      if (!choice.isEmptyVS()) backtrackElement.add(choice);
    }

    public void clearBacktrack() {
      backtrackSchedulingChoice = new ArrayList<>();
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
}
