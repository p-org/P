P 3.0 Changes

=== PChecker runtime values used by foreign code ===
- `PTuple.fieldValues` and `PNamedTuple.fieldValues` are now
  `IReadOnlyList<IPValue>` (they were `List<IPValue>`): change a field through
  the tuple's indexer (`t[i] = v`, `t["name"] = v`). `PNamedTuple.fieldNames`
  is now a `ReadOnlyCollection<string>`, because tuples of a type share their
  field names. Tuples can no longer grow, and `new PNamedTuple()` is gone.
- The tuple constructors copy the arrays they are given, as before.
  Generated code uses `PTuple.FromOwnedValues`/`PNamedTuple.FromOwnedValues`,
  which keep the array.
- `PMachineValue.Permissions` is now a `ReadOnlyCollection<string>` (it was
  a `List<string>`), because copies of a machine reference share it.

=== PChecker foreign sends and creates ===
- PChecker runs state machines cooperatively on one thread, and foreign code
  cannot be suspended. `StateMachine.SendEvent` and
  `StateMachine.CreateInterface` called from foreign code are therefore
  checked right away but performed when the foreign function returns to P
  code, each at the scheduling point of a P `send` or `new`, in order. The
  checker explores the same interleavings as before. Monitors observe these
  sends (and events announced while the foreign function runs) at that time.
  A machine created by foreign code can be sent to, but does not run, until
  then.
- P functions that call foreign functions (directly or through callees), and
  P functions that send or create, are now `async` in the generated C# code.
  Foreign code that calls them back gets a `Task`, which has already
  completed: ignore it, or read `.Result` for a function that returns a value
  (code that used the value directly must be changed). A P function called
  from foreign code must not `receive`.
- Calling `SendEvent`/`CreateInterface` on a machine other than the one
  passed to the foreign function (e.g. one saved by an earlier call) now stops
  the checker with an internal error (instead of reporting a bug in the P
  program), before monitors observe anything.

=== Multi-Error Type Checker (#957, #963, #965, #967, #970) ===
- `p compile` now reports ALL type errors in one pass by default, sorted by
  source location, instead of aborting on the first error.
- Cascade-suppression (ErrorType/ErrorExpr sentinels +
  `TypeCheckingUtils.CheckAssignable`) prevents one root-cause error from
  generating downstream "incompatible operand" noise.
- Pass-level tolerance in `Analyzer.cs` (`TolerantStep`) so one bad
  machine/function doesn't clobber diagnostics from its siblings (#967).
- Cross-machine `<state>` lookup in `x is <state>` test expressions now
  resolves against the instance's specific machine (#963).
- New CLI flag `--strict-errors` / `-se` opts back into legacy abort-on-
  first behavior for users / CI scripts that depend on it (#970).
- Exit codes unchanged: 0 on success, 1 on any error. The change is
  user-visible only in the number of diagnostics emitted per failed
  compile.

=== First PL ===
- branch: `dev_p3.0/cleanup_targets`
- Targets have been renamed to `PChecker`, `PObserve`, and `Stately`; `PVerifier` and `PExhaustive` to be added later.
- Removed `Symbolic` and other engines around it.


