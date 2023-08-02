package psym.runtime;

import java.util.List;
import java.util.function.Consumer;
import java.util.function.Function;
import psym.runtime.values.*;
import psym.runtime.values.exceptions.InvalidIndexException;
import psym.runtime.values.exceptions.KeyNotFoundException;
import psym.valuesummary.*;

public class ForeignFunctionInvoker {

  /* Maximum number of times to invoke the foreign function on different values */
  public static final int times = 100;

  public static List<GuardedValue<List<Object>>> getConcreteValues(Guard pc, ValueSummary... args) {
    return Concretizer.getConcreteValues(pc, x -> x >= times, Concretizer::concretizePType, args);
  }

  /**
   * Invoke a foreign function with a void return type
   *
   * @param pc Guard under which to invoke the function on the provided arguments
   * @param fn function to invoke
   * @param args arguments
   */
  public static void invoke(Guard pc, Consumer<List<Object>> fn, ValueSummary... args) {
    List<GuardedValue<List<Object>>> concreteArgs = getConcreteValues(pc, args);
    for (int i = 0; i < concreteArgs.size(); i++) {
      GuardedValue<List<Object>> guardedArgs = concreteArgs.get(i);
      fn.accept(guardedArgs.getValue());
    }
  }

  /**
   * Invoke a foreign function with a non-void return type
   *
   * @param pc Guard under which to invoke the function on the provided arguments
   * @param def instance of the return type
   * @param fn function to invoke
   * @param args arguments
   * @return the return value of the function
   */
  public static ValueSummary invoke(
      Guard pc, ValueSummary<?> def, Function<List<Object>, Object> fn, ValueSummary... args) {
    List<GuardedValue<List<Object>>> concreteArgs = getConcreteValues(pc, args);
    UnionVS ret = new UnionVS();
    for (int i = 0; i < concreteArgs.size(); i++) {
      GuardedValue<List<Object>> guardedArgs = concreteArgs.get(i);
      ret =
          ret.merge(
              new UnionVS(
                  convertConcrete(guardedArgs.getGuard(), fn.apply(guardedArgs.getValue()))));
    }
    if (def instanceof UnionVS) {
      return ret;
    } else {
      return ValueSummary.castFromAny(ret.getUniverse(), def, ret);
    }
  }

  /**
   * Convert concrete value into a value summary
   *
   * @param pc Guard under for the value summary
   * @param o concrete value
   * @return the value summary for the concrete value
   */
  public static ValueSummary<?> convertConcrete(Guard pc, Object o) {
    if (o instanceof PSeq) {
      PSeq list = (PSeq) o;
      ListVS listVS = new ListVS(pc);
      int size = list.size();
      for (int i = 0; i < size; i++) {
        try {
          listVS = listVS.add(convertConcrete(pc, list.getValue(i)));
        } catch (InvalidIndexException e) {
          e.printStackTrace();
        }
      }
      return listVS;
    } else if (o instanceof PMap) {
      PMap map = (PMap) o;
      MapVS mapVS = new MapVS(pc);
      PSeq keys = map.getKeys();
      int size = keys.size();
      for (int i = 0; i < size; i++) {
        try {
          PValue key = keys.getValue(i);
          mapVS.add(new PrimitiveVS(key).restrict(pc), convertConcrete(pc, map.getValue(key)));
        } catch (InvalidIndexException | KeyNotFoundException e) {
          e.printStackTrace();
        }
      }
      return mapVS;
    } else if (o instanceof PTuple) {
      PTuple tuple = (PTuple) o;
      ValueSummary[] tupleObjects = new ValueSummary[tuple.getArity()];
      for (int i = 0; i < tuple.getArity(); i++) {
        tupleObjects[i] = convertConcrete(pc, tuple.getField(i));
      }
      return new TupleVS(tupleObjects);
    } else if (o instanceof PNamedTuple) {
      PNamedTuple namedTuple = (PNamedTuple) o;
      List<String> fields = namedTuple.getFields();
      Object[] namesAndFields = new Object[fields.size() * 2];
      for (int i = 0, j = 0; i < namesAndFields.length; i += 2, j++) {
        namesAndFields[i] = fields.get(j);
        namesAndFields[i + 1] = convertConcrete(pc, namedTuple.getField(fields.get(j)));
      }
      return new NamedTupleVS(namesAndFields);
    } else if (o instanceof PBool) {
      return new PrimitiveVS<>(((PBool) o).getValue()).restrict(pc);
    } else if (o instanceof PInt) {
      return new PrimitiveVS<>(((PInt) o).getValue()).restrict(pc);
    } else if (o instanceof PFloat) {
      return new PrimitiveVS<>(((PFloat) o).getValue()).restrict(pc);
    } else if (o instanceof PString) {
      return new PrimitiveVS<>(((PString) o).getValue()).restrict(pc);
    } else if (o instanceof PEnum) {
      return new PrimitiveVS<>(((PEnum) o).getValue()).restrict(pc);
    } else {
      return new PrimitiveVS(o).restrict(pc);
    }
  }
}
