// PVerifier Paxos Consensus Protocol Implementation
// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

type tProposal = (id: int, value: int);
type tPrepareReq = (proposalId: int, proposer: Proposer);
type tPromiseResp = (proposalId: int, acceptedId: int, acceptedValue: int, acceptor: Acceptor);
type tAcceptReq = (proposalId: int, value: int, proposer: Proposer);
type tAcceptedResp = (proposalId: int, value: int, acceptor: Acceptor);

// ============================================================================
// EVENT DEFINITIONS
// ============================================================================

// Phase 1: Prepare/Promise
event ePrepare: tPrepareReq;
event ePromise: tPromiseResp;
event eReject: (proposalId: int, acceptor: Acceptor);

// Phase 2: Accept/Accepted
event eAccept: tAcceptReq;
event eAccepted: tAcceptedResp;

// Learning and coordination
event eChosen: (value: int, proposalId: int, proposer: Proposer);
event ePropose: (value: int);

// ============================================================================
// PURE FUNCTIONS (System Configuration)
// ============================================================================

pure proposers(): set[machine];
pure acceptors(): set[machine];
pure learners(): set[machine];
pure isQuorum(s: set[machine]): bool;
pure proposalId(p: machine): int;
init-condition forall (m1: Proposer, m2: Proposer) :: (m1 == m2) == (proposalId(m1) == proposalId(m2));

// ============================================================================
// PROPOSER MACHINE
// ============================================================================

machine Proposer {
    var currentProposalId: int;
    var proposedValue: int;
    var promiseSet: set[machine];
    var acceptSet: set[machine];
    var highestAcceptedId: int;
    var highestAcceptedValue: int;
    
    start state Idle {
        on ePropose do (payload: (value: int)) {
            var a: machine;
            proposedValue = payload.value;
            promiseSet = default(set[machine]);
            acceptSet = default(set[machine]);
            highestAcceptedId = -1;
            highestAcceptedValue = -1;
            assume currentProposalId == proposalId(this);
            assume forall (m: Proposer) :: m != this ==> m.currentProposalId != currentProposalId;
            
            // Phase 1: Send prepare to all acceptors
            foreach (a in acceptors())
                invariant forall new (e: event) :: e is ePrepare;
                invariant currentProposalId == proposalId(this);
                invariant forall (m: Proposer) :: m != this ==> m.currentProposalId != currentProposalId;
                invariant forall new (e: ePrepare) :: e.proposalId == currentProposalId && e.proposer == this;
                invariant forall new (e: event) :: forall (m: machine) :: e targets m ==> m in acceptors();
            {
                send a, ePrepare, (proposalId = currentProposalId, proposer = this);
            }
            goto Preparing;
        }
        ignore ePromise, eReject, eAccepted, eChosen;
    }
    
    state Preparing {
        on ePromise do (resp: tPromiseResp) {
            var a: machine;
            var valueToPropose: int;
            assume currentProposalId == proposalId(this);
            assume forall (m: Proposer) :: m != this ==> m.currentProposalId != currentProposalId;
            if (resp.proposalId == currentProposalId) {
                promiseSet += (resp.acceptor);
                
                // Track highest accepted proposal
                if (resp.acceptedId > highestAcceptedId) {
                    highestAcceptedId = resp.acceptedId;
                    highestAcceptedValue = resp.acceptedValue;
                }
                
                // If we have quorum promises, move to Phase 2
                if (isQuorum(promiseSet)) {
                    if (highestAcceptedId >= 0) {
                        valueToPropose = highestAcceptedValue;
                    } else {
                        valueToPropose = proposedValue;
                    }

                    proposedValue = valueToPropose;
                    
                    // Phase 2: Send accept to all acceptors
                    foreach (a in acceptors())
                        invariant forall new (e: event) :: e is eAccept;
                        invariant isQuorum(promiseSet);
                        invariant forall (m: Proposer) :: m != this ==> m.currentProposalId != currentProposalId;
                        invariant forall new (e1: eAccept) :: forall new (e2: eAccept) :: e1.proposalId == e2.proposalId && e1.value == e2.value && e1.proposer == e2.proposer;
                        invariant forall new (e: eAccept) :: e.proposalId == currentProposalId && e.value == valueToPropose && e.proposer == this;
                        invariant forall new (e: event) :: forall (m: machine) :: e targets m ==> m in acceptors();
                    {
                        send a, eAccept, (proposalId = currentProposalId, value = valueToPropose, proposer = this);
                    }
                    goto Proposing;
                }
            }
        }
        
        on eReject do (resp: (proposalId: int, acceptor: machine)) {
            goto Done;
        }
        
        ignore eAccepted, eChosen, ePropose;
    }
    
    state Proposing {
        on eAccepted do (resp: tAcceptedResp) {
            var l: machine;
            if (resp.proposalId == currentProposalId) {
                acceptSet += (resp.acceptor);
                
                // If quorum accepted, notify learners
                if (isQuorum(acceptSet)) {
                    foreach (l in learners())
                        invariant isQuorum(acceptSet);
                        invariant forall new (e: event) :: e is eChosen;
                        invariant forall new (e: eChosen) :: e.value == resp.value && e.proposalId == resp.proposalId && e.proposer == this;
                        invariant forall new (e: eChosen) :: exists (a: eAccepted) :: sent a && e.value == a.value && e.proposalId == a.proposalId;
                        invariant forall new (e: event) :: forall (m: machine) :: e targets m ==> m in learners();
                    {
                        send l, eChosen, (value=resp.value, proposalId=resp.proposalId, proposer=this);
                    }
                    goto Chosen;
                }
            }
        }
        
        ignore ePromise, eReject, ePropose;
    }
    
    state Chosen {
        ignore ePromise, eReject, eAccepted, eChosen, ePropose;
    }

    state Done {
        ignore ePromise, eReject, eAccepted, eChosen, ePropose;
    }
}

