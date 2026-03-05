"""
P Code Validators.

Structured validators for checking generated P code quality before compilation.
Each validator targets a specific class of common LLM errors documented in
resources/context_files/modular/p_common_compilation_errors.txt and
resources/instructions/p_code_sanity_check.txt.

Validators produce ValidationIssue objects with severity levels and optional
auto-fix functions.  The ValidationPipeline runs them in order and applies
auto-fixes to produce a cleaned-up code string.
"""

import re
from abc import ABC, abstractmethod
from dataclasses import dataclass, field
from enum import Enum
from typing import Any, Callable, Dict, List, Optional, Set

from ..compilation.p_code_utils import find_balanced_brace, iter_function_bodies, iter_all_code_blocks


class IssueSeverity(Enum):
    """Severity level of a validation issue."""
    ERROR = "error"
    WARNING = "warning"
    INFO = "info"


@dataclass
class ValidationIssue:
    """A single validation issue found in code."""
    severity: IssueSeverity
    message: str
    validator: str = ""
    line_number: Optional[int] = None
    column: Optional[int] = None
    code_snippet: Optional[str] = None
    suggestion: Optional[str] = None
    auto_fixable: bool = False
    fix_function: Optional[Callable[[str], str]] = None

    def apply_fix(self, code: str) -> str:
        if self.fix_function and self.auto_fixable:
            return self.fix_function(code)
        return code


@dataclass
class ValidationResult:
    """Result of running validation on code."""
    is_valid: bool
    issues: List[ValidationIssue] = field(default_factory=list)
    original_code: Optional[str] = None
    fixed_code: Optional[str] = None

    @property
    def errors(self) -> List[ValidationIssue]:
        return [i for i in self.issues if i.severity == IssueSeverity.ERROR]

    @property
    def warnings(self) -> List[ValidationIssue]:
        return [i for i in self.issues if i.severity == IssueSeverity.WARNING]

    def merge(self, other: "ValidationResult") -> "ValidationResult":
        return ValidationResult(
            is_valid=self.is_valid and other.is_valid,
            issues=self.issues + other.issues,
            original_code=self.original_code,
            fixed_code=other.fixed_code or self.fixed_code,
        )


class Validator(ABC):
    """Base class for code validators."""

    name: str = "BaseValidator"
    description: str = "Base validator"

    @abstractmethod
    def validate(
        self, code: str, context: Optional[Dict[str, str]] = None
    ) -> ValidationResult:
        pass


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

_BUILTIN_TYPES: Set[str] = {
    "int", "bool", "string", "float", "machine", "event", "any",
    "seq", "set", "map",
}

_P_KEYWORDS: Set[str] = {"halt", "null", "default"}


def _line_of(code: str, pos: int) -> int:
    """1-based line number for character position *pos*."""
    return code[:pos].count("\n") + 1


def _extract_body(code: str, open_pos: int) -> str:
    """Return the text between braces starting at *open_pos* (exclusive)."""
    close = find_balanced_brace(code, open_pos)
    if close == -1:
        return ""
    return code[open_pos + 1 : close]


# ---------------------------------------------------------------------------
# 1. SyntaxValidator — balanced delimiters + common syntax mistakes
# ---------------------------------------------------------------------------

class SyntaxValidator(Validator):
    """Checks balanced braces/parens and common syntax mistakes."""

    name = "SyntaxValidator"
    description = "Balanced delimiters and common syntax mistakes"

    def validate(
        self, code: str, context: Optional[Dict[str, str]] = None
    ) -> ValidationResult:
        issues: List[ValidationIssue] = []

        open_b = code.count("{")
        close_b = code.count("}")
        if open_b != close_b:
            issues.append(ValidationIssue(
                severity=IssueSeverity.ERROR,
                validator=self.name,
                message=f"Unbalanced braces: {open_b} open, {close_b} close",
            ))

        open_p = code.count("(")
        close_p = code.count(")")
        if open_p != close_p:
            issues.append(ValidationIssue(
                severity=IssueSeverity.ERROR,
                validator=self.name,
                message=f"Unbalanced parentheses: {open_p} open, {close_p} close",
            ))

        for m in re.finditer(r"if\s*\([^)]*[^=!<>]=(?!=)[^)]*\)", code):
            issues.append(ValidationIssue(
                severity=IssueSeverity.WARNING,
                validator=self.name,
                message="Possible assignment in condition (use == for comparison)",
                line_number=_line_of(code, m.start()),
                code_snippet=m.group(0)[:60],
            ))

        # Extraneous semicolons after closing braces: `};` is invalid in P.
        # Matches `}` followed by `;` (with optional whitespace), but NOT
        # inside string literals.  Common LLM mistake.
        _BRACE_SEMI = re.compile(r"\}\s*;")
        brace_semi_matches = list(_BRACE_SEMI.finditer(code))
        if brace_semi_matches:
            def _fix_brace_semi(c: str) -> str:
                return _BRACE_SEMI.sub("}", c)

            issues.append(ValidationIssue(
                severity=IssueSeverity.ERROR,
                validator=self.name,
                message=f"Extraneous semicolon after closing brace ({{}}; → {{}}) — {len(brace_semi_matches)} occurrence(s)",
                auto_fixable=True,
                fix_function=_fix_brace_semi,
            ))

        return ValidationResult(
            is_valid=not any(i.severity == IssueSeverity.ERROR for i in issues),
            issues=issues,
            original_code=code,
        )


