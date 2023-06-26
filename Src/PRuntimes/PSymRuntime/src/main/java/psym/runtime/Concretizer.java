package psym.runtime;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.function.Function;
import java.util.function.Predicate;
import psym.runtime.machine.Machine;
import psym.runtime.machine.events.Event;
import psym.runtime.machine.events.Message;
import psym.runtime.values.*;
import psym.valuesummary.*;

public class Concretizer {
  public static boolean print = false;

  /**
   * Get a concrete value for a value summary
   *
   * @param valueSummary value summary to concretize
   * @return a concrete value represented by the value summary
   */
  public static GuardedValue concretize(Object valueSummary) {
    if (valueSummary instanceof PrimitiveVS<?>) {
      List<? extends GuardedValue<?>> list = ((PrimitiveVS<?>) valueSummary).getGuardedValues();
      if (list.size() > 0) {
        GuardedValue<?> item = list.get(0);
        return new GuardedValue(item.getValue(), item.getGuard());
      }
    } else if (valueSummary instanceof ListVS<?>) {
      ListVS<?> listVS = (ListVS<?>) valueSummary;
      Guard pc = listVS.getUniverse();
      if (pc.isFalse()) return null;
      List<Object> list = new ArrayList<>();
      List<GuardedValue<Integer>> guardedValues = listVS.size().getGuardedValues();
      if (guardedValues.size() > 0) {
        GuardedValue<Integer> guardedValue = guardedValues.iterator().next();
        pc = guardedValue.getGuard();
        for (int i = 0; i < guardedValue.getValue(); i++) {
          listVS = listVS.restrict(pc);
          GuardedValue<?> elt = concretize(listVS.get(new PrimitiveVS<>(i).restrict(pc)));
          pc = pc.and(elt.getGuard());
          list.add(i, elt.getValue());
        }
      }
      return new GuardedValue<>(list, pc);
    } else if (valueSummary instanceof MapVS<?, ?, ?>) {
      MapVS<?, ?, ?> mapVS = (MapVS<?, ?, ?>) valueSummary;
      Guard pc = mapVS.getUniverse();
      if (pc.isFalse()) return null;
      Map map = new HashMap<>();
      ListVS<?> keyList = mapVS.keys.getElements();
      List<GuardedValue<Integer>> guardedValues = keyList.size().getGuardedValues();
      if (guardedValues.size() > 0) {
        GuardedValue<Integer> guardedValue = guardedValues.iterator().next();
        pc = guardedValue.getGuard();
        for (int i = 0; i < guardedValue.getValue(); i++) {
          keyList = keyList.restrict(pc);
          GuardedValue<?> key = concretize(keyList.get(new PrimitiveVS<>(i).restrict(pc)));
          pc = pc.and(key.getGuard());
          mapVS = mapVS.restrict(pc);
          GuardedValue<?> value = concretize(mapVS.entries.get(key.getValue()));
          if (value != null) {
            pc = pc.and(value.getGuard());
            map.put(key.getValue(), value.getValue());
          }
        }
      }
      return new GuardedValue<>(map, pc);
    } else if (valueSummary instanceof SetVS<?>) {
      SetVS<?> setVS = (SetVS<?>) valueSummary;
      Guard pc = setVS.getUniverse();
      if (pc.isFalse()) return null;
      List set = new ArrayList<>();
      ListVS<?> eltList = setVS.getElements();
      List<GuardedValue<Integer>> guardedValues = eltList.size().getGuardedValues();
      if (guardedValues.size() > 0) {
        GuardedValue<Integer> guardedValue = guardedValues.iterator().next();
        pc = guardedValue.getGuard();
        for (int i = 0; i < guardedValue.getValue(); i++) {
          eltList = eltList.restrict(pc);
          GuardedValue<?> elt = concretize(eltList.get(new PrimitiveVS<>(i).restrict(pc)));
          pc = pc.and(elt.getGuard());
          set.add(elt.getValue());
        }
      }
      return new GuardedValue<>(set, pc);
    } else if (valueSummary instanceof TupleVS) {
      TupleVS tupleVS = (TupleVS) valueSummary;
      Guard pc = tupleVS.getUniverse();
      if (pc.isFalse()) return null;
      int length = tupleVS.getArity();
      Object[] fieldValues = new Object[length];
      for (int i = 0; i < length; i++) {
        GuardedValue<?> entry = concretize(tupleVS.getField(i));
        fieldValues[i] = entry.getValue();
        pc = pc.and(entry.getGuard());
        tupleVS = tupleVS.restrict(pc);
      }
      return new GuardedValue<>(fieldValues, pc);
    } else if (valueSummary instanceof NamedTupleVS) {
      NamedTupleVS namedTupleVS = (NamedTupleVS) valueSummary;
      Guard pc = namedTupleVS.getUniverse();
      if (pc.isFalse()) return null;
      String[] names = namedTupleVS.getNames();
      Map<String, Object> map = new HashMap<>();
      for (int i = 0; i < names.length; i++) {
        String name = names[i];
        GuardedValue<?> entry = concretize(namedTupleVS.getField(name));
        map.put(name, entry.getValue());
        pc = pc.and(entry.getGuard());
        namedTupleVS = namedTupleVS.restrict(pc);
      }
      return new GuardedValue<>(map, pc);
    } else if (valueSummary instanceof UnionVS) {
      UnionVS unionVS = (UnionVS) valueSummary;
      if (unionVS.getUniverse().isFalse()) return null;
      UnionVStype type = ((GuardedValue<UnionVStype>) concretize(unionVS.getType())).getValue();
      return concretize(unionVS.getValue(type));
    } else if (valueSummary instanceof Message) {
      Message messageVS = (Message) valueSummary;
      Guard pc = messageVS.getUniverse();
      if (pc.isFalse()) return null;
      List<GuardedValue<Machine>> guardedMachineValues = messageVS.getTarget().getGuardedValues();
      Machine m = null;
      if (!guardedMachineValues.isEmpty()) {
        GuardedValue<Machine> guardedMachineValue = messageVS.getTarget().getGuardedValues().get(0);
        m = guardedMachineValue.getValue();
        messageVS = messageVS.restrict(guardedMachineValue.getGuard());
      }
      GuardedValue<Event> guardedEventValue = messageVS.getEvent().getGuardedValues().get(0);
      Event e = guardedEventValue.getValue();
      messageVS = messageVS.restrict(guardedEventValue.getGuard());
      GuardedValue guardedPayloadValue = concretize(messageVS.getPayload());
      List<Object> messageComponents = new ArrayList<>();
      if (m != null) {
        messageComponents.add(m);
      }
      messageComponents.add(e);
      if (guardedPayloadValue == null) {
        return new GuardedValue(messageComponents, guardedEventValue.getGuard());
      }
      messageComponents.add(guardedPayloadValue.getValue());
      return new GuardedValue(messageComponents, guardedPayloadValue.getGuard());
    }
    return null;
  }

