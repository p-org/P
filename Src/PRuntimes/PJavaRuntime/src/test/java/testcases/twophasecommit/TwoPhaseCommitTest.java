package testcases.twophasecommit;

import java.util.List;

import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import prt.exceptions.PAssertionFailureException;

import static org.junit.jupiter.api.Assertions.*;

import static testcases.twophasecommit.TwoPhaseCommit.*;

public class TwoPhaseCommitTest {
    private AtomicityInvariant initedMonitor() {
        AtomicityInvariant m = new AtomicityInvariant();
        m.ready();
        m.accept(new eMonitor_AtomicityInitialize(3)); // We'll now be in WaitForEvents
        return m;
    }

    @Test
    @DisplayName("Can handle successful commits")
    public void testSuccessfulCommitTest() {
        AtomicityInvariant m = initedMonitor();

        for (long participant : List.of(1L,2L,3L)) {
            m.accept(new ePrepareResp(
                    new PTuple_prtcp_trans_stts(participant, 0, tTransStatus.SUCCESS)));
        }

        m.accept(new eWriteTransResp(
                new PTuple_trans_stts(0, tTransStatus.SUCCESS)));
    }

    @Test
    @DisplayName("Can handle successful rollbacks")
    public void testSuccessfulRollback() {
        AtomicityInvariant m = initedMonitor();

        // Participants 1 and two say yes...
        for (long participant : List.of(1L,2L)) {
            m.accept(new ePrepareResp(
                    new PTuple_prtcp_trans_stts(participant, 0, tTransStatus.SUCCESS)));
        }
        // Participant 3 says no!
        m.accept(new ePrepareResp(
                new PTuple_prtcp_trans_stts(3L, 0, tTransStatus.ERROR)));

        // should be able to handle a txn response with an error
        m.accept(new eWriteTransResp(
                new PTuple_trans_stts(0, tTransStatus.ERROR)));
    }

    @Test
    @DisplayName("Throws on an unsuccessful commit (owing to a premature commit message)")
    public void testPrematureCommitTest() {
        AtomicityInvariant m = initedMonitor();

        /* Only two SUCCESSes; one never arrives! */
        for (long participant : List.of(1L,2L)) {
            m.accept(new ePrepareResp(
                    new PTuple_prtcp_trans_stts(participant, 0, tTransStatus.SUCCESS)));
        }

        assertThrows(PAssertionFailureException.class,
                () -> m.accept(new eWriteTransResp(
                        new PTuple_trans_stts(0, tTransStatus.SUCCESS))));
    }

    @Test
    @DisplayName("Throws on an unsuccessful commit (owing to a premature commit message)")
    public void testUnsuccessfulCommitTest() {
        AtomicityInvariant m = initedMonitor();

        for (long participant : List.of(1L,2L)) {
            m.accept(new ePrepareResp(
                    new PTuple_prtcp_trans_stts(participant, 0, tTransStatus.SUCCESS)));
        }
        // Participant 3 says no dice!
        m.accept(
                new ePrepareResp(
                        new PTuple_prtcp_trans_stts(
                                3L,
                                0,
                                tTransStatus.ERROR)));

        assertThrows(PAssertionFailureException.class,
                () -> m.accept(new eWriteTransResp(
                        new PTuple_trans_stts(0, tTransStatus.SUCCESS))));
    }
}
