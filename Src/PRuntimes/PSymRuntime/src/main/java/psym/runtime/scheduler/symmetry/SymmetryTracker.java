package psym.runtime.scheduler.symmetry;

import psym.runtime.machine.Machine;
import psym.valuesummary.*;

import java.util.*;

public class SymmetryTracker {
    Map<String, SetVS<PrimitiveVS<Machine>>> typeToSymmetrySet;

    public SymmetryTracker() {
        typeToSymmetrySet = new HashMap<>();
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
        List<ValueSummary> reduced = new ArrayList<>();
        Map<String, PrimitiveVS<Machine>> typeToSymmetryGuard = new HashMap<>();

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
                        Guard guard = guardedValues.get(0).getGuard().and(hasMachine);
                        Guard typeGuard = Guard.constFalse();
                        PrimitiveVS<Machine> representative = typeToSymmetryGuard.get(machine.getName());
                        if (representative != null) {
                            typeGuard = representative.getUniverse();
                        }
                        representative = symSet.get(new PrimitiveVS<>(Collections.singletonMap(0, typeGuard.or(guard))));
                        typeToSymmetryGuard.put(machine.getName(), representative);
                        added = true;
                    }
                }
            }
            if (!added) {
                reduced.add(choice);
            }
        }

        for (Map.Entry<String, PrimitiveVS<Machine>> entry: typeToSymmetryGuard.entrySet()) {
            reduced.add(entry.getValue());
        }

        if (typeToSymmetryGuard.size() != 0) {
            if (reduced.size() != original.size()){
                System.out.println(String.format("Original: %s", original));
                System.out.println(String.format("Reduced: %s", reduced));
//                assert (false);
            }
        }

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
