"""Fixing-related MCP tools."""

from typing import Dict, Any, List, Optional, Tuple
from pydantic import BaseModel, Field
from pathlib import Path
import logging

from core.services.compilation import ParsedError

logger = logging.getLogger(__name__)


class FixCompilerErrorParams(BaseModel):
    """Parameters for fixing compilation errors"""
    project_path: str = Field(..., description="Absolute path to the P project")
    error_message: str = Field(..., description="The compiler error message")
    file_path: str = Field(..., description="Path to the file with the error")
    line_number: int = Field(default=0, description="Line number of the error")
    column_number: int = Field(default=0, description="Column number of the error")
    user_guidance: Optional[str] = Field(
        default=None,
        description="User guidance after failed attempts (provide when needs_guidance was returned)"
    )


class FixCheckerErrorParams(BaseModel):
    """Parameters for fixing PChecker errors"""
    project_path: str = Field(..., description="Absolute path to the P project")
    trace_log: str = Field(..., description="The PChecker trace log showing the error")
    error_category: Optional[str] = Field(
        default=None,
        description="Category of the error (e.g., 'assertion_failure', 'deadlock')"
    )
    user_guidance: Optional[str] = Field(
        default=None,
        description="User guidance after failed attempts"
    )


class FixIterativelyParams(BaseModel):
    """Parameters for iterative compilation fixing"""
    project_path: str = Field(..., description="Absolute path to the P project")
    max_iterations: int = Field(default=10, description="Maximum fix iterations")


class FixBuggyProgramParams(BaseModel):
    """Parameters for fixing a buggy P program after PChecker failure"""
    project_path: str = Field(..., description="Absolute path to the P project")
    test_name: Optional[str] = Field(
        default=None,
        description="Name of the failed test (optional, auto-detected from latest run)"
    )


def _get_manual_fix_guidance(analysis) -> Dict[str, Any]:
    """Generate detailed manual fix guidance based on error category."""
    from core.compilation import CheckerErrorCategory

    error = analysis.error
    guidance = {
        "category": error.category.value,
        "steps": [],
        "recommended_changes": [],
        "example_fix": None
    }

    if error.category == CheckerErrorCategory.UNHANDLED_EVENT:
        if error.is_test_driver_bug:
            guidance["steps"] = [
                "1. Identify why the test driver is sending this protocol event",
                "2. Create a dedicated setup event in Enums_Types_Events.p",
                "3. Add a handler for the setup event in the receiving machine's Init state",
                "4. Update the test driver to use the new setup event instead",
                "5. Add 'ignore' for the protocol event in all machines that may receive it",
                "6. Ensure safety specs can still observe the events they monitor",
            ]
            guidance["example_fix"] = f"""
// In Enums_Types_Events.p — add a dedicated setup event:
event eSetup{error.machine_type}Components: seq[machine];

// In {error.machine_type}.p — handle the setup event:
start state Init {{
    entry InitEntry;
    on eSetup{error.machine_type}Components do (payload: seq[machine]) {{
        components = payload;
    }}
    ignore {error.event_name};
}}

// In TestDriver.p — use the new setup event instead:
send {error.machine_type.lower()}, eSetup{error.machine_type}Components, allComponents;
"""
        else:
            guidance["steps"] = [
                "1. Trace back to the sender — find where the event is sent",
                "2. Determine if this is a test-driver bug or protocol-logic bug",
                "3. Add a handler, ignore, or defer in the receiving state",
                "4. Check if other machines also lack handlers for this event",
                "5. Update specs/tests if expected behavior changes",
            ]
            guidance["example_fix"] = f"""
state {error.machine_state} {{
    // Option A: Ignore the event
    ignore {error.event_name};
    
    // Option B: Defer until another state
    defer {error.event_name};
    
    // Option C: Handle explicitly
    on {error.event_name} do Handle{error.event_name.replace('e', '', 1)};
}}
"""
    elif error.category == CheckerErrorCategory.ASSERTION_FAILURE:
        guidance["steps"] = [
            "1. Examine the assertion that failed in the trace",
            "2. Identify what condition was expected vs actual",
            "3. Trace back through the execution to find where the invariant was violated",
            "4. Fix the logic that leads to the invalid state",
        ]
    elif error.category == CheckerErrorCategory.DEADLOCK:
        guidance["steps"] = [
            "1. Check which machines are waiting and in which states",
            "2. Look for circular dependencies in event handling",
            "3. Ensure all expected events are being sent",
            "4. Add timeout mechanisms if appropriate",
        ]

    return guidance


