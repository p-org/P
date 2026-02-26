"""
P Language Workflow Steps.

This module provides concrete workflow step implementations for P code generation,
compilation, and verification. These steps use the service layer (GenerationService,
CompilationService, FixerService) to perform their work.
"""

import logging
import os
from typing import Any, Dict, List, Optional

from .steps import WorkflowStep, StepResult, StepStatus
from ..services.generation import GenerationService, GenerationResult
from ..services.compilation import CompilationService, CompilationResult
from ..services.fixer import FixerService

logger = logging.getLogger(__name__)


def _run_validation_pipeline(
    code: str,
    filename: str,
    project_path: str,
    is_test_file: bool = False,
) -> str:
    """Run the ValidationPipeline on generated code and return the fixed code.

    This is the single place where post-processing + structured validation
    happens for the workflow path.  The MCP tool path has its own equivalent
    call in ``_review_generated_code()`` (tools/generation.py).
    """
    try:
        from ..validation.pipeline import ValidationPipeline

        pipeline = ValidationPipeline(include_test_validators=is_test_file)
        result = pipeline.validate(
            code,
            filename=filename,
            project_path=project_path,
            is_test_file=is_test_file,
        )
        if result.fixes_applied:
            logger.info(
                f"Validation pipeline applied {len(result.fixes_applied)} "
                f"fix(es) to {filename}"
            )
        return result.fixed_code
    except Exception as e:
        logger.warning(f"Validation pipeline failed for {filename}: {e}")
        return code


class CreateProjectStructureStep(WorkflowStep):
    """Step to create P project directory structure."""
    
    name = "create_project_structure"
    description = "Create P project directories (PSrc, PSpec, PTst) and .pproj file"
    max_retries = 1  # No point retrying filesystem operations
    
    def __init__(self, generation_service: GenerationService):
        self.service = generation_service
    
    def execute(self, context: Dict[str, Any]) -> StepResult:
        project_path = context.get("project_path")
        project_name = context.get("project_name", "PProject")
        
        if not project_path:
            return StepResult.failure("project_path is required")
        
        try:
            result = self.service.create_project_structure(
                output_dir=project_path,
                project_name=project_name
            )
            
            if result.success:
                # The service creates a timestamped subdirectory
                actual_project_path = result.file_path or project_path
                return StepResult.success(
                    output={"project_path": actual_project_path},
                    artifacts={"pproj_file": os.path.join(actual_project_path, f"{project_name}.pproj")}
                )
            else:
                return StepResult.failure(result.error or "Failed to create project structure")
                
        except Exception as e:
            return StepResult.failure(str(e))
    
    def can_skip(self, context: Dict[str, Any]) -> bool:
        project_path = context.get("project_path")
        if not project_path:
            return False
        # Skip if PSrc directory already exists
        return os.path.exists(os.path.join(project_path, "PSrc"))


class GenerateTypesEventsStep(WorkflowStep):
    """Step to generate Enums_Types_Events.p file."""
    
    name = "generate_types_events"
    description = "Generate shared types, enums, and events file"
    max_retries = 3
    
    def __init__(self, generation_service: GenerationService):
        self.service = generation_service
    
    def execute(self, context: Dict[str, Any]) -> StepResult:
        design_doc = context.get("design_doc")
        project_path = context.get("project_path")
        
        if not design_doc:
            return StepResult.failure("design_doc is required")
        if not project_path:
            return StepResult.failure("project_path is required")
        
        try:
            result = self.service.generate_types_events(
                design_doc=design_doc,
                project_path=project_path,
                save_to_disk=False  # Preview mode
            )
            
            if result.success:
                filename = result.filename or "Enums_Types_Events.p"
                code = _run_validation_pipeline(
                    result.code, filename, project_path
                )
                return StepResult.success(
                    output={
                        "types_events_code": code,
                        "types_events_path": result.file_path,
                        "types_events_filename": filename,
                    },
                    artifacts={"types_events": code}
                )
            else:
                return StepResult.failure(result.error or "Failed to generate types/events")
                
        except Exception as e:
            return StepResult.failure(str(e))
    
    def can_skip(self, context: Dict[str, Any]) -> bool:
        # Skip if we already have types code in context or any .p file in PSrc
        if context.get("types_events_code"):
            return True
        project_path = context.get("project_path")
        if not project_path:
            return False
        psrc = os.path.join(project_path, "PSrc")
        if os.path.isdir(psrc):
            return any(f.endswith(".p") for f in os.listdir(psrc))
        return False


