/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */
package unittest;

import org.junit.jupiter.api.Test;
import com.runtimeverification.rvmonitor.java.rt.annotation.MonitorOn;

public class StatementTest {

    @Test
    @MonitorOn("unittest")
    void test() {
        Instrumented i = new Instrumented();
        Assert.stateNameIs(i, "Init");
        i.event1();
        Assert.stateNameIs(i, "S1");
        i.event1();
        Assert.stateNameIs(i, "S2");
        i.event1();
        Assert.stateNameIs(i, "Success");
    }

}
