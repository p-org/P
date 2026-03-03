"""Generation-related MCP tools."""

from typing import Dict, Any, List, Optional
from pathlib import Path
from pydantic import BaseModel, Field
import logging

from core.security import (
    validate_project_path,
    validate_file_write_path,
    PathSecurityError,
    sanitize_error,
    check_input_size,
    MAX_DESIGN_DOC_BYTES,
    MAX_CODE_BYTES,
)

logger = logging.getLogger(__name__)


def _build_generation_payload(
    tool_name: str,
    result,
    review: Dict[str, Any],
    code: Optional[str],
    with_metadata,
    *,
    extra: Optional[Dict[str, Any]] = None,
) -> Dict[str, Any]:
    """Build a standardized MCP response payload for generation tools.

    Surfaces validation severity (is_valid, error_count, warning_count) at
    the top level so the calling agent can decide whether to save the file
    or request regeneration.
    """
    errors: List[str] = review.get("errors", [])
    warnings: List[str] = review.get("warnings", [])
    is_valid: bool = review.get("is_valid", True)

    if not result.success:
        message = result.error or f"{tool_name} failed"
    elif errors:
        message = (
            f"Code generated with {len(errors)} validation error(s) "
            f"that will likely cause compilation failure. "
            f"Review the 'review.errors' field and consider regenerating."
        )
    elif warnings:
        message = (
            "Code generated for preview with warnings. "
            "Use peasy-ai-save-file to save to disk."
        )
    else:
        message = (
            "Code generated for preview. "
            "Use peasy-ai-save-file to save to disk."
        )

    payload: Dict[str, Any] = {
        "success": result.success,
        "filename": result.filename,
        "file_path": result.file_path,
        "code": code,
        "error": result.error,
        "token_usage": result.token_usage,
        "preview_only": True,
        "is_valid": is_valid,
        "error_count": len(errors),
        "warning_count": len(warnings),
        "review": review,
        "message": message,
    }
    if extra:
        payload.update(extra)
    return with_metadata(tool_name, payload, token_usage=result.token_usage)


def _find_project_root(file_path: str) -> Optional[str]:
    """
    Walk up from file_path to find the P project root (directory containing .pproj).
    Returns the project root path or None.
    """
    current = Path(file_path).parent
    for _ in range(10):
        if any(current.glob("*.pproj")):
            return str(current)
        parent = current.parent
        if parent == current:
            break
        current = parent
    return None


def _review_generated_code(
    code: str,
    filename: str,
    project_path: str,
    is_test_file: bool = False,
    context_files: Optional[Dict[str, str]] = None,
) -> Dict[str, Any]:
    """
    Run the unified validation pipeline on generated code.

    Stage 1 — deterministic auto-fixes (PCodePostProcessor).
    Stage 2 — structured validators (syntax, types, events, specs, duplicates, …).

    Args:
        context_files: Previously generated files (filename -> code) that
            haven't been saved to disk yet.  Merged with any on-disk project
            files so cross-file validators (e.g. NamedTupleConstructionValidator)
            can resolve type definitions from earlier generation steps.

    Returns a dict with:
      - code: the (possibly fixed) code
      - fixes_applied: list of auto-fixes that were applied
      - warnings: list of issues that need manual attention
      - errors: list of issues that will likely cause compilation failure
      - is_valid: whether the code passed all error-level checks
      - validators_run: names of validators that ran
    """
    from core.validation.pipeline import ValidationPipeline

    pipeline = ValidationPipeline(include_test_validators=is_test_file)

    merged_context: Optional[Dict[str, str]] = None
    if context_files:
        merged_context = dict(context_files)

    result = pipeline.validate(
        code,
        context=merged_context,
        filename=filename,
        project_path=project_path,
        is_test_file=is_test_file,
    )
    return result.to_review_dict()


def _validate_generation_inputs(
    tool_name: str,
    with_metadata,
    project_path: Optional[str] = None,
    design_doc: Optional[str] = None,
) -> Optional[Dict[str, Any]]:
    """Validate common generation tool inputs. Returns an error payload or None."""
    try:
        if project_path:
            validate_project_path(project_path)
        if design_doc:
            check_input_size(design_doc, "design_doc", MAX_DESIGN_DOC_BYTES)
    except (PathSecurityError, ValueError) as e:
        return with_metadata(tool_name, {"success": False, "error": str(e)})
    return None


