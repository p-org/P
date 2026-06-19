#!/usr/bin/env python3
"""
invariant_core.py — engine-agnostic shared core for agentic invariant learning.

This is the reusable heart of the PInfer agentic workflow, with NO dependency on
PeasyAI or Claude Code. Both wrappers build on it:
  * the PeasyAI MCP tool  (Src/PeasyAI/.../tools/invariants.py) drives it via PeasyAI's LLM,
  * the Claude Code skill  (.claude/skills/learn-invariants) drives it via agents.

It provides two things:
  1. DETERMINISTIC VALIDATION — wire candidate spec monitors into a P project, compile,
     bounded-model-check, classify (HOLDS / FAILS+cex / VACUOUS / INCONCLUSIVE / COMPILE-ERR),
     with vacuity canaries and detection of pre-existing SUT failures. No LLM.
  2. PROMPT BUILDERS — the template (Specy G→W∧H shapes) + intent-lens proposer prompts, the
     judge rubric (Specy Table 3), and the P-syntax repair rules. Engine-agnostic strings.

`check_candidates.py` is a thin CLI over the validation half of this module.
"""
from __future__ import annotations

import glob
import os
import re
import shutil
import subprocess
from dataclasses import dataclass, field
from pathlib import Path
from typing import Dict, List, Optional, Tuple

SPEC_RE = re.compile(r"^\s*spec\s+([A-Za-z_]\w*)", re.MULTILINE)
BUG_RE = re.compile(r"Found (\d+) bug")
FAIL_RE = re.compile(r"(Assertion Failed:.*|detected liveness bug.*|Deadlock detected.*)")


# ───────────────────────────── environment ──────────────────────────────

def build_env() -> Dict[str, str]:
    """Env where `dotnet` and the `p` global tool resolve. Prefers a working
    PATH/DOTNET_ROOT; else probes common install dirs (Linux + macOS/homebrew)."""
    env = dict(os.environ)
    tools = str(Path.home() / ".dotnet" / "tools")
    if tools not in env.get("PATH", ""):
        env["PATH"] = tools + os.pathsep + env.get("PATH", "")
    if shutil.which("dotnet", path=env["PATH"]):
        return env
    probes = ([env["DOTNET_ROOT"]] if env.get("DOTNET_ROOT") else []) + [
        "/usr/share/dotnet", "/usr/local/share/dotnet", str(Path.home() / ".dotnet"),
    ] + sorted(glob.glob("/opt/homebrew/Cellar/dotnet@*/*/libexec"))
    for root in probes:
        if Path(root, "dotnet").exists() or Path(root, "host").is_dir():
            env["DOTNET_ROOT"] = root
            env["PATH"] = root + os.pathsep + env["PATH"]
            return env
    return env


# ───────────────────────────── data model ───────────────────────────────

@dataclass
class Verdict:
    name: str
    verdict: str                 # HOLDS | FAILS | VACUOUS | INCONCLUSIVE | COMPILE-ERR
    detail: str = ""
    bugs: int = 0
    owner: str = "monitor"       # monitor | sut


# ─────────────────────── deterministic validation ───────────────────────

def split_specs(candidates_src: str) -> Dict[str, str]:
    """Map each `spec <Name> ... { ... }` block to its source text."""
    spans = [(m.start(), m.group(1)) for m in SPEC_RE.finditer(candidates_src)]
    spans.append((len(candidates_src), None))
    return {spans[i][1]: candidates_src[spans[i][0]:spans[i + 1][0]]
            for i in range(len(spans) - 1)}


def _wire(project: Path, names: List[str], block_of: Dict[str, str],
          main: str, assert_in: str, env) -> bool:
    """Write the given specs + one test each into the project; return compile success."""
    (project / "PSpec" / "_candidates.p").write_text("\n".join(block_of[n] for n in names))
    (project / "PTst" / "_candidate_tests.p").write_text(
        "// AUTO-GENERATED\n\n" + "\n".join(
            f"test tc_{n} [main={main}]:\n  assert {n} in {assert_in};\n" for n in names))
    out = subprocess.run(["p", "compile"], cwd=project, env=env,
                         capture_output=True, text=True).stdout
    return "Compilation succeeded" in out