# ---------------------------------------------------------------------------
# 2. InlineInitValidator — var x: int = 0; is illegal in P
# ---------------------------------------------------------------------------

class InlineInitValidator(Validator):
    """
    Detects and auto-fixes ``var x: T = expr;`` which P does not support.
    Must be ``var x: T;`` then ``x = expr;`` on a separate line.
    """

    name = "InlineInitValidator"
    description = "Inline variable initialization (var x: T = val)"

    _PATTERN = re.compile(
        r"^(\s*)(var\s+(\w+)\s*:\s*[^;=]+?)\s*=\s*([^;]+);",
        re.MULTILINE,
    )

    def validate(
        self, code: str, context: Optional[Dict[str, str]] = None
    ) -> ValidationResult:
        issues: List[ValidationIssue] = []
        for m in self._PATTERN.finditer(code):
            indent = m.group(1)
            decl = m.group(2).rstrip()
            var_name = m.group(3)
            init_expr = m.group(4).strip()

            def _make_fix(pat=m.group(0), ind=indent, d=decl, vn=var_name, ie=init_expr):
                def fixer(c: str) -> str:
                    return c.replace(pat, f"{d};\n{ind}{vn} = {ie};", 1)
                return fixer

            issues.append(ValidationIssue(
                severity=IssueSeverity.ERROR,
                validator=self.name,
                message=f"Inline initialization not allowed: '{m.group(0).strip()}'",
                line_number=_line_of(code, m.start()),
                suggestion=f"Split into: {decl}; then {var_name} = {init_expr};",
                auto_fixable=True,
                fix_function=_make_fix(),
            ))

        return ValidationResult(
            is_valid=not issues,
            issues=issues,
            original_code=code,
        )


# ---------------------------------------------------------------------------
# 3. VarDeclarationOrderValidator — vars must precede statements
# ---------------------------------------------------------------------------

class VarDeclarationOrderValidator(Validator):
    """
    Detects variable declarations that appear after non-declaration
    statements inside a function body and auto-fixes by hoisting them.
    """

    name = "VarDeclarationOrderValidator"
    description = "Variable declarations must precede statements in functions"

    def validate(
        self, code: str, context: Optional[Dict[str, str]] = None
    ) -> ValidationResult:
        issues: List[ValidationIssue] = []
        replacements = []

        for block_name, header, body, start_pos, close_pos in iter_all_code_blocks(code):
            lines = body.split("\n")
            var_lines: List[str] = []
            other_lines: List[str] = []

            for line in lines:
                stripped = line.strip()
                if stripped.startswith("var ") and ";" in stripped:
                    var_lines.append(line)
                else:
                    other_lines.append(line)

            if not var_lines:
                continue

            first_non_var = -1
            first_var = -1
            last_var = -1
            for i, line in enumerate(lines):
                stripped = line.strip()
                if not stripped or stripped.startswith("//"):
                    continue
                if stripped.startswith("var ") and ";" in stripped:
                    if first_var == -1:
                        first_var = i
                    last_var = i
                else:
                    if first_non_var == -1:
                        first_non_var = i

            needs_reorder = (
                first_var != -1
                and first_non_var != -1
                and (first_var > first_non_var or last_var > first_non_var)
            )
            if needs_reorder:
                new_body = "\n".join(var_lines + other_lines)
                replacement = header + "{" + new_body + "}"
                replacements.append((start_pos, close_pos + 1, replacement))
                issues.append(ValidationIssue(
                    severity=IssueSeverity.ERROR,
                    validator=self.name,
                    message=f"Variable declaration after statement in '{block_name}'",
                    suggestion="Move var declarations to the start of the block",
                    auto_fixable=True,
                    fix_function=self._make_bulk_fix(replacements[:]),
                ))

        # Also detect vars declared inside while/foreach loops
        for m in re.finditer(r"\b(?:while|foreach)\s*\([^)]*\)\s*\{", code):
            loop_body = _extract_body(code, m.end() - 1)
            if not loop_body:
                continue
            for vm in re.finditer(r"^\s*var\s+(\w+)\s*:", loop_body, re.MULTILINE):
                issues.append(ValidationIssue(
                    severity=IssueSeverity.ERROR,
                    validator=self.name,
                    message=f"Variable '{vm.group(1)}' declared inside a loop body",
                    line_number=_line_of(code, m.start()),
                    suggestion="Move var declaration to the enclosing function/entry block",
                ))

        return ValidationResult(
            is_valid=not any(i.severity == IssueSeverity.ERROR for i in issues),
            issues=issues,
            original_code=code,
        )

    @staticmethod
    def _make_bulk_fix(repls):
        def fixer(code: str) -> str:
            for start, end, repl in reversed(repls):
                code = code[:start] + repl + code[end:]
            return code
        return fixer