def _try_llm_checker_fix(
    services: Dict[str, Any],
    project_path: str,
    project_files: Dict[str, str],
    trace_content: str,
    analysis,
    response: Dict[str, Any],
) -> bool:
    """
    LLM-based fallback for checker errors the specialised fixer cannot handle.

    Sends the trace excerpt + all project files to the LLM and asks it to
    produce a fixed version of the most-likely-broken file.

    Returns True if the fix compiled and passed PChecker, False otherwise.
    """
    try:
        from core.llm.base import Message, MessageRole, LLMConfig

        error = analysis.error
        # Determine which file to ask the LLM to fix.
        # Heuristic: assertion failures → spec file, others → the machine file.
        target_file = None
        target_content = None
        from core.compilation import CheckerErrorCategory

        if error.category == CheckerErrorCategory.ASSERTION_FAILURE:
            for fname, content in project_files.items():
                if "PSpec" in fname or "spec " in content:
                    target_file = fname
                    target_content = content
                    break
        if not target_file:
            for fname, content in project_files.items():
                if error.machine_type and f"machine {error.machine_type}" in content:
                    target_file = fname
                    target_content = content
                    break
        if not target_file:
            return False

        # Build context from other files
        context_parts = []
        for fname, content in project_files.items():
            if fname != target_file:
                short = content[:3000] if len(content) > 3000 else content
                context_parts.append(f"<{Path(fname).name}>\n{short}\n</{Path(fname).name}>")

        # Truncate trace to last 150 lines
        trace_lines = trace_content.splitlines()
        trace_excerpt = "\n".join(trace_lines[-150:]) if len(trace_lines) > 150 else trace_content

        target_basename = Path(target_file).name
        messages = [
            Message(role=MessageRole.USER, content="\n".join(context_parts)),
            Message(role=MessageRole.USER, content=(
                f"The P project compiles but PChecker found a bug.\n\n"
                f"Error category: {error.category.value}\n"
                f"Error message: {error.message}\n"
                f"Machine: {error.machine_type}, State: {error.machine_state}\n\n"
                f"Trace excerpt (last 150 lines):\n```\n{trace_excerpt}\n```\n\n"
                f"The file that most likely needs fixing is {target_basename}:\n"
                f"```\n{target_content}\n```\n\n"
                f"Please rewrite {target_basename} to fix this bug. "
                f"Return the complete file in <{target_basename}>...</{target_basename}> tags."
            )),
        ]

        import re as _re
        provider = services["llm_provider"]
        system_prompt = services["resources"].load_context("about_p.txt")
        config = LLMConfig(max_tokens=4096)
        llm_resp = provider.complete(messages, config, system_prompt)

        from core.compilation.p_code_utils import extract_p_code_from_response
        _, new_code = extract_p_code_from_response(
            llm_resp.content, expected_filename=target_basename
        )
        if not new_code:
            return False
        backup = target_content

        # Resolve the full path for writing
        full_path = target_file
        if not Path(full_path).is_absolute():
            full_path = str(Path(project_path) / target_file)

        services["compilation"].write_file(full_path, new_code)

        compile_result = services["compilation"].compile(project_path)
        if not compile_result.success:
            services["compilation"].write_file(full_path, backup)
            return False

        check_result = services["compilation"].run_checker(
            project_path, schedules=50, timeout=60
        )
        if check_result.success:
            response["fixed"] = True
            response["verification"] = "LLM-based fix verified - PChecker passed 50 schedules"
            response["fix_applied"] = {
                "description": f"LLM rewrote {target_basename} based on trace analysis",
                "file": full_path,
                "strategy": "llm_fallback",
            }
            logger.info(f"LLM checker fix succeeded for {target_basename}")
            return True
        else:
            services["compilation"].write_file(full_path, backup)
            return False

    except Exception as e:
        logger.warning(f"LLM checker fix fallback failed: {e}")
        return False


