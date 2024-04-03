
!!! note "Foreign Types"
    Foreign types in P are types that are declared in P but defined or implemented in the foreign language ([foreign type](datatypes.md#foreign)).

!!! note "Foreign Functions"
    Foreign functions in P are local or global functions that are declared in P but defined or implemented in the foreign language ([foreign functions](functions.md#foreign-functions)).


!!! hint "Recommendation: Using Foreign Types and Functions"
    Programmers can consider implementing a particular type as Foreign type in P if it's a complicated data structure and implementing them using P types is either not possible or too cumbersome. For example, P does not support declaring recursive data types, and hence, implementing a linked-list or a tree like data-structure in P is hard. Hence, we recommend programmers to implement such types as foreign types.

    When modeling complex systems, many times programmers need to implement complicated logic/functions that manipulate or iterate over data-structures. Such functions can be easily implemented in foreign languages like Java and C# as compared to P. For example, sorting a list is easier in Java and C# through standard library functions and verbose in P. Also, functions that manipulate foreign types are implemented as foreign functions.

The [Two Phase Commit](../tutorial/twophasecommit.md) example had introduced the foreign function feature. We will now use a simple PriorityQueue example to go into the details of foreign interface in P.



### [PriorityQueue](https://github.com/p-org/P/tree/master/Tutorial/PriorityQueue)

The PriorityQueue [project](https://github.com/p-org/P/blob/master/Tutorial/PriorityQueue/PriorityQueue.pproj) presents an example where a `Client` uses a priority queue implemented as foreign types and functions.

#### P Source

The [PriorityQueue.p](https://github.com/p-org/P/blob/master/Tutorial/PriorityQueue/PSrc/PriorityQueue.p) file declares (1) a foreign type [`tPriorityQueue`](https://github.com/p-org/P/blob/master/Tutorial/PriorityQueue/PSrc/PriorityQueue.p#L2) and uses (2) [global foreign functions](https://github.com/p-org/P/blob/master/Tutorial/PriorityQueue/PSrc/PriorityQueue.p#L7-L22) to operate or interact with the priority queue.

!!! danger "P has Value Semantics (no Pass by Reference!!)"
    Note that P does not support pass by references, everything in P is always pass by value. Hence, the functions above must return a priority queue after adding the element into the queue (mutated queue).



#### P Test

The [Client.p](https://github.com/p-org/P/blob/master/Tutorial/PriorityQueue/PTst/Client.p) file presents a [`Client`](https://github.com/p-org/P/blob/master/Tutorial/PriorityQueue/PTst/Client.p#L5) state machine that uses the priority queue and performs operations on it by calling the foreign function. The `Client` machine also declares a [local foreign function](https://github.com/p-org/P/blob/master/Tutorial/PriorityQueue/PTst/Client.p#L46-L47) to add an element into the queue.

#### P Foreign Code

The implementation of the `tPriorityQueue` is available in [PriorityQueue.cs](https://github.com/p-org/P/blob/master/Tutorial/PriorityQueue/PForeign/PriorityQueue.cs)

!!! note ""
    If you want to pretty print the foreign type value, you can also override the ToString() function in their implementation.

The implementation of the global functions in [PriorityQueue.p](https://github.com/p-org/P/blob/master/Tutorial/PriorityQueue/PSrc/PriorityQueue.p) is available in [PriorityQueueFunctions.cs](https://github.com/p-org/P/blob/master/Tutorial/PriorityQueue/PForeign/PriorityQueueFunctions.cs).
Finally, the implementation of the local function in the Client machine is available in [ClientFunctions.cs](https://github.com/p-org/P/blob/master/Tutorial/PriorityQueue/PForeign/ClientFunctions.cs).

### Compiling PriorityQueue

Navigate to the [PriorityQueue](https://github.com/p-org/P/tree/master/Tutorial/PriorityQueue) folder and run the following command to compile the PriorityQueue project:

```shell
p compile
```

??? note "Expected Output"
    ```
    $ p compile

    .. Searching for a P project file *.pproj locally in the current folder
    .. Found P project file: P/Tutorial/PriorityQueue/PriorityQueue.pproj
    ----------------------------------------
    ==== Loading project file: P/Tutorial/PriorityQueue/PriorityQueue.pproj
    ....... includes p file: P/Tutorial/PriorityQueue/PSrc/PriorityQueue.p
    ....... includes p file: P/Tutorial/PriorityQueue/PTst/Client.p
    ....... includes p file: P/Tutorial/PriorityQueue/PTst/TestScripts.p
    ....... includes foreign file: P/Tutorial/PriorityQueue/PForeign/PriorityQueue.cs
    ....... includes foreign file: P/Tutorial/PriorityQueue/PForeign/PriorityQueueFunctions.cs
    ....... includes foreign file: P/Tutorial/PriorityQueue/PForeign/ClientFunctions.cs
    ----------------------------------------
    ----------------------------------------
    Parsing ...
    Type checking ...
    Code generation ...
    Generated PriorityQueue.cs.
    ----------------------------------------
    Compiling PriorityQueue...
    MSBuild version 17.3.1+2badb37d1 for .NET
    Determining projects to restore...
    Restored P/Tutorial/PriorityQueue/PGenerated/CSharp/PriorityQueue.csproj (in 392 ms).
    PriorityQueue -> P/Tutorial/PriorityQueue/PGenerated/CSharp/net6.0/PriorityQueue.dll

    Build succeeded.
    0 Warning(s)
    0 Error(s)

    Time Elapsed 00:00:04.08


    ----------------------------------------
    [PTool]: Thanks for using P!
    ```

### Checking PriorityQueue

Run the following command to run the test case [`tcCheckPriorityQueue`](https://github.com/p-org/P/blob/master/Tutorial/PriorityQueue/PTst/TestScripts.p#L2) in verbose mode:

```shell
p check -v
```

??? note "Expected Output"
    ``` xml
    $ p check -v

    .. Searching for a P compiled file locally in the current folder
    .. Found a P compiled file: P/Tutorial/PriorityQueue/PGenerated/CSharp/net6.0/PriorityQueue.dll
    .. Checking P/Tutorial/PriorityQueue/PGenerated/CSharp/net6.0/PriorityQueue.dll
    .. Test case :: tcCheckPriorityQueue
    ... Checker is using 'random' strategy (seed:1636311106).
    ..... Schedule #1
    <TestLog> Running test 'tcCheckPriorityQueue'.
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
    ... ### Process 0 is terminating
    ... Emitting coverage reports:
    ..... Writing PCheckerOutput/BugFinding/PriorityQueue.dgml
    ..... Writing PCheckerOutput/BugFinding/PriorityQueue.coverage.txt
    ..... Writing PCheckerOutput/BugFinding/PriorityQueue.sci
    ... Checking statistics:
    ..... Found 0 bugs.
    ... Scheduling statistics:
    ..... Explored 1 schedule: 1 fair and 0 unfair.
    ..... Number of scheduling points in fair terminating schedules: 5 (min), 5 (avg), 5 (max).
    ... Elapsed 0.3070145 sec.
    . Done
    [PTool]: Thanks for using P!
    ```

Hope you exploit the P Foreign interface to implement and check complex systems. If you have any questions, please feel free to post them in the discussions or issues.
