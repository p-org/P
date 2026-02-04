"""
P ChatBot MCP Server for Cursor IDE Integration

This MCP server exposes tools for P code generation, compilation, and error fixing.
It uses the Phase 1 service layer for all operations, ensuring consistency with
the Streamlit and CLI interfaces.

Tools:
- Generation: generate_project, generate_types_events, generate_machine, generate_spec, generate_test
- Compilation: p_compile, p_check
- Fixing: fix_compiler_error, fix_checker_error
- Query: syntax_help, list_project_files, read_p_file
"""

from fastmcp import FastMCP
import logging
from typing import Dict, Any, Optional, List
from pydantic import BaseModel, Field
from pathlib import Path
import os
import sys

# ============================================================================
# PATH SETUP
# ============================================================================

# Add project root and src to path for imports
PROJECT_ROOT = Path(__file__).parent.parent.parent.parent
SRC_ROOT = Path(__file__).parent.parent.parent

if str(SRC_ROOT) not in sys.path:
    sys.path.insert(0, str(SRC_ROOT))
if str(PROJECT_ROOT) not in sys.path:
    sys.path.insert(0, str(PROJECT_ROOT))

# Change to project root for relative paths
os.chdir(str(PROJECT_ROOT))

# Load environment variables
from dotenv import load_dotenv
load_dotenv(PROJECT_ROOT / ".env")

# ============================================================================
# IMPORTS FROM PHASE 1 SERVICE LAYER
# ============================================================================

from core.llm import LLMProviderFactory, get_default_provider
from core.services import (
    GenerationService,
    CompilationService,
    FixerService,
)
from core.services.base import EventCallback, ResourceLoader
from core.services.compilation import ParsedError

# Import new compilation utilities
try:
    from core.compilation import (
        ensure_environment,
        EnvironmentDetector,
        PCompilerErrorParser,
        parse_compilation_output,
    )
    HAS_NEW_COMPILATION = True
except ImportError:
    HAS_NEW_COMPILATION = False

# ============================================================================
# MCP SERVER SETUP
# ============================================================================

mcp = FastMCP("P-ChatBot")

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='[%(levelname)s] %(message)s'
)
logger = logging.getLogger(__name__)

# ============================================================================
# SERVICE INITIALIZATION
# ============================================================================

# Lazy-initialized services (created on first use)
_services: Dict[str, Any] = {}


def get_services() -> Dict[str, Any]:
    """Get or create service instances"""
    if not _services:
        logger.info("Initializing services...")
        
        # Setup P development environment (auto-detect paths)
        if HAS_NEW_COMPILATION:
            env_info = ensure_environment()
            if env_info.is_valid:
                logger.info(f"P environment: P={env_info.p_compiler_path}, dotnet={env_info.dotnet_path}")
            else:
                logger.warning(f"P environment issues: {env_info.issues}")
        
        # Get LLM provider from environment
        provider = get_default_provider()
        logger.info(f"Using LLM provider: {provider.name}")
        
        # Create resource loader
        resource_loader = ResourceLoader(PROJECT_ROOT / "resources")
        
        # Create services with shared provider
        _services["generation"] = GenerationService(
            llm_provider=provider,
            resource_loader=resource_loader
        )
        _services["compilation"] = CompilationService(
            llm_provider=provider,
            resource_loader=resource_loader
        )
        _services["fixer"] = FixerService(
            llm_provider=provider,
            resource_loader=resource_loader,
            compilation_service=_services["compilation"]
        )
        _services["resources"] = resource_loader
        
        logger.info("Services initialized")
    
    return _services


# ============================================================================
# GENERATION TOOLS
# ============================================================================

class GenerateProjectParams(BaseModel):
    """Parameters for full project generation"""
    design_doc: str = Field(
        ..., 
        description="The design document content describing the P program. "
                    "Should include <title>, <introduction>, <components>, and <interactions>."
    )
    output_dir: str = Field(
        ..., 
        description="Absolute path to the directory where the project should be created"
    )
    project_name: str = Field(
        default="PProject", 
        description="Name for the P project"
    )


