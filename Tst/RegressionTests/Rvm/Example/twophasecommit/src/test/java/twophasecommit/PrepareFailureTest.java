/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */
package twophasecommit;

import org.junit.jupiter.api.Test;
import com.runtimeverification.rvmonitor.java.rt.annotation.MonitorOn;

public class PrepareFailureTest {

    @Test
    @MonitorOn("twoPhaseCommit")
    void testPrepareFailure() {
        Coordinator co = new Coordinator();
        co.addParticipant(new Participant(0, true));
        co.addParticipant(new Participant(1, false));
        co.commit(false);
    }

}
