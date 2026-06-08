/-
PLean Phase-3 — `#guard_msgs`-pinned regression tests for the
error paths the second-pass review (REVIEW_P3 §4 / §B.1) flagged as
unprotected. Each test exercises one of the validations added during
the REVIEW_P3 follow-up sweep so a future refactor that weakens any
of these checks fails CI rather than passing silently.

`#guard_msgs in` wraps a *single* command. We keep each error case
in its own `pmodule` and place the `#guard_msgs in <command>` against
the specific declaration that should fail. (Wrapping a whole
`pmodule M ... end M` block fails to capture diagnostics that
elaborate at a child position; the `#guard_msgs` must appear
*inside* the open pmodule, immediately before the offending decl.)
-/
import PLean

open PLean

/-! ## §4.7 — `Lemma default` reserved-name rejection -/

pmodule Phase3ErrLemmaDefault
/--
error: `Lemma` name 'default' is reserved for the sanity-invariant sentinel used by `prove default;`
-/
#guard_msgs in
Lemma default {
  invariant t : ∀ _ : GlobalState Sig, True
}
end Phase3ErrLemmaDefault

/-! ## §4.7 — same applies to `Theorem default` -/

pmodule Phase3ErrTheoremDefault
/--
error: `Theorem` name 'default' is reserved for the sanity-invariant sentinel used by `prove default;`
-/
#guard_msgs in
Theorem default {
  invariant t : ∀ _ : GlobalState Sig, True
}
end Phase3ErrTheoremDefault

/-! ## §4.6 — `prove <unknown>;` rejected at the `prove` line -/

pmodule Phase3ErrUnknownTarget
  event eFoo
  machine M {
    start state S { on eFoo { pure () } }
  }

  Lemma good {
    invariant t : ∀ _ : GlobalState Sig, True
  }

/--
error: `prove`: no `Lemma` or `Theorem` named 'notALemma' in pmodule 'Phase3ErrUnknownTarget' (must be `default` or a previously-declared lemma)
-/
#guard_msgs in
Proof {
  prove notALemma ;
}
end Phase3ErrUnknownTarget

/-! ## §4.6 — `prove ... using <unknown>;` rejected at the using-token -/

pmodule Phase3ErrUnknownUsing
  event eFoo
  machine M {
    start state S { on eFoo { pure () } }
  }

  Lemma good {
    invariant t : ∀ _ : GlobalState Sig, True
  }

/--
error: `prove ... using`: no `Lemma` or `Theorem` named 'notALemma' in pmodule 'Phase3ErrUnknownUsing'
-/
#guard_msgs in
Proof {
  prove good using notALemma ;
}
end Phase3ErrUnknownUsing
