"""
P Code Post-Processor.

Automatically fixes common LLM-generated P code issues before compilation.
This reduces the need for iterative fixing and improves code generation quality.
"""

import re
import logging
from typing import List, Tuple, Dict, Optional
from dataclasses import dataclass

logger = logging.getLogger(__name__)


@dataclass
class PostProcessResult:
    """Result of post-processing."""
    code: str
    fixes_applied: List[str]
    warnings: List[str]


class PCodePostProcessor:
    """
    Auto-fix common P syntax issues in generated code.
    
    Handles:
    1. Variable declaration order (must be at start of functions)
    2. Single-field tuple syntax (needs trailing comma)
    3. Enum access syntax (EnumName.VALUE -> VALUE)
    4. Test declaration syntax fixes
    5. Missing semicolons
    """
    
    def __init__(self):
        self.fixes_applied: List[str] = []
        self.warnings: List[str] = []
    
    def process(self, code: str, filename: str = "") -> PostProcessResult:
        """
        Process P code and fix common issues.
        
        Args:
            code: The P code to process
            filename: Optional filename for better error messages
            
        Returns:
            PostProcessResult with fixed code and list of fixes applied
        """
        self.fixes_applied = []
        self.warnings = []
        
        original_code = code
        
        # Apply fixes in order
        code = self._fix_variable_declaration_order(code)
        code = self._fix_single_field_tuples(code)
        code = self._fix_enum_access_syntax(code)
        code = self._fix_test_declaration_syntax(code)
        code = self._fix_missing_semicolons(code)
        code = self._fix_entry_function_syntax(code)
        
        if code != original_code:
            logger.info(f"Post-processing applied {len(self.fixes_applied)} fix(es) to {filename or 'code'}")
        
        return PostProcessResult(
            code=code,
            fixes_applied=self.fixes_applied,
            warnings=self.warnings
        )
    
    def _fix_variable_declaration_order(self, code: str) -> str:
        """
        Move variable declarations to the start of functions.
        
        In P, all var declarations must come before any statements in a function.
        """
        # Pattern to match function bodies
        func_pattern = r'(fun\s+\w+\s*\([^)]*\)\s*(?::\s*\w+)?\s*\{)(.*?)(\})'
        
        def reorder_vars(match):
            func_header = match.group(1)
            func_body = match.group(2)
            func_close = match.group(3)
            
            # Split into lines
            lines = func_body.split('\n')
            var_lines = []
            other_lines = []
            
            for line in lines:
                stripped = line.strip()
                if stripped.startswith('var ') and ';' in stripped:
                    var_lines.append(line)
                else:
                    other_lines.append(line)
            
            # Check if reordering is needed
            if var_lines:
                # Find first non-var, non-empty line
                first_non_var_idx = -1
                first_var_idx = -1
                
                for i, line in enumerate(lines):
                    stripped = line.strip()
                    if not stripped or stripped.startswith('//'):
                        continue
                    if stripped.startswith('var '):
                        if first_var_idx == -1:
                            first_var_idx = i
                    else:
                        if first_non_var_idx == -1:
                            first_non_var_idx = i
                            break
                
                # Only reorder if vars come after statements
                if first_var_idx > first_non_var_idx and first_non_var_idx != -1:
                    self.fixes_applied.append("Moved variable declarations to start of function")
                    # Reconstruct with vars first
                    new_body = '\n'.join(var_lines + other_lines)
                    return func_header + new_body + func_close
            
            return match.group(0)
        
        return re.sub(func_pattern, reorder_vars, code, flags=re.DOTALL)
    
    def _fix_single_field_tuples(self, code: str) -> str:
        """
        Add trailing comma to single-field named tuples.
        
        P requires: (field = value,) not (field = value)
        """
        # Pattern for single-field named tuple without trailing comma
        # Match: (fieldName = value) where value doesn't end with comma
        # But not: (fieldName = value,)
        
        # This is tricky - we need to find send statements with single-field payloads
        original = code
        
        # Pattern for send with single-field tuple payload
        # send target, event, (field = value);
        pattern = r'(send\s+[^,]+,\s*\w+,\s*\()(\w+\s*=\s*[^,)]+)(\)\s*;)'
        
        def add_trailing_comma(match):
            prefix = match.group(1)
            field_value = match.group(2).strip()
            suffix = match.group(3)
            
            # Check if it's truly single-field (no comma in value)
            if ',' not in field_value and not field_value.endswith(','):
                self.fixes_applied.append(f"Added trailing comma to single-field tuple")
                return f"{prefix}{field_value},{suffix}"
            return match.group(0)
        
        code = re.sub(pattern, add_trailing_comma, code)
        
        # Also handle type annotations with single field tuples
        # e.g., fun HandleX(payload: (reservationId: int))
        # This is less common but can occur
        
        return code
    
    def _fix_enum_access_syntax(self, code: str) -> str:
        """
        Fix enum access from EnumName.VALUE to just VALUE.
        
        In P, enums are accessed directly without the type prefix.
        """
        # Common enum patterns to fix
        # Pattern: EnumName.VALUE where EnumName starts with 't' (P convention)
        pattern = r'\bt([A-Z]\w*)\.([\w]+)\b'
        
        def fix_enum(match):
            enum_name = 't' + match.group(1)
            value = match.group(2)
            self.fixes_applied.append(f"Fixed enum access: {enum_name}.{value} -> {value}")
            return value
        
        return re.sub(pattern, fix_enum, code)
    
    def _fix_test_declaration_syntax(self, code: str) -> str:
        """
        Fix test declaration syntax.
        
        Correct: test Name [main=Driver]: (union Module, { Driver });
        Wrong: test Name [main=Driver]: assert X in (union ...);
        """
        # Fix 'assert X in' pattern
        pattern = r'(test\s+\w+\s*\[main=\w+\]\s*:\s*)assert\s+\w+\s+in\s*'
        if re.search(pattern, code):
            code = re.sub(pattern, r'\1', code)
            self.fixes_applied.append("Fixed test declaration: removed 'assert X in'")
        
        # Fix missing module declaration
        # If test uses union with raw machines, suggest module
        
        return code
    
    def _fix_missing_semicolons(self, code: str) -> str:
        """
        Add missing semicolons after statements.
        """
        # Pattern for statements that should end with semicolon but don't
        # This is conservative to avoid breaking valid code
        
        # Fix: return statement without semicolon
        pattern = r'(return\s+[^;{}\n]+)(\n)'
        
        def add_semicolon(match):
            stmt = match.group(1).rstrip()
            newline = match.group(2)
            if not stmt.endswith(';') and not stmt.endswith('{') and not stmt.endswith('}'):
                self.fixes_applied.append("Added missing semicolon after return")
                return stmt + ';' + newline
            return match.group(0)
        
        code = re.sub(pattern, add_semicolon, code)
        
        return code
    
    def _fix_entry_function_syntax(self, code: str) -> str:
        """
        Fix entry function references.
        
        entry { ... } is valid
        entry FunctionName; is valid
        entry FunctionName() is WRONG - should be entry FunctionName;
        """
        # Fix entry FunctionName() -> entry FunctionName;
        pattern = r'entry\s+(\w+)\s*\(\s*\)\s*;'
        if re.search(pattern, code):
            code = re.sub(pattern, r'entry \1;', code)
            self.fixes_applied.append("Fixed entry function syntax: removed ()")
        
        return code


