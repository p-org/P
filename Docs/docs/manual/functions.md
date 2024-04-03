There are two types of functions in P: (1) anonymous functions, and (2) named functions.

- **Anonymous functions** are unnamed functions that can appear as an entry functions or exit functions or as event-handlers.
- **Named functions** can either be declared inside a state machine or globally as a top-level declaration. The named functions declared within a state machine are local to that machine and hence can access the local-variables of the machine. Global named functions are shared across state machines. Note that the purpose of having named functions is to enable code reuse. Named functions can be also be used as entry/exit functions and also as event handlers.

Please look at [P state machine](manual/../statemachines.md) for more details about the declaration of these functions.

??? note "P Functions Grammar"

    ```
      anonFunction : (funParam?) functionBody                         # AnonymousFunDecl

      funDecl :
        | fun name (funParamList?) (: returnType)? ;                  # ForeignFunDecl
        | fun name (funParamList?) (: returnType)? functionBody       # FunDecl
        ;

      functionBody : { varDecl* statement* }                          # FunctionBody

      varDecl : var iden : type; ;                                    # VariableDecl

      funParamList : funParam (, funParam)* ;                         # FunParameterList
      funParam : iden : type                                          # FunParameter
    ```

### Anonymous Functions
Anonymous functions are unnamed functions that appear as entry or exit or event-handler functions.

**Syntax**: `(funParam?) functionBody`

`funParam` is the optional **single** parameter allowed with the annonymous functions. To know more about the places annonymous functions can be used, please checkout the grammar of [p state machines](statemachines.md) and [receive statements](statements.md#receive).
### Named Functions
Named functions in P can be declared within P state machines as local functions that can access the local variables of the state machine. Named functions that are declared as top-level declarations are global functions that can be used across state machines.

Check out the grammar of [P state machines](statemachines.md) for local functions declarations and [top-level declarations](../manualoutline.md) grammar for the global function declarations.

Example of a local functions: [here](https://github.com/p-org/P/blob/master/Tutorial/4_FailureDetector/PSrc/FailureDetector.p#L118) and an example of a global function that is shared across all machines: [here](https://github.com/p-org/P/blob/master/Tutorial/Common/FailureInjector/PSrc/NetworkFunctions.p)

**Syntax**: `fun name (funParamList?) (: returnType)? functionBody`

`name` is the name of the named function, `funParamList` is the optional function parameters, and `returnType` is the optional return type of the function.

#### Foreign Functions

P named function declarations without any function body are referred to as foreign functions. Foreign functions are functions that are declared in P and can be used in the P program just like any other function but the implementation of these functions is provided in the foreign language.

**Syntax**: `fun name (funParamList?) (: returnType)? ;`

`name` is the name of the named function, `funParamList` is the optional function parameters, and `returnType` is the optional return type of the function.

To know more about the foreign interface and functions, please look at the [PriorityQueue example](https://p-org.github.io/P/manual/foriegntypesfunctions/).

### Function Body

Function body in P is similar to other programming languages with a restriction that P function-local variables uses C style local variable declarations.
Declaration of local variables in a function must come before any other statement in the function body. So, the body of a function in P is a sequence of variable declarations followed by a sequence of [statements](statements.md).

```
functionBody : { varDecl* statement* }
```
