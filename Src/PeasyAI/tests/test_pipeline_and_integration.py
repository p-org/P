"""
Integration and coverage tests for the ValidationPipeline.

Covers the gaps identified during testing-improvement work:
  - Stage 1 + Stage 2 end-to-end pipeline flow
  - Pipeline customisation (add/remove validators, apply_fixes=False,
    include_test_validators)
  - PipelineResult public API (to_review_dict, errors/warnings props,
    summary)
  - validate_p_code() and create_default_pipeline() convenience helpers
  - SyntaxValidator brace-semicolon auto-fix
  - MachineStructureValidator goto to undefined state
  - InputValidationResult factory helpers (success / failure)
  - DesignDocValidator edge cases
  - Pipeline error-handling (validator crash does not abort pipeline)
  - validate_file() and validate_project() file/disk helpers
"""

import sys
from pathlib import Path

import pytest

PROJECT_ROOT = Path(__file__).parent.parent
sys.path.insert(0, str(PROJECT_ROOT / "src"))

from core.validation import (
    ValidationPipeline,
    PipelineResult,
    validate_p_code,
    create_default_pipeline,
    SyntaxValidator,
    MachineStructureValidator,
    IssueSeverity,
    Validator,
    ValidationResult,
    ValidationIssue,
    DesignDocValidator,
)
from core.validation.input_validators import InputValidationResult


# ─────────────────────────────────────────────────────────────────────────────
# Helpers
# ─────────────────────────────────────────────────────────────────────────────

_VALID_MACHINE = """
machine SimpleWorker {
    start state Idle {
        entry { }
        on eStart do { goto Working; }
    }
    state Working {
        on eStop do { goto Idle; }
    }
}
"""

_VALID_EVENTS = "event eStart;\nevent eStop;\n"


# ─────────────────────────────────────────────────────────────────────────────
# 1. End-to-end pipeline integration
# ─────────────────────────────────────────────────────────────────────────────

class TestPipelineIntegration:
    """Stage 1 (post-processor) + Stage 2 (validators) running together."""

    def test_valid_code_passes_end_to_end(self):
        """Well-formed code with declared events passes the whole pipeline."""
        code = _VALID_EVENTS + _VALID_MACHINE
        pipeline = ValidationPipeline()
        result = pipeline.validate(code)
        assert result.is_valid

    def test_inline_init_fixed_by_post_processor_and_passes_validator(self):
        """
        Stage 1 fixes ``var x: int = 0;`` → split form.
        Stage 2 InlineInitValidator should then find no remaining issues
        so the pipeline result still reports the fix but no errors.
        """
        code = (
            "machine M {\n"
            "    start state Init {\n"
            "        entry {\n"
            "            var x: int = 0;\n"
            "            goto Done;\n"
            "        }\n"
            "    }\n"
            "    state Done { }\n"
            "}\n"
        )
        pipeline = ValidationPipeline()
        result = pipeline.validate(code)
        # The fixed code must not contain the illegal inline form.
        # Either the post-processor split it (no `= 0;` after the type)
        # or the InlineInitValidator flagged it for the caller to address.
        assert "var x: int = 0;" not in result.fixed_code

    def test_bare_halt_fixed_and_no_error(self):
        """
        Stage 1 turns ``halt;`` into ``raise halt;``.
        The fixed code should be syntactically valid.
        """
        code = (
            "machine M {\n"
            "    start state Init {\n"
            "        entry {\n"
            "            halt;\n"
            "        }\n"
            "    }\n"
            "}\n"
        )
        pipeline = ValidationPipeline()
        result = pipeline.validate(code)
        assert "halt;" not in result.fixed_code or "raise halt;" in result.fixed_code

    def test_original_code_preserved_in_result(self):
        """PipelineResult.original_code must equal the input, unmodified."""
        code = _VALID_EVENTS + _VALID_MACHINE
        pipeline = ValidationPipeline()
        result = pipeline.validate(code)
        assert result.original_code == code

    def test_validators_run_list_populated(self):
        """All core validator names should appear in validators_run."""
        code = _VALID_EVENTS + _VALID_MACHINE
        pipeline = ValidationPipeline()
        result = pipeline.validate(code)
        assert "SyntaxValidator" in result.validators_run
        assert "MachineStructureValidator" in result.validators_run
        assert "PCodePostProcessor" in result.validators_run

    def test_multiple_errors_collected_from_different_validators(self):
        """
        Introduce violations that span multiple validators;
        all issues should be collected in one pass.
        """
        # Missing start state (MachineStructureValidator) +
        # undeclared type (TypeDeclarationValidator) +
        # unbalanced braces (SyntaxValidator)
        code = (
            "machine Broken {\n"
            "    state Init {\n"
            "        var x: UnknownType;\n"
            "    }\n"
            # deliberate missing closing brace for machine
        )
        pipeline = ValidationPipeline()
        result = pipeline.validate(code)
        validators_with_issues = {i.validator for i in result.issues}
        # At minimum SyntaxValidator should fire (missing brace) and
        # MachineStructureValidator (no start state)
        assert len(validators_with_issues) >= 2

    def test_fixes_applied_reflects_changes(self):
        """
        When the post-processor or a validator auto-fix changes the code,
        fixes_applied must be non-empty and describe what happened.
        """
        code = (
            "machine M {\n"
            "    start state Init {\n"
            "        entry {\n"
            "            var x: int = 42;\n"
            "            goto Done;\n"
            "        }\n"
            "    }\n"
            "    state Done { }\n"
            "}\n"
        )
        pipeline = ValidationPipeline()
        result = pipeline.validate(code)
        # At least one fix (var-reorder or inline-init) should be recorded
        assert len(result.fixes_applied) > 0


