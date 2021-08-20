P Supports the following data types:

| P Types                                       | Description                                                                                                                |
|:----------------------------------------------|:---------------------------------------------------------------------------------------------------------------------------|
| [Primitive](#primitive-data-types)            | `int`, `bool`, `float`, `string`, `enum`, `machine`, and `event`.                                                          |
| [Record](#record-data-types)                  | `tuple` and `named tuple`                                                                                                  |
| [Collection](#collection-data-types)          | `map`, `seq`, and `set`                                                                                                    |
| [Foreign](#foreign-types)                     | These are types that are not defined in P but in an external language (e.g., C# or Java) and can be used in the P program. |
| [User Defined](#user-defined-types)           | These are user defined types that are constructed using any of the P types listed above                               |
| [Universal Supertypes](#universal-supertypes) | `any` and `data`                                                                                                           |

??? note "P Types Grammar"

### Primitive

### Record

### Collection

### Foreign

### User Defined

### Universal Supertypes

P supports two universal supertypes (`any` and `data`), type that are supertypes of all
types in the language.

- `any` type in P is the supertype of all types.
- `data` type in P is the supertype of all types in P that do not have a `machine` type
  embedded in it. This type is mainly used to represent values in P that do not have a
  machine reference embedded in them i.e., the value is purely "data" and has no machine
  "references" in it. For example, `data` is a supertype of `(key: string, value: int)`
  but not `(key: string, client: machine)`.