class GenerateMachineStep(WorkflowStep):
    """Step to generate a single P state machine."""
    
    name = "generate_machine"
    description = "Generate a P state machine implementation"
    max_retries = 3
    
    def __init__(self, generation_service: GenerationService, machine_name: str, ensemble_size: int = 3):
        self.service = generation_service
        self.machine_name = machine_name
        self.ensemble_size = ensemble_size
        self.name = f"generate_machine_{machine_name}"
    
    def execute(self, context: Dict[str, Any]) -> StepResult:
        design_doc = context.get("design_doc")
        project_path = context.get("project_path")
        ensemble_size = context.get("ensemble_size", self.ensemble_size)
        
        if not design_doc:
            return StepResult.failure("design_doc is required")
        if not project_path:
            return StepResult.failure("project_path is required")
        
        # Collect context files from previous steps
        context_files = {}
        # Use actual types filename from context, fallback to convention
        types_filename = context.get("types_events_filename", "Enums_Types_Events.p")
        if "types_events_code" in context:
            context_files[types_filename] = context["types_events_code"]
        
        # Add previously generated machines
        for key, value in context.items():
            if key.startswith("machine_code_") and value:
                machine_file = key.replace("machine_code_", "") + ".p"
                context_files[machine_file] = value
        
        try:
            if ensemble_size > 1:
                result = self.service.generate_machine_ensemble(
                    machine_name=self.machine_name,
                    design_doc=design_doc,
                    project_path=project_path,
                    context_files=context_files,
                    ensemble_size=ensemble_size,
                    save_to_disk=False  # Preview mode
                )
            else:
                result = self.service.generate_machine(
                    machine_name=self.machine_name,
                    design_doc=design_doc,
                    project_path=project_path,
                    context_files=context_files,
                    save_to_disk=False  # Preview mode
                )
            
            if result.success:
                filename = result.filename or f"{self.machine_name}.p"
                code = _run_validation_pipeline(
                    result.code, filename, project_path
                )
                return StepResult.success(
                    output={
                        f"machine_code_{self.machine_name}": code,
                        f"machine_path_{self.machine_name}": result.file_path
                    },
                    artifacts={f"machine_{self.machine_name}": code}
                )
            else:
                return StepResult.failure(result.error or f"Failed to generate {self.machine_name}")
                
        except Exception as e:
            return StepResult.failure(str(e))
    
    def can_skip(self, context: Dict[str, Any]) -> bool:
        project_path = context.get("project_path")
        if not project_path:
            return False
        path = os.path.join(project_path, "PSrc", f"{self.machine_name}.p")
        return os.path.exists(path)


class GenerateSpecStep(WorkflowStep):
    """Step to generate a P specification/monitor."""
    
    name = "generate_spec"
    description = "Generate a P specification/monitor file"
    max_retries = 3
    
    def __init__(self, generation_service: GenerationService, spec_name: str = "Safety", ensemble_size: int = 3):
        self.service = generation_service
        self.spec_name = spec_name
        self.ensemble_size = ensemble_size
        self.name = f"generate_spec_{spec_name}"
    
    def execute(self, context: Dict[str, Any]) -> StepResult:
        design_doc = context.get("design_doc")
        project_path = context.get("project_path")
        ensemble_size = context.get("ensemble_size", self.ensemble_size)
        
        if not design_doc:
            return StepResult.failure("design_doc is required")
        if not project_path:
            return StepResult.failure("project_path is required")
        
        # Collect context files
        context_files = self._collect_context_files(context, project_path)
        
        try:
            if ensemble_size > 1:
                result = self.service.generate_spec_ensemble(
                    spec_name=self.spec_name,
                    design_doc=design_doc,
                    project_path=project_path,
                    context_files=context_files,
                    ensemble_size=ensemble_size,
                    save_to_disk=False
                )
            else:
                result = self.service.generate_spec(
                    spec_name=self.spec_name,
                    design_doc=design_doc,
                    project_path=project_path,
                    context_files=context_files,
                    save_to_disk=False
                )
            
            if result.success:
                filename = result.filename or f"{self.spec_name}.p"
                code = _run_validation_pipeline(
                    result.code, filename, project_path
                )
                return StepResult.success(
                    output={
                        f"spec_code_{self.spec_name}": code,
                        f"spec_path_{self.spec_name}": result.file_path
                    },
                    artifacts={f"spec_{self.spec_name}": code}
                )
            else:
                return StepResult.failure(result.error or f"Failed to generate {self.spec_name}")
                
        except Exception as e:
            return StepResult.failure(str(e))
    
    def _collect_context_files(self, context: Dict[str, Any], project_path: str) -> Dict[str, str]:
        """Collect P source files for context."""
        context_files = {}
        
        # From context — use actual types filename
        types_filename = context.get("types_events_filename", "Enums_Types_Events.p")
        if "types_events_code" in context:
            context_files[types_filename] = context["types_events_code"]
        
        for key, value in context.items():
            if key.startswith("machine_code_") and value:
                machine_file = key.replace("machine_code_", "") + ".p"
                context_files[machine_file] = value
        
        # From disk (if not in context) — picks up whatever filenames exist
        psrc_dir = os.path.join(project_path, "PSrc")
        if os.path.exists(psrc_dir):
            for filename in os.listdir(psrc_dir):
                if filename.endswith(".p") and filename not in context_files:
                    filepath = os.path.join(psrc_dir, filename)
                    with open(filepath, "r") as f:
                        context_files[filename] = f.read()
        
        return context_files
    
    def can_skip(self, context: Dict[str, Any]) -> bool:
        project_path = context.get("project_path")
        if not project_path:
            return False
        path = os.path.join(project_path, "PSpec", f"{self.spec_name}.p")
        return os.path.exists(path)


