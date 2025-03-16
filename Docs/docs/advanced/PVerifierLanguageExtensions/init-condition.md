# Initialization Conditions

Initialization conditions let us constrain the kinds of systems that we consider for formal verification. You can think of these as constraints that P test harnesses have to satisfy to be considered valid.

??? note "P Init Condition Declaration Grammar"

    ```
    initConditionDecl :
        | init-condition expression;     # P Init Condition Declaration
    ```

    `expression` is a boolean expression that should evaluate to true at initialization time.

**Syntax:** `init-condition expression;`

`expression` is a boolean expression that must be satisfied for the system to be considered valid. This is typically used with quantifiers to express constraints over sets of machines or values.

=== "Init Condition Examples"

    ``` java
    // Ensures that there's a unique machine of type coordinator
    init-condition forall (m: machine) :: m == coordinator() == m is Coordinator;
    
    // Ensures that every machine in the participants set is a machine of type participant
    init-condition forall (m: machine) :: m in participants() == m is Participant;
    
    // Ensures that all yesVotes tallies start empty
    init-condition forall (c: Coordinator) :: c.yesVotes == default(set[machine]);
    ```
