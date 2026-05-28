P 3.0 Changes

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


