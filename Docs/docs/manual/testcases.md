P Test cases are used to define different finite scenarios under which we would like to
check the correctness of our system.

???+ note "P Test Cases Grammar"

    ```
    testcase
    | test iden [main=iden] : modExpr ;                  # TestDecl
    | test iden [main=iden] : modExpr refines modExpr;   # RefinementTestDecl
    ;
    ```
    `modExpr` represent the P modules defined using the module expressions described in [P Module System](modulesystem.md)

### Test Declaration


### Refinement Test Declaration

