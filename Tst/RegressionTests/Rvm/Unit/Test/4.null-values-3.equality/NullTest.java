/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */
package unittest;

import org.junit.jupiter.api.Test;

public class NullTest {

    @Test
    void testNullValues() {
        Instrumented i = new Instrumented();
        Assert.stateNameIs(i, "Start");
        i.event1();
        Assert.stateNameIs(i, "If1");
        i.event1();
        Assert.stateNameIs(i, "If2");
        i.event1();
        Assert.stateNameIs(i, "If3");
        i.event1();
        Assert.stateNameIs(i, "If4");
        i.event1();
        Assert.stateNameIs(i, "If5");
        i.event1();
        Assert.stateNameIs(i, "Success");
    }

}
