"""
LearnInvariantsService — agentic invariant learning for P, the PeasyAI surface of the
PInfer agentic workflow.

Orchestrates: PROPOSE (LLM, template + intent-lens priors) -> VALIDATE (deterministic
trace/PChecker pruning of vacuous + false-positive candidates) -> JUDGE (LLM, Specy
Table-3 classification) -> RANK.

The deterministic validation + the prompt builders live in the engine-agnostic shared
core `Src/PInfer/Scripts/invariant_core.py` (also used by the Claude Code skill). This
service wires that core to PeasyAI's LLM provider.
"""
from __future__ import annotations

import json
import logging
import sys
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, Dict, List, Optional

from ..llm import LLMConfig, Message, MessageRole
from .base import BaseService, ServiceResult

logger = logging.getLogger(__name__)

# Import the engine-agnostic shared core from Src/PInfer/Scripts (same repo).
_SCRIPTS = Path(__file__).resolve().parents[4] / "PInfer" / "Scripts"
try:
    if str(_SCRIPTS) not in sys.path:
        sys.path.insert(0, str(_SCRIPTS))
    import invariant_core as core  # type: ignore
    HAS_CORE = True
except Exception as e:  # pragma: no cover
    logger.warning("invariant_core unavailable (%s); validation disabled", e)
    core = None  # type: ignore
    HAS_CORE = False


@dataclass
class LearningResult(ServiceResult):
    """Result of an invariant-learning run."""
    invariants: List[Dict[str, Any]] = field(default_factory=list)   # ranked, classified specs
    suggestions: Dict[str, Any] = field(default_factory=dict)        # UG1/UG2/UG3 hints
    summary: Dict[str, int] = field(default_factory=dict)            # counts by verdict


def _extract_json_array(text: str) -> List[Dict[str, Any]]:
    """Pull the first top-level JSON array out of an LLM response."""
    start = text.find("[")
    if start == -1:
        return []
    depth, in_str, esc = 0, False, False
    for i in range(start, len(text)):
        c = text[i]
        if in_str:
            esc = (c == "\\" and not esc)
            if c == '"' and not esc:
                in_str = False
        elif c == '"':
            in_str = True
        elif c == "[":
            depth += 1
        elif c == "]":
            depth -= 1
            if depth == 0:
                try:
                    return json.loads(text[start:i + 1])
                except json.JSONDecodeError:
                    return []
    return []


class LearnInvariantsService(BaseService):
    """Propose -> validate -> judge -> rank correctness specs for a P project."""

    def _llm(self, system: str, user: str, max_tokens: int = 8192):
        resp = self.llm.complete(
            [Message(role=MessageRole.USER, content=user)],
            LLMConfig(max_tokens=max_tokens),
            system,
        )
        return resp.content, resp.usage.to_dict()

    # ── PROPOSE ───────────────────────────────────────────────────────────
    def propose(self, benchmark: str, project_dir: str, events: str,
                priors: Optional[List[str]] = None, design_doc: str = "") -> Dict[str, Any]:
        """Generate candidate spec monitors using template + intent-lens priors."""
        priors = priors or ["templates", "lenses"]
        candidates: List[Dict[str, Any]] = []
        tokens: Dict[str, int] = {}
        prompts: List[tuple] = []
        if "templates" in priors:
            prompts += [(f"tmpl_{k}", f) for k, f in core.TEMPLATE_SHAPES]
        if "lenses" in priors:
            prompts += [(k, f) for k, f in core.INTENT_LENSES]
        for kind, focus in prompts:
            content, usage = self._llm(
                core.propose_system_prompt(),
                core.propose_user_prompt(benchmark, project_dir, events, kind, focus, design_doc),
            )
            for c in _extract_json_array(content):
                c["lens"] = kind
                candidates.append(c)
            for k, v in usage.items():
                tokens[k] = tokens.get(k, 0) + v
        return {"candidates": candidates, "token_usage": tokens}

    # ── VALIDATE ──────────────────────────────────────────────────────────
    def validate(self, project_path: str, candidates: List[Dict[str, Any]],
                 main: str, assert_in: str, iters: int = 2000) -> List[Any]:
        """Deterministic pruning: compile, bounded-check, drop vacuous + false positives."""
        if not HAS_CORE:
            return []
        src = "\n\n".join(c.get("specCode", "") for c in candidates if c.get("specCode"))
        return core.validate_candidates(project_path, src, main, assert_in, iters)

    # ── JUDGE ─────────────────────────────────────────────────────────────
    def judge(self, benchmark: str, scenario: str, name: str, cex: str,
              spec_src: str = "") -> Dict[str, Any]:
        content, usage = self._llm(
            core.judge_system_prompt(),
            core.judge_user_prompt(name, benchmark, scenario, cex, spec_src),
            max_tokens=2048,
        )
        return {"reasoning": content.strip(), "token_usage": usage}

    # ── ORCHESTRATE ───────────────────────────────────────────────────────
    def learn(self, benchmark: str, project_path: str, events: str, main: str,
              assert_in: str, design_doc: str = "", priors: Optional[List[str]] = None,
              iters: int = 2000) -> LearningResult:
        if not HAS_CORE:
            return LearningResult(success=False, error="invariant_core (Src/PInfer/Scripts) not importable")
        try:
            self._status(f"Proposing invariants for {benchmark}...")
            proposed = self.propose(benchmark, project_path, events, priors, design_doc)
            cands = proposed["candidates"]
            by_name = {c.get("name"): c for c in cands}
            tokens = dict(proposed["token_usage"])

            self._status(f"Validating {len(cands)} candidates ({iters} schedules each)...")
            verdicts = self.validate(project_path, cands, main, assert_in, iters)

            results: List[Dict[str, Any]] = []
            summary: Dict[str, int] = {}
            for v in verdicts:
                summary[v.verdict] = summary.get(v.verdict, 0) + 1
                entry = {"name": v.name, "verdict": v.verdict, "detail": v.detail,
                         "intent": by_name.get(v.name, {}).get("intent", ""),
                         "predicted": by_name.get(v.name, {}).get("predictedBucket", "?")}
                if v.verdict == "FAILS":   # only adjudicate genuine monitor failures
                    j = self.judge(benchmark, main, v.name, v.detail,
                                   by_name.get(v.name, {}).get("specCode", ""))
                    entry["judgment"] = j["reasoning"]
                    for k, val in j["token_usage"].items():
                        tokens[k] = tokens.get(k, 0) + val
                results.append(entry)

            # Rank: verified first, then failures-to-review, then vacuous/inconclusive.
            order = {"HOLDS": 0, "FAILS": 1, "VACUOUS": 2, "INCONCLUSIVE": 3, "COMPILE-ERR": 4}
            results.sort(key=lambda r: order.get(r["verdict"], 9))
            return LearningResult(success=True, invariants=results, summary=summary, token_usage=tokens)
        except Exception as e:  # pragma: no cover
            logger.exception("learn() failed")
            return LearningResult(success=False, error=str(e))
