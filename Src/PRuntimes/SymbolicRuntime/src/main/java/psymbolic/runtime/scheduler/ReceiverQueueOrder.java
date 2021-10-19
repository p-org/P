package psymbolic.runtime.scheduler;

import psymbolic.runtime.*;
import psymbolic.runtime.machine.Machine;
import psymbolic.runtime.machine.buffer.SymbolicQueue;

import psymbolic.valuesummary.*;

import java.util.HashMap;
import java.util.Map;

public class ReceiverQueueOrder implements MessageOrder {
    
    private ReceiverQueueOrder() {}

    public static ReceiverQueueOrder getInstance() { return new ReceiverQueueOrder(); }

    public Guard lessThan (Message m0, Message m1) {
        Guard sameTarget = m0.getTarget().symbolicEquals(m1.getTarget(), m0.getUniverse()).getGuardFor(true);
        PrimitiveVS<Integer> res = m0.getVectorClock().cmp(m1.getVectorClock());
        return res.getGuardFor(-1).and(sameTarget);
    }
}