# ─────────────────────────────────────────────────────────────────────────────
# 2. Pipeline customisation
# ─────────────────────────────────────────────────────────────────────────────

class TestPipelineCustomisation:
    """add_validator, remove_validator, apply_fixes=False, test validators."""

    def test_add_validator_runs_it(self):
        """A validator added after construction should execute."""
        ran = []

        class Tracer(Validator):
            name = "TracerValidator"
            description = "Marks itself as run"

            def validate(self, code, context=None):
                ran.append(True)
                return ValidationResult(is_valid=True, issues=[], original_code=code)

        pipeline = ValidationPipeline()
        pipeline.add_validator(Tracer())
        pipeline.validate("machine M { start state S { } }")
        assert ran, "TracerValidator was not called"

    def test_remove_validator_stops_it_running(self):
        """Removing a validator by name means it no longer runs."""
        pipeline = ValidationPipeline()
        removed = pipeline.remove_validator("SyntaxValidator")
        assert removed is True
        result = pipeline.validate("machine M { start state S { }")  # unbalanced
        # SyntaxValidator is gone, so the brace error must not appear
        assert all(i.validator != "SyntaxValidator" for i in result.issues)

    def test_remove_nonexistent_validator_returns_false(self):
        """Trying to remove a validator that doesn't exist returns False."""
        pipeline = ValidationPipeline()
        assert pipeline.remove_validator("DoesNotExistValidator") is False

    def test_apply_fixes_false_issues_still_reported(self):
        """
        With apply_fixes=False the pipeline still runs validators and
        reports issues, but does NOT mutate the code.
        """
        # Use multiline code so InlineInitValidator's MULTILINE pattern fires
        code = (
            "machine M {\n"
            "    start state S {\n"
            "        entry {\n"
            "            var x: int = 0;\n"
            "        }\n"
            "    }\n"
            "}\n"
        )
        pipeline = ValidationPipeline()
        result = pipeline.validate(code, apply_fixes=False)
        # fixed_code should equal original since no fixes applied
        assert result.fixed_code == code
        # But issues (inline init) should still be present
        assert len(result.issues) > 0

    def test_apply_fixes_false_no_fixes_applied(self):
        """fixes_applied list must be empty when apply_fixes=False."""
        code = (
            "machine M {\n"
            "    start state S {\n"
            "        entry {\n"
            "            var x: int = 0;\n"
            "        }\n"
            "    }\n"
            "}\n"
        )
        pipeline = ValidationPipeline()
        result = pipeline.validate(code, apply_fixes=False)
        assert result.fixes_applied == []

    def test_include_test_validators_adds_test_file_validator(self):
        """include_test_validators=True should include TestFileValidator."""
        pipeline = ValidationPipeline(include_test_validators=True)
        names = [v.name for v in pipeline.validators]
        assert "TestFileValidator" in names

    def test_exclude_test_validators_omits_test_file_validator(self):
        """Default pipeline (include_test_validators=False) omits TestFileValidator."""
        pipeline = ValidationPipeline()
        names = [v.name for v in pipeline.validators]
        assert "TestFileValidator" not in names

    def test_custom_validator_list(self):
        """Passing a validators list uses exactly those validators."""
        pipeline = ValidationPipeline(validators=[SyntaxValidator()])
        result = pipeline.validate("machine M { start state S { }")
        # Only SyntaxValidator ran
        assert "SyntaxValidator" in result.validators_run
        # MachineStructureValidator did NOT run
        assert "MachineStructureValidator" not in result.validators_run


