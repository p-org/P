package psym.runtime.scheduler.explicit;

import java.util.*;
import psym.runtime.machine.Machine;
import psym.runtime.machine.Monitor;
import psym.runtime.scheduler.symmetry.SymmetryTracker;
import psym.valuesummary.*;

public class ExplicitSymmetryTracker extends SymmetryTracker {
  Map<String, List<TreeSet<Machine>>> typeToSymmetryClasses;
  Machine pendingMerge;

  public ExplicitSymmetryTracker() {
    typeToSymmetryClasses = new HashMap<>();
    for (String type : typeToAllSymmetricMachines.keySet()) {
      typeToSymmetryClasses.put(type, null);
    }
    pendingMerge = null;
  }

  private ExplicitSymmetryTracker(
      Map<String, List<TreeSet<Machine>>> symClasses,
      Machine pending) {
    typeToSymmetryClasses = new HashMap<>();
    for (Map.Entry<String, List<TreeSet<Machine>>> entry: symClasses.entrySet()) {
      List<TreeSet<Machine>> newClasses = null;
      if (entry.getValue() != null) {
        newClasses = new ArrayList<>();
        for (TreeSet<Machine> symClass : entry.getValue()) {
          newClasses.add(new TreeSet<>(symClass));
        }
      }
      typeToSymmetryClasses.put(entry.getKey(), newClasses);
    }
    pendingMerge = pending;
  }

  public SymmetryTracker getCopy() {
    return new ExplicitSymmetryTracker(typeToSymmetryClasses, pendingMerge);
  }

  public void reset() {
    for (Set<Machine> symMachines : typeToAllSymmetricMachines.values()) {
      symMachines.clear();
    }
    typeToSymmetryClasses.replaceAll((t, v) -> null);
    pendingMerge = null;
  }

  public void createMachine(Machine machine, Guard guard) {
    assert (guard.isTrue());

    Set<Machine> symMachines = typeToAllSymmetricMachines.get(machine.getName());
    if (symMachines != null) {
      symMachines.add(machine);
    }
    if (typeToSymmetryClasses.containsKey(machine.getName())) {
      List<TreeSet<Machine>> symClasses = typeToSymmetryClasses.get(machine.getName());
      // initialize list summary if null
      if (symClasses == null) {
        symClasses = new ArrayList<>();
      }

      // if empty, create an empty class
      if (symClasses.isEmpty()) {
        symClasses.add(new TreeSet<>());
      }
      // get the first class
      TreeSet<Machine> symSet = symClasses.get(0);
      // add machine to the first class
      symSet.add(machine);
      // add updated first class to all class list
      symClasses.set(0, symSet);
      // add to map
      typeToSymmetryClasses.put(machine.getName(), symClasses);
    }
  }

  public List<ValueSummary> getReducedChoices(List<ValueSummary> original) {
    // trivial case
    if (original.size() <= 1 || typeToSymmetryClasses.isEmpty()) {
      return original;
    }

    // resulting symmetrically-reduced choices
    List<ValueSummary> reduced = new ArrayList<>();

    // set of choices to be added to reduced set
    Set<Machine> pendingSummaries = new HashSet<>();

    // for each choice
    for (ValueSummary choice : original) {
      boolean added = false;
      if (choice instanceof PrimitiveVS) {
        PrimitiveVS primitiveVS = (PrimitiveVS) choice;
        List<GuardedValue<?>> guardedValues = primitiveVS.getGuardedValues();

        // make sure choice has a single guarded value
        assert (guardedValues.size() == 1);

        Object value = guardedValues.get(0).getValue();
        if (value instanceof Machine) {
          Machine machine = ((Machine) value);

          // check if symmetric machine
          List<TreeSet<Machine>> symClasses =
              typeToSymmetryClasses.get(machine.getName());
          if (symClasses != null) {
            // check each symmetry class
            for (TreeSet<Machine> symSet : symClasses) {
              boolean hasMachine = symSet.contains(machine);

              if (hasMachine) {
                // machine is present in the symmetry class

                // get representative as first element of the class
                Machine m = symSet.first();
                assert (!m.sendBuffer.isEmpty());
                pendingSummaries.add(m);
              }
            }
            added = true;
          }
        }
      }
      if (!added) {
        reduced.add(choice);
      }
    }

    for (Machine m : pendingSummaries) {
      assert (!m.sendBuffer.isEmpty());
      reduced.add(new PrimitiveVS(Collections.singletonMap(m, Guard.constTrue())));
    }

    return reduced;
  }

