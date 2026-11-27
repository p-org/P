/-
PLean.Verify.Progress — `#pverify`'s live progress in the editor.

`#pverify` shows a single message whose widget polls `pverifyProgress`
and re-renders, so the progress is updated in place: how many obligations
are done, the one being proved, and the goal and solver `pverify_close` is
working on. The state is a process-wide store, shared by the elaborator
and the RPC handler (both run in the file worker); nothing is recorded
when no `#pverify` reports progress (on the command line).
-/
import Lean

namespace PLean.Progress

open Lean Server

/-- The progress of one `#pverify` run. -/
structure Entry where
  /-- What is done. -/
  status : String := ""
  /-- What is being proved. -/
  task : String := ""
  /-- What `pverify_close` is doing. -/
  detail : String := ""
  startMs : Nat := 0
  /-- When the run finished. -/
  endMs : Option Nat := none

initialize store : IO.Ref (Std.HashMap Nat Entry) ← IO.mkRef {}
initialize nextId : IO.Ref Nat ← IO.mkRef 0
/-- The run reporting progress, if any. -/
initialize current : IO.Ref (Option Nat) ← IO.mkRef none
/-- Whether `setDetail` reports (off while obligations run in parallel, where
each would overwrite the others' detail). -/
initialize details : IO.Ref Bool ← IO.mkRef true

/-- Start a run; returns its id. -/
def start (status : String) : BaseIO Nat := do
  let id ← nextId.modifyGet fun n => (n, n + 1)
  store.modify (·.insert id { status, startMs := ← IO.monoMsNow })
  current.set (some id)
  return id

/-- Set what is done and what is being proved. -/
def setHead (status task : String) : BaseIO Unit := do
  if let some id ← current.get then
    store.modify fun m => m.modify id ({ · with status, task, detail := "" })

/-- Set what `pverify_close` is doing. -/
def setDetail (detail : String) : BaseIO Unit := do
  unless ← details.get do return
  if let some id ← current.get then
    store.modify fun m => m.modify id ({ · with detail })

/-- Finish the run, leaving `status`. -/
def finish (status : String) : BaseIO Unit := do
  if let some id ← current.get then
    let now ← IO.monoMsNow
    store.modify fun m => m.modify id ({ · with status, task := "", detail := "", endMs := some now })
    current.set none

/-- `n` seconds as `1 min 5 s`. -/
def duration (n : Nat) : String :=
  if n < 60 then s!"{n} s" else s!"{n / 60} min {n % 60} s"

structure Params where
  id : Nat
  deriving FromJson, ToJson

structure Status where
  text : String
  done : Bool
  deriving FromJson, ToJson

/-- The progress of run `id`. -/
@[server_rpc_method]
def pverifyProgress (p : Params) : RequestM (RequestTask Status) := do
  let some e := (← (store.get : BaseIO _))[p.id]? | return RequestTask.pure { text := "", done := true }
  let now ← (IO.monoMsNow : BaseIO _)
  let lines := [s!"{e.status} · {duration ((e.endMs.getD now - e.startMs) / 1000)}", e.task,
    if e.detail.isEmpty then "" else "  " ++ e.detail].filter (!·.isEmpty)
  return RequestTask.pure { text := "\n".intercalate lines, done := e.endMs.isSome }

/-- Shows `pverifyProgress`, polling it until the run is done. -/
@[widget_module]
def widget : Widget.Module where
  javascript := "
import * as React from 'react';
import { useRpcSession } from '@leanprover/infoview';

export default function PverifyProgress(props) {
  const rs = useRpcSession();
  const [st, setSt] = React.useState({ text: props.text, done: false });
  React.useEffect(() => {
    let alive = true;
    let timer = null;
    const poll = () => {
      rs.call('PLean.Progress.pverifyProgress', { id: props.id })
        .then(r => {
          if (!alive) return;
          setSt(r);
          if (!r.done) timer = setTimeout(poll, 500);
        })
        .catch(() => { if (alive) timer = setTimeout(poll, 2000); });
    };
    poll();
    return () => { alive = false; if (timer) clearTimeout(timer); };
  }, [props.id]);
  return React.createElement('span', { style: { whiteSpace: 'pre-wrap' } }, st.text);
}
"

/-- The progress message of run `id`. -/
def message (id : Nat) (text : String) : CoreM MessageData := do
  let wi ← Widget.WidgetInstance.ofHash widget.javascriptHash
    (pure (Json.mkObj [("id", toJson id), ("text", toJson text)]))
  return .ofWidget wi text

end PLean.Progress