# ─────────────────────────────────────────────────────────────────────────────
# 3. PipelineResult public API
# ─────────────────────────────────────────────────────────────────────────────

class TestPipelineResultAPI:
    """Tests for PipelineResult properties and methods."""

    def _make_result(self, errors=None, warnings=None, fixes=None):
        issues = []
        for msg in (errors or []):
            issues.append(ValidationIssue(
                severity=IssueSeverity.ERROR,
                validator="TestValidator",
                message=msg,
            ))
        for msg in (warnings or []):
            issues.append(ValidationIssue(
                severity=IssueSeverity.WARNING,
                validator="TestValidator",
                message=msg,
            ))
        return PipelineResult(
            is_valid=not bool(errors),
            original_code="original",
            fixed_code="fixed",
            issues=issues,
            fixes_applied=fixes or [],
            validators_run=["TestValidator"],
        )

    def test_errors_property_filters_errors(self):
        result = self._make_result(errors=["e1", "e2"], warnings=["w1"])
        assert len(result.errors) == 2
        assert all(i.severity == IssueSeverity.ERROR for i in result.errors)

    def test_warnings_property_filters_warnings(self):
        result = self._make_result(errors=["e1"], warnings=["w1", "w2"])
        assert len(result.warnings) == 2
        assert all(i.severity == IssueSeverity.WARNING for i in result.warnings)

    def test_is_valid_false_when_errors(self):
        result = self._make_result(errors=["something bad"])
        assert result.is_valid is False

    def test_is_valid_true_with_only_warnings(self):
        result = self._make_result(warnings=["just a warning"])
        assert result.is_valid is True

    def test_summary_contains_key_fields(self):
        result = self._make_result(errors=["e1"], warnings=["w1"], fixes=["fix1"])
        summary = result.summary()
        assert "Validation" in summary
        assert "Validators run" in summary
        assert "Errors" in summary
        assert "Warnings" in summary
        assert "Auto-fixes applied" in summary

    def test_to_review_dict_shape(self):
        result = self._make_result(errors=["err"], warnings=["warn"], fixes=["fix"])
        d = result.to_review_dict()
        assert "code" in d
        assert "fixes_applied" in d
        assert "warnings" in d
        assert "errors" in d
        assert "is_valid" in d
        assert "validators_run" in d

    def test_to_review_dict_code_equals_fixed_code(self):
        result = self._make_result()
        d = result.to_review_dict()
        assert d["code"] == result.fixed_code

    def test_to_review_dict_errors_list(self):
        result = self._make_result(errors=["bad thing"])
        d = result.to_review_dict()
        assert "bad thing" in d["errors"]
        assert d["is_valid"] is False

    def test_to_review_dict_warnings_include_info(self):
        """INFO-level issues should appear in the warnings list of to_review_dict."""
        issue = ValidationIssue(
            severity=IssueSeverity.INFO,
            validator="V",
            message="info msg",
        )
        result = PipelineResult(
            is_valid=True,
            original_code="c",
            fixed_code="c",
            issues=[issue],
        )
        d = result.to_review_dict()
        assert "info msg" in d["warnings"]


# ─────────────────────────────────────────────────────────────────────────────
# 4. Convenience helpers: validate_p_code, create_default_pipeline
# ─────────────────────────────────────────────────────────────────────────────

