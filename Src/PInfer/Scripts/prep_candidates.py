#!/usr/bin/env python3
"""Turn a propose-invariants workflow result into per-benchmark candidate .p files.

Applies the deterministic P fixups that LLM-generated spec monitors most often need
(the same class PeasyAI's PCodePostProcessor handles): HTML-entity unescape, `x !in s`
-> `!(x in s)`, `for(` -> `foreach(`, and splitting inline `var x:T = e;` into a hoisted
declaration + an in-place assignment (P requires var decls at the top of a block).
"""
import html, json, re, sys
from pathlib import Path

HANDLER_RE = re.compile(r'(on\s+\w+\s+do\s*\([^)]*\)\s*|on\s+\w+\s+goto\s+\w+\s+with\s*\([^)]*\)\s*|entry\s*(?:\([^)]*\))?\s*|exit\s*)\{')
VARINIT_RE = re.compile(r'^(\s*)var\s+(\w+)\s*:\s*([^=;]+?)\s*=\s*(.+?);\s*$')
VARDECL_RE = re.compile(r'^(\s*)var\s+(\w+)\s*:\s*([^=;]+?)\s*;\s*$')


def hoist_block(body):
    """Within one handler body (no outer braces), hoist all var decls to the top,
    leaving assignments in place."""
    lines = body.split('\n')
    decls, rest = [], []
    for ln in lines:
        mi = VARINIT_RE.match(ln)
        md = VARDECL_RE.match(ln)
        if mi:
            indent, name, typ, expr = mi.groups()
            decls.append(f'{indent}var {name}: {typ.strip()};')
            rest.append(f'{indent}{name} = {expr.strip()};')
        elif md:
            indent, name, typ = md.groups()
            decls.append(f'{indent}var {name}: {typ.strip()};')
        else:
            rest.append(ln)
    return '\n'.join(decls + rest)


def fix_handlers(src):
    """Find each handler body by brace-matching and hoist its var decls."""
    out, i = [], 0
    for m in HANDLER_RE.finditer(src):
        head_end = m.end()  # position just after the '{'
        depth, j = 1, head_end
        while j < len(src) and depth:
            if src[j] == '{':
                depth += 1
            elif src[j] == '}':
                depth -= 1
            j += 1
        body = src[head_end:j - 1]
        if i <= m.start():
            out.append(src[i:head_end])
            out.append(hoist_block(body))
            out.append('}')
            i = j
    out.append(src[i:])
    return ''.join(out)


def clean(code):
    code = html.unescape(code)
    # `a !in b` -> `!(a in b)` (operands are simple identifiers / field / index accesses)
    code = re.sub(r'([\w.]+(?:\[[^\]]+\])?)\s*!in\s+([\w.]+(?:\[[^\]]+\])?)', r'!(\1 in \2)', code)
    code = re.sub(r'\bfor\s*\(', 'foreach (', code)
    return fix_handlers(code)


def main():
    data = json.loads(Path(sys.argv[1]).read_text())
    by = data.get('result', data)
    outdir = Path(sys.argv[2]) if len(sys.argv) > 2 else Path('/tmp')
    for bench, info in by.items():
        cands = info['candidates']
        blocks = []
        for c in cands:
            blocks.append(f"// [{c.get('lens','?')}/{c['predictedBucket']}] {c['name']}: {c['intent']}\n"
                          + clean(c['specCode']))
        (outdir / f"cand_{bench}.p").write_text('\n\n'.join(blocks) + '\n')
        print(f"{bench}: {len(cands)} candidates -> {outdir}/cand_{bench}.p  (dir={info['dir']})")


if __name__ == '__main__':
    main()
