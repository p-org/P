#!/usr/bin/env python3
"""merge_candidates.py — hybrid merge: union candidate sets from multiple proposers, dedup
by the structured property key (invariant_core.dedup_key), cluster-and-keep representatives.

Accepts any mix of:
  * proposer workflow results:  {"<benchmark>": {"dir": ..., "candidates": [...]}, ...}
    (the shape propose_templated.js / propose_intent.js return)
  * plain candidate lists:      [{...candidate...}, ...]

Usage:
    python3 merge_candidates.py --out merged.json templated.json intent.json [enumerative.json]

Output JSON: {"<benchmark>": {"dir": ..., "candidates": [representatives...],
              "clusters": [[names...], ...], "stats": {in, out, per_provenance}}}
The PInfer enumerative adapter (PLAN.md §6.5) emits the same shape, so plugging it in is
just another positional argument. Nothing is silently dropped: every duplicate is recorded
in its cluster, and stats carry the in/out counts the §8 reduction-ratio metric needs.
"""
import argparse
import json
import sys
from collections import Counter
from pathlib import Path
from typing import Dict, List, Tuple

sys.path.insert(0, str(Path(__file__).resolve().parent))
from invariant_core import Candidate, dedup_candidates  # noqa: E402


def load_by_benchmark(path: str) -> Dict[str, Tuple[str, List[Candidate]]]:
    """Normalize either accepted input shape to {benchmark: (dir, [Candidate...])}."""
    data = json.loads(Path(path).read_text())
    if isinstance(data, dict) and "result" in data:      # raw Workflow tool result wrapper
        data = data["result"]
    if isinstance(data, list):                            # plain candidate list
        return {"_default": ("", [Candidate.from_dict(c) for c in data])}
    out: Dict[str, Tuple[str, List[Candidate]]] = {}
    for bench, info in data.items():
        cands = info.get("candidates", []) if isinstance(info, dict) else info
        out[bench] = (info.get("dir", "") if isinstance(info, dict) else "",
                      [Candidate.from_dict(c) for c in cands])
    return out


def merge(paths: List[str]) -> Dict[str, Dict]:
    pooled: Dict[str, Tuple[str, List[Candidate]]] = {}
    for p in paths:
        for bench, (d, cands) in load_by_benchmark(p).items():
            prev_dir, prev = pooled.get(bench, ("", []))
            pooled[bench] = (prev_dir or d, prev + cands)

    result: Dict[str, Dict] = {}
    for bench, (d, cands) in pooled.items():
        reps, clusters = dedup_candidates(cands)
        result[bench] = {
            "dir": d,
            "candidates": [c.to_dict() for c in reps],
            "clusters": [[c.name for c in cl] for cl in clusters if len(cl) > 1],
            "stats": {
                "in": len(cands),
                "out": len(reps),
                "per_provenance": dict(Counter(c.provenance for c in cands)),
            },
        }
    return result


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("inputs", nargs="+", help="candidate JSON files (workflow results or lists)")
    ap.add_argument("--out", default="merged_candidates.json")
    args = ap.parse_args()

    result = merge(args.inputs)
    Path(args.out).write_text(json.dumps(result, indent=2))
    for bench, info in result.items():
        s = info["stats"]
        dup = ", ".join("+".join(cl) for cl in info["clusters"]) or "none"
        print(f"{bench}: {s['in']} in -> {s['out']} out "
              f"({s['per_provenance']}); duplicate clusters: {dup}")
    print(f"-> {args.out}")


if __name__ == "__main__":
    main()
