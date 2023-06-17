package psym.runtime.scheduler.symmetry;

import lombok.Setter;
import psym.runtime.machine.Machine;
import psym.runtime.scheduler.Scheduler;
import psym.valuesummary.*;

import java.io.Serializable;
import java.util.*;

public class SymmetryTracker implements Serializable {
    public static final Map<String, Set<Machine>> typeToAllSymmetricMachines = new HashMap<>();

    @Setter
    private static Scheduler scheduler;

    final Map<String, List<SetVS<PrimitiveVS<Machine>>>> typeToSymmetryClasses;

    public SymmetryTracker() {
        typeToSymmetryClasses = new HashMap<>();
    }

    public SymmetryTracker(SymmetryTracker rhs) {
        typeToSymmetryClasses = new HashMap<>(rhs.typeToSymmetryClasses);
    }

    public void reset() {
        for (Set<Machine> symMachines : typeToAllSymmetricMachines.values()) {
            symMachines.clear();
        }
        for (String type : typeToSymmetryClasses.keySet()) {
            typeToSymmetryClasses.put(type, new ArrayList<>());
        }
    }

    public void addSymmetryType(String type) {
        typeToAllSymmetricMachines.put(type, new TreeSet<>());
        typeToSymmetryClasses.put(type, new ArrayList<>());
    }

