#!/usr/bin/env python3
"""
End-to-end MCP validation for protocol examples.

Runs complete MCP tool flows for:
1) Basic Paxos Protocol
2) Two Phase Commit
"""

from __future__ import annotations

import json
import logging
import sys
from datetime import datetime
from pathlib import Path
from typing import Dict, Any, List

logger = logging.getLogger(__name__)
# Suppress noisy Streamlit warnings when running outside Streamlit
logging.getLogger("streamlit").setLevel(logging.ERROR)

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
    GenerateCompleteProjectParams,
    SavePFileParams,
)
from src.ui.mcp.tools.compilation import (
    register_compilation_tools,
    PCompileParams,
    PCheckParams,
)
from src.ui.mcp.tools.fixing import register_fixing_tools, FixIterativelyParams, FixBuggyProgramParams
from src.core.workflow.factory import extract_machine_names_from_design_doc


class DummyMCP:
    def tool(self, *args, **kwargs):
        def decorator(fn):
            return fn
        return decorator

    def resource(self, *args, **kwargs):
        def decorator(fn):
            return fn
        return decorator


def _load_design_doc(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def _register_tools() -> Dict[str, Any]:
    dummy_mcp = DummyMCP()
    env_tools = register_env_tools(dummy_mcp, mcp_server._with_metadata)
    gen_tools = register_generation_tools(
        dummy_mcp, mcp_server.get_services, mcp_server._with_metadata
    )
    comp_tools = register_compilation_tools(
        dummy_mcp, mcp_server.get_services, mcp_server._with_metadata
    )
    fix_tools = register_fixing_tools(
        dummy_mcp, mcp_server.get_services, mcp_server._with_metadata
    )
    all_tools = {
        "validate_environment": env_tools,
        **gen_tools,
        **comp_tools,
        **fix_tools,
    }
    return all_tools


def _save_generated(tools: Dict[str, Any], generated: Dict[str, Dict[str, Any]]) -> None:
    for _, payload in generated.items():
        if payload.get("success") and payload.get("file_path") and payload.get("code"):
            tools["save_p_file"](
                SavePFileParams(
                    file_path=payload["file_path"],
                    code=payload["code"],
                )
            )


def run_protocol_ensemble(
    tools: Dict[str, Any],
    design_doc: str,
    out_root: Path,
    project_name: str,
    ensemble_size: int = 3,
) -> Dict[str, Any]:
    """
    Run a protocol end-to-end using ``generate_complete_project`` with
    ensemble generation.  This is the primary flow — it generates N candidates
    per file, picks the best, compiles, fixes, and runs PChecker in one call.
    """
    result: Dict[str, Any] = {
        "project_name": project_name,
        "success": False,
        "mode": "ensemble",
        "ensemble_size": ensemble_size,
        "steps": [],
        "generated_files": [],
        "compile": None,
        "check": None,
        "errors": [],
    }

    resp = tools["generate_complete_project"](
        GenerateCompleteProjectParams(
            design_doc=design_doc,
            output_dir=str(out_root),
            project_name=project_name,
            include_spec=True,
            include_test=True,
            auto_fix=True,
            run_checker=True,
            ensemble_size=ensemble_size,
        )
    )

    result["steps"].append({
        "name": "generate_complete_project",
        "success": resp.get("success"),
    })
    result["project_path"] = resp.get("project_path")
    result["generated_files"] = sorted(resp.get("generated_files", {}).values())
    result["compile"] = resp.get("compilation")
    result["check"] = resp.get("checker")
    result["errors"] = resp.get("errors", [])
    result["warnings"] = resp.get("warnings", [])
    result["success"] = bool(resp.get("success"))

    # If generate_complete_project didn't run checker or checker failed,
    # try the explicit fix_buggy_program loop as a fallback.
    project_path = resp.get("project_path")
    check_info = resp.get("checker")
    compile_info = resp.get("compilation", {})
    if (
        project_path
        and compile_info.get("success")
        and check_info
        and not check_info.get("success")
        and check_info.get("failed_tests")
    ):
        MAX_CHECKER_FIX_ROUNDS = 2
        for fix_round in range(1, MAX_CHECKER_FIX_ROUNDS + 1):
            logger.info(f"[CHECKER-FIX] Round {fix_round}: attempting fix_buggy_program")
            fix_bug_resp = tools["fix_buggy_program"](
                FixBuggyProgramParams(project_path=project_path)
            )
            result.setdefault("checker_fixes", []).append(fix_bug_resp)

            if not fix_bug_resp.get("fixed"):
                break

            recompile = tools["p_compile"](PCompileParams(path=project_path))
            if not recompile.get("success"):
                result["errors"].append(
                    f"Recompilation failed after checker fix round {fix_round}"
                )
                break

            recheck = tools["p_check"](
                PCheckParams(path=project_path, schedules=100, timeout=90)
            )
            result["check"] = {
                "success": recheck.get("success"),
                "failed_tests": recheck.get("failed_tests", []),
                "error": recheck.get("error"),
            }
            if recheck.get("success"):
                result["success"] = True
                break

    return result


def run_protocol(tools: Dict[str, Any], design_doc: str, out_root: Path, project_name: str) -> Dict[str, Any]:
    """
    Run a protocol end-to-end using the step-by-step flow (legacy).
    Kept for backward compatibility; prefer ``run_protocol_ensemble``.
    """
    result: Dict[str, Any] = {
        "project_name": project_name,
        "success": False,
        "mode": "step_by_step",
        "steps": [],
        "generated_files": [],
        "compile": None,
        "fix": None,
        "check": None,
        "errors": [],
    }

    create = tools["generate_project_structure"](
        GenerateProjectParams(
            design_doc=design_doc,
            output_dir=str(out_root),
            project_name=project_name,
        )
    )
    result["steps"].append({"name": "generate_project_structure", "success": create.get("success")})
    if not create.get("success"):
        result["errors"].append(create.get("error"))
        return result

    project_path = create["project_path"]

    generated: Dict[str, Dict[str, Any]] = {}
    types_resp = tools["generate_types_events"](
        GenerateTypesEventsParams(
            design_doc=design_doc,
            project_path=project_path,
        )
    )
    generated["types"] = types_resp
    result["steps"].append({"name": "generate_types_events", "success": types_resp.get("success")})
    if not types_resp.get("success"):
        result["errors"].append(types_resp.get("error"))
        return result

    machine_names: List[str] = extract_machine_names_from_design_doc(design_doc)
    context_files = {"Enums_Types_Events.p": types_resp["code"]}

    for machine_name in machine_names:
        machine_resp = tools["generate_machine"](
            GenerateMachineParams(
                machine_name=machine_name,
                design_doc=design_doc,
                project_path=project_path,
                context_files=context_files,
            )
        )
        generated[f"machine:{machine_name}"] = machine_resp
        result["steps"].append({"name": f"generate_machine:{machine_name}", "success": machine_resp.get("success")})
        if machine_resp.get("success") and machine_resp.get("filename") and machine_resp.get("code"):
            context_files[machine_resp["filename"]] = machine_resp["code"]

    spec_resp = tools["generate_spec"](
        GenerateSpecParams(
            spec_name="Safety",
            design_doc=design_doc,
            project_path=project_path,
            context_files=context_files,
        )
    )
    generated["spec"] = spec_resp
    result["steps"].append({"name": "generate_spec", "success": spec_resp.get("success")})

    test_resp = tools["generate_test"](
        GenerateTestParams(
            test_name="TestDriver",
            design_doc=design_doc,
            project_path=project_path,
            context_files=context_files,
        )
    )
    generated["test"] = test_resp
    result["steps"].append({"name": "generate_test", "success": test_resp.get("success")})

    _save_generated(tools, generated)
    result["generated_files"] = sorted(
        [v["file_path"] for v in generated.values() if v.get("success") and v.get("file_path")]
    )

    compile_resp = tools["p_compile"](PCompileParams(path=project_path))
    result["compile"] = {
        "success": compile_resp.get("success"),
        "error": compile_resp.get("error"),
    }

    if not compile_resp.get("success"):
        fix_resp = tools["fix_iteratively"](
            FixIterativelyParams(project_path=project_path, max_iterations=8)
        )
        result["fix"] = fix_resp
        compile_resp = tools["p_compile"](PCompileParams(path=project_path))
        result["compile_after_fix"] = {
            "success": compile_resp.get("success"),
            "error": compile_resp.get("error"),
        }

    if not compile_resp.get("success"):
        result["errors"].append("Compilation failed after fix attempts")
        return result

    check_resp = tools["p_check"](
        PCheckParams(path=project_path, schedules=100, timeout=90)
    )
    result["check"] = {
        "success": check_resp.get("success"),
        "failed_tests": check_resp.get("failed_tests", []),
        "error": check_resp.get("error"),
    }

    # If PChecker found bugs, attempt automated fix and re-check (up to 2 rounds)
    MAX_CHECKER_FIX_ROUNDS = 2
    checker_fix_round = 0
    while not check_resp.get("success") and check_resp.get("failed_tests") and checker_fix_round < MAX_CHECKER_FIX_ROUNDS:
        checker_fix_round += 1
        logger.info(f"[CHECKER-FIX] Round {checker_fix_round}: attempting fix_buggy_program")
        fix_bug_resp = tools["fix_buggy_program"](
            FixBuggyProgramParams(project_path=project_path)
        )
        result.setdefault("checker_fixes", []).append(fix_bug_resp)

        if not fix_bug_resp.get("fixed"):
            break

        # Recompile after fix
        recompile = tools["p_compile"](PCompileParams(path=project_path))
        if not recompile.get("success"):
            result["errors"].append(f"Recompilation failed after checker fix round {checker_fix_round}")
            break

        # Re-check
        check_resp = tools["p_check"](
            PCheckParams(path=project_path, schedules=100, timeout=90)
        )
        result["check"] = {
            "success": check_resp.get("success"),
            "failed_tests": check_resp.get("failed_tests", []),
            "error": check_resp.get("error"),
        }

    result["success"] = bool(check_resp.get("success"))
    if not result["success"]:
        result["errors"].append("PChecker reported failing tests")
    return result


def main() -> int:
    root = PROJECT_ROOT
    docs_dir = root / "resources" / "system_design_docs"
    out_root = root / "generated_code" / "mcp_e2e" / datetime.now().strftime("%Y%m%d_%H%M%S")
    out_root.mkdir(parents=True, exist_ok=True)

    tools = _register_tools()
    env = tools["validate_environment"](ValidateEnvironmentParams())

    report: Dict[str, Any] = {
        "timestamp": datetime.now().isoformat(),
        "output_root": str(out_root),
        "environment": env,
        "protocols": [],
    }

    runs = [
        ("Paxos", docs_dir / "[Design Doc] Basic Paxos Protocol.txt"),
        ("TwoPhaseCommit", docs_dir / "[Design Doc] Two Phase Commit.txt"),
        ("MessageBroker", docs_dir / "[Design Doc] Simple Message Broker.txt"),
        ("DistributedLock", docs_dir / "[Design Doc] Distributed Lock Server.txt"),
        ("HotelManagement", docs_dir / "[Design Doc] Hotel Management Application.txt"),
    ]

    ensemble_size = 3  # Default ensemble size for higher reliability

    for name, path in runs:
        design_doc = _load_design_doc(path)
        proto_result = run_protocol_ensemble(
            tools, design_doc, out_root, name,
            ensemble_size=ensemble_size,
        )
        report["protocols"].append(proto_result)

    report_path = out_root / "mcp_e2e_report.json"
    report_path.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps(report, indent=2))
    print(f"\nReport written to: {report_path}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