class GenerateProjectParams(BaseModel):
    """Parameters for project structure creation (STEP 1)"""
    design_doc: str = Field(
        ...,
        description="The design document content describing the P program in markdown format. "
                    "Should include headings: # Title, ## Introduction, ## Components, ## Interactions."
    )
    output_dir: str = Field(
        ...,
        description="Absolute path to the directory where the project should be created"
    )
    project_name: str = Field(
        default="PProject",
        description="Name for the P project"
    )


class GenerateTypesEventsParams(BaseModel):
    """Parameters for types/events generation"""
    design_doc: str = Field(..., description="The design document content", max_length=500_000)
    project_path: str = Field(..., description="Absolute path to the P project root")


class GenerateMachineParams(BaseModel):
    """Parameters for machine generation"""
    machine_name: str = Field(..., description="Name of the machine to generate")
    design_doc: str = Field(..., description="The design document content", max_length=500_000)
    project_path: str = Field(..., description="Absolute path to the P project root")
    context_files: Optional[Dict[str, str]] = Field(
        default=None,
        description="Additional context files (filename -> content)"
    )
    ensemble_size: int = Field(
        default=3,
        description="Number of candidate generations for ensemble selection (best-of-N). "
                    "Set to 1 to disable ensemble."
    )


class GenerateSpecParams(BaseModel):
    """Parameters for specification generation"""
    spec_name: str = Field(..., description="Name of the specification file to generate")
    design_doc: str = Field(..., description="The design document content", max_length=500_000)
    project_path: str = Field(..., description="Absolute path to the P project root")
    context_files: Optional[Dict[str, str]] = Field(
        default=None,
        description="Additional context files"
    )
    ensemble_size: int = Field(
        default=3,
        description="Number of candidate generations for ensemble selection (best-of-N). "
                    "Set to 1 to disable ensemble."
    )
    checker_feedback: Optional[str] = Field(
        default=None,
        description="PChecker bug report from a previous failing run. "
                    "If provided, injected as context so the LLM avoids the same bug."
    )


class GenerateTestParams(BaseModel):
    """Parameters for test generation"""
    test_name: str = Field(..., description="Name of the test file to generate")
    design_doc: str = Field(..., description="The design document content", max_length=500_000)
    project_path: str = Field(..., description="Absolute path to the P project root")
    context_files: Optional[Dict[str, str]] = Field(
        default=None,
        description="Additional context files"
    )
    ensemble_size: int = Field(
        default=3,
        description="Number of candidate generations for ensemble selection (best-of-N). "
                    "Set to 1 to disable ensemble."
    )
    checker_feedback: Optional[str] = Field(
        default=None,
        description="PChecker bug report from a previous failing run. "
                    "If provided, injected as context so the LLM avoids the same bug."
    )


class SavePFileParams(BaseModel):
    """Parameters for saving a P file"""
    file_path: str = Field(..., description="Absolute path where to save the file")
    code: str = Field(..., description="The P code content to save", max_length=200_000)


