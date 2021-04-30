package psymbolic.util;

import p.runtime.values.*;
import p.runtime.values.exceptions.InvalidIndexException;
import p.runtime.values.exceptions.KeyNotFoundException;
import psymbolic.valuesummary.*;
import psymbolic.valuesummary.bdd.Bdd;

import java.util.*;
import java.util.function.Consumer;
import java.util.function.Function;

public class ForeignFunctionInvoker {

    public static int times = 1;

    public static GuardedValue<PValue<?>> concretize (Object valueSummary) {
        if (valueSummary instanceof PrimVS<?>) {
            List<? extends GuardedValue<?>> list = ((PrimVS<?>) valueSummary).getGuardedValues();
            if (list.size() > 0) {
                GuardedValue<?> item = list.get(0);
                return new GuardedValue(item.value, item.guard);
            }
        } else if (valueSummary instanceof ListVS<?>) {
            ListVS<?> listVS = (ListVS<?>) valueSummary;
            Bdd pc = listVS.getUniverse();
            PSeq list = new PSeq(new ArrayList<>());
            List<GuardedValue<Integer>> guardedValues = listVS.size().getGuardedValues();
            if (guardedValues.size() > 0) {
                GuardedValue<Integer> guardedValue = guardedValues.iterator().next();
                pc = guardedValue.guard;
                for (int i = 0; i < guardedValue.value; i++) {
                    listVS = listVS.guard(pc);
                    GuardedValue<? extends PValue<?>> elt = concretize(listVS.get(new PrimVS<>(i).guard(pc)));
                    pc = pc.and(elt.guard);
                    list.insertValue(i, elt.value);
                }
            }
            return new GuardedValue<>(list, pc);
        } else if (valueSummary instanceof MapVS<?, ?>) {
            MapVS<?, ?> mapVS = (MapVS<?, ?>) valueSummary;
            Bdd pc = mapVS.getUniverse();
            PMap map = new PMap(new HashMap<>());
            ListVS<?> keyList = mapVS.keys.getElements();
            List<GuardedValue<Integer>> guardedValues = keyList.size().getGuardedValues();
            if (guardedValues.size() > 0) {
                GuardedValue<Integer> guardedValue = guardedValues.iterator().next();
                pc = guardedValue.guard;
                for (int i = 0; i < guardedValue.value; i++) {
                    keyList = keyList.guard(pc);
                    GuardedValue<? extends PValue<?>> key = concretize(keyList.get(new PrimVS<>(i).guard(pc)));
                    pc = pc.and(key.guard);
                    mapVS = mapVS.guard(pc);
                    GuardedValue<? extends PValue<?>> value = concretize(mapVS.entries.get(key));
                    pc = pc.and(value.guard);
                    map.putValue(key.value, value.value);
                }
            }
            return new GuardedValue<>(map, pc);
        } else if (valueSummary instanceof SetVS<?>) {
            SetVS<?> setVS = (SetVS<?>) valueSummary;
            Bdd pc = setVS.getUniverse();
            PSet set = new PSet(new ArrayList<>());
            ListVS<?> eltList = setVS.getElements();
            List<GuardedValue<Integer>> guardedValues = eltList.size().getGuardedValues();
            if (guardedValues.size() > 0) {
                GuardedValue<Integer> guardedValue = guardedValues.iterator().next();
                pc = guardedValue.guard;
                for (int i = 0; i < guardedValue.value; i++) {
                    eltList = eltList.guard(pc);
                    GuardedValue<? extends PValue<?>> elt = concretize(eltList.get(new PrimVS<>(i).guard(pc)));
                    pc = pc.and(elt.guard);
                    set.insertValue(elt.value);
                }
            }
            return new GuardedValue<>(set, pc);
        } else if (valueSummary instanceof TupleVS) {
            TupleVS tupleVS = (TupleVS) valueSummary;
            Bdd pc = tupleVS.getUniverse();
            int length = tupleVS.getArity();
            PValue<?>[] fieldValues = new PValue[length];
            for (int i = 0; i < length; i++) {
                GuardedValue<? extends PValue<?>> entry = concretize(tupleVS.getField(i));
                fieldValues[i] = entry.value;
                pc = pc.and(entry.guard);
                tupleVS = tupleVS.guard(pc);
            }
            return new GuardedValue<>(new PTuple(fieldValues), pc);
        } else if (valueSummary instanceof NamedTupleVS) {
            NamedTupleVS namedTupleVS = (NamedTupleVS) valueSummary;
            Bdd pc = namedTupleVS.getUniverse();
            String[] names = namedTupleVS.getNames();
            Map<String, PValue<?>> map = new HashMap<>();
            for (int i = 0; i < names.length; i++) {
                String name = names[i];
                GuardedValue<? extends PValue<?>> entry = concretize(namedTupleVS.getField(name));
                map.put(name, entry.value);
                pc = pc.and(entry.guard);
                namedTupleVS = namedTupleVS.guard(pc);
            }
            return new GuardedValue<>(new PNamedTuple(map), pc);
        }
        return null;
    }

    public static void invoke(Bdd pc, Consumer<List<Object>> fn, ValueSummary ... args) {
        Bdd iterPc = Bdd.constFalse();
        boolean skip = false;
        boolean done = false;
        while (!done) {
            iterPc = pc.and(iterPc.not());
            List<Object> concreteArgs = new ArrayList<>();
            for (int j = 0; j < args.length && !done; j++) {
                GuardedValue guardedValue = concretize(args[j].guard(iterPc));
                if (guardedValue == null) {
                    if (j == 0) done = true;
                    skip = true;
                    break;
                } else {
                    iterPc = iterPc.and(guardedValue.guard);
                    concreteArgs.add(guardedValue.value);
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

    public static ValueSummary invoke(Bdd pc, Class<? extends ValueSummary> c, Function<List<PValue<?>>, PValue<?>> fn, ValueSummary ... args) {
        Bdd iterPc = Bdd.constFalse();
        boolean skip = false;
        UnionVS ret = new UnionVS();
        boolean done = false;
        for (int i = 0; i < times; i++) {
            iterPc = pc.and(iterPc.not());
            List<PValue<?>> concreteArgs = new ArrayList<>();
            for (int j = 0; j < args.length && !done; j++) {
                GuardedValue<PValue<?>> guardedValue = concretize(args[j].guard(iterPc));
                if (guardedValue == null) {
                    if (j == 0) done = true;
                    skip = true;
                    break;
                } else {
                    iterPc = iterPc.and(guardedValue.guard);
                    concreteArgs.add(guardedValue.value);
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
        if (c.equals(UnionVS.class)) {
            return ret;
        } else {
            return ValueSummary.fromAny(ret.getUniverse(), c, ret);
        }
    }

    public static ValueSummary<?> convertConcrete(Bdd pc, PValue<?> o) {
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
                    mapVS.add(new PrimVS(key).guard(pc), convertConcrete(pc, map.getValue(key)));
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
        } { // must be PBool, PEnum, PFloat, PInt, PFloat
           return new PrimVS(o).guard(pc);
        }
    }

}
