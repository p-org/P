package psym.runtime.scheduler.search.explicit;

import java.util.*;
import lombok.Getter;
import psym.runtime.PSymGlobal;
import psym.runtime.machine.Machine;
import psym.runtime.machine.Monitor;
import psym.runtime.scheduler.search.symmetry.SymmetryTracker;
import psym.valuesummary.*;

public class ExplicitSymmetryTracker extends SymmetryTracker {
  @Getter
  private static int pruneCount = 0;
  Map<String, List<TreeSet<Machine>>> typeToSymmetryClasses;
  Set<Machine> pendingMerges;

  Map<Machine, Map<String, Object>> machineToSymData;
  Map<Machine, Map<String, Machine>> machineToChildren;
  Map<Machine, Machine> machineToParent;

  public ExplicitSymmetryTracker() {
    typeToSymmetryClasses = new HashMap<>();
    for (String type : typeToAllSymmetricMachines.keySet()) {
      typeToSymmetryClasses.put(type, null);
    }
    pendingMerges = new HashSet<>();
    machineToSymData = new HashMap<>();
    machineToChildren = new HashMap<>();
    machineToParent = new HashMap<>();
  }

  private ExplicitSymmetryTracker(
      Map<String, List<TreeSet<Machine>>> symClasses,
      Set<Machine> pending,
      Map<Machine, Map<String, Object>> symData,
      Map<Machine, Map<String, Machine>> symChildren,
      Map<Machine, Machine> symParent) {
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
    pendingMerges = new HashSet<>(pending);
    machineToSymData = new HashMap<>(symData);
    machineToChildren = new HashMap<>(symChildren);
    machineToParent = new HashMap<>(symParent);
  }

  public SymmetryTracker getCopy() {
    return new ExplicitSymmetryTracker(
            typeToSymmetryClasses,
            pendingMerges,
            machineToSymData,
            machineToChildren,
            machineToParent);
  }

  public void reset() {
    for (Set<Machine> symMachines : typeToAllSymmetricMachines.values()) {
      symMachines.clear();
    }
    typeToSymmetryClasses.replaceAll((t, v) -> null);
    pendingMerges = new HashSet<>();
    machineToSymData = new HashMap<>();
    machineToChildren = new HashMap<>();
    machineToParent = new HashMap<>();
  }

  public void addMachineSymData(Machine machine, String name, Object data) {
    String dataName = name + data.getClass().toString();
    Map<String, Object> machineSymmetricData = machineToSymData.get(machine);
    if (machineSymmetricData == null) {
      machineSymmetricData = new HashMap<>();
    }
    machineSymmetricData.put(dataName, data);
    machineToSymData.put(machine, machineSymmetricData);
    if (data instanceof Machine) {
      Machine depMachine  = (Machine) data;
      Map<String, Machine> machineDeps = machineToChildren.get(machine);
      if (machineDeps == null) {
        machineDeps = new HashMap<>();
      }
      machineDeps.put(depMachine.getName(), depMachine);
      machineToChildren.put(machine, machineDeps);
      machineToParent.put(depMachine, machine);
    }
  }

  public Object getMachineSymData(Machine machine, String name, Object data) {
    String dataName = name + data.getClass().toString();
    Map<String, Object> machineSymmetricData = machineToSymData.get(machine);
    if (machineSymmetricData != null) {
      Object result = machineSymmetricData.get(dataName);
      if (result != null) {
        return result;
      }
    }
    return data;
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
      if (!machineToParent.containsKey(machine)) {
        machineToParent.put(machine, machine);
      }
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
          Machine machineOrig = ((Machine) value);
          Machine machineParent = machineToParent.get(machineOrig);

          if (machineParent != null) {
            // check if symmetric machine
            List<TreeSet<Machine>> symClasses = typeToSymmetryClasses.get(machineParent.getName());
            assert (symClasses != null);

            // check each symmetry class
            for (TreeSet<Machine> symSet : symClasses) {
              boolean hasMachine = symSet.contains(machineParent);

              if (hasMachine) {
                // machine is present in the symmetry class

                // get representative as first element of the class
                Machine machineParentRep = symSet.first();

                Machine machineRep = machineParentRep;
                if (machineOrig != machineParent) {
                  Map<String, Machine> machineParentRepChildren = machineToChildren.get(machineParentRep);
                  assert (machineParentRepChildren != null);
                  machineRep = machineParentRepChildren.get(machineOrig.getName());
                }
                assert (machineRep != null);

                assert isData || (!machineRep.getEventBuffer().isEmpty());
                pendingSummaries.add(machineRep);
              }
              added = true;
            }
          }
        }
      }
      if (!added) {
        reduced.add(choice);
      }
    }

    for (Machine m_orig : pendingSummaries) {
      assert (!m_orig.getEventBuffer().isEmpty());
      reduced.add(new PrimitiveVS(Collections.singletonMap(m_orig, Guard.constTrue())));
    }

    pruneCount += original.size() - reduced.size();
    return reduced;
  }

  public void updateSymmetrySet(Machine machine_orig, Guard guard) {
    Machine machine = machineToParent.get(machine_orig);
    if (machine != null) {
      String type = machine.getName();

      List<TreeSet<Machine>> symClasses = typeToSymmetryClasses.get(type);
      assert (symClasses != null);
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
      pendingMerges.add(machine);
    }
  }

  public void mergeAllSymmetryClasses() {
    for (Machine m: pendingMerges) {
      mergeSymmetryClassesForType(m);
    }
    pendingMerges.clear();
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

  private boolean haveSymEqLocalState(Machine m1, Machine m2, Map<Machine, Machine> mapping) {
    assert (m1 != m2);

    List<ValueSummary> m1State = m1.getMachineLocalState().getLocals();
    List<ValueSummary> m2State = m2.getMachineLocalState().getLocals();
    assert (m1State.size() == m2State.size());

    for (int i = 0; i < m1State.size(); i++) {
      ValueSummary original = m1State.get(i);
      ValueSummary permuted = m2State.get(i).swap(mapping);
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

    Map<Machine, Machine> mapping = new HashMap<>();
    getMappingWithDependencies(m1, m2, mapping);

    Set<Machine> processed = new HashSet<>();
    for (Map.Entry<Machine, Machine> entry: mapping.entrySet()) {
      if (processed.contains(entry.getKey())) {
        continue;
      }
      if (!haveSymEqLocalState(entry.getKey(), entry.getValue(), mapping)) {
        return false;
      }
      processed.add(entry.getKey());
      processed.add(entry.getValue());
    }

    for (Machine m : PSymGlobal.getScheduler().getMachines()) {
      if (m instanceof Monitor || mapping.containsKey(m)) {
        continue;
      }
      Machine other  = m;
      for (ValueSummary original : other.getMachineLocalState().getLocals()) {
        ValueSummary permuted = original.swap(mapping);
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

  private void getMappingWithDependencies(Machine m1, Machine m2, Map<Machine, Machine> mapping) {
    assert (m1 != m2);
    mapping.put(m1, m2);
    mapping.put(m2, m1);

    Map<String, Machine> m1Deps = machineToChildren.get(m1);
    if (m1Deps != null) {
      Map<String, Machine> m2Deps = machineToChildren.get(m2);
      if (m2Deps != null) {
        for (Map.Entry<String, Machine> entry : m1Deps.entrySet()) {
          String machineType = entry.getKey();
          Machine m1D = entry.getValue();
          assert (m1D != null);

          Machine m2D = m2Deps.get(machineType);
          assert (m2D != null);

          assert (m1D != m2D);
          getMappingWithDependencies(m1D, m2D, mapping);
        }
      }
    }
  }

}
