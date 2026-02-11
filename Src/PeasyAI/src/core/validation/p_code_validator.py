"""
P Code Validation and Post-Processing

Validates and cleans up LLM-generated P code before saving.
"""

import re
import logging
from typing import List, Tuple, Optional
from dataclasses import dataclass, field

logger = logging.getLogger(__name__)


@dataclass
class ValidationIssue:
    """A validation issue found in P code"""
    line: int
    column: int
    severity: str  # 'error', 'warning', 'info'
    message: str
    suggestion: Optional[str] = None


@dataclass
class ValidationResult:
    """Result of P code validation"""
    is_valid: bool
    issues: List[ValidationIssue] = field(default_factory=list)
    cleaned_code: Optional[str] = None
    
    @property
    def errors(self) -> List[ValidationIssue]:
        return [i for i in self.issues if i.severity == 'error']
    
    @property
    def warnings(self) -> List[ValidationIssue]:
        return [i for i in self.issues if i.severity == 'warning']


class PCodePostProcessor:
    """Post-processes LLM-generated P code to fix common issues."""
    
    @staticmethod
    def strip_markdown_artifacts(code: str) -> str:
        """Remove markdown code fences and other artifacts."""
        # Remove opening code fence with optional language tag
        code = re.sub(r'^```\w*\s*\n?', '', code)
        # Remove closing code fence
        code = re.sub(r'\n?```\s*$', '', code)
        # Remove any remaining triple backticks
        code = code.replace('```', '')
        return code.strip()
    
    @staticmethod
    def fix_var_declarations(code: str) -> Tuple[str, List[str]]:
        """
        Move variable declarations to the start of functions/entry blocks.
        Returns (fixed_code, list_of_fixes_applied).
        """
        fixes = []
        lines = code.split('\n')
        result_lines = []
        
        i = 0
        while i < len(lines):
            line = lines[i]
            
            # Check if we're entering a function or entry block
            if re.match(r'\s*(fun\s+\w+|entry)\s*[({]?\s*$', line) or \
               re.match(r'\s*entry\s*\{?\s*$', line):
                # Collect the block
                block_start = i
                block_lines = [line]
                brace_count = line.count('{') - line.count('}')
                i += 1
                
                while i < len(lines) and (brace_count > 0 or '{' not in ''.join(block_lines)):
                    block_lines.append(lines[i])
                    brace_count += lines[i].count('{') - lines[i].count('}')
                    i += 1
                
                # Now reorder var declarations in this block
                fixed_block, block_fixes = PCodePostProcessor._reorder_vars_in_block(block_lines)
                result_lines.extend(fixed_block)
                fixes.extend(block_fixes)
            else:
                result_lines.append(line)
                i += 1
        
        return '\n'.join(result_lines), fixes
    
    @staticmethod
    def _reorder_vars_in_block(block_lines: List[str]) -> Tuple[List[str], List[str]]:
        """Reorder variable declarations to come first in a block."""
        fixes = []
        
        # Find the opening brace
        brace_idx = -1
        for idx, line in enumerate(block_lines):
            if '{' in line:
                brace_idx = idx
                break
        
        if brace_idx == -1:
            return block_lines, fixes
        
        # Separate var declarations from other statements
        var_decls = []
        other_stmts = []
        
        for idx in range(brace_idx + 1, len(block_lines)):
            line = block_lines[idx]
            stripped = line.strip()
            
            # Skip empty lines and closing braces at the end
            if not stripped or stripped == '}':
                other_stmts.append(line)
                continue
            
            # Check if it's a var declaration
            if stripped.startswith('var ') and ';' in stripped:
                var_decls.append(line)
            else:
                other_stmts.append(line)
        
        # Check if we need to reorder
        if var_decls and other_stmts:
            # Check if vars were not at the start
            first_non_empty = next((l for l in other_stmts if l.strip() and l.strip() != '}'), None)
            if first_non_empty and not first_non_empty.strip().startswith('var '):
                # Need to reorder
                fixes.append(f"Moved {len(var_decls)} var declaration(s) to start of block")
                
                # Reconstruct block
                result = block_lines[:brace_idx + 1]  # Up to and including opening brace
                result.extend(var_decls)
                result.extend([l for l in other_stmts if l.strip() and l.strip() != '}'])
                result.extend([l for l in other_stmts if l.strip() == '}' or not l.strip()])
                return result, fixes
        
        return block_lines, fixes
    
    @staticmethod
    def add_missing_semicolons(code: str) -> Tuple[str, List[str]]:
        """Add missing semicolons to statements."""
        fixes = []
        lines = code.split('\n')
        result = []
        
        statement_keywords = ['var ', 'return ', 'send ', 'raise ', 'print ', 'assert ', 'assume ']
        
        for i, line in enumerate(lines):
            stripped = line.strip()
            
            # Check if line needs a semicolon
            needs_semicolon = False
            for kw in statement_keywords:
                if stripped.startswith(kw) and not stripped.endswith(';') and not stripped.endswith('{'):
                    needs_semicolon = True
                    break
            
            if needs_semicolon:
                # Add semicolon
                result.append(line.rstrip() + ';')
                fixes.append(f"Line {i+1}: Added missing semicolon")
            else:
                result.append(line)
        
        return '\n'.join(result), fixes
    
    @classmethod
    def process(cls, code: str) -> Tuple[str, List[str]]:
        """
        Apply all post-processing steps to P code.
        Returns (processed_code, list_of_changes).
        """
        all_fixes = []
        
        # Step 1: Strip markdown
        original = code
        code = cls.strip_markdown_artifacts(code)
        if code != original:
            all_fixes.append("Removed markdown code fences")
        
        # Step 2: Fix var declarations
        code, fixes = cls.fix_var_declarations(code)
        all_fixes.extend(fixes)
        
        # Step 3: Add missing semicolons (be conservative)
        # code, fixes = cls.add_missing_semicolons(code)
        # all_fixes.extend(fixes)
        
        return code, all_fixes


