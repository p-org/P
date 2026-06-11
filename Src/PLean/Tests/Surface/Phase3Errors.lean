/-
`#guard_msgs`-pinned regression tests for surface error paths:

- `Lemma default` / `Theorem default` are rejected (`default` is the
  sanity-invariant sentinel).
- `prove <unknown>;` and `prove … using <unknown>;` raise an error at
  the offending token.

`#guard_msgs in` wraps a single command and must appear *inside* the
open pmodule, immediately before the offending decl.
-/
import PLean

open PLean

/-! ## `Lemma default` is reserved -/

pmodule Phase3ErrLemmaDefault
/--
error: `Lemma` name 'default' is reserved for the sanity-invariant sentinel used by `prove default;`
-/
#guard_msgs in
Lemma default {
  invariant t : True
}
end Phase3ErrLemmaDefault

/-! ## `Theorem default` is reserved -/

pmodule Phase3ErrTheoremDefault
/--
error: `Theorem` name 'default' is reserved for the sanity-invariant sentinel used by `prove default;`
-/
#guard_msgs in
Theorem default {
  invariant t : True
}
end Phase3ErrTheoremDefault

/-! ## `prove <unknown>;` is rejected -/

pmodule Phase3ErrUnknownTarget
  event eFoo
  machine M {
    start state S { on eFoo { pure () } }
  }

  Lemma good {
    invariant t : True
  }

/--
error: `prove`: no `Lemma` or `Theorem` named 'notALemma' in pmodule 'Phase3ErrUnknownTarget' (must be `default` or a previously-declared lemma)
-/
#guard_msgs in
Proof {
  prove notALemma ;
}
end Phase3ErrUnknownTarget

/-! ## `prove ... using <unknown>;` is rejected at the `using` token -/

pmodule Phase3ErrUnknownUsing
  event eFoo
  machine M {
    start state S { on eFoo { pure () } }
  }

  Lemma good {
    invariant t : True
  }

/--
error: `prove ... using`: no `Lemma` or `Theorem` named 'notALemma' in pmodule 'Phase3ErrUnknownUsing'
-/
#guard_msgs in
Proof {
  prove good using notALemma ;
}
end Phase3ErrUnknownUsing