def _check_one(project: Path, name: str, iters: int, env) -> Tuple[int, str, str]:
    """Run `p check` for one wired candidate; return (bugs, cex, owner)."""
    bf = project / "PCheckerOutput" / "BugFinding"
    for f in glob.glob(str(bf / "*_0_*.txt")):
        os.remove(f)
    out = subprocess.run(["p", "check", "-tc", f"tc_{name}", "-i", str(iters)],
                        cwd=project, env=env, capture_output=True, text=True).stdout
    m = BUG_RE.search(out)
    bugs = int(m.group(1)) if m else -1
    cex, owner = "", "monitor"
    if bugs > 0:
        tfs = sorted(glob.glob(str(bf / "*_0_*.txt")), key=os.path.getmtime)
        if tfs:
            fm = FAIL_RE.search(Path(tfs[-1]).read_text())
            cex = (fm.group(1) if fm else "").strip()[:160]
            # Did THIS candidate's monitor fail, or a pre-existing SUT assertion/deadlock?
            owner = "monitor" if ("_candidates.p" in cex or "liveness" in cex
                                  or "hot state" in cex) else "sut"
    return bugs, cex, owner


def validate_candidates(project_path: str, candidates_src: str, main: str,
                        assert_in: str, iters: int = 2000,
                        keep: bool = False) -> List[Verdict]:
    """Wire, compile, bounded-check and classify every candidate spec.

    A `<Name>_canary` block is treated as a vacuity probe for `<Name>`: if both
    `<Name>` and `<Name>_canary` HOLD, the canary's guarded branch is unreachable,
    so `<Name>` is VACUOUS. Candidates whose model-check failure is owned by a
    pre-existing SUT assertion are INCONCLUSIVE (the candidate was never tested).
    """
    project = Path(project_path).resolve()
    env = build_env()
    block_of = split_specs(candidates_src)
    names = list(block_of)
    reals = [n for n in names if not n.endswith("_canary")]
    canaries = {n[:-7] for n in names if n.endswith("_canary")}

    results: Dict[str, Tuple[int, str, str]] = {}
    compile_err: set = set()
    try:
        if _wire(project, names, block_of, main, assert_in, env):       # fast path
            for n in names:
                results[n] = _check_one(project, n, iters, env)
        else:                                                            # robust: isolate
            for n in names:
                if _wire(project, [n], block_of, main, assert_in, env):
                    results[n] = _check_one(project, n, iters, env)
                else:
                    compile_err.add(n)
                    results[n] = (-1, "COMPILE-ERROR", "monitor")
    finally:
        if not keep:
            (project / "PSpec" / "_candidates.p").unlink(missing_ok=True)
            (project / "PTst" / "_candidate_tests.p").unlink(missing_ok=True)

    out: List[Verdict] = []
    for n in reals:
        bugs, cex, owner = results[n]
        if n in compile_err:
            out.append(Verdict(n, "COMPILE-ERR", "candidate did not compile (dropped)", bugs, owner))
        elif bugs > 0 and owner == "sut":
            out.append(Verdict(n, "INCONCLUSIVE", f"SUT assertion fired first: {cex}", bugs, owner))
        elif bugs > 0:
            out.append(Verdict(n, "FAILS", cex, bugs, owner))
        elif n in canaries and results.get(f"{n}_canary", (1,))[0] == 0:
            out.append(Verdict(n, "VACUOUS", "guarded branch never reached (canary did not trip)", 0, owner))
        else:
            out.append(Verdict(n, "HOLDS", "non-vacuous" if n in canaries else "", 0, owner))
    return out


# ───────────────────────── prompt builders (LLM) ─────────────────────────
# Engine-agnostic strings: the PeasyAI MCP tool and the Claude Code skill both reuse these.

P_SPEC_RULES = """P SPEC MONITOR SYNTAX RULES:
- NO inline var init (`var x:T = e;`): declare `var x:T;` at the TOP of a block, assign `x = e;` after.
- ALL var decls come before any statement in their block. No ternary `?:`. No `not`/`not in` (use `!`, `!(x in s)`).
- format strings use positional args: `format("...{0}", x)`. assert: `assert <bool>, format(...);`.
- State names unique within a spec. No `is` keyword. `foreach` not `for`. keys()/values() return seq, not set.
- Build sets with `s += (elem)`; remove with `-=`. Cast Node to machine via `(x as machine)` for set[machine] membership.
- EVERY observed event handled in EVERY state. Use a `hot state` only for genuine liveness."""

TEMPLATE_GRAMMAR = """PInfer/Specy template grammar (the search space you instantiate):
    forall e1:E1,...,eA:EA :: Guards(e) => exists* f1:F1,... :: Witness /\\ Hypothesis
TERMS = payload-field accesses up to depth 2 (e.g. e1.trial, e2.node); PREDICATES = ==,!=,<,<=,>,>=, in.
GUARDS constrain which event tuples the property ranges over; HYPOTHESIS is what must then hold.
ARITY A = #forall events; EXISTS = #existentials (0 = pure universal)."""

