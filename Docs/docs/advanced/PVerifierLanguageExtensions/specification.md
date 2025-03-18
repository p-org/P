# Specifications

The P verifier adds three kinds of specifications: global invariants, loop invariants, and function contracts. We cover each type in its own subsection below.

## Global Invariants

Global invariants are properties that should hold true throughout the execution of the system.

**Syntax:** `invariant [name:] expression;`

`name` is an optional name for the invariant, and `expression` is a boolean expression that should hold true in all reachable states.

??? note "Grammar"
    ```
    invariant [name:] expression;    # Global Invariant Declaration
    ```

=== "Basic Invariants"

    ``` java
    // Simple invariant that checks a coordinator property
    invariant one_coordinator: forall (m: machine) :: m == coordinator() <==> m is Coordinator;
    
    // Invariant to ensure messages are directed to the right machines
    invariant never_req_to_coordinator: forall (e: event) :: e is eVoteReq && e targets coordinator() ==> !inflight e;
    
    // Safety property invariant
    invariant safety: forall (p1: Participant) :: p1 is Accepted ==> (forall (p2: Participant) :: preference(p2));
    ```

## Loop Invariants

Loop invariants are properties that should hold true during the execution of loops.

**Syntax:** `invariant expression;`

`expression` is a boolean expression that should hold true throughout the execution of the loop.

??? note "Grammar"
    ```
    invariant expression;    # Loop Invariant
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

## Function Contracts

Function contracts specify preconditions, postconditions, and return value names for functions.

**Syntax:** `requires expression;` and `ensures expression;` and `return (name: type);`

`requires` specifies a precondition that must be true before the function is executed, `ensures` specifies a postcondition that must be true after the function is executed, and `return` binds the return value to name that you can write pre- and postconditions over.

??? note "Grammar"
    ```
    requires expression;    # Function Precondition
    ensures expression;     # Function Postcondition
    return (name: type);    # Function Return Binding
    ```

=== "Function Contracts"

    ``` java
    fun RandomParticipant(s: set[machine]) 
        return (x: machine);
        ensures x in s;
        requires NotEmpty(s); // where NotEmpty is a helper function
    ```

## Special Predicates

P provides special predicates for specifying message state:

- `inflight e`: True if message `e` has been sent but not yet received
- `sent e`: True if message `e` has been sent (regardless of whether it has been received)

### Quantifiers

P supports several quantifier expressions for specifying properties over collections:

- `forall (x: type) :: expression`: True if the expression is true for all instances of the specified type
- `exists (x: type) :: expression`: True if the expression is true for at least one instance of the specified type
- `forall new (x: type) :: expression`: Similar to `forall`, but specifically quantifies over newly sent events in the body of a loop. Can only be used in loop invariants.

=== "Quantifier Examples"

    ``` java
    // Universal quantification
    invariant all_participants_ready: forall (p: Participant) :: p.status == READY;
    
    // Existential quantification
    invariant leader_exists: exists (p: Participant) :: p.isLeader;
    
    // Quantifying over new events in a loop
    foreach (p in participants()) 
        invariant forall new (e: event) :: e is eVoteReq;
    {
        send p, eVoteReq;
    }
    ```