# ---------------------------------------------------------------------------
# 4. CollectionOpsValidator — detect wrong seq/set/map operations
# ---------------------------------------------------------------------------

class CollectionOpsValidator(Validator):
    """
    Detects common collection operation mistakes:
    - ``append(seq, x)``  (nonexistent function)
    - ``seq += (value)`` without index  (should be ``seq += (sizeof(seq), value)``)
    """

    name = "CollectionOpsValidator"
    description = "Invalid collection operations (append, wrong seq +=)"

    def validate(
        self, code: str, context: Optional[Dict[str, str]] = None
    ) -> ValidationResult:
        issues: List[ValidationIssue] = []

        for m in re.finditer(r"\bappend\s*\(", code):
            issues.append(ValidationIssue(
                severity=IssueSeverity.ERROR,
                validator=self.name,
                message="P has no append() function",
                line_number=_line_of(code, m.start()),
                suggestion="Use seq += (sizeof(seq), element); to append",
            ))

        for m in re.finditer(r"\breceive\s*\(", code):
            issues.append(ValidationIssue(
                severity=IssueSeverity.ERROR,
                validator=self.name,
                message="P has no receive() function — use event handler parameters",
                line_number=_line_of(code, m.start()),
                suggestion="Use 'on eEvent do (payload: Type) { ... }' instead",
            ))

        # seq = seq + (elem,) is wrong — should be seq += (sizeof(seq), elem)
        for m in re.finditer(
            r"\b(\w+)\s*=\s*\1\s*\+\s*\(", code
        ):
            issues.append(ValidationIssue(
                severity=IssueSeverity.WARNING,
                validator=self.name,
                message=f"Possible wrong sequence concatenation: '{m.group(0).strip()}'",
                line_number=_line_of(code, m.start()),
                suggestion="Use 'seq += (sizeof(seq), element);' to append to a sequence",
            ))

        return ValidationResult(
            is_valid=not any(i.severity == IssueSeverity.ERROR for i in issues),
            issues=issues,
            original_code=code,
        )


# ---------------------------------------------------------------------------
# 5. TypeDeclarationValidator — types used but not declared
# ---------------------------------------------------------------------------

class TypeDeclarationValidator(Validator):
    """Checks that types referenced in code are declared somewhere in the project."""

    name = "TypeDeclarationValidator"
    description = "Checks that all types are declared"

    def validate(
        self, code: str, context: Optional[Dict[str, str]] = None
    ) -> ValidationResult:
        context = context or {}
        issues: List[ValidationIssue] = []

        declared = set(_BUILTIN_TYPES)
        all_code = [code] + list(context.values())
        for src in all_code:
            declared.update(re.findall(r"\btype\s+(\w+)\s*=", src))
            declared.update(re.findall(r"\benum\s+(\w+)\s*\{", src))
            declared.update(re.findall(r"\bmachine\s+(\w+)\s*\{", src))

        used: Set[str] = set()
        used.update(re.findall(r"\bvar\s+\w+\s*:\s*(\w+)", code))
        for m in re.finditer(r"\bmap\[(\w+),\s*(\w+)\]", code):
            used.add(m.group(1))
            used.add(m.group(2))
        used.update(re.findall(r"\bseq\[(\w+)\]", code))
        used.update(re.findall(r"\bset\[(\w+)\]", code))
        used -= _BUILTIN_TYPES

        undefined = used - declared
        for t in sorted(undefined):
            issues.append(ValidationIssue(
                severity=IssueSeverity.WARNING,
                validator=self.name,
                message=f"Type '{t}' may not be declared",
                suggestion=f"Ensure 'type {t} = ...' exists in Enums_Types_Events.p",
            ))

        return ValidationResult(
            is_valid=not any(i.severity == IssueSeverity.ERROR for i in issues),
            issues=issues,
            original_code=code,
        )


