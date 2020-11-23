package unittest;

import org.junit.jupiter.api.Test;

import p.runtime.exceptions.AssertStmtError;

public class AssertTest {

    @Test
    void test() {
        try {
            Instrumented i = new Instrumented();
            i.event1();
            throw new AssertionError("Expected a AssertStmtError.");
        } catch (AssertStmtError e) {
            assert(e.getMessage().equals("Assertion Failed: assert false"));
        }
    }

}
