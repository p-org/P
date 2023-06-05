package psym.runtime.scheduler.symmetry;

import psym.runtime.machine.Machine;
import psym.valuesummary.*;

import java.util.*;

public class SymmetryTracker {
    Map<String, SetVS<PrimitiveVS<Machine>>> typeToSymmetrySet;

    public SymmetryTracker() {
        typeToSymmetrySet = new HashMap<>();
    }

    public SymmetryTracker(SymmetryTracker rhs) {
        typeToSymmetrySet = new HashMap<>(rhs.typeToSymmetrySet);
    }

    public void reset() {
        for (String type: typeToSymmetrySet.keySet()) {
            typeToSymmetrySet.put(type, null);
        }
    }

    public void addSymmetryType(String type) {
        typeToSymmetrySet.put(type, null);
    }

    public void createMachine(Machine machine, Guard guard) {
        if (typeToSymmetrySet.containsKey(machine.getName())) {
            SetVS<PrimitiveVS<Machine>> symSet = typeToSymmetrySet.get(machine.getName());
            if (symSet == null) {
                symSet = new SetVS<>(Guard.constTrue());
            }
            symSet = symSet.add(new PrimitiveVS<>(Collections.singletonMap(machine, guard)));
            typeToSymmetrySet.put(machine.getName(), symSet);
        }
    }

    public List<ValueSummary> getReducedChoices(List<ValueSummary> original) {
        if (original.size() <= 1) {
            return original;
        }

        List<ValueSummary> reduced = new ArrayList<>();
        Map<Machine, Guard> pendingSummaries = new HashMap<>();

        for (ValueSummary choice: original) {
            boolean added = false;
            if (choice instanceof PrimitiveVS) {
                PrimitiveVS primitiveVS = (PrimitiveVS) choice;
                List<GuardedValue<?>> guardedValues = primitiveVS.getGuardedValues();
                assert (guardedValues.size() == 1);

                Object value = guardedValues.get(0).getValue();
                if (value instanceof Machine) {
                    Machine machine = ((Machine) value);
                    SetVS<PrimitiveVS<Machine>> symSet = typeToSymmetrySet.get(machine.getName());
                    if (symSet != null) {
                        Guard hasMachine = symSet.contains(primitiveVS).getGuardFor(true);
                        Guard guard = guardedValues.get(0).getGuard();
                        Guard typeGuard = guard.and(hasMachine);
                        Guard remaining = guard.and(typeGuard.not());

                        if (!typeGuard.isFalse()) {
                            PrimitiveVS<Machine> representativeVS = symSet.get(new PrimitiveVS<>(Collections.singletonMap(0, typeGuard)));
                            List<GuardedValue<Machine>> representativeGVs = representativeVS.getGuardedValues();
                            for (GuardedValue<Machine> representativeGV: representativeGVs) {
                                Machine m = representativeGV.getValue();
                                Guard g = representativeGV.getGuard();
                                if (m == machine) {
                                    g = g.or(remaining);
                                    remaining = Guard.constFalse();
                                }
                                Guard currentGuard = pendingSummaries.get(m);
                                if (currentGuard == null) {
                                    currentGuard = Guard.constFalse();
                                }
                                currentGuard = currentGuard.or(g);
                                pendingSummaries.put(m, currentGuard);
                            }
                        }
                        if (!remaining.isFalse()) {
                            reduced.add(choice.restrict(remaining));
                        }
                        added = true;
                    }
                }
            }
            if (!added) {
                reduced.add(choice);
            }
        }

        for (Map.Entry<Machine, Guard> entry: pendingSummaries.entrySet()) {
            reduced.add(new PrimitiveVS(Collections.singletonMap(entry.getKey(), entry.getValue())));
        }

//        if (pendingSummaries.size() != 0) {
////            System.out.println(String.format("\t(symmetry-aware) %d -> %d", original.size(), reduced.size()));
//            System.out.println(String.format("Original: %s", original));
//            System.out.println(String.format("Reduced: %s", reduced));
//        }

        return reduced;
    }

    public void updateSymmetrySet(PrimitiveVS chosenVS) {
        List<? extends GuardedValue<?>> choices = ((PrimitiveVS<?>) chosenVS).getGuardedValues();
        for (GuardedValue<?> choice: choices) {
            Object value = choice.getValue();
            if (value instanceof Machine) {
                Machine machine = ((Machine) value);
                SetVS<PrimitiveVS<Machine>> symSet = typeToSymmetrySet.get(machine.getName());
                if (symSet != null) {
                    PrimitiveVS<Machine> primitiveVS = new PrimitiveVS<>(Collections.singletonMap(machine, choice.getGuard()));
                    symSet = symSet.remove(primitiveVS);
                    typeToSymmetrySet.put(machine.getName(), symSet);
                }
            }
        }
    }
}
