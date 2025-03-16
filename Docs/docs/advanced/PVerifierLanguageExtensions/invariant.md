# Invariants

In P formal verification, **invariants** are properties that should always hold true during the execution of a system. They are used to specify and prove correctness properties of P programs.

??? note "P Invariant Declaration Grammar"

    ```
    invariantDecl :
        | invariant [name:] expression;     # P Invariant Declaration
        | invariant forall new (params) :: expression;  # P Loop Invariant
    ```

    `name` is an optional identifier for the invariant, `expression` is a boolean expression that should evaluate to true in all reachable states, and `params` are the parameters in a loop invariant.

**Syntax:** `invariant [name:] expression;` or within loops `invariant forall new (params) :: expression;`

`name` is an optional name for the invariant, and `expression` is a boolean expression that should hold true throughout the execution of the system. For loop invariants, `params` define the scope of the loop invariant.

=== "Basic Invariants"

    ``` java
    // Simple invariant that checks a coordinator property
    invariant one_coordinator: forall (m: machine) :: m == coordinator() == m is Coordinator;
    
    // Invariant to ensure messages are directed to the right machines
    invariant never_req_to_coordinator: forall (e: event) :: e is eVoteReq && e targets coordinator() ==> !inflight e;
    
    // Safety property invariant
    invariant safety: forall (p1: Participant) :: p1 is Accepted ==> (forall (p2: Participant) :: preference(p2));
    ```

=== "Loop Invariants"

    ``` java
    foreach (p in participants()) 
        invariant forall new (e: event) :: forall (m: machine) :: e targets m ==> m in participants();
        invariant forall new (e: event) :: e is eVoteReq;
    {
        send p, eVoteReq;
    }
    ```

=== "Invariant Groups (Lemmas)"

    ``` java
    Lemma system_config {
        invariant one_coordinator: forall (m: machine) :: m == coordinator() == m is Coordinator;
        invariant participant_set: forall (m: machine) :: m in participants() == m is Participant;
        invariant never_commit_to_coordinator: forall (e: event) :: e is eCommit && e targets coordinator() ==> !inflight e;
    }
    
    Proof {
        prove system_config;
        prove safety using system_config;
    }
    ```

## Special Predicates in Invariants

In invariants, P provides special predicates for specifying message state:

- `inflight e`: True if message `e` has been sent but not yet received
- `sent e`: True if message `e` has been sent (regardless of whether it has been received)

## Using Invariants for Verification

Invariants are essential for formal verification in P, allowing you to:

1. Specify correctness properties your system must maintain
2. Create inductive proofs through lemmas and proof scripts
3. Annotate loops with invariants to help the verifier reason about loop behavior
4. Group related invariants into lemmas for modular proofs
