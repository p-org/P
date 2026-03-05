"""
Unified Validation Pipeline for PeasyAI.

Two-stage approach:
  Stage 1 — **Auto-fix** (PCodePostProcessor): deterministic regex fixes for
            the most common LLM mistakes (var ordering, trailing commas, enum
            syntax, bare halt, forbidden keywords in monitors, etc.).
  Stage 2 — **Validate** (Validator chain): structured checks that produce
            typed issues with severity levels.  Some validators also carry
            auto-fix functions; the pipeline applies those too.

The pipeline replaces the ad-hoc orchestration that previously lived in
``generation.py::_review_generated_code``.
"""

import logging
from pathlib import Path
from typing import Dict, List, Optional, Type
from dataclasses import dataclass, field

from .validators import (
    Validator,
    ValidationIssue,
    IssueSeverity,
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

logger = logging.getLogger(__name__)


@dataclass
class PipelineResult:
    """Result of running the full validation pipeline."""
    is_valid: bool
    original_code: str
    fixed_code: str
    issues: List[ValidationIssue] = field(default_factory=list)
    fixes_applied: List[str] = field(default_factory=list)
    validators_run: List[str] = field(default_factory=list)

    @property
    def errors(self) -> List[ValidationIssue]:
        return [i for i in self.issues if i.severity == IssueSeverity.ERROR]

    @property
    def warnings(self) -> List[ValidationIssue]:
        return [i for i in self.issues if i.severity == IssueSeverity.WARNING]

    def summary(self) -> str:
        lines = [
            f"Validation {'PASSED' if self.is_valid else 'FAILED'}",
            f"  Validators run: {len(self.validators_run)}",
            f"  Errors: {len(self.errors)}",
            f"  Warnings: {len(self.warnings)}",
            f"  Auto-fixes applied: {len(self.fixes_applied)}",
        ]
        return "\n".join(lines)

    def to_review_dict(self) -> Dict:
        """Backward-compatible dict matching the old _review_generated_code output."""
        return {
            "code": self.fixed_code,
            "fixes_applied": self.fixes_applied,
            "warnings": [
                i.message for i in self.issues
                if i.severity in (IssueSeverity.WARNING, IssueSeverity.INFO)
            ],
            "errors": [i.message for i in self.errors],
            "is_valid": self.is_valid,
            "validators_run": self.validators_run,
        }


# ── Default validator sets ────────────────────────────────────────────

CORE_VALIDATORS: List[Type[Validator]] = [
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
    PayloadFieldValidator,
    NamedTupleConstructionValidator,
]

TEST_FILE_VALIDATORS: List[Type[Validator]] = [
    TestFileValidator,
]


class ValidationPipeline:
    """
    Two-stage pipeline: auto-fix then validate.

    Usage::

        pipeline = ValidationPipeline()
        result = pipeline.validate(code, project_path="/path/to/project")

        if not result.is_valid:
            print(result.summary())
            for issue in result.errors:
                print(f"  ERROR: {issue.message}")
    """

    def __init__(
        self,
        validators: Optional[List[Validator]] = None,
        include_test_validators: bool = False,
    ):
        if validators is not None:
            self.validators = validators
        else:
            classes = list(CORE_VALIDATORS)
            if include_test_validators:
                classes.extend(TEST_FILE_VALIDATORS)
            self.validators = [v() for v in classes]

    def add_validator(self, validator: Validator) -> None:
        self.validators.append(validator)

    def remove_validator(self, validator_name: str) -> bool:
        for i, v in enumerate(self.validators):
            if v.name == validator_name:
                self.validators.pop(i)
                return True
        return False

    # ── Main entry point ──────────────────────────────────────────────

    def validate(
        self,
        code: str,
        context: Optional[Dict[str, str]] = None,
        filename: str = "",
        project_path: Optional[str] = None,
        is_test_file: bool = False,
        apply_fixes: bool = True,
    ) -> PipelineResult:
        """
        Run the full two-stage pipeline.

        Args:
            code: The P code to validate.
            context: Other project files (relative path -> content).
                     If *project_path* is given and *context* is None,
                     project files are loaded automatically.
            filename: Name of the file being validated (for messages).
            project_path: Absolute path to the P project root.
            is_test_file: If True, also run test-file-specific validators.
            apply_fixes: Whether to apply auto-fixes from both stages.
        """
        # Merge on-disk project files with any in-memory context provided
        # by the caller.  In-memory files take precedence (they may be newer
        # than what's on disk, e.g. during the preview-then-save flow).
        disk_files: Dict[str, str] = {}
        if project_path:
            disk_files = self._load_project_files(project_path)
        if context is not None:
            disk_files.update(context)
        context = disk_files

        # Remove the file being validated from context so cross-file
        # validators don't see its own declarations as duplicates.
        # The file may appear under its basename or a relative path.
        if filename:
            basename = Path(filename).name
            context = {
                k: v for k, v in context.items()
                if k != filename and Path(k).name != basename
            }

        fixes_applied: List[str] = []
        all_issues: List[ValidationIssue] = []
        validators_run: List[str] = []
        current_code = code

        # ── Stage 1: deterministic auto-fixes via PCodePostProcessor ──
        if apply_fixes:
            try:
                from ..compilation.p_post_processor import PCodePostProcessor
                processor = PCodePostProcessor()
                pp_result = processor.process(
                    current_code, filename,
                    is_test_file=is_test_file,
                )
                current_code = pp_result.code
                fixes_applied.extend(pp_result.fixes_applied)

                for w in pp_result.warnings:
                    all_issues.append(ValidationIssue(
                        severity=IssueSeverity.WARNING,
                        validator="PCodePostProcessor",
                        message=w,
                    ))
                validators_run.append("PCodePostProcessor")
            except Exception as e:
                logger.warning(f"Post-processor stage failed: {e}")
                all_issues.append(ValidationIssue(
                    severity=IssueSeverity.INFO,
                    validator="PCodePostProcessor",
                    message=f"Post-processor skipped: {e}",
                ))

        # ── Stage 2: structured validators ────────────────────────────
        for validator in self.validators:
            try:
                result = validator.validate(current_code, context)
                validators_run.append(validator.name)
                all_issues.extend(result.issues)

                if apply_fixes:
                    for issue in result.issues:
                        if issue.auto_fixable and issue.fix_function:
                            try:
                                new_code = issue.apply_fix(current_code)
                                if new_code != current_code:
                                    current_code = new_code
                                    fixes_applied.append(
                                        f"[{validator.name}] {issue.message}"
                                    )
                            except Exception:
                                pass
            except Exception as e:
                logger.warning(f"Validator {validator.name} failed: {e}")
                all_issues.append(ValidationIssue(
                    severity=IssueSeverity.INFO,
                    validator=validator.name,
                    message=f"Validator skipped: {e}",
                ))

        is_valid = not any(
            i.severity == IssueSeverity.ERROR for i in all_issues
        )

        return PipelineResult(
            is_valid=is_valid,
            original_code=code,
            fixed_code=current_code,
            issues=all_issues,
            fixes_applied=fixes_applied,
            validators_run=validators_run,
        )

    # ── Convenience methods ───────────────────────────────────────────

    def validate_file(
        self,
        file_path: str,
        context: Optional[Dict[str, str]] = None,
    ) -> PipelineResult:
        with open(file_path, "r", encoding="utf-8") as f:
            code = f.read()
        project_path = self._find_project_root(file_path)
        filename = Path(file_path).name
        is_test = "PTst" in file_path
        return self.validate(
            code,
            context=context,
            filename=filename,
            project_path=project_path,
            is_test_file=is_test,
        )

    def validate_project(
        self, project_path: str
    ) -> Dict[str, PipelineResult]:
        results: Dict[str, PipelineResult] = {}
        all_files = self._load_project_files(project_path)
        for rel_path, code in all_files.items():
            is_test = rel_path.startswith("PTst")
            results[rel_path] = self.validate(
                code,
                context=dict(all_files),
                filename=rel_path,
                project_path=project_path,
                is_test_file=is_test,
            )
        return results

    # ── Helpers ────────────────────────────────────────────────────────

    @staticmethod
    def _load_project_files(project_path: str) -> Dict[str, str]:
        files: Dict[str, str] = {}
        pp = Path(project_path)
        for p_file in pp.rglob("*.p"):
            try:
                rel = str(p_file.relative_to(pp))
                files[rel] = p_file.read_text(encoding="utf-8")
            except Exception:
                pass
        return files

    @staticmethod
    def _find_project_root(file_path: str) -> Optional[str]:
        current = Path(file_path).parent
        for _ in range(10):
            if any(current.glob("*.pproj")):
                return str(current)
            parent = current.parent
            if parent == current:
                break
            current = parent
        return None


# ── Convenience functions ─────────────────────────────────────────────

def create_default_pipeline(is_test_file: bool = False) -> ValidationPipeline:
    return ValidationPipeline(include_test_validators=is_test_file)


def validate_p_code(
    code: str,
    context: Optional[Dict[str, str]] = None,
    filename: str = "",
    project_path: Optional[str] = None,
    is_test_file: bool = False,
) -> PipelineResult:
    pipeline = create_default_pipeline(is_test_file=is_test_file)
    return pipeline.validate(
        code,
        context=context,
        filename=filename,
        project_path=project_path,
        is_test_file=is_test_file,
    )
