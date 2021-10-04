There are two types of functions in P, anonymous functions and named functions. Anonymous functions are unnamed functions that can appear as an entry or exit functions or as event-handlers. Named function in P can either be declared inside a state machine or globally as a top-level declaration. The named functions declared within a state machine are local to that machine and hence can access the local-variables of the machine. Global named functions are shared across state machines. Note that the purpose of having named functions is to enable code reuse, named functions can be used as entry/exit functions and also as event handlers. You can look at the [P State machine](manual/../statemachines.md) to get more details about the declaration of these functions.

??? note "P Functions Grammar"

    ```
      anonEventHandler : (funParam?) functionBody                     # AnonymousFunDecl

      namedfunDecl : 
          | fun name (funParamList?) (: returnType)? ;                # ForeignFunDecl
          | fun name (funParamList?) (: returnType)? functionBody     # FunDecl
          ;

      functionBody : { varDecl* statement* }                          # FunctionBody

      varDecl : var iden : type; ;                                    # VariableDecl

      funParamList : funParam (, funParam)* ;                         # FunParameterList
      funParam : iden : type                                          # FunParameter
    ```

### Anonymous Functions

### Named Functions

### Foreign Functions
P function declarations without any function body are referred to as foreign functions. Foreign functions are functions that are declared in P and can be used in the P program just like any other function but the implementation of these functions is provided in the foreign native language. 

**Syntax**: `fun name (funParamList?) (: returnType)? functionBody`

### Function Body

Body of any function in P is similar to any other programming language which a restriction that P uses C style local variable declarations.
Declaration of local variables in a function must come before any other statement in the function body. So, the body of a function in P is a sequence of variable declarations followed by a sequence of [statements](statements.md).

```
functionBody : { varDecl* statement* }                          
```
