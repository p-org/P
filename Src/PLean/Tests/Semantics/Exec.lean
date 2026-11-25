/-
The model is executable: run a ping-pong program on a concrete world.
-/
import PLean

open PLean

pmodule ExecT

  type tPing = (src : PLean.MachineRef)

  event ePing : tPing
  event ePong

  machine Pinger {
    var target : PLean.MachineRef
    var pongs  : Int

    start state Run {
      entry {
        send target, ePing, (src = this.ref)
      }

      on ePong {
        pongs = pongs + 1
        send target, ePing, (src = this.ref)
      }
    }
  }

  machine Ponger {
    start state Serve {
      on ePing (p : tPing) {
        send p.src, ePong
      }
    }
  }

end ExecT

#gen_module ExecT

namespace ExecT

/-- Pinger 0 and Ponger 1, both with their start entry pending. -/
def w0 : PLean.World S E F G W :=
  { machines := fun r =>
      if r = 0 then
        { «state» := some S.Pinger_Run, fields := { (default : F) with Pinger_target := 1 },
          pending := some G.none, waiting := none, halted := false }
      else if r = 1 then
        { «state» := some S.Ponger_Serve, fields := default, pending := some G.none,
          waiting := none, halted := false }
      else default
    nextRef := 2, net := fun _ => none, clock := 0, error := none }

def run (w : PLean.World S E F G W) (cs : List PLean.Choice) : PLean.World S E F G W :=
  cs.foldl (fun w c => PLean.step prog w c (fun _ => 0)) w

-- The Pinger's entry pings; the Ponger answers; the Pinger counts the pong
-- and pings again.
def w4 := run w0 [.control 0, .control 1, .deliver 0, .deliver 1]

#guard (w4.machines 0).fields.Pinger_pongs == 1
#guard w4.clock == 3
#guard (w4.net 2).isSome
#guard (w4.net 0).isNone && (w4.net 1).isNone
#guard w4.error.isNone

-- A machine with a pending entry receives nothing until the entry ran.
#guard !(prog.enabled .bag (run w0 [.control 0]) (.deliver 0))
#guard prog.enabled .bag (run w0 [.control 0, .control 1]) (.deliver 0)

end ExecT
