package psym.runtime.scheduler.symmetry;

import java.io.Serializable;
import java.util.*;
import lombok.Setter;
import psym.runtime.machine.Machine;
import psym.runtime.scheduler.Scheduler;
import psym.valuesummary.*;

public abstract class SymmetryTracker implements Serializable {
  public static final Map<String, Set<Machine>> typeToAllSymmetricMachines = new HashMap<>();

  @Setter protected static Scheduler scheduler;

  public static void addSymmetryType(String type) {
    typeToAllSymmetricMachines.put(type, new TreeSet<>());
  }

  public abstract SymmetryTracker getCopy();

  public abstract void reset();

  public abstract void createMachine(Machine machine, Guard guard);

  public abstract List<ValueSummary> getReducedChoices(List<ValueSummary> original);

  public abstract void updateSymmetrySet(PrimitiveVS chosenVS);

  public abstract void mergeAllSymmetryClasses();

}
