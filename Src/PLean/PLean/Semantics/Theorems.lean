/-
PLean.Semantics.Theorems — generic metatheory of the transition model,
proved once for every program:

* `triple_run_ok`: a handler triple with exception postcondition `False`
  means the handler runs to completion and its postcondition holds.
* `framework_gstep`: every step preserves `Framework`.
* `step_ok`: "every step preserves `Post`" reduces to one obligation per
  case (deliver / resume / entry / drop).
* `safety_of_step`: an inductive invariant holds on every reachable world.
* `GReachable.fifo_bag`, `GReachable.erase`, `Reachable.lift`: FIFO runs
  are bag runs, and ghost runs project onto real runs and back.
-/
import PLean.Semantics.World

namespace PLean

open Std.WP

variable {S E F G W : Type}

/-! ## Adequacy -/

theorem triple_run_ok {act : HandlerM S E F G W Unit} {hs0 : HState S E F G W}
    {Q : Unit → HState S E F G W → Prop}
    (h : Triple act (fun hs => hs = hs0) Q (fun _ => False)) :
    ∃ hs, act.run hs0 = .ok ((), hs) ∧ Q () hs := by
  have h1 : wp act Q (fun _ => False) hs0 := h.le_wp hs0 rfl
  rw [StateT.wp_apply_eq] at h1
  revert h1
  cases act.run hs0 with
  | ok r => intro h1; exact ⟨r.2, rfl, h1⟩
  | error e => intro h1; exact h1.elim

section
variable [Inhabited F]

/-- The obligation for running `act` as machine `m`, with local state `ms`,
on `g`: it never errs, and `Post` holds once its effects are committed. -/
def RunOK (Post : GWorld S E F G W → Prop) (g : GWorld S E F G W) (m : MachineRef)
    (ms : MachineState S E F G W) (act : HandlerM S E F G W Unit) : Prop :=
  ∀ coins, Triple act (fun hs => hs = HState.start g.toWorld m ms coins)
    (fun _ hs => Post (gcommit g m hs)) (fun _ => False)

theorem triple_of_run {act : HandlerM S E F G W Unit} {hs0 : HState S E F G W}
    {Q : Unit → HState S E F G W → Prop} (hs : HState S E F G W)
    (hrun : act.run hs0 = .ok ((), hs)) (hq : Q () hs) :
    Triple act (fun hs => hs = hs0) Q (fun _ => False) := by
  constructor
  intro s hs'
  subst hs'
  show wp act Q (fun _ => False) s
  rw [StateT.wp_apply_eq, hrun]
  exact hq