@mcp.tool(
    name="generate_project_structure",
    description="Create a P project skeleton with PSrc, PSpec, PTst folders and .pproj file"
)
def generate_project_structure(params: GenerateProjectParams) -> Dict[str, Any]:
    """Create P project structure with folders and .pproj file"""
    logger.info(f"[TOOL] generate_project_structure: {params.project_name}")
    
    services = get_services()
    result = services["generation"].create_project_structure(
        output_dir=params.output_dir,
        project_name=params.project_name
    )
    
    return {
        "success": result.success,
        "project_path": result.file_path,
        "project_name": result.filename,
        "error": result.error,
        "message": f"Created P project at {result.file_path}" if result.success else result.error
    }


class GenerateTypesEventsParams(BaseModel):
    """Parameters for types/events generation"""
    design_doc: str = Field(..., description="The design document content")
    project_path: str = Field(..., description="Absolute path to the P project root")


@mcp.tool(
    name="generate_types_events",
    description="Generate types, enums, and events file (Enums_Types_Events.p) from a design document. Returns code for preview - use save_p_file to save."
)
def generate_types_events(params: GenerateTypesEventsParams) -> Dict[str, Any]:
    """Generate Enums_Types_Events.p file (preview only, does not save)"""
    logger.info("[TOOL] generate_types_events (preview)")
    
    services = get_services()
    result = services["generation"].generate_types_events(
        design_doc=params.design_doc,
        project_path=params.project_path,
        save_to_disk=False  # Preview only
    )
    
    return {
        "success": result.success,
        "filename": result.filename,
        "file_path": result.file_path,
        "code": result.code,
        "error": result.error,
        "token_usage": result.token_usage,
        "preview_only": True,
        "message": "Code generated for preview. Use save_p_file to save to disk." if result.success else result.error
    }


class GenerateMachineParams(BaseModel):
    """Parameters for machine generation"""
    machine_name: str = Field(..., description="Name of the machine to generate")
    design_doc: str = Field(..., description="The design document content")
    project_path: str = Field(..., description="Absolute path to the P project root")
    context_files: Optional[Dict[str, str]] = Field(
        default=None, 
        description="Additional context files (filename -> content)"
    )


@mcp.tool(
    name="generate_machine",
    description="Generate a single P state machine implementation using two-stage generation (structure first, then implementation). Returns code for preview - use save_p_file to save."
)
def generate_machine(params: GenerateMachineParams) -> Dict[str, Any]:
    """Generate a P state machine file (preview only, does not save)"""
    logger.info(f"[TOOL] generate_machine: {params.machine_name} (preview)")
    
    services = get_services()
    result = services["generation"].generate_machine(
        machine_name=params.machine_name,
        design_doc=params.design_doc,
        project_path=params.project_path,
        context_files=params.context_files,
        two_stage=True,
        save_to_disk=False  # Preview only
    )
    
    return {
        "success": result.success,
        "filename": result.filename,
        "file_path": result.file_path,
        "code": result.code,
        "error": result.error,
        "token_usage": result.token_usage,
        "preview_only": True,
        "message": "Code generated for preview. Use save_p_file to save to disk." if result.success else result.error
    }


class GenerateSpecParams(BaseModel):
    """Parameters for specification generation"""
    spec_name: str = Field(..., description="Name of the specification file to generate")
    design_doc: str = Field(..., description="The design document content")
    project_path: str = Field(..., description="Absolute path to the P project root")
    context_files: Optional[Dict[str, str]] = Field(
        default=None, 
        description="Additional context files"
    )


@mcp.tool(
    name="generate_spec",
    description="Generate a P specification/monitor file. Returns code for preview - use save_p_file to save."
)
def generate_spec(params: GenerateSpecParams) -> Dict[str, Any]:
    """Generate a P specification file (preview only, does not save)"""
    logger.info(f"[TOOL] generate_spec: {params.spec_name} (preview)")
    
    services = get_services()
    result = services["generation"].generate_spec(
        spec_name=params.spec_name,
        design_doc=params.design_doc,
        project_path=params.project_path,
        context_files=params.context_files,
        save_to_disk=False  # Preview only
    )
    
    return {
        "success": result.success,
        "filename": result.filename,
        "file_path": result.file_path,
        "code": result.code,
        "error": result.error,
        "token_usage": result.token_usage,
        "preview_only": True,
        "message": "Code generated for preview. Use save_p_file to save to disk." if result.success else result.error
    }


