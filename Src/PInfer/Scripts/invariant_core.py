#!/usr/bin/env python3
"""
invariant_core.py — engine-agnostic shared core for agentic invariant learning.

This is the reusable heart of the PInfer agentic workflow, with NO dependency on
PeasyAI or Claude Code. Both wrappers build on it:
  * the PeasyAI MCP tool  (Src/PeasyAI/.../tools/invariants.py) drives it via PeasyAI's LLM,
  * the Claude Code skill  (.claude/skills/learn-invariants) drives it via agents.

It provides two things:
  1. DETERMINISTIC VALIDATION — wire candidate spec monitors into a P project, compile,
     bounded-model-check, classify (HOLDS-BOUNDED / FAILS+cex / VACUOUS / UNKNOWN-VACUITY /
     INCONCLUSIVE / COMPILE-ERR), with vacuity canaries and detection of pre-existing SUT
     failures. No LLM. See PLAN.md §6.4 for why passing is HOLDS-BOUNDED, not "HOLDS".
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
#
# The verdict vocabulary is deliberately explicit about what model-checking proves
# (see PLAN.md §6.4). `p check -i N` is *bounded* exploration, not a proof, so a
# passing candidate is HOLDS-BOUNDED — never a bare "HOLDS". HOLDS-PROVEN is reserved
# for a future PVerifier stage (Phase 5) and is defined here only so the vocabulary is
# frozen. Vacuity is a first-class outcome: a candidate whose guard is never reached is
# VACUOUS, and a candidate for which we cannot even decide vacuity (no canary) is
# UNKNOWN-VACUITY rather than being silently reported as holding.

VERDICTS = frozenset({
    "HOLDS-BOUNDED",     # passed bounded model checking; guard provably reached (canary tripped)
    "HOLDS-PROVEN",      # reserved: proven by PVerifier (Phase 5), never emitted by bounded checking
    "FAILS",             # monitor assertion violated (candidate-owned failure) + counterexample
    "VACUOUS",           # holds only because the guarded branch is never reached
    "UNKNOWN-VACUITY",   # holds, but no canary exists to decide vacuity
    "INCONCLUSIVE",      # a pre-existing SUT failure fired first; the candidate was never tested
    "COMPILE-ERR",       # candidate did not compile (dropped)
})

# What the JUDGE may conclude about a model-checked candidate, and the follow-up it implies.
JUDGE_VERDICTS = frozenset({"verified", "bug", "spurious", "monitor-bug", "vacuous"})
DEVELOPMENT_ACTIONS = frozenset({"add-to-tests", "debug-bug", "drop-spurious", "drop-vacuous"})


@dataclass
class Verdict:
    name: str
    verdict: str                 # one of VERDICTS
    detail: str = ""
    bugs: int = 0
    owner: str = "monitor"       # monitor | sut


@dataclass
class Formula:
    """Structured formula record — the part of a candidate that makes dedup/merge/ranking
    well-defined (PLAN.md §5). Proposers fill this alongside the opaque `spec_code` monitor
    so two candidates expressing the same property collapse regardless of monitor phrasing."""
    quantifiers: List[Dict[str, str]] = field(default_factory=list)  # [{var,type,kind: forall|exists}]
    guards: List[str] = field(default_factory=list)                  # conjunct predicates (antecedent)
    relations: List[str] = field(default_factory=list)              # conjunct predicates (consequent)
    sc: Optional[Dict[str, str]] = None                             # quorum/cardinality {op, bound}
    config_event: Optional[str] = None                             # population source for sc
    uses_index: bool = False                                        # happens-before term present

    @property
    def arity(self) -> int:
        return sum(1 for q in self.quantifiers if q.get("kind", "forall") == "forall")

    def to_dict(self) -> Dict:
        return {"quantifiers": self.quantifiers, "guards": self.guards,
                "relations": self.relations, "sc": self.sc,
                "config_event": self.config_event, "uses_index": self.uses_index}

    @classmethod
    def from_dict(cls, d: Optional[Dict]) -> "Formula":
        d = d or {}
        return cls(quantifiers=list(d.get("quantifiers", [])),
                   guards=list(d.get("guards", [])),
                   relations=list(d.get("relations", [])),
                   sc=d.get("sc"), config_event=d.get("config_event"),
                   uses_index=bool(d.get("uses_index", False)))


@dataclass
class Candidate:
    """The unit that flows through the whole pipeline. `formula` enables structured dedup;
    `spec_code` is the compilable monitor; `verdict`/`provenance_run` are filled by the
    backbone, not the proposer. Field names are mirrored in propose_templated.js's
    CAND_SCHEMA — the JS↔Python parity test guards drift (PLAN.md §5, N1)."""
    name: str
    intent: str = ""
    category: str = ""
    provenance: str = "intent"           # templated | intent | enumerative
    observes: List[str] = field(default_factory=list)
    formula: Formula = field(default_factory=Formula)
    spec_code: str = ""
    canary: Optional[str] = None
    predicted_bucket: str = "verified"    # verified | bug | spurious | vacuous
    verdict: Optional[Verdict] = None
    provenance_run: Dict = field(default_factory=dict)  # {model,temperature,prompt_hash,iters,seed}

    def to_dict(self) -> Dict:
        return {"name": self.name, "intent": self.intent, "category": self.category,
                "provenance": self.provenance, "observes": self.observes,
                "formula": self.formula.to_dict(), "specCode": self.spec_code,
                "canary": self.canary, "predictedBucket": self.predicted_bucket,
                "verdict": None if self.verdict is None else {
                    "name": self.verdict.name, "verdict": self.verdict.verdict,
                    "detail": self.verdict.detail, "bugs": self.verdict.bugs,
                    "owner": self.verdict.owner},
                "provenanceRun": self.provenance_run}

    @classmethod
    def from_dict(cls, d: Dict) -> "Candidate":
        # Accept both camelCase (JS/JSON) and snake_case (Python) spellings.
        v = d.get("verdict")
        return cls(
            name=d["name"], intent=d.get("intent", ""), category=d.get("category", ""),
            provenance=d.get("provenance", "intent"), observes=list(d.get("observes", [])),
            formula=Formula.from_dict(d.get("formula")),
            spec_code=d.get("specCode", d.get("spec_code", "")),
            canary=d.get("canary"),
            predicted_bucket=d.get("predictedBucket", d.get("predicted_bucket", "verified")),
            verdict=None if not v else Verdict(v["name"], v["verdict"], v.get("detail", ""),
                                               v.get("bugs", 0), v.get("owner", "monitor")),
            provenance_run=d.get("provenanceRun", d.get("provenance_run", {})))


# Python mirror of propose_templated.js CAND_SCHEMA (the proposer output contract).
CANDIDATE_FIELDS = ("name", "intent", "category", "provenance", "observes",
                    "formula", "specCode", "canary", "predictedBucket")


def _norm_pred(p: str) -> str:
    """Whitespace-insensitive predicate normal form for dedup keys."""
    return re.sub(r"\s+", "", p)


def dedup_key(c: Candidate) -> Tuple:
    """Canonical identity of a candidate's *property* (PLAN.md §5). Two candidates with the
    same observed events, same (order-insensitive) guard/relation conjuncts, same SC and
    quantifier structure are the same property regardless of monitor phrasing or name.
    On collision, callers cluster-and-keep a representative — they must NOT silently drop."""
    f = c.formula
    return (
        tuple(sorted(c.observes)),
        tuple(sorted(_norm_pred(g) for g in f.guards)),
        tuple(sorted(_norm_pred(r) for r in f.relations)),
        (f.sc["op"], _norm_pred(str(f.sc.get("bound", "")))) if f.sc else None,
        tuple(sorted(q.get("kind", "forall") for q in f.quantifiers)),
    )


def dedup_candidates(cands: List[Candidate]) -> Tuple[List[Candidate], List[List[Candidate]]]:
    """Return (representatives, clusters). First candidate of each key is the representative;
    clusters preserves every member so nothing is lost from metrics."""
    order: List[Tuple] = []
    groups: Dict[Tuple, List[Candidate]] = {}
    for c in cands:
        k = dedup_key(c)
        if k not in groups:
            groups[k] = []
            order.append(k)
        groups[k].append(c)
    return [groups[k][0] for k in order], [groups[k] for k in order]


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
        elif n in canaries:
            # Canary asserts false inside the guarded branch: if it TRIPPED (bugs>0) the guard is
            # reachable, so the candidate holds non-vacuously; if it HELD (bugs==0) the guard is
            # unreachable, so the candidate is vacuous. A canary that itself failed to run (-1)
            # leaves vacuity undecided.
            cbugs = results.get(f"{n}_canary", (-1,))[0]
            if cbugs == 0:
                out.append(Verdict(n, "VACUOUS", "guarded branch never reached (canary did not trip)", 0, owner))
            elif cbugs > 0:
                out.append(Verdict(n, "HOLDS-BOUNDED", "non-vacuous (canary tripped)", 0, owner))
            else:
                out.append(Verdict(n, "UNKNOWN-VACUITY", "canary did not run (compile/check error)", 0, owner))
        else:
            # No vacuity canary: bounded checking cannot tell a real invariant from a never-fired
            # guard. PREP should auto-generate a canary (PLAN.md §6.1); until then, don't overclaim.
            out.append(Verdict(n, "UNKNOWN-VACUITY", "no vacuity canary (run PREP auto-canary)", 0, owner))
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
    "A liveness (hot-state) failure 'at end of program execution' is almost always spurious/monitor-bug, NOT a bug.\n"
    "NOTE: 'vacuous' is decided upstream by the VALIDATE stage's canary, not by you — you only see "
    "candidates that FAILED model checking. Every verdict carries a development_action: "
    "verified->add-to-tests, bug->debug-bug, spurious/monitor-bug->drop-spurious.")

# Structured output contract for the JUDGE (PLAN.md §7 Phase 4). Kept as a plain dict so both the
# PeasyAI MCP tool and the Claude Code skill can hand it to their respective structured-output layer.
JUDGE_OUTPUT_SCHEMA = {
    "type": "object",
    "additionalProperties": False,
    "required": ["verdict", "confidence", "cex_grounding", "development_action"],
    "properties": {
        "verdict": {"type": "string", "enum": sorted(JUDGE_VERDICTS)},
        "confidence": {"type": "number", "minimum": 0, "maximum": 1},
        "cex_grounding": {"type": "string", "description": "one-paragraph reasoning grounded in the counterexample"},
        "development_action": {"type": "string", "enum": sorted(DEVELOPMENT_ACTIONS)},
    },
}


def judge_system_prompt() -> str:
    return "You are a JUDGE in an agentic invariant-mining pipeline for P. " + JUDGE_RUBRIC


def judge_user_prompt(name: str, benchmark: str, scenario: str, cex: str, spec_src: str = "") -> str:
    src = f"\nMONITOR SOURCE:\n{spec_src}\n" if spec_src else ""
    return (f"Candidate {name} on {benchmark} (scenario: {scenario}) FAILED model checking.\n"
            f"Counterexample: {cex}{src}\nRead the benchmark source + design intent, then return JSON "
            "{\"verdict\"(bug|spurious|monitor-bug),\"confidence\"(0-1),\"cex_grounding\":\"...\","
            "\"development_action\"(debug-bug|drop-spurious)} with reasoning grounded in the cex.")


def repair_user_prompt(name: str, benchmark: str, project_dir: str, error: str, source: str) -> str:
    return (f"This P spec monitor for {benchmark} ({project_dir}) FAILED TO COMPILE. Fix ONLY its "
            f"syntax/encoding — keep the property. NEVER invent event names; read {project_dir}/PSrc for real ones.\n"
            f"COMPILER ERROR:\n{error}\n\nBROKEN SOURCE:\n{source}\n\n{P_SPEC_RULES}\n\n"
            f"Return the corrected full `spec {name} ... {{ ... }}` source.")
