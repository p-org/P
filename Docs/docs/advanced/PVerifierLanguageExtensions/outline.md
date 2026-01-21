!!! tip ""
    **We recommend that you start with the [Tutorials](../../tutsoutline.md) to get familiar with
    the P language and its tool chain.**

??? note "PVerifier Extension Top Level Declarations Grammar"

    ```
    topDecl:                # Top-level P Program Declarations
    | pureFunDecl           # PureFunctionDeclaration
    | initCondDecl          # InitConditionPredicateDeclaration
    | invariantDecl         # InvariantDeclaration
    | lemmaDecl             # LemmaDeclaration
    | proofScript           # ProofScript
    ;
    ```

A PVerifier program consists P top-level declarations along with the following:

| Top Level Declarations                       | Description                                                                                                                             |
| :------------------------------------------- | :-------------------------------------------------------------------------------------------------------------------------------------- |
| [Pure Functions](pure.md)                    | P supports declaring pure functions that do not have side effects                                                                               |
| [Init Conditions](init-condition.md)         | P supports declaring initial condition predicates                                                                                               |
| [Invariants](specification.md)               | P supports declaring invariants that must hold true for the system                                                                              |
| [Lemmas and Proofs](proof.md)                | P supports declaring lemmas and proof scripts to verify the correctness of the system                                                           |