class GenerateTestParams(BaseModel):
    """Parameters for test generation"""
    test_name: str = Field(..., description="Name of the test file to generate")
    design_doc: str = Field(..., description="The design document content")
    project_path: str = Field(..., description="Absolute path to the P project root")
    context_files: Optional[Dict[str, str]] = Field(
        default=None, 
        description="Additional context files"
    )


@mcp.tool(
    name="generate_test",
    description="Generate a P test file. Returns code for preview - use save_p_file to save."
)
def generate_test(params: GenerateTestParams) -> Dict[str, Any]:
    """Generate a P test file (preview only, does not save)"""
    logger.info(f"[TOOL] generate_test: {params.test_name} (preview)")
    
    services = get_services()
    result = services["generation"].generate_test(
        test_name=params.test_name,
        design_doc=params.design_doc,
        project_path=params.project_path,
        context_files=params.context_files,
        save_to_disk=False  # Preview only
    )
    
    return {
        "success": result.success,
        "filename": result.filename,
        "file_path": result.file_path,
        "code": result.code,
        "error": result.error,
        "token_usage": result.token_usage,
        "preview_only": True,
        "message": "Code generated for preview. Use save_p_file to save to disk." if result.success else result.error
    }


# ============================================================================
# FILE SAVE TOOL (for preview-then-save workflow)
# ============================================================================

class SavePFileParams(BaseModel):
    """Parameters for saving a P file"""
    file_path: str = Field(..., description="Absolute path where to save the file")
    code: str = Field(..., description="The P code content to save")


@mcp.tool(
    name="save_p_file",
    description="Save generated P code to a file. Use this after previewing code from generate_* tools and user approves."
)
def save_p_file(params: SavePFileParams) -> Dict[str, Any]:
    """Save P code to a file"""
    logger.info(f"[TOOL] save_p_file: {params.file_path}")
    
    services = get_services()
    result = services["generation"].save_p_file(
        file_path=params.file_path,
        code=params.code
    )
    
    return {
        "success": result.success,
        "filename": result.filename,
        "file_path": result.file_path,
        "error": result.error,
        "message": f"Saved {result.filename} to disk" if result.success else result.error
    }


# ============================================================================
# COMPILATION TOOLS
# ============================================================================

class PCompileParams(BaseModel):
    """Parameters for compilation"""
    path: str = Field(
        ..., 
        description="Absolute path to the P project directory (must contain .pproj file)"
    )


@mcp.tool(
    name="p_compile",
    description="Compile a P project and return compilation results"
)
def p_compile(params: PCompileParams) -> Dict[str, Any]:
    """Compile a P project"""
    logger.info(f"[TOOL] p_compile: {params.path}")
    
    services = get_services()
    result = services["compilation"].compile(params.path)
    
    response = {
        "success": result.success,
        "stdout": result.stdout,
        "stderr": result.stderr,
        "return_code": result.return_code,
        "error": result.error
    }
    
    # Add structured error information if compilation failed
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
                # Add summary
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
    
    return response


class PCheckParams(BaseModel):
    """Parameters for PChecker"""
    path: str = Field(..., description="Absolute path to the P project directory")
    schedules: int = Field(default=100, description="Number of schedules to explore")
    timeout: int = Field(default=60, description="Timeout in seconds")


@mcp.tool(
    name="p_check",
    description="Run PChecker on a P project to verify correctness via model checking"
)
def p_check(params: PCheckParams) -> Dict[str, Any]:
    """Run PChecker on a P project"""
    logger.info(f"[TOOL] p_check: {params.path}")
    
    services = get_services()
    result = services["compilation"].run_checker(
        project_path=params.path,
        schedules=params.schedules,
        timeout=params.timeout
    )
    
    return {
        "success": result.success,
        "test_results": result.test_results,
        "passed_tests": result.passed_tests,
        "failed_tests": result.failed_tests,
        "error": result.error
    }


