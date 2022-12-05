package psym.runtime.scheduler;

import psym.runtime.*;
import psym.valuesummary.Guard;

import java.io.Serializable;

public interface MessageOrder extends Serializable {
    public Guard lessThan(Message m0, Message m1);
}
