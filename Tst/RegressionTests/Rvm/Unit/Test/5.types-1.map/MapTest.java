/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */
package unittest;

import org.junit.jupiter.api.Test;

import p.runtime.exceptions.MapInsertError;
import com.runtimeverification.rvmonitor.java.rt.annotation.MonitorOn;

public class MapTest {

    @Test
    @MonitorOn("unittest")
    void testMap() {
        Instrumented i = new Instrumented();
        Assert.stateNameIs(i, "Init");
        i.event1();
        Assert.stateNameIs(i, "InsertDuplicateKey");
        try {
            i.event2();
            throw new AssertionError("Expected a MapInsertError.");
        } catch (MapInsertError e) {
            assert(e.getMessage().equals("key 'a' already exists."));
        }
    }

}
