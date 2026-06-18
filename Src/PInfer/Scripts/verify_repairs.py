#!/usr/bin/env python3
"""Verify LLM-repaired spec monitors: wire each fixed spec alone into its project and
recompile, reporting how many of the previously-broken candidates now compile.

Input JSON (the repair workflow result): [{benchmark, dir, name, fixedSource, changes?}]
Usage: python3 verify_repairs.py repairs.json
"""
import glob, json, os, shutil, subprocess, sys
from pathlib import Path


def build_env():
    """Portable dotnet/`p` resolver (see check_candidates.py)."""
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
    env = build_env()
    data = json.loads(Path(sys.argv[1]).read_text())
    ok, bad = [], []
    for r in data:
        d = r["dir"]
        diag = Path(d) / "PSpec" / "_diag.p"
        diag.write_text(r["fixedSource"])
        out = subprocess.run(["p", "compile"], cwd=d, env=env, capture_output=True, text=True).stdout
        diag.unlink(missing_ok=True)
        if "Compilation succeeded" in out:
            ok.append(r["name"])
            print(f"  OK   {r['name']}  ({r.get('changes','')[:70]})")
        else:
            first = next((l.strip() for l in out.splitlines()
                          if "error" in l.lower() and "0 Error" not in l), "")
            bad.append((r["name"], first[:120]))
            print(f"  FAIL {r['name']}: {first[:100]}")
    print(f"\nRepaired-and-compiles: {len(ok)}/{len(data)}")
    if bad:
        print("Still failing (feed these errors back for another repair round):")
        for n, e in bad:
            print(f"  - {n}: {e}")


if __name__ == "__main__":
    main()
