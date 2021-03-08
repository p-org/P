/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */
package twophasecommit;

import org.junit.jupiter.api.Test;
import com.runtimeverification.rvmonitor.java.rt.annotation.MonitorOn;

public class PrepareSuccessTest {

    @Test
    @MonitorOn("twoPhaseCommit")
    void testPrepareSuccess() {
        Coordinator co = new Coordinator();
        co.addParticipant(new Participant(0, true));
        co.addParticipant(new Participant(1, true));
        co.commit(false);
    }

}
