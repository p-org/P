event eRequestVote: (src: Node);
event eVote: (voter: Node);

pure nodes(): set[machine];
pure isQuorum(s: set[machine]): bool;

init-condition forall (m: machine) :: m in nodes() == m is Node;
axiom forall (q1: set[machine], q2: set[machine]) :: 
    isQuorum(q1) && isQuorum(q2) ==> exists (a: machine) :: a in q1 && a in q2;
axiom forall (q: set[machine]) :: 
    isQuorum(q) ==> forall (a: machine) :: a in q ==> a in nodes();

init-condition forall (n: Node) :: !n.voted && n.votes == default(set[machine]);

machine Node {
    var voted: bool;
    var votes: set[machine];

    start state RequestVoting {
        entry {
            var m: machine;
            foreach (m in nodes())
                invariant forall new (e: event) :: e is eRequestVote;
                invariant forall new (e: eRequestVote) :: e.src == this;
            {
                send m, eRequestVote, (src=this,);
            }
        }

        on eRequestVote do (payload: (src: Node)) {
            if (!voted) {
                voted = true;
                send payload.src, eVote, (voter=this,);
            }
        }

        on eVote do (payload: (voter: Node)) {
            votes += (payload.voter);
            if (isQuorum(votes)) {
                goto Won;
            }
        }

    }

    state Won {
        ignore eRequestVote, eVote;
    }
}

Lemma quorum_votes {
    invariant one_vote_per_node:
        forall (e1: eVote, e2: eVote) :: e1.voter == e2.voter ==> e1 == e2;
    invariant won_implies_quorum_votes:
        forall (n: Node) :: n is Won ==> isQuorum(n.votes);
}
Proof {
    prove quorum_votes;
}

Theorem election_safety {
    invariant unique_leader:
        forall (n1: Node, n2: Node) :: n1 is Won && n2 is Won ==> n1 == n2;
}
Proof {
    prove election_safety using quorum_votes;
    prove default;
}