class GenerateTestStep(WorkflowStep):
    """Step to generate a P test file."""
    
    name = "generate_test"
    description = "Generate a P test driver file"
    max_retries = 3
    
    def __init__(self, generation_service: GenerationService, test_name: str = "TestDriver", ensemble_size: int = 3):
        self.service = generation_service
        self.test_name = test_name
        self.ensemble_size = ensemble_size
        self.name = f"generate_test_{test_name}"
    
    def execute(self, context: Dict[str, Any]) -> StepResult:
        design_doc = context.get("design_doc")
        project_path = context.get("project_path")
        ensemble_size = context.get("ensemble_size", self.ensemble_size)
        
        if not design_doc:
            return StepResult.failure("design_doc is required")
        if not project_path:
            return StepResult.failure("project_path is required")
        
        # Collect all context files (source + spec)
        context_files = self._collect_all_context(context, project_path)
        
        try:
            if ensemble_size > 1:
                result = self.service.generate_test_ensemble(
                    test_name=self.test_name,
                    design_doc=design_doc,
                    project_path=project_path,
                    context_files=context_files,
                    ensemble_size=ensemble_size,
                    save_to_disk=False
                )
            else:
                result = self.service.generate_test(
                    test_name=self.test_name,
                    design_doc=design_doc,
                    project_path=project_path,
                    context_files=context_files,
                    save_to_disk=False
                )
            
            if result.success:
                filename = result.filename or f"{self.test_name}.p"
                code = _run_validation_pipeline(
                    result.code, filename, project_path,
                    is_test_file=True,
                )
                return StepResult.success(
                    output={
                        f"test_code_{self.test_name}": code,
                        f"test_path_{self.test_name}": result.file_path
                    },
                    artifacts={f"test_{self.test_name}": code}
                )
            else:
                return StepResult.failure(result.error or f"Failed to generate {self.test_name}")
                
        except Exception as e:
            return StepResult.failure(str(e))
    
    def _collect_all_context(self, context: Dict[str, Any], project_path: str) -> Dict[str, str]:
        """Collect all P files for context."""
        context_files = {}
        
        # From context (generated in this workflow run)
        types_filename = context.get("types_events_filename", "Enums_Types_Events.p")
        for key, value in context.items():
            if value and ("_code_" in key or key == "types_events_code"):
                if key == "types_events_code":
                    context_files[types_filename] = value
                elif key.startswith("machine_code_"):
                    context_files[key.replace("machine_code_", "") + ".p"] = value
                elif key.startswith("spec_code_"):
                    context_files[key.replace("spec_code_", "") + ".p"] = value
        
        # From disk
        for folder in ["PSrc", "PSpec"]:
            folder_path = os.path.join(project_path, folder)
            if os.path.exists(folder_path):
                for filename in os.listdir(folder_path):
                    if filename.endswith(".p") and filename not in context_files:
                        filepath = os.path.join(folder_path, filename)
                        with open(filepath, "r") as f:
                            context_files[filename] = f.read()
        
        return context_files
    
    def can_skip(self, context: Dict[str, Any]) -> bool:
        project_path = context.get("project_path")
        if not project_path:
            return False
        path = os.path.join(project_path, "PTst", f"{self.test_name}.p")
        return os.path.exists(path)


