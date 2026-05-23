# P Compiler Redundancy Review

Scope: `Src/PCompiler/` — `CompilerCore/` (compiler library) and `PCommandLine/` (CLI).
Size: ~204 .cs files, ~25,847 LOC.

The compiler follows a visitor-pattern + large-switch-dispatch architecture for code generation.
Each backend (PChecker, PEx, PObserve, PVerifier, Stately) re-implements the full visitor
dispatch rather than sharing a base. This is the root cause of most of the redundancy below:
roughly **1,200+ lines** of duplicated case-statement dispatch across backends.

---

## Findings (highest signal first)

### 1. `WriteExpr` dispatch duplicated across backends — **High**

- `Src/PCompiler/CompilerCore/Backend/PChecker/PCheckerCodeGenerator.cs:1242-1575`
- `Src/PCompiler/CompilerCore/Backend/PEx/PExCodeGenerator.cs:1537-1947`

Both backends implement a monolithic `WriteExpr()` with 62+ case labels (`BinOpExpr`,
`UnaryOpExpr`, `CastExpr`, `CloneExpr`, `CtorExpr`, `DefaultExpr`, etc.). The case labels are
identical; only the emitted target syntax differs. ~330 lines per backend.

**Refactor:** Extract an abstract `ExpressionCodeGenerator` that owns the `switch(expr)`
dispatch once and exposes `protected abstract` hooks per case (`WriteBinOp`, `WriteCast`, …).
Each backend overrides the emission, not the dispatch. Saves ~330 LOC and prevents drift when a
new expression node is added.

### 2. `WriteStmt` dispatch duplicated across backends — **High**

- `Src/PCompiler/CompilerCore/Backend/PChecker/PCheckerCodeGenerator.cs:842-1240`
- `Src/PCompiler/CompilerCore/Backend/PEx/PExCodeGenerator.cs:687-1530`

Same pattern as #1 for 20+ statement kinds (`AssignStmt`, `IfStmt`, `WhileStmt`, `ForeachStmt`,
`SendStmt`, `ReceiveStmt`, …). ~400 lines per backend.

**Refactor:** Mirror the abstract base from #1 with a `StatementCodeGenerator`. Combined with
#1 this collapses ~1,460 LOC of dispatch into ~150 LOC of shared structure + targeted
overrides.

### 3. `PLanguageType → target-type` switch repeated per backend — **High**

- `PChecker/PCheckerCodeGenerator.cs:1577-1630` (`GetCSharpType`)
- `PEx/PExCodeGenerator.cs:1949-1985` (`GetPExType`)
- `PObserve/JavaSourceGenerator.cs` (via `TypeManager`)

Same ~20-case switch (`DataType`, `EnumType`, `ForeignType`, `MapType`, `SequenceType`,
`SetType`, `NamedTupleType`, `TupleType`, `PermissionType`, 8+ `PrimitiveType` cases) repeated
three times. Only the emitted strings differ (e.g. `IPValue` vs `PValue<?>`).

**Refactor:** Introduce a `TypeMapper` strategy with a default dispatch table; backends inject
their own mapping for primitives and collections. Eliminates ~150 LOC and prevents the common
bug of forgetting to add a new type to one backend.

### 4. `NameManager` sanitization logic forked — **High**

- `Src/PCompiler/CompilerCore/Backend/NameManagerBase.cs:1-71`
- `Backend/PChecker/PCheckerNameManager.cs:44-78`
- `Backend/PObserve/NameManager.cs:92-130`

`ComputeNameForDecl()` is essentially the same in PChecker and PObserve:
1. Special-case `NullEvent`/`HaltEvent` (`DefaultEvent` / `PHalt`)
2. Prefix `Interface` with `I_`
3. Replace anonymous decls with `Anon`
4. Strip leading `$` → `TMP_`
5. Disambiguate against reserved words
6. Call `UniquifyName(name)`

