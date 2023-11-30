package psym.runtime.scheduler.search.symmetry;

import java.io.Serializable;
import java.util.*;
import psym.runtime.machine.Machine;
import psym.valuesummary.*;

public abstract class SymmetryTracker implements Serializable {
  public static final Map<String, Set<Machine>> typeToAllSymmetricMachines = new HashMap<>();

  public static void addSymmetryType(String type) {
    typeToAllSymmetricMachines.put(type, new TreeSet<>());
  }

  public abstract SymmetryTracker getCopy();

  public abstract void reset();

  public abstract void createMachine(Machine machine, Guard guard);

  public abstract List<ValueSummary> getReducedChoices(List<ValueSummary> original, boolean isData);

  public abstract void updateSymmetrySet(Machine machine, Guard guard);

  public abstract void mergeAllSymmetryClasses();

}
