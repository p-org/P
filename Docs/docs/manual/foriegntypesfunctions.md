
!!! note "Foreign Types"
    Foreign types in P are types that are declared in P but defined or implemented in the foreign language ([foreign type](datatypes.md#foreign)).

!!! note "Foreign Functions"
    Foreign functions in P are local or global functions that are declared in P but defined or implemented in the foreign language ([foreign functions](functions.md#foreign-functions)).


!!! hint "Recommendation: Using Foreign Types and Functions"
    Programmers can consider implementing a particular type as Foreign type in P if its a complicated data structure and implementing them using P types is either not possible or too combursome. For example, P does not support declaring recursive data types, and hence, implementing a linked-list or a tree like data-structure in P is hard. Hence, we recommend programmers to implement such types as foreign types.

    When modeling complex systems, many a times programmers need to implement complicated logic/functions that manupulate or iterate over data-structures. Such functions can be easily implemented in foreign languages like Java and C# as compared to P. For example, iterating over collections and manipulating them is easier in Java and C#, and verbose in P as P only supports `while` loop and not a iterator like `foreach`. Also functions that operate on foreign types are implemented as foreign functions.   

The [Two Phase Commit](../tutorial/twophasecommit.md) example had introduced the foreign function feature. We will now use a simple PriorityQueue example to go into the details of foreign interface in P.



### [PriorityQueue](https://github.com/p-org/P/tree/master/Tutorial/PriorityQueue)

The PriorityQueue [project](https://github.com/p-org/P/blob/master/Tutorial/PriorityQueue/PriorityQueue.pproj) presents an example where a `Client` uses a priority queue implemented as foreign types and functions.

#### P Source

The [PriorityQueue.p](https://github.com/p-org/P/blob/master/Tutorial/PriorityQueue/PSrc/PriorityQueue.p) file declares:  (1) a foreign type [`tPriorityQueue`](https://github.com/p-org/P/blob/master/Tutorial/PriorityQueue/PSrc/PriorityQueue.p#L3) and (2) [global foreign functions](https://github.com/p-org/P/blob/master/Tutorial/PriorityQueue/PSrc/PriorityQueue.p#L7-L22) are used to operate or interact with the priority queue.

!!! danger "P has Value Semantics (no Pass by Reference!!)"
    Note that P does not support pass by references, everything in P is always pass by value. Hence the functions above must return a priority queue after adding the element into the queue (mutated queue).

#### P Test

The [Client.p](https://github.com/p-org/P/blob/master/Tutorial/PriorityQueue/PTst/Client.p) files presents a [`Client`](https://github.com/p-org/P/blob/master/Tutorial/PriorityQueue/PTst/Client.p#L5) state machine that uses the priority queue and performs operations on it by calling the foreign function. The `Client` machine also declares a [local foreign function]() to add an element into the queue.

#### P Foreign Code



