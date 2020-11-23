package unittest;

import org.junit.jupiter.api.Test;

public class TupleTest {

    @Test
    void test() {
        Instrumented i = new Instrumented();
        i.event1();
    }

}
