### Getting Started
- To use `JSylvan`, we need to first create a `JSylvan` object. A common way to do this is through the use of the `init` method, which creates the instance object. The `init` method supports the following arguments:
1. `workers`: The number of workers, 0 for autodetect; type `int`
2. `maxMemory`: Maximum bytes for the unique table and computed table; type `long`
3. `tableRatio`: How much bigger the unique table is vs computed table; type `int`
4. `initialRatio`: How much smaller the tables are initially; type `int`
5. `granularity`: Controls how often the cache is used (typically set between 1 and 10)

- To terminate the usage of a `JSylvan` object, we call `quit` on the object; that will clear all operations and free the memory in the process used by the object.
- Usually, after we have instantiated the `JSylvan` object, we should call the garbage collector methods in the following order:
    1. `JSylvan.disableGC()`
    2. `JSylvan.enableGC()`

This helps reset the garbage collector setting and turns it automatically on.
### Common Usage Pattern
- To create a BDD representation of variable(s), we usually use the following structure:
```java
long a = JSylvan.ref(JSylvan.makeVar(...));
```
Here, `ref` is a reference pointer that directs to the actual object; `makeVar` creates a variable with the given subindex (for example `makeVar(4)` would be creating a variable called $x_4$).

- To create a BDD set of variables, we use the method `makeSet()`, whose argument takes in an array of integer values
- To calculate the number of satisfying assignments with a given domain $C$ (from `makeSet()`), we use the method `satcount(<BDD representation>, <set of variables>)`
- For testing existential quantification `there exists a such that relation holds`, we use the method `makeExists(relations, variable)`
- Multiple variables can be grouped together as an array of `makeVar` outputs and applied disjunction operation using `makeUnionPar`

### List of Commonly Used Methods
- Here is a list of methods commonly used by `JSylvan` to access/compute components within a BDD:
  1. `getTrue()`: return the BDD representing `True`
  2. `getFalse()`: return the BDD representing `False`
  3. `makeVar(int a)`: compute the BDD representing the variable `a`
  4. `makeNot(long a)`: Given the BDD representing the formula `<a>`, compute its negation
  5. `makeAnd(long a, long b)`: compute `a and b`
  6. `makeOr(long a, long b)`: compute `a or b`
  7. `makeImplies(long a, long b)`: compute the logic assertion `a implies b`
  8. `makeIte(long a, long b, long c)`: compute the logic predicate `IF a THEN b ELSE c`
  9. `makeEquals(long a, long b)`: compute `a == b`
  10. `makeNotEquals(long a, long b)`: compute `a xor b`
  11. `makeExists(long a, long variables)`: Given set of variables encoded as BDD variables, compute the existential quantification (there exists)
  12. `makeForall(long a, long variables)`: Given set of variables encoded as BDD variables, compute universal quantification (for all)
  13. `makePrev(long a, long b, long variables)`: `a` is the transition relation, `b` is the set of next states. This method computes the predecessors of states in `b` given relation `a`. This can be used to concatenate two transition relations `a` and `b`
  14. `makeUnionPair(long[] bdds)`: Given an array of BDDs, compute their disjunction (union) in parallel
  15. `getVar(long bdd)`: retrieves the label of the variable of the root node of the BDD
  16. `getIf(long bdd)`: returns a BDD representing that variable
  17. `getThen(long bdd)`: returns the BDD where `getVar(bdd)` is True
  18. `getElse(long bdd)`: same as `getThen` but when `getVar(bdd)` is False
  19. `print(long bdd)`: writes the BDD in text format to standard output
  20. `fprint(String filename, long bdd)`: writes the BDD in text format to a given file path
  21. `enableGC()/disableGC()`: enables/disables automatic garbage collection
  22. `getTableUsed()`: returns the current number of BDD nodes in the hash table
  23. `getTableSize()`: returns the current max number of nodes in the hash table
  24. `satcount()`: calculates the number of variable assignments for which the BDD evaluation yields true
  25. `nodeCount()`: calculates the number of nodes in the BDD
  26. `makeSupport()`: calculates the set of variables used in the BDD
  27. The "BDD mapping" is used by `compose` (functional composition, aka variable renaming: If a node `n` of variable `x` is in the BDD where `x` is in the mapping, then replace that node with the result of `IF mapping[v] THEN n.then ELSE n.else`). Here's an assorted list of all the methods supported by BDD mapping:
        - `mapEmpty`: return an empty mapping 
        - `mapIsEmpty`: return true if a mapping is empty
        - `mapKey`: return key of first entry in the mapping
        - `mapValue`: return value of first entry in the mapping
        - `mapNext`: return mapping without first entry
        - `mapContains`: return true if key in mapping
        - `mapCount`: return number of entries in mapping
        - `mapAdd`: add a (key,value) to the mapping
        - `mapUpdate`: return map1 for values not in map2, map2 otherwise
        - `mapRemove`: remove a key from the mapping
        - `mapRemoveAll`: remove multiple keys from the mapping