// ============================================================================
// ACCEPTOR MACHINE
// ============================================================================

machine Acceptor {
    var highestPrepare: int;
    var acceptedProposal: tProposal;
    
    start state Waiting {
        entry {
            highestPrepare = -1;
            acceptedProposal = (id = -1, value = -1);
        }
        
        on ePrepare do (req: tPrepareReq) {
            if (req.proposalId > highestPrepare) {
                highestPrepare = req.proposalId;
                send req.proposer, ePromise, (
                    proposalId = req.proposalId,
                    acceptedId = acceptedProposal.id,
                    acceptedValue = acceptedProposal.value,
                    acceptor = this
                );
            } else {
                send req.proposer, eReject, (proposalId = req.proposalId, acceptor = this);
            }
        }
        
        on eAccept do (req: tAcceptReq) {
            if (req.proposalId >= highestPrepare) {
                acceptedProposal = (id = req.proposalId, value = req.value);
                send req.proposer, eAccepted, (
                    proposalId = req.proposalId,
                    value = req.value,
                    acceptor = this
                );
            }
        }
        
        ignore eChosen, ePropose;
    }
}

// ============================================================================
// LEARNER MACHINE
// ============================================================================

machine Learner {
    var chosenValue: int;
    
    start state Learning {
        entry {
            chosenValue = -1;
        }
        
        on eChosen do (payload: (value: int, proposalId: int, proposer: Proposer)) {
            if (chosenValue == -1) {
                chosenValue = payload.value;
                goto Learned;
            }
        }
        
        ignore ePrepare, ePromise, eReject, eAccept, eAccepted;
    }
    
    state Learned {
        ignore ePrepare, ePromise, eReject, eAccept, eAccepted, eChosen;
    }
}

// ============================================================================
// INITIALIZATION CONDITIONS
// ============================================================================

init-condition forall (m: machine) :: m in proposers() == m is Proposer;
init-condition forall (m: machine) :: m in acceptors() == m is Acceptor;
init-condition forall (m: machine) :: m in learners() == m is Learner;

// Quorum axioms - any two quorums must intersect
init-condition forall (q1: set[machine], q2: set[machine]) :: 
    isQuorum(q1) && isQuorum(q2) ==> exists (a: machine) :: a in q1 && a in q2;

