package psym.runtime.scheduler.symmetry;

import psym.runtime.machine.Machine;
import psym.valuesummary.*;

import java.io.Serializable;
import java.util.*;

public class SymmetryTracker implements Serializable {
    public static final Map<String, Set<Machine>> typeToAllSymmetricMachines = new HashMap<>();
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

    public void mergeSymmetrySet() {
        // TODO
    }
}