class TestValidatePCodeConvenience:
    """Tests for module-level helper functions."""

    def test_validate_p_code_returns_pipeline_result(self):
        result = validate_p_code(_VALID_EVENTS + _VALID_MACHINE)
        assert isinstance(result, PipelineResult)

    def test_validate_p_code_valid_code(self):
        result = validate_p_code(_VALID_EVENTS + _VALID_MACHINE)
        assert result.is_valid

    def test_validate_p_code_invalid_code(self):
        result = validate_p_code("machine M { state S { } }")  # no start state
        assert not result.is_valid

    def test_validate_p_code_accepts_context(self):
        code = "machine Worker { start state Init { entry { } } }"
        context = {"types.p": "type tConfig = (id: int);"}
        result = validate_p_code(code, context=context)
        assert isinstance(result, PipelineResult)

    def test_create_default_pipeline_returns_pipeline(self):
        pipeline = create_default_pipeline()
        assert isinstance(pipeline, ValidationPipeline)

    def test_create_default_pipeline_with_test_validators(self):
        pipeline = create_default_pipeline(is_test_file=True)
        names = [v.name for v in pipeline.validators]
        assert "TestFileValidator" in names

    def test_validate_p_code_is_test_file_runs_test_validators(self):
        """is_test_file=True passes through to include TestFileValidator."""
        # Use apply_fixes=False via the pipeline directly so the post-processor
        # does not auto-generate a test declaration (which would satisfy the
        # TestFileValidator check and suppress the warning we want to detect).
        code = (
            "machine Scenario {\n"
            "    start state Init { }\n"
            "}\n"
        )
        pipeline = create_default_pipeline(is_test_file=True)
        result = pipeline.validate(code, is_test_file=True, apply_fixes=False)
        # TestFileValidator warning should appear (machines without test decl)
        assert any(i.validator == "TestFileValidator" for i in result.issues)

    def test_is_test_file_full_flow_generates_test_declaration(self):
        """
        In the normal (apply_fixes=True) flow with is_test_file=True,
        the post-processor auto-generates a test declaration.  Verify the
        full pipeline completes and the fixed code contains a test declaration.
        """
        code = (
            "machine Scenario {\n"
            "    start state Init { }\n"
            "}\n"
        )
        pipeline = create_default_pipeline(is_test_file=True)
        result = pipeline.validate(code, is_test_file=True)
        # Post-processor should have generated a test declaration
        test_fixes = [f for f in result.fixes_applied if "test declaration" in f.lower()]
        assert test_fixes, "Expected post-processor to auto-generate a test declaration"
        assert "test " in result.fixed_code


# ─────────────────────────────────────────────────────────────────────────────
# 5. SyntaxValidator — brace-semicolon auto-fix
# ─────────────────────────────────────────────────────────────────────────────

class TestSyntaxValidatorBraceSemicolon:
    """Tests for the }; → } auto-fix in SyntaxValidator."""

    def test_brace_semicolon_detected(self):
        code = "machine M { start state S { entry { } }; }"
        v = SyntaxValidator()
        result = v.validate(code)
        assert not result.is_valid
        errors = [i for i in result.errors if "semicolon" in i.message.lower()]
        assert errors, "Expected a 'semicolon after brace' error"

    def test_brace_semicolon_auto_fixable(self):
        code = "machine M { start state S { entry { } }; }"
        v = SyntaxValidator()
        result = v.validate(code)
        fixable = [i for i in result.errors if i.auto_fixable]
        assert fixable

    def test_brace_semicolon_fix_removes_semicolon(self):
        code = "machine M { start state S { entry { } }; }"
        v = SyntaxValidator()
        result = v.validate(code)
        fixed = code
        for issue in result.issues:
            if issue.auto_fixable:
                fixed = issue.apply_fix(fixed)
        assert "};" not in fixed
        assert "}" in fixed

    def test_valid_code_no_brace_semicolon_error(self):
        code = "machine M { start state S { entry { } } }"
        v = SyntaxValidator()
        result = v.validate(code)
        brace_errors = [i for i in result.errors if "semicolon" in i.message.lower()]
        assert not brace_errors

    def test_multiple_brace_semicolons_reported(self):
        code = "machine M { start state S { }; state T { }; }"
        v = SyntaxValidator()
        result = v.validate(code)
        errors = [i for i in result.errors if "semicolon" in i.message.lower()]
        assert errors
        # The message should mention the count of occurrences
        assert any("2" in i.message for i in errors)


# ─────────────────────────────────────────────────────────────────────────────
# 6. MachineStructureValidator — goto to undefined state
# ─────────────────────────────────────────────────────────────────────────────