class SaveGeneratedFilesStep(WorkflowStep):
    """Step to save all generated files to disk."""
    
    name = "save_generated_files"
    description = "Save all generated P code files to disk"
    max_retries = 1
    
    def execute(self, context: Dict[str, Any]) -> StepResult:
        project_path = context.get("project_path")
        if not project_path:
            return StepResult.failure("project_path is required")
        
        saved_files = []
        errors = []
        
        # Save types/events
        if "types_events_code" in context and "types_events_path" in context:
            try:
                self._save_file(context["types_events_path"], context["types_events_code"])
                saved_files.append(context["types_events_path"])
            except Exception as e:
                errors.append(f"Failed to save types/events: {e}")
        
        # Save machines
        for key, value in context.items():
            if key.startswith("machine_code_") and value:
                path_key = key.replace("machine_code_", "machine_path_")
                if path_key in context:
                    try:
                        self._save_file(context[path_key], value)
                        saved_files.append(context[path_key])
                    except Exception as e:
                        errors.append(f"Failed to save {key}: {e}")
        
        # Save specs
        for key, value in context.items():
            if key.startswith("spec_code_") and value:
                path_key = key.replace("spec_code_", "spec_path_")
                if path_key in context:
                    try:
                        self._save_file(context[path_key], value)
                        saved_files.append(context[path_key])
                    except Exception as e:
                        errors.append(f"Failed to save {key}: {e}")
        
        # Save tests
        for key, value in context.items():
            if key.startswith("test_code_") and value:
                path_key = key.replace("test_code_", "test_path_")
                if path_key in context:
                    try:
                        self._save_file(context[path_key], value)
                        saved_files.append(context[path_key])
                    except Exception as e:
                        errors.append(f"Failed to save {key}: {e}")
        
        if errors:
            return StepResult.failure("; ".join(errors))
        
        return StepResult.success(
            output={"saved_files": saved_files},
            artifacts={"saved_files": saved_files}
        )
    
    def _save_file(self, path: str, content: str) -> None:
        """Save content to file, creating directories if needed."""
        os.makedirs(os.path.dirname(path), exist_ok=True)
        with open(path, "w") as f:
            f.write(content)
    
    def can_skip(self, context: Dict[str, Any]) -> bool:
        # Never skip saving
        return False


class CompileProjectStep(WorkflowStep):
    """Step to compile the P project."""
    
    name = "compile_project"
    description = "Compile the P project and check for errors"
    max_retries = 1  # Compilation itself doesn't benefit from retries
    
    def __init__(self, compilation_service: CompilationService):
        self.service = compilation_service
    
    def execute(self, context: Dict[str, Any]) -> StepResult:
        project_path = context.get("project_path")
        if not project_path:
            return StepResult.failure("project_path is required")
        
        try:
            result = self.service.compile(project_path)
            
            if result.success:
                return StepResult.success(
                    output={
                        "compilation_success": True,
                        "compilation_output": result.stdout
                    }
                )
            else:
                error_output = result.stderr or result.stdout or "Unknown compilation error"
                return StepResult.failure(
                    f"Compilation failed: {error_output[:500]}"
                )
                
        except Exception as e:
            return StepResult.failure(str(e))
    
    def can_skip(self, context: Dict[str, Any]) -> bool:
        # Always compile to verify
        return False


class FixCompilationErrorsStep(WorkflowStep):
    """Step to fix compilation errors iteratively."""
    
    name = "fix_compilation_errors"
    description = "Attempt to fix compilation errors using AI"
    max_retries = 5  # More retries for fixing
    
    def __init__(self, fixer_service: FixerService):
        self.service = fixer_service
    
    def execute(self, context: Dict[str, Any]) -> StepResult:
        project_path = context.get("project_path")
        if not project_path:
            return StepResult.failure("project_path is required")
        
        # Check if there are errors to fix
        if context.get("compilation_success"):
            return StepResult.skipped("No compilation errors to fix")
        
        try:
            result = self.service.fix_iteratively(
                project_path=project_path,
                max_iterations=10,
            )
            
            if result.get("success"):
                return StepResult.success(
                    output={
                        "compilation_success": True,
                        "fixes_applied": result.get("iterations", [])
                    }
                )
            else:
                iterations = result.get("iterations", [])
                last_error = iterations[-1].get("error", "Unknown") if iterations else "Unknown"
                return StepResult.failure(
                    f"Failed to fix compilation errors after {result.get('total_iterations', 0)} iterations: {last_error}"
                )
                
        except Exception as e:
            return StepResult.failure(str(e))
    
    def can_skip(self, context: Dict[str, Any]) -> bool:
        # Skip if compilation succeeded
        return context.get("compilation_success", False)


