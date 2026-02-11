#!/usr/bin/env python3
"""
PChatBot MCP Regression Test Suite.

Runs all protocol design docs through the full MCP pipeline and produces
a scored report. Supports baseline comparison to detect regressions.

Usage:
    # Run and establish baseline:
    python scripts/regression_test.py --save-baseline

    # Run and compare against baseline:
    python scripts/regression_test.py

    # Run a single protocol:
    python scripts/regression_test.py --protocol Paxos
"""

from __future__ import annotations

import argparse
import json
import logging
import os
import sys
import time
from datetime import datetime
from pathlib import Path
from typing import Any, Dict, List, Optional

PROJECT_ROOT = Path(__file__).resolve().parents[1]
SRC_ROOT = PROJECT_ROOT / "src"
sys.path.insert(0, str(PROJECT_ROOT))
sys.path.insert(0, str(SRC_ROOT))

from src.ui.mcp import server as mcp_server
from src.ui.mcp.tools.env import register_env_tools, ValidateEnvironmentParams
from src.ui.mcp.tools.generation import (
    register_generation_tools,
    GenerateProjectParams,
    GenerateTypesEventsParams,
    GenerateMachineParams,
    GenerateSpecParams,
    GenerateTestParams,
    SavePFileParams,
)
from src.ui.mcp.tools.compilation import (
    register_compilation_tools,
    PCompileParams,
    PCheckParams,
)
from src.ui.mcp.tools.fixing import (
    register_fixing_tools,
    FixIterativelyParams,
    FixBuggyProgramParams,
)
from src.core.workflow.factory import extract_machine_names_from_design_doc

logger = logging.getLogger(__name__)

BASELINE_PATH = PROJECT_ROOT / "tests" / "regression_baseline.json"
RESULTS_DIR = PROJECT_ROOT / "generated_code" / "regression"

# ============================================================================
# Scoring
# ============================================================================

SCORE_WEIGHTS = {
    "generation_all_ok":    20,   # All files generated
    "compile_first_try":    25,   # Compiled without fixes
    "compile_after_fix":    15,   # Compiled after fix iterations
    "tests_discovered":     15,   # PChecker found > 0 test cases
    "all_tests_pass":       25,   # All PChecker tests pass
}


def score_protocol(result: Dict[str, Any]) -> Dict[str, Any]:
    """Score a single protocol run on a 0-100 scale."""
    scores: Dict[str, int] = {}

    # Generation
    steps = result.get("steps", [])
    gen_ok = all(s.get("success") for s in steps)
    scores["generation_all_ok"] = SCORE_WEIGHTS["generation_all_ok"] if gen_ok else 0

    # Compilation
    compile_ok = result.get("compile", {}).get("success", False)
    fix_info = result.get("fix")
    fix_ok = fix_info.get("success") if fix_info else False
    compile_after_fix = result.get("compile_after_fix", {}).get("success", False)

    if compile_ok:
        scores["compile_first_try"] = SCORE_WEIGHTS["compile_first_try"]
        scores["compile_after_fix"] = SCORE_WEIGHTS["compile_after_fix"]
    elif compile_after_fix or fix_ok:
        scores["compile_first_try"] = 0
        scores["compile_after_fix"] = SCORE_WEIGHTS["compile_after_fix"]
    else:
        scores["compile_first_try"] = 0
        scores["compile_after_fix"] = 0

    # Tests
    check = result.get("check") or {}
    failed = check.get("failed_tests", [])
    passed = check.get("passed_tests", [])
    has_tests = bool(failed or passed)
    all_pass = check.get("success", False)

    scores["tests_discovered"] = SCORE_WEIGHTS["tests_discovered"] if has_tests else 0
    scores["all_tests_pass"] = SCORE_WEIGHTS["all_tests_pass"] if all_pass else 0

    total = sum(scores.values())
    return {"scores": scores, "total": total, "max": 100}


# ============================================================================
# Protocol runner (reused from mcp_e2e_protocols.py)
# ============================================================================

class DummyMCP:
    def tool(self, *args, **kwargs):
        def decorator(fn): return fn
        return decorator
    def resource(self, *args, **kwargs):
        def decorator(fn): return fn
        return decorator


def register_tools() -> Dict[str, Any]:
    dummy = DummyMCP()
    env = register_env_tools(dummy, mcp_server._with_metadata)
    gen = register_generation_tools(dummy, mcp_server.get_services, mcp_server._with_metadata)
    comp = register_compilation_tools(dummy, mcp_server.get_services, mcp_server._with_metadata)
    fix = register_fixing_tools(dummy, mcp_server.get_services, mcp_server._with_metadata)
    return {"validate_environment": env, **gen, **comp, **fix}


