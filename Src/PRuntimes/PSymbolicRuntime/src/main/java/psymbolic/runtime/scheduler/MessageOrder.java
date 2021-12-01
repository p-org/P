package psymbolic.runtime.scheduler;

import psymbolic.runtime.*;
import psymbolic.valuesummary.Guard;

public interface MessageOrder {
    public Guard lessThan(Message m0, Message m1);
}