class RunCheckerStep(WorkflowStep):
    """Step to run PChecker on the project.
    
    Always returns success so that downstream fix steps can run.
    Sets checker_success=False in output when bugs are found, along with
    the checker output for the fixer to analyze.
    """
    
    name = "run_checker"
    description = "Run PChecker to verify correctness"
    max_retries = 1
    
    def __init__(self, compilation_service: CompilationService, schedules: int = 100, timeout: int = 60):
        self.service = compilation_service
        self.schedules = schedules
        self.timeout = timeout
    
    def execute(self, context: Dict[str, Any]) -> StepResult:
        project_path = context.get("project_path")
        if not project_path:
            return StepResult.failure("project_path is required")
        
        try:
            result = self.service.run_checker(
                project_path=project_path,
                schedules=self.schedules,
                timeout=self.timeout
            )
            
            summary = (
                f"Passed: {result.passed_tests}, Failed: {result.failed_tests}"
                if result.test_results else ""
            )
            
            if result.success:
                return StepResult.success(
                    output={
                        "checker_success": True,
                        "checker_output": summary
                    }
                )
            else:
                # Return success so the workflow continues to the fix step.
                # checker_success=False signals that bugs were found.
                return StepResult.success(
                    output={
                        "checker_success": False,
                        "checker_output": result.error or summary or ""
                    }
                )
                
        except Exception as e:
            return StepResult.failure(str(e))
    
    def can_skip(self, context: Dict[str, Any]) -> bool:
        # Skip only if compilation explicitly failed (not if key is absent)
        return context.get("compilation_success") is False


class FixCheckerErrorsStep(WorkflowStep):
    """Step to fix PChecker errors.
    
    Reads the detailed trace file from PCheckerOutput/BugFinding/ for
    accurate root-cause analysis, falling back to checker stdout if
    no trace file is found.
    """
    
    name = "fix_checker_errors"
    description = "Attempt to fix PChecker errors using AI"
    max_retries = 3
    
    def __init__(self, fixer_service: FixerService):
        self.service = fixer_service
    
    def execute(self, context: Dict[str, Any]) -> StepResult:
        project_path = context.get("project_path")
        if not project_path:
            return StepResult.failure("project_path is required")
        
        if context.get("checker_success"):
            return StepResult.skipped("No checker errors to fix")
        
        user_guidance = context.get("user_guidance")
        
        # Read the detailed trace file from PCheckerOutput/BugFinding/
        # This contains the full execution trace needed for analysis,
        # rather than just the PChecker stdout summary.
        trace_log = self._read_latest_trace(project_path)
        if not trace_log:
            # Fall back to checker stdout if no trace file found
            trace_log = context.get("checker_output", "")
        
        if not trace_log:
            return StepResult.failure(
                "No PChecker trace available. Cannot diagnose the bug."
            )
        
        try:
            result = self.service.fix_checker_error(
                project_path=project_path,
                trace_log=trace_log,
                user_guidance=user_guidance
            )
            
            if result.fixed:
                return StepResult.success(
                    output={
                        "checker_fix_applied": True,
                        "checker_success": True,
                        "fix_file": result.file_path,
                    }
                )
            elif result.needs_guidance:
                guidance_msg = "Unable to fix checker errors automatically."
                if result.guidance_request:
                    guidance_msg = result.guidance_request.get("message", guidance_msg)
                return StepResult.needs_guidance(
                    guidance_msg,
                    {"trace_log": trace_log}
                )
            else:
                return StepResult.failure(result.error or "Failed to fix checker errors")
                
        except Exception as e:
            return StepResult.failure(str(e))
    
    def _read_latest_trace(self, project_path: str) -> Optional[str]:
        """Read the latest PChecker trace file from PCheckerOutput/BugFinding/."""
        bug_dir = os.path.join(project_path, "PCheckerOutput", "BugFinding")
        if not os.path.isdir(bug_dir):
            return None
        
        trace_files = sorted(
            [f for f in os.listdir(bug_dir) if f.endswith(".txt")],
            key=lambda f: os.path.getmtime(os.path.join(bug_dir, f)),
            reverse=True
        )
        
        if not trace_files:
            return None
        
        trace_path = os.path.join(bug_dir, trace_files[0])
        try:
            with open(trace_path, "r") as f:
                return f.read()
        except Exception:
            return None
    
    def can_skip(self, context: Dict[str, Any]) -> bool:
        return context.get("checker_success", False)