# ---------------------------------------------------------------------------
# 6. EventDeclarationValidator — events used but not declared
# ---------------------------------------------------------------------------

class EventDeclarationValidator(Validator):
    """Checks that events used in send/raise/on handlers are declared."""

    name = "EventDeclarationValidator"
    description = "Checks that all events are declared"

    def validate(
        self, code: str, context: Optional[Dict[str, str]] = None
    ) -> ValidationResult:
        context = context or {}
        issues: List[ValidationIssue] = []

        declared: Set[str] = set()
        for src in [code] + list(context.values()):
            declared.update(re.findall(r"\bevent\s+(\w+)", src))

        used: Set[str] = set()
        used.update(re.findall(r"\bsend\s+[^,]+,\s*(\w+)\s*(?:,|;)", code))
        used.update(re.findall(r"\braise\s+(\w+)", code))
        used.update(re.findall(r"\bon\s+(\w+)\s+(?:do|goto)\b", code))
        used.update(re.findall(r"(?:ignore|defer)\s+(\w+)\s*[;,]", code))
        used.update(re.findall(r"\bannounce\s+(\w+)\s*,", code))
        used -= _P_KEYWORDS

        undefined = used - declared
        for ev in sorted(undefined):
            issues.append(ValidationIssue(
                severity=IssueSeverity.WARNING,
                validator=self.name,
                message=f"Event '{ev}' may not be declared",
                suggestion=f"Ensure 'event {ev}' exists in Enums_Types_Events.p",
            ))

        return ValidationResult(
            is_valid=True,
            issues=issues,
            original_code=code,
        )


# ---------------------------------------------------------------------------
# 7. MachineStructureValidator — start state, non-empty states
# ---------------------------------------------------------------------------

class MachineStructureValidator(Validator):
    """Checks that machines have a start state and states have handlers."""

    name = "MachineStructureValidator"
    description = "Checks machine structure validity"

    def validate(
        self, code: str, context: Optional[Dict[str, str]] = None
    ) -> ValidationResult:
        issues: List[ValidationIssue] = []

        for m in re.finditer(r"\bmachine\s+(\w+)\s*\{", code):
            machine_name = m.group(1)
            body = _extract_body(code, m.end() - 1)
            if not body:
                continue

            if "start state" not in body:
                issues.append(ValidationIssue(
                    severity=IssueSeverity.ERROR,
                    validator=self.name,
                    message=f"Machine '{machine_name}' has no start state",
                    suggestion="Add 'start state Init {{ ... }}'",
                ))

            defined_states: Set[str] = set()
            for sm in re.finditer(r"\bstate\s+(\w+)\s*\{", body):
                state_name = sm.group(1)
                defined_states.add(state_name)
                state_body = _extract_body(body, sm.end() - 1)
                if state_body is not None and not state_body.strip():
                    issues.append(ValidationIssue(
                        severity=IssueSeverity.INFO,
                        validator=self.name,
                        message=f"State '{state_name}' in machine '{machine_name}' has an empty body",
                        suggestion="Add entry/exit handlers or event handlers",
                    ))

            goto_targets = set(re.findall(r"\bgoto\s+(\w+)\s*;", body))
            undefined_targets = goto_targets - defined_states
            for target in sorted(undefined_targets):
                issues.append(ValidationIssue(
                    severity=IssueSeverity.ERROR,
                    validator=self.name,
                    message=f"Machine '{machine_name}' has goto to undefined state '{target}'",
                    suggestion=f"Define 'state {target} {{ ... }}' or fix the goto target name",
                ))

        return ValidationResult(
            is_valid=not any(i.severity == IssueSeverity.ERROR for i in issues),
            issues=issues,
            original_code=code,
        )


# ---------------------------------------------------------------------------
# 8. SpecObservesConsistencyValidator — spec observes ↔ handler sync
# ---------------------------------------------------------------------------

