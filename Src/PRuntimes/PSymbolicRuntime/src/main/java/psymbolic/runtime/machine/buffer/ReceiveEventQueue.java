package psymbolic.runtime.machine.buffer;

import psymbolic.runtime.Event;
import psymbolic.runtime.NondetUtil;
import psymbolic.runtime.scheduler.Scheduler;
import psymbolic.runtime.logger.TraceLogger;
import psymbolic.runtime.machine.Machine;
import psymbolic.runtime.Message;
import psymbolic.valuesummary.*;

import java.util.Map;
import java.util.HashMap;
import java.util.List;
import java.util.ArrayList;

import java.util.function.Function;


public class ReceiveEventQueue extends EventBag {

    public ReceiveEventQueue(Machine sender) {
        super(sender);
    }

    @Override
    public PrimitiveVS<Boolean> satisfiesPredUnderGuard(Function<Message, PrimitiveVS<Boolean>> pred) {
        Guard cond = isEnabledUnderGuard();
        assert(!cond.isFalse());
        Message top = peek(cond);
        return pred.apply(top).restrict(top.getUniverse());
    }


    @Override
    public Message peek(Guard pc) {
        assert (elements.getUniverse().isTrue());
        ListVS<Message> filtered = elements.restrict(pc);
        PrimitiveVS<Integer> size = filtered.size();
        List<PrimitiveVS> choices = new ArrayList<>();
        Map<Machine, Guard> targetMap = new HashMap<>();
        PrimitiveVS<Integer> idx = new PrimitiveVS<>(0).restrict(pc);
        while(BooleanVS.isEverTrue(IntegerVS.lessThan(idx, size))) {
            Guard cond = IntegerVS.lessThan(idx, size).getGuardFor(true);
            Message item = filtered.restrict(cond).get(idx);
            Guard add = Guard.constFalse();
            for (GuardedValue<Machine> machine : ((Message) item).getTarget().getGuardedValues()) {
                if (!targetMap.containsKey(machine.getValue())) {
                    targetMap.put(machine.getValue(), Guard.constFalse());
                }
                add = add.or(machine.getGuard().and(targetMap.get(machine.getValue()).not()));
                targetMap.put(machine.getValue(), targetMap.get(machine.getValue()).or(machine.getGuard()));
            }
            choices.add(idx.restrict(cond.and(add)));
            idx = IntegerVS.add(idx, 1);
        }
        PrimitiveVS<Integer> index = (PrimitiveVS<Integer>) NondetUtil.getNondetChoice(choices);
        return filtered.restrict(index.getUniverse()).get(index);
    }
        
    @Override
    public Message remove(Guard pc) {
        assert (elements.getUniverse().isTrue());
        ListVS<Message> filtered = elements.restrict(pc);
        PrimitiveVS<Integer> size = filtered.size();
        List<PrimitiveVS> choices = new ArrayList<>();
        Map<Machine, Guard> targetMap = new HashMap<>();
        PrimitiveVS<Integer> idx = new PrimitiveVS<>(0).restrict(pc);
        while(BooleanVS.isEverTrue(IntegerVS.lessThan(idx, size))) {
            Guard cond = IntegerVS.lessThan(idx, size).getGuardFor(true);
            Message item = filtered.restrict(cond).get(idx);
            Guard add = Guard.constFalse();
            for (GuardedValue<Machine> machine : ((Message) item).getTarget().getGuardedValues()) {
                if (!targetMap.containsKey(machine.getValue())) {
                    targetMap.put(machine.getValue(), Guard.constFalse());
                }
                add = add.or(machine.getGuard().and(targetMap.get(machine.getValue()).not()));
                targetMap.put(machine.getValue(), targetMap.get(machine.getValue()).or(machine.getGuard()));
            }
            choices.add(idx.restrict(cond.and(add)));
            idx = IntegerVS.add(idx, 1);
        }
        PrimitiveVS<Integer> index = (PrimitiveVS<Integer>) NondetUtil.getNondetChoice(choices);
        Message element = filtered.restrict(index.getUniverse()).get(index);
        elements = elements.removeAt(index);
        return element;
    }

}
