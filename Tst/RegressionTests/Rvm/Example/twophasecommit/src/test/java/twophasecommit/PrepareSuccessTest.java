package twophasecommit;

import org.junit.jupiter.api.Test;

public class PrepareSuccessTest {

    @Test
    void testPrepareSuccess() {
        Coordinator co = new Coordinator();
        co.addParticipant(new Participant(0, true));
        co.addParticipant(new Participant(1, true));
        co.commit(false);
    }

}
