#!/usr/bin/env python3
"""
check_candidates.py — CLI over the shared `invariant_core` engine: wire candidate spec
monitors into a P project, compile, bounded model-check, and classify each as
HOLDS / FAILS(+counterexample) / VACUOUS / INCONCLUSIVE / COMPILE-ERR.

A `<Name>_canary` companion spec (assert false in the same guarded branch) drives vacuity
detection; a failure owned by a pre-existing SUT assertion is reported INCONCLUSIVE.

    python3 check_candidates.py --project Tutorial/4_FailureDetector \
        --candidates fd_candidates.p --main TestMultipleClients \
        --assert-in "union { TestMultipleClients }, FailureDetector, FailureInjector" --iters 3000

This is the deterministic middle of the agentic loop (propose → check → judge). See
README_agentic_invariants.md; the engine lives in invariant_core.py.
"""
import argparse
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from invariant_core import validate_candidates  # noqa: E402


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--project", required=True, help="P project dir (contains the .pproj)")
    ap.add_argument("--candidates", required=True, help=".p file of candidate spec monitors")
    ap.add_argument("--main", required=True, help="test main machine name")
    ap.add_argument("--assert-in", required=True, help="module expression after 'in' (no trailing ';')")
    ap.add_argument("--iters", type=int, default=3000, help="schedules per candidate")
    ap.add_argument("--keep", action="store_true", help="leave generated files in place")
    args = ap.parse_args()

    src = Path(args.candidates).read_text()
    verdicts = validate_candidates(args.project, src, args.main, args.assert_in, args.iters, args.keep)

    print(f"{'CANDIDATE':<42}{'VERDICT':<17}DETAIL")
    print("-" * 100)
    for v in verdicts:
        print(f"{v.name:<42}{v.verdict:<17}{v.detail}")
    print("\nNext: an agent judges each FAILS as `bug` (required property) or `spurious` "
          "(over-strong), and each HOLDS-BOUNDED for vacuity/interestingness.")
    print(json.dumps([{"name": v.name, "verdict": v.verdict, "detail": v.detail} for v in verdicts]))


if __name__ == "__main__":
    main()