class PCodeValidator:
    """Validates P code for common issues."""
    
    @staticmethod
    def validate_braces(code: str) -> List[ValidationIssue]:
        """Check for balanced braces."""
        issues = []
        count = 0
        for i, char in enumerate(code):
            if char == '{':
                count += 1
            elif char == '}':
                count -= 1
                if count < 0:
                    line_num = code[:i].count('\n') + 1
                    issues.append(ValidationIssue(
                        line=line_num,
                        column=0,
                        severity='error',
                        message="Unmatched closing brace",
                        suggestion="Check for missing opening brace"
                    ))
        
        if count > 0:
            issues.append(ValidationIssue(
                line=code.count('\n') + 1,
                column=0,
                severity='error',
                message=f"Missing {count} closing brace(s)",
                suggestion="Add closing braces"
            ))
        
        return issues
    
    @staticmethod
    def validate_var_declarations(code: str) -> List[ValidationIssue]:
        """Check that var declarations come first in blocks."""
        issues = []
        lines = code.split('\n')
        
        in_block = False
        seen_statement = False
        block_start_line = 0
        
        for i, line in enumerate(lines):
            stripped = line.strip()
            
            if '{' in line:
                in_block = True
                seen_statement = False
                block_start_line = i + 1
            
            if in_block:
                if stripped.startswith('var ') and seen_statement:
                    issues.append(ValidationIssue(
                        line=i + 1,
                        column=0,
                        severity='error',
                        message="Variable declaration after statement",
                        suggestion="Move var declarations to the start of the block"
                    ))
                elif stripped and not stripped.startswith('var ') and not stripped.startswith('//') and stripped != '}':
                    seen_statement = True
            
            if '}' in line:
                in_block = False
                seen_statement = False
        
        return issues
    
    @staticmethod
    def validate_event_references(code: str, defined_events: List[str]) -> List[ValidationIssue]:
        """Check that referenced events are defined."""
        issues = []
        
        # Find event references (send X, eEventName, on eEventName)
        event_pattern = r'\b(send\s+\w+,\s*|on\s+|raise\s+)(e\w+)'
        
        for match in re.finditer(event_pattern, code):
            event_name = match.group(2)
            if event_name not in defined_events:
                line_num = code[:match.start()].count('\n') + 1
                issues.append(ValidationIssue(
                    line=line_num,
                    column=match.start(),
                    severity='warning',
                    message=f"Event '{event_name}' may not be defined",
                    suggestion=f"Ensure '{event_name}' is declared in types/events file"
                ))
        
        return issues
    
    @classmethod
    def validate(cls, code: str, context: dict = None) -> ValidationResult:
        """Run all validations on P code."""
        context = context or {}
        all_issues = []
        
        # Check braces
        all_issues.extend(cls.validate_braces(code))
        
        # Check var declarations
        all_issues.extend(cls.validate_var_declarations(code))
        
        # Check event references if we have context
        if 'defined_events' in context:
            all_issues.extend(cls.validate_event_references(code, context['defined_events']))
        
        has_errors = any(i.severity == 'error' for i in all_issues)
        
        return ValidationResult(
            is_valid=not has_errors,
            issues=all_issues,
            cleaned_code=code
        )


def process_and_validate(code: str, context: dict = None) -> Tuple[str, ValidationResult]:
    """
    Post-process and validate P code.
    Returns (processed_code, validation_result).
    """
    # Post-process
    processed_code, fixes = PCodePostProcessor.process(code)
    
    # Validate
    result = PCodeValidator.validate(processed_code, context)
    
    # Add fix information to result
    for fix in fixes:
        result.issues.append(ValidationIssue(
            line=0,
            column=0,
            severity='info',
            message=f"Auto-fixed: {fix}"
        ))
    
    result.cleaned_code = processed_code
    
    return processed_code, result