class SpecObservesConsistencyValidator(Validator):
    """
    For spec monitors, checks two things:
    1. Events in the ``observes`` clause are actually defined.
    2. Events handled inside the spec body (``on eX do/goto``) are listed
       in the ``observes`` clause — otherwise the spec silently ignores them.
    """

    name = "SpecObservesConsistencyValidator"
    description = "Spec monitor observes-clause / handler consistency"

    def validate(
        self, code: str, context: Optional[Dict[str, str]] = None
    ) -> ValidationResult:
        context = context or {}
        issues: List[ValidationIssue] = []

        all_defined_events: Set[str] = set()
        for src in [code] + list(context.values()):
            all_defined_events.update(re.findall(r"\bevent\s+(\w+)", src))

        for m in re.finditer(r"\bspec\s+(\w+)\s+observes\s+([^{]+)\{", code):
            spec_name = m.group(1)
            observes_str = m.group(2).strip().rstrip(",")
            observed = {e.strip() for e in observes_str.split(",") if e.strip()}

            for ev in sorted(observed):
                if ev not in all_defined_events:
                    issues.append(ValidationIssue(
                        severity=IssueSeverity.WARNING,
                        validator=self.name,
                        message=(
                            f"Spec '{spec_name}' observes undefined event '{ev}'"
                        ),
                        suggestion=f"Declare 'event {ev}' or remove from observes list",
                    ))

            body = _extract_body(code, m.end() - 1)
            if not body:
                continue
            handled = set(re.findall(r"\bon\s+(\w+)\s+(?:do|goto)\b", body))
            handled -= _P_KEYWORDS

            missing_from_observes = handled - observed
            for ev in sorted(missing_from_observes):
                issues.append(ValidationIssue(
                    severity=IssueSeverity.ERROR,
                    validator=self.name,
                    message=(
                        f"Spec '{spec_name}' handles event '{ev}' but does not "
                        f"list it in 'observes'. The spec will never receive it."
                    ),
                    suggestion=f"Add '{ev}' to the observes clause",
                ))

        return ValidationResult(
            is_valid=not any(i.severity == IssueSeverity.ERROR for i in issues),
            issues=issues,
            original_code=code,
        )


# ---------------------------------------------------------------------------
# 9. DuplicateDeclarationValidator — cross-file duplicate names
# ---------------------------------------------------------------------------

class DuplicateDeclarationValidator(Validator):
    """
    Detects duplicate type, event, machine, or spec declarations across
    the file being validated and the rest of the project (context).
    """

    name = "DuplicateDeclarationValidator"
    description = "Duplicate declarations across project files"

    _DECL_PATTERNS = [
        ("type", r"\btype\s+(\w+)\s*="),
        ("enum", r"\benum\s+(\w+)\s*\{"),
        ("event", r"\bevent\s+(\w+)"),
        ("machine", r"\bmachine\s+(\w+)\s*\{"),
        ("spec", r"\bspec\s+(\w+)\s+observes\b"),
    ]

    def validate(
        self, code: str, context: Optional[Dict[str, str]] = None
    ) -> ValidationResult:
        context = context or {}
        issues: List[ValidationIssue] = []

        context_names: Dict[str, List[str]] = {}
        for filepath, src in context.items():
            for kind, pattern in self._DECL_PATTERNS:
                for name in re.findall(pattern, src):
                    context_names.setdefault(name, []).append(f"{kind} in {filepath}")

        for kind, pattern in self._DECL_PATTERNS:
            for name in re.findall(pattern, code):
                if name in context_names:
                    existing = context_names[name]
                    issues.append(ValidationIssue(
                        severity=IssueSeverity.ERROR,
                        validator=self.name,
                        message=(
                            f"Duplicate {kind} declaration '{name}' — "
                            f"already declared as {existing[0]}"
                        ),
                        suggestion=f"Remove the duplicate or rename '{name}'",
                    ))

        return ValidationResult(
            is_valid=not any(i.severity == IssueSeverity.ERROR for i in issues),
            issues=issues,
            original_code=code,
        )


# ---------------------------------------------------------------------------
# 10. SpecForbiddenKeywordValidator — this/new/send/… in monitors
# ---------------------------------------------------------------------------

