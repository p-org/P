"""Generation-related MCP tools."""

from typing import Dict, Any, Optional, List
from pathlib import Path
from pydantic import BaseModel, Field
import logging

logger = logging.getLogger(__name__)


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
) -> Dict[str, Any]:
    """
    Run post-processing and cross-file consistency checks on generated code.

    Returns a dict with:
      - code: the (possibly fixed) code
      - fixes_applied: list of auto-fixes that were applied
      - warnings: list of issues that need manual attention
    """
    from core.compilation.p_post_processor import (
        PCodePostProcessor,
        TypeConsistencyChecker,
        CrossFileReviewer,
    )

    processor = PCodePostProcessor()
    result = processor.process(code, filename, is_test_file=is_test_file)
    warnings = list(result.warnings)

    try:
        project_files: Dict[str, str] = {}
        pp = Path(project_path)
        for p_file in pp.rglob("*.p"):
            rel = str(p_file.relative_to(pp))
            project_files[rel] = p_file.read_text(encoding="utf-8")
        project_files[filename] = result.code

        type_checker = TypeConsistencyChecker()
        for content in project_files.values():
            type_checker.extract_definitions(content)
        undef_types = type_checker.find_undefined_types(result.code)
        undef_events = type_checker.find_undefined_events(result.code)
        if undef_types:
            warnings.append(f"Undefined types in {filename}: {undef_types}")
        if undef_events:
            warnings.append(f"Undefined events in {filename}: {undef_events}")

        reviewer = CrossFileReviewer()

        if is_test_file:
            spec_names = reviewer.extract_spec_names(project_files)
            if spec_names:
                spec_issues = reviewer.validate_test_includes_specs(
                    result.code, spec_names, filename
                )
                warnings.extend(spec_issues)

            machine_configs = reviewer.extract_machine_config_types(project_files)
            if machine_configs:
                ctor_issues = reviewer.validate_constructor_patterns(
                    result.code, machine_configs, filename
                )
                warnings.extend(ctor_issues)

        types_code = ""
        for rel, content in project_files.items():
            if "Enums_Types_Events" in rel or "types" in rel.lower():
                types_code = content
                break
        if types_code and "spec " in result.code:
            spec_warnings = processor.validate_spec_events(
                result.code, types_code, filename
            )
            warnings.extend(spec_warnings)

        # Validate payload field names against type definitions
        if types_code:
            type_fields = reviewer.extract_type_field_names(types_code)
            if type_fields:
                field_issues = reviewer.validate_payload_field_names(
                    result.code, type_fields, filename
                )
                warnings.extend(field_issues)

    except Exception as e:
        logger.debug(f"Cross-file review skipped: {e}")

    return {
        "code": result.code,
        "fixes_applied": result.fixes_applied,
        "warnings": warnings,
    }


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
    design_doc: str = Field(..., description="The design document content")
    project_path: str = Field(..., description="Absolute path to the P project root")


class GenerateMachineParams(BaseModel):
    """Parameters for machine generation"""
    machine_name: str = Field(..., description="Name of the machine to generate")
    design_doc: str = Field(..., description="The design document content")
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
    design_doc: str = Field(..., description="The design document content")
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
    design_doc: str = Field(..., description="The design document content")
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


class GenerateCompleteProjectParams(BaseModel):
    """Parameters for complete project generation"""
    design_doc: str = Field(
        ...,
        description="The design document describing the P program"
    )
    output_dir: str = Field(
        ...,
        description="Absolute path to directory where project will be created"
    )
    project_name: str = Field(
        default="PProject",
        description="Name of the P project"
    )
    machine_names: Optional[List[str]] = Field(
        default=None,
        description="List of machine names (auto-extracted if not provided)"
    )
    include_spec: bool = Field(
        default=True,
        description="Whether to generate safety specification"
    )
    include_test: bool = Field(
        default=True,
        description="Whether to generate test driver"
    )
    auto_fix: bool = Field(
        default=True,
        description="Automatically fix compilation errors"
    )
    run_checker: bool = Field(
        default=False,
        description="Run PChecker after successful compilation"
    )
    ensemble_size: int = Field(
        default=3,
        description="Number of candidate generations per file for ensemble selection. "
                    "Higher values (e.g. 3) produce more reliable code by picking the "
                    "best candidate, at the cost of more LLM calls."
    )


