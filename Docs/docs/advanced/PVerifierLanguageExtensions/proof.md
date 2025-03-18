# Lemmas and Proof Scripts

Lemmas and proof scripts go hand in hand in the P Verifier. Lemmas allow you to decompose specifications and proof scripts allow you to relate lemmas to write larger proofs.

## Lemmas

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
        invariant one_coordinator: forall (m: machine) :: m == coordinator() <==> m is Coordinator;
        invariant participant_set: forall (m: machine) :: m in participants() <==> m is Participant;
        invariant never_commit_to_coordinator: forall (e: event) :: e is eCommit && e targets coordinator() ==> !inflight e;
        // More invariants...
    }
    ```

## Proofs

In P's verification framework, **proofs** provide a way to structure verification tasks by specifying what to verify and which lemmas to use. Proof scripts help decompose complex verification problems into smaller, more manageable parts and enable caching of intermediate results.

??? note "P Proof Declaration Grammar"

    ```
    proofDecl :
        | Proof { proofStmtList }    # P Proof Declaration
    
    proofStmtList :
        | proofStmt;
        | proofStmtList proofStmt;
    
    proofStmt :
        | prove iden;                # Prove a lemma or invariant
        | prove iden using iden;     # Prove using another lemma
    ```

    `iden` is the name of a lemma or invariant that should be verified.

**Syntax:** `Proof { prove target1; prove target2 using helper; ... }`

Where `targetN` are the names of lemmas or invariants to verify, and `helper` is an optional lemma to use during verification.

=== "Basic Proof"

    ```java
    Proof {
        prove system_config;         // Verify system_config lemma
        prove default using system_config;  // Verify default P proof obligations using system_config
    }
    ```

=== "Complex Proof With Dependencies"

    ```java
    Proof {
        prove system_config;         // First prove the system configuration lemma
        prove kondo using system_config;  // Use system_config to prove kondo
        prove safety using kondo;    // Use kondo to prove the safety property
        prove default using system_config;  // Verify default P obligations
    }
    ```

=== "Using Default Keyword"

    ```java
    Proof {
        prove lemma1;
        prove lemma2;
        // The special keyword "default" refers to P's built-in specifications
        prove default using lemma1, lemma2;  // Verify using both lemmas
    }
    ```

### Benefits of Proof Scripts

1. **Organization**: Break down complex proofs into manageable parts
2. **Verification Stability**: They enable the verifier to construct smaller, more focused queries
3. **Caching**: Results are cached per proof step, avoiding redundant verification across runs