# ============================================================================
# FIX TOOLS WITH HUMAN-IN-THE-LOOP
# ============================================================================

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


@mcp.tool(
    name="fix_compiler_error",
    description="""Fix a P compiler error using AI.
    
After 3 failed attempts, returns needs_guidance=true with questions for the user.
If you receive needs_guidance, ask the user the questions and call again with user_guidance."""
)
def fix_compiler_error(params: FixCompilerErrorParams) -> Dict[str, Any]:
    """Fix a compilation error with human-in-the-loop fallback"""
    logger.info(f"[TOOL] fix_compiler_error: {params.file_path}")
    
    services = get_services()
    
    # Create ParsedError from params
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
    
    # Add human-in-the-loop fields if needed
    if result.needs_guidance:
        response["needs_guidance"] = True
        response["guidance_request"] = result.guidance_request
    
    return response


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


@mcp.tool(
    name="fix_checker_error",
    description="""Fix a PChecker error using AI.
    
Analyzes the execution trace and fixes state machine logic issues.
After 3 failed attempts, returns needs_guidance=true with questions for the user.

If you receive needs_guidance, ask the user the questions and call again with user_guidance."""
)
def fix_checker_error(params: FixCheckerErrorParams) -> Dict[str, Any]:
    """Fix a PChecker error with enhanced analysis and human-in-the-loop fallback"""
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
    
    # Add enhanced analysis (always included when available)
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
    
    # Include file info if fix was applied
    if result.fixed:
        response["filename"] = result.filename
        response["file_path"] = result.file_path
    
    return response


class FixIterativelyParams(BaseModel):
    """Parameters for iterative compilation fixing"""
    project_path: str = Field(..., description="Absolute path to the P project")
    max_iterations: int = Field(default=10, description="Maximum fix iterations")


@mcp.tool(
    name="fix_iteratively",
    description="Iteratively fix compilation errors until success or max iterations reached"
)
def fix_iteratively(params: FixIterativelyParams) -> Dict[str, Any]:
    """Iteratively fix compilation errors"""
    logger.info(f"[TOOL] fix_iteratively: {params.project_path}")
    
    services = get_services()
    result = services["fixer"].fix_iteratively(
        project_path=params.project_path,
        max_iterations=params.max_iterations
    )
    
    return result


class FixBuggyProgramParams(BaseModel):
    """Parameters for fixing a buggy P program after PChecker failure"""
    project_path: str = Field(..., description="Absolute path to the P project")
    test_name: Optional[str] = Field(
        default=None,
        description="Name of the failed test (optional, auto-detected from latest run)"
    )


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
    """Automatically fix a buggy P program based on PChecker trace"""
    logger.info(f"[TOOL] fix_buggy_program: {params.project_path}")
    
    project_path = Path(params.project_path)
    
    # Step 1: Find the latest trace file
    trace_dir = project_path / "PCheckerOutput" / "BugFinding"
    if not trace_dir.exists():
        return {
            "success": False,
            "error": f"No PChecker output found at {trace_dir}. Run p_check first.",
        }
    
    # Find trace files (format: ProjectName_0_0.txt)
    trace_files = list(trace_dir.glob("*_0_0.txt"))
    if not trace_files:
        return {
            "success": False,
            "error": "No trace files found. The program may have passed all tests.",
        }
    
    # Get the most recent trace file
    latest_trace = max(trace_files, key=lambda f: f.stat().st_mtime)
    
    try:
        trace_content = latest_trace.read_text()
    except Exception as e:
        return {
            "success": False,
            "error": f"Could not read trace file: {e}",
        }
    
    # Step 2: Analyze the trace
    services = get_services()
    
    try:
        from core.compilation import (
            PCheckerErrorParser,
            PCheckerErrorFixer,
            CheckerErrorCategory,
            analyze_and_suggest_fix,
        )
        
        # Get project files for context
        project_files = services["compilation"].get_project_files(str(project_path))
        
        # Analyze the trace
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
        
        # Step 3: Attempt automatic fix
        if specialized_fix:
            logger.info(f"Attempting specialized fix: {specialized_fix.description}")
            
            # Apply the fix
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
                
                # Verify by recompiling
                compile_result = services["compilation"].compile(str(project_path))
                
                if compile_result.success:
                    # Run quick check to verify
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
                    # Revert the fix
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
            # No automatic fix available
            response["requires_manual_fix"] = True
            response["manual_fix_guidance"] = _get_manual_fix_guidance(analysis)
        
        return response
        
    except ImportError as e:
        logger.warning(f"Checker analysis modules not available: {e}")
        # Fall back to basic analysis
        return _basic_trace_analysis(trace_content, str(project_path), services)
    except Exception as e:
        logger.error(f"Error in fix_buggy_program: {e}")
        return {
            "success": False,
            "error": str(e),
            "trace_file": str(latest_trace),
        }


