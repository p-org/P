"""
P Code Validators.

This module provides validators for checking generated P code quality
before compilation. Validators can identify issues and suggest fixes.
"""

import re
from abc import ABC, abstractmethod
from dataclasses import dataclass, field
from enum import Enum
from typing import Any, Dict, List, Optional, Set, Callable


class IssueSeverity(Enum):
    """Severity level of a validation issue."""
    ERROR = "error"      # Will definitely cause compilation failure
    WARNING = "warning"  # Might cause issues
    INFO = "info"        # Suggestion for improvement


@dataclass
class ValidationIssue:
    """A single validation issue found in code."""
    severity: IssueSeverity
    message: str
    line_number: Optional[int] = None
    column: Optional[int] = None
    code_snippet: Optional[str] = None
    suggestion: Optional[str] = None
    auto_fixable: bool = False
    fix_function: Optional[Callable[[str], str]] = None
    
    def apply_fix(self, code: str) -> str:
        """Apply the auto-fix if available."""
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
        """Get only error-level issues."""
        return [i for i in self.issues if i.severity == IssueSeverity.ERROR]
    
    @property
    def warnings(self) -> List[ValidationIssue]:
        """Get only warning-level issues."""
        return [i for i in self.issues if i.severity == IssueSeverity.WARNING]
    
    def merge(self, other: "ValidationResult") -> "ValidationResult":
        """Merge with another validation result."""
        return ValidationResult(
            is_valid=self.is_valid and other.is_valid,
            issues=self.issues + other.issues,
            original_code=self.original_code,
            fixed_code=other.fixed_code or self.fixed_code
        )


class Validator(ABC):
    """Base class for code validators."""
    
    name: str = "BaseValidator"
    description: str = "Base validator"
    
    @abstractmethod
    def validate(self, code: str, context: Optional[Dict[str, str]] = None) -> ValidationResult:
        """
        Validate the given code.
        
        Args:
            code: The P code to validate
            context: Optional context (other files in project)
            
        Returns:
            ValidationResult with any issues found
        """
        pass


class SyntaxValidator(Validator):
    """
    Validates basic P syntax patterns.
    
    Checks for common syntax issues that will cause compilation errors.
    """
    
    name = "SyntaxValidator"
    description = "Checks basic P syntax patterns"
    
    # Common P syntax patterns
    PATTERNS = {
        # Check for proper machine declaration
        "machine_decl": r"machine\s+\w+\s*\{",
        # Check for proper state declaration
        "state_decl": r"state\s+\w+\s*\{",
        # Check for proper event declaration
        "event_decl": r"event\s+\w+",
        # Check for proper type declaration
        "type_decl": r"type\s+\w+",
    }
    
    # Common mistakes
    MISTAKES = [
        # Missing semicolons after statements
        (r"(send\s+\w+[^;]*)\n(?!\s*//)", "Missing semicolon after send statement"),
        # Using = instead of == in conditions
        (r"if\s*\([^)]*[^=!<>]=(?!=)[^)]*\)", "Possible assignment in condition (use == for comparison)"),
        # Named tuple access without .
        (r"\w+\[\d+\]", "Array-style access on what might be a named tuple (use .field instead)"),
    ]
    
    def validate(self, code: str, context: Optional[Dict[str, str]] = None) -> ValidationResult:
        issues = []
        
        # Check for common mistakes
        for pattern, message in self.MISTAKES:
            for match in re.finditer(pattern, code):
                line_num = code[:match.start()].count('\n') + 1
                issues.append(ValidationIssue(
                    severity=IssueSeverity.WARNING,
                    message=message,
                    line_number=line_num,
                    code_snippet=match.group(0)[:50]
                ))
        
        # Check for unbalanced braces
        open_braces = code.count('{')
        close_braces = code.count('}')
        if open_braces != close_braces:
            issues.append(ValidationIssue(
                severity=IssueSeverity.ERROR,
                message=f"Unbalanced braces: {open_braces} open, {close_braces} close",
            ))
        
        # Check for unbalanced parentheses
        open_parens = code.count('(')
        close_parens = code.count(')')
        if open_parens != close_parens:
            issues.append(ValidationIssue(
                severity=IssueSeverity.ERROR,
                message=f"Unbalanced parentheses: {open_parens} open, {close_parens} close",
            ))
        
        return ValidationResult(
            is_valid=len([i for i in issues if i.severity == IssueSeverity.ERROR]) == 0,
            issues=issues,
            original_code=code
        )


