package twophasecommit;

import org.junit.jupiter.api.Test;

public class PrepareFailureTest {

    @Test
    void testPrepareFailure() {
        Coordinator co = new Coordinator();
        co.addParticipant(new Participant(0, true));
        co.addParticipant(new Participant(1, false));
        co.commit(false);
    }

}