def _get_manual_fix_guidance(analysis) -> Dict[str, Any]:
    """Generate detailed manual fix guidance based on error category."""
    from core.compilation import CheckerErrorCategory
    
    error = analysis.error
    guidance = {
        "category": error.category.value,
        "steps": [],
    }
    
    if error.category == CheckerErrorCategory.NULL_TARGET:
        guidance["steps"] = [
            f"1. Open the file containing machine '{error.machine_type}'",
            f"2. Find where event '{error.event_name}' is sent (look for 'send ... {error.event_name}')",
            f"3. Identify the target variable being used (likely '{error.target_field or 'unknown'}')",
            "4. Ensure this variable is initialized before the send statement",
            "5. Options to fix:",
            f"   a) Add 'with FunctionName' to the transition entering state '{error.machine_state}' to capture the reference",
            "   b) Add a configuration event handler in the start state to receive the reference",
            "   c) Pass the reference as part of the machine creation parameters",
        ]
        guidance["code_example"] = f"""
// Option A: Use 'with' clause on transition
on eYourEvent goto {error.machine_state} with SaveReference;

fun SaveReference(payload: tYourPayloadType) {{
    {error.target_field or 'targetMachine'} = payload.machineRef;
}}

// Option B: Add configuration event
start state Init {{
    entry Initialize;
    on eConfigureMachine do Configure;
}}

fun Configure(config: tMachineConfig) {{
    {error.target_field or 'targetMachine'} = config.targetRef;
}}
"""
    
    elif error.category == CheckerErrorCategory.UNHANDLED_EVENT:
        guidance["steps"] = [
            f"1. Open the file containing machine '{error.machine_type}'",
            f"2. Find state '{error.machine_state}'",
            f"3. Add handling for event '{error.event_name}'",
            "4. Options:",
            f"   a) Add 'ignore {error.event_name};' to silently drop the event",
            f"   b) Add 'defer {error.event_name};' to handle it later",
            f"   c) Add 'on {error.event_name} do HandlerFunction;' for custom handling",
        ]
        guidance["code_example"] = f"""
state {error.machine_state} {{
    // ... existing handlers ...
    
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
    
    # Extract error line
    error_match = re.search(r'<ErrorLog>\s*(.+)', trace_content)
    error_msg = error_match.group(1) if error_match else "Unknown error"
    
    # Detect error type
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


# ============================================================================
# QUERY TOOLS
# ============================================================================

class SyntaxHelperParams(BaseModel):
    """Parameters for syntax help"""
    topic: str = Field(
        ..., 
        description="The P language topic to get help on (e.g., 'state machines', 'events', 'types', 'send', 'goto')"
    )


@mcp.tool(
    name="syntax_help",
    description="Get syntax help and examples for P language constructs"
)
def syntax_help(params: SyntaxHelperParams) -> Dict[str, Any]:
    """Get syntax help for P language constructs"""
    logger.info(f"[TOOL] syntax_help: {params.topic}")
    
    services = get_services()
    resources = services["resources"]
    
    topic_lower = params.topic.lower()
    
    # Map topics to context files
    topic_files = {
        "machine": "modular/p_machines_guide.txt",
        "state": "modular/p_machines_guide.txt",
        "event": "modular/p_events_guide.txt",
        "type": "modular/p_types_guide.txt",
        "enum": "modular/p_enums_guide.txt",
        "statement": "modular/p_statements_guide.txt",
        "spec": "modular/p_spec_monitors_guide.txt",
        "monitor": "modular/p_spec_monitors_guide.txt",
        "test": "modular/p_test_cases_guide.txt",
        "module": "modular/p_module_system_guide.txt",
        "syntax": "P_syntax_guide.txt",
        "basic": "modular/p_basics.txt",
        "example": "modular/p_program_example.txt",
        "compiler": "modular/p_compiler_guide.txt",
        "error": "modular/p_common_compilation_errors.txt",
        "send": "modular/p_statements_guide.txt",
        "goto": "modular/p_machines_guide.txt",
        "raise": "modular/p_statements_guide.txt",
    }
    
    # Find matching files
    matching_files = []
    for keyword, filepath in topic_files.items():
        if keyword in topic_lower:
            if filepath not in matching_files:
                matching_files.append(filepath)
    
    if not matching_files:
        matching_files = ["P_syntax_guide.txt", "modular/p_basics.txt"]
    
    # Read and combine content
    content_parts = []
    for filepath in matching_files[:3]:
        try:
            content = resources.load(f"context_files/{filepath}")
            content_parts.append(f"=== {filepath} ===\n{content}")
        except Exception as e:
            logger.warning(f"Could not load {filepath}: {e}")
    
    return {
        "topic": params.topic,
        "content": "\n\n".join(content_parts),
        "files_referenced": matching_files[:3]
    }


class ListProjectFilesParams(BaseModel):
    """Parameters for listing project files"""
    project_path: str = Field(..., description="Absolute path to the P project")


@mcp.tool(
    name="list_project_files",
    description="List all P files in a project organized by folder (PSrc, PSpec, PTst)"
)
def list_project_files(params: ListProjectFilesParams) -> Dict[str, Any]:
    """List all P files in a project"""
    logger.info(f"[TOOL] list_project_files: {params.project_path}")
    
    services = get_services()
    files = services["compilation"].get_project_files(params.project_path)
    
    # Organize by folder
    organized = {"PSrc": [], "PSpec": [], "PTst": []}
    for filepath in files.keys():
        folder = filepath.split("/")[0] if "/" in filepath else "other"
        if folder in organized:
            organized[folder].append(filepath)
    
    return {
        "project_path": params.project_path,
        "files": organized,
        "total_files": len(files)
    }


class ReadPFileParams(BaseModel):
    """Parameters for reading a P file"""
    file_path: str = Field(..., description="Absolute path to the P file")


@mcp.tool(
    name="read_p_file",
    description="Read the contents of a P file"
)
def read_p_file(params: ReadPFileParams) -> Dict[str, Any]:
    """Read a P file's contents"""
    logger.info(f"[TOOL] read_p_file: {params.file_path}")
    
    services = get_services()
    content = services["compilation"].read_file(params.file_path)
    
    if content is not None:
        return {
            "success": True,
            "file_path": params.file_path,
            "content": content,
            "lines": len(content.splitlines())
        }
    else:
        return {
            "success": False,
            "error": f"Could not read file: {params.file_path}"
        }