  /**
   * Get a concrete P value for a value summary
   *
   * @param valueSummary value summary to concretize
   * @return a concrete value represented by the value summary
   */
  public static GuardedValue<? extends PValue<?>> concretizePType(Object valueSummary) {
    if (valueSummary instanceof PrimitiveVS<?>) {
      List<? extends GuardedValue<?>> list = ((PrimitiveVS<?>) valueSummary).getGuardedValues();
      if (list.size() > 0) {
        GuardedValue<?> item = list.get(0);
        if (item.getValue() instanceof Integer) {
          return new GuardedValue(new PInt(item.getValue()), item.getGuard());
        } else if (item.getValue() instanceof Boolean) {
          return new GuardedValue(new PBool(item.getValue()), item.getGuard());
        } else if (item.getValue() instanceof Float) {
          return new GuardedValue(new PFloat(item.getValue()), item.getGuard());
        }
        return new GuardedValue(item.getValue(), item.getGuard());
      }
    } else if (valueSummary instanceof ListVS<?>) {
      ListVS<?> listVS = (ListVS<?>) valueSummary;
      Guard pc = listVS.getUniverse();
      if (pc.isFalse()) return null;
      PSeq list = new PSeq(new ArrayList<>());
      List<GuardedValue<Integer>> guardedValues = listVS.size().getGuardedValues();
      if (guardedValues.size() > 0) {
        GuardedValue<Integer> guardedValue = guardedValues.iterator().next();
        pc = guardedValue.getGuard();
        for (int i = 0; i < guardedValue.getValue(); i++) {
          listVS = listVS.restrict(pc);
          GuardedValue<? extends PValue<?>> elt =
              concretize(listVS.get(new PrimitiveVS<>(i).restrict(pc)));
          pc = pc.and(elt.getGuard());
          list.insertValue(i, elt.getValue());
        }
      }
      return new GuardedValue<>(list, pc);
    } else if (valueSummary instanceof MapVS<?, ?, ?>) {
      MapVS<?, ?, ?> mapVS = (MapVS<?, ?, ?>) valueSummary;
      Guard pc = mapVS.getUniverse();
      if (pc.isFalse()) return null;
      PMap map = new PMap(new HashMap<>());
      ListVS<?> keyList = mapVS.keys.getElements();
      List<GuardedValue<Integer>> guardedValues = keyList.size().getGuardedValues();
      if (guardedValues.size() > 0) {
        GuardedValue<Integer> guardedValue = guardedValues.iterator().next();
        pc = guardedValue.getGuard();
        for (int i = 0; i < guardedValue.getValue(); i++) {
          keyList = keyList.restrict(pc);
          GuardedValue<? extends PValue<?>> key =
              concretize(keyList.get(new PrimitiveVS<>(i).restrict(pc)));
          pc = pc.and(key.getGuard());
          mapVS = mapVS.restrict(pc);
          GuardedValue<? extends PValue<?>> value = concretize(mapVS.entries.get(key));
          pc = pc.and(value.getGuard());
          map.putValue(key.getValue(), value.getValue());
        }
      }
      return new GuardedValue<>(map, pc);
    } else if (valueSummary instanceof SetVS<?>) {
      SetVS<?> setVS = (SetVS<?>) valueSummary;
      Guard pc = setVS.getUniverse();
      if (pc.isFalse()) return null;
      PSet set = new PSet(new ArrayList<>());
      ListVS<?> eltList = setVS.getElements();
      List<GuardedValue<Integer>> guardedValues = eltList.size().getGuardedValues();
      if (guardedValues.size() > 0) {
        GuardedValue<Integer> guardedValue = guardedValues.iterator().next();
        pc = guardedValue.getGuard();
        for (int i = 0; i < guardedValue.getValue(); i++) {
          eltList = eltList.restrict(pc);
          GuardedValue<? extends PValue<?>> elt =
              concretize(eltList.get(new PrimitiveVS<>(i).restrict(pc)));
          pc = pc.and(elt.getGuard());
          set.insertValue(elt.getValue());
        }
      }
      return new GuardedValue<>(set, pc);
    } else if (valueSummary instanceof TupleVS) {
      TupleVS tupleVS = (TupleVS) valueSummary;
      Guard pc = tupleVS.getUniverse();
      if (pc.isFalse()) return null;
      int length = tupleVS.getArity();
      PValue<?>[] fieldValues = new PValue[length];
      for (int i = 0; i < length; i++) {
        GuardedValue<? extends PValue<?>> entry = concretize(tupleVS.getField(i));
        fieldValues[i] = entry.getValue();
        pc = pc.and(entry.getGuard());
        tupleVS = tupleVS.restrict(pc);
      }
      return new GuardedValue<>(new PTuple(fieldValues), pc);
    } else if (valueSummary instanceof NamedTupleVS) {
      NamedTupleVS namedTupleVS = (NamedTupleVS) valueSummary;
      Guard pc = namedTupleVS.getUniverse();
      if (pc.isFalse()) return null;
      String[] names = namedTupleVS.getNames();
      List<String> fields = new ArrayList<>();
      Map<String, PValue<?>> values = new HashMap<>();
      for (int i = 0; i < names.length; i++) {
        String name = names[i];
        fields.add(name);
        GuardedValue<? extends PValue<?>> entry = concretize(namedTupleVS.getField(name));
        values.put(name, entry.getValue());
        pc = pc.and(entry.getGuard());
        namedTupleVS = namedTupleVS.restrict(pc);
      }
      return new GuardedValue<>(new PNamedTuple(fields, values), pc);
    } else if (valueSummary instanceof UnionVS) {
      UnionVS unionVS = (UnionVS) valueSummary;
      if (unionVS.getUniverse().isFalse()) return null;
      UnionVStype type = ((GuardedValue<UnionVStype>) concretize(unionVS.getType())).getValue();
      return concretize(unionVS.getValue(type));
    }
    return null;
  }

