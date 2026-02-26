"""
Validation Pipeline for PeasyAI.

Two-stage validation:
  Stage 1 — PCodePostProcessor: deterministic auto-fixes for common LLM mistakes.
  Stage 2 — Validator chain: structured checks with severity levels and auto-fix support.

Usage::

    from core.validation import validate_p_code

    result = validate_p_code(code, project_path="/path/to/project")
    print(result.summary())
"""

from .validators import (
    ValidationResult,
    ValidationIssue,
    IssueSeverity,
    Validator,
    SyntaxValidator,
    InlineInitValidator,
    VarDeclarationOrderValidator,
    CollectionOpsValidator,
    TypeDeclarationValidator,
    EventDeclarationValidator,
    MachineStructureValidator,
    SpecObservesConsistencyValidator,
    DuplicateDeclarationValidator,
    SpecForbiddenKeywordValidator,
    TestFileValidator,
    PayloadFieldValidator,
    NamedTupleConstructionValidator,
)
from .pipeline import (
    ValidationPipeline,
    PipelineResult,
    validate_p_code,
    create_default_pipeline,
)
from .input_validators import (
    DesignDocValidator,
    ProjectPathValidator,
)

__all__ = [
    # Results
    "ValidationResult",
    "ValidationIssue",
    "IssueSeverity",
    "PipelineResult",
    # Base
    "Validator",
    # Code validators
    "SyntaxValidator",
    "InlineInitValidator",
    "VarDeclarationOrderValidator",
    "CollectionOpsValidator",
    "TypeDeclarationValidator",
    "EventDeclarationValidator",
    "MachineStructureValidator",
    "SpecObservesConsistencyValidator",
    "DuplicateDeclarationValidator",
    "SpecForbiddenKeywordValidator",
    "TestFileValidator",
    "PayloadFieldValidator",
    "NamedTupleConstructionValidator",
    # Pipeline
    "ValidationPipeline",
    "validate_p_code",
    "create_default_pipeline",
    # Input validators
    "DesignDocValidator",
    "ProjectPathValidator",
]
