package pcon;

import unittest.*;

import p.runtime.values.*;

public aspect unittestAspect {
    before () : (execution(* Instrumented.getState())) {
        unittestRuntimeMonitor.unittest_getStateEvent();
    }

    before () : (execution(* Instrumented.event1())) {
        unittestRuntimeMonitor.unittest_event1Event();
    }

    before () : (execution(* Instrumented.event2())) {
        unittestRuntimeMonitor.unittest_event2Event();
    }

    before () : (execution(* Instrumented.event3())) {
        unittestRuntimeMonitor.unittest_event3Event();
    }

    before (int a) : (execution(* Instrumented.event1Int(int))) && args(a) {
        unittestRuntimeMonitor.unittest_event1IntEvent(new IntValue(a));
    }

    before (int a) : (execution(* Instrumented.event2Int(int))) && args(a) {
        unittestRuntimeMonitor.unittest_event2IntEvent(new IntValue(a));
    }

    before (int a) : (execution(* Instrumented.event3Int(int))) && args(a) {
        unittestRuntimeMonitor.unittest_event3IntEvent(new IntValue(a));
    }
}
