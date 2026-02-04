"""
Validation Pipeline for PChatBot.

This module provides validation utilities for:
- Input validation (design documents, file paths)
- Output validation (generated P code)
- P language syntax validation
- Code post-processing and cleanup
"""

from .validators import (
    ValidationResult,
    ValidationIssue,
    IssueSeverity,
    Validator,
    SyntaxValidator,
    TypeDeclarationValidator,
    EventDeclarationValidator,
    MachineStructureValidator,
)
from .pipeline import ValidationPipeline
from .input_validators import (
    DesignDocValidator,
    ProjectPathValidator,
)
from .p_code_validator import (
    PCodePostProcessor,
    PCodeValidator,
    process_and_validate,
)

__all__ = [
    # Results
    "ValidationResult",
    "ValidationIssue",
    "IssueSeverity",
    # Base
    "Validator",
    # Code validators
    "SyntaxValidator",
    "TypeDeclarationValidator",
    "EventDeclarationValidator",
    "MachineStructureValidator",
    # Pipeline
    "ValidationPipeline",
    # Input validators
    "DesignDocValidator",
    "ProjectPathValidator",
    # P code post-processing
    "PCodePostProcessor",
    "PCodeValidator",
    "process_and_validate",
]
