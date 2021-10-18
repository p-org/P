P Supports implementing types and functions in a foreign language, and then using them in P programs just like any other P types and functions.

!!! hint "Recommendation: Using Foreign Types and Functions"
    Programmers can consider implementing a particular type as Foreign type in P if its a complicated data structure and implementing them using P types is either not possible or too combursome. For example, P does not support declaring recursive data types, and hence, implementing a linked-list or a tree like data-structure in P is hard. Hence, we recommend programmers to implement such types as foreign types.

    When modeling complex systems, many a times programmers need to implement complicated logic/functions that manupulate or iterate over data-structures. Such functions can be easily implemented in foreign languages like Java and C# as compared to P. For example, iterating over collections and manipulating them is easier in Java and C#, and verbose in P as P only supports `while` loop and not a iterator like `foreach`. Also functions that operate on foreign types are implemented as foreign functions.   

The [Two Phase Commit](../tutorial/twophasecommit.md) example had introduced the Foreign function feature. We now use a simple PriorityQueue example to go into the details of foreign interface in P.

### [PriorityQueue]()

The PriorityQueue project presents an example where a `Client` uses a priority queue implemented as foreign types and functions.

#### P Source

The [PriorityQueue.p]() file declares the foreign type [`tPriorityQueue`]() and also [global foreign functions]() that can be used to operate or interact with the priority queue.

#### P Test

The [Client.p]() files presents a `Client` state machine that uses the priority queue and performs operations on it by calling the foreign function. The `Client` machine also declares a [local foreign function]() to add an element into the queue.

#### P Foreign Code



