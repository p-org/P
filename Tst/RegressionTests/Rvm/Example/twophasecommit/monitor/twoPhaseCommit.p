event addParticipant: int;
event startTx;
event prepareSuccess: int;
event prepareFailure: int;
event rollbackSuccess: int;
event commitSuccess: int;
event endTx;

spec twoPhaseCommit observes addParticipant, startTx, prepareSuccess, prepareFailure, rollbackSuccess, commitSuccess, endTx {
    var participantNum: int;
    var preparedNum: int;
    var rolledbackNum: int;
    var committedNum: int;

    start state Init {
        entry {
            participantNum = 0;
            preparedNum = 0;
            rolledbackNum = 0;
            committedNum = 0;
        }

        on addParticipant do (m: int) {
            participantNum = participantNum + 1;
        }

        on startTx do {
            goto Prepare;
        }
    }

    state Prepare {
        on prepareSuccess do (m: int) {
            preparedNum = preparedNum + 1;
            if (preparedNum == participantNum) {
                goto Commit;
            }
        }

        on prepareFailure do (m: int) {
            goto Rollback;
        }
    }

    state Rollback {
        on prepareSuccess do (m: int) {
        }

        on prepareFailure do (m: int) {
        }

        on rollbackSuccess do (m: int) {
            rolledbackNum = rolledbackNum + 1;
        }

        on endTx do {
            assert (rolledbackNum == participantNum), "Rollback failed.";
            print "RolledBack.";
        }
    }

    state Commit {
        on commitSuccess do (m: int) {
            committedNum = committedNum + 1;
        }

        on endTx do {
            assert (committedNum == participantNum), "Commit failed.";
            print "Committed.";
        }
    }
}
