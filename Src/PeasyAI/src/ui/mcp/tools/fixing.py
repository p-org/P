"""Fixing-related MCP tools."""

from typing import Dict, Any, Optional
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
        name="fix_compiler_error",
        description="""Fix a P compiler error using AI.

After 3 failed attempts, returns needs_guidance=true with questions for the user.
If you receive needs_guidance, ask the user the questions and call again with user_guidance."""
    )
    def fix_compiler_error(params: FixCompilerErrorParams) -> Dict[str, Any]:
        logger.info(f"[TOOL] fix_compiler_error: {params.file_path}")

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

        return with_metadata("fix_compiler_error", response, token_usage=result.token_usage)

    @mcp.tool(
        name="fix_checker_error",
        description="""Fix a PChecker error using AI.

Analyzes the execution trace and fixes state machine logic issues.
After 3 failed attempts, returns needs_guidance=true with questions for the user.

If you receive needs_guidance, ask the user the questions and call again with user_guidance."""
    )
    def fix_checker_error(params: FixCheckerErrorParams) -> Dict[str, Any]:
        logger.info(f"[TOOL] fix_checker_error: {params.project_path}")

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

        return with_metadata("fix_checker_error", response, token_usage=result.token_usage)

    @mcp.tool(
        name="fix_iteratively",
        description="Iteratively fix compilation errors until success or max iterations reached"
    )
    def fix_iteratively(params: FixIterativelyParams) -> Dict[str, Any]:
        logger.info(f"[TOOL] fix_iteratively: {params.project_path}")

        services = get_services()
        result = services["fixer"].fix_iteratively(
            project_path=params.project_path,
            max_iterations=params.max_iterations
        )

        return with_metadata("fix_iteratively", result)

    @mcp.tool(
        name="fix_buggy_program",
        description="""Automatically fix a buggy P program based on PChecker trace analysis.

This tool:
1. Reads the latest PChecker trace from PCheckerOutput/BugFinding/
2. Analyzes the trace to identify the bug type (null_target, unhandled_event, assertion, deadlock)
3. Provides detailed root cause analysis
4. Attempts to automatically fix the bug
5. Verifies the fix by recompiling and re-running PChecker

Use this after p_check returns a failure to automatically diagnose and fix the bug.

Returns:
- analysis: Detailed breakdown of the error (machine, state, event, category)
- root_cause: Human-readable explanation of why the bug occurred
- suggested_fixes: List of specific fixes to apply
- fix_applied: Description of the fix that was applied (if auto-fix succeeded)
- fixed: Whether the bug was successfully fixed
- requires_manual_fix: If true, includes instructions for manual intervention"""
    )
    def fix_buggy_program(params: FixBuggyProgramParams) -> Dict[str, Any]:
        logger.info(f"[TOOL] fix_buggy_program: {params.project_path}")

        project_path = Path(params.project_path)

        # Find the most recent BugFinding*/ directory — that contains
        # the trace for the latest failing test.
        checker_output = project_path / "PCheckerOutput"
        if not checker_output.exists():
            payload = {
                "success": False,
                "error": f"No PChecker output found at {checker_output}. Run p_check first.",
            }
            return with_metadata("fix_buggy_program", payload)

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
            return with_metadata("fix_buggy_program", payload)

        # Use the most recently modified BugFinding*/ directory
        latest_bug_dir = bug_dirs[0]
        trace_files = list(latest_bug_dir.glob("*_0_0.txt"))

        if not trace_files:
            payload = {
                "success": False,
                "error": f"No trace files found in {latest_bug_dir.name}/. "
                         "The program may have passed all tests.",
            }
            return with_metadata("fix_buggy_program", payload)

        latest_trace = max(trace_files, key=lambda f: f.stat().st_mtime)

        try:
            trace_content = latest_trace.read_text()
        except Exception as e:
            payload = {
                "success": False,
                "error": f"Could not read trace file: {e}",
            }
            return with_metadata("fix_buggy_program", payload)

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
                    # Collect all backups for potential revert
                    backups = {}

                    # Apply primary fix
                    backups[specialized_fix.file_path] = specialized_fix.original_code
                    services["compilation"].write_file(
                        specialized_fix.file_path,
                        specialized_fix.fixed_code
                    )

                    # Apply additional patches for multi-file fixes
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
                            str(project_path), schedules=20, timeout=30
                        )

                        if check_result.success:
                            response["fixed"] = True
                            response["verification"] = "Fix verified - PChecker passed 20 schedules"
                        else:
                            response["fixed"] = False
                            response["verification"] = "Fix applied but bug persists"
                            response["requires_manual_fix"] = True
                    else:
                        # Revert ALL patches
                        for file_path, original_code in backups.items():
                            services["compilation"].write_file(file_path, original_code)
                        response["fix_applied"]["reverted"] = True
                        response["requires_manual_fix"] = True
                        response["verification"] = f"Fix caused compilation error: {compile_result.stdout[:200]}"

                except Exception as e:
                    logger.error(f"Error applying fix: {e}")
                    response["requires_manual_fix"] = True
                    response["fix_error"] = str(e)
            else:
                response["requires_manual_fix"] = True
                response["manual_fix_guidance"] = _get_manual_fix_guidance(analysis)

            return with_metadata("fix_buggy_program", response)

        except ImportError as e:
            logger.warning(f"Checker analysis modules not available: {e}")
            payload = _basic_trace_analysis(trace_content, str(project_path), services)
            return with_metadata("fix_buggy_program", payload)
        except Exception as e:
            logger.error(f"Error in fix_buggy_program: {e}")
            payload = {
                "success": False,
                "error": str(e),
                "trace_file": str(latest_trace),
            }
            return with_metadata("fix_buggy_program", payload)

    return {
        "fix_compiler_error": fix_compiler_error,
        "fix_checker_error": fix_checker_error,
        "fix_iteratively": fix_iteratively,
        "fix_buggy_program": fix_buggy_program,
    }