Only the reserved-word lookup differs (binary-searched C# keyword list vs `Constants.IsReserved()`).

**Refactor:** Move steps 1–5 into `NameManagerBase.ComputeNameForDecl()` as a template method
with a `protected abstract bool IsReserved(string)` hook. Removes ~30 LOC of duplication and a
real correctness risk — the two implementations have already diverged on edge cases.

### 5. `GetDefaultValue(type)` switch duplicated — **Medium**

- `PChecker/PCheckerCodeGenerator.cs:1631-1680`
- `PEx/PExCodeGenerator.cs:1987-2071`
- `PObserve/TypesGenerator.cs` (via `TypeManager`)

Same per-type structure: enums → min value, collections → `new T()`, tuples → construct with
field defaults, primitives → literals/nulls. ~50 LOC per backend.

**Refactor:** Fold into the `TypeMapper` from #3 as a `DefaultValueFor(type)` method.

### 6. `BinOpToStr` / `UnOpToStr` operator tables — **Medium**

- `PChecker/PCheckerCodeGenerator.cs:1709-1750`
- `PEx/PExCodeGenerator.cs:1867-1907`

18-case switches mapping `BinOpType` → operator string. PChecker emits operators (`+`, `-`, …);
PEx emits method names (`add`, `sub`, …). Identical structure.

**Refactor:** Replace with a `Dictionary<BinOpType,string>` per backend, populated once. Same
for unary ops. Drops ~50 LOC and makes the mapping table inspectable.

### 7. Error handler is 40+ near-identical one-liners — **Medium**

`Src/PCompiler/CompilerCore/DefaultTranslationErrorHandler.cs:1-398`

Forty-plus methods follow the shape:

```csharp
public Exception XYZ(ParserRuleContext loc, /* ... */) =>
    IssueError(loc, $"... {param} ...");
```

(`DuplicateStartState:23-31`, `DuplicateDeclaration:51-55`, `IncorrectArgumentCount:86-90`, …)

**Refactor:** Either (a) collapse to one `IssueError(ErrorCode, location, params object[] args)`
with messages in a resource/dictionary, or (b) keep typed methods but generate them from a
single table. The current shape is ~120 LOC of boilerplate that hides which errors actually
exist.

### 8. `CompilationContext` construction pattern repeated — **Medium**

- `Backend/PChecker/CompilationContext.cs:1-31`
- `Backend/PEx/CompilationContext.cs:1-138`
- `Backend/PObserve/CompilationContext.cs:1-22`
- `Backend/PVerifier/CompilationContext.cs:1-11`

Each derives from `CompilationContextBase`, instantiates a backend-specific `NameManager`,
sets `FileName`/`ProjectName`, and (in two cases) computes a main class name. The shape is the
same; only the parameters differ.

**Refactor:** Let `CompilationContextBase` take the `NameManager` factory and file-naming
template via constructor; concrete subclasses shrink to a few lines.

### 9. `FindLocalPProject` duplicated across CLI options — **Medium**

- `Src/PCompiler/PCommandLine/Options/PCheckerOptions.cs:135-158`
- `Src/PCompiler/PCommandLine/Options/PCompilerOptions.cs:103-132`

Byte-for-byte identical except (a) `PCompilerOptions` has the pproj/pfiles early-exit guard
and `PCheckerOptions` doesn't, and (b) one has commented-out logging. The missing guard in
`PCheckerOptions` looks like a latent bug — the two should never have diverged.

**Refactor:** Move to a `ProjectFileLocator.FindLocalPProject(...)` utility, call from both.

### 10. POM templates duplicated between backends — **Low**

- `Backend/PEx/Constants.cs:5-108`
- `Backend/PObserve/Constants.cs:114-179`

`pomTemplate` and `pomForeignTemplate` XML blobs are character-identical except for the Java
version (`1.8` vs `17`), `groupId`, and `artifactId`.

**Refactor:** Parameterize one template in a shared `MavenConstants` class.

### 11. Literal AST-node boilerplate — **Low**

- `TypeChecker/AST/Expressions/BoolLiteralExpr.cs`
- `TypeChecker/AST/Expressions/IntLiteralExpr.cs`
- `TypeChecker/AST/Expressions/FloatLiteralExpr.cs`

Three classes, each ~18-28 lines, all carrying `Value`, `SourceLocation`, `Type`, and two
constructor variants. The three have already drifted: `IntLiteralExpr` skips initializing
`SourceLocation` in one overload; `FloatLiteralExpr` lacks the parameterless ctor that the
others have.

**Refactor:** Generic `PrimitiveLiteralExpr<T>` with a `PrimitiveType` argument, or at minimum
align the three constructors so future literals don't pick the wrong template.

### 12. Literal emission and argument-list rendering — **Low**

- `PChecker/PCheckerCodeGenerator.cs:1428-1433, 1482, 1532`
- `PEx/PExCodeGenerator.cs:1623-1639, ~1699`

Per-literal emission (`((PInt)(...))` vs `new PInt(...)`) and ad-hoc `string.Join(",", …)` for
tuple/call argument rendering are sprinkled through both generators.

**Refactor:** A `EmitLiteral(type, value)` hook on the shared base (#1) plus a single
`RenderArgList(args, sep, fmt)` helper removes another ~50 LOC of micro-duplication.

---

## Summary

| # | Issue | Severity | Approx. LOC | Sites |
|---|---|---|---|---|
| 1 | `WriteExpr` dispatch | **High** | 330+ | 2 |
| 2 | `WriteStmt` dispatch | **High** | 400+ | 2 |
| 3 | Type-mapping switch | **High** | 150+ | 3 |
| 4 | `NameManager` sanitization | **High** | 60 | 2 |
| 5 | `GetDefaultValue` switch | Medium | 100 | 2 |
| 6 | BinOp/UnOp tables | Medium | 50 | 2 |
| 7 | Error-handler boilerplate | Medium | 120 | 40+ |
| 8 | `CompilationContext` ctor pattern | Medium | 120 | 4 |
| 9 | `FindLocalPProject` in CLI | Medium | 24 | 2 |
| 10 | POM templates | Low | 100 | 2 |
| 11 | Literal AST boilerplate | Low | 70 | 3 |
| 12 | Literal emit / arg rendering | Low | 50 | 5+ |

Total duplicated/boilerplate code: **~1,500 LOC** out of ~26k (~5.8% of the compiler).

## Recommended order

1. Visitor base classes for `WriteExpr` + `WriteStmt` (#1, #2) — biggest LOC win, biggest
   correctness payoff (new AST nodes break the build until every backend handles them).
2. `TypeMapper` strategy folding in `GetDefaultValue` and the BinOp/UnOp tables (#3, #5, #6).
3. `NameManagerBase` template method (#4) — small but the divergence is already a latent bug
   source.
4. `ProjectFileLocator` extraction (#9) — quick fix that closes a real bug in the checker CLI
   (missing pproj/pfiles early-exit guard).
5. Error handler consolidation (#7) — independent, can be done any time.
6. The Low-severity items (#10-#12) — pick up while touching the surrounding code, not as a
   dedicated pass.

Notably, none of these refactors change observable compiler behavior — they're pure
structural cleanups that should be guarded by the existing `dotnet test` and tutorial suites.
