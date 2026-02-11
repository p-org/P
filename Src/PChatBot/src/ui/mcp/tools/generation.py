"""Generation-related MCP tools."""

from typing import Dict, Any, Optional, List
from pydantic import BaseModel, Field
import logging

logger = logging.getLogger(__name__)


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


class GenerateSpecParams(BaseModel):
    """Parameters for specification generation"""
    spec_name: str = Field(..., description="Name of the specification file to generate")
    design_doc: str = Field(..., description="The design document content")
    project_path: str = Field(..., description="Absolute path to the P project root")
    context_files: Optional[Dict[str, str]] = Field(
        default=None,
        description="Additional context files"
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


class SavePFileParams(BaseModel):
    """Parameters for saving a P file"""
    file_path: str = Field(..., description="Absolute path where to save the file")
    code: str = Field(..., description="The P code content to save")


def register_generation_tools(mcp, get_services, with_metadata):
    """Register generation tools."""

    @mcp.tool(
        name="generate_project_structure",
        description="Create a P project skeleton with PSrc, PSpec, PTst folders and .pproj file"
    )
    def generate_project_structure(params: GenerateProjectParams) -> Dict[str, Any]:
        logger.info(f"[TOOL] generate_project_structure: {params.project_name}")

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
        return with_metadata("generate_project_structure", payload, token_usage=result.token_usage)

    @mcp.tool(
        name="generate_types_events",
        description="Generate types, enums, and events file (Enums_Types_Events.p) from a design document. Returns code for preview - use save_p_file to save."
    )
    def generate_types_events(params: GenerateTypesEventsParams) -> Dict[str, Any]:
        logger.info("[TOOL] generate_types_events (preview)")

        services = get_services()
        result = services["generation"].generate_types_events(
            design_doc=params.design_doc,
            project_path=params.project_path,
            save_to_disk=False  # Preview only
        )

        payload = {
            "success": result.success,
            "filename": result.filename,
            "file_path": result.file_path,
            "code": result.code,
            "error": result.error,
            "token_usage": result.token_usage,
            "preview_only": True,
            "message": "Code generated for preview. Use save_p_file to save to disk." if result.success else result.error
        }
        return with_metadata("generate_types_events", payload, token_usage=result.token_usage)

    @mcp.tool(
        name="generate_machine",
        description="Generate a single P state machine implementation using two-stage generation (structure first, then implementation). Returns code for preview - use save_p_file to save."
    )
    def generate_machine(params: GenerateMachineParams) -> Dict[str, Any]:
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

        payload = {
            "success": result.success,
            "filename": result.filename,
            "file_path": result.file_path,
            "code": result.code,
            "error": result.error,
            "token_usage": result.token_usage,
            "preview_only": True,
            "message": "Code generated for preview. Use save_p_file to save to disk." if result.success else result.error
        }
        return with_metadata("generate_machine", payload, token_usage=result.token_usage)

    @mcp.tool(
        name="generate_spec",
        description="Generate a P specification/monitor file. Returns code for preview - use save_p_file to save."
    )
    def generate_spec(params: GenerateSpecParams) -> Dict[str, Any]:
        logger.info(f"[TOOL] generate_spec: {params.spec_name} (preview)")

        services = get_services()
        result = services["generation"].generate_spec(
            spec_name=params.spec_name,
            design_doc=params.design_doc,
            project_path=params.project_path,
            context_files=params.context_files,
            save_to_disk=False  # Preview only
        )

        payload = {
            "success": result.success,
            "filename": result.filename,
            "file_path": result.file_path,
            "code": result.code,
            "error": result.error,
            "token_usage": result.token_usage,
            "preview_only": True,
            "message": "Code generated for preview. Use save_p_file to save to disk." if result.success else result.error
        }
        return with_metadata("generate_spec", payload, token_usage=result.token_usage)

    @mcp.tool(
        name="generate_test",
        description="Generate a P test file. Returns code for preview - use save_p_file to save."
    )
    def generate_test(params: GenerateTestParams) -> Dict[str, Any]:
        logger.info(f"[TOOL] generate_test: {params.test_name} (preview)")

        services = get_services()
        result = services["generation"].generate_test(
            test_name=params.test_name,
            design_doc=params.design_doc,
            project_path=params.project_path,
            context_files=params.context_files,
            save_to_disk=False  # Preview only
        )

        payload = {
            "success": result.success,
            "filename": result.filename,
            "file_path": result.file_path,
            "code": result.code,
            "error": result.error,
            "token_usage": result.token_usage,
            "preview_only": True,
            "message": "Code generated for preview. Use save_p_file to save to disk." if result.success else result.error
        }
        return with_metadata("generate_test", payload, token_usage=result.token_usage)

    @mcp.tool(
        name="generate_complete_project",
        description="""Generate a complete P project in one call with automatic post-processing.

This tool performs the following steps:
1. Creates project structure (folders and .pproj file)
2. Generates types/events file with all needed definitions
3. Generates all machine implementations with proper context
4. Optionally generates safety specification
5. Generates test driver with correct syntax
6. Applies post-processing to fix common issues (var order, tuple syntax)
7. Compiles the project
8. Optionally runs iterative fix if compilation fails
9. Optionally runs PChecker

Returns comprehensive results including all generated files and any issues found."""
    )
    def generate_complete_project(params: GenerateCompleteProjectParams) -> Dict[str, Any]:
        logger.info(f"[TOOL] generate_complete_project: {params.project_name}")

        import os
        from pathlib import Path
        from core.compilation.p_post_processor import (
            PCodePostProcessor,
            TypeConsistencyChecker,
            MachineConfigDetector
        )
        from core.workflow.factory import extract_machine_names_from_design_doc

        services = get_services()
        results = {
            "success": False,
            "project_path": None,
            "generated_files": {},
            "post_processing": [],
            "compilation": None,
            "checker": None,
            "errors": [],
            "warnings": []
        }

        try:
            machine_names = params.machine_names
            if not machine_names:
                machine_names = extract_machine_names_from_design_doc(params.design_doc)
                if not machine_names:
                    results["errors"].append("Could not extract machine names from design doc")
                    return with_metadata("generate_complete_project", results)

            logger.info(f"Generating project with machines: {machine_names}")

            struct_result = services["generation"].create_project_structure(
                output_dir=params.output_dir,
                project_name=params.project_name
            )

            if not struct_result.success:
                results["errors"].append(f"Failed to create structure: {struct_result.error}")
                return with_metadata("generate_complete_project", results)

            project_path = struct_result.file_path
            results["project_path"] = project_path

            types_result = services["generation"].generate_types_events(
                design_doc=params.design_doc,
                project_path=project_path,
                save_to_disk=True
            )

            if types_result.success:
                results["generated_files"]["Enums_Types_Events.p"] = types_result.file_path
            else:
                results["errors"].append(f"Failed to generate types: {types_result.error}")
                return with_metadata("generate_complete_project", results)

            context_files = {"Enums_Types_Events.p": types_result.code}

            config_detector = MachineConfigDetector()
            machine_dependencies = {}

            for machine_name in machine_names:
                machine_result = services["generation"].generate_machine(
                    machine_name=machine_name,
                    design_doc=params.design_doc,
                    project_path=project_path,
                    context_files=context_files,
                    save_to_disk=True
                )

                if machine_result.success:
                    results["generated_files"][f"{machine_name}.p"] = machine_result.file_path
                    context_files[f"{machine_name}.p"] = machine_result.code

                    deps = config_detector.detect_dependencies(machine_result.code)
                    if deps:
                        machine_dependencies[machine_name] = deps
                        results["warnings"].append(
                            f"{machine_name} has dependencies: {deps}. Ensure configuration events are sent."
                        )
                else:
                    results["errors"].append(f"Failed to generate {machine_name}: {machine_result.error}")

            if params.include_spec:
                spec_result = services["generation"].generate_spec(
                    spec_name="Safety",
                    design_doc=params.design_doc,
                    project_path=project_path,
                    context_files=context_files,
                    save_to_disk=True
                )
                if spec_result.success:
                    results["generated_files"]["Safety.p"] = spec_result.file_path
                else:
                    results["warnings"].append(f"Spec generation failed: {spec_result.error}")

            if params.include_test:
                test_result = services["generation"].generate_test(
                    test_name="TestDriver",
                    design_doc=params.design_doc,
                    project_path=project_path,
                    context_files=context_files,
                    save_to_disk=True
                )
                if test_result.success:
                    results["generated_files"]["TestDriver.p"] = test_result.file_path
                else:
                    results["warnings"].append(f"Test generation failed: {test_result.error}")

            processor = PCodePostProcessor()
            for filename, file_path in results["generated_files"].items():
                if filename.endswith(".p"):
                    try:
                        code = Path(file_path).read_text()
                        processed = processor.process(code, filename)
                        if processed.fixes_applied:
                            Path(file_path).write_text(processed.code)
                            results["post_processing"].append({
                                "file": filename,
                                "fixes": processed.fixes_applied
                            })
                    except Exception as e:
                        results["warnings"].append(f"Post-processing failed for {filename}: {e}")

            # Run type consistency check across all generated files
            try:
                type_checker = TypeConsistencyChecker()
                project_files = services["compilation"].get_project_files(project_path)
                # Extract definitions from all files first
                for _, content in project_files.items():
                    type_checker.extract_definitions(content)
                # Then check each file for undefined references
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

            if params.run_checker and results["compilation"]["success"]:
                checker_result = services["compilation"].run_checker(
                    project_path=project_path,
                    schedules=100,
                    timeout=60
                )
                results["checker"] = {
                    "success": checker_result.success,
                    "output": checker_result.stdout[:500] if checker_result.stdout else ""
                }

            results["success"] = results["compilation"]["success"]
            results["message"] = "Project generated successfully" if results["success"] else "Project generated with errors"

        except Exception as e:
            logger.error(f"Error in generate_complete_project: {e}")
            results["errors"].append(str(e))

        return with_metadata("generate_complete_project", results)

    @mcp.tool(
        name="save_p_file",
        description="Save generated P code to a file. Use this after previewing code from generate_* tools and user approves."
    )
    def save_p_file(params: SavePFileParams) -> Dict[str, Any]:
        logger.info(f"[TOOL] save_p_file: {params.file_path}")

        services = get_services()
        result = services["generation"].save_p_file(
            file_path=params.file_path,
            code=params.code
        )

        payload = {
            "success": result.success,
            "filename": result.filename,
            "file_path": result.file_path,
            "error": result.error,
            "message": f"Saved {result.filename} to disk" if result.success else result.error
        }
        return with_metadata("save_p_file", payload, token_usage=result.token_usage)

    return {
        "generate_project_structure": generate_project_structure,
        "generate_types_events": generate_types_events,
        "generate_machine": generate_machine,
        "generate_spec": generate_spec,
        "generate_test": generate_test,
        "generate_complete_project": generate_complete_project,
        "save_p_file": save_p_file,
    }