  /**
   * Get a list of concrete values for the arguments
   *
   * @param pc Guard under which to concretize values
   * @param stop specifies when to stop getting more concrete values
   * @param args arguments
   * @return list of concrete values for arguments
   */
  public static List<GuardedValue<List<Object>>> getConcreteValues(
      Guard pc,
      Predicate<Integer> stop,
      Function<ValueSummary, GuardedValue<?>> concretizer,
      ValueSummary... args) {
    Guard iterPc;
    Guard alreadySeen = Guard.constFalse();
    boolean skip = false;
    boolean done = false;
    int i = 0;
    List<GuardedValue<List<Object>>> concreteArgsList = new ArrayList();
    if (args.length == 0) {
      concreteArgsList.add(new GuardedValue<>(new ArrayList<>(), pc));
      return concreteArgsList;
    }
    while (!stop.test(i)) {
      iterPc = pc.and(alreadySeen.not());
      List<Object> concreteArgs = new ArrayList<>();
      for (int j = 0; j < args.length; j++) {
        GuardedValue<?> guardedValue = concretizer.apply(args[j].restrict(iterPc));
        if (guardedValue == null) {
          if (j == 0) {
            done = true;
            break;
          }
          if (print) {
            concreteArgs.add(null);
          } else {
            skip = true;
            break;
          }
        } else {
          iterPc = iterPc.and(guardedValue.getGuard());
          concreteArgs.add(guardedValue.getValue());
        }
      }
      alreadySeen = alreadySeen.or(iterPc);
      if (done) {
        break;
      }
      if (skip) {
        i--;
        continue;
      }
      if (print) {
        System.out.println("\t#" + concreteArgsList.size() + "\t" + concreteArgs);
      }
      concreteArgsList.add(new GuardedValue<>(concreteArgs, iterPc));
      i++;
    }
    return concreteArgsList;
  }