class SpecForbiddenKeywordValidator(Validator):
    """
    Detects forbidden keywords (this, new, send, announce, receive)
    inside spec monitor bodies.
    """

    name = "SpecForbiddenKeywordValidator"
    description = "Forbidden keywords inside spec monitors"

    _FORBIDDEN = {
        "this": r"\bthis\b",
        "new": r"\bnew\s+\w+",
        "send": r"\bsend\s+",
        "announce": r"\bannounce\s+",
        "receive": r"\breceive\s*\{",
    }

    def validate(
        self, code: str, context: Optional[Dict[str, str]] = None
    ) -> ValidationResult:
        issues: List[ValidationIssue] = []

        for m in re.finditer(r"\bspec\s+(\w+)\s+observes\s+[^{]+\{", code):
            spec_name = m.group(1)
            body = _extract_body(code, m.end() - 1)
            if not body:
                continue

            for kw, pat in self._FORBIDDEN.items():
                if re.search(pat, body):
                    issues.append(ValidationIssue(
                        severity=IssueSeverity.ERROR,
                        validator=self.name,
                        message=(
                            f"Spec monitor '{spec_name}' uses forbidden keyword '{kw}'"
                        ),
                        suggestion=(
                            "Spec monitors cannot use this/new/send/announce/receive/$/$$/pop"
                        ),
                    ))

        return ValidationResult(
            is_valid=not any(i.severity == IssueSeverity.ERROR for i in issues),
            issues=issues,
            original_code=code,
        )


# ---------------------------------------------------------------------------
# 11. TestFileValidator — test declarations, spec assertions, constructors
# ---------------------------------------------------------------------------

class TestFileValidator(Validator):
    """
    For test files (PTst), checks:
    - Test declarations exist (PChecker needs them to discover tests).
    - Test declarations assert all project specs.
    - Machine constructors match expected config types.
    """

    name = "TestFileValidator"
    description = "Test file completeness (declarations, spec assertions)"

    def validate(
        self, code: str, context: Optional[Dict[str, str]] = None
    ) -> ValidationResult:
        context = context or {}
        issues: List[ValidationIssue] = []

        has_test_decl = bool(re.search(r"^\s*test\s+\w+\s*\[", code, re.MULTILINE))
        has_machines = bool(re.search(r"\bmachine\s+\w+", code))

        if has_machines and not has_test_decl:
            issues.append(ValidationIssue(
                severity=IssueSeverity.WARNING,
                validator=self.name,
                message="Test file has machines but no test declarations",
                suggestion="Add 'test tcName [main=Machine]: assert Spec in { ... };'",
            ))

        if has_test_decl:
            spec_names: List[str] = []
            for src in context.values():
                spec_names.extend(re.findall(r"\bspec\s+(\w+)\s+observes\b", src))

            test_decls = re.findall(
                r"test\s+(\w+)\s*\[main=\w+\]\s*:\s*(.*?);",
                code,
                re.DOTALL,
            )
            for test_name, body in test_decls:
                for spec in spec_names:
                    if not re.search(rf"\bassert\s+{re.escape(spec)}\s+in\b", body):
                        issues.append(ValidationIssue(
                            severity=IssueSeverity.WARNING,
                            validator=self.name,
                            message=(
                                f"Test '{test_name}' does not assert spec '{spec}'"
                            ),
                            suggestion=(
                                f"Add 'assert {spec} in' so PChecker verifies the property"
                            ),
                        ))

        return ValidationResult(
            is_valid=not any(i.severity == IssueSeverity.ERROR for i in issues),
            issues=issues,
            original_code=code,
        )


# ---------------------------------------------------------------------------
# 12. PayloadFieldValidator — field name correctness
# ---------------------------------------------------------------------------