    public void createMachine(Machine machine, Guard guard) {
        assert (guard.isTrue());

        Set<Machine> symMachines = typeToAllSymmetricMachines.get(machine.getName());
        if (symMachines != null) {
            symMachines.add(machine);
        }
        List<SetVS<PrimitiveVS<Machine>>> symClasses = typeToSymmetryClasses.get(machine.getName());
        if (symClasses != null) {
            // if empty, create an empty class
            if (symClasses.isEmpty()) {
                symClasses.add(new SetVS<>(Guard.constTrue()));
            }
            // get the first class
            SetVS<PrimitiveVS<Machine>> symSet = symClasses.get(0);
            // add machine to the first class
            symSet = symSet.add(new PrimitiveVS<>(machine, guard));
            // add updated first class to all class list
            symClasses.set(0, symSet);
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
                    List<SetVS<PrimitiveVS<Machine>>> symClasses = typeToSymmetryClasses.get(machine.getName());
                    if (symClasses != null) {
                        Guard guard = guardedValues.get(0).getGuard();

                        // remaining guard tracks if any condition remains
                        Guard remaining = guard;

                        // check each symmetry class
                        for (SetVS<PrimitiveVS<Machine>> symSet: symClasses) {
                            Guard hasMachine = symSet.contains(primitiveVS).getGuardFor(true);
                            Guard classGuard = guard.and(hasMachine);

                            if (!classGuard.isFalse()) {
                                // machine is present in the symmetry class

                                // get representative as first element of the class
                                PrimitiveVS<Machine> representativeVS = symSet.get(new PrimitiveVS<>(0, classGuard));

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
                                    pendingSummaries.put(m, currentGuard);
                                }

                                // update remaining
                                remaining = remaining.and(classGuard.not());
                            }
                        }

                        // make sure all conditions are covered and nothing remains
                        assert  (remaining.isFalse());

                        added = true;
                    }
                }
            }
            if (!added) {
                reduced.add(choice);
            }
        }

        for (Map.Entry<Machine, Guard> entry : pendingSummaries.entrySet()) {
            reduced.add(new PrimitiveVS(Collections.singletonMap(entry.getKey(), entry.getValue())));
        }

        return reduced;
    }

    public void updateSymmetrySet(PrimitiveVS chosenVS) {
        List<? extends GuardedValue<?>> choices = ((PrimitiveVS<?>) chosenVS).getGuardedValues();
        for (GuardedValue<?> choice : choices) {
            Object value = choice.getValue();
            if (value instanceof Machine) {
                Machine machine = ((Machine) value);

                List<SetVS<PrimitiveVS<Machine>>> symClasses = typeToSymmetryClasses.get(machine.getName());
                if (symClasses != null) {
                    PrimitiveVS<Machine> primitiveVS = new PrimitiveVS<>(machine, choice.getGuard());
                    List<SetVS<PrimitiveVS<Machine>>> newClasses = new ArrayList<>();

                    // check each symmetry class
                    for (SetVS<PrimitiveVS<Machine>> symSet: symClasses) {
                        // remove chosen from each class
                        symSet = symSet.remove(primitiveVS);

                        // if class not empty, add to new classes
                        if (!symSet.isEmpty()) {
                            newClasses.add(symSet);
                        }
                    }

                    // add self as a single-element class
                    SetVS<PrimitiveVS<Machine>> selfSet = new SetVS<>(Guard.constTrue());
                    selfSet = selfSet.add(primitiveVS);
                    newClasses.add(selfSet);

                    // update symmetry classes map
                    typeToSymmetryClasses.put(machine.getName(), newClasses);
                }
            }
        }
    }

    public void mergeAllSymmetryClasses() {
        for (String type: typeToSymmetryClasses.keySet()) {
            mergeSymmetryClassesForType(type);
        }
    }

    private void mergeSymmetryClassesForType(String type) {
        List<SetVS<PrimitiveVS<Machine>>> symClasses = typeToSymmetryClasses.get(type);
        List<SetVS<PrimitiveVS<Machine>>> newClasses = new ArrayList<>();

        for (int i = 0; i < symClasses.size() - 1; i++) {
            for (int j = i + 1; j < symClasses.size(); j++) {
                SetVS<PrimitiveVS<Machine>>[] mergedClasses = mergeSymmetryClassPair(symClasses.get(i), symClasses.get(j));
                assert (mergedClasses.length == 2);
                symClasses.set(i, mergedClasses[0]);
                symClasses.set(j, mergedClasses[1]);
            }
        }

        for (SetVS<PrimitiveVS<Machine>> symSet: symClasses) {
            // if class not empty, add to new classes
            if (!symSet.isEmpty()) {
                newClasses.add(symSet);
            }
        }

        // update symmetry classes map
        typeToSymmetryClasses.put(type, newClasses);
    }

    private SetVS<PrimitiveVS<Machine>>[] mergeSymmetryClassPair(SetVS<PrimitiveVS<Machine>> lhs, SetVS<PrimitiveVS<Machine>> rhs) {
        if (lhs.isEmpty() || rhs.isEmpty()) {
            return new SetVS[]{lhs, rhs};
        } else {
            // get representative of lhs class
            PrimitiveVS<Machine> lhsRep = lhs.get(new PrimitiveVS<>(0, lhs.getUniverse()));

            Guard symEqGuard = Guard.constFalse();

            List<GuardedValue<Machine>> lhsRepGVs = lhsRep.getGuardedValues();
            for (GuardedValue<Machine> lhsRepGV : lhsRepGVs) {
                Machine lhsRepMachine = lhsRepGV.getValue();
                Guard lhsRepGuard = lhsRepGV.getGuard();

                // get representative of rhs class
                PrimitiveVS<Machine> rhsRep = rhs.get(new PrimitiveVS<>(0, rhs.getUniverse().and(lhsRepGuard)));

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

                for (PrimitiveVS<Machine> vs: list) {
                    lhs = lhs.add(vs);
                }

                rhs = rhs.restrict(symEqGuard.not());
            }

            return new SetVS[]{lhs, rhs};
        }
    }

    private Guard haveSymEqLocalState(Machine m1, Machine m2, Guard pc) {
        assert (m1 != m2);

        List<ValueSummary> m1State = m1.getLocalState();
        List<ValueSummary> m2State = m2.getLocalState();
        assert (m1State.size() == m2State.size());

        Guard result = Guard.constTrue();

        for (int i = 0; i < m1State.size(); i++) {
            ValueSummary original = m1State.get(i).restrict(pc);
            ValueSummary permuted = m2State.get(i).restrict(pc).swap(m1, m2);
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

        for (Machine other: scheduler.getMachines()) {
            if (other == m1 || other == m2) {
                continue;
            }
            for (ValueSummary original: other.getLocalState()) {
                ValueSummary permuted = original.restrict(result).swap(m1, m2);
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
}
