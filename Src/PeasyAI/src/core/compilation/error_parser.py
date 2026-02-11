"""
P Compiler Error Parser

Parses P compiler output into structured error objects for easier handling.
"""

import re
import logging
from typing import List, Optional, Dict, Any
from dataclasses import dataclass, field
from enum import Enum

logger = logging.getLogger(__name__)


class ErrorType(Enum):
    """Types of P compiler errors"""
    PARSE = "parse"
    TYPE = "type"
    SEMANTIC = "semantic"
    INTERNAL = "internal"
    UNKNOWN = "unknown"


class ErrorCategory(Enum):
    """Common error categories for pattern matching"""
    VAR_DECLARATION_ORDER = "var_declaration_order"
    MISSING_SEMICOLON = "missing_semicolon"
    UNDEFINED_EVENT = "undefined_event"
    UNDEFINED_TYPE = "undefined_type"
    UNDEFINED_VARIABLE = "undefined_variable"
    FOREACH_ITERATOR = "foreach_iterator"
    TYPE_MISMATCH = "type_mismatch"
    DUPLICATE_DECLARATION = "duplicate_declaration"
    UNHANDLED_EVENT = "unhandled_event"
    NULL_TARGET = "null_target"
    INVALID_CHARACTER = "invalid_character"
    UNBALANCED_BRACES = "unbalanced_braces"
    UNKNOWN = "unknown"


@dataclass
class PCompilerError:
    """Structured representation of a P compiler error"""
    file: str
    line: int
    column: int
    error_type: ErrorType
    category: ErrorCategory
    message: str
    raw_message: str
    suggestion: Optional[str] = None
    context_lines: List[str] = field(default_factory=list)
    
    def to_dict(self) -> Dict[str, Any]:
        return {
            "file": self.file,
            "line": self.line,
            "column": self.column,
            "error_type": self.error_type.value,
            "category": self.category.value,
            "message": self.message,
            "suggestion": self.suggestion,
        }


