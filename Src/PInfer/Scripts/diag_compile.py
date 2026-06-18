#!/usr/bin/env python3
"""Diagnose WHY candidate spec monitors fail to compile.

Wires each `spec` block (from a candidates .p file) ALONE into its P project and
captures the compiler's first error, so the repair stage can fix each given the
exact diagnostic. Emits a JSON list [{benchmark,dir,name,source,error}].

Usage:
    python3 diag_compile.py --out fails.json \
        ClientServer=Tutorial/1_ClientServer=cand_ClientServer.p \
        FailureDetector=Tutorial/4_FailureDetector=cand_FailureDetector.p
each positional arg is  <label>=<projectDir>=<candidateFile>.
"""
import argparse, glob, json, os, re, shutil, subprocess, sys
from pathlib import Path

SPEC_RE = re.compile(r'^\s*spec\s+([A-Za-z_]\w*)', re.MULTILINE)
ERR_RE = re.compile(r'(error:|\[.*Error.*\]|Error:|expecting|mismatched|no viable|cannot|'
                    r'undeclared|not declared|Expected|duplicates|could not find)', re.IGNORECASE)


def build_env():
    """Same portable dotnet/`p` resolver as check_candidates.py."""
    env = dict(os.environ)
    tools = str(Path.home() / ".dotnet" / "tools")
    if tools not in env.get("PATH", ""):
        env["PATH"] = tools + os.pathsep + env.get("PATH", "")
    if shutil.which("dotnet", path=env["PATH"]):
        return env
    probes = ([env["DOTNET_ROOT"]] if env.get("DOTNET_ROOT") else []) + [
        "/usr/share/dotnet", "/usr/local/share/dotnet", str(Path.home() / ".dotnet"),
    ] + sorted(glob.glob("/opt/homebrew/Cellar/dotnet@*/*/libexec"))
    for root in probes:
        if Path(root, "dotnet").exists() or Path(root, "host").is_dir():
            env["DOTNET_ROOT"] = root
            env["PATH"] = root + os.pathsep + env["PATH"]
            return env
    return env


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default="compile_fails.json")
    ap.add_argument("targets", nargs="+", help="<label>=<projectDir>=<candidateFile> ...")
    args = ap.parse_args()
    env = build_env()

    fails = []
    for t in args.targets:
        label, d, candfile = t.split("=", 2)
        src = Path(candfile).read_text()
        spans = [(m.start(), m.group(1)) for m in SPEC_RE.finditer(src)] + [(len(src), None)]
        diag = Path(d) / "PSpec" / "_diag.p"
        for i in range(len(spans) - 1):
            name, block = spans[i][1], src[spans[i][0]:spans[i + 1][0]].strip()
            diag.write_text(block)
            out = subprocess.run(["p", "compile"], cwd=d, env=env, capture_output=True, text=True).stdout
            if "Compilation succeeded" not in out:
                errs = [l.strip() for l in out.splitlines() if ERR_RE.search(l)]
                first = next((l for l in errs if "0 Error" not in l and "Build succeeded" not in l), "")
                print(f"[{label}] {name}: {first[:150]}")
                fails.append({"benchmark": label, "dir": d, "name": name, "source": block, "error": first[:300]})
        diag.unlink(missing_ok=True)

    Path(args.out).write_text(json.dumps(fails, indent=2))
    print(f"\n{len(fails)} failing specs -> {args.out}")


if __name__ == "__main__":
    main()
