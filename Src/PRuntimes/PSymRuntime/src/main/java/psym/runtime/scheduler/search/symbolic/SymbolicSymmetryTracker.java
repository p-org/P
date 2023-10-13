package psym.runtime.scheduler.search.symbolic;

import java.util.*;
import psym.runtime.PSymGlobal;
import psym.runtime.machine.Machine;
import psym.runtime.machine.Monitor;
import psym.runtime.scheduler.search.symmetry.SymmetryPendingMerges;
import psym.runtime.scheduler.search.symmetry.SymmetryTracker;
import psym.valuesummary.*;

public class SymbolicSymmetryTracker extends SymmetryTracker {
  Map<String, ListVS<SetVS<PrimitiveVS<Machine>>>> typeToSymmetryClasses;
  Map<String, SymmetryPendingMerges> typeToPendingMerges;

  public SymbolicSymmetryTracker() {
    typeToSymmetryClasses = new HashMap<>();
    for (String type : typeToAllSymmetricMachines.keySet()) {
      typeToSymmetryClasses.put(type, null);
    }
    typeToPendingMerges = new HashMap<>();
  }

  private SymbolicSymmetryTracker(
      Map<String, ListVS<SetVS<PrimitiveVS<Machine>>>> symClasses,
      Map<String, SymmetryPendingMerges> pending) {
    typeToSymmetryClasses = new HashMap<>(symClasses);
    typeToPendingMerges = new HashMap<>(pending);
  }

  public SymmetryTracker getCopy() {
    return new SymbolicSymmetryTracker(typeToSymmetryClasses, typeToPendingMerges);
  }

  public void reset() {
    for (Set<Machine> symMachines : typeToAllSymmetricMachines.values()) {
      symMachines.clear();
    }
    typeToSymmetryClasses.replaceAll((t, v) -> null);
    typeToPendingMerges.clear();
  }

  public void createMachine(Machine machine, Guard guard) {
    assert (guard.isTrue());

    Set<Machine> symMachines = typeToAllSymmetricMachines.get(machine.getName());
    if (symMachines != null) {
      symMachines.add(machine);
    }
    if (typeToSymmetryClasses.containsKey(machine.getName())) {
      ListVS<SetVS<PrimitiveVS<Machine>>> symClasses = typeToSymmetryClasses.get(machine.getName());
      // initialize list summary if null
      if (symClasses == null) {
        symClasses = new ListVS<>(Guard.constTrue());
      }

      // if empty, create an empty class
      if (symClasses.isEmpty()) {
        symClasses = symClasses.add(new SetVS<>(guard));
      }
      PrimitiveVS<Integer> indexVS = new PrimitiveVS<>(0, guard);
      // get the first class
      SetVS<PrimitiveVS<Machine>> symSet = symClasses.get(indexVS);
      // add machine to the first class
      symSet = symSet.add(new PrimitiveVS<>(machine, guard));
      // add updated first class to all class list
      symClasses = symClasses.set(indexVS, symSet);
      // add to map
      typeToSymmetryClasses.put(machine.getName(), symClasses);
    }
  }

