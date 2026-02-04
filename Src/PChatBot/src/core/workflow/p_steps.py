"""
P Language Workflow Steps.

This module provides concrete workflow step implementations for P code generation,
compilation, and verification. These steps use the service layer (GenerationService,
CompilationService, FixerService) to perform their work.
"""

import os
from typing import Any, Dict, List, Optional

from .steps import WorkflowStep, StepResult, StepStatus
from ..services.generation import GenerationService, GenerationResult
from ..services.compilation import CompilationService, CompilationResult
from ..services.fixer import FixerService


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
                return StepResult.success(
                    output={
                        "types_events_code": result.code,
                        "types_events_path": result.file_path
                    },
                    artifacts={"types_events": result.code}
                )
            else:
                return StepResult.failure(result.error or "Failed to generate types/events")
                
        except Exception as e:
            return StepResult.failure(str(e))
    
    def can_skip(self, context: Dict[str, Any]) -> bool:
        project_path = context.get("project_path")
        if not project_path:
            return False
        path = os.path.join(project_path, "PSrc", "Enums_Types_Events.p")
        return os.path.exists(path)


class GenerateMachineStep(WorkflowStep):
    """Step to generate a single P state machine."""
    
    name = "generate_machine"
    description = "Generate a P state machine implementation"
    max_retries = 3
    
    def __init__(self, generation_service: GenerationService, machine_name: str):
        self.service = generation_service
        self.machine_name = machine_name
        self.name = f"generate_machine_{machine_name}"
    
    def execute(self, context: Dict[str, Any]) -> StepResult:
        design_doc = context.get("design_doc")
        project_path = context.get("project_path")
        
        if not design_doc:
            return StepResult.failure("design_doc is required")
        if not project_path:
            return StepResult.failure("project_path is required")
        
        # Collect context files from previous steps
        context_files = {}
        if "types_events_code" in context:
            context_files["Enums_Types_Events.p"] = context["types_events_code"]
        
        # Add previously generated machines
        for key, value in context.items():
            if key.startswith("machine_code_") and value:
                machine_file = key.replace("machine_code_", "") + ".p"
                context_files[machine_file] = value
        
        try:
            result = self.service.generate_machine(
                machine_name=self.machine_name,
                design_doc=design_doc,
                project_path=project_path,
                context_files=context_files,
                save_to_disk=False  # Preview mode
            )
            
            if result.success:
                return StepResult.success(
                    output={
                        f"machine_code_{self.machine_name}": result.code,
                        f"machine_path_{self.machine_name}": result.file_path
                    },
                    artifacts={f"machine_{self.machine_name}": result.code}
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
    
    def __init__(self, generation_service: GenerationService, spec_name: str = "Safety"):
        self.service = generation_service
        self.spec_name = spec_name
        self.name = f"generate_spec_{spec_name}"
    
    def execute(self, context: Dict[str, Any]) -> StepResult:
        design_doc = context.get("design_doc")
        project_path = context.get("project_path")
        
        if not design_doc:
            return StepResult.failure("design_doc is required")
        if not project_path:
            return StepResult.failure("project_path is required")
        
        # Collect context files
        context_files = self._collect_context_files(context, project_path)
        
        try:
            result = self.service.generate_spec(
                spec_name=self.spec_name,
                design_doc=design_doc,
                project_path=project_path,
                context_files=context_files,
                save_to_disk=False
            )
            
            if result.success:
                return StepResult.success(
                    output={
                        f"spec_code_{self.spec_name}": result.code,
                        f"spec_path_{self.spec_name}": result.file_path
                    },
                    artifacts={f"spec_{self.spec_name}": result.code}
                )
            else:
                return StepResult.failure(result.error or f"Failed to generate {self.spec_name}")
                
        except Exception as e:
            return StepResult.failure(str(e))
    
    def _collect_context_files(self, context: Dict[str, Any], project_path: str) -> Dict[str, str]:
        """Collect P source files for context."""
        context_files = {}
        
        # From context
        if "types_events_code" in context:
            context_files["Enums_Types_Events.p"] = context["types_events_code"]
        
        for key, value in context.items():
            if key.startswith("machine_code_") and value:
                machine_file = key.replace("machine_code_", "") + ".p"
                context_files[machine_file] = value
        
        # From disk (if not in context)
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
    
    def __init__(self, generation_service: GenerationService, test_name: str = "TestDriver"):
        self.service = generation_service
        self.test_name = test_name
        self.name = f"generate_test_{test_name}"
    
    def execute(self, context: Dict[str, Any]) -> StepResult:
        design_doc = context.get("design_doc")
        project_path = context.get("project_path")
        
        if not design_doc:
            return StepResult.failure("design_doc is required")
        if not project_path:
            return StepResult.failure("project_path is required")
        
        # Collect all context files (source + spec)
        context_files = self._collect_all_context(context, project_path)
        
        try:
            result = self.service.generate_test(
                test_name=self.test_name,
                design_doc=design_doc,
                project_path=project_path,
                context_files=context_files,
                save_to_disk=False
            )
            
            if result.success:
                return StepResult.success(
                    output={
                        f"test_code_{self.test_name}": result.code,
                        f"test_path_{self.test_name}": result.file_path
                    },
                    artifacts={f"test_{self.test_name}": result.code}
                )
            else:
                return StepResult.failure(result.error or f"Failed to generate {self.test_name}")
                
        except Exception as e:
            return StepResult.failure(str(e))
    
    def _collect_all_context(self, context: Dict[str, Any], project_path: str) -> Dict[str, str]:
        """Collect all P files for context."""
        context_files = {}
        
        # From context (generated in this workflow run)
        for key, value in context.items():
            if value and ("_code_" in key or key == "types_events_code"):
                if key == "types_events_code":
                    context_files["Enums_Types_Events.p"] = value
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
                        "compilation_output": result.output
                    }
                )
            else:
                # Store errors for potential fixing
                return StepResult.failure(
                    f"Compilation failed with {len(result.errors)} error(s): {result.errors[0] if result.errors else 'Unknown'}"
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
        
        user_guidance = context.get("user_guidance")
        
        try:
            result = self.service.fix_iteratively(
                project_path=project_path,
                max_iterations=10,
                user_guidance=user_guidance
            )
            
            if result.success:
                return StepResult.success(
                    output={
                        "compilation_success": True,
                        "fixes_applied": result.fixes_applied
                    }
                )
            elif result.needs_guidance:
                return StepResult.needs_guidance(
                    result.guidance_questions or "Unable to fix errors automatically. Please provide guidance.",
                    {"last_error": result.error}
                )
            else:
                return StepResult.failure(result.error or "Failed to fix compilation errors")
                
        except Exception as e:
            return StepResult.failure(str(e))
    
    def can_skip(self, context: Dict[str, Any]) -> bool:
        # Skip if compilation succeeded
        return context.get("compilation_success", False)


class RunCheckerStep(WorkflowStep):
    """Step to run PChecker on the project."""
    
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
            
            if result.success:
                return StepResult.success(
                    output={
                        "checker_success": True,
                        "checker_output": result.output
                    }
                )
            else:
                return StepResult.failure(
                    f"PChecker found errors: {result.output or result.error}"
                )
                
        except Exception as e:
            return StepResult.failure(str(e))
    
    def can_skip(self, context: Dict[str, Any]) -> bool:
        # Skip if compilation failed
        return not context.get("compilation_success", False)


class FixCheckerErrorsStep(WorkflowStep):
    """Step to fix PChecker errors."""
    
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
        
        checker_output = context.get("checker_output", "")
        user_guidance = context.get("user_guidance")
        
        try:
            result = self.service.fix_checker_error(
                project_path=project_path,
                trace_log=checker_output,
                user_guidance=user_guidance
            )
            
            if result.success:
                return StepResult.success(
                    output={
                        "checker_fix_applied": True,
                        "fix_description": result.fix_description
                    }
                )
            elif result.needs_guidance:
                return StepResult.needs_guidance(
                    result.guidance_questions or "Unable to fix checker errors automatically.",
                    {"trace_log": checker_output}
                )
            else:
                return StepResult.failure(result.error or "Failed to fix checker errors")
                
        except Exception as e:
            return StepResult.failure(str(e))
    
    def can_skip(self, context: Dict[str, Any]) -> bool:
        return context.get("checker_success", False)
