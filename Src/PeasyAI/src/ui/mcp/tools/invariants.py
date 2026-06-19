"""MCP tools for agentic invariant learning (the PInfer agentic workflow)."""

from typing import Any, Dict, List, Optional
from pydantic import BaseModel, Field
import logging

logger = logging.getLogger(__name__)


class LearnInvariantsParams(BaseModel):
    """Parameters for learning correctness specifications for a P project."""
    benchmark: str = Field(..., description="Short benchmark/protocol name, e.g. 'FailureDetector'")
    project_path: str = Field(..., description="Absolute path to the P project directory")
    events: str = Field(..., description="Event signatures, e.g. 'ePing: (fd: FailureDetector, trial: int) [FD->Node]\\n...'")
    main: str = Field(..., description="Test main machine name (from the project's PTst test, the [main=...] value)")
    assert_in: str = Field(..., description="Module expression after 'in' from the project's existing test, e.g. 'union { TestMain }, FailureDetector, FailureInjector'")
    design_doc: str = Field("", description="Optional design-intent text to ground proposals")
    priors: List[str] = Field(default_factory=lambda: ["templates", "lenses"], description="Proposer priors: 'templates' (Specy grammar shapes) and/or 'lenses' (intent lenses)")
    iters: int = Field(2000, description="Schedules per candidate for bounded model checking")


class ValidateInvariantsParams(BaseModel):
    """Parameters for deterministic (no-LLM) validation of candidate spec monitors."""
    project_path: str = Field(..., description="Absolute path to the P project directory")
    candidates_p: str = Field(..., description="P source containing one or more `spec <Name> ... { }` blocks (and optional `<Name>_canary` vacuity probes)")
    main: str = Field(..., description="Test main machine name")
    assert_in: str = Field(..., description="Module expression after 'in' from the project's existing test")
    iters: int = Field(2000, description="Schedules per candidate")


def register_invariant_tools(mcp, get_services, with_metadata):
    """Register invariant-learning tools."""

    @mcp.tool(
        name="peasy-ai-learn-invariants",
        description="Learn correctness specifications (safety + some liveness, forall and forall-exists) for a P project. Proposes intent-level invariants using template (Specy grammar) + intent-lens priors, model-checks each with PChecker, prunes vacuous specs and false positives, and classifies survivors (verified / bug / spurious / vacuous). Returns a ranked, machine-checked spec set."
    )
    def learn_invariants(params: LearnInvariantsParams) -> Dict[str, Any]:
        logger.info(f"[TOOL] peasy-ai-learn-invariants: {params.benchmark} @ {params.project_path}")
        try:
            from core.security import validate_project_path  # type: ignore
            validate_project_path(params.project_path)
        except Exception:
            pass  # security helpers optional in some checkouts
        svc = get_services().get("learn_invariants")
        if svc is None:
            return with_metadata("peasy-ai-learn-invariants",
                                 {"success": False, "error": "learn_invariants service unavailable",
                                  "message": "Ensure Src/PInfer/Scripts/invariant_core.py is present."})
        result = svc.learn(
            benchmark=params.benchmark, project_path=params.project_path, events=params.events,
            main=params.main, assert_in=params.assert_in, design_doc=params.design_doc,
            priors=params.priors, iters=params.iters,
        )
        payload = {
            "success": result.success,
            "error": result.error,
            "invariants": result.invariants,
            "summary": result.summary,
            "message": (f"Learned {result.summary.get('HOLDS', 0)} verified invariants "
                        f"({result.summary.get('FAILS', 0)} failures to review, "
                        f"{result.summary.get('VACUOUS', 0)} vacuous)." if result.success
                        else f"Failed: {result.error}"),
        }
        return with_metadata("peasy-ai-learn-invariants", payload, token_usage=result.token_usage)

    @mcp.tool(
        name="peasy-ai-validate-invariants",
        description="Deterministically validate candidate P spec monitors (no LLM): compile, bounded model-check, and classify each as HOLDS / FAILS(+counterexample) / VACUOUS / INCONCLUSIVE / COMPILE-ERR. Use `<Name>_canary` companion specs (assert false in the guarded branch) for vacuity detection. Removes vacuous specs and false positives."
    )
    def validate_invariants(params: ValidateInvariantsParams) -> Dict[str, Any]:
        logger.info(f"[TOOL] peasy-ai-validate-invariants: {params.project_path}")
        svc = get_services().get("learn_invariants")
        if svc is None:
            return with_metadata("peasy-ai-validate-invariants",
                                 {"success": False, "error": "learn_invariants service unavailable"})
        # split the supplied source into pseudo-candidates so we reuse one code path
        try:
            import invariant_core as core  # type: ignore
        except Exception:
            return with_metadata("peasy-ai-validate-invariants",
                                 {"success": False, "error": "invariant_core unavailable"})
        cands = [{"name": n, "specCode": src} for n, src in core.split_specs(params.candidates_p).items()]
        verdicts = svc.validate(params.project_path, cands, params.main, params.assert_in, params.iters)
        rows = [{"name": v.name, "verdict": v.verdict, "detail": v.detail} for v in verdicts]
        summary: Dict[str, int] = {}
        for r in rows:
            summary[r["verdict"]] = summary.get(r["verdict"], 0) + 1
        payload = {"success": True, "verdicts": rows, "summary": summary,
                   "message": f"Validated {len(rows)} candidates: {summary}"}
        return with_metadata("peasy-ai-validate-invariants", payload)

    return {"learn_invariants": learn_invariants, "validate_invariants": validate_invariants}