# ============================================================================
# WORKFLOW TOOLS (Phase 3)
# ============================================================================

from core.workflow import (
    WorkflowEngine,
    WorkflowFactory,
    EventEmitter,
    WorkflowEvent,
    LoggingEventListener,
    extract_machine_names_from_design_doc,
)


class RunWorkflowParams(BaseModel):
    """Parameters for running a workflow"""
    workflow_name: str = Field(description="Name of the workflow to run: 'full_generation', 'compile_and_fix', 'full_verification', 'quick_check'")
    project_path: str = Field(description="Absolute path to the P project directory")
    design_doc: Optional[str] = Field(default=None, description="Design document (required for generation workflows)")
    machine_names: Optional[List[str]] = Field(default=None, description="List of machine names (auto-extracted from design_doc if not provided)")
    schedules: int = Field(default=100, description="Number of schedules for PChecker")
    timeout: int = Field(default=60, description="Timeout in seconds for PChecker")


class ResumeWorkflowParams(BaseModel):
    """Parameters for resuming a paused workflow"""
    workflow_id: str = Field(description="ID of the paused workflow")
    user_guidance: str = Field(description="User guidance to continue the workflow")


class ListWorkflowsParams(BaseModel):
    """Parameters for listing workflows"""
    pass


# Workflow engine instance (lazy-initialized)
_workflow_engine: Optional[WorkflowEngine] = None
_workflow_factory: Optional[WorkflowFactory] = None