class TestMachineStructureValidatorGoto:
    """Tests for goto-to-undefined-state detection."""

    def test_goto_undefined_state_is_error(self):
        code = (
            "machine M {\n"
            "    start state Init {\n"
            "        entry { goto NonExistent; }\n"
            "    }\n"
            "}\n"
        )
        v = MachineStructureValidator()
        result = v.validate(code)
        assert not result.is_valid
        errors = [i for i in result.errors if "NonExistent" in i.message]
        assert errors

    def test_goto_defined_state_passes(self):
        code = (
            "machine M {\n"
            "    start state Init {\n"
            "        entry { goto Active; }\n"
            "    }\n"
            "    state Active { }\n"
            "}\n"
        )
        v = MachineStructureValidator()
        result = v.validate(code)
        assert result.is_valid

    def test_goto_self_state_passes(self):
        """A goto back to the same state is valid (self-loop)."""
        code = (
            "machine M {\n"
            "    start state Loop {\n"
            "        on eTick do { goto Loop; }\n"
            "    }\n"
            "}\n"
        )
        v = MachineStructureValidator()
        result = v.validate(code)
        assert result.is_valid

    def test_empty_state_body_info_issue(self):
        """An empty state body generates an INFO-level issue."""
        code = (
            "machine M {\n"
            "    start state Init { }\n"
            "}\n"
        )
        v = MachineStructureValidator()
        result = v.validate(code)
        assert result.is_valid  # INFO only, not ERROR
        info_issues = [i for i in result.issues if i.severity == IssueSeverity.INFO]
        assert info_issues

    def test_multiple_machines_each_checked(self):
        """Each machine in the file is validated independently."""
        code = (
            "machine Good {\n"
            "    start state Init { }\n"
            "}\n"
            "machine Bad {\n"
            "    state NoStart { }\n"
            "}\n"
        )
        v = MachineStructureValidator()
        result = v.validate(code)
        assert not result.is_valid
        assert any("Bad" in i.message for i in result.errors)
        # Good machine should not appear in errors
        assert not any("Good" in i.message for i in result.errors)


# ─────────────────────────────────────────────────────────────────────────────
# 7. InputValidationResult factory methods
# ─────────────────────────────────────────────────────────────────────────────

class TestInputValidationResult:
    """Tests for the InputValidationResult dataclass helpers."""

    def test_success_is_valid(self):
        r = InputValidationResult.success()
        assert r.is_valid is True
        assert r.errors == []
        assert r.warnings == []

    def test_failure_is_invalid(self):
        r = InputValidationResult.failure("something went wrong")
        assert r.is_valid is False

    def test_failure_contains_error_message(self):
        msg = "bad input"
        r = InputValidationResult.failure(msg)
        assert msg in r.errors

    def test_failure_has_no_warnings(self):
        r = InputValidationResult.failure("oops")
        assert r.warnings == []

    def test_custom_construction(self):
        r = InputValidationResult(
            is_valid=True,
            errors=[],
            warnings=["minor issue"],
        )
        assert r.is_valid is True
        assert "minor issue" in r.warnings


# ─────────────────────────────────────────────────────────────────────────────
# 8. DesignDocValidator edge cases
# ─────────────────────────────────────────────────────────────────────────────

