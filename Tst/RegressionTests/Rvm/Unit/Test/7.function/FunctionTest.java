package unittest;

import org.junit.jupiter.api.Test;

public class FunctionTest {

    @Test
    void test() {
        Instrumented i = new Instrumented();
        i.event1();
    }

}