class TypeConsistencyChecker:
    """
    Check and fix type consistency across P files.
    """
    
    def __init__(self):
        self.defined_types: set = set()
        self.defined_events: set = set()
        self.defined_machines: set = set()
    
    def extract_definitions(self, code: str) -> None:
        """Extract type, event, and machine definitions from code."""
        # Extract types
        type_pattern = r'type\s+(\w+)\s*='
        self.defined_types.update(re.findall(type_pattern, code))
        
        # Extract enums
        enum_pattern = r'enum\s+(\w+)\s*\{'
        self.defined_types.update(re.findall(enum_pattern, code))
        
        # Extract events
        event_pattern = r'event\s+(\w+)'
        self.defined_events.update(re.findall(event_pattern, code))
        
        # Extract machines
        machine_pattern = r'machine\s+(\w+)\s*\{'
        self.defined_machines.update(re.findall(machine_pattern, code))
    
    def find_undefined_types(self, code: str) -> List[str]:
        """Find types used but not defined."""
        # Pattern for type usage
        # var x: TypeName
        # map[Type1, Type2]
        # seq[Type]
        # (field: Type)
        
        used_types = set()
        
        # var declarations
        var_pattern = r'var\s+\w+\s*:\s*(\w+)'
        used_types.update(re.findall(var_pattern, code))
        
        # Map types
        map_pattern = r'map\[(\w+),\s*(\w+)\]'
        for match in re.finditer(map_pattern, code):
            used_types.add(match.group(1))
            used_types.add(match.group(2))
        
        # Seq types
        seq_pattern = r'seq\[(\w+)\]'
        used_types.update(re.findall(seq_pattern, code))
        
        # Set types
        set_pattern = r'set\[(\w+)\]'
        used_types.update(re.findall(set_pattern, code))
        
        # Filter out built-in types
        builtin_types = {'int', 'bool', 'string', 'machine', 'any', 'float', 'event'}
        used_types -= builtin_types
        used_types -= self.defined_machines  # Machine names are valid types
        
        undefined = used_types - self.defined_types
        return list(undefined)
    
    def find_undefined_events(self, code: str) -> List[str]:
        """Find events used but not defined."""
        # Pattern for event usage in send/raise/on
        used_events = set()
        
        # send target, event, ...
        send_pattern = r'send\s+[^,]+,\s*(\w+)'
        used_events.update(re.findall(send_pattern, code))
        
        # raise event
        raise_pattern = r'raise\s+(\w+)'
        used_events.update(re.findall(raise_pattern, code))
        
        # on event
        on_pattern = r'on\s+(\w+)\s+'
        used_events.update(re.findall(on_pattern, code))
        
        undefined = used_events - self.defined_events
        return list(undefined)
    
    def generate_missing_type_stubs(self, undefined_types: List[str]) -> str:
        """Generate stub definitions for missing types."""
        stubs = []
        for type_name in undefined_types:
            # Generate a simple tuple type as placeholder
            stubs.append(f"// TODO: Define type {type_name}")
            stubs.append(f"type {type_name} = (placeholder: int);")
            stubs.append("")
        return '\n'.join(stubs)


