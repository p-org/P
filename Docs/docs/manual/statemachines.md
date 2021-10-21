The underlying model of computation for P state machines is similar to that of [Gul Agha's](http://osl.cs.illinois.edu/members/agha.html) [Actor-model-of-computation](https://dspace.mit.edu/handle/1721.1/6952) ([wiki](https://en.wikipedia.org/wiki/Actor_model)). A P program is a collection of concurrently executing state machines that communicate with eachother by sending events (or messages) asynchronously. Each P state machine has an **unbounded FIFO buffer** associated with it. Sends are **asynchronous**, i.e., executing a send operation `send t,e,v;` adds event `e` with payload value `v` into the FIFO buffer of the target machine `t`. 
Each state in the P state machine has an entry and an exit function associated with it, entry function get executed when the state machine enters that state and similarly, the exit function gets executed when the machine exits that sate on an out going transition. After executing the entry function, the machine tries to dequeue an event from the input buffer or blocks if the buffer is empty. Upon dequeuing an event from the input queue of the machine, the attached handler is executed which might transition the machine to a different state.

For detailed formal semantics of P state machines, we refer the readers to the [original P paper](https://ankushdesai.github.io/assets/papers/p.pdf) and the [more recent paper](https://ankushdesai.github.io/assets/papers/modp.pdf) with updated semantics.


???+ note "P State Machine Grammar"

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
    varDecl : var iden : type; ;                                    

    # State Declaration in P
    stateDecl : start? (hot | cold)? state name { stateBodyItem* }

    # State Body
    stateBody:
      | entry anonFunction                # StateEntryFunAnon
      | entry funName ;                   # StateEntryFunNamed
      | exit noParamAnonFunction          # StateExitFunAnon
      | exit funName;                     # StateExitFunNamed

      ## Transition or Event Handlers in each state
      | defer eventList ;                               # StateDeferHandler
      | ignore eventList ;                              # StateIgnoreHandler
      | on eventList do funName ;                       # OnEventDoAnonHandler
      | on eventList do anonFunction                    # OnEventDoNamedHandler
      | on eventList goto stateName ;                   # OnEventGotoState
      | on eventList goto stateName with anonFunction   # OnEventGotoStateWithAnonHandler
      | on eventList goto stateName with funName ;      # OnEventGotoStateWithNamedHandler
      ;
    ```