/-- Two handler obligations about the same run combine. -/
theorem RunOK.and {P Q : GWorld S E F G W → Prop} {g : GWorld S E F G W} {m ms act}
    (h1 : RunOK P g m ms act) (h2 : RunOK Q g m ms act) :
    RunOK (fun g' => P g' ∧ Q g') g m ms act := by
  intro coins
  obtain ⟨hs, hrun, hp⟩ := triple_run_ok (h1 coins)
  obtain ⟨hs', hrun', hq⟩ := triple_run_ok (h2 coins)
  rw [hrun] at hrun'
  cases hrun'
  exact triple_of_run hs hrun ⟨hp, hq⟩

theorem RunOK.mono {P Q : GWorld S E F G W → Prop} {g : GWorld S E F G W} {m ms act}
    (hpq : ∀ g', P g' → Q g') (h : RunOK P g m ms act) : RunOK Q g m ms act := by
  intro coins
  obtain ⟨hs, hrun, hp⟩ := triple_run_ok (h coins)
  exact triple_of_run hs hrun (hpq _ hp)

theorem RunOK.run {Post : GWorld S E F G W → Prop} {g : GWorld S E F G W} {m ms act}
    (h : RunOK Post g m ms act) (coins : Nat → Nat) :
    ∃ hs, g.toWorld.run m ms coins act = (commit g.toWorld m hs, some hs) ∧
      Post (gcommit g m hs) := by
  obtain ⟨hs, hrun, hpost⟩ := triple_run_ok (h coins)
  exact ⟨hs, by simp [World.run, hrun], hpost⟩

/-! ## Framework -/

omit [Inhabited F] in
/-- An in-flight packet is in the ghost log. -/
theorem Framework.log_of_net {g : GWorld S E F G W} (hF : Framework g) {i : Nat} {p : Packet E}
    (h : g.net i = some p) : g.log i = some p := hF.2.2.1 i p h

/-- An in-flight packet's id is below the clock. -/
theorem Framework.lt_clock_of_net {g : GWorld S E F G W} (hF : Framework g) {i : Nat}
    {p : Packet E} (h : g.net i = some p) : i < g.clock := by
  rcases Nat.lt_or_ge i g.clock with h' | h'
  · exact h'
  · rw [hF.1 i h'] at h; cases h

theorem framework_consume {g : GWorld S E F G W} (i : Nat) (hF : Framework g) :
    Framework (g.consume i) := by
  obtain ⟨h1, h2, h3, h4⟩ := hF
  refine ⟨fun j hj => ?_, h2, fun j p hj => ?_, h4⟩
  · show (if j = i then none else g.net j) = none
    split
    · rfl
    · exact h1 j hj
  · change (if j = i then none else g.net j) = some p at hj
    split at hj
    · cases hj
    · exact h3 j p hj

theorem framework_gcommit {g : GWorld S E F G W} {m : MachineRef} {hs : HState S E F G W}
    (hF : Framework g) (hm : (g.machines m).state.isSome = true) :
    Framework (gcommit g m hs) := by
  obtain ⟨h1, h2, h3, h4⟩ := hF
  have hmlt : m < g.nextRef := by
    rcases Nat.lt_or_ge m g.nextRef with h | h
    · exact h
    · have := h4 m h; simp [this] at hm
  have hc1 := Nat.le_max_left g.clock hs.out.size
  have hc2 := Nat.le_max_right g.clock hs.out.size
  have hn1 := Nat.le_max_left g.nextRef hs.spawn.size
  have hn2 := Nat.le_max_right g.nextRef hs.spawn.size
  refine ⟨fun j hj => ?_, fun j hj => ?_, fun j p hj => ?_, fun r hr => ?_⟩
  · simp only [gcommit, commit] at hj ⊢
    have ha := Nat.le_trans hc1 hj
    have hb := Nat.le_trans hc2 hj
    have : ¬ (g.clock ≤ j ∧ j < hs.out.size) := by omega
    simp only [this, ite_false]; exact h1 j ha
  · simp only [gcommit, logAfter, commit] at hj ⊢
    have ha := Nat.le_trans hc1 hj
    have hb := Nat.le_trans hc2 hj
    have : ¬ (g.clock ≤ j ∧ j < hs.out.size) := by omega
    simp only [this, ite_false]; exact h2 j ha
  · simp only [gcommit, logAfter, commit] at hj ⊢
    split at hj
    · rw [ite_eq_left (by assumption)]; exact hj
    · rw [ite_eq_right (by assumption)]; exact h3 j p hj
  · simp only [gcommit, commit] at hr ⊢
    have ha := Nat.le_trans hn1 hr
    have hb := Nat.le_trans hn2 hr
    have hne : r ≠ m := fun h => by
      rw [h] at ha; exact Nat.lt_irrefl _ (Nat.lt_of_lt_of_le hmlt ha)
    have : ¬ (g.nextRef ≤ r ∧ r < hs.spawn.size) := fun ⟨_, h⟩ =>
      Nat.lt_irrefl _ (Nat.lt_of_lt_of_le h hb)
    simp only [hne, this, ite_false]; exact h4 r ha

end

/-! ## `gstep` in each case -/

section
variable [Inhabited F] (prog : Program S E F G W) (g : GWorld S E F G W) (coins : Nat → Nat)

/-- The ghost world after running `act` as `m` on `g` (the run's world plus
the log update). -/
def GWorld.runOn (m : MachineRef) (ms : MachineState S E F G W) (act : HandlerM S E F G W Unit) :
    GWorld S E F G W :=
  let r := g.toWorld.run m ms coins act
  { r.1 with log := match r.2 with
                    | some hs => logAfter g.log g.clock hs
                    | none => g.log }

theorem GWorld.runOn_of_RunOK {Post : GWorld S E F G W → Prop} {m ms act}
    (h : RunOK Post g m ms act) :
    (g.runOn coins m ms act).error = g.error ∧ Post (g.runOn coins m ms act) := by
  obtain ⟨hs, hrun, hpost⟩ := h.run coins
  have : g.runOn coins m ms act = gcommit g m hs := by
    simp only [GWorld.runOn, hrun]; rfl
  rw [this]; exact ⟨rfl, hpost⟩

theorem gstep_deliver_drop {i p} (hnet : g.net i = some p)
    (hh : (g.machines p.dst).halted = true) :
    gstep prog g (.deliver i) coins = g.consume i := by
  simp only [gstep, stepOut, hnet, hh, ite_true]; rfl

theorem gstep_deliver_handler {i p s act} (hnet : g.net i = some p)
    (hh : (g.machines p.dst).halted = false) (hw : (g.machines p.dst).waiting = none)
    (hs : (g.machines p.dst).state = some s) (ha : prog.handler s p.ev = some act) :
    gstep prog g (.deliver i) coins = (g.consume i).runOn coins p.dst (g.machines p.dst) act := by
  simp only [gstep, stepOut, hnet, hh, hw, hs, ha]; rfl

theorem gstep_deliver_resume {i p k act} (hnet : g.net i = some p)
    (hh : (g.machines p.dst).halted = false) (hw : (g.machines p.dst).waiting = some k)
    (ha : prog.resume k p.ev = some act) :
    gstep prog g (.deliver i) coins =
      (g.consume i).runOn coins p.dst { g.machines p.dst with waiting := none } act := by
  simp only [gstep, stepOut, hnet, hh, hw, ha]; rfl

theorem gstep_control_entry {m s arg} (hp : (g.machines m).pending = some arg)
    (hs : (g.machines m).state = some s) :
    gstep prog g (.control m) coins =
      g.runOn coins m { g.machines m with pending := none } (prog.entry s arg) := by
  simp only [gstep, stepOut, hp, hs]; rfl

end

/-! ## Framework preservation -/

section
variable [Inhabited F] {prog : Program S E F G W}

theorem framework_runOn {g : GWorld S E F G W} {m ms act} (coins : Nat → Nat)
    (hF : Framework g) (hm : (g.machines m).state.isSome = true) :
    Framework (g.runOn coins m ms act) := by
  unfold GWorld.runOn World.run
  split
  · rename_i hs _
    show Framework (gcommit g m hs)
    exact framework_gcommit hF hm
  · exact hF

theorem framework_gstep {g : GWorld S E F G W} {c : Choice} (coins : Nat → Nat)
    (hF : Framework g) (hen : prog.enabled .bag g.toWorld c = true) :
    Framework (gstep prog g c coins) := by
  cases c with
  | deliver i =>
    simp only [Program.enabled, Program.deliverable, Bool.and_eq_true] at hen
    obtain ⟨⟨_, hdel⟩, -⟩ := hen
    cases hnet : g.net i with
    | none => simp [hnet] at hdel
    | some p =>
      simp only [hnet, Bool.and_eq_true] at hdel
      obtain ⟨⟨hst, _⟩, _⟩ := hdel
      have hFc := framework_consume i hF
      unfold gstep stepOut
      simp only [hnet]
      split
      · exact hFc
      · split
        · split
          · exact framework_runOn coins hFc hst
          · exact hFc
        · split
          · split
            · exact framework_runOn coins hFc hst
            · exact hFc
          · exact hFc
  | control m =>
    simp only [Program.enabled, Bool.and_eq_true] at hen
    obtain ⟨⟨⟨_, hst⟩, _⟩, _⟩ := hen
    dsimp only [gstep, stepOut]
    split
    · exact framework_runOn coins hF hst
    · exact hF

end

/-! ## Per-case obligations -/

section
variable [Inhabited F]

/-- "Every enabled step preserves `Post` without error" reduces to one
obligation per case of the semantics. -/
theorem step_ok (prog : Program S E F G W) (Pre Post : GWorld S E F G W → Prop)
    (hdeliver : ∀ g i p s, Framework g → g.error = none → Pre g → g.net i = some p →
      (g.machines p.dst).state = some s → (g.machines p.dst).pending = none →
      (g.machines p.dst).waiting = none → (g.machines p.dst).halted = false →
      prog.deferred s p.ev = false →
      ∃ act, prog.handler s p.ev = some act ∧
        RunOK Post (g.consume i) p.dst (g.machines p.dst) act)
    (hresume : ∀ g i p k, Framework g → g.error = none → Pre g → g.net i = some p →
      (g.machines p.dst).pending = none → (g.machines p.dst).waiting = some k →
      (g.machines p.dst).halted = false → ∀ act, prog.resume k p.ev = some act →
      RunOK Post (g.consume i) p.dst { g.machines p.dst with waiting := none } act)
    (hentry : ∀ g m s arg, Framework g → g.error = none → Pre g →
      (g.machines m).state = some s → (g.machines m).pending = some arg →
      (g.machines m).halted = false →
      RunOK Post g m { g.machines m with pending := none } (prog.entry s arg))
    (hdrop : ∀ g i p, Framework g → g.error = none → Pre g → g.net i = some p →
      (g.machines p.dst).halted = true → Post (g.consume i)) :
    ∀ g c coins, Framework g → g.error = none → Pre g → prog.enabled .bag g.toWorld c = true →
      (gstep prog g c coins).error = none ∧ Post (gstep prog g c coins) := by
  intro g c coins hF herr hpre hen
  cases c with
  | deliver i =>
    simp only [Program.enabled, Program.deliverable, Bool.and_eq_true] at hen
    obtain ⟨⟨_, hdel⟩, -⟩ := hen
    cases hnet : g.net i with
    | none => simp [hnet] at hdel
    | some p =>
      simp only [hnet, Bool.and_eq_true] at hdel
      obtain ⟨⟨hst, hpend⟩, hacc⟩ := hdel
      have hpend' : (g.machines p.dst).pending = none := Option.isNone_iff_eq_none.mp hpend
      cases hh : (g.machines p.dst).halted
      · cases hw : (g.machines p.dst).waiting with
        | some k =>
          simp only [Program.accepts, hh, hw, Bool.false_or] at hacc
          cases hr : prog.resume k p.ev with
          | none => simp [hr] at hacc
          | some act =>
            rw [gstep_deliver_resume prog g coins hnet hh hw hr]
            obtain ⟨he, hp⟩ := (g.consume i).runOn_of_RunOK coins
              (hresume g i p k hF herr hpre hnet hpend' hw hh act hr)
            exact ⟨he.trans herr, hp⟩
        | none =>
          cases hs : (g.machines p.dst).state with
          | none => simp [hs] at hst
          | some s =>
            simp only [Program.accepts, hh, hw, hs, Bool.false_or, Bool.not_eq_eq_eq_not,
              Bool.not_true] at hacc
            obtain ⟨act, ha, hok⟩ := hdeliver g i p s hF herr hpre hnet hs hpend' hw hh hacc
            rw [gstep_deliver_handler prog g coins hnet hh hw hs ha]
            obtain ⟨he, hp⟩ := (g.consume i).runOn_of_RunOK coins hok
            exact ⟨he.trans herr, hp⟩
      · rw [gstep_deliver_drop prog g coins hnet hh]
        exact ⟨herr, hdrop g i p hF herr hpre hnet hh⟩
  | control m =>
    simp only [Program.enabled, Bool.and_eq_true, Bool.not_eq_eq_eq_not, Bool.not_true] at hen
    obtain ⟨⟨⟨_, hst⟩, hpend⟩, hh⟩ := hen
    cases hp : (g.machines m).pending with
    | none => simp [hp] at hpend
    | some arg =>
      cases hs : (g.machines m).state with
      | none => simp [hs] at hst
      | some s =>
        rw [gstep_control_entry prog g coins hp hs]
        obtain ⟨he, hpost⟩ := g.runOn_of_RunOK coins (hentry g m s arg hF herr hpre hs hp hh)
        exact ⟨he.trans herr, hpost⟩

/-- An inductive invariant holds, without errors, on every reachable world. -/
theorem safety_of_step (prog : Program S E F G W) (Init Inv : GWorld S E F G W → Prop)
    (hinit : ∀ g, Init g → Framework g ∧ g.error = none ∧ Inv g)
    (hstep : ∀ g c coins, Framework g → g.error = none → Inv g →
      prog.enabled .bag g.toWorld c = true →
      (gstep prog g c coins).error = none ∧ Inv (gstep prog g c coins)) :
    ∀ g, GReachable prog .bag Init g → Framework g ∧ g.error = none ∧ Inv g := by
  intro g hg
  induction hg with
  | init hi => exact hinit _ hi
  | step _ hen ih =>
    obtain ⟨hF, herr, hInv⟩ := ih
    obtain ⟨herr', hInv'⟩ := hstep _ _ _ hF herr hInv hen
    exact ⟨framework_gstep _ hF hen, herr', hInv'⟩

/-! ## FIFO runs are bag runs; ghost runs and real runs correspond -/

theorem enabled_fifo_bag {prog : Program S E F G W} {w : World S E F G W} {c : Choice}
    (h : prog.enabled .fifo w c = true) : prog.enabled .bag w c = true := by
  cases c with
  | deliver i =>
    simp only [Program.enabled, Bool.and_eq_true] at h ⊢
    exact ⟨h.1, by simp⟩
  | control m => exact h

theorem GReachable.fifo_bag {prog : Program S E F G W} {Init} {g : GWorld S E F G W}
    (h : GReachable prog .fifo Init g) : GReachable prog .bag Init g := by
  induction h with
  | init hi => exact .init hi
  | step _ hen ih => exact .step ih (enabled_fifo_bag hen)

theorem Reachable.fifo_bag {prog : Program S E F G W} {Init} {w : World S E F G W}
    (h : Reachable prog .fifo Init w) : Reachable prog .bag Init w := by
  induction h with
  | init hi => exact .init hi
  | step _ hen ih => exact .step ih (enabled_fifo_bag hen)

@[simp] theorem gstep_toWorld (prog : Program S E F G W) (g : GWorld S E F G W) (c : Choice)
    (coins : Nat → Nat) : (gstep prog g c coins).toWorld = step prog g.toWorld c coins := rfl

/-- Every ghost run erases to a real run. -/
theorem GReachable.erase {prog : Program S E F G W} {sem Init} {g : GWorld S E F G W}
    (h : GReachable prog sem Init g) :
    Reachable prog sem (fun w => ∃ g0, Init g0 ∧ g0.toWorld = w) g.toWorld := by
  induction h with
  | init hi => exact .init ⟨_, hi, rfl⟩
  | step _ hen ih => rw [gstep_toWorld]; exact .step ih hen

/-- Every real run lifts to a ghost run. -/
theorem Reachable.lift {prog : Program S E F G W} {sem} {Init : GWorld S E F G W → Prop}
    {w : World S E F G W}
    (h : Reachable prog sem (fun w => ∃ g0, Init g0 ∧ g0.toWorld = w) w) :
    ∃ g, GReachable prog sem Init g ∧ g.toWorld = w := by
  induction h with
  | init hi => obtain ⟨g0, hi, rfl⟩ := hi; exact ⟨g0, .init hi, rfl⟩
  | step _ hen ih =>
    obtain ⟨g, hg, rfl⟩ := ih
    exact ⟨gstep prog g _ _, .step hg hen, rfl⟩

end

end PLean
