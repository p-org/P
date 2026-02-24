"""Compilation-related MCP tools."""

from typing import Dict, Any
from pydantic import BaseModel, Field
import logging

logger = logging.getLogger(__name__)

try:
    from core.compilation import parse_compilation_output
    HAS_NEW_COMPILATION = True
except ImportError:
    HAS_NEW_COMPILATION = False


class PCompileParams(BaseModel):
    """Parameters for compilation"""
    path: str = Field(
        ...,
        description="Absolute path to the P project directory (must contain .pproj file)"
    )


class PCheckParams(BaseModel):
    """Parameters for PChecker"""
    path: str = Field(..., description="Absolute path to the P project directory")
    schedules: int = Field(default=100, description="Number of schedules to explore")
    timeout: int = Field(default=60, description="Timeout in seconds")


def register_compilation_tools(mcp, get_services, with_metadata):
    """Register compilation tools."""

    @mcp.tool(
        name="peasy-ai-compile",
        description="Compile a P project and return compilation results. The project directory must contain a .pproj file. On failure, the response includes parsed errors with file, line, and message details. Use peasy-ai-fix-compile-error or peasy-ai-fix-all to resolve compilation errors."
    )
    def p_compile(params: PCompileParams) -> Dict[str, Any]:
        logger.info(f"[TOOL] peasy-ai-compile: {params.path}")

        services = get_services()
        result = services["compilation"].compile(params.path)

        response = {
            "success": result.success,
            "stdout": result.stdout,
            "stderr": result.stderr,
            "return_code": result.return_code,
            "error": result.error
        }

        if not result.success and HAS_NEW_COMPILATION:
            try:
                parsed = parse_compilation_output(result.stdout or result.stderr or "")
                if parsed.errors:
                    response["parsed_errors"] = [
                        {
                            "file": e.file,
                            "line": e.line,
                            "column": e.column,
                            "category": e.category.value,
                            "message": e.message,
                            "suggestion": e.suggestion,
                        }
                        for e in parsed.errors
                    ]
                    response["error_summary"] = f"Found {len(parsed.errors)} error(s)"
                    if parsed.errors:
                        first_error = parsed.errors[0]
                        response["first_error"] = {
                            "file": first_error.file,
                            "line": first_error.line,
                            "message": first_error.message,
                            "suggestion": first_error.suggestion,
                        }
            except Exception as e:
                logger.debug(f"Error parsing compilation output: {e}")

        return with_metadata("peasy-ai-compile", response)

    @mcp.tool(
        name="peasy-ai-check",
        description="Run PChecker on a compiled P project to verify correctness via model checking. The project must compile successfully first (use peasy-ai-compile). Explores random schedules to find concurrency bugs like deadlocks, assertion failures, and unhandled events. On failure, use peasy-ai-fix-checker-error or peasy-ai-fix-bug to diagnose and fix the bug."
    )
    def p_check(params: PCheckParams) -> Dict[str, Any]:
        logger.info(f"[TOOL] peasy-ai-check: {params.path}")

        services = get_services()
        result = services["compilation"].run_checker(
            project_path=params.path,
            schedules=params.schedules,
            timeout=params.timeout
        )

        payload = {
            "success": result.success,
            "test_results": result.test_results,
            "passed_tests": result.passed_tests,
            "failed_tests": result.failed_tests,
            "error": result.error
        }
        return with_metadata("peasy-ai-check", payload)

    return {"p_compile": p_compile, "p_check": p_check}
