"""
Specialized Error Fixers for P Code

Implements automatic fixes for common P compiler errors.
"""

import re
import logging
from typing import Optional, Tuple, List, Callable
from dataclasses import dataclass

from .error_parser import PCompilerError, ErrorCategory

logger = logging.getLogger(__name__)


@dataclass
class CodeFix:
    """A fix to apply to code."""
    file_path: str
    original_code: str
    fixed_code: str
    description: str
    line_changes: List[Tuple[int, str, str]] = None  # [(line, old, new), ...]
    
    @property
    def diff_summary(self) -> str:
        if self.line_changes:
            changes = []
            for line, old, new in self.line_changes:
                changes.append(f"  Line {line}: '{old.strip()}' → '{new.strip()}'")
            return "\n".join(changes)
        return f"  {self.description}"


class PErrorFixer:
    """Fixes common P compiler errors automatically."""
    
    def __init__(self):
        self._fixers = {
            ErrorCategory.VAR_DECLARATION_ORDER: self._fix_var_declaration_order,
            ErrorCategory.FOREACH_ITERATOR: self._fix_foreach_iterator,
            ErrorCategory.INVALID_CHARACTER: self._fix_invalid_characters,
            ErrorCategory.UNHANDLED_EVENT: self._fix_unhandled_event,
            ErrorCategory.MISSING_SEMICOLON: self._fix_missing_semicolon,
        }
    
    def can_fix(self, error: PCompilerError) -> bool:
        """Check if we can automatically fix this error."""
        return error.category in self._fixers
    
    def fix(self, error: PCompilerError, code: str) -> Optional[CodeFix]:
        """
        Attempt to fix an error in the code.
        Returns CodeFix if successful, None otherwise.
        """
        fixer = self._fixers.get(error.category)
        if not fixer:
            logger.debug(f"No fixer available for category: {error.category}")
            return None
        
        try:
            return fixer(error, code)
        except Exception as e:
            logger.error(f"Error in fixer for {error.category}: {e}")
            return None
    
    def _fix_var_declaration_order(self, error: PCompilerError, code: str) -> Optional[CodeFix]:
        """Fix variable declarations that appear after statements."""
        lines = code.split('\n')
        error_line_idx = error.line - 1
        
        if error_line_idx >= len(lines):
            return None
        
        problem_line = lines[error_line_idx]
        
        # Check if it's a var declaration
        if not problem_line.strip().startswith('var '):
            return None
        
        # Find the start of the current block (function or entry)
        block_start = None
        brace_count = 0
        
        for i in range(error_line_idx - 1, -1, -1):
            line = lines[i]
            brace_count += line.count('}') - line.count('{')
            
            # Check for function or entry start
            if re.match(r'\s*(fun\s+\w+|entry)\s*', line) and '{' in line:
                block_start = i
                break
            elif '{' in line and brace_count <= 0:
                block_start = i
                break
        
        if block_start is None:
            return None
        
        # Find where var declarations should end
        var_end = block_start + 1
        for i in range(block_start + 1, error_line_idx):
            if lines[i].strip().startswith('var '):
                var_end = i + 1
            elif lines[i].strip() and not lines[i].strip().startswith('//'):
                break
        
        # Move the problematic var declaration
        var_decl = lines.pop(error_line_idx)
        lines.insert(var_end, var_decl)
        
        fixed_code = '\n'.join(lines)
        
        return CodeFix(
            file_path=error.file,
            original_code=code,
            fixed_code=fixed_code,
            description=f"Moved var declaration from line {error.line} to line {var_end + 1}",
            line_changes=[(error.line, problem_line, f"(moved to line {var_end + 1})")]
        )
    
    def _fix_foreach_iterator(self, error: PCompilerError, code: str) -> Optional[CodeFix]:
        """Fix missing foreach iterator variable declaration."""
        lines = code.split('\n')
        error_line_idx = error.line - 1
        
        if error_line_idx >= len(lines):
            return None
        
        problem_line = lines[error_line_idx]
        
        # Extract iterator variable name from foreach
        match = re.search(r'foreach\s*\(\s*(\w+)\s+in', problem_line)
        if not match:
            # Try to extract from error message
            match = re.search(r"variable '(\w+)'", error.message)
        
        if not match:
            return None
        
        iterator_name = match.group(1)
        
        # Find the function start to add var declaration
        func_start = None
        for i in range(error_line_idx - 1, -1, -1):
            line = lines[i]
            if re.match(r'\s*fun\s+\w+', line):
                func_start = i
                break
        
        if func_start is None:
            return None
        
        # Find where to insert var declaration (after opening brace)
        insert_line = func_start + 1
        for i in range(func_start, error_line_idx):
            if '{' in lines[i]:
                insert_line = i + 1
                break
        
        # Determine the type (try to infer from context)
        # Default to 'machine' as that's common for iterating over sets
        iterator_type = "machine"
        
        # Check if we can infer type from the collection
        collection_match = re.search(r'foreach\s*\(\s*\w+\s+in\s+(\w+)', problem_line)
        if collection_match:
            collection_name = collection_match.group(1)
            # Search for collection type in the code
            type_match = re.search(rf'var\s+{collection_name}\s*:\s*(set|seq|map)\s*\[\s*(\w+)', code)
            if type_match:
                iterator_type = type_match.group(2)
        
        # Get indentation
        indent = len(problem_line) - len(problem_line.lstrip())
        base_indent = "    " * (indent // 4)
        
        # Insert var declaration
        var_decl = f"{base_indent}var {iterator_name}: {iterator_type};"
        lines.insert(insert_line, var_decl)
        
        fixed_code = '\n'.join(lines)
        
        return CodeFix(
            file_path=error.file,
            original_code=code,
            fixed_code=fixed_code,
            description=f"Added declaration 'var {iterator_name}: {iterator_type};' at line {insert_line + 1}",
            line_changes=[(insert_line + 1, "", var_decl)]
        )
    
    def _fix_invalid_characters(self, error: PCompilerError, code: str) -> Optional[CodeFix]:
        """Fix invalid characters like markdown code fences."""
        original = code
        
        # Remove markdown artifacts
        code = re.sub(r'^```\w*\s*\n?', '', code)
        code = re.sub(r'\n?```\s*$', '', code)
        code = code.replace('```', '')
        
        if code == original:
            return None
        
        return CodeFix(
            file_path=error.file,
            original_code=original,
            fixed_code=code,
            description="Removed markdown code fence artifacts (```)"
        )
    
    def _fix_unhandled_event(self, error: PCompilerError, code: str) -> Optional[CodeFix]:
        """Fix unhandled event by adding ignore statement."""
        # Extract event name from error
        match = re.search(r"event '([^']+)'", error.message)
        if not match:
            match = re.search(r"(e\w+)", error.message)
        
        if not match:
            return None
        
        event_name = match.group(1)
        
        # Find the state where this needs to be added
        lines = code.split('\n')
        
        # Look for the state definition
        state_match = re.search(r"state\s+'([^']+)'", error.message)
        if not state_match:
            return None
        
        state_name = state_match.group(1)
        # Extract just the state name without the full path
        if '.' in state_name:
            state_name = state_name.split('.')[-1]
        
        # Find the state in the code
        state_line = None
        for i, line in enumerate(lines):
            if f'state {state_name}' in line:
                state_line = i
                break
        
        if state_line is None:
            return None
        
        # Find where to insert ignore statement (before closing brace of state)
        insert_line = None
        brace_count = 0
        for i in range(state_line, len(lines)):
            brace_count += lines[i].count('{') - lines[i].count('}')
            if brace_count == 0 and '}' in lines[i]:
                insert_line = i
                break
        
        if insert_line is None:
            return None
        
        # Get indentation
        state_indent = len(lines[state_line]) - len(lines[state_line].lstrip())
        inner_indent = "    " * ((state_indent // 4) + 1)
        
        # Insert ignore statement
        ignore_stmt = f"{inner_indent}ignore {event_name};"
        lines.insert(insert_line, ignore_stmt)
        
        fixed_code = '\n'.join(lines)
        
        return CodeFix(
            file_path=error.file,
            original_code=code,
            fixed_code=fixed_code,
            description=f"Added 'ignore {event_name};' to state {state_name}",
            line_changes=[(insert_line + 1, "", ignore_stmt)]
        )
    
    def _fix_missing_semicolon(self, error: PCompilerError, code: str) -> Optional[CodeFix]:
        """Fix missing semicolon at end of statement."""
        lines = code.split('\n')
        error_line_idx = error.line - 1
        
        if error_line_idx >= len(lines):
            return None
        
        problem_line = lines[error_line_idx]
        
        # Check if line needs semicolon
        stripped = problem_line.rstrip()
        if stripped.endswith(';') or stripped.endswith('{') or stripped.endswith('}'):
            return None
        
        # Add semicolon
        lines[error_line_idx] = stripped + ';'
        
        fixed_code = '\n'.join(lines)
        
        return CodeFix(
            file_path=error.file,
            original_code=code,
            fixed_code=fixed_code,
            description=f"Added missing semicolon at line {error.line}",
            line_changes=[(error.line, problem_line, stripped + ';')]
        )


def apply_fix(code: str, fix: CodeFix) -> str:
    """Apply a fix to code."""
    return fix.fixed_code


def fix_all_errors(code: str, errors: List[PCompilerError]) -> Tuple[str, List[CodeFix]]:
    """
    Attempt to fix all errors in the code.
    Returns (fixed_code, list_of_applied_fixes).
    """
    fixer = PErrorFixer()
    applied_fixes = []
    current_code = code
    
    for error in errors:
        if fixer.can_fix(error):
            fix = fixer.fix(error, current_code)
            if fix:
                current_code = fix.fixed_code
                applied_fixes.append(fix)
                logger.info(f"Applied fix: {fix.description}")
    
    return current_code, applied_fixes
