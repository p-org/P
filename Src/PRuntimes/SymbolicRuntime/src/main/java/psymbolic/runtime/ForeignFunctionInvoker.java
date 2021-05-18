package psymbolic.runtime;

import psymbolic.valuesummary.*;
import psymbolic.valuesummary.Guard;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.Set;
import java.util.function.Consumer;
import java.util.function.Function;

public class ForeignFunctionInvoker {

    public static int times = 1;

    public static GuardedValue concretize (Object valueSummary) {
        if (valueSummary instanceof PrimitiveVS<?>) {
            List<? extends GuardedValue<?>> list = ((PrimitiveVS<?>) valueSummary).getGuardedValues();
            if (list.size() > 0) {
                GuardedValue<?> item = list.get(0);
                return new GuardedValue(item.getValue(), item.getGuard());
            }
        } else if (valueSummary instanceof ListVS<?>) {
            ListVS<?> listVS = (ListVS<?>) valueSummary;
            Guard pc = listVS.getUniverse();
            List list = new ArrayList();
            List<GuardedValue<Integer>> guardedValues = listVS.size().getGuardedValues();
            if (guardedValues.size() > 0) {
                GuardedValue<Integer> guardedValue = guardedValues.iterator().next();
                pc = guardedValue.getGuard();
                listVS = listVS.restrict(pc);
                for (int i = 0; i < guardedValue.getValue(); i++) {
                    GuardedValue elt = concretize(listVS.get(new PrimitiveVS<>(i).restrict(pc)));
                    assert elt != null;
                    pc = pc.and(elt.getGuard());
                    listVS.restrict(pc);
                    list.add(elt.getValue());
                }
            }
            return new GuardedValue(list, pc);
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

    public static ValueSummary invoke(Guard pc, Class<? extends ValueSummary<?>> c, Function<List<Object>, Object> fn, ValueSummary ... args) {
        Guard iterPc = Guard.constFalse();
        boolean skip = false;
        UnionVS ret = new UnionVS();
        boolean done = false;
        for (int i = 0; i < times; i++) {
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
                i--;
                continue;
            }
            ret = ret.merge(new UnionVS(convertConcrete(iterPc, fn.apply(concreteArgs))));
        }
        if (c.equals(UnionVS.class)) {
            return ret;
        } else {
            return ValueSummary.castFromAny(ret.getUniverse(), c, ret);
        }
    }

    public static ValueSummary convertConcrete(Guard pc, Object o) {
        if (o instanceof List) {
            List list = (List) o;
            ListVS listVS = new ListVS(pc);
            for (Object itm : list) {
                listVS.add(convertConcrete(pc, itm));
            }
            return listVS;
        } else if (o instanceof Map) {
            Map map = (Map) o;
            MapVS mapVS = new MapVS(pc);
            for (Map.Entry entry : (Set<Map.Entry>) map.entrySet()) {
                mapVS.add(new PrimitiveVS(entry.getKey()).restrict(pc), convertConcrete(pc, entry.getValue()));
            }
            return mapVS;
        } else {
           return new PrimitiveVS(o).restrict(pc);
        }
    }

}
