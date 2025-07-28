P test cases define different finite scenarios under which we would like to check the correctness of our system. Each test case is automatically discharged by the P Checker.

??? note "P Test Cases Grammar"
    ```
    testcase
    | test iden [main=iden] : modExpr ;                                           # TestDecl
    | test param (paramList) iden [main=iden] : modExpr ;                         # ParamTestDecl
    | test param (paramList) assume (expr) iden [main=iden] : modExpr ;           # AssumeTestDecl
    | test param (paramList) (num wise) iden [main=iden] : modExpr;               # TWiseTestDecl
    ;

    paramList
    | iden in [valueList]                                                         # SingleParam
    | iden in [valueList], paramList                                              # MultiParam
    ;

    valueList
    | value                                                                       # SingleValue
    | value, valueList                                                            # MultiValue
    ;

    value
    | num                                                                         # NumberValue
    | bool                                                                        # BoolValue
    ;
    ```
    
    `modExpr` represent the P module defined using the module expressions described in [P Module System](modulesystem.md)

### Basic Test Case

A basic test case checks the correctness of a module under a specific scenario.

**Syntax**: `test tName [main=mName] : module_under_test ;`

- `tName` is the name of the test case
- `mName` is the name of the **main** machine where execution starts
- `module_under_test` is the module to be tested

=== "Basic Test"
    ```
    test tcSingleClient [main=TestWithSingleClient]:
      assert BankBalanceIsAlwaysCorrect in
      (union Client, Bank, { TestWithSingleClient });
    ```

### Parameterized Test Cases

Parameterized tests allow systematic exploration of different system configurations. Before using parameters in test cases, they must be declared as global variables with their types.

**Parameter Declaration Syntax**: `param name : type ;`

For example:
```
param nClients : int;   // For numeric parameters
param b1 : bool;        // For boolean parameters
param g1 : int;         // For another numeric parameter
```

=== "Basic Parameter Test"
    ```
    test param (nClients in [2, 3, 4]) tcTest [main=TestWithConfig]:
      assert BankBalanceIsAlwaysCorrect in
      (union Client, Bank, { TestWithConfig });
    ```

=== "Multiple Parameters"
    ```
    test param (nClients in [2, 3, 4], g1 in [1, 2], g2 in [4, 5]) tcTest [main=TestWithConfig]:
      assert BankBalanceIsAlwaysCorrect in
      (union Client, Bank, { TestWithConfig });
    ```

=== "With Assumption"
    ```
    test param (nClients in [2, 3, 4], g1 in [1, 2], g2 in [4, 5]) 
      assume (nClients + g1 < g2) tcTest [main=TestWithConfig]:
      assert BankBalanceIsAlwaysCorrect in
      (union Client, Bank, { TestWithConfig });
    ```

=== "N-wise Testing"
    ```
    test param (nClients in [2, 3, 4], g1 in [1, 2], g2 in [4, 5], b1 in [true, false])
      (2 wise) tcTest [main=TestWithConfig]:
      assert BankBalanceIsAlwaysCorrect in
      (union Client, Bank, { TestWithConfig });
    ```

!!! info "Properties Checked"
    For each test case, the P checker asserts:
    1. No `unhandled event` exceptions
    2. All local assertions hold
    3. No deadlocks
    4. All [specification monitors](modulesystem.md#assert-monitors-module) properties hold
