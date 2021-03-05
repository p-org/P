/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */
package db;

import org.junit.jupiter.api.Test;
import com.runtimeverification.rvmonitor.java.rt.annotation.MonitorOn;

public class GetConsistentTest {

    @Test
    @MonitorOn("getConsistency")
    void testDatabase() {
        Database db = new Database();
        db.putReq(new Record("a", 5, 0), 0);
        db.putReq(new Record("a", 6, 1), 1);
        db.putRes(true, new Record("a", 5, 0), 0);
        db.putRes(true, new Record("a", 6, 1), 1);
        db.getReq("a", 2);
        db.getRes(true, new Record("a", 6, 1), 2);
    }

}
