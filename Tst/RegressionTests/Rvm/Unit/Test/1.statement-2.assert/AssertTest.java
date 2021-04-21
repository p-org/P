/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */
package unittest;

import org.junit.jupiter.api.Test;

import p.runtime.exceptions.AssertStmtError;
import com.runtimeverification.rvmonitor.java.rt.annotation.MonitorOn;

public class AssertTest {

    @Test
    @MonitorOn("unittest")
    void test() {
        try {
            Instrumented i = new Instrumented();
            i.event1();
            throw new AssertionError("Expected a AssertStmtError.");
        } catch (AssertStmtError e) {
            assert(e.getMessage().equals("Assertion Failed: assert false"));
        }
    }

}
