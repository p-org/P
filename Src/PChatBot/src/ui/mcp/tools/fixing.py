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
        guidance["steps"] = [
            "1. Find where the event is sent and add a handler in the receiving state",
            "2. Ensure the handler transitions to a valid state or defers/ignores the event",
            "3. Update specs/tests if expected behavior changes",
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

        trace_dir = project_path / "PCheckerOutput" / "BugFinding"
        if not trace_dir.exists():
            payload = {
                "success": False,
                "error": f"No PChecker output found at {trace_dir}. Run p_check first.",
            }
            return with_metadata("fix_buggy_program", payload)

        trace_files = list(trace_dir.glob("*_0_0.txt"))
        if not trace_files:
            payload = {
                "success": False,
                "error": "No trace files found. The program may have passed all tests.",
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

            if specialized_fix:
                logger.info(f"Attempting specialized fix: {specialized_fix.description}")

                try:
                    services["compilation"].write_file(
                        specialized_fix.file_path,
                        specialized_fix.fixed_code
                    )

                    response["fix_applied"] = {
                        "description": specialized_fix.description,
                        "file": specialized_fix.file_path,
                        "confidence": specialized_fix.confidence,
                        "requires_review": specialized_fix.requires_review,
                        "review_notes": specialized_fix.review_notes,
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
                        services["compilation"].write_file(
                            specialized_fix.file_path,
                            specialized_fix.original_code
                        )
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
