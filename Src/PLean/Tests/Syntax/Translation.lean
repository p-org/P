/-
Translation pins: the generated program and specifications mean what the
P surface says (payloads through `on e goto`, parameters shadowing
machine variables, P's `==` on containers, map errors, kind guards,
`raise` of an ignored event), and what P rejects is rejected.
-/
import PLean

open PLean

pmodule TransT
  system s

  type tV = (v : Int)
  event eGo : tV
  event eVal : Int
  event eMap : tV
  event eRead : Int
  event eIgn
  event eRaise
  event eGuard : Int

  -- functions in source order, each using the previous one
  function zz : Int = 1
  function aa : Int = zz + 1
  function mm : Int = aa + 1
  -- `=` between sets in a function body is P's `==`
  function isEmpty (x : set[Int]) : Prop = (x = ∅)

  machine Node {
    var got  : Int
    var last : Int
    var seen : Int
    var mp   : map[Int, Int]
    var a    : set[Int]
    var b    : set[Int]
    var same : Bool
    var next : PLean.MachineRef

    start state A {
      on eGo goto B
      -- the parameter `last` shadows the variable `last`
      on eVal (last : Int) {
        seen = last
        last = 5
      }
      on eMap (p : tV) {
        mp += ((p.v, 1))
        mp += ((p.v, 2))
      }
      on eRead (k : Int) {
        seen = mp[k]
      }
      -- the read runs only when the guard holds, as P's `&&` short-circuits
      on eGuard (k : Int) {
        if k ∈ mp ∧ mp[k] = 1 then do
          seen = 1
      }
      on eRaise {
        a += (1)
        a += (2)
        b += (2)
        b += (1)
        same = (a == b)
        raise eIgn
      }
      ignore eIgn
    }

    state B {
      entry (p : tV) {
        got = p.v
      }
      ignore eGo, eVal, eMap, eRead, eIgn, eRaise, eGuard
    }
  }

  invariant sets_empty : ∀ n : Node, n.a = ∅
  invariant some_succ : ∃ n : Node, is_Node n.next s
end TransT

#gen_module TransT

namespace TransT

def w0 : PLean.World S E F G W :=
  { machines := fun r => if r = 1 then
      { «state» := some S.Node_A, fields := default, pending := none, waiting := none,
        halted := false } else default
    nextRef := 2, net := fun _ => none, clock := 0, error := none }

/-- Deliver `ev` to machine 1, then run its pending entry, if any. -/
def deliver (ev : E) : PLean.World S E F G W :=
  let w := { w0 with net := fun j => if j = 0 then some ⟨1, 1, ev⟩ else none, clock := 1 }
  let w := PLean.step prog w (.deliver 0) (fun _ => 0)
  PLean.step prog w (.control 1) (fun _ => 0)

-- `on eGo goto B` passes the payload to B's entry
#guard ((deliver (.eGo ⟨7⟩)).machines 1).fields.Node_got == 7
-- a parameter shadows the variable of the same name, and assigning it
-- leaves the variable alone
#guard ((deliver (.eVal 42)).machines 1).fields.Node_seen == 42
#guard ((deliver (.eVal 42)).machines 1).fields.Node_last == 0
-- `m += (k, v)` fails on a key already present
#guard (deliver (.eMap ⟨3⟩)).error == some .duplicateKey
-- `m[k]` fails on a missing key
#guard (deliver (.eRead 3)).error == some .keyNotFound
-- a guarded read of a missing key does not fail
#guard (deliver (.eGuard 3)).error == none
-- `==` on sets compares members; `raise` of an ignored event does nothing
#guard ((deliver .eRaise).machines 1).fields.Node_same == true
#guard (deliver .eRaise).error == none

end TransT

-- `=` between sets in a specification is P's `==`
/--
info: @[reducible] def TransT.sets_empty : TransT.GW → Prop :=
fun s ↦ ∀ (n : TransT.Node), TransT.is_Node n.ref s → (s.machines n.ref).fields.Node_a.Equiv ∅
-/
#guard_msgs in #print TransT.sets_empty

/--
info: def TransT.isEmpty : PSet Int → Prop :=
fun x ↦ x.Equiv ∅
-/
#guard_msgs in #print TransT.isEmpty

-- a kind guard about another term does not replace the binder's own
/--
info: @[reducible] def TransT.some_succ : TransT.GW → Prop :=
fun s ↦ ∃ n, TransT.is_Node n.ref s ∧ TransT.is_Node (s.machines n.ref).fields.Node_next s
-/
#guard_msgs in #print TransT.some_succ

/-! What P rejects. -/

