P 3.0 Changes

=== Multi-Error Type Checker (#957, #963, #965) ===
- Opt-in via `P_COMPILER_COLLECT_ERRORS=1` environment variable.
- When enabled, `p compile` reports all type errors in one pass instead of
  aborting on the first error. Errors are sorted by source location.
- Cascade-suppression rules (ErrorType/ErrorExpr sentinels +
  `TypeCheckingUtils.CheckAssignable`) prevent one root-cause error from
  generating downstream "incompatible operand" noise.
- Cross-machine `<state>` lookup (in `x is <state>` test expressions) now
  resolves against the instance's specific machine; same-named states in
  unrelated machines no longer bind silently (#963).
- Default behavior (env var unset) is unchanged — strict mode still
  aborts on the first error, bit-for-bit identical to pre-3.0 behavior
  for valid programs and same exit code on invalid programs.

=== First PL ===
- branch: `dev_p3.0/cleanup_targets`
- Targets have been renamed to `PChecker`, `PObserve`, and `Stately`; `PVerifier` and `PExhaustive` to be added later.
- Removed `Symbolic` and other engines around it.