  public List<ValueSummary> getReducedChoices(List<ValueSummary> original, boolean isData) {
    // trivial case
    if (original.size() <= 1 || typeToSymmetryClasses.isEmpty()) {
      return original;
    }

    // resulting symmetrically-reduced choices
    List<ValueSummary> reduced = new ArrayList<>();

    // set of choices to be added to reduced set
    Map<Machine, Guard> pendingSummaries = new HashMap<>();

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
          ListVS<SetVS<PrimitiveVS<Machine>>> symClasses =
              typeToSymmetryClasses.get(machine.getName());
          if (symClasses != null) {
            Guard guard = guardedValues.get(0).getGuard();

            // remaining guard tracks if any condition remains
            Guard remaining = guard;

            // check each symmetry class
            for (SetVS<PrimitiveVS<Machine>> symSet : symClasses.getItems()) {
              Guard hasMachine = symSet.contains(primitiveVS).getGuardFor(true);
              Guard classGuard = guard.and(hasMachine);

              if (!classGuard.isFalse()) {
                // machine is present in the symmetry class

                // get representative as first element of the class
                PrimitiveVS<Machine> representativeVS =
                    symSet.get(new PrimitiveVS<>(0, classGuard));

                // if multiple guarded values in the representative, add them one by one
                List<GuardedValue<Machine>> representativeGVs = representativeVS.getGuardedValues();
                for (GuardedValue<Machine> representativeGV : representativeGVs) {
                  Machine m = representativeGV.getValue();
                  Guard g = representativeGV.getGuard();
                  Guard currentGuard = pendingSummaries.get(m);
                  if (currentGuard == null) {
                    currentGuard = Guard.constFalse();
                  }
                  currentGuard = currentGuard.or(g);

                  assert (!BooleanVS.isEverTrue(
                      IntegerVS.lessThan(m.getEventBuffer().size().restrict(currentGuard), 1)));
                  pendingSummaries.put(m, currentGuard);
                }

                // update remaining
                remaining = remaining.and(classGuard.not());
              }
            }

            // make sure all conditions are covered and nothing remains
            assert (remaining.isFalse());

            added = true;
          }
        }
      }
      if (!added) {
        reduced.add(choice);
      }
    }

    for (Map.Entry<Machine, Guard> entry : pendingSummaries.entrySet()) {
      assert (!BooleanVS.isEverTrue(
          IntegerVS.lessThan(entry.getKey().getEventBuffer().size().restrict(entry.getValue()), 1)));
      reduced.add(new PrimitiveVS(Collections.singletonMap(entry.getKey(), entry.getValue())));
    }

    return reduced;
  }

  public void updateSymmetrySet(Machine machine, Guard guard) {
    String type = machine.getName();

    ListVS<SetVS<PrimitiveVS<Machine>>> symClasses = typeToSymmetryClasses.get(type);
    if (symClasses != null) {
      PrimitiveVS<Machine> primitiveVS = new PrimitiveVS<>(machine, guard);
      ListVS<SetVS<PrimitiveVS<Machine>>> newClasses = new ListVS<>(Guard.constTrue());

      // iterate over each symmetry class
      PrimitiveVS<Integer> size = symClasses.size();
      PrimitiveVS<Integer> zero = new PrimitiveVS<>(0);
      PrimitiveVS<Integer> indexI = new PrimitiveVS<>(0, size.getUniverse());
      while (BooleanVS.isEverTrue(IntegerVS.lessThan(indexI, size))) {
        Guard condI = BooleanVS.getTrueGuard(IntegerVS.lessThan(indexI, size));

        if (!condI.isFalse()) {
          PrimitiveVS<Integer> condIndexI = indexI.restrict(condI);
          SetVS<PrimitiveVS<Machine>> symClassI = symClasses.get(condIndexI);

          // remove chosen from the ith class
          symClassI = symClassI.remove(primitiveVS);

          // if class not empty, add to new classes
          Guard isNonEmpty = BooleanVS.getFalseGuard(IntegerVS.equalTo(symClassI.size(), zero));
          if (!isNonEmpty.isFalse()) {
            assert (!BooleanVS.isEverTrue(
                IntegerVS.lessThan(
                    typeToAllSymmetricMachines.get(type).size(), symClassI.size())));
            newClasses = newClasses.add(symClassI.restrict(isNonEmpty));
          }
        }
        indexI = IntegerVS.add(indexI, 1);
      }

      // add self as a single-element class
      SetVS<PrimitiveVS<Machine>> selfSet = new SetVS<>(guard);
      selfSet = selfSet.add(primitiveVS);
      newClasses = newClasses.add(selfSet);

      // update symmetry classes map
      assert (!BooleanVS.isEverTrue(
          IntegerVS.lessThan(typeToAllSymmetricMachines.get(type).size(), newClasses.size())));
      typeToSymmetryClasses.put(type, newClasses);
      //                    checkSymmetryClassesForType(type);

      SymmetryPendingMerges pendingMerges = typeToPendingMerges.get(type);
      if (pendingMerges == null) {
        pendingMerges = new SymmetryPendingMerges();
      }
      pendingMerges.add(primitiveVS);
      typeToPendingMerges.put(type, pendingMerges);
    }
  }

  public void mergeAllSymmetryClasses() {
    for (Map.Entry<String, SymmetryPendingMerges> entry : typeToPendingMerges.entrySet()) {
      mergeSymmetryClassesForType(entry.getKey(), entry.getValue());
    }
    typeToPendingMerges.clear();
  }

  private void mergeSymmetryClassesForType(String type, SymmetryPendingMerges pendingMerges) {
    ListVS<SetVS<PrimitiveVS<Machine>>> symClasses = typeToSymmetryClasses.get(type);

    PrimitiveVS<Integer> size = symClasses.size().restrict(pendingMerges.getUniverse());
    PrimitiveVS<Integer> sizeMinusOne = IntegerVS.subtract(size, 1);

    PrimitiveVS<Integer> indexI = new PrimitiveVS<>(0, sizeMinusOne.getUniverse());
    while (BooleanVS.isEverTrue(IntegerVS.lessThan(indexI, sizeMinusOne))) {
      Guard condI = BooleanVS.getTrueGuard(IntegerVS.lessThan(indexI, sizeMinusOne));

      PrimitiveVS<Integer> indexJ = IntegerVS.add(indexI.restrict(condI), 1);
      while (BooleanVS.isEverTrue(IntegerVS.lessThan(indexJ, size))) {
        Guard condJ = BooleanVS.getTrueGuard(IntegerVS.lessThan(indexJ, size));

        if (!condJ.isFalse()) {
          PrimitiveVS<Integer> condIndexI = indexI.restrict(condJ);
          PrimitiveVS<Integer> condIndexJ = indexJ.restrict(condJ);

          SetVS<PrimitiveVS<Machine>> lhs = symClasses.get(condIndexI);
          SetVS<PrimitiveVS<Machine>> rhs = symClasses.get(condIndexJ);
          Guard g = lhs.getUniverse().and(rhs.getUniverse());

          // restrict (i, j) to class pairs containing a pending merge
          Guard condContains =
              BooleanVS.getTrueGuard(lhs.contains(pendingMerges.getPendingMachines()));
          condContains =
              condContains.or(
                  BooleanVS.getTrueGuard(rhs.contains(pendingMerges.getPendingMachines())));
          g = g.and(condContains);

          SetVS<PrimitiveVS<Machine>>[] mergedClasses =
              mergeSymmetryClassPair(lhs.restrict(g), rhs.restrict(g));
          assert (mergedClasses.length == 2);
          symClasses = symClasses.set(condIndexI.restrict(g), mergedClasses[0]);
          symClasses = symClasses.set(condIndexJ.restrict(g), mergedClasses[1]);
        }

        indexJ = IntegerVS.add(indexJ, 1);
      }
      indexI = IntegerVS.add(indexI, 1);
    }

    ListVS<SetVS<PrimitiveVS<Machine>>> newClasses = new ListVS<>(Guard.constTrue());
    PrimitiveVS<Integer> zero = new PrimitiveVS<>(0);
    for (SetVS<PrimitiveVS<Machine>> symClass : symClasses.getItems()) {
      // if class not empty, add to new classes
      Guard isNonEmpty = BooleanVS.getFalseGuard(IntegerVS.equalTo(symClass.size(), zero));
      if (!isNonEmpty.isFalse()) {
        assert (!BooleanVS.isEverTrue(
            IntegerVS.lessThan(typeToAllSymmetricMachines.get(type).size(), symClass.size())));
        newClasses = newClasses.add(symClass.restrict(isNonEmpty));
      }
    }

    // update symmetry classes map
    assert (!BooleanVS.isEverTrue(
        IntegerVS.lessThan(typeToAllSymmetricMachines.get(type).size(), newClasses.size())));
    typeToSymmetryClasses.put(type, newClasses);
    //        checkSymmetryClassesForType(type);
  }

  private SetVS<PrimitiveVS<Machine>>[] mergeSymmetryClassPair(
      SetVS<PrimitiveVS<Machine>> lhs, SetVS<PrimitiveVS<Machine>> rhs) {
    if (lhs.isEmpty() || rhs.isEmpty()) {
      return new SetVS[] {lhs, rhs};
    } else {
      // get representative of lhs class
      PrimitiveVS<Machine> lhsRep = lhs.get(new PrimitiveVS<>(0, lhs.getUniverse()));

      Guard symEqGuard = Guard.constFalse();

      List<GuardedValue<Machine>> lhsRepGVs = lhsRep.getGuardedValues();
      for (GuardedValue<Machine> lhsRepGV : lhsRepGVs) {
        Machine lhsRepMachine = lhsRepGV.getValue();
        Guard lhsRepGuard = lhsRepGV.getGuard();

        // get representative of rhs class
        PrimitiveVS<Machine> rhsRep =
            rhs.get(new PrimitiveVS<>(0, rhs.getUniverse().and(lhsRepGuard)));

        List<GuardedValue<Machine>> rhsRepGVs = rhsRep.getGuardedValues();
        for (GuardedValue<Machine> rhsRepGV : rhsRepGVs) {
          Machine rhsRepMachine = rhsRepGV.getValue();
          Guard guard = rhsRepGV.getGuard();

          if (lhsRepMachine == rhsRepMachine) {
            symEqGuard = symEqGuard.or(guard);
          } else {
            symEqGuard = symEqGuard.or(getSymEquivGuard(lhsRepMachine, rhsRepMachine, guard));
          }
        }
      }

      if (!symEqGuard.isFalse()) {
        ListVS<PrimitiveVS<Machine>> rhsToMerge = rhs.getElements().restrict(symEqGuard);

        PrimitiveVS<Integer> size = rhsToMerge.restrict(symEqGuard).size();
        PrimitiveVS<Integer> index = new PrimitiveVS<>(0).restrict(size.getUniverse());
        List<PrimitiveVS<Machine>> list = new ArrayList<>();
        while (BooleanVS.isEverTrue(IntegerVS.lessThan(index, size))) {
          Guard cond = BooleanVS.getTrueGuard(IntegerVS.lessThan(index, size));
          if (cond.isTrue()) {
            list.add(rhsToMerge.get(index));
          } else {
            list.add(rhsToMerge.restrict(cond).get(index));
          }
          index = IntegerVS.add(index, 1);
        }

        for (PrimitiveVS<Machine> vs : list) {
          lhs = lhs.add(vs);
        }

        rhs = rhs.restrict(symEqGuard.not());

        //                checkSymmetryClass(lhs);
        //                checkSymmetryClass(rhs);
      }

      return new SetVS[] {lhs, rhs};
    }
  }

  private Guard haveSymEqLocalState(Machine m1, Machine m2, Guard pc) {
    assert (m1 != m2);

    List<ValueSummary> m1State = m1.getMachineLocalState().getLocals();
    List<ValueSummary> m2State = m2.getMachineLocalState().getLocals();
    assert (m1State.size() == m2State.size());

    Guard result = Guard.constTrue();

    for (int i = 0; i < m1State.size(); i++) {
      ValueSummary original = m1State.get(i).restrict(pc);
      ValueSummary permuted = m2State.get(i).restrict(pc).swap(Collections.singletonMap(m1, m2));
      if (original.isEmptyVS() && permuted.isEmptyVS()) {
        continue;
      }
      result = result.and(original.symbolicEquals(permuted, Guard.constTrue()).getGuardFor(true));
      if (result.isFalse()) {
        return result;
      }
    }
    return result;
  }

  private Guard getSymEquivGuard(Machine m1, Machine m2, Guard pc) {
    assert (m1 != m2);

    Guard result = haveSymEqLocalState(m1, m2, pc);
    if (result.isFalse()) {
      return result;
    }

    for (Machine other : PSymGlobal.getScheduler().getMachines()) {
      if (other == m1 || other == m2 || other instanceof Monitor) {
        continue;
      }
      for (ValueSummary original : other.getMachineLocalState().getLocals()) {
        original = original.restrict(pc);
        ValueSummary permuted = original.swap(Collections.singletonMap(m1, m2));
        if (original.isEmptyVS() && permuted.isEmptyVS()) {
          continue;
        }

        result = result.and(original.symbolicEquals(permuted, Guard.constTrue()).getGuardFor(true));
        if (result.isFalse()) {
          return result;
        }
      }
      if (result.isFalse()) {
        return result;
      }
    }

    return result;
  }

  public void resetAllSymmetryClasses() {
    for (String type : typeToSymmetryClasses.keySet()) {
      ListVS<SetVS<PrimitiveVS<Machine>>> lvs = typeToSymmetryClasses.get(type);
      for (SetVS<PrimitiveVS<Machine>> svs : lvs.getItems()) {
        for (PrimitiveVS<Machine> mvs : svs.getElements().getItems()) {
          for (GuardedValue<Machine> gv: mvs.getGuardedValues()) {
            updateSymmetrySet(gv.getValue(), gv.getGuard());
          }
        }
      }
      assert (!BooleanVS.isEverFalse(
          IntegerVS.equalTo(
              typeToSymmetryClasses.get(type).size(),
              new PrimitiveVS<>(typeToAllSymmetricMachines.get(type).size()))));
    }
  }

  public void checkAllSymmetryClasses() {
    for (String type : typeToSymmetryClasses.keySet()) {
      checkSymmetryClassesForType(type);
    }
  }

  private void checkSymmetryClassesForType(String type) {
    ListVS<SetVS<PrimitiveVS<Machine>>> symClasses = typeToSymmetryClasses.get(type);

    for (SetVS<PrimitiveVS<Machine>> symSet : symClasses.getItems()) {
      checkSymmetryClass(symSet);
    }
  }

  private void checkSymmetryClass(SetVS<PrimitiveVS<Machine>> symSet) {
    // get representative of lhs class
    PrimitiveVS<Integer> zero = new PrimitiveVS<>(0);
    // if class not empty, add to new classes
    Guard isNonEmpty = BooleanVS.getFalseGuard(IntegerVS.equalTo(symSet.size(), zero));
    if (isNonEmpty.isFalse()) {
      return;
    }

    PrimitiveVS<Machine> lhsRep = symSet.get(new PrimitiveVS<>(0, isNonEmpty));
    for (GuardedValue<Machine> lhsRepGV : lhsRep.getGuardedValues()) {
      Machine lhsRepMachine = lhsRepGV.getValue();
      Guard lhsRepGuard = lhsRepGV.getGuard();

      for (PrimitiveVS<Machine> rhsRep : symSet.getElements().getItems()) {
        rhsRep = rhsRep.restrict(lhsRepGuard);
        for (GuardedValue<Machine> rhsRepGV : rhsRep.getGuardedValues()) {
          Machine rhsRepMachine = rhsRepGV.getValue();
          Guard rhsRepGuard = rhsRepGV.getGuard();

          if (lhsRepMachine != rhsRepMachine) {
            Guard areSymEq = haveSymEqLocalState(lhsRepMachine, rhsRepMachine, rhsRepGuard);
            if (!areSymEq.equals(rhsRepGuard)) {
              haveSymEqLocalState(lhsRepMachine, rhsRepMachine, rhsRepGuard);
            }
            assert (areSymEq.equals(rhsRepGuard));
          }
        }
      }
    }
  }
}
