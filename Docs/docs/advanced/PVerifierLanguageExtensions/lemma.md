# Lemmas

Lemmas in P allow you to group related invariants together, which helps organize complex proofs, create smaller and more stable verification queries, and enable proof caching.

??? note "P Lemma Declaration Grammar"

    ```
    lemmaDecl :
        | Lemma iden { invariantsList }    # P Lemma Declaration
    
    invariantsList :
        | invariant iden: expression;
        | invariantsList invariant iden: expression;
    ```

    `iden` is the name of the lemma or invariant, and `expression` is a boolean expression that should hold throughout system execution.

**Syntax:** `Lemma lemmaName { invariant invName1: expr1; invariant invName2: expr2; ... }`

`lemmaName` is the name of the lemma group, `invNameX` are the names of individual invariants, and `exprX` are the boolean expressions that should hold.

=== "Lemma Declaration"

    ```java
    Lemma system_config {
        invariant one_coordinator: forall (m: machine) :: m == coordinator() == m is Coordinator;
        invariant participant_set: forall (m: machine) :: m in participants() == m is Participant;
        invariant never_commit_to_coordinator: forall (e: event) :: e is eCommit && e targets coordinator() ==> !inflight e;
        // More invariants...
    }
    ```

=== "Using Lemmas in Proofs"

    ```java
    Proof {
        prove system_config;
        prove default using system_config;
    }
    ```

=== "Complex Proof Structure"

    ```java
    Lemma kondo {
        invariant a1a: forall (e: eYes) :: inflight e ==> e.source in participants();
        invariant a1b: forall (e: eNo)  :: inflight e ==> e.source in participants();
        // More invariants...
    }

    invariant safety: forall (p1: Participant) :: p1 is Accepted ==> (forall (p2: Participant) :: preference(p2));

    Proof {
        prove system_config;
        prove kondo using system_config;
        prove safety using kondo;
        prove default using system_config;
    }
    ```

Grouping invariants into lemmas provides several benefits:

1. **Organization**: Lemmas help structure complex proofs by grouping related invariants
2. **Verification Stability**: They enable the verifier to construct smaller, more focused queries
3. **Caching**: Proof results are cached per lemma, avoiding redundant verification across multiple runs
4. **Proof Decomposition**: Lemmas can build on each other in proof scripts, making large proofs more manageable