def run_protocol(
    tools: Dict[str, Any],
    design_doc: str,
    out_root: Path,
    project_name: str,
) -> Dict[str, Any]:
    """Run full pipeline for one protocol. Returns structured result."""
    result: Dict[str, Any] = {
        "project_name": project_name,
        "success": False,
        "steps": [],
        "generated_files": [],
        "compile": None,
        "fix": None,
        "check": None,
        "checker_fixes": [],
        "errors": [],
        "timing": {},
    }

    t0 = time.time()

    # --- Generate project structure ---
    create = tools["generate_project_structure"](
        GenerateProjectParams(design_doc=design_doc, output_dir=str(out_root), project_name=project_name)
    )
    result["steps"].append({"name": "generate_project_structure", "success": create.get("success")})
    if not create.get("success"):
        result["errors"].append(create.get("error"))
        result["timing"]["total_s"] = round(time.time() - t0, 1)
        return result
    project_path = create["project_path"]

    # --- Generate types/events ---
    types_resp = tools["generate_types_events"](
        GenerateTypesEventsParams(design_doc=design_doc, project_path=project_path)
    )
    result["steps"].append({"name": "generate_types_events", "success": types_resp.get("success")})
    if not types_resp.get("success"):
        result["errors"].append(types_resp.get("error"))
        result["timing"]["total_s"] = round(time.time() - t0, 1)
        return result

    # --- Generate machines ---
    machine_names: List[str] = extract_machine_names_from_design_doc(design_doc)
    context_files = {"Enums_Types_Events.p": types_resp["code"]}
    generated: Dict[str, Dict[str, Any]] = {"types": types_resp}

    for mn in machine_names:
        resp = tools["generate_machine"](
            GenerateMachineParams(machine_name=mn, design_doc=design_doc, project_path=project_path, context_files=context_files)
        )
        generated[f"machine:{mn}"] = resp
        result["steps"].append({"name": f"generate_machine:{mn}", "success": resp.get("success")})
        if resp.get("success") and resp.get("filename") and resp.get("code"):
            context_files[resp["filename"]] = resp["code"]

    # --- Generate spec ---
    spec_resp = tools["generate_spec"](
        GenerateSpecParams(spec_name="Safety", design_doc=design_doc, project_path=project_path, context_files=context_files)
    )
    generated["spec"] = spec_resp
    result["steps"].append({"name": "generate_spec", "success": spec_resp.get("success")})

    # --- Generate test ---
    test_resp = tools["generate_test"](
        GenerateTestParams(test_name="TestDriver", design_doc=design_doc, project_path=project_path, context_files=context_files)
    )
    generated["test"] = test_resp
    result["steps"].append({"name": "generate_test", "success": test_resp.get("success")})

    # --- Save all files ---
    for _, payload in generated.items():
        if payload.get("success") and payload.get("file_path") and payload.get("code"):
            tools["save_p_file"](SavePFileParams(file_path=payload["file_path"], code=payload["code"]))

    result["generated_files"] = sorted(
        [v["file_path"] for v in generated.values() if v.get("success") and v.get("file_path")]
    )

    # --- Compile ---
    compile_resp = tools["p_compile"](PCompileParams(path=project_path))
    result["compile"] = {"success": compile_resp.get("success"), "error": compile_resp.get("error")}

    if not compile_resp.get("success"):
        fix_resp = tools["fix_iteratively"](FixIterativelyParams(project_path=project_path, max_iterations=8))
        result["fix"] = fix_resp
        compile_resp = tools["p_compile"](PCompileParams(path=project_path))
        result["compile_after_fix"] = {"success": compile_resp.get("success"), "error": compile_resp.get("error")}

    if not compile_resp.get("success"):
        result["errors"].append("Compilation failed after fix attempts")
        result["timing"]["total_s"] = round(time.time() - t0, 1)
        return result

    # --- PChecker ---
    check_resp = tools["p_check"](PCheckParams(path=project_path, schedules=100, timeout=90))
    result["check"] = {
        "success": check_resp.get("success"),
        "failed_tests": check_resp.get("failed_tests", []),
        "passed_tests": check_resp.get("passed_tests", []),
        "error": check_resp.get("error"),
    }

    # --- Auto-fix checker bugs (up to 2 rounds) ---
    for rnd in range(2):
        if check_resp.get("success") or not check_resp.get("failed_tests"):
            break
        fix_bug = tools["fix_buggy_program"](FixBuggyProgramParams(project_path=project_path))
        result["checker_fixes"].append({"round": rnd + 1, "fixed": fix_bug.get("fixed")})
        if not fix_bug.get("fixed"):
            break
        recomp = tools["p_compile"](PCompileParams(path=project_path))
        if not recomp.get("success"):
            break
        check_resp = tools["p_check"](PCheckParams(path=project_path, schedules=100, timeout=90))
        result["check"] = {
            "success": check_resp.get("success"),
            "failed_tests": check_resp.get("failed_tests", []),
            "passed_tests": check_resp.get("passed_tests", []),
            "error": check_resp.get("error"),
        }

    result["success"] = bool(check_resp.get("success"))
    if not result["success"]:
        result["errors"].append("PChecker reported failing tests")

    result["timing"]["total_s"] = round(time.time() - t0, 1)
    return result


