A P program is a collection of concurrently executing state machines that communicate with each other by sending events (or messages) asynchronously.

!!! info "P State Machine Semantics"
    The underlying model of computation is similar to that of [Gul Agha's](http://osl.cs.illinois.edu/members/agha.html) [Actor-model-of-computation](https://dspace.mit.edu/handle/1721.1/6952) ([wiki](https://en.wikipedia.org/wiki/Actor_model)). Here is a summary of important semantic details:

    - Each P state machine has an **unbounded FIFO buffer** associated with it.
    - Sends are **asynchronous**, i.e., executing a send operation `send t,e,v;` adds event `e` with payload value `v` into the FIFO buffer of the target machine `t`.
    - Variables and functions declared within a machine are **local**, i.e., they are accessible only from within that machine.
    - Each state in a machine has an **entry** and an **exit** function associated with it. The entry function gets executed when the machine enters that state, and similarly, the exit function gets executed when the machine exits that state on an outgoing transition.
    - After executing the entry function, a machine tries to **dequeue an event** from its input buffer or **blocks** if the buffer is empty. Upon dequeuing an event from its input queue, a machine executes the attached event handler which might transition the machine to a different state.

    For detailed formal semantics of P state machines, we refer the readers to the [original P paper](https://ankushdesai.github.io/assets/papers/p.pdf) and the [more recent paper](https://ankushdesai.github.io/assets/papers/modp.pdf) with updated semantics.


??? note "P State Machine Grammar"

    ```
    # State Machine in P
    machineDecl : machine name machineBody

    # State Machine Body
    machineBody : LBRACE machineEntry* RBRACE;
    machineEntry
      | varDecl
      | funDecl
      | stateDecl
      ;

    # Variable Decl
    varDecl : var iden : type ;

    # State Declaration in P
    stateDecl : start? (hot | cold)? state name { stateBody* }

    # State Body
    stateBody:
      | entry anonFunction                # StateEntryFunAnon
      | entry funName ;                   # StateEntryFunNamed
      | exit noParamAnonFunction          # StateExitFunAnon
      | exit funName;                     # StateExitFunNamed

      ## Transition or Event Handlers in each state
      | defer eventList ;                               # StateDeferHandler
      | ignore eventList ;                              # StateIgnoreHandler
      | on eventList do anonFunction                    # OnEventDoNamedHandler
      | on eventList do funName ;                       # OnEventDoAnonHandler
      | on eventList goto stateName ;                   # OnEventGotoState
      | on eventList goto stateName with anonFunction   # OnEventGotoStateWithAnonHandler
      | on eventList goto stateName with funName ;      # OnEventGotoStateWithNamedHandler
      ;
    ```

### Variables
A machine can define a set of local variables that are accessible only from within that machine.

**Syntax**: `var iden : type ;`

`iden` is the name of the variable and `type` is the variable datatype. To know more about different datatypes supported, please checkout [P DataTypes](datatypes.md).

### Functions
A machine can have a set of local named functions that are accessible only from within that machine. Please checkout [Named Functions](functions.md#named-functions) for a detailed description.

### State
A state can be declared in a machine with a `name` and a `stateBody`.

**Syntax**: `start? (hot | cold)? state name { stateBody* }`

A single state in each machine should be marked as the `start` state to identify this state as the starting state on machine creation. Additionally, a state declared inside a [P Monitor](monitors.md) can also be marked as `hot` or `cold`.

### State Body
The state body for a state defines its entry/exit functions and attached event handlers supported by that state. Additionally, the state body can mark certain events as deferred or ignored in the state.

#### Entry Function
The entry function gets executed when a machine enters that state. If the corresponding state is marked as the `start` state, the entry function gets executed at machine creation.

**Syntax**: `entry (anonFunction | funName ;)`

=== "Entry function"

    ``` kotlin
    entry {
        print format ("Entering state");
    }
    ```

=== "Entry function with input parameters"

    ``` kotlin
    entry (payload : (id : int, msg : string)) {
        print format ("Entering state with id {0} and message {1}", payload.id, payload.msg);
    }
    ```

Defining the entry function for a state is optional. By default, the entry function is defined as a no-op function, i.e., `{ // nothing }`.

#### Exit Function
The exit function gets executed when a machine exits that state to transition to another state.

**Syntax**: `exit (noParamAnonFunction | funName ;)`

=== "Exit function"

    ``` kotlin
    exit {
        print format ("Exiting state");
    }
    ```

Defining the exit function for a state is optional. By default, the exit function is defined as a no-op function, i.e., `{ // nothing }`.

#### Event Handler
An event handler defined for event `E` in state `S` describes what statements are executed when a machine dequeues event `E` in state `S`.

**Syntax**: `on eventList do (anonFunction | funName ;)`

=== "Event handler"

    ``` kotlin
    on eWarmUpReq do {
        send controller, eWarmUpCompleted;
    }
    ```

=== "Event handler with input parameters"

    ``` kotlin
    on eWithDrawReq do (req: tWithDrawReq) {
      assert req.accountId in bankBalance,
        format ("Unknown accountId {0} in the withdraw request. Valid accountIds = {1}",
          req.accountId, keys(bankBalance));
      pendingWithDraws[req.rId] = req;
    }
    ```

#### Event Handler with Goto
An event handler can be further combined with a [Goto](statements.md#goto) statement. After the attached event handler statements are executed, the machine transitions to the `goto` state.

**Syntax**: `on eventList goto stateName (; | with anonFunction | with funName ;)`

=== "Event handler with goto only"

    ``` kotlin
    on eWarmUpCompleted goto CoffeeMakerReady;
    ```

=== "Event handler with goto"

    ``` kotlin
    on eTimeOut goto WaitForTransactions with { DoGlobalAbort(TIMEOUT); }
    ```


=== "Event handler with goto and input parameters"

    ``` kotlin
    on eSpec_BankBalanceIsAlwaysCorrect_Init goto WaitForWithDrawReqAndResp with (balance: map[int, int]) {
      bankBalance = balance;
    }
    ```

#### Defer
An event can be deferred in a state. Defer basically defers the dequeue of the event `E` in state `S` until the machine transitions to a non-deferred state, i.e., a state that does not defer the event `E`. The position of the event `E` that is deferred in state `S` does not change the FIFO buffer of the machine.

**Syntax**: `defer eventList ;`

!!! warning "Dequeuing of an event"
    Whenever a machine encounters a dequeue event, the machine goes over its unbounded FIFO buffer from the front and removes the first event that is not deferred in its current state, keeping rest of the buffer unchanged.

#### Ignore
An event `E` can be ignored in a state, which basically drops the event `E`.

**Syntax**: `ignore eventList ;`

Think of ignore as a short hand for `on E do { // nothing };`.
