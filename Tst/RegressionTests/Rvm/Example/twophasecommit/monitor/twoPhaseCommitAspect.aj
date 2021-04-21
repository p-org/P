package pcon;

import p.runtime.values.*;
import twophasecommit.*;

public aspect twoPhaseCommitAspect {
    // Implement your code here.
    pointcut twoPhaseCommit_startTx() : (execution(* Coordinator.commit(..)));
    before () : twoPhaseCommit_startTx() {
        twoPhaseCommitRuntimeMonitor.twoPhaseCommit_startTxEvent();
    }

    pointcut twoPhaseCommit_addParticipant(Participant p) : (execution(* Coordinator.addParticipant(Participant)) && args(p));
    after (Participant p) : twoPhaseCommit_addParticipant(p) {
        twoPhaseCommitRuntimeMonitor.twoPhaseCommit_addParticipantEvent(new IntValue(p.machineId));
    }

    pointcut twoPhaseCommit_prepare(Participant p) : (execution(* Participant.prepare(..)) && target(p));
    after (Participant p) returning (boolean result) : twoPhaseCommit_prepare(p) {
        if (result) {
            twoPhaseCommitRuntimeMonitor.twoPhaseCommit_prepareSuccessEvent(new IntValue(p.machineId));
        } else {
            twoPhaseCommitRuntimeMonitor.twoPhaseCommit_prepareFailureEvent(new IntValue(p.machineId));
        }
    }

    pointcut twoPhaseCommit_rollbackSuccess(Participant p) : (execution(* Participant.rollback(..)) && target(p));
    after (Participant p) : twoPhaseCommit_rollbackSuccess(p) {
        twoPhaseCommitRuntimeMonitor.twoPhaseCommit_rollbackSuccessEvent(new IntValue(p.machineId));
    }

    pointcut twoPhaseCommit_commitSuccess(Participant p) : (execution(* Participant.commit(..)) && target(p));
    after (Participant p) : twoPhaseCommit_commitSuccess(p) {
        twoPhaseCommitRuntimeMonitor.twoPhaseCommit_commitSuccessEvent(new IntValue(p.machineId));
    }

    after () : twoPhaseCommit_startTx() {
        twoPhaseCommitRuntimeMonitor.twoPhaseCommit_endTxEvent();
    }
}