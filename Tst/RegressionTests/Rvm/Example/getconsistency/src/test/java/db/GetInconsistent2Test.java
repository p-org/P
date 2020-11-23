package db;

import org.junit.jupiter.api.Test;

import p.runtime.exceptions.AssertStmtError;

public class GetInconsistent2Test {

    @Test
    void testDatabase() {
        try {
            Database db = new Database();
            db.putReq(new Record("a", 5, 0), 0);
            db.putReq(new Record("a", 6, 1), 1);
            db.putRes(true, new Record("a", 5, 0), 0);
            db.putRes(true, new Record("a", 6, 1), 1);
            db.getReq("a", 2);
            db.getRes(true, new Record("a", 5, 0), 2);
            throw new AssertionError("Expected a monitor assertion failure.");
        } catch (AssertStmtError e) {
            assert (e.getMessage().equals("Assertion Failed: For key a, expected value of sequencer is >= 1 but got 0. Get is not Consistent!"));
        }
    }

}