  /**
   * Get the number of concrete values for the arguments
   *
   * @param pc Guard under which to concretize values
   * @param stop specifies when to stop getting more concrete values
   * @param args arguments
   * @return number of concrete values for arguments
   */
  public static int countConcreteValues(
      Guard pc,
      Predicate<Integer> stop,
      Function<ValueSummary, GuardedValue<?>> concretizer,
      ValueSummary... args) {
    Guard iterPc;
    Guard alreadySeen = Guard.constFalse();
    boolean skip = false;
    boolean done = false;
    int i = 0;
    int result = 0;
    while (!stop.test(i)) {
      iterPc = pc.and(alreadySeen.not());
      for (int j = 0; j < args.length && !done; j++) {
        GuardedValue<?> guardedValue = concretizer.apply(args[j].restrict(iterPc));
        if (guardedValue == null) {
          if (j == 0) done = true;
        } else {
          iterPc = iterPc.and(guardedValue.getGuard());
        }
      }
      alreadySeen = alreadySeen.or(iterPc);
      if (done) {
        break;
      }
      result++;
      i++;
    }
    return result;
  }

  /**
   * Count the number of concrete values for arguments
   *
   * @param pc Guard under which to concretize values
   * @param args arguments
   * @return number of concrete values
   */
  public static int getNumConcreteValues(Guard pc, ValueSummary... args) {
    int i;
    try {
      if (print) {
        i = getConcreteValues(pc, x -> false, Concretizer::concretize, args).size();
      } else {
        i = countConcreteValues(pc, x -> false, Concretizer::concretize, args);
      }
    } catch (NullPointerException e) {
      throw new RuntimeException("Counting concrete values failed.", e);
    }
    return i;
  }
}
