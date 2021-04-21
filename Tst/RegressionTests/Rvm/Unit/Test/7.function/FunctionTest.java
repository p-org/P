/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */
package unittest;

import org.junit.jupiter.api.Test;
import com.runtimeverification.rvmonitor.java.rt.annotation.MonitorOn;

public class FunctionTest {

    @Test
    @MonitorOn("unittest")
    void test() {
        Instrumented i = new Instrumented();
        i.event1();
    }

}