// Quorum must be subset of acceptors
init-condition forall (q: set[machine]) :: 
    isQuorum(q) ==> forall (a: machine) :: a in q ==> a in acceptors();

// Non-empty acceptor set has at least one quorum
init-condition acceptors() != default(set[machine]) ==> 
    exists (q: set[machine]) :: isQuorum(q);

// Initialize acceptor state
init-condition forall (a: Acceptor) :: a.highestPrepare == -1;
init-condition forall (a: Acceptor) :: a.acceptedProposal.id == -1;

// Initialize proposer state
init-condition forall (p: Proposer) :: p.currentProposalId == proposalId(p);
invariant id_unchange: forall (p: Proposer) :: p.currentProposalId == proposalId(p);
init-condition forall (p: Proposer) :: p.promiseSet == default(set[machine]);
init-condition forall (p: Proposer) :: p.acceptSet == default(set[machine]);

// non-empty acceptor and proposer sets
init-condition exists (p: Proposer) :: p in proposers();
init-condition exists (a: Acceptor) :: a in acceptors();
// non-empty learner set
init-condition exists (l: Learner) :: l in learners();

// Initialize learner state
init-condition forall (l: Learner) :: l.chosenValue == -1;

axiom forall (e1: ePropose, e2: ePropose, m1: Proposer, m2: Proposer) :: sent e1 && sent e2 && e1 targets m1 && e2 targets m2 && m1 != m2 ==> e1.value != e2.value;

// ============================================================================
// SYSTEM CONFIGURATION INVARIANTS
// ============================================================================

Lemma system_config {
    // Machine type invariants
    invariant proposer_set: forall (m: machine) :: m in proposers() == m is Proposer;
    invariant acceptor_set: forall (m: machine) :: m in acceptors() == m is Acceptor;
    invariant learner_set: forall (m: machine) :: m in learners() == m is Learner;
    
    // Quorum properties
    invariant quorum_intersection: forall (q1: set[machine], q2: set[machine]) :: 
        isQuorum(q1) && isQuorum(q2) ==> exists (a: machine) :: a in q1 && a in q2;
    invariant quorum_subset: forall (q: set[machine]) :: 
        isQuorum(q) ==> forall (a: machine) :: a in q ==> a in acceptors();

    invariant proposedIdUnique:
        forall (m1: Proposer, m2: Proposer) :: (m1 == m2) == (proposalId(m1) == proposalId(m2));
    
    // Message routing invariants
    invariant prepare_to_acceptors: forall (e: event) :: e is ePrepare ==> 
        forall (m: machine) :: e targets m ==> m in acceptors();
    invariant promise_to_proposers: forall (e: event) :: e is ePromise ==> 
        forall (m: machine) :: e targets m ==> m in proposers();
    invariant reject_to_proposers: forall (e: event) :: e is eReject ==> 
        forall (m: machine) :: e targets m ==> m in proposers();
    invariant accept_to_acceptors: forall (e: event) :: e is eAccept ==> 
        forall (m: machine) :: e targets m ==> m in acceptors();
    invariant accepted_to_proposers: forall (e: event) :: e is eAccepted ==> 
        forall (m: machine) :: e targets m ==> m in proposers();
    invariant chosen_to_learners: forall (e: event) :: e is eChosen ==> 
        forall (m: machine) :: e targets m ==> m in learners();
    invariant no_propose_to_learners:
        forall (e: ePropose, l: Learner) :: e targets l ==> !inflight e;
    invariant eChosen_implies_in_chosen:
        forall (e: eChosen) :: sent e ==> e.proposer is Chosen;
    invariant in_chosen_implies_quorum_accept:
        forall (m: Proposer) :: m is Chosen ==> isQuorum(m.acceptSet);
    
    // Proposer behaviors
    invariant proposer_id_correspond:
        forall (e: eAccept) :: sent e ==> e.proposalId == e.proposer.currentProposalId && e.value == e.proposer.proposedValue;
    invariant preparing_means_not_proposed:
        forall (m: Proposer) :: m is Preparing || m is Idle ==> forall (e: eAccept) :: e.proposer != m && e.proposalId != m.currentProposalId;
    invariant proposed_means_not_preparing:
        forall (e: eAccept) :: sent e ==> (e.proposer is Proposing || e.proposer is Chosen || e.proposer is Done);
    invariant unique_proposal:
        forall (e1: eAccept, e2: eAccept) :: sent e1 && sent e2 && e1.proposalId == e2.proposalId ==> e1.value == e2.value;
}