def get_workflow_engine() -> tuple[WorkflowEngine, WorkflowFactory]:
    """Get or create workflow engine and factory"""
    global _workflow_engine, _workflow_factory
    
    if _workflow_engine is None:
        services = get_services()
        
        # Create event emitter with logging
        emitter = EventEmitter()
        emitter.on_all(LoggingEventListener(verbose=True))
        
        # Create engine and factory
        _workflow_engine = WorkflowEngine(emitter)
        _workflow_factory = WorkflowFactory(
            generation_service=services["generation"],
            compilation_service=services["compilation"],
            fixer_service=services["fixer"]
        )
        
        # Register standard workflows
        _workflow_engine.register_workflow(
            _workflow_factory.create_compile_and_fix_workflow()
        )
        _workflow_engine.register_workflow(
            _workflow_factory.create_full_verification_workflow()
        )
        _workflow_engine.register_workflow(
            _workflow_factory.create_quick_check_workflow()
        )
        
        logger.info("Workflow engine initialized")
    
    return _workflow_engine, _workflow_factory


@mcp.tool()
def run_workflow(params: RunWorkflowParams) -> Dict[str, Any]:
    """
    Execute a predefined workflow.
    
    Available workflows:
    - full_generation: Generate complete P project from design doc
    - compile_and_fix: Compile and automatically fix errors
    - full_verification: Compile, fix, and run PChecker
    - quick_check: Run PChecker only
    
    For full_generation, provide design_doc and optionally machine_names.
    """
    engine, factory = get_workflow_engine()
    
    # Build context
    context = {
        "project_path": params.project_path,
    }
    
    if params.design_doc:
        context["design_doc"] = params.design_doc
    
    # Handle full_generation workflow specially
    if params.workflow_name == "full_generation":
        if not params.design_doc:
            return {
                "success": False,
                "error": "design_doc is required for full_generation workflow"
            }
        
        # Extract or use provided machine names
        machine_names = params.machine_names
        if not machine_names:
            machine_names = extract_machine_names_from_design_doc(params.design_doc)
            if not machine_names:
                return {
                    "success": False,
                    "error": "Could not extract machine names from design_doc. Please provide machine_names explicitly."
                }
        
        # Create and register the workflow
        workflow = factory.create_full_generation_workflow(
            machine_names=machine_names
        )
        engine.register_workflow(workflow)
    
    # Execute workflow
    try:
        result = engine.execute(params.workflow_name, context)
        
        # Check if paused for human input
        if result.get("needs_guidance"):
            return {
                "success": False,
                "paused": True,
                "workflow_id": result.get("_workflow_id"),
                "guidance_needed": result.get("guidance_context"),
                "message": "Workflow paused - human guidance needed"
            }
        
        return {
            "success": result.get("success", False),
            "completed_steps": result.get("completed_steps", []),
            "skipped_steps": result.get("skipped_steps", []),
            "errors": result.get("errors", [])
        }
        
    except Exception as e:
        logger.error(f"Workflow execution failed: {e}")
        return {
            "success": False,
            "error": str(e)
        }


@mcp.tool()
def resume_workflow(params: ResumeWorkflowParams) -> Dict[str, Any]:
    """
    Resume a paused workflow with user guidance.
    
    Call this after a workflow has paused for human input.
    Provide the workflow_id from the paused response and your guidance.
    """
    engine, _ = get_workflow_engine()
    
    try:
        result = engine.resume(params.workflow_id, params.user_guidance)
        
        if result.get("needs_guidance"):
            return {
                "success": False,
                "paused": True,
                "workflow_id": result.get("_workflow_id"),
                "guidance_needed": result.get("guidance_context"),
                "message": "Workflow still needs guidance"
            }
        
        return {
            "success": result.get("success", False),
            "completed_steps": result.get("completed_steps", []),
            "errors": result.get("errors", [])
        }
        
    except ValueError as e:
        return {
            "success": False,
            "error": str(e)
        }
    except Exception as e:
        logger.error(f"Workflow resume failed: {e}")
        return {
            "success": False,
            "error": str(e)
        }


