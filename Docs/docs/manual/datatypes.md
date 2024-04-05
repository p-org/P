P Supports the following data types:

| P Types                                       | Description                                                                                                                |
| :-------------------------------------------- | :------------------------------------------------------------------------------------------------------------------------- |
| [Primitive](#primitive)            | `int`, `bool`, `float`, `string`, `enum`, `machine`, and `event`.                                                          |
| [Record](#record)                  | `tuple` and `named tuple`                                                                                                  |
| [Collection](#collection)          | `map`, `seq`, and `set`                                                                                                    |
| [Foreign](#foreign)                     | These are types that are not defined in P but in an external language (e.g., C# or Java) and can be used in the P program. |
| [User Defined](#user-defined)           | These are user defined types that are constructed using any of the P types listed above                                    |
| [Universal Supertypes](#universal-supertypes) | `any` and `data`                                                                                                           |

??? note "P Types Grammar"

    Data types in P:

    ```
    type :
     | bool                         # PrimitiveType
     | int                          # PrimitiveType
     | float                        # PrimitiveType
     | string                       # PrimitiveType
     | event                        # PrimitiveType
     | machine                      # PrimitiveType

     | (type (, type)*)             # TupleType
     | (iden: type (, iden: type)*) # NamedTupleType

     | seq[type]                    # SeqType
     | set[type]                    # SetType
     | map[type, type]              # MapType

     | data                         # UniversalType
     | any                          # UniversalType

     | iden                         # UserDefinedType
     ;
    ```

    Declaring user defined types and foreign types:
    ```
    typeDecl :
     | type iden ;                  # ForeignTypeDeclaration
     | type iden = type ;           # UserDefinedTypeDeclaration
     ;
    ```

    Declaring enum types:

    ```
    enumTypeDecl :
     | enum iden { enumElemList }
     | enum iden { numberedEnumElemList }
     ;
    enumElemList : enumElem (, enumElem)* ;
    enumElem : iden ;
    numberedEnumElemList : numberedEnumElem (, numberedEnumElem)* ;
    numberedEnumElem : iden = IntLiteral ;
    ```

!!! info "Operations on P data types"
    Details for the operations that can be performed on
    P datatypes are described in the [expressions](expressions.md) and [statements](statements.md).

### Primitive

P supports the common primitive datatypes like `int`, `bool`, `float`, and `string`. Two
additional primitive data types that are specific to the P language are `event` and
`machine`. `event` type represents the set of all P events. Similarly, `machine` type
represents the set of all machine references.

=== "Primitive Data types"

    ``` java
    ...
    event eRequest: bool;
    ...

    // some function body in the P program
    {
        var i: int;
        var j: float;
        var k: string;
        var l: bool;
        var ev: event;
        var client: machine;

        ev = eRequest;
        client = new Client();
        i = 10; j = 10.0; k = "Failure!!";
        l = (i == (j to int));
        assert l, k;

        send client, ev, l;
    }
    ```

### Enum

P supports enums, enum values in P are considered as global constants and must have unique
name. Enums by default are given integer values starting from 0 (if no values are assigned
to the elements). Enums in P can be coerced to `int`. Please refer to the grammar above for the syntax for declaring enums.

=== "Enum Declaration"

    ```
    enum tResponseStatus { ERROR, SUCCESS, TIMEOUT }

    // usage of enums
    var status: tResponseStatus;

    status = ERROR;

    // you can coerce an enum to int
    assert (ERROR to int) == 0;
    ```

=== "Enum Declaration with Values"

    ```
    enum tResponseStatus { ERROR = 500, SUCCESS = 200,
    TIMEOUT = 400; }

    // usage of enums
    var status: tResponseStatus;

    status = ERROR;

    // you can coerce an enum to int
    assert (ERROR to int) == 500;
    ```

### Record

P supports two types of records: tuples and named tuples. The fields of a tuple can be
accessed by using the `.` operation followed by the field index.

```
// tuple with three fields
var tupleEx: (int, bool, int);

// constructing a value of tuple type.
tupleEx = (20, false, 21);

// accessing the first and third element of the tupleEx
tupleEx.0 = tupleEx.0 + tupleEx.2;
```

Named tuples are similar to tuples with each field having an associated name. The fields
of a named tuple can be accessed by using the `.` operation followed by the field name.

```
// named tuple with three fields
var namedTupleEx: (x1: int, x2: bool, x3: int);

// constructing a value of named tuple type.
namedTupleEx = (x1 = 20, x2 = false, x3 = 21);

// accessing the first and third element of the namedTupleEx
namedTupleEx.x1 = namedTupleEx.x1 + namedTupleEx.x3;
```

!!! note "Note"
    Tuple and Named tuple types are disjoint, i.e., a tuple of type `(int,
    bool, int)` cannot be assigned to a variable of tuple `(x1: int, x2: bool, x3: int)`
    though the elements of the tuple have same types (and vice versa). And similarly, a named
    tuple of type `(x1: int, x2: bool, x3: int)` cannot be assigned to a variable of tuple
    `(y1: int, y2: bool, y3: int)` they are two distinct types.

### Collection

P supports three collection types: map, sequence (lists), and set. The operations to mutate the collection types like insert, update, and remove elements are described in the [statements](statements.md) section. One can use the `while` loop to iterate over these collection types. Other operations like `sizeof`, `in` (to check containment), `choose` (pick a value nondeterministically), `keys`, and `values` on these collection types are defined in the [expressions](expressions.md) section.

**Syntax**:

- `map[K, V]` represents a `map` type with keys of type `K` and values of type `V`.
- `seq[T]` represents a sequence type with elements of type `T`.
- `set[T]` represents a set type with elements of type `T`.

### Foreign

P allows programmers to define (or implement) types in the external languages. We refer to these types as foreign types, they are declared in P but are implemented in an external language. They can be used inside P programs just like any other types.

**Syntax:**: `type tName;`

`tName` is the name of the foreign type.

Note that foreign types are disjoint from all other types in P. They are subtype of the `any` type.
Details about how to define/implement foreign types in P is described [here](foriegntypesfunctions.md).

### User Defined

P supports assigning names to type i.e., creating `typedef`. Note that these typedefs are simply assigning names to P types and does not effect the sub-typing relation.

**Syntax:**:  `type iden = type ;`

=== "User Defined Type Declaration"

    ``` java
    // defining a type tLookUpRequest
    type tLookUpRequest = (client: machine, requestId: int, key: string);
    // defining a type tLookUpRequestX
    type tLookUpRequestX = (client: machine, requestId: int, key: string);
    ```
    The programmers can use type `tLookUpRequest` as a short hand for referring to the type `(client: machine, requestId: int, key: string)`
    Note that the types `tLookUpRequest` and `tLookUpRequestX` are same, the compiler does not distinguish between the two types.


### Universal Supertypes

P supports two universal supertypes (`any` and `data`), type that are supertypes of all
types in the language.

- `any` type in P is the supertype of all types. Also, note that in P, `seq[any]` is a super type of `seq[int]` and similarly for other collection types.
- `data` type in P is the supertype of all types in P that do not have a `machine` type
  embedded in it. This type is mainly used to represent values in P that do not have a
  machine reference embedded in them i.e., the value is purely "data" and has no machine
  "references" in it. For example, `data` is a supertype of `(key: string, value: int)`
  but not `(key: string, client: machine)`.

### Default values for P data types

The `default` feature in P (checkout details in [expressions](expressions.md)) can be used
to obtain the default value of any P type. P variables on declaration are automatically
initialized to their default values.

??? info  "`default` values of P types"

    P variables on declaration are automatically initialized to their default values. For example:

    ```
    var s : set[int];
    // by default a set type is initialized to an empty set
    assert sizeof(s) == 0;

    s += (100);
    assert sizeof(s) == 1;

    // reset the variable to an empty set
    s = default(set[int]);
    ```

    Similarly,

    ```
    type tRequest = (client: machine, requestId: int);
    type tResponse = (values: map[int, int]);

    ...
    // initializes x to (client = null, requestId = 0);
    x = default(tRequest);
    // initializes y to (values = {}), empty map.
    y = default(tResponse);

    assert x.client == default(machine);
    assert sizeof(y.values) == 0;
    ```

    | P Types                        | Default Value                                                      |
    | :----------------------------- | :----------------------------------------------------------------- |
    | int                            | 0                                                                  |
    | float                          | 0.0                                                                |
    | bool                           | false                                                              |
    | string                         | ""                                                                 |
    | event                          | null                                                               |
    | machine                        | null                                                               |
    | enum                           | Element of the enum with lowest (int) value                        |
    | Record (tuple or named tuple)  | Each field in the record type is initialized to its default value. |
    | Collection (set, seq, and map) | Empty collection                                                   |
    | Foreign                        | null                                                               |
    | `any` and `data`               | null                                                               |
