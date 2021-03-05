/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */
package db;

import org.junit.jupiter.api.Test;

import p.runtime.exceptions.AssertStmtError;
import com.runtimeverification.rvmonitor.java.rt.annotation.MonitorOn;

public class GetInconsistent1Test {

    @Test
    @MonitorOn("getConsistency")
    void testDatabase() {
        try {
            Database db = new Database();
            db.putReq(new Record("a", 5, 0), 0);
            db.putRes(true, new Record("a", 5, 0), 0);
            db.getReq("a", 1);
            db.getRes(false, new Record("a", 0, 0), 1);
            throw new AssertionError("Expected a monitor assertion failure.");
        } catch (AssertStmtError e) {
            assert (e.getMessage().equals("Assertion Failed: Get is not Consistent!! Get responded KEYNOTFOUND for a key '{a}', even when a record a existed"));
        }
    }

}
