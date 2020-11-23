/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */
package unittest;

import org.junit.jupiter.api.Test;

public class NullTest {

    @Test
    void testNullValues() {
        Instrumented i = new Instrumented();
        Assert.stateNameIs(i, "Start");
        i.event1();
        Assert.stateNameIs(i, "Tuple1");
        i.event1();
        Assert.stateNameIs(i, "Tuple2");
        i.event1();
        Assert.stateNameIs(i, "Tuple3");
        Assert.nullPointerException(() -> i.event1());
        Assert.stateNameIs(i, "Tuple3");
        i.event2();
        Assert.stateNameIs(i, "Tuple4");
        i.event1();
        Assert.stateNameIs(i, "Tuple5");
        i.event1();
        Assert.stateNameIs(i, "Success");
    }

}