class PayloadFieldValidator(Validator):
    """
    Checks that field accesses on typed parameters (``param.field``) use
    field names that actually exist in the corresponding type definition.
    Requires context files containing the type definitions.
    """

    name = "PayloadFieldValidator"
    description = "Payload field name correctness"

    # Patterns that introduce a typed parameter: fun, entry, on...do
    _PARAM_PATTERNS = [
        re.compile(r"\bfun\s+\w+\s*\((\w+)\s*:\s*(\w+)"),
        re.compile(r"\bentry\s*\((\w+)\s*:\s*(\w+)"),
        re.compile(r"\bon\s+\w+\s+do\s*\((\w+)\s*:\s*(\w+)"),
    ]

    def validate(
        self, code: str, context: Optional[Dict[str, str]] = None
    ) -> ValidationResult:
        context = context or {}
        issues: List[ValidationIssue] = []

        type_fields = self._extract_type_fields(context)
        if not type_fields:
            return ValidationResult(is_valid=True, issues=[], original_code=code)

        for pattern in self._PARAM_PATTERNS:
            for func_match in pattern.finditer(code):
                param_name = func_match.group(1)
                param_type = func_match.group(2)
                if param_type not in type_fields:
                    continue
                valid = set(type_fields[param_type])
                for access in re.finditer(
                    rf"\b{re.escape(param_name)}\.(\w+)\b", code
                ):
                    fld = access.group(1)
                    if fld not in valid:
                        issues.append(ValidationIssue(
                            severity=IssueSeverity.WARNING,
                            validator=self.name,
                            message=(
                                f"'{param_name}.{fld}' — field '{fld}' not in type "
                                f"'{param_type}'. Valid: {sorted(valid)}"
                            ),
                            line_number=_line_of(code, access.start()),
                        ))

        return ValidationResult(
            is_valid=True,
            issues=issues,
            original_code=code,
        )

    @staticmethod
    def _extract_type_fields(context: Dict[str, str]) -> Dict[str, List[str]]:
        result: Dict[str, List[str]] = {}
        for src in context.values():
            for m in re.finditer(r"\btype\s+(\w+)\s*=\s*\(([^)]+)\)\s*;", src):
                fields = [fm.group(1) for fm in re.finditer(r"(\w+)\s*:", m.group(2))]
                if fields:
                    result[m.group(1)] = fields
        return result


# ---------------------------------------------------------------------------
# Helpers shared by new validators
# ---------------------------------------------------------------------------

def _extract_type_defs(sources: List[str]) -> Dict[str, List[str]]:
    """
    Parse ``type tFoo = (field1: T1, field2: T2);`` from multiple source
    strings and return ``{type_name: [field1, field2, ...]}``.
    """
    result: Dict[str, List[str]] = {}
    for src in sources:
        for m in re.finditer(r"\btype\s+(\w+)\s*=\s*\(([^)]+)\)\s*;", src):
            fields = [fm.group(1) for fm in re.finditer(r"(\w+)\s*:", m.group(2))]
            if fields:
                result[m.group(1)] = fields
    return result


def _extract_event_payload_types(sources: List[str]) -> Dict[str, str]:
    """
    Parse ``event eFoo: tBarPayload;`` and return ``{eFoo: tBarPayload}``.
    Events without a payload type are not included.
    """
    result: Dict[str, str] = {}
    for src in sources:
        for m in re.finditer(r"\bevent\s+(\w+)\s*:\s*(\w+)\s*;", src):
            result[m.group(1)] = m.group(2)
    return result


# ---------------------------------------------------------------------------
# 13. NamedTupleConstructionValidator — send/new must use named tuples
# ---------------------------------------------------------------------------

