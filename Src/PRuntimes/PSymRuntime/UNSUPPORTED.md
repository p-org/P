# What PSym currently does not support

Here is a summary of P features and constructs that PSym *does not* currently support:
- C# foreign functions (PSym supports Java foreign functions instead)
- Module refinement and interfaces
- Deadlock detection (coming soon)
- Recursive functions (except tail recursion)
- Relational operations over strings
- Receive statement in state exit functions
- Continue statement in a loop
- Enums with non-zero default values
- Type casting collections over any type
- Complex type casting with any type
- Comparison of null with any type
- Null events
