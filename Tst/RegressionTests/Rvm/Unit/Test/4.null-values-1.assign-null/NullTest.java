/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */
package unittest;

import org.junit.jupiter.api.Test;
import com.runtimeverification.rvmonitor.java.rt.annotation.MonitorOn;

public class NullTest {

    @Test
    @MonitorOn("unittest")
    void testNullValues() {
        Instrumented i = new Instrumented();
        Assert.stateNameIs(i, "Start");
        i.event1();
        Assert.stateNameIs(i, "Success");
    }

}