// ============================================================================
// PAXOS SAFETY INVARIANTS
// ============================================================================

// Lemmas
Lemma accepted_value_corresponds_to_proposed {
    invariant accepted_value_corres:
        forall (e: eAccepted, p: Proposer) :: sent e && e targets p && e.proposalId == p.currentProposalId ==> e.value == p.proposedValue;
    invariant accepted_means_proposed:
        forall (e1: eAccepted, p: Proposer) :: sent e1 && e1 targets p ==>
            exists (e2: eAccept) :: sent e2 && e2 targets e1.acceptor && e2.proposer == p
                                        && e2.proposalId == e1.proposalId
                                        && e2.value == e1.value
                                        && p.proposedValue == e1.value;
}
Proof Lem_accepted_value_corresponds_to_proposed {
    prove accepted_value_corresponds_to_proposed using system_config;
}

Lemma chosen_means_quorum_accept {
    invariant chosen_accepted:
        forall (e1: eChosen) :: sent e1 ==> isQuorum(e1.proposer.acceptSet);
    invariant chosen_value_proposed:
        forall (e1: eChosen) :: sent e1 ==> e1.value == e1.proposer.proposedValue;
}
Proof Lem_chosen_means_quorum_accept {
    prove chosen_means_quorum_accept using system_config, accepted_value_corresponds_to_proposed;
}

Lemma quorum_accept_implies_quorum_promise {
    invariant quorum_accepts:
        forall (p: Proposer) :: isQuorum(p.acceptSet) ==> isQuorum(p.promiseSet);
    invariant quorum_accept_implies_received_accepted:
        forall (p: Proposer) :: isQuorum(p.acceptSet) ==> exists (e: eAccepted) :: sent e && e targets p;
    invariant received_accepted_implies_accept_sent:
        forall (e: eAccepted, p: Proposer) :: sent e && e targets p ==> exists (e1: eAccept) :: sent e1 && e1.proposer == p;
    invariant accept_sent_implies_quorum_promise:
        forall (e: eAccept, p: Proposer) :: sent e && e.proposer == p ==> isQuorum(p.promiseSet);  
}
Proof Lem_quorum_accept_implies_quorum_promise {
    prove quorum_accept_implies_quorum_promise using system_config;
}

Lemma quorum_promise_same_value {   
    invariant quorum_promise_implies_same_value:
        forall (p1: Proposer, p2: Proposer) :: 
            isQuorum(p1.promiseSet) && isQuorum(p2.promiseSet) ==>
                p1.proposedValue == p2.proposedValue;
}
Proof Lem_quorum_accepts_and_quorum_promise {
    prove quorum_promise_same_value using system_config;
}

Lemma two_quorum_accept_implies_same_value {
    invariant two_quorum_accepts:
        forall (p1: Proposer, p2: Proposer) ::
            isQuorum(p1.acceptSet) && isQuorum(p2.acceptSet) ==>
            p1.proposedValue == p2.proposedValue;
}
Proof Lem_two_quorum_accept_implies_same_value {
    prove two_quorum_accept_implies_same_value using
        system_config, quorum_promise_same_value, quorum_accept_implies_quorum_promise;
}

// ============================================================================
// THEOREM: unique_chosen_value
// ============================================================================
Theorem unique_chosen_value {
    invariant unique_learned_value:
        forall (e1: eChosen, e2: eChosen) :: sent e1 && sent e2 ==> e1.value == e2.value;
}
Proof PaxosSafety {
    prove system_config;
    prove unique_chosen_value using system_config, chosen_means_quorum_accept, two_quorum_accept_implies_same_value;
    prove default using system_config;
}
