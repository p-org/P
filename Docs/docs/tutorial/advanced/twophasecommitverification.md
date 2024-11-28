# Introduction to Formal Verification in P

This tutorial describes the formal verification features of P through an example. We assume that the reader has P installed along with the verification dependencies (i,e., UCLID5 and Z3). Installation instructions are available [here](https://github.com/p-org/P/blob/experimental/pverifier/INSTALL.md).

When using P for formal verification, our goal is to show that no execution of any test driver will violate a specification. To do this, we will rely on proofs by induction---more on that later. This backend is different from P's explicit state model checker, which you are accustomed to using. These differences can influence the modeling decisions you make. 

To get a sense of these differences, and to cover the new features in P for verification, we will verify a simplified 2PC protocol. The P tutorial already describes a 2PC protocol, but we will make some different modeling choices that will make the verification process easier. In particular, we will follow the modeling decisions made by [Zhang et al.](https://www.usenix.org/system/files/osdi24-zhang-nuda.pdf) in their running example.

## 2PC Model

The 2PC protocol consists of two types of machines: coordinator and participant. There is a single coordinator, who receives requests from the outside world, and a set of participants, that must agree on whether to accept or reject the request. If any participant wants to reject the request, they must all agree to reject the request; if all participants want to accept the request, then they must all agree to accept the request. The job of the coordinator is to mediate this agreement.
To accomplish this, the system executes the following steps:

1. The coordinator sends a message to all participants which asks the participants to vote on the request in question.
2. When a participant receives this vote request message, they reply with their vote.
3. When the coordinator receives a "no" vote---indicating that a participant wants to reject the request---it will send a message to all participants telling them to abort the request.
4. Otherwise, the coordinator tallies the "yes" vote. If all participants have voted "yes," then the coordinator sends a message to all participants telling them to commit the request.
5. When a participant receives either a commit or abort message from the coordinator, it store the decision locally.

This system is extremely easy to model in P. Before defining the machines in the system, let's declare the five types of messages that are sent.

```
type tVoteResp = (source: machine); // we will keep track of who sent a vote
event eVoteReq;
event eYes: tVoteResp;
event eNo: tVoteResp;
event eAbort;
event eCommit;
```

### The Coordinator

The coordinator is a machine with a single variable that we will use to keep track of votes. This machine has four states: one to kickoff the voting process, one to collect the votes; and two to indicate if the voting process resulted in a commit or an abort.

```
machine Coordinator
{
    var yesVotes: set[machine]; // set of machines that have voted yes
    
    start state Init {...}
    state WaitForResponses {...}
    state Committed {ignore eYes, eNo;}
    state Aborted {ignore eYes, eNo;}
}
```

In the above code we omitted the internal details of states. Now let's go through these details one by one. First, the start state, called Init, contains a single for-loop that sends a vote request message to all the participants in the system. We use a function called "participants" to get the set of active participants.

```
    start state Init {
        entry {
            var p: machine;
            foreach (p in participants())
            {
                send p, eVoteReq; // broadcast vote request to all participants
            }
            goto WaitForResponses; // move to WaitForResponses state
        }
    }
```

Second, the `WaitForResponses` state has two event handlers, one for each type of vote that participants can cast.

```
state WaitForResponses {
    on eYes do (resp: tVoteResp) {...}
    on eNo do (resp: tVoteResp) {...}
}
```

The details of when the coordinator receives "no" votes is simpler, so lets begin there. When the coordinator receives a "no" vote, it sends an abort message to all participants.

```
on eNo do (resp: tVoteResp) {
    var p: machine;
    foreach (p in participants())
    {
        send p, eAbort; // broadcast abort to all participants
    }
    goto Aborted;
}
```

When a coordinator receives a "yes" vote, it will tally the vote and only broadcast a commit message to all participants if all participants have voted yes.

```
on eYes do (resp: tVoteResp) {
    var p: machine;
    yesVotes += (resp.source);   
    if (yesVotes == participants()) { // if everyone voted "yes"
        foreach (p in participants())
        {
            send p, eCommit; // broadcast commit message
        }
        goto Committed; // move to committed state: request was accepted
    }
}
```

The final two states, `Committed` and `Aborted` will remain empty for now: we just want to use them to indicate the state of a request.

### Participants

Participants are slightly simpler. They consist of two states: one that does all the work, and two that we use to indicate that a request has been committed or aborted, just like we did for the coordinator.

```
machine Participant {
    start state Undecided {...}
    state Accepted {ignore eVoteReq, eCommit, eAbort;}
    state Rejected {ignore eVoteReq, eCommit, eAbort;}
}
```

The main state, called "Undecided," has three event handlers: one for responding to vote requests, and two simpler ones for handling commit and abort messages.

```
on eVoteReq do {
    // vote based on your preference!
    if (preference(this)) {
        send coordinator(), eYes, (source = this,);
    } else {
        send coordinator(), eNo, (source = this,);
    }
}

on eCommit do {goto Accepted;}
on eAbort do {goto Rejected;}
```

We use a function called "preference" to decide whether to vote yes or no on a transaction. We also use a function, "coordinator," to get the address of the coordinator machine.

## Pure Functions

The 2PC model described above uses three special functions, `participants`, `coordinator`, and `preference`, that capture the set of participants, the coordinator in charge, and the preference of individual participants for the given request. In this simple system, there is always one coordinator and a fixed set of participants, but we want the proof to work for any function that satisfies those conditions. In P, we can use the new concept of "pure" functions to model this (what SMT-LIB calls functions). Specifically, we can declare the three special functions as follows.

```
pure participants(): set[machine];
pure coordinator(): machine;
pure preference(m: machine) : bool;
```

The participants function is a pure function with no body that takes no argument and returns a set of machines. The coordinator function is similar but only returns a single machine. The preference function, which also has no body, takes a machine and returns a preference. We call these functions "pure" because they can have no side-effects and behave like mathematical functions (e.g., calling the same pure function twice with the same arguments must give the same result). When pure functions do not have bodies, they are like foreign functions that we can guarantee don't have side-effects. When pure functions do have bodies, the bodies must be side-effect-free expressions.

## Initialization Conditions

We want our model to capture many different system configurations (e.g., number of participants) but not all configurations are valid. For example, we want to constrain the `participants` function to only point to participant machines. Initialization conditions let us constrain the kinds of systems that we consider. You can think of these as constraints that P test harnesses have to satisfy to be considered valid.

In our 2PC model, for example, we can state that, at initialization, there is a unique machine of type coordinator, and the `coordinator` function points to that machine; and every machine in the participants set is a machine of type participant.

```
init-condition forall (m: machine) :: m == coordinator() == m is Coordinator;
init-condition forall (m: machine) :: m in participants() == m is Participant;
```

We can also state that all `yesVotes` tallies start empty.

```
init-condition forall (c: Coordinator) :: c.yesVotes == default(set[machine]);
```

When we write a proof of correctness later in this tutorial, we will be restricting the systems that we consider to those that satisfy the initialization conditions listed above.

## Quantifiers and Machine Types

Our initialization conditions contain two new P features: the `init` keyword, and quantified expressions (`forall` and `exists`). Even more interesting, one quantifier above is over a machine subtype (`coordinator`). 

In P, the only way to dereference a machine variable inside of a specification (like the `init-condition`s above) is by specifically quantifying over that machine type. In other words, `forall (c: Coordinator) :: c.yesVotes == default(set[machine])` is legal but `forall (c: machine) :: c.yesVotes == default(set[machine])` is not, even though they might appear to be similar. The reason for this is that selecting (using the `.` operator) on an incorrect subtype (e.g., trying to get `yesVotes` from a participant machine) is undefined. Undefined behavior in formal verification can lead to surprising results that can be really hard to debug, so in P we syntactically disallow this kind of undefined behavior altogether.

## P's Builtin Specifications And Our First Proof Attempt

Using the code described above, and by setting the target to `UCLID5` in the `.pproj` file, you can now run the verification engine for the first time (execute `p compile`). This run will result in a large list of failures, containing items like `❌  Failed to verify that Coordinator never receives eVoteReq in Init`. These failures represent P's builtin requirements that all events are handled. They also give us a glimpse into how verification by induction works.

Proofs by induction consist of a base case check and an inductive step check. The inductive step is more interesting and so we will focus our attention there. The high level idea is that you assume you are in a _good_ state of the system (I will describe what I mean by _good_ in the next section), and then you check if taking any step of the system will again land you in a _good_ state. in P, taking a step of the system means executing any event handler in any machine.

When we ran our verification engine it reported that it failed to prove that all of P's builtin specifications were satisfied. Specifically, the verification engine gave us a list of all the builtin specifications that it failed to prove, like ``❌  Failed to verify that Coordinator never receives eVoteReq in Init``.

The verification engine is unable to prove these properties not because the system is incorrect, but rather because it needs help from the user to complete it's proof: it needs the user to define the _good_ states. More formally, it needs the user to define an inductive invariant that implies that no builtin P specification is violated.

## Invariants And Our First Proof

Users can provide invariants to P using the `invariant` keyword. The goal is to find a set of invariants whose conjunction is inductive and implies the desired property. For now, the desired property is that no builtin P specification is violated.

In the 2PC model, the following 10 invariants are sufficient to prove that no builtin P specification is violated.

```
invariant one_coordinator: forall (m: machine) :: m == coordinator() == m is Coordinator;
invariant participant_set: forall (m: machine) :: m in participants() == m is Participant;

invariant never_commit_to_coordinator: forall (e: event) :: e is eCommit && e targets coordinator() ==> !inflight e;
invariant never_abort_to_coordinator: forall (e: event) :: e is eAbort && e targets coordinator() ==> !inflight e;
invariant never_req_to_coordinator: forall (e: event) :: e is eVoteReq && e targets coordinator() ==> !inflight e;
invariant never_yes_to_participant: forall (e: event, p: Participant) :: e is eYes && e targets p ==> !inflight e;
invariant never_yes_to_init: forall (e: event, c: Coordinator) :: e is eYes && e targets c && c is Init ==> !inflight e;
invariant never_no_to_participant: forall (e: event, p: Participant) :: e is eNo && e targets p ==> !inflight e;
invariant never_no_to_init: forall (e: event, c: Coordinator) :: e is eNo && e targets c && c is Init ==> !inflight e;
invariant req_implies_not_init: forall (e: event, c: Coordinator) :: e is eVoteReq && c is Init ==> !inflight e;
```

The first two invariants state that, over the run of the system, our assumptions about the `coordinator` and `participants` functions remain satisfied. The next eight invariants ensure that messages target the correct kind of machine. For example, the invariant called `never_req_to_coordinator` says that there is never a vote request message going to a coordinator. These invariants use the special predicate `inflight` which is true iff the argument message has been sent but not received. P also supports a similar predicate called `sent` which is true iff the argument message has been sent.

After adding these invariants, we can re-run the verification engine to get the following output.

```
🎉 Verified 10 invariants!
✅ one_coordinator
✅ participant_set
✅ never_commit_to_coordinator
✅ never_abort_to_coordinator
✅ never_req_to_coordinator
✅ never_yes_to_participant
✅ never_yes_to_init
✅ never_no_to_participant
✅ never_no_to_init
✅ req_implies_not_init
❌ Failed to verify 30 properties!
❓  Failed to verify invariant never_commit_to_coordinator at PSrc/System.p:13:5 
...
❓  Failed to verify invariant never_no_to_participant at PSrc/System.p:40:12 
```

What went wrong? Well, loops are tricky to reason about, so P requires users to provide loop invariants. You can think of these as summaries of what the loop does. P uses these summaries to prove other properties and checks that the loops actually abide by the given summaries. For example, we can add loop invariants to the first broadcast loop that we wrote---the one that sends out vote requests---as follows.

```
foreach (p in participants()) 
    invariant forall new (e: event) :: forall (m: machine) :: e targets m ==> m in participants();
    invariant forall new (e: event) :: e is eVoteReq;
{
    send p, eVoteReq;
}
```

After adding similar loop invariants to all three loops in the model, we can re-run the verification engine to get the following output.

```
🎉 Verified 10 invariants!
✅ one_coordinator
✅ participant_set
✅ never_commit_to_coordinator
✅ never_abort_to_coordinator
✅ never_req_to_coordinator
✅ never_yes_to_participant
✅ never_yes_to_init
✅ never_no_to_participant
✅ never_no_to_init
✅ req_implies_not_init
✅ default P proof obligations
```

Notice that our initial model uses `ignore` statements that we did not describe when introducing the model. If we remove these statements, the verification engine will not be able to prove that no builtin P specification is violated. In some cases, that is because the ignore statements are necessary. For example, it is possible for the coordinator to receive a "yes" or "no" vote when it is in the `Aborted` state. The `ignore` keyword lets us tell the verifier that we have thought of these cases.

## Invariant Groups, Proof Scripts, and Proof Caching

When writing larger proofs, it will become useful to group invariants into lemmas, and then to tell the verifier how to use these lemmas for its proof checking. This helps the user organize their proofs; it helps the verifier construct smaller, more stable queries; and it helps avoid checking the same queries over and over, through caching.

For example, we will write more complex specifications for the 2PC protocol but we will continue to verify that the builtin P specifications are satisfied. Instead of having to execute the verifier over and over to check these builtin properties as we build our larger proofs, we want to cache these results. Futhermore, we don't want any new specifications to interfere with these proof results---unless the model changes there really is no reason to look for a different proof of these same properties.

P allows users to define invariant groups, like the following.

```
Lemma system_config {
    invariant one_coordinator: forall (m: machine) :: m == coordinator() == m is Coordinator;
    invariant participant_set: forall (m: machine) :: m in participants() == m is Participant;
    invariant never_commit_to_coordinator: forall (e: event) :: e is eCommit && e targets coordinator() ==> !inflight e;
    invariant never_abort_to_coordinator: forall (e: event) :: e is eAbort && e targets coordinator() ==> !inflight e;
    invariant never_req_to_coordinator: forall (e: event) :: e is eVoteReq && e targets coordinator() ==> !inflight e;
    invariant never_yes_to_participant: forall (e: event, p: Participant) :: e is eYes && e targets p ==> !inflight e;
    invariant never_yes_to_init: forall (e: event, c: Coordinator) :: e is eYes && e targets c && c is Init ==> !inflight e;
    invariant never_no_to_participant: forall (e: event, p: Participant) :: e is eNo && e targets p ==> !inflight e;
    invariant never_no_to_init: forall (e: event, c: Coordinator) :: e is eNo && e targets c && c is Init ==> !inflight e;
    invariant req_implies_not_init: forall (e: event, c: Coordinator) :: e is eVoteReq && c is Init ==> !inflight e;
}
```

These are all the same invariants we used above but now grouped inside a lemma called `system_config`. We can then use this lemma to decompose our proof using a proof script.

```
Proof {
    prove system_config;
    prove default using system_config;
}
```

This proof script has two steps. First it says that we need to prove that the lemma always holds (that the conjunction of the invariants are inductive for this model). Second we prove that P's default specifications always hold and tell the solver to use the `system_config` lemma to do so. When we run the verification engine again, we will get the following output.

```
🎉 Verified 10 invariants!
✅ system_config: one_coordinator
✅ system_config: participant_set
✅ system_config: never_commit_to_coordinator
✅ system_config: never_abort_to_coordinator
✅ system_config: never_req_to_coordinator
✅ system_config: never_yes_to_participant
✅ system_config: never_yes_to_init
✅ system_config: never_no_to_participant
✅ system_config: never_no_to_init
✅ system_config: req_implies_not_init
✅ default P proof obligations
```

Notice that the first time you run this verification it will take much longer than the second time. That is because the proof is cached and the solver is not executed the second time. If we don't change our model or our lemma, these queries will never be executed again.

## First 2PC Specification and Proof

Proving that the builtin P specifications are satisfied is all well and good, but it isn't exactly the most interesting property. Zhang et al. provide a more exciting specification that we can verify ("2PC-Safety"). Translated into P, their specification looks like the following invariant.

```
invariant safety: forall (p1: Participant) :: p1 is Accepted ==> (forall (p2: Participant) :: preference(p2));
```

This invariant says that if any participant is in the accepted state, then every participant must have wanted to accept the request. Zhang et al. also provide a set of inductive invariants that help prove the safety property above (Fig. 5 in their paper). For our model in P, this set of invariants looks like the following.

```
Lemma kondo {
    invariant a1a: forall (e: eYes) :: inflight e ==> e.source in participants();
    invariant a1b: forall (e: eNo)  :: inflight e ==> e.source in participants();
    invariant a2a: forall (e: eYes) :: inflight e ==> preference(e.source);
    invariant a2b: forall (e: eNo)  :: inflight e ==> !preference(e.source);
    invariant a3b: forall (e: eAbort)  :: inflight e ==> coordinator() is Aborted;
    invariant a3a: forall (e: eCommit) :: inflight e ==> coordinator() is Committed;
    invariant  a4: forall (p: Participant) :: p is Accepted ==> coordinator() is Committed;
    invariant  a5: forall (p: Participant, c: Coordinator) :: p in c.yesVotes ==> preference(p);
    invariant  a6: coordinator() is Committed ==> (forall (p: Participant) :: p in participants() ==> preference(p));
}
```

Given the desired safety property that we want to prove and the set of invariants that helps us prove it, we can write the following proof script in P that checks that the proof by induction passes.

```
Proof {
    prove system_config;
    prove kondo using system_config;
    prove safety using kondo;
    prove default using system_config;
}
```

Running the verification engine on this file will produce the following.

```
🎉 Verified 20 invariants!
✅ safety
✅ system_config: one_coordinator
✅ system_config: participant_set
✅ system_config: never_commit_to_coordinator
✅ system_config: never_abort_to_coordinator
✅ system_config: never_req_to_coordinator
✅ system_config: never_yes_to_participant
✅ system_config: never_yes_to_init
✅ system_config: never_no_to_participant
✅ system_config: never_no_to_init
✅ system_config: req_implies_not_init
✅ kondo: a1a
✅ kondo: a1b
✅ kondo: a2a
✅ kondo: a2b
✅ kondo: a3b
✅ kondo: a3a
✅ kondo: a4
✅ kondo: a5
✅ kondo: a6
✅ default P proof obligations
```

Showing that the verification passes. Notice that if you remove any of the invariants from the lemmas, like say `a5` in `kondo`, the proof will fail with the following output.

```
❌ Failed to verify 1 properties!
❓ Failed to verify invariant kondo: a6 at PSrc/System.p:27:12 
```

## Recap and Next Steps

In this tutorial, we formally verified a simplified 2PC protocol in P. The full, final code for the verification is available [here](https://github.com/p-org/P/blob/experimental/pverifier/Tutorial/Advanced/2_TwoPhaseCommitVerification/Single/PSrc/System.p).

Our proof followed the running example of Zhang et al. but also included the verification of builtin P specifications. Along the way, we introduced the following new P keywords and concepts.

1. `pure` functions;
2. `init-condition` predicates;
3. quantifiers;
4. proofs by induction;
5. `invariant`s;
6. `inflight` and `sent`;
6. loop invariants;
7. `lemma`s as invariant groups;
8. `proof` scripts; and
9. proof caching.

In a future tutorial, we will expand on this simple 2PC protocol by introducing a key-value store and a monitor specification.