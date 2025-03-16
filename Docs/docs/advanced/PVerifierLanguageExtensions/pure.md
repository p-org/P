??? note "P Pure Function Declaration Grammar"

    ```
    pureFunctionDecl :
        | pure iden (params)? : type;     # P Pure Function Declaration
    ```

    `iden` is the name of the pure function, `params` are the parameters of the function, and `type` is the return type of the function.

**Syntax:** `pure functionName();` or `pure functionName(param1: type1, param2: type2) : returnType;`

`functionName` is the name of the P pure function, `param1`, `param2`, etc. are the parameters of the function, and `returnType` is the type of the value returned by the function.

=== "Pure Function Declarations"

    ``` java
    // declaration of pure functions with no parameters
    pure participants(): set[machine];
    pure coordinator(): machine;

    // declaration of pure functions with parameters
    pure preference(m: machine) : bool;
    ```
