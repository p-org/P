"""
Validation Pipeline for PeasyAI.

This module provides a pipeline that runs multiple validators on generated code
and can apply auto-fixes for common issues.
"""

from typing import Dict, List, Optional, Type
from dataclasses import dataclass, field

from .validators import (
    Validator,
    ValidationResult,
    ValidationIssue,
    IssueSeverity,
    SyntaxValidator,
    TypeDeclarationValidator,
    EventDeclarationValidator,
    MachineStructureValidator,
)


@dataclass
class PipelineResult:
    """Result of running the validation pipeline."""
    is_valid: bool
    original_code: str
    fixed_code: str
    issues: List[ValidationIssue] = field(default_factory=list)
    fixes_applied: int = 0
    validators_run: List[str] = field(default_factory=list)
    
    @property
    def errors(self) -> List[ValidationIssue]:
        return [i for i in self.issues if i.severity == IssueSeverity.ERROR]
    
    @property
    def warnings(self) -> List[ValidationIssue]:
        return [i for i in self.issues if i.severity == IssueSeverity.WARNING]
    
    def summary(self) -> str:
        """Get a human-readable summary."""
        lines = []
        lines.append(f"Validation {'PASSED' if self.is_valid else 'FAILED'}")
        lines.append(f"  Validators run: {len(self.validators_run)}")
        lines.append(f"  Errors: {len(self.errors)}")
        lines.append(f"  Warnings: {len(self.warnings)}")
        lines.append(f"  Auto-fixes applied: {self.fixes_applied}")
        return "\n".join(lines)


class ValidationPipeline:
    """
    Pipeline that runs multiple validators on P code.
    
    The pipeline:
    1. Runs all registered validators
    2. Collects issues from each validator
    3. Applies auto-fixes where possible
    4. Returns combined results
    
    Usage:
        pipeline = ValidationPipeline()
        result = pipeline.validate(code, context)
        
        if not result.is_valid:
            print(result.summary())
            for issue in result.errors:
                print(f"  - {issue.message}")
    """
    
    # Default validators
    DEFAULT_VALIDATORS: List[Type[Validator]] = [
        SyntaxValidator,
        TypeDeclarationValidator,
        EventDeclarationValidator,
        MachineStructureValidator,
    ]
    
    def __init__(self, validators: Optional[List[Validator]] = None):
        """
        Initialize the pipeline.
        
        Args:
            validators: Optional list of validator instances.
                       If not provided, uses default validators.
        """
        if validators is not None:
            self.validators = validators
        else:
            self.validators = [v() for v in self.DEFAULT_VALIDATORS]
    
    def add_validator(self, validator: Validator) -> None:
        """Add a validator to the pipeline."""
        self.validators.append(validator)
    
    def remove_validator(self, validator_name: str) -> bool:
        """Remove a validator by name."""
        for i, v in enumerate(self.validators):
            if v.name == validator_name:
                self.validators.pop(i)
                return True
        return False
    
    def validate(
        self,
        code: str,
        context: Optional[Dict[str, str]] = None,
        apply_fixes: bool = True
    ) -> PipelineResult:
        """
        Run all validators on the code.
        
        Args:
            code: The P code to validate
            context: Optional context (other project files)
            apply_fixes: Whether to apply auto-fixes
            
        Returns:
            PipelineResult with combined results
        """
        all_issues: List[ValidationIssue] = []
        validators_run: List[str] = []
        current_code = code
        fixes_applied = 0
        
        for validator in self.validators:
            result = validator.validate(current_code, context)
            validators_run.append(validator.name)
            all_issues.extend(result.issues)
            
            # Apply auto-fixes if enabled
            if apply_fixes:
                for issue in result.issues:
                    if issue.auto_fixable and issue.fix_function:
                        try:
                            new_code = issue.apply_fix(current_code)
                            if new_code != current_code:
                                current_code = new_code
                                fixes_applied += 1
                        except Exception:
                            pass  # Skip failed fixes
        
        # Determine overall validity (no errors)
        is_valid = all(
            issue.severity != IssueSeverity.ERROR
            for issue in all_issues
        )
        
        return PipelineResult(
            is_valid=is_valid,
            original_code=code,
            fixed_code=current_code,
            issues=all_issues,
            fixes_applied=fixes_applied,
            validators_run=validators_run
        )
    
    def validate_file(
        self,
        file_path: str,
        context: Optional[Dict[str, str]] = None
    ) -> PipelineResult:
        """
        Validate a P file.
        
        Args:
            file_path: Path to the .p file
            context: Optional context files
            
        Returns:
            PipelineResult
        """
        with open(file_path, "r") as f:
            code = f.read()
        return self.validate(code, context)
    
    def validate_project(
        self,
        project_path: str
    ) -> Dict[str, PipelineResult]:
        """
        Validate all P files in a project.
        
        Args:
            project_path: Path to the P project
            
        Returns:
            Dictionary mapping file paths to their validation results
        """
        import os
        
        results = {}
        all_files = {}
        
        # First, collect all files for context
        for dir_name in ["PSrc", "PSpec", "PTst"]:
            dir_path = os.path.join(project_path, dir_name)
            if os.path.exists(dir_path):
                for filename in os.listdir(dir_path):
                    if filename.endswith(".p"):
                        file_path = os.path.join(dir_path, filename)
                        rel_path = os.path.join(dir_name, filename)
                        with open(file_path, "r") as f:
                            all_files[rel_path] = f.read()
        
        # Now validate each file with context
        for rel_path, code in all_files.items():
            # Create context without current file
            context = {k: v for k, v in all_files.items() if k != rel_path}
            results[rel_path] = self.validate(code, context)
        
        return results


def create_default_pipeline() -> ValidationPipeline:
    """Create a validation pipeline with default validators."""
    return ValidationPipeline()


def validate_p_code(
    code: str,
    context: Optional[Dict[str, str]] = None
) -> PipelineResult:
    """
    Convenience function to validate P code.
    
    Args:
        code: The P code to validate
        context: Optional context files
        
    Returns:
        PipelineResult
    """
    pipeline = create_default_pipeline()
    return pipeline.validate(code, context)