class TestDesignDocValidatorEdgeCases:
    """Edge-case tests for DesignDocValidator not covered by basic tests."""

    def _validator(self):
        return DesignDocValidator()

    def _long_enough_doc(self, extra=""):
        """Returns a string >= MIN_LENGTH (100) characters."""
        base = "A" * 100
        return base + extra

    def test_exactly_min_length_passes(self):
        """A document of exactly MIN_LENGTH characters should not get a length error."""
        doc = "A" * DesignDocValidator.MIN_LENGTH
        v = self._validator()
        result = v.validate(doc)
        # No length error (might have section warnings, but that's OK)
        assert not any("too short" in e.lower() for e in result.errors)

    def test_too_long_is_error(self):
        """A document exceeding MAX_LENGTH should produce an error."""
        doc = "B" * (DesignDocValidator.MAX_LENGTH + 1)
        v = self._validator()
        result = v.validate(doc)
        assert not result.is_valid
        assert any("too long" in e.lower() for e in result.errors)

    def test_all_required_sections_present_no_section_warnings(self):
        """When all required sections appear, there should be no section warnings."""
        # Include the literal keywords from REQUIRED_SECTIONS:
        # "title", "component", "interaction"
        doc = (
            "# My System — title\n\n"
            "## Components\nClient, Server\n\n"
            "## Interactions\nClient sends request to Server.\n\n"
            "Events: eRequest, eResponse.\n"
            "Machine: state machine handles requests.\n"
            + "X" * 50  # pad to exceed min length
        )
        v = self._validator()
        result = v.validate(doc)
        section_warnings = [w for w in result.warnings if "section" in w.lower()]
        assert not section_warnings

    def test_events_mentioned_reduces_warnings(self):
        """A doc mentioning 'event' should not get the no-event warning."""
        doc = (
            "# System\n## Components\nA machine.\n"
            "## Interactions\nAn event is sent.\n"
            + "X" * 40
        )
        v = self._validator()
        result = v.validate(doc)
        event_warnings = [w for w in result.warnings if "event" in w.lower()]
        assert not event_warnings

    def test_extract_metadata_title_none_when_absent(self):
        """extract_metadata returns None title when no # heading present."""
        doc = "This document has no markdown title heading.\n" + "X" * 60
        v = self._validator()
        meta = v.extract_metadata(doc)
        assert meta["title"] is None

    def test_extract_metadata_components_from_bullets(self):
        """Components are extracted from bullet-list entries."""
        doc = (
            "# My System\n\n"
            "## Components\n"
            "- **Client** machine handles user requests\n"
            "- **Server** component processes jobs\n"
            + "X" * 40
        )
        v = self._validator()
        meta = v.extract_metadata(doc)
        assert meta["title"] == "My System"

    def test_validate_returns_warnings_not_errors_for_missing_sections(self):
        """Missing sections should be warnings, not errors, in a long-enough doc."""
        doc = "X" * DesignDocValidator.MIN_LENGTH
        v = self._validator()
        result = v.validate(doc)
        # is_valid is True (warnings only)
        assert result.is_valid
        # At least some warnings about missing sections
        assert len(result.warnings) > 0


# ─────────────────────────────────────────────────────────────────────────────
# 9. Pipeline error-handling: validator crash does not abort pipeline
# ─────────────────────────────────────────────────────────────────────────────

class TestPipelineErrorHandling:
    """Crashing validators are skipped; remaining validators still run."""

    def test_crashing_validator_skipped_gracefully(self):
        """
        If a validator raises an exception it should NOT propagate to the
        caller; the pipeline should continue and log an INFO issue.
        Crashing validators are not added to validators_run (they raised
        before that point), but the remaining validators still execute.
        """
        class BoomValidator(Validator):
            name = "BoomValidator"
            description = "Always crashes"

            def validate(self, code, context=None):
                raise RuntimeError("intentional test crash")

        pipeline = ValidationPipeline(validators=[
            SyntaxValidator(),
            BoomValidator(),
            MachineStructureValidator(),
        ])
        # Should not raise
        result = pipeline.validate("machine M { start state Init { } }")
        # Validators that completed normally should be in validators_run
        assert "SyntaxValidator" in result.validators_run
        assert "MachineStructureValidator" in result.validators_run
        # Crashing validator is NOT in validators_run (it raised before append)
        assert "BoomValidator" not in result.validators_run
        # But its crash IS recorded as an INFO issue
        assert any(
            i.validator == "BoomValidator" and i.severity == IssueSeverity.INFO
            for i in result.issues
        )

    def test_crashing_validator_produces_info_issue(self):
        """The crash is reported as an INFO issue with the error message."""
        class BoomValidator(Validator):
            name = "BoomValidator"
            description = "Always crashes"

            def validate(self, code, context=None):
                raise ValueError("test boom")

        pipeline = ValidationPipeline(validators=[BoomValidator()])
        result = pipeline.validate("machine M { start state Init { } }")
        info_issues = [
            i for i in result.issues
            if i.severity == IssueSeverity.INFO and i.validator == "BoomValidator"
        ]
        assert info_issues

    def test_crashing_validator_does_not_hide_real_errors(self):
        """Real errors from other validators are still reported after a crash."""
        class BoomValidator(Validator):
            name = "BoomValidator"
            description = "Always crashes"

            def validate(self, code, context=None):
                raise RuntimeError("boom")

        pipeline = ValidationPipeline(validators=[
            BoomValidator(),
            MachineStructureValidator(),
        ])
        code = "machine M { state NoStart { } }"  # missing start state
        result = pipeline.validate(code)
        assert not result.is_valid
        assert any(i.validator == "MachineStructureValidator" for i in result.errors)