TEMPLATE_SHAPES = [
    ("arity1", "ARITY=1, EXISTS=0. forall e:E :: P(e.fields). Single-event domain/validity invariants over EACH event."),
    ("arity2-same", "ARITY=2 same type, guarded by a key equality. Monotonicity / uniqueness / no-conflict."),
    ("arity2-cross", "ARITY=2 different types, guarded by a cross-event key equality. Agreement / causality / consistency."),
    ("exists", "forall e :: G => exists f :: f.id==e.id /\\ R. Completeness / response / justification (encode as outstanding-fact tracking)."),
]

INTENT_LENSES = [
    ("agreement", "SAFETY / AGREEMENT / ATOMICITY: a decision is justified by prior events; no contradictory outcomes; all-or-nothing."),
    ("liveness", "LIVENESS / PROGRESS: every request/round is eventually answered (hot-state monitors)."),
    ("consistency", "CONSISTENCY / ORDERING / CAUSALITY: read-your-writes, monotonicity, response-preceded-by-request."),
    ("validity", "VALIDITY / WELL-FORMEDNESS / UNIQUENESS: payload/status domains, unique ids, non-empty sets; include one deliberately-vacuous candidate + a `<Name>_canary` companion that asserts false in the same branch."),
]


def propose_system_prompt() -> str:
    return ("You are the PROPOSER stage of an agentic invariant-mining pipeline for the P language. "
            "Derive intent-level correctness specifications a protocol SHOULD satisfy, and encode each "
            "as a P `spec` monitor. Derive from INTENT, not from the implementation — mirroring the code "
            "turns a bug into a 'correct' invariant. " + P_SPEC_RULES)


def propose_user_prompt(benchmark: str, project_dir: str, events: str,
                        prior_kind: str, prior_focus: str, design_doc: str = "") -> str:
    doc = f"\nDESIGN INTENT:\n{design_doc}\n" if design_doc else ""
    return (f"TARGET: {benchmark} at {project_dir}. Read its PSrc/*.p and PTst/*.p for exact event/payload "
            f"names.{doc}\nEvent signatures:\n{events}\n\n{TEMPLATE_GRAMMAR}\n\nYOUR PRIOR ({prior_kind}): "
            f"{prior_focus}\n\nProduce 3-6 candidates with UNIQUE names prefixed '{benchmark}_{prior_kind}_'. "
            "Return a JSON array; each item: {\"name\",\"intent\",\"predictedBucket\"(verified|bug|spurious|vacuous),"
            "\"observes\":[...],\"specCode\":\"<compilable P spec monitor>\"}.")


JUDGE_RUBRIC = (
    "Classify a model-checked candidate the way Specy compares golden vs learned specs (Table 3):\n"
    "- 'verified': holds and is meaningful (antecedent fires).\n"
    "- 'bug': an intended guarantee the implementation genuinely VIOLATES (highest value; cite the cex).\n"
    "- 'spurious': plausible but too strong / fails for a benign reason (e.g. a liveness monitor flagging "
    "legitimate end-of-trace quiescence; a broadcast-to-N-clients 'duplicate'; a timeout that is expected).\n"
    "- 'monitor-bug': the failure is an artifact of the monitor being mis-encoded, not a system property.\n"
    "A liveness (hot-state) failure 'at end of program execution' is almost always spurious/monitor-bug, NOT a bug.")


def judge_system_prompt() -> str:
    return "You are a JUDGE in an agentic invariant-mining pipeline for P. " + JUDGE_RUBRIC


def judge_user_prompt(name: str, benchmark: str, scenario: str, cex: str, spec_src: str = "") -> str:
    src = f"\nMONITOR SOURCE:\n{spec_src}\n" if spec_src else ""
    return (f"Candidate {name} on {benchmark} (scenario: {scenario}) FAILED model checking.\n"
            f"Counterexample: {cex}{src}\nRead the benchmark source + design intent, then classify "
            "(bug / spurious / monitor-bug) with confidence and one-paragraph reasoning grounded in the cex.")


def repair_user_prompt(name: str, benchmark: str, project_dir: str, error: str, source: str) -> str:
    return (f"This P spec monitor for {benchmark} ({project_dir}) FAILED TO COMPILE. Fix ONLY its "
            f"syntax/encoding — keep the property. NEVER invent event names; read {project_dir}/PSrc for real ones.\n"
            f"COMPILER ERROR:\n{error}\n\nBROKEN SOURCE:\n{source}\n\n{P_SPEC_RULES}\n\n"
            f"Return the corrected full `spec {name} ... {{ ... }}` source.")