class MachineConfigDetector:
    """
    Detect when machines need configuration events.
    """
    
    def detect_dependencies(self, machine_code: str) -> List[Tuple[str, str]]:
        """
        Detect machine fields that reference other machines.
        
        Returns list of (field_name, machine_type) tuples.
        """
        dependencies = []
        
        # Pattern: var fieldName: MachineName;
        # Where MachineName starts with uppercase (P convention)
        pattern = r'var\s+(\w+)\s*:\s*([A-Z]\w*)\s*;'
        
        for match in re.finditer(pattern, machine_code):
            field_name = match.group(1)
            machine_type = match.group(2)
            
            # Skip built-in types that look like machine names
            if machine_type not in {'Int', 'Bool', 'String', 'Machine', 'Any'}:
                dependencies.append((field_name, machine_type))
        
        return dependencies
    
    def generate_config_event(self, machine_name: str, dependencies: List[Tuple[str, str]]) -> str:
        """Generate configuration event and handler for a machine."""
        if not dependencies:
            return ""
        
        # Generate event type
        fields = ', '.join(f"{name}: {mtype}" for name, mtype in dependencies)
        event_name = f"eConfig{machine_name}"
        
        code_lines = [
            f"// Configuration event for {machine_name}",
            f"event {event_name}: ({fields},);",
            ""
        ]
        
        return '\n'.join(code_lines)
    
    def generate_config_handler(self, machine_name: str, dependencies: List[Tuple[str, str]]) -> str:
        """Generate configuration handler code."""
        if not dependencies:
            return ""
        
        event_name = f"eConfig{machine_name}"
        
        lines = [
            f"  fun Configure(config: ({', '.join(f'{name}: {mtype}' for name, mtype in dependencies)},)) {{",
        ]
        
        for field_name, _ in dependencies:
            lines.append(f"    {field_name} = config.{field_name};")
        
        lines.append("  }")
        
        return '\n'.join(lines)


def post_process_file(code: str, filename: str = "") -> PostProcessResult:
    """Convenience function to post-process a single file."""
    processor = PCodePostProcessor()
    return processor.process(code, filename)


def check_type_consistency(types_code: str, machine_code: str) -> Dict[str, List[str]]:
    """Check type consistency between types file and machine code."""
    checker = TypeConsistencyChecker()
    checker.extract_definitions(types_code)
    
    return {
        "undefined_types": checker.find_undefined_types(machine_code),
        "undefined_events": checker.find_undefined_events(machine_code)
    }