class SavePFileParams(BaseModel):
    """Parameters for saving a P file"""
    file_path: str = Field(..., description="Absolute path where to save the file")
    code: str = Field(..., description="The P code content to save")


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

        services = get_services()
        result = services["generation"].generate_types_events(
            design_doc=params.design_doc,
            project_path=params.project_path,
            save_to_disk=False  # Preview only
        )

        review = {"fixes_applied": [], "warnings": []}
        code = result.code
        if result.success and code:
            review = _review_generated_code(
                code, result.filename or "Enums_Types_Events.p", params.project_path
            )
            code = review["code"]

        payload = {
            "success": result.success,
            "filename": result.filename,
            "file_path": result.file_path,
            "code": code,
            "error": result.error,
            "token_usage": result.token_usage,
            "preview_only": True,
            "review": review,
            "message": "Code generated for preview. Use peasy-ai-save-file to save to disk." if result.success else result.error
        }
        return with_metadata("peasy-ai-gen-types-events", payload, token_usage=result.token_usage)

    @mcp.tool(
        name="peasy-ai-gen-machine",
        description="STEP 3 of the recommended step-by-step workflow. Generates a single P state machine implementation using two-stage generation (structure first, then implementation). Call once per machine in the design. Returns code for preview so the user can review it before saving with peasy-ai-save-file. Pass previously generated files as context_files for cross-file consistency."
    )
    def generate_machine(params: GenerateMachineParams) -> Dict[str, Any]:
        logger.info(f"[TOOL] peasy-ai-gen-machine: {params.machine_name} (preview, ensemble={params.ensemble_size})")

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

        review = {"fixes_applied": [], "warnings": []}
        code = result.code
        if result.success and code:
            review = _review_generated_code(
                code, result.filename or f"{params.machine_name}.p", params.project_path
            )
            code = review["code"]

        payload = {
            "success": result.success,
            "filename": result.filename,
            "file_path": result.file_path,
            "code": code,
            "error": result.error,
            "token_usage": result.token_usage,
            "preview_only": True,
            "review": review,
            "message": "Code generated for preview. Use peasy-ai-save-file to save to disk." if result.success else result.error
        }
        return with_metadata("peasy-ai-gen-machine", payload, token_usage=result.token_usage)

    @mcp.tool(
        name="peasy-ai-gen-spec",
        description="STEP 4 of the recommended step-by-step workflow. Generates a P safety specification/monitor file. Returns code for preview so the user can review it before saving with peasy-ai-save-file. Run this after all machines have been generated, passing them as context_files."
    )
    def generate_spec(params: GenerateSpecParams) -> Dict[str, Any]:
        logger.info(f"[TOOL] peasy-ai-gen-spec: {params.spec_name} (preview, ensemble={params.ensemble_size})")

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

        review = {"fixes_applied": [], "warnings": []}
        code = result.code
        if result.success and code:
            review = _review_generated_code(
                code, result.filename or f"{params.spec_name}.p", params.project_path
            )
            code = review["code"]

        payload = {
            "success": result.success,
            "filename": result.filename,
            "file_path": result.file_path,
            "code": code,
            "error": result.error,
            "token_usage": result.token_usage,
            "preview_only": True,
            "review": review,
            "message": "Code generated for preview. Use peasy-ai-save-file to save to disk." if result.success else result.error
        }
        return with_metadata("peasy-ai-gen-spec", payload, token_usage=result.token_usage)

    @mcp.tool(
        name="peasy-ai-gen-test",
        description="STEP 5 of the recommended step-by-step workflow. Generates a P test driver file. Returns code for preview so the user can review it before saving with peasy-ai-save-file. Run this after all machines and specs have been generated, passing them as context_files. After saving, use peasy-ai-compile to compile and peasy-ai-check to verify."
    )
    def generate_test(params: GenerateTestParams) -> Dict[str, Any]:
        logger.info(f"[TOOL] peasy-ai-gen-test: {params.test_name} (preview, ensemble={params.ensemble_size})")

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

        review = {"fixes_applied": [], "warnings": []}
        code = result.code
        if result.success and code:
            review = _review_generated_code(
                code,
                result.filename or f"{params.test_name}.p",
                params.project_path,
                is_test_file=True,
            )
            code = review["code"]

        payload = {
            "success": result.success,
            "filename": result.filename,
            "file_path": result.file_path,
            "code": code,
            "error": result.error,
            "token_usage": result.token_usage,
            "preview_only": True,
            "review": review,
            "message": "Code generated for preview. Use peasy-ai-save-file to save to disk." if result.success else result.error
        }
        return with_metadata("peasy-ai-gen-test", payload, token_usage=result.token_usage)

    @mcp.tool(
        name="peasy-ai-gen-full-project",
        description="""ADVANCED: Generate an entire P project in a single autonomous call. This is a convenience shortcut that runs all generation steps without human review.

IMPORTANT: Prefer the step-by-step tools (peasy-ai-create-project, peasy-ai-gen-types-events, peasy-ai-gen-machine, peasy-ai-gen-spec, peasy-ai-gen-test) instead. The step-by-step approach lets the user review and approve each file before proceeding, resulting in higher quality code.

Only use this tool when the user EXPLICITLY asks for fully automated / one-shot / hands-off generation.

Steps performed: create structure → generate types/events → generate machines → generate spec → generate test → post-process → compile → auto-fix → optionally run PChecker."""
    )
    def generate_complete_project(params: GenerateCompleteProjectParams) -> Dict[str, Any]:
        logger.info(f"[TOOL] peasy-ai-gen-full-project: {params.project_name} (ensemble={params.ensemble_size})")

        import os
        from pathlib import Path
        from core.compilation.p_post_processor import (
            PCodePostProcessor,
            TypeConsistencyChecker,
            MachineConfigDetector
        )
        from core.workflow.factory import extract_machine_names_from_design_doc, validate_design_doc

        services = get_services()
        use_ensemble = params.ensemble_size > 1
        results = {
            "success": False,
            "project_path": None,
            "generated_files": {},
            "post_processing": [],
            "compilation": None,
            "checker": None,
            "errors": [],
            "warnings": [],
            "ensemble_size": params.ensemble_size,
        }

        try:
            # Validate design doc first
            doc_validation = validate_design_doc(params.design_doc)
            if not doc_validation["valid"]:
                results["errors"].extend(doc_validation["errors"])
                results["warnings"].extend(doc_validation["warnings"])
                return with_metadata("peasy-ai-gen-full-project", results)
            if doc_validation["warnings"]:
                results["warnings"].extend(doc_validation["warnings"])

            machine_names = params.machine_names
            if not machine_names:
                machine_names = doc_validation["components"] or extract_machine_names_from_design_doc(params.design_doc)
                if not machine_names:
                    results["errors"].append("Could not extract machine names from design doc")
                    return with_metadata("peasy-ai-gen-full-project", results)

            logger.info(f"Generating project with machines: {machine_names}")

            struct_result = services["generation"].create_project_structure(
                output_dir=params.output_dir,
                project_name=params.project_name
            )

            if not struct_result.success:
                results["errors"].append(f"Failed to create structure: {struct_result.error}")
                return with_metadata("peasy-ai-gen-full-project", results)

            project_path = struct_result.file_path
            results["project_path"] = project_path

            types_result = services["generation"].generate_types_events(
                design_doc=params.design_doc,
                project_path=project_path,
                save_to_disk=True
            )

            # Use actual filename from generation result (LLM picks the name)
            types_filename = types_result.filename or "Enums_Types_Events.p"
            if types_result.success:
                results["generated_files"][types_filename] = types_result.file_path
            else:
                results["errors"].append(f"Failed to generate types: {types_result.error}")
                return with_metadata("peasy-ai-gen-full-project", results)

            context_files = {types_filename: types_result.code}

            config_detector = MachineConfigDetector()
            machine_dependencies = {}

            # ── Machine generation: ensemble or parallel ───────────────
            if use_ensemble:
                # Ensemble: sequential machines, N candidates each, best-of-N selection.
                # Each machine sees code from previously generated machines.
                machine_results = services["generation"].generate_machines_ensemble(
                    machine_names=machine_names,
                    design_doc=params.design_doc,
                    project_path=project_path,
                    context_files=context_files,
                    ensemble_size=params.ensemble_size,
                    save_to_disk=True,
                )
            else:
                # Non-ensemble: parallel generation for speed
                machine_results = services["generation"].generate_machines_parallel(
                    machine_names=machine_names,
                    design_doc=params.design_doc,
                    project_path=project_path,
                    context_files=context_files,
                    save_to_disk=True,
                )

            for machine_name in machine_names:
                machine_result = machine_results.get(machine_name)
                if machine_result and machine_result.success:
                    mach_filename = machine_result.filename or f"{machine_name}.p"
                    results["generated_files"][mach_filename] = machine_result.file_path
                    context_files[mach_filename] = machine_result.code

                    deps = config_detector.detect_dependencies(machine_result.code)
                    if deps:
                        machine_dependencies[machine_name] = deps
                        results["warnings"].append(
                            f"{machine_name} has dependencies: {deps}. Ensure configuration events are sent."
                        )
                else:
                    err = machine_result.error if machine_result else "Generation returned no result"
                    results["errors"].append(f"Failed to generate {machine_name}: {err}")

            # ── Spec generation: ensemble or single ────────────────────
            spec_filename = None
            if params.include_spec:
                if use_ensemble:
                    spec_result = services["generation"].generate_spec_ensemble(
                        spec_name="Safety",
                        design_doc=params.design_doc,
                        project_path=project_path,
                        context_files=context_files,
                        ensemble_size=params.ensemble_size,
                        save_to_disk=True,
                    )
                else:
                    spec_result = services["generation"].generate_spec(
                        spec_name="Safety",
                        design_doc=params.design_doc,
                        project_path=project_path,
                        context_files=context_files,
                        save_to_disk=True
                    )
                if spec_result.success:
                    spec_filename = spec_result.filename or "Safety.p"
                    results["generated_files"][spec_filename] = spec_result.file_path
                    context_files[spec_filename] = spec_result.code
                else:
                    results["warnings"].append(f"Spec generation failed: {spec_result.error}")

            # ── Test generation: ensemble or single ────────────────────
            if params.include_test:
                if use_ensemble:
                    test_result = services["generation"].generate_test_ensemble(
                        test_name="TestDriver",
                        design_doc=params.design_doc,
                        project_path=project_path,
                        context_files=context_files,
                        ensemble_size=params.ensemble_size,
                        save_to_disk=True,
                    )
                else:
                    test_result = services["generation"].generate_test(
                        test_name="TestDriver",
                        design_doc=params.design_doc,
                        project_path=project_path,
                        context_files=context_files,
                        save_to_disk=True
                    )
                if test_result.success:
                    test_filename = test_result.filename or "TestDriver.p"
                    results["generated_files"][test_filename] = test_result.file_path
                else:
                    results["warnings"].append(f"Test generation failed: {test_result.error}")

            # ── Post-processing ────────────────────────────────────────
            processor = PCodePostProcessor()
            for filename, file_path in results["generated_files"].items():
                if filename.endswith(".p"):
                    try:
                        code = Path(file_path).read_text()
                        is_test = "PTst" in file_path
                        processed = processor.process(code, filename, is_test_file=is_test)
                        if processed.fixes_applied:
                            Path(file_path).write_text(processed.code)
                            results["post_processing"].append({
                                "file": filename,
                                "fixes": processed.fixes_applied
                            })
                    except Exception as e:
                        results["warnings"].append(f"Post-processing failed for {filename}: {e}")

            # Validate spec events exist in types file
            try:
                if spec_filename and spec_filename in results["generated_files"] and types_result.code:
                    spec_path = results["generated_files"][spec_filename]
                    spec_code = Path(spec_path).read_text(encoding="utf-8")
                    spec_warnings = processor.validate_spec_events(spec_code, types_result.code, spec_filename)
                    for w in spec_warnings:
                        results["warnings"].append(w)
                        logger.warning(w)
            except Exception as e:
                logger.debug(f"Spec event validation skipped: {e}")

            # Run type consistency check across all generated files
            try:
                type_checker = TypeConsistencyChecker()
                project_files = services["compilation"].get_project_files(project_path)
                for _, content in project_files.items():
                    type_checker.extract_definitions(content)
                all_issues = []
                for rel_path, content in project_files.items():
                    undef_types = type_checker.find_undefined_types(content)
                    undef_events = type_checker.find_undefined_events(content)
                    if undef_types:
                        all_issues.append(f"{rel_path}: undefined types {undef_types}")
                    if undef_events:
                        all_issues.append(f"{rel_path}: undefined events {undef_events}")
                if all_issues:
                    results["warnings"].append(f"Type consistency issues: {'; '.join(all_issues)}")
            except Exception as e:
                logger.debug(f"Type consistency check skipped: {e}")

            # ── Compilation ────────────────────────────────────────────
            compile_result = services["compilation"].compile(project_path)
            results["compilation"] = {
                "success": compile_result.success,
                "output": compile_result.stdout[:500] if compile_result.stdout else "",
                "error": compile_result.error
            }

            if not compile_result.success and params.auto_fix:
                fix_results = services["fixer"].fix_iteratively(
                    project_path=project_path,
                    max_iterations=5
                )
                results["compilation"]["fix_results"] = fix_results

                # Re-check compilation status after iterative fix
                recompile = services["compilation"].compile(project_path)
                results["compilation"]["success"] = recompile.success
                if recompile.success:
                    results["compilation"]["output"] = recompile.stdout[:500] if recompile.stdout else ""
                    results["compilation"]["error"] = None

            # ── PChecker + auto-fix loop ───────────────────────────────
            if params.run_checker and results["compilation"]["success"]:
                MAX_CHECKER_FIX_ROUNDS = 2
                checker_result = services["compilation"].run_checker(
                    project_path=project_path,
                    schedules=100,
                    timeout=60
                )
                results["checker"] = {
                    "success": checker_result.success,
                    "test_results": checker_result.test_results,
                    "passed_tests": checker_result.passed_tests,
                    "failed_tests": checker_result.failed_tests,
                }

                checker_fix_round = 0
                while (
                    not checker_result.success
                    and checker_result.failed_tests
                    and checker_fix_round < MAX_CHECKER_FIX_ROUNDS
                ):
                    checker_fix_round += 1
                    logger.info(f"[CHECKER-FIX] Round {checker_fix_round}: attempting auto-fix")

                    # Try to fix using trace logs from the failed test
                    fixed_any = False
                    for failed_test in checker_result.failed_tests:
                        trace = checker_result.trace_logs.get(failed_test, "")
                        if trace:
                            fix_result = services["fixer"].fix_checker_error(
                                project_path=project_path,
                                trace_log=trace,
                                error_category=None,
                            )
                            if fix_result.fixed:
                                fixed_any = True
                                break

                    if not fixed_any:
                        logger.info("[CHECKER-FIX] No fix applied, stopping")
                        break

                    # Recompile after fix
                    recompile = services["compilation"].compile(project_path)
                    if not recompile.success:
                        results["warnings"].append(
                            f"Recompilation failed after checker fix round {checker_fix_round}"
                        )
                        break

                    # Re-run PChecker
                    checker_result = services["compilation"].run_checker(
                        project_path=project_path,
                        schedules=100,
                        timeout=60
                    )
                    results["checker"] = {
                        "success": checker_result.success,
                        "test_results": checker_result.test_results,
                        "passed_tests": checker_result.passed_tests,
                        "failed_tests": checker_result.failed_tests,
                    }

            results["success"] = results["compilation"]["success"]
            if params.run_checker and results.get("checker"):
                results["success"] = results["success"] and results["checker"]["success"]
            results["message"] = "Project generated successfully" if results["success"] else "Project generated with errors"

        except Exception as e:
            import traceback
            err_msg = f"{type(e).__name__}: {e}"
            logger.error(f"Error in generate_complete_project: {err_msg}\n{traceback.format_exc()}")
            results["errors"].append(err_msg)

        return with_metadata("peasy-ai-gen-full-project", results)

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
        "generate_complete_project": generate_complete_project,
        "save_p_file": save_p_file,
    }