class PCompilerErrorParser:
    """Parses P compiler output into structured errors."""
    
    # Patterns for different error formats
    PATTERNS = [
        # [filename.p] parse error: line X:Y message
        (r'\[([^\]]+\.p)\]\s*parse error:\s*line\s*(\d+):(\d+)\s*(.+)', ErrorType.PARSE),
        # [filename.p] error: line X:Y message
        (r'\[([^\]]+\.p)\]\s*error:\s*line\s*(\d+):(\d+)\s*(.+)', ErrorType.TYPE),
        # [Error:] [filename.p:line:col] message
        (r'\[Error:\]\s*\[([^\]:]+\.p):(\d+):(\d+)\]\s*(.+)', ErrorType.SEMANTIC),
        # [Parser Error:] [filename.p] parse error: line X:Y message
        (r'\[Parser Error:\]\s*\[([^\]]+\.p)\]\s*parse error:\s*line\s*(\d+):(\d+)\s*(.+)', ErrorType.PARSE),
        # Generic error pattern
        (r'\[([^\]]+\.p)\].*?line\s*(\d+):(\d+)\s*(.+)', ErrorType.UNKNOWN),
    ]
    
    # Category detection patterns
    CATEGORY_PATTERNS = {
        ErrorCategory.VAR_DECLARATION_ORDER: [
            r"extraneous input 'var'",
            r"var.*expecting.*announce",
        ],
        ErrorCategory.FOREACH_ITERATOR: [
            r"could not find foreach iterator",
            r"foreach.*iterator.*variable",
        ],
        ErrorCategory.TYPE_MISMATCH: [
            r"got type:.*expected:",
            r"type mismatch",
            r"cannot convert",
        ],
        ErrorCategory.UNDEFINED_EVENT: [
            r"event.*not found",
            r"undefined event",
            r"could not find event",
        ],
        ErrorCategory.UNDEFINED_TYPE: [
            r"type.*not found",
            r"undefined type",
            r"could not find type",
        ],
        ErrorCategory.UNDEFINED_VARIABLE: [
            r"variable.*not found",
            r"undefined variable",
            r"could not find.*variable",
        ],
        ErrorCategory.DUPLICATE_DECLARATION: [
            r"duplicates declaration",
            r"already declared",
            r"duplicate definition",
        ],
        ErrorCategory.UNHANDLED_EVENT: [
            r"cannot be handled",
            r"unhandled event",
            r"no handler for",
        ],
        ErrorCategory.NULL_TARGET: [
            r"target.*cannot be null",
            r"null target",
            r"send.*null",
        ],
        ErrorCategory.INVALID_CHARACTER: [
            r"token recognition error",
            r"invalid character",
            r"unexpected character",
        ],
        ErrorCategory.MISSING_SEMICOLON: [
            r"missing.*semicolon",
            r"expecting.*';'",
        ],
        ErrorCategory.UNBALANCED_BRACES: [
            r"missing.*'}'",
            r"unmatched.*brace",
            r"expecting.*'}'",
        ],
    }
    
    # Suggestions for each category
    SUGGESTIONS = {
        ErrorCategory.VAR_DECLARATION_ORDER: 
            "Move variable declarations to the start of the function/block, before any statements.",
        ErrorCategory.FOREACH_ITERATOR:
            "Declare the foreach iterator variable before the loop: 'var iterName: Type;'",
        ErrorCategory.TYPE_MISMATCH:
            "Check that the types match. You may need to cast or convert the value.",
        ErrorCategory.UNDEFINED_EVENT:
            "Ensure the event is declared in the types/events file with 'event eEventName: PayloadType;'",
        ErrorCategory.UNDEFINED_TYPE:
            "Ensure the type is declared with 'type TypeName = ...;'",
        ErrorCategory.UNDEFINED_VARIABLE:
            "Declare the variable before using it with 'var varName: Type;'",
        ErrorCategory.DUPLICATE_DECLARATION:
            "Remove the duplicate declaration or rename one of them.",
        ErrorCategory.UNHANDLED_EVENT:
            "Add a handler for the event with 'on eEvent do Handler;' or 'ignore eEvent;'",
        ErrorCategory.NULL_TARGET:
            "Ensure the target machine is initialized before sending. Add configuration events to wire machines together.",
        ErrorCategory.INVALID_CHARACTER:
            "Remove invalid characters (like markdown code fences ```) from the file.",
        ErrorCategory.MISSING_SEMICOLON:
            "Add a semicolon at the end of the statement.",
        ErrorCategory.UNBALANCED_BRACES:
            "Check for missing or extra braces. Each '{' needs a matching '}'.",
    }
    
    @classmethod
    def parse(cls, compiler_output: str) -> List[PCompilerError]:
        """Parse compiler output into structured errors."""
        errors = []
        
        for pattern, error_type in cls.PATTERNS:
            for match in re.finditer(pattern, compiler_output, re.IGNORECASE | re.MULTILINE):
                file_name = match.group(1)
                line = int(match.group(2))
                column = int(match.group(3))
                message = match.group(4).strip()
                
                # Determine category
                category = cls._categorize_error(message)
                
                # Get suggestion
                suggestion = cls.SUGGESTIONS.get(category)
                
                error = PCompilerError(
                    file=file_name,
                    line=line,
                    column=column,
                    error_type=error_type,
                    category=category,
                    message=message,
                    raw_message=match.group(0),
                    suggestion=suggestion,
                )
                
                errors.append(error)
        
        # Deduplicate errors
        seen = set()
        unique_errors = []
        for e in errors:
            key = (e.file, e.line, e.column, e.message)
            if key not in seen:
                seen.add(key)
                unique_errors.append(e)
        
        return unique_errors
    
    @classmethod
    def _categorize_error(cls, message: str) -> ErrorCategory:
        """Categorize an error message."""
        message_lower = message.lower()
        
        for category, patterns in cls.CATEGORY_PATTERNS.items():
            for pattern in patterns:
                if re.search(pattern, message_lower):
                    return category
        
        return ErrorCategory.UNKNOWN
    
    @classmethod
    def parse_checker_trace(cls, trace: str) -> Dict[str, Any]:
        """Parse a PChecker trace log to extract error information."""
        result = {
            "error_type": None,
            "error_message": None,
            "failing_machine": None,
            "failing_state": None,
            "event_involved": None,
            "trace_summary": [],
        }
        
        lines = trace.strip().split('\n')
        
        for line in lines:
            # Extract error log
            if '<ErrorLog>' in line:
                result["error_message"] = line.split('<ErrorLog>')[-1].strip()
                
                # Check for specific error types
                if 'cannot be handled' in line:
                    result["error_type"] = "unhandled_event"
                    # Extract event name
                    match = re.search(r"event '([^']+)'", line)
                    if match:
                        result["event_involved"] = match.group(1)
                elif 'cannot be null' in line:
                    result["error_type"] = "null_target"
                elif 'assertion' in line.lower():
                    result["error_type"] = "assertion_failure"
            
            # Extract state transitions
            if '<StateLog>' in line:
                result["trace_summary"].append(line)
                # Get current machine and state
                match = re.search(r"(\w+)\((\d+)\)\s+enters state\s+'([^']+)'", line)
                if match:
                    result["failing_machine"] = f"{match.group(1)}({match.group(2)})"
                    result["failing_state"] = match.group(3)
            
            # Extract send events
            if '<SendLog>' in line:
                result["trace_summary"].append(line)
        
        return result


@dataclass 
class CompilationResult:
    """Result of a P compilation attempt."""
    success: bool
    errors: List[PCompilerError] = field(default_factory=list)
    warnings: List[str] = field(default_factory=list)
    output: str = ""
    
    def get_first_error(self) -> Optional[PCompilerError]:
        return self.errors[0] if self.errors else None
    
    def get_errors_by_file(self) -> Dict[str, List[PCompilerError]]:
        by_file = {}
        for error in self.errors:
            if error.file not in by_file:
                by_file[error.file] = []
            by_file[error.file].append(error)
        return by_file
    
    def to_dict(self) -> Dict[str, Any]:
        return {
            "success": self.success,
            "error_count": len(self.errors),
            "errors": [e.to_dict() for e in self.errors],
            "warnings": self.warnings,
        }


def parse_compilation_output(output: str) -> CompilationResult:
    """Parse full compilation output into a result object."""
    success = "Compilation succeeded" in output or "Build succeeded" in output
    errors = PCompilerErrorParser.parse(output)
    
    # Extract warnings
    warnings = []
    for line in output.split('\n'):
        if 'warning' in line.lower() and 'error' not in line.lower():
            warnings.append(line.strip())
    
    return CompilationResult(
        success=success and len(errors) == 0,
        errors=errors,
        warnings=warnings,
        output=output,
    )