# ============================================================================
# Baseline comparison
# ============================================================================

def compare_with_baseline(
    current: Dict[str, Any], baseline: Dict[str, Any]
) -> Dict[str, Any]:
    """Compare current run against baseline. Returns diff report."""
    diff: Dict[str, Any] = {"regressions": [], "improvements": [], "unchanged": []}

    current_protos = {p["project_name"]: p for p in current.get("protocols", [])}
    baseline_protos = {p["project_name"]: p for p in baseline.get("protocols", [])}

    for name in sorted(set(list(current_protos.keys()) + list(baseline_protos.keys()))):
        cur = current_protos.get(name)
        base = baseline_protos.get(name)

        if not base:
            diff["improvements"].append({"protocol": name, "reason": "new protocol added"})
            continue
        if not cur:
            diff["regressions"].append({"protocol": name, "reason": "protocol removed"})
            continue

        cur_score = cur.get("score", {}).get("total", 0)
        base_score = base.get("score", {}).get("total", 0)
        delta = cur_score - base_score

        entry = {
            "protocol": name,
            "baseline_score": base_score,
            "current_score": cur_score,
            "delta": delta,
        }

        # Check specific regressions
        details = []
        if base.get("compile", {}).get("success") and not cur.get("compile", {}).get("success"):
            details.append("compile regressed")
        if base.get("check", {}).get("success") and not cur.get("check", {}).get("success"):
            details.append("checker regressed")
        base_gen = all(s.get("success") for s in base.get("steps", []))
        cur_gen = all(s.get("success") for s in cur.get("steps", []))
        if base_gen and not cur_gen:
            details.append("generation regressed")

        entry["details"] = details

        if delta < 0:
            diff["regressions"].append(entry)
        elif delta > 0:
            diff["improvements"].append(entry)
        else:
            diff["unchanged"].append(entry)

    return diff


def print_report(report: Dict[str, Any], diff: Optional[Dict[str, Any]] = None):
    """Print a human-readable report."""
    print("\n" + "=" * 70)
    print("  PChatBot MCP Regression Test Report")
    print(f"  {report['timestamp']}")
    print("=" * 70)

    total_score = 0
    max_score = 0

    for p in report["protocols"]:
        name = p["project_name"]
        sc = p.get("score", {})
        total = sc.get("total", 0)
        total_score += total
        max_score += 100

        gen_ok = all(s.get("success") for s in p.get("steps", []))
        comp_ok = (p.get("compile") or {}).get("success", False)
        check = p.get("check") or {}
        check_ok = check.get("success", False)
        passed = len(check.get("passed_tests", []))
        failed = len(check.get("failed_tests", []))
        timing = p.get("timing", {}).get("total_s", "?")

        status = "✅ PASS" if p["success"] else "❌ FAIL"
        retries = p.get("retries_used", 0)
        retry_tag = f" [retry {retries}x]" if retries else ""

        print(f"\n  {name}: {status}  (score: {total}/100, {timing}s{retry_tag})")
        print(f"    generate: {'✅' if gen_ok else '❌'}  "
              f"compile: {'✅' if comp_ok else '❌'}  "
              f"check: {'✅' if check_ok else '❌'} ({passed} passed, {failed} failed)")

        if sc.get("scores"):
            breakdown = ", ".join(f"{k}={v}" for k, v in sc["scores"].items())
            print(f"    scores: {breakdown}")

    pct = (total_score / max_score * 100) if max_score else 0
    print(f"\n  AGGREGATE: {total_score}/{max_score} ({pct:.0f}%)")

    if diff:
        print(f"\n  --- Baseline Comparison ---")
        if diff["improvements"]:
            for d in diff["improvements"]:
                print(f"  ⬆️  {d['protocol']}: +{d.get('delta', '?')} pts  {d.get('details', d.get('reason', ''))}")
        if diff["regressions"]:
            for d in diff["regressions"]:
                print(f"  ⬇️  {d['protocol']}: {d.get('delta', '?')} pts  {d.get('details', d.get('reason', ''))}")
        if diff["unchanged"]:
            for d in diff["unchanged"]:
                print(f"  ➡️  {d['protocol']}: unchanged ({d.get('current_score', '?')}/100)")
        if not diff["regressions"]:
            print("  ✅ No regressions detected!")
        else:
            print(f"  ⚠️  {len(diff['regressions'])} regression(s) detected!")

    print("\n" + "=" * 70)