def register_generation_tools(mcp, get_services, with_metadata):
    """Register generation tools."""

    @mcp.tool(
        name="peasy-ai-create-project",
        description="STEP 1 of the recommended step-by-step workflow. Creates a P project skeleton with PSrc, PSpec, PTst folders and .pproj file. After this, use peasy-ai-gen-types-events to define types and events."
    )
    def generate_project_structure(params: GenerateProjectParams) -> Dict[str, Any]:
        logger.info(f"[TOOL] peasy-ai-create-project: {params.project_name}")

        services = get_services()
        result = services["generation"].create_project_structure(
            output_dir=params.output_dir,
            project_name=params.project_name
        )

        payload = {
            "success": result.success,
            "project_path": result.file_path,
            "project_name": result.filename,
            "error": result.error,
            "message": f"Created P project at {result.file_path}" if result.success else result.error
        }
        return with_metadata("peasy-ai-create-project", payload, token_usage=result.token_usage)

    @mcp.tool(
        name="peasy-ai-gen-types-events",
        description="STEP 2 of the recommended step-by-step workflow. Generates the types, enums, and events file (Enums_Types_Events.p) from the design document. Returns code for preview so the user can review it before saving with peasy-ai-save-file. Run this after peasy-ai-create-project."
    )
    def generate_types_events(params: GenerateTypesEventsParams) -> Dict[str, Any]:
        logger.info("[TOOL] peasy-ai-gen-types-events (preview)")

        err = _validate_generation_inputs(
            "peasy-ai-gen-types-events", with_metadata,
            project_path=params.project_path, design_doc=params.design_doc,
        )
        if err:
            return err

        services = get_services()
        result = services["generation"].generate_types_events(
            design_doc=params.design_doc,
            project_path=params.project_path,
            save_to_disk=False  # Preview only
        )

        review: Dict[str, Any] = {"fixes_applied": [], "warnings": [], "errors": [], "is_valid": True}
        code = result.code
        if result.success and code:
            review = _review_generated_code(
                code, result.filename or "Enums_Types_Events.p", params.project_path,
            )
            code = review["code"]

            try:
                documented = services["generation"].review_code_documentation(
                    code=code,
                    design_doc=params.design_doc,
                    context_files=params.context_files,
                )
                if documented:
                    code = documented
                    review["fixes_applied"].append(
                        "[DocReview] LLM added documentation comments"
                    )
            except Exception as e:
                logger.warning(f"Documentation review skipped: {e}")

        return _build_generation_payload(
            "peasy-ai-gen-types-events", result, review, code, with_metadata
        )

    @mcp.tool(
        name="peasy-ai-gen-machine",
        description="STEP 3 of the recommended step-by-step workflow. Generates a single P state machine implementation using two-stage generation (structure first, then implementation). Call once per machine in the design. Returns code for preview so the user can review it before saving with peasy-ai-save-file. Pass previously generated files as context_files for cross-file consistency."
    )
    def generate_machine(params: GenerateMachineParams) -> Dict[str, Any]:
        logger.info(f"[TOOL] peasy-ai-gen-machine: {params.machine_name} (preview, ensemble={params.ensemble_size})")

        err = _validate_generation_inputs(
            "peasy-ai-gen-machine", with_metadata,
            project_path=params.project_path, design_doc=params.design_doc,
        )
        if err:
            return err

        services = get_services()
        if params.ensemble_size > 1:
            result = services["generation"].generate_machine_ensemble(
                machine_name=params.machine_name,
                design_doc=params.design_doc,
                project_path=params.project_path,
                context_files=params.context_files,
                ensemble_size=params.ensemble_size,
                save_to_disk=False  # Preview only
            )
        else:
            result = services["generation"].generate_machine(
                machine_name=params.machine_name,
                design_doc=params.design_doc,
                project_path=params.project_path,
                context_files=params.context_files,
                two_stage=True,
                save_to_disk=False  # Preview only
            )

        review: Dict[str, Any] = {"fixes_applied": [], "warnings": [], "errors": [], "is_valid": True}
        code = result.code
        if result.success and code:
            review = _review_generated_code(
                code, result.filename or f"{params.machine_name}.p", params.project_path,
                context_files=params.context_files,
            )
            code = review["code"]

            try:
                documented = services["generation"].review_code_documentation(
                    code=code,
                    design_doc=params.design_doc,
                    context_files=params.context_files,
                )
                if documented:
                    code = documented
                    review["fixes_applied"].append(
                        "[DocReview] LLM added documentation comments"
                    )
            except Exception as e:
                logger.warning(f"Documentation review skipped: {e}")

        return _build_generation_payload(
            "peasy-ai-gen-machine", result, review, code, with_metadata
        )

    @mcp.tool(
        name="peasy-ai-gen-spec",
        description="STEP 4 of the recommended step-by-step workflow. Generates a P safety specification/monitor file. Returns code for preview so the user can review it before saving with peasy-ai-save-file. Run this after all machines have been generated, passing them as context_files."
    )
    def generate_spec(params: GenerateSpecParams) -> Dict[str, Any]:
        logger.info(f"[TOOL] peasy-ai-gen-spec: {params.spec_name} (preview, ensemble={params.ensemble_size})")

        err = _validate_generation_inputs(
            "peasy-ai-gen-spec", with_metadata,
            project_path=params.project_path, design_doc=params.design_doc,
        )
        if err:
            return err

        ctx = dict(params.context_files) if params.context_files else {}
        if params.checker_feedback:
            ctx["__checker_bug_report__"] = params.checker_feedback

        services = get_services()
        if params.ensemble_size > 1:
            result = services["generation"].generate_spec_ensemble(
                spec_name=params.spec_name,
                design_doc=params.design_doc,
                project_path=params.project_path,
                context_files=ctx or None,
                ensemble_size=params.ensemble_size,
                save_to_disk=False
            )
        else:
            result = services["generation"].generate_spec(
                spec_name=params.spec_name,
                design_doc=params.design_doc,
                project_path=params.project_path,
                context_files=ctx or None,
                save_to_disk=False
            )

        review: Dict[str, Any] = {"fixes_applied": [], "warnings": [], "errors": [], "is_valid": True}
        code = result.code
        spec_fixes: Dict[str, str] = {}
        if result.success and code:
            # Stage A: regex/structural validation
            review = _review_generated_code(
                code, result.filename or f"{params.spec_name}.p", params.project_path,
                context_files=params.context_files,
            )
            code = review["code"]

            # Stage B: LLM-based spec correctness review (observes
            # completeness, assertion logic, payload usage).
            try:
                spec_fixes = services["generation"].review_spec_correctness(
                    spec_code=code,
                    design_doc=params.design_doc,
                    context_files=params.context_files,
                )
                spec_filename = result.filename or f"{params.spec_name}.p"
                # Try to find the fixed spec under its actual filename
                for candidate in [spec_filename, "Specification.p", "Spec.p", "Safety.p"]:
                    if candidate in spec_fixes:
                        code = spec_fixes.pop(candidate)
                        review["fixes_applied"].append(
                            "[SpecReview] LLM corrected spec monitor logic"
                        )
                        break
            except Exception as e:
                logger.warning(f"Spec review skipped: {e}")

            # Stage C: LLM-based documentation comments
            try:
                documented = services["generation"].review_code_documentation(
                    code=code,
                    design_doc=params.design_doc,
                    context_files=params.context_files,
                )
                if documented:
                    code = documented
                    review["fixes_applied"].append(
                        "[DocReview] LLM added documentation comments"
                    )
            except Exception as e:
                logger.warning(f"Documentation review skipped: {e}")

        extra: Dict[str, Any] = {}
        if spec_fixes:
            extra["spec_fixes"] = spec_fixes

        return _build_generation_payload(
            "peasy-ai-gen-spec", result, review, code, with_metadata,
            extra=extra,
        )

    @mcp.tool(
        name="peasy-ai-gen-test",
        description="STEP 5 of the recommended step-by-step workflow. Generates a P test driver file. Returns code for preview so the user can review it before saving with peasy-ai-save-file. Run this after all machines and specs have been generated, passing them as context_files. After saving, use peasy-ai-compile to compile and peasy-ai-check to verify."
    )
    def generate_test(params: GenerateTestParams) -> Dict[str, Any]:
        logger.info(f"[TOOL] peasy-ai-gen-test: {params.test_name} (preview, ensemble={params.ensemble_size})")

        err = _validate_generation_inputs(
            "peasy-ai-gen-test", with_metadata,
            project_path=params.project_path, design_doc=params.design_doc,
        )
        if err:
            return err

        ctx = dict(params.context_files) if params.context_files else {}
        if params.checker_feedback:
            ctx["__checker_bug_report__"] = params.checker_feedback

        services = get_services()
        if params.ensemble_size > 1:
            result = services["generation"].generate_test_ensemble(
                test_name=params.test_name,
                design_doc=params.design_doc,
                project_path=params.project_path,
                context_files=ctx or None,
                ensemble_size=params.ensemble_size,
                save_to_disk=False
            )
        else:
            result = services["generation"].generate_test(
                test_name=params.test_name,
                design_doc=params.design_doc,
                project_path=params.project_path,
                context_files=ctx or None,
                save_to_disk=False
            )

        review: Dict[str, Any] = {"fixes_applied": [], "warnings": [], "errors": [], "is_valid": True}
        code = result.code
        wiring_fixes: Dict[str, str] = {}
        if result.success and code:
            # Stage A: regex/structural validation
            review = _review_generated_code(
                code,
                result.filename or f"{params.test_name}.p",
                params.project_path,
                is_test_file=True,
                context_files=params.context_files,
            )
            code = review["code"]

            # Stage B: LLM-based wiring review (initialization order,
            # circular dependencies, empty collections).
            try:
                wiring_fixes = services["generation"].review_test_wiring(
                    test_code=code,
                    design_doc=params.design_doc,
                    context_files=params.context_files,
                )
                test_filename = result.filename or f"{params.test_name}.p"
                if test_filename in wiring_fixes:
                    code = wiring_fixes.pop(test_filename)
                    review["fixes_applied"].append(
                        "[WiringReview] LLM rewired machine initialization"
                    )
                elif "TestDriver.p" in wiring_fixes:
                    code = wiring_fixes.pop("TestDriver.p")
                    review["fixes_applied"].append(
                        "[WiringReview] LLM rewired machine initialization"
                    )
            except Exception as e:
                logger.warning(f"Wiring review skipped: {e}")

            # Stage C: LLM-based documentation comments
            try:
                documented = services["generation"].review_code_documentation(
                    code=code,
                    design_doc=params.design_doc,
                    context_files=params.context_files,
                )
                if documented:
                    code = documented
                    review["fixes_applied"].append(
                        "[DocReview] LLM added documentation comments"
                    )
            except Exception as e:
                logger.warning(f"Documentation review skipped: {e}")

        extra: Dict[str, Any] = {}
        if wiring_fixes:
            extra["wiring_fixes"] = wiring_fixes

        return _build_generation_payload(
            "peasy-ai-gen-test", result, review, code, with_metadata,
            extra=extra,
        )

    @mcp.tool(
        name="peasy-ai-save-file",
        description="Save generated P code to a file on disk and run a proactive compilation check. "
                    "In the step-by-step workflow, call this after the user reviews and approves the code "
                    "returned by peasy-ai-gen-types-events, peasy-ai-gen-machine, peasy-ai-gen-spec, or peasy-ai-gen-test. "
                    "Provide the absolute file_path (from the generate tool's response) and the code content. "
                    "The response includes a 'compilation_check' field with any syntax errors found in this file."
    )
    def save_p_file(params: SavePFileParams) -> Dict[str, Any]:
        logger.info(f"[TOOL] peasy-ai-save-file: {params.file_path}")

        project_root = _find_project_root(params.file_path)
        if project_root:
            try:
                validate_file_write_path(params.file_path, project_root)
            except PathSecurityError as e:
                return with_metadata("peasy-ai-save-file", {
                    "success": False, "error": str(e),
                })
        else:
            resolved = Path(params.file_path).resolve()
            if resolved.suffix not in {".p", ".pproj"}:
                return with_metadata("peasy-ai-save-file", {
                    "success": False,
                    "error": f"Only .p files can be saved; got '{resolved.suffix}'",
                })

        try:
            check_input_size(params.code, "code", MAX_CODE_BYTES)
        except ValueError as e:
            return with_metadata("peasy-ai-save-file", {
                "success": False, "error": str(e),
            })

        services = get_services()
        result = services["generation"].save_p_file(
            file_path=params.file_path,
            code=params.code
        )

        compilation_check = None
        if result.success:
            try:
                project_path = _find_project_root(params.file_path)
                if project_path:
                    compile_result = services["compilation"].compile(project_path)
                    if compile_result.success:
                        compilation_check = {"success": True, "errors": []}
                    else:
                        raw_output = compile_result.stdout or compile_result.stderr or ""
                        # Extract errors relevant to this file
                        file_basename = Path(params.file_path).name
                        file_errors = []
                        all_errors = []
                        for line in raw_output.splitlines():
                            if "error" in line.lower() or "parse error" in line.lower():
                                all_errors.append(line.strip())
                                if file_basename in line:
                                    file_errors.append(line.strip())
                        compilation_check = {
                            "success": False,
                            "file_errors": file_errors,
                            "all_errors": all_errors[:10],
                        }
            except Exception as e:
                logger.debug(f"Proactive compilation check skipped: {e}")

        payload = {
            "success": result.success,
            "filename": result.filename,
            "file_path": result.file_path,
            "error": result.error,
            "compilation_check": compilation_check,
            "message": f"Saved {result.filename} to disk" if result.success else result.error
        }
        return with_metadata("peasy-ai-save-file", payload, token_usage=result.token_usage)

    return {
        "generate_project_structure": generate_project_structure,
        "generate_types_events": generate_types_events,
        "generate_machine": generate_machine,
        "generate_spec": generate_spec,
        "generate_test": generate_test,
        "save_p_file": save_p_file,
    }
