package pcontainment.runtime.machine.eventhandlers;

import com.microsoft.z3.BoolExpr;
import pcontainment.Pair;
import pcontainment.Triple;
import pcontainment.runtime.Event;
import pcontainment.runtime.Payloads;
import pcontainment.runtime.machine.Locals;
import pcontainment.runtime.machine.Machine;
import pcontainment.runtime.machine.State;

import java.util.Map;

public abstract class EventHandler {
    public final Event event;
    public final State state;

    public EventHandler(Event eventType, State state) {
        this.event = eventType;
        this.state = state;
    }

    // return Encoding -> ReturnReason
    public abstract Map<BoolExpr, Triple<Integer, Locals, EventHandlerReturnReason>>
        getEncoding(int sends, Locals locals, Machine target, Payloads payloads);
}
