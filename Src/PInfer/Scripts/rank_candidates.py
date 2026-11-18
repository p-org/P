#!/usr/bin/env python3
"""rank_candidates.py — deterministic half of the RANK stage (PLAN.md Phase 4).

The LLM half (scoring the four metrics) runs in whatever wrapper owns the provider —
the Claude Code skill, the PeasyAI MCP tool, or a Workflow agent. This CLI covers the
deterministic ends of that seam:

  1. --emit-prompt : build the ranking prompt for the merged candidate file, so the
     wrapper only has to relay it and collect RANK_OUTPUT_SCHEMA-shaped scores.
  2. --apply       : combine returned metric scores with the candidates' verdicts
     (compute_score kernel + verdict gating) and emit the final ranked report.

Usage:
    python3 rank_candidates.py --emit-prompt merged.json --benchmark FD --summary summary.txt
    python3 rank_candidates.py --apply merged.json --scores scores.json --out ranked.json

Only HOLDS-BOUNDED/HOLDS-PROVEN candidates are ranked; everything else is listed
unranked with its verdict, so nothing silently disappears from the report.
"""
import argparse
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from invariant_core import (Candidate, apply_scores, rank_system_prompt,  # noqa: E402
                            rank_user_prompt)


def load_candidates(path: str, benchmark: str = None):
    data = json.loads(Path(path).read_text())
    if isinstance(data, dict) and "result" in data:
        data = data["result"]
    if isinstance(data, list):
        return [Candidate.from_dict(c) for c in data]
    if benchmark is None:
        if len(data) != 1:
            sys.exit(f"--benchmark required: file has {sorted(data)}")
        benchmark = next(iter(data))
    return [Candidate.from_dict(c) for c in data[benchmark]["candidates"]]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--benchmark", help="benchmark key when the file holds several")
    mode = ap.add_mutually_exclusive_group(required=True)
    mode.add_argument("--emit-prompt", metavar="MERGED_JSON",
                      help="print system+user ranking prompts for these candidates")
    mode.add_argument("--apply", metavar="MERGED_JSON",
                      help="apply LLM metric scores to these candidates")
    ap.add_argument("--summary", help="path to a P-model summary text (for --emit-prompt)")
    ap.add_argument("--scores", help="RANK_OUTPUT_SCHEMA-shaped JSON (for --apply)")
    ap.add_argument("--out", default="ranked.json")
    args = ap.parse_args()

    if args.emit_prompt:
        cands = load_candidates(args.emit_prompt, args.benchmark)
        summary = Path(args.summary).read_text() if args.summary else "(no summary provided)"
        print(json.dumps({"system": rank_system_prompt(),
                          "user": rank_user_prompt(args.benchmark or "?", summary, cands)}))
        return

    cands = load_candidates(args.apply, args.benchmark)
    scores = json.loads(Path(args.scores).read_text())
    scores = scores.get("scores", scores)
    ranked, unranked = apply_scores(cands, scores)

    print(f"{'#':<4}{'CANDIDATE':<42}{'OVERALL':<9}gen/crit/dist/vis")
    print("-" * 100)
    for i, r in enumerate(ranked, 1):
        print(f"{i:<4}{r['name']:<42}{r['overall']:<9}"
              f"{r['generalization']}/{r['criticality']}/{r['distinguishability']}/{r['visibility']}")
    if unranked:
        print(f"\nUNRANKED ({len(unranked)}):")
        for u in unranked:
            print(f"    {u['name']:<42}{u['reason']}")

    Path(args.out).write_text(json.dumps({
        "ranked": [{k: v for k, v in r.items() if k != "candidate"} |
                   {"candidate": r["candidate"].to_dict()} for r in ranked],
        "unranked": [{"name": u["name"], "reason": u["reason"],
                      "candidate": u["candidate"].to_dict()} for u in unranked],
    }, indent=2))
    print(f"-> {args.out}")


if __name__ == "__main__":
    main()