@mcp.tool()
def list_workflows(params: ListWorkflowsParams) -> Dict[str, Any]:
    """
    List available workflows and any active/paused workflows.
    """
    engine, _ = get_workflow_engine()
    
    available = [
        {
            "name": "full_generation",
            "description": "Generate complete P project from design document",
            "requires": ["design_doc", "project_path"]
        },
        {
            "name": "compile_and_fix",
            "description": "Compile project and automatically fix errors",
            "requires": ["project_path"]
        },
        {
            "name": "full_verification",
            "description": "Compile, fix errors, and run PChecker",
            "requires": ["project_path"]
        },
        {
            "name": "quick_check",
            "description": "Run PChecker on compiled project",
            "requires": ["project_path"]
        }
    ]
    
    active = [
        {
            "workflow_id": state.workflow_id,
            "name": state.workflow_name,
            "status": state.status,
            "current_step": state.current_step_index,
            "completed_steps": state.completed_steps
        }
        for state in engine.get_active_workflows()
    ]
    
    return {
        "available_workflows": available,
        "active_workflows": active
    }


# ============================================================================
# MCP RESOURCES - P Language Guides
# ============================================================================

def _load_resource(path: str) -> str:
    """Load a resource file"""
    services = get_services()
    try:
        return services["resources"].load(f"context_files/{path}")
    except Exception as e:
        return f"Error loading resource: {e}"


@mcp.resource("p://guides/syntax")
def get_syntax_guide() -> str:
    """Complete P language syntax reference"""
    return _load_resource("P_syntax_guide.txt")


@mcp.resource("p://guides/basics")
def get_basics_guide() -> str:
    """P language fundamentals and core concepts"""
    return _load_resource("modular/p_basics.txt")


@mcp.resource("p://guides/machines")
def get_machines_guide() -> str:
    """Guide to P state machines"""
    return _load_resource("modular/p_machines_guide.txt")


@mcp.resource("p://guides/types")
def get_types_guide() -> str:
    """P language type system guide"""
    return _load_resource("modular/p_types_guide.txt")


@mcp.resource("p://guides/events")
def get_events_guide() -> str:
    """P language events guide"""
    return _load_resource("modular/p_events_guide.txt")


@mcp.resource("p://guides/enums")
def get_enums_guide() -> str:
    """P language enums guide"""
    return _load_resource("modular/p_enums_guide.txt")


@mcp.resource("p://guides/statements")
def get_statements_guide() -> str:
    """P language statements guide"""
    return _load_resource("modular/p_statements_guide.txt")


@mcp.resource("p://guides/specs")
def get_specs_guide() -> str:
    """P specification and monitors guide"""
    return _load_resource("modular/p_spec_monitors_guide.txt")


@mcp.resource("p://guides/tests")
def get_tests_guide() -> str:
    """P test cases guide"""
    return _load_resource("modular/p_test_cases_guide.txt")


@mcp.resource("p://guides/modules")
def get_modules_guide() -> str:
    """P module system guide"""
    return _load_resource("modular/p_module_system_guide.txt")


@mcp.resource("p://guides/compiler")
def get_compiler_guide() -> str:
    """P compiler usage and error guide"""
    return _load_resource("modular/p_compiler_guide.txt")


@mcp.resource("p://guides/common_errors")
def get_common_errors_guide() -> str:
    """Common P compilation errors and fixes"""
    return _load_resource("modular/p_common_compilation_errors.txt")


@mcp.resource("p://examples/program")
def get_program_example() -> str:
    """Complete P program example"""
    return _load_resource("modular/p_program_example.txt")


@mcp.resource("p://about")
def get_about_p() -> str:
    """About the P language"""
    return _load_resource("about_p.txt")


# ============================================================================
# MAIN
# ============================================================================

if __name__ == "__main__":
    logger.info("Starting P-ChatBot MCP Server...")
    logger.info(f"Project root: {PROJECT_ROOT}")
    mcp.run()