pmodule GotoNoArg
  type tV = (v : Int)
  event eGo
  machine Node {
    start state A {
      on eGo {
        goto B
      }
    }
    state B {
      entry (p : tV) {
        assert p.v = p.v
      }
      ignore eGo
    }
  }
end GotoNoArg

/--
error: `goto B`: the entry of state `B` takes an argument; pass it with `goto B, e`
-/
#guard_msgs in #gen_module GotoNoArg

pmodule GotoNoPayload
  type tV = (v : Int)
  event eGo
  machine Node {
    start state A {
      on eGo goto B
    }
    state B {
      entry (p : tV) {
        assert p.v = p.v
      }
      ignore eGo
    }
  }
end GotoNoPayload

/--
error: `on eGo goto B`: the entry of state `B` takes an argument, but `eGo` has no payload
-/
#guard_msgs in #gen_module GotoNoPayload

pmodule RaiseUnhandled
  event eA
  event eB
  machine Node {
    start state A {
      on eA {
        raise eB
      }
      defer eB
    }
  }
end RaiseUnhandled

/--
error: `raise eB`: state `Node.A` neither handles nor ignores `eB` (in P, an unhandled-event error)
-/
#guard_msgs in #gen_module RaiseUnhandled

pmodule LetAcrossReceive
  event eReq
  event eResp
  machine Node {
    var total : Int
    start state A {
      entry {
        let k := 3
        receive {
          case eResp => {
            total = k
          }
        }
      }
      ignore eReq, eResp
    }
  }
end LetAcrossReceive

/--
error: `k` is bound by `let` before a `receive` and used after it, but only `var` locals and parameters are kept while the machine waits; declare it with `var k : T`
-/
#guard_msgs in #gen_module LetAcrossReceive

pmodule DecideEq
  event eA
  machine Node {
    var a : set[Int]
    var b : set[Int]
    var same : Bool
    start state A {
      on eA {
        if a = b then do
          same = true
      }
    }
  }
end DecideEq

/--
error: `=` between sets or maps compares their update histories; compare them with `==` (P's `==`, which compares their contents)
-/
#guard_msgs in #gen_module DecideEq

/-! A `receive` case's binder is scoped to its case. -/

pmodule CaseScope
  event eAck : Int
  machine Node {
    var v : Int
    var out : Int
    var first : Int
    start state Run {
      entry {
        first = v
        send this.ref, eAck, 42
        receive {
          case eAck (v : Int) => {
            out = v
          }
        }
        first = v + 1
      }
      ignore eAck
    }
  }
end CaseScope

#gen_module CaseScope

namespace CaseScope

def w0 : PLean.World S E F G W :=
  { machines := fun r => if r = 1 then
      { «state» := some S.Node_Run, fields := { (default : F) with Node_v := 5 },
        pending := some G.none, waiting := none, halted := false } else default
    nextRef := 2, net := fun _ => none, clock := 0, error := none }

def w2 := [PLean.Choice.control 1, .deliver 0].foldl (fun w c => PLean.step prog w c (fun _ => 0)) w0

-- inside the case, `v` is the payload; after it, the machine variable
#guard (w2.machines 1).fields.Node_out == 42
#guard (w2.machines 1).fields.Node_first == 6

end CaseScope

/-! Instances, functions and axioms are created in source order, instances first. -/

class HasNext (t : Type) where
  next : t → t

pmodule OrderT
  event ePing
  pinstance hn : HasNext Int
  paxiom next_ne : ∀ x : Int, HasNext.next x ≠ x
  machine Node {
    start state Run {
      ignore ePing
    }
  }
end OrderT

#guard_msgs in #gen_module OrderT

/-! A decided `=` between containers in a specification. -/

pmodule SpecDecide
  system s
  event ePing
  machine Node {
    var a : set[Int]
    start state Run {
      ignore ePing
    }
  }
  invariant small : ∀ n : Node, (if n.a = ∅ then 1 else 0) ≤ 1
end SpecDecide

/--
error: `=` between sets or maps compares their update histories; compare them with `==` (P's `==`, which compares their contents)
-/
#guard_msgs in #gen_module SpecDecide

/-! A pattern `let` before a `receive`. -/

pmodule LetPattern
  event eResp
  machine Node {
    var total : Int
    start state A {
      entry {
        let (a, b) := (1, 2)
        total = a
        receive {
          case eResp => {
            total = b
          }
        }
      }
      ignore eResp
    }
  }
end LetPattern

/--
error: `b` is bound by `let` before a `receive` and used after it, but only `var` locals and parameters are kept while the machine waits; declare it with `var b : T`
-/
#guard_msgs in #gen_module LetPattern
