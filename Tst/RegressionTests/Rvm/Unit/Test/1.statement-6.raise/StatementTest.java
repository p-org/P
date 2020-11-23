package unittest;

import org.junit.jupiter.api.Test;

public class StatementTest {

    @Test
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