  public void updateSymmetrySet(PrimitiveVS chosenVS) {
    List<? extends GuardedValue<?>> choices = ((PrimitiveVS<?>) chosenVS).getGuardedValues();
    for (GuardedValue<?> choice : choices) {
      Object value = choice.getValue();
      if (value instanceof Machine) {
        Machine machine = ((Machine) value);
        String type = machine.getName();

        List<TreeSet<Machine>> symClasses = typeToSymmetryClasses.get(type);
        if (symClasses != null) {
          // iterate over each symmetry class
          for (TreeSet<Machine> symSet: symClasses) {
            // remove chosen from the ith class
            symSet.remove(machine);
          }
          // remove empty classes
          symClasses.removeIf(x -> x.isEmpty());

          // add self as a single-element class
          TreeSet<Machine> selfSet = new TreeSet<>();
          selfSet.add(machine);
          // update symmetry classes map
          symClasses.add(selfSet);

          assert (symClasses.size() <= typeToAllSymmetricMachines.get(type).size());
          pendingMerge = machine;
        }
      }
    }
  }

  public void mergeAllSymmetryClasses() {
    if (pendingMerge != null) {
      mergeSymmetryClassesForType(pendingMerge);
      pendingMerge = null;
    }
  }

  private void mergeSymmetryClassesForType(Machine pending) {
    String type = pending.getName();
    List<TreeSet<Machine>> symClasses = typeToSymmetryClasses.get(type);

    for (int i = 0; i < symClasses.size()-1; i++) {
      for (int j = i+1; j < symClasses.size(); j++) {
        TreeSet<Machine> symClassI = symClasses.get(i);
        TreeSet<Machine> symClassJ = symClasses.get(j);
        if (symClassI.contains(pending) || symClassJ.contains(pending)) {
          TreeSet<Machine>[] mergedClasses = mergeSymmetryClassPair(symClassI, symClassJ);
          assert (mergedClasses.length == 2);
          symClasses.set(i, mergedClasses[0]);
          symClasses.set(j, mergedClasses[1]);
        }
      }
    }

    // remove empty classes
    symClasses.removeIf(x -> x.isEmpty());

    // update symmetry classes map
    assert (symClasses.size() <= typeToAllSymmetricMachines.get(type).size());
    typeToSymmetryClasses.put(type, symClasses);
  }

  private TreeSet<Machine>[] mergeSymmetryClassPair(
      TreeSet<Machine> lhs, TreeSet<Machine> rhs) {
    if (lhs.isEmpty() || rhs.isEmpty()) {
      return new TreeSet[] {lhs, rhs};
    } else {
      // get representative of lhs class
      Machine lhsRepMachine = lhs.first();
      // get representative of rhs class
      Machine rhsRepMachine = rhs.first();
      assert (lhsRepMachine != rhsRepMachine);

      if (isSymEquiv(lhsRepMachine, rhsRepMachine)) {
        // add rhs to lhs, and clear rhs
        lhs.addAll(rhs);
        rhs.clear();
      }
      return new TreeSet[] {lhs, rhs};
    }
  }

  private boolean haveSymEqLocalState(Machine m1, Machine m2) {
    assert (m1 != m2);

    List<ValueSummary> m1State = m1.getMachineLocalState().getLocals();
    List<ValueSummary> m2State = m2.getMachineLocalState().getLocals();
    assert (m1State.size() == m2State.size());

    for (int i = 0; i < m1State.size(); i++) {
      ValueSummary original = m1State.get(i);
      ValueSummary permuted = m2State.get(i).swap(m1, m2);
      if (original.isEmptyVS() && permuted.isEmptyVS()) {
        continue;
      }
      if (original.getConcreteHash() != permuted.getConcreteHash()) {
        return false;
      }
    }
    return true;
  }

  private boolean isSymEquiv(Machine m1, Machine m2) {
    assert (m1 != m2);

    if (!haveSymEqLocalState(m1, m2)) {
      return false;
    }

    for (Machine other : scheduler.getMachines()) {
      if (other == m1 || other == m2 || other instanceof Monitor) {
        continue;
      }
      for (ValueSummary original : other.getMachineLocalState().getLocals()) {
        ValueSummary permuted = original.swap(m1, m2);
        if (original.isEmptyVS() && permuted.isEmptyVS()) {
          continue;
        }
        if (original.getConcreteHash() != permuted.getConcreteHash()) {
          return false;
        }
      }
    }

    return true;
  }

}
