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
    
    def process(self, code: str, filename: str = "", is_test_file: bool = False) -> PostProcessResult:
        """
        Process P code and fix common issues.
        
        Args:
            code: The P code to process
            filename: Optional filename for better error messages
            is_test_file: If True, this file belongs to PTst and must
                          contain test declarations.
            
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
        code = self._fix_bare_halt(code)
        code = self._fix_forbidden_in_monitors(code)
        
        # For PTst files, check for common wiring bugs and ensure test declarations
        if is_test_file:
            code = self._warn_timer_wired_to_this(code, filename)
            code = self._ensure_test_declarations(code, filename)
        
        if code != original_code:
            logger.info(f"Post-processing applied {len(self.fixes_applied)} fix(es) to {filename or 'code'}")
        
        return PostProcessResult(
            code=code,
            fixes_applied=self.fixes_applied,
            warnings=self.warnings
        )
    
    def _fix_named_field_tuple_construction(self, code: str) -> str:
        """
        Convert named-field tuple construction to positional form.

        LLMs frequently generate ``(field1 = val1, field2 = val2)`` when
        creating a value of a user-defined named-tuple type.  P requires
        the *positional* form ``(val1, val2)`` — the field names are part
        of the type definition, not the value constructor.

        This fix targets the most common occurrences:
          - send target, event, (field = value, ...);
          - new Machine((field = value, ...));
          - raise event, (field = value, ...);
          - variable assignment:  x = (field = value, ...);
          - function call argument:  fun(..., (field = value, ...));

        Single-field named tuples ``(field = value,)`` → ``(value,)``
        are handled too.
        """
        original = code

        def _strip_field_names(tuple_inner: str) -> str:
            """Strip ``field =`` prefixes from a comma-separated tuple body."""
            parts = []
            for part in tuple_inner.split(','):
                part = part.strip()
                if not part:
                    continue
                # Match  fieldName = expression
                m = re.match(r'^([a-zA-Z_]\w*)\s*=\s*(.+)$', part)
                if m:
                    parts.append(m.group(2).strip())
                else:
                    parts.append(part)
            return ', '.join(parts)

        def _has_named_fields(inner: str) -> bool:
            """Return True if the inner text looks like named-field assignments."""
            parts = [p.strip() for p in inner.split(',') if p.strip()]
            named = sum(1 for p in parts if re.match(r'^[a-zA-Z_]\w*\s*=\s*', p))
            return named >= 1 and named == len(parts)

        def _find_balanced_paren(text: str, open_pos: int) -> int:
            """Return index of matching close-paren, or -1."""
            depth = 0
            for i in range(open_pos, len(text)):
                if text[i] == '(':
                    depth += 1
                elif text[i] == ')':
                    depth -= 1
                    if depth == 0:
                        return i
            return -1

        # We iterate through occurrences of ``(identifier = `` which is the
        # telltale opening of a named-field construction.  We then extract the
        # balanced content up to the matching ``)`` and convert.
        result_parts: List[str] = []
        pos = 0
        fix_count = 0

        named_field_open = re.compile(r'\(\s*[a-zA-Z_]\w*\s*=\s*')

        while pos < len(code):
            m = named_field_open.search(code, pos)
            if not m:
                result_parts.append(code[pos:])
                break

            open_pos = m.start()
            close_pos = _find_balanced_paren(code, open_pos)
            if close_pos == -1:
                result_parts.append(code[pos:])
                break

            inner = code[open_pos + 1:close_pos]

            # Skip if the inner text contains nested parens (complex expressions)
            # that would make naive splitting unreliable.
            if '(' in inner or ')' in inner:
                result_parts.append(code[pos:close_pos + 1])
                pos = close_pos + 1
                continue

            if _has_named_fields(inner):
                stripped = _strip_field_names(inner)
                trailing_comma = ',' if inner.rstrip().endswith(',') else ''
                replacement = f"({stripped}{trailing_comma})"
                result_parts.append(code[pos:open_pos])
                result_parts.append(replacement)
                fix_count += 1
                pos = close_pos + 1
            else:
                result_parts.append(code[pos:close_pos + 1])
                pos = close_pos + 1

        code = ''.join(result_parts)

        if fix_count > 0:
            self.fixes_applied.append(
                f"Converted {fix_count} named-field tuple construction(s) to positional form"
            )

        return code

    def _fix_variable_declaration_order(self, code: str) -> str:
        """
        Move variable declarations to the start of functions.
        
        In P, all var declarations must come before any statements in a function.
        Uses brace-balanced extraction to handle nested blocks correctly.
        """
        from .p_code_utils import iter_function_bodies

        # Process functions from last to first so position offsets stay valid
        replacements = []
        for func_name, header, body, start_pos, close_pos in iter_function_bodies(code):
            lines = body.split('\n')
            var_lines = []
            other_lines = []

            for line in lines:
                stripped = line.strip()
                if stripped.startswith('var ') and ';' in stripped:
                    var_lines.append(line)
                else:
                    other_lines.append(line)

            if not var_lines:
                continue

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

            if first_var_idx > first_non_var_idx and first_non_var_idx != -1:
                new_body = '\n'.join(var_lines + other_lines)
                # Reconstruct: header + { + new_body + }
                replacement = header + '{' + new_body + '}'
                replacements.append((start_pos, close_pos + 1, replacement))

        if replacements:
            self.fixes_applied.append("Moved variable declarations to start of function")
            # Apply from end to start to preserve positions
            for start, end, repl in reversed(replacements):
                code = code[:start] + repl + code[end:]

        return code
    
    def _fix_single_field_tuples(self, code: str) -> str:
        """
        Add trailing comma to single-field named tuples in ALL contexts.
        
        P requires trailing comma for single-field tuples:
          (field = value,)   not  (field = value)
          (field: type,)     not  (field: type)
        
        Handles: send payloads, new Machine(...), raise, assignments,
        and function parameter type annotations.
        """
        original = code
        fix_count = 0

        # --- Value context: (identifier = expression) without trailing comma ---
        # Matches (word = stuff) where "stuff" has no comma and no nested parens.
        # The value part [^,()=]+ excludes parens so double-paren contexts like
        # new Machine((field = value)) are handled correctly (inner match only).
        value_pattern = r'\((\s*[a-zA-Z_]\w*\s*=\s*[^,()=]+[^,\s()=])\s*\)'

        def _add_comma_value(m):
            inner = m.group(1)
            if inner.rstrip().endswith(','):
                return m.group(0)
            if inner.count('=') > 1:
                return m.group(0)
            nonlocal fix_count
            fix_count += 1
            return f'({inner},)'

        code = re.sub(value_pattern, _add_comma_value, code)

        # --- Type annotation context: (identifier: type) without trailing comma ---
        # Matches (word: type_expr) in function signatures and type definitions.
        # type_expr can be: int, bool, machine, seq[X], map[X,Y], set[X], tFoo
        type_pattern = r'\((\s*[a-zA-Z_]\w*\s*:\s*(?:seq|map|set)\s*\[[^\]]*\]\s*)\)'
        code = re.sub(type_pattern, r'(\1,)', code)

        # Simple types: (field: int), (field: machine), (field: tFoo)
        simple_type_pattern = r'\((\s*[a-zA-Z_]\w*\s*:\s*(?:int|bool|float|string|machine|any|event|[a-zA-Z_]\w*)\s*)\)'

        def _add_comma_type(m):
            inner = m.group(1)
            if inner.rstrip().endswith(','):
                return m.group(0)
            # Skip if there's more than one colon (multi-field)
            if inner.count(':') > 1:
                return m.group(0)
            nonlocal fix_count
            fix_count += 1
            return f'({inner},)'

        code = re.sub(simple_type_pattern, _add_comma_type, code)

        if fix_count > 0:
            self.fixes_applied.append(
                f"Added trailing comma to {fix_count} single-field named tuple(s)"
            )

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

    def validate_spec_events(self, spec_code: str, types_code: str, filename: str = "") -> List[str]:
        """
        Validate that all events in `spec ... observes` clauses are
        actually defined in the types/events file.

        Args:
            spec_code: The spec file code
            types_code: The Enums_Types_Events.p content
            filename: For error messages

        Returns:
            List of warning strings for undefined events
        """
        # Extract defined events from types file
        defined_events = set(re.findall(r'\bevent\s+(\w+)', types_code))

        # Extract observed events from spec declarations
        warnings = []
        for match in re.finditer(r'\bspec\s+(\w+)\s+observes\s+([^{]+)\{', spec_code):
            spec_name = match.group(1)
            observes_str = match.group(2).strip().rstrip(',')
            observed = [e.strip() for e in observes_str.split(',')]
            for ev in observed:
                if ev and ev not in defined_events:
                    warnings.append(
                        f"[{filename}] Spec '{spec_name}' observes undefined event '{ev}'. "
                        f"Defined events: {sorted(defined_events)}"
                    )
        return warnings

    def _fix_bare_halt(self, code: str) -> str:
        """
        Fix bare `halt;` → `raise halt;`.
        In P, `halt` is an event and must be raised, not used as a statement.
        """
        pattern = r'(?<!\braise\s)(?<!\w)\bhalt\s*;'
        if re.search(pattern, code):
            code = re.sub(pattern, 'raise halt;', code)
            self.fixes_applied.append("Fixed bare 'halt;' → 'raise halt;'")
        return code

    def _fix_forbidden_in_monitors(self, code: str) -> str:
        """
        Detect and remove forbidden keywords inside spec monitor bodies.
        P spec monitors cannot use: this, new, send, announce, receive, $, $$, pop.
        """
        # Find all spec blocks
        spec_pattern = r'\bspec\s+(\w+)\s+observes\s+[^{]+\{'
        for match in re.finditer(spec_pattern, code):
            spec_name = match.group(1)
            start = match.end() - 1
            depth = 0
            body_end = start
            for ci in range(start, len(code)):
                if code[ci] == '{':
                    depth += 1
                elif code[ci] == '}':
                    depth -= 1
                    if depth == 0:
                        body_end = ci
                        break
            body = code[start:body_end + 1]

            # Check for forbidden keywords (only standalone uses, not in strings/comments)
            forbidden = {
                'this': r'\bthis\b',
                'new': r'\bnew\s+\w+',
                'send': r'\bsend\s+',
                'announce': r'\bannounce\s+',
                'receive': r'\breceive\s*\{',
            }
            for kw, pattern_kw in forbidden.items():
                if re.search(pattern_kw, body):
                    self.warnings.append(
                        f"Spec monitor '{spec_name}' uses forbidden keyword '{kw}'. "
                        "Monitors cannot use this/new/send/announce/receive/$/$$/pop."
                    )
                    logger.warning(self.warnings[-1])

            # Auto-fix `this as machine` → remove the line (common pattern)
            if re.search(r'\bthis\b', body):
                # Try to remove `var = this as machine;` assignments
                fixed_body = re.sub(
                    r'\n\s*\w+\s*=\s*this\s+as\s+machine\s*;\s*\n',
                    '\n',
                    body,
                )
                if fixed_body != body:
                    code = code[:start] + fixed_body + code[body_end + 1:]
                    self.fixes_applied.append(
                        f"Removed 'this as machine' from spec monitor '{spec_name}'"
                    )

        return code

    def _warn_timer_wired_to_this(self, code: str, filename: str) -> str:
        """
        Detect when a Timer is created with `this` as client inside a
        scenario/test machine.  Timer(this) in a scenario machine is almost
        always wrong — the timer should fire to the Coordinator or the
        machine that actually handles eTimeOut.
        """
        # Find `new Timer(this)` or `new Timer((client = this, ...))` patterns
        pattern = r'\bnew\s+Timer\s*\(\s*this\s*\)'
        if re.search(pattern, code):
            self.warnings.append(
                f"[{filename}] Timer created with 'this' as client. "
                "In a scenario machine this means eTimeOut will be sent "
                "to the scenario machine which likely doesn't handle it. "
                "Pass the Coordinator or the machine that handles eTimeOut instead."
            )
            logger.warning(self.warnings[-1])
        # Also check named-tuple form: (client = this, ...) passed to Timer
        pattern2 = r'\bnew\s+Timer\s*\(\s*\([^)]*client\s*=\s*this[^)]*\)\s*\)'
        if re.search(pattern2, code):
            self.warnings.append(
                f"[{filename}] Timer created with 'client = this'. "
                "See above — pass the actual handler machine instead."
            )
            logger.warning(self.warnings[-1])
        return code

    def _ensure_test_declarations(self, code: str, filename: str) -> str:
        """
        Check that a test file contains `test` declarations.
        If machines exist but no test declarations, generate stub
        declarations so PChecker can discover and run the tests.
        """
        has_test_decl = bool(re.search(r'^\s*test\s+\w+\s*\[', code, re.MULTILINE))
        if has_test_decl:
            return code  # Already has test declarations

        # Find all machine names in the file
        machine_names = re.findall(r'\bmachine\s+(\w+)', code)
        if not machine_names:
            return code

        # In PTst files, machines that set up the system are test entry points.
        # A scenario machine typically: creates other machines (``new``),
        # sends config events (``send``), or is named with common scenario
        # prefixes (Scenario*, TestSetup*, Driver*).
        candidate_machines: List[str] = []

        for name in machine_names:
            pattern = rf'\bmachine\s+{re.escape(name)}\s*\{{'
            match = re.search(pattern, code)
            if not match:
                continue
            start = match.end() - 1
            depth = 0
            body_end = start
            for ci in range(start, len(code)):
                if code[ci] == '{':
                    depth += 1
                elif code[ci] == '}':
                    depth -= 1
                    if depth == 0:
                        body_end = ci
                        break
            body = code[start:body_end]

            is_scenario = (
                'new ' in body
                or 'send ' in body
                or re.match(r'(?i)(Scenario|TestSetup|Driver|Setup|Test)', name)
            )
            if is_scenario:
                candidate_machines.append(name)

        # Fallback: if no candidates found via heuristic, treat ALL machines
        # in the test file as potential scenario machines.
        if not candidate_machines:
            candidate_machines = list(machine_names)

        if not candidate_machines:
            self.warnings.append(
                f"[{filename}] No test declarations found and no scenario machines detected. "
                "PChecker will not discover any tests. "
                "Add 'test tcName [main=Machine]: ...;' declarations."
            )
            logger.warning(self.warnings[-1])
            return code

        # Collect all machine names for the test scope
        all_machines = ', '.join(machine_names)

        # Generate test declarations
        test_lines = ['\n// Auto-generated test declarations (post-processor)']
        for sc in candidate_machines:
            tc_name = 'tc' + sc.replace('Scenario', '').replace('_', '')
            test_lines.append(
                f'test {tc_name} [main={sc}]:\n'
                f'  {{{all_machines}}};'
            )

        appended = '\n'.join(test_lines) + '\n'
        self.fixes_applied.append(
            f"Added {len(candidate_machines)} test declaration(s) for scenario machines: "
            + ', '.join(candidate_machines)
        )
        logger.info(
            f"[{filename}] Auto-generated {len(candidate_machines)} test declaration(s): "
            + ', '.join(candidate_machines)
        )
        return code + appended


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