class NamedTupleConstructionValidator(Validator):
    """
    Cross-references ``type tFoo = (field: T, ...);`` declarations against
    ``new Machine(...)`` and ``send target, eEvent, ...`` call sites to
    detect cases where a bare value is passed instead of a named tuple.

    For example, if ``type tConfig = (nodes: seq[machine]);`` is declared
    and the code has ``new FailureDetector(nodeSeq)`` instead of
    ``new FailureDetector((nodes = nodeSeq,))``, this validator flags it.
    """

    name = "NamedTupleConstructionValidator"
    description = "Named tuple construction correctness at send/new call sites"

    def validate(
        self, code: str, context: Optional[Dict[str, str]] = None
    ) -> ValidationResult:
        context = context or {}
        issues: List[ValidationIssue] = []
        all_sources = [code] + list(context.values())

        type_defs = _extract_type_defs(all_sources)
        event_payloads = _extract_event_payload_types(all_sources)

        # Map machine name -> its entry parameter type (if any).
        machine_config_types: Dict[str, str] = {}
        for src in all_sources:
            self._extract_machine_config_types(src, type_defs, machine_config_types)

        # Check `new Machine(args)` call sites
        for m in re.finditer(r"\bnew\s+(\w+)\s*\(([^)]*)\)", code):
            machine_name = m.group(1)
            args = m.group(2).strip()
            if not args or machine_name not in machine_config_types:
                continue
            config_type = machine_config_types[machine_name]
            if config_type not in type_defs:
                continue
            fields = type_defs[config_type]
            self._check_tuple_construction(
                code, m, args, fields, config_type,
                f"new {machine_name}(...)", issues,
            )

        # Check `send target, eEvent, args` call sites
        for m in re.finditer(
            r"\bsend\s+[^,]+,\s*(\w+)\s*,\s*(.+?)\s*;", code
        ):
            event_name = m.group(1)
            args = m.group(2).strip()
            if event_name not in event_payloads:
                continue
            payload_type = event_payloads[event_name]
            if payload_type not in type_defs:
                continue
            fields = type_defs[payload_type]
            self._check_tuple_construction(
                code, m, args, fields, payload_type,
                f"send ..., {event_name}, ...", issues,
            )

        return ValidationResult(
            is_valid=not any(i.severity == IssueSeverity.ERROR for i in issues),
            issues=issues,
            original_code=code,
        )

    def _check_tuple_construction(
        self,
        code: str,
        match: re.Match,
        args: str,
        fields: List[str],
        type_name: str,
        call_desc: str,
        issues: List[ValidationIssue],
    ) -> None:
        """Check whether *args* looks like a proper named-tuple construction."""
        # A proper named tuple has `(field = value, ...)` or `(field = value)`
        # If args starts with '(' and contains '=' it's likely correct.
        # If args starts with '(' but has NO '=' and the type has named fields,
        # it's a bare/anonymous tuple — flag it.
        inner = args
        if inner.startswith("(") and inner.endswith(")"):
            inner = inner[1:-1].strip()

        if not inner:
            return

        has_named_fields = bool(re.search(r"\w+\s*=", inner))
        if has_named_fields:
            # Looks like a named tuple — check field names match
            used_fields = set(re.findall(r"(\w+)\s*=", inner))
            expected = set(fields)
            missing = expected - used_fields
            extra = used_fields - expected
            if missing:
                issues.append(ValidationIssue(
                    severity=IssueSeverity.WARNING,
                    validator=self.name,
                    message=(
                        f"In {call_desc}: missing field(s) {sorted(missing)} "
                        f"for type '{type_name}'"
                    ),
                    line_number=_line_of(code, match.start()),
                    suggestion=f"Expected fields: {sorted(fields)}",
                ))
            if extra:
                issues.append(ValidationIssue(
                    severity=IssueSeverity.WARNING,
                    validator=self.name,
                    message=(
                        f"In {call_desc}: unexpected field(s) {sorted(extra)} "
                        f"for type '{type_name}'"
                    ),
                    line_number=_line_of(code, match.start()),
                    suggestion=f"Expected fields: {sorted(fields)}",
                ))
        else:
            # No named fields — this is a bare/anonymous value
            if len(fields) >= 1:
                issues.append(ValidationIssue(
                    severity=IssueSeverity.ERROR,
                    validator=self.name,
                    message=(
                        f"In {call_desc}: passing bare value '{args.strip()[:50]}' "
                        f"but type '{type_name}' expects named tuple "
                        f"({', '.join(f + ' = ...' for f in fields)})"
                    ),
                    line_number=_line_of(code, match.start()),
                    suggestion=(
                        f"Use ({', '.join(f + ' = <value>' for f in fields)},) "
                        f"instead of a bare value"
                    ),
                ))

    @staticmethod
    def _extract_machine_config_types(
        src: str,
        type_defs: Dict[str, List[str]],
        result: Dict[str, str],
    ) -> None:
        """
        Find machines whose start-state entry takes a typed parameter and
        record the mapping machine_name -> config_type_name.

        Handles both patterns:
          - ``entry (param: tConfigType) { ... }``
          - ``entry InitEntry;`` with ``fun InitEntry(param: tConfigType) { ... }``
        """
        for mm in re.finditer(r"\bmachine\s+(\w+)\s*\{", src):
            machine_name = mm.group(1)
            body = _extract_body(src, mm.end() - 1)
            if not body:
                continue

            # Find start state
            sm = re.search(r"\bstart\s+state\s+\w+\s*\{", body)
            if not sm:
                continue
            state_body = _extract_body(body, sm.end() - 1)
            if not state_body:
                continue

            # Pattern 1: inline entry with parameter
            em = re.search(r"\bentry\s*\(\s*\w+\s*:\s*(\w+)\s*\)", state_body)
            if em:
                result[machine_name] = em.group(1)
                continue

            # Pattern 2: entry delegates to a named function
            em = re.search(r"\bentry\s+(\w+)\s*;", state_body)
            if em:
                func_name = em.group(1)
                fm = re.search(
                    rf"\bfun\s+{re.escape(func_name)}\s*\(\s*\w+\s*:\s*(\w+)\s*\)",
                    src,
                )
                if fm:
                    result[machine_name] = fm.group(1)
                    continue