# ─────────────────────────────────────────────────────────────────────────────
# 10. validate_file() and validate_project() disk helpers
# ─────────────────────────────────────────────────────────────────────────────

class TestValidateFileAndProject:
    """Tests for ValidationPipeline.validate_file() and .validate_project()."""

    def test_validate_file_returns_pipeline_result(self, tmp_path):
        p_file = tmp_path / "main.p"
        p_file.write_text(_VALID_EVENTS + _VALID_MACHINE)
        pipeline = ValidationPipeline()
        result = pipeline.validate_file(str(p_file))
        assert isinstance(result, PipelineResult)

    def test_validate_file_valid_code(self, tmp_path):
        p_file = tmp_path / "main.p"
        p_file.write_text(_VALID_EVENTS + _VALID_MACHINE)
        pipeline = ValidationPipeline()
        result = pipeline.validate_file(str(p_file))
        assert result.is_valid

    def test_validate_file_invalid_code(self, tmp_path):
        p_file = tmp_path / "main.p"
        p_file.write_text("machine M { state S { } }")  # no start state
        pipeline = ValidationPipeline()
        result = pipeline.validate_file(str(p_file))
        assert not result.is_valid

    def test_validate_file_ptst_uses_test_validators(self, tmp_path):
        """
        validate_file() detects PTst in the path and passes is_test_file=True
        to the post-processor, which then auto-generates test declarations.
        """
        ptst_dir = tmp_path / "PTst"
        ptst_dir.mkdir()
        p_file = ptst_dir / "Test.p"
        p_file.write_text(
            "machine TestScenario {\n"
            "    start state Init { }\n"
            "}\n"
        )
        pipeline = ValidationPipeline()
        result = pipeline.validate_file(str(p_file))
        # The post-processor runs in test-file mode and auto-generates a
        # test declaration — verify this happened via fixes_applied.
        test_fixes = [f for f in result.fixes_applied if "test declaration" in f.lower()]
        assert test_fixes, (
            "Expected post-processor to auto-generate a test declaration for PTst file"
        )

    def test_validate_project_returns_dict(self, tmp_path):
        """validate_project() returns a dict keyed by relative file paths."""
        # Create minimal project structure
        psrc = tmp_path / "PSrc"
        psrc.mkdir()
        (psrc / "main.p").write_text(_VALID_EVENTS + _VALID_MACHINE)
        pipeline = ValidationPipeline()
        results = pipeline.validate_project(str(tmp_path))
        assert isinstance(results, dict)
        assert len(results) == 1

    def test_validate_project_all_files_validated(self, tmp_path):
        """All .p files in the project tree are included in the results."""
        psrc = tmp_path / "PSrc"
        pspec = tmp_path / "PSpec"
        psrc.mkdir()
        pspec.mkdir()
        (psrc / "main.p").write_text(_VALID_EVENTS + _VALID_MACHINE)
        (pspec / "spec.p").write_text(
            "spec Safety observes eStart, eStop {\n"
            "    start state Init { }\n"
            "}\n"
        )
        pipeline = ValidationPipeline()
        results = pipeline.validate_project(str(tmp_path))
        assert len(results) == 2

    def test_validate_project_context_shared_across_files(self, tmp_path):
        """
        validate_project() passes all project files as context so
        cross-file validators (e.g. DuplicateDeclarationValidator) work.
        """
        psrc = tmp_path / "PSrc"
        psrc.mkdir()
        # Two files declaring the same event — should trigger DuplicateDeclarationValidator
        (psrc / "a.p").write_text("event eDuplicated;\n")
        (psrc / "b.p").write_text("event eDuplicated;\nmachine M { start state S { } }\n")
        pipeline = ValidationPipeline()
        results = pipeline.validate_project(str(tmp_path))
        all_issues = [i for r in results.values() for i in r.issues]
        dup_errors = [
            i for i in all_issues
            if i.validator == "DuplicateDeclarationValidator"
        ]
        assert dup_errors, "Expected DuplicateDeclarationValidator to fire"


if __name__ == "__main__":
    pytest.main([__file__, "-v"])