class TypeDeclarationValidator(Validator):
    """
    Validates that all used types are declared.
    
    Checks that types referenced in the code are either:
    - Built-in P types (int, bool, string, etc.)
    - Declared in the same file
    - Declared in context files
    """
    
    name = "TypeDeclarationValidator"
    description = "Checks that all types are declared"
    
    # Built-in P types
    BUILTIN_TYPES = {
        "int", "bool", "string", "float", "machine", "event", "any",
        "seq", "set", "map", "tuple"
    }
    
    def validate(self, code: str, context: Optional[Dict[str, str]] = None) -> ValidationResult:
        issues = []
        context = context or {}
        
        # Find all type declarations
        declared_types = set(self.BUILTIN_TYPES)
        
        # From current code
        type_decl_pattern = r"type\s+(\w+)"
        for match in re.finditer(type_decl_pattern, code):
            declared_types.add(match.group(1))
        
        # From context files
        for filename, content in context.items():
            for match in re.finditer(type_decl_pattern, content):
                declared_types.add(match.group(1))
        
        # Find all type usages (simplified - looks for : TypeName patterns)
        type_usage_pattern = r":\s*(\w+)"
        for match in re.finditer(type_usage_pattern, code):
            type_name = match.group(1)
            if type_name not in declared_types:
                line_num = code[:match.start()].count('\n') + 1
                issues.append(ValidationIssue(
                    severity=IssueSeverity.WARNING,
                    message=f"Type '{type_name}' may not be declared",
                    line_number=line_num,
                    suggestion=f"Ensure 'type {type_name} = ...' is declared"
                ))
        
        return ValidationResult(
            is_valid=len([i for i in issues if i.severity == IssueSeverity.ERROR]) == 0,
            issues=issues,
            original_code=code
        )


class EventDeclarationValidator(Validator):
    """
    Validates that all used events are declared.
    
    Checks that events used in send/raise statements are declared.
    """
    
    name = "EventDeclarationValidator"
    description = "Checks that all events are declared"
    
    def validate(self, code: str, context: Optional[Dict[str, str]] = None) -> ValidationResult:
        issues = []
        context = context or {}
        
        # Find all event declarations
        declared_events = set()
        
        # From current code
        event_decl_pattern = r"event\s+(\w+)"
        for match in re.finditer(event_decl_pattern, code):
            declared_events.add(match.group(1))
        
        # From context files
        for filename, content in context.items():
            for match in re.finditer(event_decl_pattern, content):
                declared_events.add(match.group(1))
        
        # Find all event usages in send statements
        send_pattern = r"send\s+(\w+)\s*,"
        for match in re.finditer(send_pattern, code):
            event_name = match.group(1)
            if event_name not in declared_events:
                line_num = code[:match.start()].count('\n') + 1
                issues.append(ValidationIssue(
                    severity=IssueSeverity.WARNING,
                    message=f"Event '{event_name}' may not be declared",
                    line_number=line_num,
                    suggestion=f"Ensure 'event {event_name}' is declared"
                ))
        
        # Find event usages in raise statements
        raise_pattern = r"raise\s+(\w+)"
        for match in re.finditer(raise_pattern, code):
            event_name = match.group(1)
            if event_name not in declared_events:
                line_num = code[:match.start()].count('\n') + 1
                issues.append(ValidationIssue(
                    severity=IssueSeverity.WARNING,
                    message=f"Event '{event_name}' may not be declared",
                    line_number=line_num,
                    suggestion=f"Ensure 'event {event_name}' is declared"
                ))
        
        return ValidationResult(
            is_valid=len([i for i in issues if i.severity == IssueSeverity.ERROR]) == 0,
            issues=issues,
            original_code=code
        )


class MachineStructureValidator(Validator):
    """
    Validates P machine structure.
    
    Checks:
    - Machine has a start state
    - States have proper entry/exit handlers
    - Event handlers are properly formed
    """
    
    name = "MachineStructureValidator"
    description = "Checks machine structure validity"
    
    def validate(self, code: str, context: Optional[Dict[str, str]] = None) -> ValidationResult:
        issues = []
        
        # Check if this is a machine file
        machine_match = re.search(r"machine\s+(\w+)\s*\{", code)
        if not machine_match:
            # Not a machine file, skip validation
            return ValidationResult(is_valid=True, issues=[], original_code=code)
        
        machine_name = machine_match.group(1)
        
        # Check for start state
        if "start state" not in code:
            issues.append(ValidationIssue(
                severity=IssueSeverity.ERROR,
                message=f"Machine '{machine_name}' has no start state",
                suggestion="Add 'start state InitState { ... }'"
            ))
        
        # Check for at least one state
        state_count = len(re.findall(r"\bstate\s+\w+\s*\{", code))
        if state_count == 0:
            issues.append(ValidationIssue(
                severity=IssueSeverity.ERROR,
                message=f"Machine '{machine_name}' has no states defined",
                suggestion="Add at least one state definition"
            ))
        
        # Check for entry handlers in states
        states = re.findall(r"state\s+(\w+)\s*\{", code)
        for state in states:
            # Find the state block
            state_pattern = rf"state\s+{state}\s*\{{([^}}]*(?:\{{[^}}]*\}}[^}}]*)*)\}}"
            state_match = re.search(state_pattern, code, re.DOTALL)
            if state_match:
                state_body = state_match.group(1)
                if "entry" not in state_body and "on" not in state_body:
                    issues.append(ValidationIssue(
                        severity=IssueSeverity.INFO,
                        message=f"State '{state}' has no entry handler or event handlers",
                        suggestion="Consider adding entry { ... } or on eEvent do { ... }"
                    ))
        
        return ValidationResult(
            is_valid=len([i for i in issues if i.severity == IssueSeverity.ERROR]) == 0,
            issues=issues,
            original_code=code
        )