def _basic_trace_analysis(trace_content: str, project_path: str, services) -> Dict[str, Any]:
    """Basic trace analysis without specialized modules."""
    import re

    error_match = re.search(r'<ErrorLog>\s*(.+)', trace_content)
    error_msg = error_match.group(1) if error_match else "Unknown error"

    if "null" in error_msg.lower():
        category = "null_target"
    elif "cannot be handled" in error_msg.lower():
        category = "unhandled_event"
    elif "assert" in error_msg.lower():
        category = "assertion_failure"
    elif "deadlock" in error_msg.lower():
        category = "deadlock"
    else:
        category = "unknown"

    return {
        "success": True,
        "analysis": {
            "error_category": category,
            "error_message": error_msg,
        },
        "root_cause": f"PChecker found a {category.replace('_', ' ')} error",
        "suggested_fixes": [
            "Review the trace file for detailed execution path",
            "Check the error message for specific machine and state information",
        ],
        "fixed": False,
        "requires_manual_fix": True,
    }


def register_fixing_tools(mcp, get_services, with_metadata):
    """Register fixing tools."""

    @mcp.tool(
        name="peasy-ai-fix-compile-error",
        description="""Fix a single P compiler error using AI. Provide the error message, file path, and optionally line/column numbers from the peasy-ai-compile output.

To fix all compilation errors at once, use peasy-ai-fix-all instead.

After 3 failed attempts, returns needs_guidance=true with questions for the user. If you receive needs_guidance, ask the user the questions and call again with user_guidance."""
    )
    def fix_compiler_error(params: FixCompilerErrorParams) -> Dict[str, Any]:
        logger.info(f"[TOOL] peasy-ai-fix-compile-error: {params.file_path}")

        services = get_services()

        error = ParsedError(
            file_path=params.file_path,
            line_number=params.line_number,
            column_number=params.column_number,
            message=params.error_message
        )

        result = services["fixer"].fix_compilation_error(
            project_path=params.project_path,
            error=error,
            user_guidance=params.user_guidance
        )

        response = {
            "success": result.success,
            "fixed": result.fixed,
            "filename": result.filename,
            "file_path": result.file_path,
            "error": result.error,
            "token_usage": result.token_usage
        }

        if result.needs_guidance:
            response["needs_guidance"] = True
            response["guidance_request"] = result.guidance_request

        return with_metadata("peasy-ai-fix-compile-error", response, token_usage=result.token_usage)

    @mcp.tool(
        name="peasy-ai-fix-checker-error",
        description="""Fix a PChecker error using AI.

Analyzes the execution trace and fixes state machine logic issues.
After 3 failed attempts, returns needs_guidance=true with questions for the user.

If you receive needs_guidance, ask the user the questions and call again with user_guidance."""
    )
    def fix_checker_error(params: FixCheckerErrorParams) -> Dict[str, Any]:
        logger.info(f"[TOOL] peasy-ai-fix-checker-error: {params.project_path}")

        services = get_services()
        result = services["fixer"].fix_checker_error(
            project_path=params.project_path,
            trace_log=params.trace_log,
            error_category=params.error_category,
            user_guidance=params.user_guidance
        )

        response = {
            "success": result.success,
            "fixed": result.fixed,
            "error": result.error,
            "token_usage": result.token_usage
        }

        if result.analysis:
            response["analysis"] = result.analysis

        if result.root_cause:
            response["root_cause"] = result.root_cause

        if result.suggested_fixes:
            response["suggested_fixes"] = result.suggested_fixes

        if result.confidence > 0:
            response["confidence"] = result.confidence

        if result.needs_guidance:
            response["needs_guidance"] = True
            response["guidance_request"] = result.guidance_request

        if result.fixed:
            response["filename"] = result.filename
            response["file_path"] = result.file_path

        # Surface vacuous pass warnings if present
        if result.analysis and "vacuous_pass_warning" in result.analysis:
            response["vacuous_pass_warning"] = result.analysis["vacuous_pass_warning"]

        return with_metadata("peasy-ai-fix-checker-error", response, token_usage=result.token_usage)

    @mcp.tool(
        name="peasy-ai-fix-all",
        description="Iteratively compile, detect errors, and fix them in a loop until the project compiles successfully or max_iterations is reached. This is the recommended way to fix multiple compilation errors at once — it automatically re-compiles after each fix to catch cascading issues. Use this instead of calling peasy-ai-fix-compile-error repeatedly."
    )
    def fix_iteratively(params: FixIterativelyParams) -> Dict[str, Any]:
        logger.info(f"[TOOL] peasy-ai-fix-all: {params.project_path}")

        services = get_services()
        result = services["fixer"].fix_iteratively(
            project_path=params.project_path,
            max_iterations=params.max_iterations
        )

        return with_metadata("peasy-ai-fix-all", result)

    @mcp.tool(
        name="peasy-ai-fix-bug",
        description="""Automatically diagnose and fix a buggy P program after a PChecker failure.

Use this after peasy-ai-check returns a failure. It reads the latest PChecker trace from PCheckerOutput/BugFinding/, identifies the bug type (null_target, unhandled_event, assertion_failure, deadlock), provides root cause analysis, attempts an automatic fix, and verifies by recompiling and re-running PChecker.

If the auto-fix fails, returns requires_manual_fix=true with step-by-step guidance and example code for manual intervention."""
    )
    def fix_buggy_program(params: FixBuggyProgramParams) -> Dict[str, Any]:
        logger.info(f"[TOOL] peasy-ai-fix-bug: {params.project_path}")

        project_path = Path(params.project_path)

        # Find the most recent BugFinding*/ directory — that contains
        # the trace for the latest failing test.
        checker_output = project_path / "PCheckerOutput"
        if not checker_output.exists():
            payload = {
                "success": False,
                "error": f"No PChecker output found at {checker_output}. Run p_check first.",
            }
            return with_metadata("peasy-ai-fix-bug", payload)

        bug_dirs = sorted(
            [d for d in checker_output.glob("BugFinding*") if d.is_dir()],
            key=lambda d: d.stat().st_mtime,
            reverse=True,
        )
        if not bug_dirs:
            payload = {
                "success": False,
                "error": "No BugFinding directories found. Run p_check first.",
            }
            return with_metadata("peasy-ai-fix-bug", payload)

        # Collect traces from ALL BugFinding*/ directories (one per failing test).
        # We pick the best trace to analyze — preferring the one whose error
        # category is most actionable.
        all_traces: List[Tuple[Path, str]] = []
        for bug_dir in bug_dirs:
            for tf in bug_dir.glob("*_0_0.txt"):
                try:
                    all_traces.append((tf, tf.read_text()))
                except Exception:
                    pass

        if not all_traces:
            payload = {
                "success": False,
                "error": f"No trace files found in BugFinding directories. "
                         "The program may have passed all tests.",
            }
            return with_metadata("peasy-ai-fix-bug", payload)

        # Rank traces by error category actionability so we fix the most
        # impactful bug first.  Order: unhandled_event > null_target >
        # assertion_failure > deadlock > unknown.
        import re as _trace_re
        _PRIORITY = {"unhandled_event": 0, "null_target": 1, "assertion_failure": 2, "deadlock": 3}

        def _trace_priority(content: str) -> int:
            error_match = _trace_re.search(r'<ErrorLog>\s*(.+)', content)
            if not error_match:
                return 99
            msg = error_match.group(1).lower()
            if "cannot be handled" in msg or "unhandled" in msg:
                return _PRIORITY["unhandled_event"]
            if "null" in msg:
                return _PRIORITY["null_target"]
            if "assert" in msg:
                return _PRIORITY["assertion_failure"]
            if "deadlock" in msg:
                return _PRIORITY["deadlock"]
            return 99

        all_traces.sort(key=lambda t: _trace_priority(t[1]))
        latest_trace, trace_content = all_traces[0]

        services = get_services()

        try:
            from core.compilation import (
                PCheckerErrorParser,
                PCheckerErrorFixer,
                CheckerErrorCategory,
                analyze_and_suggest_fix,
            )

            project_files = services["compilation"].get_project_files(str(project_path))

            analysis, specialized_fix = analyze_and_suggest_fix(
                trace_content, str(project_path), project_files
            )

            response = {
                "success": True,
                "trace_file": str(latest_trace),
                "analysis": {
                    "error_category": analysis.error.category.value,
                    "error_message": analysis.error.message,
                    "machine": analysis.error.machine,
                    "machine_type": analysis.error.machine_type,
                    "state": analysis.error.machine_state,
                    "event": analysis.error.event_name,
                    "target_field": analysis.error.target_field,
                    "execution_steps": analysis.execution_steps,
                    "machines_involved": analysis.machines_involved,
                    "last_actions": analysis.last_actions,
                },
                "root_cause": analysis.error.root_cause,
                "suggested_fixes": analysis.error.suggested_fixes,
                "fixed": False,
                "requires_manual_fix": False,
            }

            # Include enhanced analysis fields in response
            if analysis.error.sender_info:
                sender = analysis.error.sender_info
                response["analysis"]["sender"] = {
                    "machine": sender.machine,
                    "state": sender.state,
                    "is_test_driver": sender.is_test_driver,
                    "is_initialization_pattern": sender.is_initialization_pattern,
                    "semantic_mismatch": sender.semantic_mismatch,
                }
            if analysis.error.cascading_impact:
                cascade = analysis.error.cascading_impact
                response["analysis"]["cascading_impact"] = {
                    "unhandled_in": cascade.unhandled_in,
                    "broadcasters": cascade.broadcasters,
                    "all_receivers": cascade.all_receivers,
                }
            response["analysis"]["is_test_driver_bug"] = analysis.error.is_test_driver_bug
            response["analysis"]["requires_new_event"] = analysis.error.requires_new_event
            response["analysis"]["requires_multi_file_fix"] = analysis.error.requires_multi_file_fix

            if specialized_fix:
                logger.info(f"Attempting specialized fix ({specialized_fix.fix_strategy or 'auto'}): {specialized_fix.description}")

                try:
                    backups = {}

                    backups[specialized_fix.file_path] = specialized_fix.original_code
                    services["compilation"].write_file(
                        specialized_fix.file_path,
                        specialized_fix.fixed_code
                    )

                    if specialized_fix.is_multi_file and specialized_fix.additional_patches:
                        for patch in specialized_fix.additional_patches:
                            backups[patch.file_path] = patch.original_code
                            services["compilation"].write_file(
                                patch.file_path,
                                patch.fixed_code
                            )

                    response["fix_applied"] = {
                        "description": specialized_fix.description,
                        "file": specialized_fix.file_path,
                        "confidence": specialized_fix.confidence,
                        "requires_review": specialized_fix.requires_review,
                        "review_notes": specialized_fix.review_notes,
                        "strategy": specialized_fix.fix_strategy,
                        "is_multi_file": specialized_fix.is_multi_file,
                        "files_modified": list(backups.keys()),
                    }

                    compile_result = services["compilation"].compile(str(project_path))

                    if compile_result.success:
                        check_result = services["compilation"].run_checker(
                            str(project_path), schedules=50, timeout=60
                        )

                        if check_result.success:
                            response["fixed"] = True
                            response["verification"] = "Fix verified - PChecker passed 50 schedules"
                        else:
                            response["fixed"] = False
                            response["verification"] = "Fix applied but bug persists"
                            # Revert specialized fix, then try LLM fallback
                            for file_path, original_code in backups.items():
                                services["compilation"].write_file(file_path, original_code)
                            response["fix_applied"]["reverted"] = True
                    else:
                        for file_path, original_code in backups.items():
                            services["compilation"].write_file(file_path, original_code)
                        response["fix_applied"]["reverted"] = True
                        response["verification"] = f"Fix caused compilation error: {compile_result.stdout[:200]}"

                except Exception as e:
                    import traceback
                    logger.error(f"Error applying fix: {type(e).__name__}: {e}\n{traceback.format_exc()}")
                    response["fix_error"] = f"{type(e).__name__}: {e}"

            # --- LLM fallback: if specialized fix didn't work, ask the LLM ---
            if not response.get("fixed"):
                llm_fix_ok = _try_llm_checker_fix(
                    services, str(project_path), project_files,
                    trace_content, analysis, response,
                )
                if not llm_fix_ok:
                    response["requires_manual_fix"] = True
                    response["manual_fix_guidance"] = _get_manual_fix_guidance(analysis)

            return with_metadata("peasy-ai-fix-bug", response)

        except ImportError as e:
            logger.warning(f"Checker analysis modules not available: {e}")
            payload = _basic_trace_analysis(trace_content, str(project_path), services)
            return with_metadata("peasy-ai-fix-bug", payload)
        except Exception as e:
            import traceback
            err_msg = f"{type(e).__name__}: {e}"
            logger.error(f"Error in fix_buggy_program: {err_msg}\n{traceback.format_exc()}")
            payload = {
                "success": False,
                "error": err_msg,
                "trace_file": str(latest_trace),
            }
            return with_metadata("peasy-ai-fix-bug", payload)

    return {
        "fix_compiler_error": fix_compiler_error,
        "fix_checker_error": fix_checker_error,
        "fix_iteratively": fix_iteratively,
        "fix_buggy_program": fix_buggy_program,
    }
