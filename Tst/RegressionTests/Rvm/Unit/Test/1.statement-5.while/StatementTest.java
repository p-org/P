package unittest;

import org.junit.jupiter.api.Test;

public class StatementTest {

    @Test
    void test() {
        Instrumented i = new Instrumented();
        i.event1();
    }

}
