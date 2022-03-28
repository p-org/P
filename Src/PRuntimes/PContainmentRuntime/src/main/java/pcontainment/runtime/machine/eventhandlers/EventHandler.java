package pcontainment.runtime.machine.eventhandlers;

import com.microsoft.z3.BoolExpr;
import pcontainment.Pair;
import pcontainment.runtime.Event;
import pcontainment.runtime.Payloads;
import pcontainment.runtime.machine.Machine;

import java.util.Map;

public abstract class EventHandler {
    public final Event event;

    public EventHandler(Event eventType) {
        this.event = eventType;
    }

    // return Encoding -> ReturnReason
    public abstract Map<BoolExpr, Pair<Integer, EventHandlerReturnReason>> getEncoding(int sends, Machine target, Payloads payloads);
}
