package psymbolic.runtime.scheduler;

import psymbolic.runtime.*;
import psymbolic.runtime.machine.Machine;
import psymbolic.runtime.machine.buffer.SymbolicQueue;

import psymbolic.valuesummary.*;

import java.util.HashMap;
import java.util.Map;

public class ReceiverQueue {
    private static Map<Machine, SymbolicQueue<PrimitiveVS<Machine>>> receiveFrom = new HashMap<>();

    public void sendFromTo(Guard pc, Machine src, PrimitiveVS<Machine> tgt) {
        PrimitiveVS<Machine> source = new PrimitiveVS(src).restrict(pc);
        for (GuardedValue<Machine> targetGV : tgt.getGuardedValues()) {
            Machine tgtValue = targetGV.getValue();
            if (!receiveFrom.containsKey(tgtValue)) {
                receiveFrom.put(tgtValue, new SymbolicQueue<>());
            }
            receiveFrom.get(tgtValue).enqueue(source.restrict(targetGV.getGuard()));
        }
    }    

    public void remove(Guard pc, Machine source, PrimitiveVS<Machine> tgt) {
        PrimitiveVS<Machine> src = new PrimitiveVS<>(source);
        for (GuardedValue<Machine> targetGV : tgt.getGuardedValues()) {
            Machine tgtValue = targetGV.getValue(); 
            if (receiveFrom.containsKey(tgtValue)) {
                Guard peekGuard = receiveFrom.get(tgtValue).isEnabledUnderGuard().and(targetGV.getGuard().and(pc));
                if (!peekGuard.isFalse()) {
                    Guard dequeueGuard = receiveFrom.get(tgtValue).peek(peekGuard).symbolicEquals(src, peekGuard).getGuardFor(true);
                    receiveFrom.get(tgtValue).dequeueEntry(dequeueGuard);
                }
            }
        }
    }

    public PrimitiveVS<Boolean> guardFor(Machine source, Message m) {
        PrimitiveVS<Machine> tgt = m.getTarget();
        PrimitiveVS<Machine> src = new PrimitiveVS<>(source);
        Guard resGuard = Guard.constFalse();
        for (GuardedValue<Machine> targetGV : tgt.getGuardedValues()) {
            resGuard = resGuard.or(receiveFrom.get(targetGV.getValue()).peek(targetGV.getGuard()).symbolicEquals(src, targetGV.getGuard()).getGuardFor(true).and(targetGV.getGuard()));
        }
        return new PrimitiveVS<>(true).restrict(resGuard);
    } 
}
