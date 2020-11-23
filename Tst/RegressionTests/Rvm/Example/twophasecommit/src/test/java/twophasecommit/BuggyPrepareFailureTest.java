/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */
package twophasecommit;

import org.junit.jupiter.api.Test;

import p.runtime.exceptions.AssertStmtError;

public class BuggyPrepareFailureTest {

    @Test
    void testPrepareFailure() {
        try {
            Coordinator co = new Coordinator();
            co.addParticipant(new Participant(0, true));
            co.addParticipant(new Participant(1, false));
            co.commit(true);
            throw new AssertionError("Expected a monitor assertion failure.");
        } catch (AssertStmtError e) {
            assert (e.getMessage().equals("Assertion Failed: Rollback failed."));
        }
    }

}
