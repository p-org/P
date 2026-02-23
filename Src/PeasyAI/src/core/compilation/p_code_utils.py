"""
Shared utilities for robust P code parsing.

Provides brace-balanced extraction and LLM response code extraction
that replaces fragile regex-based approaches.
"""

import re
import logging
from typing import Optional, Tuple, List, Dict

logger = logging.getLogger(__name__)


def find_balanced_brace(text: str, open_pos: int) -> int:
    """
    Find the position of the matching close-brace for the open-brace
    at *open_pos*.  Returns -1 if no match is found.

    Handles arbitrary nesting depth.
    """
    depth = 0
    for i in range(open_pos, len(text)):
        if text[i] == '{':
            depth += 1
        elif text[i] == '}':
            depth -= 1
            if depth == 0:
                return i
    return -1


def extract_block_body(text: str, open_pos: int) -> Tuple[str, int]:
    """
    Extract the body between ``{`` at *open_pos* and its matching ``}``.

    Returns ``(body_text, close_pos)`` or ``("", -1)`` if unbalanced.
    The body_text does NOT include the outer braces.
    """
    close = find_balanced_brace(text, open_pos)
    if close == -1:
        return "", -1
    return text[open_pos + 1:close], close


def extract_function_body(code: str, func_name: str) -> Optional[str]:
    """
    Extract the full body of ``fun <func_name>(...) { ... }`` using
    brace-balanced parsing.

    Handles nested parens in the parameter list (e.g., tuple types).

    Returns the body text (between the braces) or None if not found.
    """
    # Find "fun Name" then skip to the opening { of the body
    # (can't use [^)]* for params since they may contain nested parens)
    pattern = rf'\bfun\s+{re.escape(func_name)}\s*\('
    match = re.search(pattern, code)
    if not match:
        return None
    # Skip past the balanced parameter parens
    paren_close = find_balanced_brace.__wrapped__(code, match.end() - 1) if hasattr(find_balanced_brace, '__wrapped__') else _find_balanced_char(code, match.end() - 1, '(', ')')
    if paren_close == -1:
        return None
    # Now find the opening { after the param list (may have : ReturnType)
    rest = code[paren_close + 1:]
    brace_match = re.search(r'\s*(?::\s*\w+)?\s*\{', rest)
    if not brace_match:
        return None
    open_pos = paren_close + 1 + brace_match.end() - 1
    body, close = extract_block_body(code, open_pos)
    return body if body or close != -1 else None


def _find_balanced_char(text: str, open_pos: int, open_ch: str, close_ch: str) -> int:
    """Find matching close character for balanced open/close pairs."""
    depth = 0
    for i in range(open_pos, len(text)):
        if text[i] == open_ch:
            depth += 1
        elif text[i] == close_ch:
            depth -= 1
            if depth == 0:
                return i
    return -1


def extract_state_body(code: str, state_name: str, is_start: bool = False) -> Optional[str]:
    """
    Extract the full body of a state declaration using brace-balanced parsing.

    Handles ``start state Name { ... }`` and ``state Name { ... }``.

    Returns the body text (between the braces) or None if not found.
    """
    prefix = r'start\s+' if is_start else r'(?:start\s+)?'
    pattern = rf'\b{prefix}state\s+{re.escape(state_name)}\s*\{{'
    match = re.search(pattern, code)
    if not match:
        return None
    open_pos = match.end() - 1
    body, _ = extract_block_body(code, open_pos)
    return body if body or _ != -1 else None


def extract_start_state(code: str) -> Tuple[Optional[str], Optional[str]]:
    """
    Find the start state in a P machine and return ``(name, body)``.

    Returns ``(None, None)`` if no start state is found.
    """
    match = re.search(r'\bstart\s+state\s+(\w+)\s*\{', code)
    if not match:
        return None, None
    state_name = match.group(1)
    open_pos = match.end() - 1
    body, _ = extract_block_body(code, open_pos)
    if _ == -1:
        return state_name, None
    return state_name, body


