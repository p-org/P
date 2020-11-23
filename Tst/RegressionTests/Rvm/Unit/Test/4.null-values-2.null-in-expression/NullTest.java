/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */
package unittest;

import org.junit.jupiter.api.Test;

public class NullTest {

    @Test
    void testNullValues() {
        Instrumented i = new Instrumented();
        Assert.stateNameIs(i, "Start");
        i.event1();
        Assert.stateNameIs(i, "And1");
        i.event1();
        Assert.stateNameIs(i, "And2");
        i.event1();
        Assert.stateNameIs(i, "And3");
        Assert.nullPointerException(() -> i.event1());
        Assert.stateNameIs(i, "And3");
        i.event2();
        Assert.stateNameIs(i, "Not1");
        Assert.nullPointerException(() -> i.event1());
        Assert.stateNameIs(i, "Not1");
        i.event2();
        Assert.stateNameIs(i, "Success");
    }

}
