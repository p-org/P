P 3.0 Changes

=== PChecker cooperative scheduler (#1002) ===
- PChecker runs the state machines of a schedule cooperatively on one thread
  (it used to run each on its own controlled task). P programs do not change,
  but C# code that uses the checker directly, and foreign code, may have to:
- Re-run `p compile` for every project. Generated code is stamped with
  `[assembly: GeneratedCodeVersion(...)]`, and `p check` refuses a
  `PGenerated` dll from another version of P (or one without the stamp) with
  "... was compiled by an older version of P ... Please re-run 'p compile'"
  and exit code 2, instead of reporting bogus bugs. Foreign code built
  against the P runtime must be recompiled too.
- Removed, with no replacement, because they need the removed multi-threaded
  task layer: the `PChecker.Tasks` namespace, asynchronous (`async` or
  `Task`-returning) test methods, `TestingEngine.Create(CheckerConfiguration,
  Func<Task>)`, `ControlledRuntime.SendEventAndExecuteAsync` and
  `ControlledRuntime.CreateStateMachineAndExecuteAsync`. Test methods must be
  synchronous (`public static void`, with no parameter or a
  `ControlledRuntime`). Test discovery of a harness built against the removed
  types lists them and says so.
- Also removed: `PChecker.Random.Generator` (use `RandomBoolean()` and
  `RandomInteger(n)` of the state machine passed to the foreign function) and
  `VectorTime.Clock`.
- `StateMachine.ReceiveEventAsync` returns `ValueTask<Event>` (it returned
  `Task<Event>`): await it once, or call `.AsTask()`.
- `StateMachine.SendEvent` and `StateMachine.CreateInterface` keep their
  synchronous signatures for foreign code, but no longer send or create right
  away (see "PChecker foreign sends and creates" below and
  Docs/docs/manual/foreigntypesfunctions.md). Generated code uses the new
  `SendEventAsync`/`CreateInterfaceAsync`, which are scheduling points, and
  wraps foreign calls in `BeginForeignCall()`/`EndForeignCallAsync()`;
  foreign code should not call them.
- `RaiseEvent` and `RaiseGotoStateEvent<S>()` called from foreign code no
  longer throw to unwind the foreign function: they record the transition,
  which is taken when the foreign function returns, so return right after
  calling them. `PNonStandardReturnException` and `NonStandardReturn` are
  kept as `[Obsolete]` and never thrown.
  `ControlledRuntime.SendEvent(StateMachineId, Event)` is `[Obsolete]`: use
  `StateMachine.SendEvent`.
- The C# test method cannot be suspended. Only its first create (while no
  state machine is enabled yet) takes the scheduling point of a P `new`; the
  machines it creates start running after it returns, so the test method no
  longer interleaves with them.
- Traces say "... was created by the test." (text and JSON) and "test" (XML)
  for a machine created by the test method, instead of "... was created by
  task 'N'", whose number varied between runs.
- Schedules are explored without building traces. A failing schedule is
  replayed with tracing to write its `.txt` and `.trace.json`, so its foreign
  code (and the per-schedule dispose method of a C# test) runs once more. If
  the replay does not reproduce the bug (e.g. the program depends on static
  state in foreign code), the checker prints a warning and puts it at the top
  of the text trace; both traces still end with the bug found by
  exploration. `--verbose` traces every schedule as it runs, as before.
- With `--explore`, which writes no traces, failing schedules are neither
  traced nor replayed.
- New option `--no-activity-coverage` skips the activity coverage reports
  (`.coverage.txt` and `.sci`), which take about a third of exploration time.
  Scenario coverage is still collected (see Docs/docs/getstarted/usingP.md).

=== PChecker runtime values used by foreign code ===
- `PTuple.fieldValues` and `PNamedTuple.fieldValues` are now
  `IReadOnlyList<IPValue>` (they were `List<IPValue>`): change a field through
  the tuple's indexer (`t[i] = v`, `t["name"] = v`). `PNamedTuple.fieldNames`
  is now a `ReadOnlyCollection<string>`, because tuples of a type share their
  field names. Tuples can no longer grow: `new PNamedTuple()` is gone, and
  `new PTuple()` now makes an empty tuple that stays empty.
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
  sends (and events announced while the foreign function runs) at that time,
  with a copy of the payload taken when the foreign code sent or announced
  it, so changing the payload afterwards changes neither what monitors nor
  what the target observes.
  A machine created by foreign code can be sent to, but does not run, until
  then. Its id is numbered at the scheduling point of the create, as for a P
  `new`, unless the foreign code prints, compares or hashes it before.
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


