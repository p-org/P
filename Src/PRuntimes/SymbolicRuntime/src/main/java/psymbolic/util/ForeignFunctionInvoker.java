package psymbolic.util;

import psymbolic.valuesummary.*;
import psymbolic.valuesummary.bdd.Bdd;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.Set;
import java.util.function.Consumer;
import java.util.function.Function;

public class ForeignFunctionInvoker {

    public static int times = 1;

    public static GuardedValue concretize (Object valueSummary) {
        if (valueSummary instanceof PrimVS<?>) {
            List<? extends GuardedValue<?>> list = ((PrimVS<?>) valueSummary).getGuardedValues();
            if (list.size() > 0) {
                GuardedValue<?> item = list.get(0);
                return new GuardedValue(item.value, item.guard);
            }
        } else if (valueSummary instanceof ListVS<?>) {
            ListVS<?> listVS = (ListVS<?>) valueSummary;
            Bdd pc = listVS.getUniverse();
            List list = new ArrayList();
            List<GuardedValue<Integer>> guardedValues = listVS.size().getGuardedValues();
            if (guardedValues.size() > 0) {
                GuardedValue<Integer> guardedValue = guardedValues.iterator().next();
                pc = guardedValue.guard;
                listVS = listVS.guard(pc);
                for (int i = 0; i < guardedValue.value; i++) {
                    GuardedValue elt = concretize(listVS.get(new PrimVS<>(i).guard(pc)));
                    pc = pc.and(elt.guard);
                    listVS.guard(pc);
                    list.add(elt.value);
                }
            }
            return new GuardedValue(list, pc);
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

    public static ValueSummary invoke(Bdd pc, Class<? extends ValueSummary> c, Function<List<Object>, Object> fn, ValueSummary ... args) {
        Bdd iterPc = Bdd.constFalse();
        boolean skip = false;
        UnionVS ret = new UnionVS();
        boolean done = false;
        for (int i = 0; i < times; i++) {
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

    public static ValueSummary convertConcrete(Bdd pc, Object o) {
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
                mapVS.add(new PrimVS(entry.getKey()).guard(pc), convertConcrete(pc, entry.getValue()));
            }
            return mapVS;
        } else {
           return new PrimVS(o).guard(pc);
        }
    }

}
