#!/usr/bin/env python3
"""Verify LLM-repaired spec monitors: wire each fixed spec alone into its project and
recompile, reporting how many of the previously-broken candidates now compile.

Input JSON (the repair workflow result): [{benchmark, dir, name, fixedSource, changes?}]
Usage: python3 verify_repairs.py repairs.json
"""
import json, subprocess, sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from invariant_core import build_env  # noqa: E402  (single source of the dotnet/`p` resolver)


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
