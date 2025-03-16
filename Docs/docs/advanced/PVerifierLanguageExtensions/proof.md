# Proofs

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

## Benefits of Proof Scripts

1. **Decomposition**: Break down complex proofs into manageable parts
2. **Dependency Management**: Explicitly state which lemmas support other lemmas
3. **Performance**: Reduce verification time by focusing the solver on relevant invariants
4. **Caching**: Results are cached per lemma, avoiding redundant verification across runs
5. **Readability**: Make the verification structure more readable and maintainable