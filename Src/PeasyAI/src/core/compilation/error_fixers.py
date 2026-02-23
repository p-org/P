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
        # Additional pattern-based fixers for issues without categories
        self._pattern_fixers = [
            # Single-field named tuples missing trailing comma — covers all variants:
            #   "no viable alternative at input 'fieldName=value)'"
            #   "missing Iden at ')'"
            #   "no viable alternative at input 'funFoo(config:(field:type,)'"
            (r"no viable alternative.*\w+\s*=\s*\w+\)'", self._fix_single_field_tuple),
            (r"missing Iden at '\)'", self._fix_single_field_tuple),
            (r"no viable alternative.*'reason=\d+\)'", self._fix_single_field_tuple),
            (r"no viable alternative.*'reservationId=", self._fix_single_field_tuple),
            (r"no viable alternative.*testTest", self._fix_test_declaration),
            (r"could not find.*type.*'(\w+)'", self._fix_undefined_type),
            (r"extraneous input 'var'", self._fix_var_declaration_order_from_message),
        ]
    
    def can_fix(self, error: PCompilerError) -> bool:
        """Check if we can automatically fix this error."""
        if error.category in self._fixers:
            return True
        # Check pattern-based fixers
        for pattern, _ in self._pattern_fixers:
            if re.search(pattern, error.message, re.IGNORECASE):
                return True
        return False
    
    def fix(self, error: PCompilerError, code: str) -> Optional[CodeFix]:
        """
        Attempt to fix an error in the code.
        Returns CodeFix if successful, None otherwise.
        """
        # Try category-based fixer first
        fixer = self._fixers.get(error.category)
        if fixer:
            try:
                result = fixer(error, code)
                if result:
                    return result
            except Exception as e:
                logger.error(f"Error in fixer for {error.category}: {e}")
        
        # Try pattern-based fixers
        for pattern, pattern_fixer in self._pattern_fixers:
            if re.search(pattern, error.message, re.IGNORECASE):
                try:
                    result = pattern_fixer(error, code)
                    if result:
                        return result
                except Exception as e:
                    logger.error(f"Error in pattern fixer for {pattern}: {e}")
        
        logger.debug(f"No fixer available for error: {error.message[:100]}")
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
    
    def _fix_named_field_tuple(self, error: PCompilerError, code: str) -> Optional[CodeFix]:
        """
        Fix named-field tuple construction on the error line.

        The P compiler rejects ``(field = value, ...)`` in value position.
        This converts every named-field assignment on the error line to
        positional form: ``(value, ...)``.
        """
        lines = code.split('\n')
        error_line_idx = error.line - 1 if error.line else 0
        if error_line_idx >= len(lines):
            return None

        problem_line = lines[error_line_idx]

        # Quick check: does the line even contain ``identifier = `` inside parens?
        if not re.search(r'\(\s*\w+\s*=\s*', problem_line):
            return None

        def _strip_fields_in_parens(line: str) -> str:
            """Replace all ``(field = val, ...)`` groups on a line."""
            result = []
            i = 0
            while i < len(line):
                # Look for opening paren followed by identifier =
                m = re.search(r'\(\s*([a-zA-Z_]\w*)\s*=\s*', line[i:])
                if not m:
                    result.append(line[i:])
                    break

                open_pos = i + m.start()
                result.append(line[i:open_pos])

                # Find balanced close-paren
                depth = 0
                close_pos = -1
                for j in range(open_pos, len(line)):
                    if line[j] == '(':
                        depth += 1
                    elif line[j] == ')':
                        depth -= 1
                        if depth == 0:
                            close_pos = j
                            break

                if close_pos == -1:
                    result.append(line[open_pos:])
                    break

                inner = line[open_pos + 1:close_pos]
                # Skip complex nested expressions
                if '(' in inner or ')' in inner:
                    result.append(line[open_pos:close_pos + 1])
                    i = close_pos + 1
                    continue

                parts = [p.strip() for p in inner.split(',') if p.strip()]
                named_count = sum(
                    1 for p in parts if re.match(r'^[a-zA-Z_]\w*\s*=\s*', p)
                )
                if named_count >= 1 and named_count == len(parts):
                    values = []
                    for p in parts:
                        fm = re.match(r'^[a-zA-Z_]\w*\s*=\s*(.+)$', p)
                        values.append(fm.group(1).strip() if fm else p)
                    trailing = ',' if inner.rstrip().endswith(',') else ''
                    result.append(f"({', '.join(values)}{trailing})")
                else:
                    result.append(line[open_pos:close_pos + 1])

                i = close_pos + 1

            return ''.join(result)

        fixed_line = _strip_fields_in_parens(problem_line)
        if fixed_line == problem_line:
            return None

        lines[error_line_idx] = fixed_line
        return CodeFix(
            file_path=error.file,
            original_code=code,
            fixed_code='\n'.join(lines),
            description=f"Converted named-field tuple to positional form at line {error.line}",
            line_changes=[(error.line, problem_line, fixed_line)],
        )

    def _fix_single_field_tuple(self, error: PCompilerError, code: str) -> Optional[CodeFix]:
        """
        Fix single-field tuple missing trailing comma on the error line.

        Handles all contexts:
          (field = value)   →  (field = value,)     value construction
          (field: type)     →  (field: type,)        type annotation
          ((field = value)) →  ((field = value,))    nested in new Machine((...))
        """
        lines = code.split('\n')
        error_line_idx = error.line - 1

        if error_line_idx >= len(lines):
            return None

        problem_line = lines[error_line_idx]

        # Value context: (identifier = expr) without trailing comma
        value_pat = r'\((\s*[a-zA-Z_]\w*\s*=\s*[^,()]+[^,\s])\s*\)'
        # Type annotation context: (identifier: type) without trailing comma
        type_pat = r'\((\s*[a-zA-Z_]\w*\s*:\s*[^,()]+[^,\s])\s*\)'

        fixed_line = problem_line
        changed = False

        for pat in [value_pat, type_pat]:
            m = re.search(pat, fixed_line)
            if m:
                inner = m.group(1)
                if not inner.rstrip().endswith(',') and inner.count('=') <= 1 and inner.count(':') <= 1:
                    fixed_line = re.sub(pat, r'(\1,)', fixed_line, count=1)
                    changed = True

        if not changed or fixed_line == problem_line:
            return None

        lines[error_line_idx] = fixed_line
        return CodeFix(
            file_path=error.file,
            original_code=code,
            fixed_code='\n'.join(lines),
            description=f"Added trailing comma to single-field tuple at line {error.line}",
            line_changes=[(error.line, problem_line, fixed_line)],
        )
    
    def _fix_test_declaration(self, error: PCompilerError, code: str) -> Optional[CodeFix]:
        """Fix test declaration syntax."""
        # Common issues:
        # - test Name [main=X]: assert Y in (union ...)  -> test Name [main=X]: assert Y in (union ...)
        # - Missing module declaration
        
        lines = code.split('\n')
        
        # Find test declarations
        fixed_lines = []
        changed = False
        
        for i, line in enumerate(lines):
            # Fix: test X [main=Y]: assert Z in (union {A, B})
            # To: module SystemModule = { A, B }; test X [main=Y]: assert Z in (union SystemModule, { Y })
            if 'test ' in line and '[main=' in line:
                # Check for malformed union syntax
                if 'union {' in line and 'union ' not in line.replace('union {', ''):
                    # Extract machines from union
                    union_match = re.search(r'union\s*\{([^}]+)\}', line)
                    if union_match:
                        machines = union_match.group(1)
                        # This is already valid syntax, skip
                        fixed_lines.append(line)
                        continue
            
            fixed_lines.append(line)
        
        if not changed:
            return None
        
        return CodeFix(
            file_path=error.file,
            original_code=code,
            fixed_code='\n'.join(fixed_lines),
            description="Fixed test declaration syntax"
        )
    
    def _fix_undefined_type(self, error: PCompilerError, code: str) -> Optional[CodeFix]:
        """Add placeholder for undefined type."""
        # Extract type name from error
        match = re.search(r"could not find.*type.*'(\w+)'", error.message, re.IGNORECASE)
        if not match:
            return None
        
        type_name = match.group(1)
        
        # Add placeholder type at the beginning of the file
        placeholder = f"// TODO: Define type properly\ntype {type_name} = (placeholder: int);\n\n"
        fixed_code = placeholder + code
        
        return CodeFix(
            file_path=error.file,
            original_code=code,
            fixed_code=fixed_code,
            description=f"Added placeholder for undefined type '{type_name}'"
        )
    
    def _fix_var_declaration_order_from_message(self, error: PCompilerError, code: str) -> Optional[CodeFix]:
        """Fix var declaration order based on 'extraneous input var' message."""
        # This handles the case when category isn't set but message indicates var order issue
        lines = code.split('\n')
        error_line_idx = error.line - 1
        
        if error_line_idx >= len(lines):
            return None
        
        problem_line = lines[error_line_idx]
        
        # Verify it's a var declaration
        if not problem_line.strip().startswith('var '):
            return None
        
        # Find function/entry block start
        block_start = None
        for i in range(error_line_idx - 1, -1, -1):
            line = lines[i]
            if re.match(r'\s*(fun\s+\w+|entry)', line):
                block_start = i
                break
        
        if block_start is None:
            return None
        
        # Find where vars should go (after opening brace)
        insert_pos = block_start + 1
        for i in range(block_start, error_line_idx):
            if '{' in lines[i]:
                insert_pos = i + 1
                break
        
        # Move the var declaration
        var_decl = lines.pop(error_line_idx)
        
        # Find end of existing var declarations
        for i in range(insert_pos, len(lines)):
            if not lines[i].strip().startswith('var ') and lines[i].strip():
                insert_pos = i
                break
        
        lines.insert(insert_pos, var_decl)
        
        return CodeFix(
            file_path=error.file,
            original_code=code,
            fixed_code='\n'.join(lines),
            description=f"Moved var declaration to start of function/block"
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
