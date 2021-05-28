package psymbolic.runtime;

import p.runtime.values.*;
import p.runtime.values.exceptions.InvalidIndexException;
import p.runtime.values.exceptions.KeyNotFoundException;
import psymbolic.valuesummary.*;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.function.Consumer;
import java.util.function.Function;

public class ForeignFunctionInvoker {

    public static int times = 1;

    public static GuardedValue concretize (Object valueSummary) {
        if (valueSummary instanceof PrimitiveVS<?>) {
            List<? extends GuardedValue<?>> list = ((PrimitiveVS<?>) valueSummary).getGuardedValues();
            if (list.size() > 0) {
                GuardedValue<?> item = list.get(0);
                if (item.getValue() instanceof Integer) {
                    return new GuardedValue(new PInt((Integer) item.getValue()), item.getGuard());
                } else if (item.getValue() instanceof Boolean) {
                    return new GuardedValue(new PBool((Boolean) item.getValue()), item.getGuard());
                } else if (item.getValue() instanceof Float) {
                    return new GuardedValue(new PFloat((Float) item.getValue()), item.getGuard());
                }
                return new GuardedValue(item.getValue(), item.getGuard());
            }
        } else if (valueSummary instanceof ListVS<?>) {
            ListVS<?> listVS = (ListVS<?>) valueSummary;
            Guard pc = listVS.getUniverse();
            PSeq list = new PSeq(new ArrayList<>());
            List<GuardedValue<Integer>> guardedValues = listVS.size().getGuardedValues();
            if (guardedValues.size() > 0) {
                GuardedValue<Integer> guardedValue = guardedValues.iterator().next();
                pc = guardedValue.getGuard();
                for (int i = 0; i < guardedValue.getValue(); i++) {
                    listVS = listVS.restrict(pc);
                    GuardedValue<? extends PValue<?>> elt = concretize(listVS.get(new PrimitiveVS<>(i).restrict(pc)));
                    pc = pc.and(elt.getGuard());
                    list.insertValue(i, elt.getValue());
                }
            }
            return new GuardedValue<>(list, pc);
        } else if (valueSummary instanceof MapVS<?, ?>) {
            MapVS<?, ?> mapVS = (MapVS<?, ?>) valueSummary;
            Guard pc = mapVS.getUniverse();
            PMap map = new PMap(new HashMap<>());
            ListVS<?> keyList = mapVS.keys.getElements();
            List<GuardedValue<Integer>> guardedValues = keyList.size().getGuardedValues();
            if (guardedValues.size() > 0) {
                GuardedValue<Integer> guardedValue = guardedValues.iterator().next();
                pc = guardedValue.getGuard();
                for (int i = 0; i < guardedValue.getValue(); i++) {
                    keyList = keyList.restrict(pc);
                    GuardedValue<? extends PValue<?>> key = concretize(keyList.get(new PrimitiveVS<>(i).restrict(pc)));
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
            PSet set = new PSet(new ArrayList<>());
            ListVS<?> eltList = setVS.getElements();
            List<GuardedValue<Integer>> guardedValues = eltList.size().getGuardedValues();
            if (guardedValues.size() > 0) {
                GuardedValue<Integer> guardedValue = guardedValues.iterator().next();
                pc = guardedValue.getGuard();
                for (int i = 0; i < guardedValue.getValue(); i++) {
                    eltList = eltList.restrict(pc);
                    GuardedValue<? extends PValue<?>> elt = concretize(eltList.get(new PrimitiveVS<>(i).restrict(pc)));
                    pc = pc.and(elt.getGuard());
                    set.insertValue(elt.getValue());
                }
            }
            return new GuardedValue<>(set, pc);
        } else if (valueSummary instanceof TupleVS) {
            TupleVS tupleVS = (TupleVS) valueSummary;
            Guard pc = tupleVS.getUniverse();
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
            String[] names = namedTupleVS.getNames();
            Map<String, PValue<?>> map = new HashMap<>();
            for (int i = 0; i < names.length; i++) {
                String name = names[i];
                GuardedValue<? extends PValue<?>> entry = concretize(namedTupleVS.getField(name));
                map.put(name, entry.getValue());
                pc = pc.and(entry.getGuard());
                namedTupleVS = namedTupleVS.restrict(pc);
            }
            return new GuardedValue<>(new PNamedTuple(map), pc);
        }
        return null;
    }

    public static void invoke(Guard pc, Consumer<List<Object>> fn, ValueSummary ... args) {
        Guard iterPc = Guard.constFalse();
        boolean skip = false;
        boolean done = false;
        while (!done) {
            iterPc = pc.and(iterPc.not());
            List<Object> concreteArgs = new ArrayList<>();
            for (int j = 0; j < args.length && !done; j++) {
                GuardedValue guardedValue = concretize(args[j].restrict(iterPc));
                if (guardedValue == null) {
                    if (j == 0) done = true;
                    skip = true;
                    break;
                } else {
                    iterPc = iterPc.and(guardedValue.getGuard());
                    concreteArgs.add(guardedValue.getValue());
                }
            }
            if (done) {
                break;
            }
            if (skip) {
                continue;
            }
            fn.accept(concreteArgs);
            return;
        }
    }

    public static ValueSummary invoke(Guard pc, ValueSummary<?> def, Function<List<Object>, Object> fn, ValueSummary ... args) {
        Guard iterPc = Guard.constFalse();
        boolean skip = false;
        UnionVS ret = new UnionVS();
        boolean done = false;
        for (int i = 0; i < times; i++) {
            iterPc = pc.and(iterPc.not());
            List<Object> concreteArgs = new ArrayList<>();
            for (int j = 0; j < args.length && !done; j++) {
                GuardedValue<Object> guardedValue = concretize(args[j].restrict(iterPc));
                if (guardedValue == null) {
                    if (j == 0) done = true;
                    skip = true;
                    break;
                } else {
                    iterPc = iterPc.and(guardedValue.getGuard());
                    concreteArgs.add(guardedValue.getValue());
                }
            }
            if (done) {
                break;
            }
            if (skip) {
                i--;
                continue;
            }
            ret = ret.merge(new UnionVS(convertConcrete(iterPc, fn.apply(concreteArgs))));
        }
        if (def instanceof UnionVS) {
            return ret;
        } else {
            return ValueSummary.castFromAny(ret.getUniverse(), def, ret);
        }
    }

    public static ValueSummary<?> convertConcrete(Guard pc, Object o) {
        System.out.println("convertConcrete");
        if (o instanceof PSeq) {
            PSeq list = (PSeq) o;
            ListVS listVS = new ListVS(pc);
            int size = list.size();
            for (int i = 0; i < size; i++) {
                try {
                    listVS.add(convertConcrete(pc, list.getValue(i)));
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
            String[] fields = namedTuple.getFields();
            Object[] namesAndFields = new Object[fields.length * 2];
            for (int i = 0; i < namesAndFields.length; i += 2) {
                namesAndFields[i] = fields[i];
                namesAndFields[i + 1] = namedTuple.getField(fields[i]);
            }
            return new NamedTupleVS(namesAndFields);
        } else if (o instanceof PBool){
           return new PrimitiveVS<>(((PBool) o).getValue()).restrict(pc);
        } else if (o instanceof PInt){
            System.out.println("int");
            return new PrimitiveVS<>(((PInt) o).getValue()).restrict(pc);
        } else if (o instanceof PFloat){
            return new PrimitiveVS<>(((PFloat) o).getValue()).restrict(pc);
        } else if (o instanceof PString){
            return new PrimitiveVS<>(((PString) o).getValue()).restrict(pc);
        } else if (o instanceof PEnum){
            return new PrimitiveVS<>(((PEnum) o).getValue()).restrict(pc);
        } else {
            System.out.println("else");
            return new PrimitiveVS(o);
        }
    }

}