# ============================================================================
# Main
# ============================================================================

def main() -> int:
    parser = argparse.ArgumentParser(description="PChatBot MCP Regression Tests")
    parser.add_argument("--save-baseline", action="store_true", help="Save results as new baseline")
    parser.add_argument("--protocol", type=str, help="Run a single protocol by name")
    parser.add_argument("--no-compare", action="store_true", help="Skip baseline comparison")
    args = parser.parse_args()

    logging.basicConfig(level=logging.INFO, format="[%(levelname)s] %(message)s")
    # Suppress noisy Streamlit warnings when running outside Streamlit
    logging.getLogger("streamlit").setLevel(logging.ERROR)

    docs_dir = PROJECT_ROOT / "resources" / "system_design_docs"
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    out_root = RESULTS_DIR / timestamp
    out_root.mkdir(parents=True, exist_ok=True)

    tools = register_tools()
    env = tools["validate_environment"](ValidateEnvironmentParams())

    all_runs = [
        ("Paxos", docs_dir / "[Design Doc] Basic Paxos Protocol.txt"),
        ("TwoPhaseCommit", docs_dir / "[Design Doc] Two Phase Commit.txt"),
        ("MessageBroker", docs_dir / "[Design Doc] Simple Message Broker.txt"),
        ("DistributedLock", docs_dir / "[Design Doc] Distributed Lock Server.txt"),
        ("HotelManagement", docs_dir / "[Design Doc] Hotel Management Application.txt"),
    ]

    if args.protocol:
        all_runs = [(n, p) for n, p in all_runs if n == args.protocol]
        if not all_runs:
            print(f"Unknown protocol: {args.protocol}")
            return 1

    report: Dict[str, Any] = {
        "timestamp": datetime.now().isoformat(),
        "output_root": str(out_root),
        "environment": env,
        "protocols": [],
    }

    # Only retry when generation or compilation completely failed (score ≤ 35).
    # Checker bugs are better addressed by fix_buggy_program, not regeneration.
    RETRY_THRESHOLD = 35
    MAX_PROTOCOL_RETRIES = 2

    for name, path in all_runs:
        design_doc = path.read_text(encoding="utf-8")
        best_result = None
        best_score = -1

        for attempt in range(1, MAX_PROTOCOL_RETRIES + 1):
            attempt_out = out_root if attempt == 1 else out_root / f"{name}_retry{attempt}"
            if attempt > 1:
                attempt_out.mkdir(parents=True, exist_ok=True)
                logger.info(f"[RETRY] {name} attempt {attempt} (previous score: {best_score})")

            result = run_protocol(tools, design_doc, attempt_out, name)
            result["score"] = score_protocol(result)
            result["attempt"] = attempt
            current_score = result["score"]["total"]

            if current_score > best_score:
                best_score = current_score
                best_result = result

            if current_score > RETRY_THRESHOLD:
                break  # Compiled successfully, no need to retry full generation

        assert best_result is not None
        if best_result.get("attempt", 1) > 1:
            best_result["retries_used"] = best_result["attempt"] - 1
        report["protocols"].append(best_result)

    # Save report
    report_path = out_root / "regression_report.json"
    report_path.write_text(json.dumps(report, indent=2), encoding="utf-8")

    # Baseline comparison
    diff = None
    if not args.no_compare and BASELINE_PATH.exists():
        baseline = json.loads(BASELINE_PATH.read_text(encoding="utf-8"))
        diff = compare_with_baseline(report, baseline)
        report["baseline_diff"] = diff

    # Print report
    print_report(report, diff)

    # Save baseline if requested
    if args.save_baseline:
        BASELINE_PATH.parent.mkdir(parents=True, exist_ok=True)
        BASELINE_PATH.write_text(json.dumps(report, indent=2), encoding="utf-8")
        print(f"\n  Baseline saved to: {BASELINE_PATH}")

    # Exit code: 1 if regressions detected
    if diff and diff.get("regressions"):
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
