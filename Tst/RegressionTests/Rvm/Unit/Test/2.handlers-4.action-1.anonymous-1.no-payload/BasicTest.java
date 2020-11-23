package unittest;

import org.junit.jupiter.api.Test;

public class BasicTest {

    @Test
    void test() {
        Instrumented i = new Instrumented();
        Assert.stateNameIs(i, "Start");
        i.event1();
        Assert.stateNameIs(i, "Success");
    }

}
