package psymbolic.runtime.scheduler;

import psymbolic.runtime.*;
import psymbolic.valuesummary.Guard;

import java.io.Serializable;

public interface MessageOrder extends Serializable {
    public Guard lessThan(Message m0, Message m1);
}
