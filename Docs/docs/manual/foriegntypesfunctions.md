
!!! note "Foreign Types"
    Foreign types in P are types that are declared in P but defined or implemented in the foreign language ([foreign type](datatypes.md#foreign)).

!!! note "Foreign Functions"
    Foreign functions in P are local or global functions that are declared in P but defined or implemented in the foreign language ([foreign functions](functions.md#foreign-functions)).


!!! hint "Recommendation: Using Foreign Types and Functions"
    Programmers can consider implementing a particular type as Foreign type in P if its a complicated data structure and implementing them using P types is either not possible or too cumbersome. For example, P does not support declaring recursive data types, and hence, implementing a linked-list or a tree like data-structure in P is hard. Hence, we recommend programmers to implement such types as foreign types.

    When modeling complex systems, many times programmers need to implement complicated logic/functions that manipulate or iterate over data-structures. Such functions can be easily implemented in foreign languages like Java and C# as compared to P. For example, iterating over collections and manipulating them is easier in Java and C#, and verbose in P as P only supports `while` loops and does not support iterators like `foreach`. Also, functions that manipulate foreign types are implemented as foreign functions.   

The [Two Phase Commit](../tutorial/twophasecommit.md) example had introduced the foreign function feature. We will now use a simple PriorityQueue example to go into the details of foreign interface in P.



### [PriorityQueue](https://github.com/p-org/P/tree/master/Tutorial/PriorityQueue)

The PriorityQueue [project](https://github.com/p-org/P/blob/master/Tutorial/PriorityQueue/PriorityQueue.pproj) presents an example where a `Client` uses a priority queue implemented as foreign types and functions.

#### P Source

The [PriorityQueue.p](https://github.com/p-org/P/blob/master/Tutorial/PriorityQueue/PSrc/PriorityQueue.p) file declares (1) a foreign type [`tPriorityQueue`](https://github.com/p-org/P/blob/master/Tutorial/PriorityQueue/PSrc/PriorityQueue.p#L3) and uses (2) [global foreign functions](https://github.com/p-org/P/blob/master/Tutorial/PriorityQueue/PSrc/PriorityQueue.p#L7-L22) to operate or interact with the priority queue.

!!! danger "P has Value Semantics (no Pass by Reference!!)"
    Note that P does not support pass by references, everything in P is always pass by value. Hence the functions above must return a priority queue after adding the element into the queue (mutated queue).



#### P Test

The [Client.p](https://github.com/p-org/P/blob/master/Tutorial/PriorityQueue/PTst/Client.p) file presents a [`Client`](https://github.com/p-org/P/blob/master/Tutorial/PriorityQueue/PTst/Client.p#L5) state machine that uses the priority queue and performs operations on it by calling the foreign function. The `Client` machine also declares a [local foreign function](https://github.com/p-org/P/blob/master/Tutorial/PriorityQueue/PTst/Client.p#L46-L47) to add an element into the queue.

#### P Foreign Code

The implementation of the `tPriorityQueue` is available in [PriorityQueue.cs](https://github.com/p-org/P/blob/master/Tutorial/PriorityQueue/PForeign/PriorityQueue.cs)

!!! note ""
    If you want to pretty print the foreign type value, you can also override the ToString() function in their implementation.

The implementation of the global functions in PriorityQueue.p is available in [PriorityQueueFunctions.cs](https://github.com/p-org/P/blob/master/Tutorial/PriorityQueue/PForeign/PriorityQueueFunctions.cs).
Finally, the implementation of the local function in the Client machine is available in [ClientFunctions.cs](https://github.com/p-org/P/blob/master/Tutorial/PriorityQueue/PForeign/ClientFunctions.cs).

#### Compiling PriorityQueue project

Run the following command:

``` shell
cd P/Tutorial/PriorityQueue
pc -proj:PriorityQueue.pproj
```

??? note "Expected Output"
    ```
    ----------------------------------------
    ==== Loading project file: PriorityQueue.pproj
    ....... includes p file: P/Tutorial/PriorityQueue/PSrc/PriorityQueue.p
    ....... includes p file: P/Tutorial/PriorityQueue/PTst/Client.p
    ....... includes p file: P/Tutorial/PriorityQueue/PTst/TestScripts.p
    ----------------------------------------
    ----------------------------------------
    Parsing ..
    Type checking ...
    Code generation ....
    Generated PriorityQueue.cs
    ----------------------------------------
    Compiling PriorityQueue.csproj ..

    Microsoft (R) Build Engine version 16.10.2+857e5a733 for .NET
    Copyright (C) Microsoft Corporation. All rights reserved.

      Determining projects to restore...
      Restored P/Tutorial/PriorityQueue/PriorityQueue.csproj (in 757 ms).
      PriorityQueue -> P/Tutorial/PriorityQueue/POutput/netcoreapp3.1/PriorityQueue.dll

    Build succeeded.
        0 Warning(s)
        0 Error(s)
    ```

#### Running PriorityQueue test case

Run the following command to run the test case [`tcCheckPriorityQueue`](https://github.com/p-org/P/blob/master/Tutorial/PriorityQueue/PTst/TestScripts.p#L2):

```
pmc <path>/PriorityQueue.dll -v
```

??? note "Expected Output"
    ```
    . Testing P/Tutorial/PriorityQueue/POutput/netcoreapp3.1/PriorityQueue.dll
    Starting TestingProcessScheduler in process 49983
    ... Created '1' testing task.
    ... Task 0 is using 'random' strategy (seed:3421113095).
    ..... Iteration #1
    <TestLog> Running test 'PImplementation.tcCheckPriorityQueue.Execute'.
    <CreateLog> Plang.CSharpRuntime._GodMachine(1) was created by task '2'.
    <CreateLog> PImplementation.Client(2) was created by Plang.CSharpRuntime._GodMachine(1).
    <StateLog> PImplementation.Client(2) enters state 'Init'.
    <PrintLog> Creating Priority Queue!
    <PrintLog> Adding Element in the Priority Queue!
    <PrintLog> Adding Element in the Priority Queue!
    <PrintLog> Adding Element in the Priority Queue!
    <PrintLog> Choosing element at location: 1
    <PrintLog> --------------
    <PrintLog> Hello
    <PrintLog> World
    <PrintLog> !!
    <PrintLog> 123
    <PrintLog> --------------

    ```

Hope you exploit the P Foreign interface to implement and test complex systems. If you have any questions, please feel free to post them in the discussions or issues.
