#!/usr/bin/env python3
"""Unit tests for the pure (no-toolchain) parts of invariant_core.

Run: python3 Src/PInfer/Scripts/test_invariant_core.py
(The model-checking half of invariant_core needs `p` and a P project; it is exercised
end-to-end by check_candidates.py against the tutorials.)
"""
import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import invariant_core as core


class TestSplitSpecs(unittest.TestCase):
    def test_splits_multiple(self):
        src = (
            "spec A observes eX {\n  start state S { on eX do {} }\n}\n\n"
            "spec B_canary observes eY {\n  start state S { on eY do {} }\n}\n"
        )
        blocks = core.split_specs(src)
        self.assertEqual(set(blocks), {"A", "B_canary"})
        self.assertIn("spec A", blocks["A"])
        self.assertIn("spec B_canary", blocks["B_canary"])

    def test_empty(self):
        self.assertEqual(core.split_specs("// no specs here"), {})


class TestPromptBuilders(unittest.TestCase):
    def test_propose_prompt_mentions_target_and_grammar(self):
        p = core.propose_user_prompt("FailureDetector", "Tutorial/4_FailureDetector",
                                     "ePing: (fd, trial)", "tmpl_arity1", "single-event invariants")
        self.assertIn("FailureDetector", p)
        self.assertIn("forall", p.lower())
        self.assertIn("JSON array", p)

    def test_judge_prompt_includes_cex(self):
        p = core.judge_user_prompt("FD_x", "FailureDetector", "TestMain",
                                   "Assertion Failed: ... false positive")
        self.assertIn("false positive", p)
        self.assertIn("bug", p.lower())

    def test_repair_prompt_includes_error_and_rules(self):
        p = core.repair_user_prompt("S", "B", "dir", "mismatched input '='", "spec S {}")
        self.assertIn("mismatched input", p)
        self.assertIn("NO inline var init", p)

    def test_priors_nonempty(self):
        self.assertTrue(core.TEMPLATE_SHAPES and core.INTENT_LENSES)
        self.assertTrue(all(len(t) == 2 for t in core.TEMPLATE_SHAPES))


class TestEnv(unittest.TestCase):
    def test_build_env_has_path(self):
        env = core.build_env()
        self.assertIn("PATH", env)
        self.assertIn(".dotnet", env["PATH"])  # global tools dir is prepended


class TestVerdictModel(unittest.TestCase):
    def test_verdict_defaults(self):
        v = core.Verdict("n", "HOLDS")
        self.assertEqual(v.owner, "monitor")
        self.assertEqual(v.bugs, 0)


if __name__ == "__main__":
    unittest.main(verbosity=2)
