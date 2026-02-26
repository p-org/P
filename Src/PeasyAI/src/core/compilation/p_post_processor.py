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
        code = self._fix_trailing_comma_in_params(code)
        code = self._fix_variable_declaration_order(code)
        code = self._fix_single_field_tuples(code)
        code = self._fix_named_field_tuple_construction(code)
        code = self._fix_enum_access_syntax(code)
        code = self._fix_test_declaration_syntax(code)
        code = self._fix_test_declaration_union_syntax(code)
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
    
    def _fix_trailing_comma_in_params(self, code: str) -> str:
        """
        Remove trailing commas from function, entry, and handler parameter lists.

        LLMs frequently generate ``fun Foo(param: Type,)`` but P requires
        ``fun Foo(param: Type)``.  Same for ``entry (param: Type,)`` and
        ``on eEvent do (param: Type,)``.

        Only targets single-parameter lists (the most common case).  Multi-param
        trailing commas are also fixed.
        """
        # fun Name(... ,)  →  fun Name(...)
        # entry (... ,)    →  entry (...)
        # on eX do (... ,) →  on eX do (...)
        pattern = re.compile(
            r'(\b(?:fun\s+\w+|entry|on\s+\w+\s+do)\s*\([^)]*?)'  # up to last content
            r',(\s*\))'                                             # trailing comma before )
        )
        fix_count = 0
        while pattern.search(code):
            code = pattern.sub(r'\1\2', code)
            fix_count += 1
            if fix_count > 50:
                break

        if fix_count > 0:
            self.fixes_applied.append(
                f"Removed trailing comma from {fix_count} parameter list(s)"
            )
        return code

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
        Add trailing comma to single-field named tuples in VALUE contexts only.
        
        P requires trailing comma for single-field tuples in value expressions:
          (field = value,)   not  (field = value)
        
        But NOT in type definitions or function parameter type annotations:
          type tFoo = (field: int);           // CORRECT — no trailing comma
          fun Bar(x: (field: machine)) {...}  // CORRECT — no trailing comma
        
        Handles: send payloads, new Machine(...), raise, assignments.
        """
        fix_count = 0

        # --- Value context only: (identifier = expression) without trailing comma ---
        # Matches (word = stuff) where "stuff" has no comma and no nested parens.
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

        # NOTE: We intentionally do NOT add trailing commas to type annotation
        # contexts like (field: type). P type definitions and function parameter
        # type annotations do NOT require trailing commas for single-field tuples.
        # Evidence: Tutorial/Advanced/4_Paxos and other examples all use
        # type tFoo = (field: type); without trailing commas.

        if fix_count > 0:
            self.fixes_applied.append(
                f"Added trailing comma to {fix_count} single-field named tuple value(s)"
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
        Fix test declaration syntax issues.
        
        Valid P test declaration forms:
          test Name [main=Driver]: assert SpecA in { Machine1, Machine2, Driver };
          test Name [main=Driver]: assert SpecA, SpecB in { Machine1, Driver };
          test Name [main=Driver]: { Machine1, Machine2, Driver };
        
        NOTE: 'assert SpecName in' is CORRECT P syntax — do NOT remove it.
        PChecker needs it to know which spec monitors to verify.
        """
        # We intentionally do NOT strip 'assert X in' — it's valid and required.
        # Only fix actual syntax errors if any are detected.
        return code
    
    def _fix_test_declaration_union_syntax(self, code: str) -> str:
        """
        Fix ``(union { ... })`` in test declarations to ``{ ... }``.

        LLMs frequently generate:
          test tc [main=M]: assert S in (union { A, B, C });
        But P requires:
          test tc [main=M]: assert S in { A, B, C };

        Also handles the variant without ``assert ... in``:
          test tc [main=M]: (union { A, B });  →  test tc [main=M]: { A, B };
        """
        # Pattern: (union { ... }) → { ... }
        # This can appear anywhere in a test declaration line
        pattern = re.compile(r'\(\s*union\s*(\{[^}]*\})\s*\)')
        matches = pattern.findall(code)
        if matches:
            code = pattern.sub(r'\1', code)
            self.fixes_applied.append(
                f"Fixed {len(matches)} test declaration(s): (union {{...}}) → {{...}}"
            )

        # Also fix missing semicolons at end of test declarations.
        # test tc [main=M]: assert S in { ... }  (no semicolon)
        # → test tc [main=M]: assert S in { ... };
        # Uses DOTALL so \s and [^}] can span newlines (multi-line decls).
        test_no_semi = re.compile(
            r'(test\s+\w+\s*\[main=\w+\]\s*:\s*'  # test header
            r'(?:assert\s+[\w,\s]+\s+in\s*)?'       # optional assert ... in
            r'\{[^}]*\})'                            # { machine list }
            r'(?!\s*;)'                              # NOT followed by ;
            r'(\s*(?:\n|$))',                         # end of line
            re.DOTALL,
        )
        semi_count = 0
        def _add_semi(m):
            nonlocal semi_count
            semi_count += 1
            return m.group(1) + ';' + m.group(2)
        code = test_no_semi.sub(_add_semi, code)
        if semi_count > 0:
            self.fixes_applied.append(
                f"Added missing semicolon to {semi_count} test declaration(s)"
            )

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


def post_process_file(code: str, filename: str = "") -> PostProcessResult:
    """Convenience function to post-process a single file."""
    processor = PCodePostProcessor()
    return processor.process(code, filename)