def iter_function_bodies(code: str):
    """
    Yield ``(func_name, header, body, start_pos, end_pos)`` for every
    ``fun`` definition in *code*, using brace-balanced extraction.

    Handles nested parens in parameter lists (e.g., tuple types).
    """
    for match in re.finditer(r'fun\s+(\w+)\s*\(', code):
        func_name = match.group(1)
        func_start = match.start()
        # Skip past balanced parameter parens
        paren_close = _find_balanced_char(code, match.end() - 1, '(', ')')
        if paren_close == -1:
            continue
        # Find the opening { after params (may have : ReturnType)
        rest = code[paren_close + 1:]
        brace_match = re.search(r'\s*(?::\s*\w+)?\s*\{', rest)
        if not brace_match:
            continue
        open_pos = paren_close + 1 + brace_match.end() - 1
        header = code[func_start:open_pos]
        body, close_pos = extract_block_body(code, open_pos)
        if close_pos != -1:
            yield func_name, header, body, func_start, close_pos


# ── LLM response code extraction ────────────────────────────────────

def extract_p_code_from_response(
    response: str,
    expected_filename: Optional[str] = None,
) -> Tuple[Optional[str], Optional[str]]:
    """
    Extract P code and filename from an LLM response.

    Tries multiple strategies in order of specificity:
    1. XML-style tags: ``<Filename.p>...</Filename.p>``
    2. Markdown code block with filename comment
    3. Markdown code block (any)
    4. Raw P code (machine/spec/test block detection)

    If no filename can be determined from the response, falls back to
    *expected_filename* (converted to a safe .p filename).

    Returns ``(filename, code)`` or ``(None, None)``.
    """
    filename: Optional[str] = None
    code: Optional[str] = None

    # Strategy 1: XML tags  <Foo.p>...</Foo.p>  (case-insensitive tag)
    xml_match = re.search(r'<(\w+\.p)>(.*?)</\1>', response, re.DOTALL)
    if xml_match:
        filename = xml_match.group(1)
        code = xml_match.group(2).strip()

    # Strategy 2: Markdown with filename comment  ```p\n// Foo.p\n...```
    if not code:
        md_match = re.search(
            r'```(?:p|P)?\s*\n\s*//\s*(\w+\.p)\s*\n(.*?)```',
            response, re.DOTALL,
        )
        if md_match:
            filename = md_match.group(1)
            code = md_match.group(2).strip()

    # Strategy 3: Any markdown code block
    if not code:
        md_bare = re.search(r'```(?:p|P)?\s*\n(.*?)```', response, re.DOTALL)
        if md_bare:
            candidate = md_bare.group(1).strip()
            # Derive filename from first machine/spec/test keyword
            for kw_pattern, suffix in [
                (r'\bmachine\s+(\w+)', ''),
                (r'\bspec\s+(\w+)', ''),
                (r'\btest\s+(\w+)', ''),
            ]:
                nm = re.search(kw_pattern, candidate)
                if nm:
                    filename = f"{nm.group(1)}.p"
                    code = candidate
                    break
            if not code and candidate:
                code = candidate

    # Strategy 4: Raw P code — find the first top-level P construct
    if not code:
        for kw in [r'machine\s+\w+\s*\{', r'spec\s+\w+\s+observes',
                    r'test\s+\w+\s*\[', r'type\s+\w+\s*=', r'event\s+\w+']:
            m = re.search(kw, response)
            if m:
                # Take from the start of the match to the end of the response,
                # but strip trailing prose after the last closing brace.
                raw = response[m.start():]
                # Find the last balanced close-brace
                last_close = raw.rfind('}')
                if last_close != -1:
                    raw = raw[:last_close + 1]
                    # Try to find a trailing semicolon-terminated section
                    # (for type/event declarations that don't use braces)
                    trailing = response[m.start() + last_close + 1:]
                    extra = re.search(r'^[^{}]*?;', trailing)
                    if extra:
                        raw += extra.group(0)
                code = raw.strip()
                nm = re.search(r'\b(?:machine|spec)\s+(\w+)', code)
                if nm:
                    filename = f"{nm.group(1)}.p"
                break

    # Fallback filename from expected_name
    if code and not filename and expected_filename:
        safe = re.sub(r'[^\w]', '', expected_filename)
        filename = f"{safe}.p" if safe else None

    if not filename or not code:
        return None, None

    # Strip leftover markdown fences
    code = re.sub(r'^```\w*\s*', '', code)
    code = re.sub(r'\s*```\s*$', '', code)

    return filename, code
