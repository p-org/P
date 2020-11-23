package unittest;

import org.junit.jupiter.api.Test;

import p.runtime.exceptions.MapInsertError;

public class MapTest {

    @Test
    void testMap() {
        Instrumented i = new Instrumented();
        Assert.stateNameIs(i, "Init");
        i.event1();
        Assert.stateNameIs(i, "InsertDuplicateKey");
        try {
            i.event2();
            throw new AssertionError("Expected a MapInsertError.");
        } catch (MapInsertError e) {
            assert(e.getMessage().equals("key 'a' already exists."));
        }
    }

}
