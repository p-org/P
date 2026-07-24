#!/usr/bin/env python3
"""Unit tests for the pure (no-toolchain) parts of invariant_core.

Run: python3 Src/PInfer/Scripts/test_invariant_core.py

These cover the Phase-1 "frozen contract": the candidate schema (round-trip), the dedup key,
the judge output schema, JS<->Python schema parity, and the full verdict-classification matrix
of validate_candidates driven by mocked model-checking (so no `p` toolchain is needed). The
model-checking half itself is exercised end-to-end by check_candidates.py against the tutorials.
"""
import re
import sys
import unittest
from pathlib import Path
from unittest import mock

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
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

    def test_judge_prompt_includes_cex_and_structured_output(self):
        p = core.judge_user_prompt("FD_x", "FailureDetector", "TestMain",
                                   "Assertion Failed: ... false positive")
        self.assertIn("false positive", p)
        self.assertIn("development_action", p)

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
        v = core.Verdict("n", "HOLDS-BOUNDED")
        self.assertEqual(v.owner, "monitor")
        self.assertEqual(v.bugs, 0)

    def test_verdict_vocabulary_frozen(self):
        # Guard against accidental verdict renames — PLAN.md §6.4 depends on these exact strings.
        self.assertEqual(core.VERDICTS, {
            "HOLDS-BOUNDED", "HOLDS-PROVEN", "FAILS", "VACUOUS",
            "UNKNOWN-VACUITY", "INCONCLUSIVE", "COMPILE-ERR"})


class TestCandidateSchema(unittest.TestCase):
    def _sample(self) -> core.Candidate:
        return core.Candidate(
            name="TPC_agreement_atomicity", intent="all-or-nothing", category="atomicity",
            provenance="intent", observes=["eCommit", "eAbort"],
            formula=core.Formula(
                quantifiers=[{"var": "e0", "type": "eDecide", "kind": "forall"},
                             {"var": "e1", "type": "eVote", "kind": "exists"}],
                guards=["e0.transId == e1.transId"], relations=["e1.vote == SUCCESS"],
                sc={"op": "==", "bound": "numParticipants"}, config_event="eConfig",
                uses_index=True),
            spec_code="spec TPC_agreement_atomicity observes eCommit, eAbort { start state S {} }",
            canary=None, predicted_bucket="verified")

    def test_round_trip(self):
        c = self._sample()
        c2 = core.Candidate.from_dict(c.to_dict())
        self.assertEqual(c2.to_dict(), c.to_dict())
        self.assertEqual(c2.formula.arity, 1)  # one forall among the two quantifiers
        self.assertTrue(c2.formula.uses_index)
        self.assertEqual(c2.spec_code, c.spec_code)

    def test_from_dict_accepts_camelcase_specCode(self):
        c = core.Candidate.from_dict({"name": "X", "specCode": "spec X {}",
                                      "predictedBucket": "bug", "observes": ["eA"]})
        self.assertEqual(c.spec_code, "spec X {}")
        self.assertEqual(c.predicted_bucket, "bug")

    def test_verdict_serialized_when_present(self):
        c = self._sample()
        c.verdict = core.Verdict("TPC_agreement_atomicity", "HOLDS-BOUNDED", "ok")
        rt = core.Candidate.from_dict(c.to_dict())
        self.assertIsNotNone(rt.verdict)
        self.assertEqual(rt.verdict.verdict, "HOLDS-BOUNDED")


class TestDedupKey(unittest.TestCase):
    def _cand(self, name, observes, guards, relations, sc=None, kinds=("forall",)):
        return core.Candidate(name=name, observes=observes, formula=core.Formula(
            quantifiers=[{"kind": k} for k in kinds], guards=guards, relations=relations, sc=sc))

    def test_whitespace_and_order_insensitive(self):
        a = self._cand("a", ["eY", "eX"], ["e0.k == e1.k", "e0.t>0"], ["e0.v==e1.v"])
        b = self._cand("b", ["eX", "eY"], ["e0.t > 0", "e0.k==e1.k"], ["e0.v == e1.v"])
        self.assertEqual(core.dedup_key(a), core.dedup_key(b))

    def test_different_relation_distinguished(self):
        a = self._cand("a", ["eX"], ["e0.k==e1.k"], ["e0.v < e1.v"])
        b = self._cand("b", ["eX"], ["e0.k==e1.k"], ["e0.v <= e1.v"])
        self.assertNotEqual(core.dedup_key(a), core.dedup_key(b))

    def test_sc_distinguishes(self):
        a = self._cand("a", ["eX"], [], ["r"], sc={"op": "==", "bound": "N"})
        b = self._cand("b", ["eX"], [], ["r"], sc=None)
        self.assertNotEqual(core.dedup_key(a), core.dedup_key(b))

    def test_dedup_clusters_and_keeps_representative(self):
        a = self._cand("a", ["eX"], ["g"], ["r"])
        b = self._cand("b", ["eX"], ["g"], ["r"])          # same property as a
        c = self._cand("c", ["eX"], ["g"], ["r2"])          # distinct
        reps, clusters = core.dedup_candidates([a, b, c])
        self.assertEqual([r.name for r in reps], ["a", "c"])       # first-wins, nothing dropped
        self.assertEqual([len(cl) for cl in clusters], [2, 1])     # a+b cluster; c alone


class TestJudgeSchema(unittest.TestCase):
    def test_judge_output_schema_shape(self):
        s = core.JUDGE_OUTPUT_SCHEMA
        self.assertEqual(set(s["required"]),
                         {"verdict", "confidence", "cex_grounding", "development_action"})
        self.assertEqual(set(s["properties"]["development_action"]["enum"]),
                         core.DEVELOPMENT_ACTIONS)
        self.assertEqual(set(s["properties"]["verdict"]["enum"]), core.JUDGE_VERDICTS)


class TestSchemaParity(unittest.TestCase):
    """The proposer output contract lives in propose_templated.js; the consumer contract lives in
    the Python Candidate dataclass. This guards drift between the two (PLAN.md §5, N1)."""
    def setUp(self):
        self.js = (HERE / "propose_templated.js").read_text()

    def test_python_fields_present_in_js_schema(self):
        for field in core.CANDIDATE_FIELDS:
            self.assertIn(field, self.js, f"{field} missing from propose_templated.js CAND_SCHEMA")

    def test_js_required_fields_are_known_to_python(self):
        m = re.search(r"required:\s*\[([^\]]*)\]", self.js)  # first required[] is the candidate's
        self.assertIsNotNone(m)
        required = set(re.findall(r"'([^']+)'", m.group(1)))
        # The candidates-wrapper required is ['candidates']; the item required is the real one.
        item = re.findall(r"required:\s*\[([^\]]*)\]", self.js)
        fields = set(re.findall(r"'([^']+)'", item[1]))
        self.assertTrue(fields.issubset(set(core.CANDIDATE_FIELDS)),
                        f"JS-required fields not modeled in Python: {fields - set(core.CANDIDATE_FIELDS)}")


class TestProposerParity(unittest.TestCase):
    """invariant_core.INTENT_LENSES is the source of truth; propose_intent.js mirrors it
    (same pattern as TestSchemaParity). Guards lens drift between Python and the workflow."""
    def setUp(self):
        self.js = (HERE / "propose_intent.js").read_text()

    def test_all_lenses_present_in_js(self):
        for key, _focus in core.INTENT_LENSES:
            self.assertIn(f"key: '{key}'", self.js, f"lens '{key}' missing from propose_intent.js")

    def test_js_declares_intent_provenance_and_schema_fields(self):
        self.assertIn("provenance: 'intent'", self.js)
        for field in ("formula", "canary", "specCode", "predictedBucket"):
            self.assertIn(field, self.js)


class TestMergeCandidates(unittest.TestCase):
    def setUp(self):
        import merge_candidates
        self.mc = merge_candidates

    def _write(self, tmp: Path, name: str, payload) -> str:
        p = tmp / name
        p.write_text(__import__("json").dumps(payload))
        return str(p)

    def _cand(self, name, provenance, guards, relations):
        return {"name": name, "intent": "", "category": "", "provenance": provenance,
                "observes": ["eX"], "specCode": f"spec {name} observes eX {{}}",
                "predictedBucket": "verified",
                "formula": {"quantifiers": [{"kind": "forall"}], "guards": guards,
                            "relations": relations, "sc": None, "config_event": None,
                            "uses_index": False}}

    def test_merge_dedups_across_proposers_and_keeps_stats(self):
        import tempfile
        with tempfile.TemporaryDirectory() as td:
            tmp = Path(td)
            templated = {"FD": {"dir": "Tutorial/4_FailureDetector", "candidates": [
                self._cand("FD_tmpl_mono", "templated", ["e0.k==e1.k"], ["e0.v <= e1.v"]),
                self._cand("FD_tmpl_uniq", "templated", [], ["e0.id != e1.id"])]}}
            intent = {"FD": {"dir": "Tutorial/4_FailureDetector", "candidates": [
                # same property as FD_tmpl_mono, different name/phrasing/whitespace
                self._cand("FD_intent_mono", "intent", ["e0.k == e1.k"], ["e0.v<=e1.v"]),
                self._cand("FD_intent_resp", "intent", ["g"], ["r"])]}}
            merged = self.mc.merge([self._write(tmp, "t.json", templated),
                                    self._write(tmp, "i.json", intent)])
        fd = merged["FD"]
        self.assertEqual(fd["stats"], {"in": 4, "out": 3,
                                       "per_provenance": {"templated": 2, "intent": 2}})
        names = [c["name"] for c in fd["candidates"]]
        self.assertEqual(names, ["FD_tmpl_mono", "FD_tmpl_uniq", "FD_intent_resp"])
        self.assertEqual(fd["clusters"], [["FD_tmpl_mono", "FD_intent_mono"]])  # nothing silently dropped
        self.assertEqual(fd["dir"], "Tutorial/4_FailureDetector")

    def test_merge_accepts_plain_list(self):
        import tempfile
        with tempfile.TemporaryDirectory() as td:
            tmp = Path(td)
            merged = self.mc.merge([self._write(tmp, "l.json",
                                                [self._cand("A", "intent", [], ["r"])])])
        self.assertEqual(merged["_default"]["stats"]["in"], 1)


class TestRanking(unittest.TestCase):
    def _cand(self, name, verdict=None):
        c = core.Candidate(name=name, intent=f"intent of {name}", observes=["eX"],
                           formula=core.Formula(guards=["g"], relations=["r"]))
        if verdict:
            c.verdict = core.Verdict(name, verdict)
        return c

    def test_compute_score_kernel(self):
        # sqrt(0.9*0.6)*0.8 + 0.5*0.2 + 1.0*0.0  (ported formula, default weights)
        self.assertEqual(core.compute_score(0.9, 0.6, 0.5, 1.0),
                         round((0.9 * 0.6) ** 0.5 * 0.8 + 0.5 * 0.2, 4))

    def test_compute_score_validates_range(self):
        with self.assertRaises(ValueError):
            core.compute_score(1.2, 0.5, 0.5, 0.5)

    def test_compute_score_custom_weights(self):
        w = {"quality": 0.0, "distinguishability": 1.0, "visibility": 0.0}
        self.assertEqual(core.compute_score(0.1, 0.1, 0.7, 0.0, weights=w), 0.7)

    def test_apply_scores_gates_and_sorts(self):
        cands = [self._cand("low", "HOLDS-BOUNDED"), self._cand("high", "HOLDS-BOUNDED"),
                 self._cand("vac", "VACUOUS"), self._cand("fails", "FAILS"),
                 self._cand("unvalidated"), self._cand("noscores", "HOLDS-BOUNDED")]
        scores = [
            {"name": "low", "generalization": 0.2, "criticality": 0.2,
             "distinguishability": 0.2, "visibility": 0.0},
            {"name": "high", "generalization": 0.9, "criticality": 0.9,
             "distinguishability": 0.9, "visibility": 0.0},
            {"name": "vac", "generalization": 1.0, "criticality": 1.0,   # gated out anyway
             "distinguishability": 1.0, "visibility": 1.0},
        ]
        ranked, unranked = core.apply_scores(cands, scores)
        self.assertEqual([r["name"] for r in ranked], ["high", "low"])
        reasons = {u["name"]: u["reason"] for u in unranked}
        self.assertEqual(reasons["vac"], "verdict=VACUOUS")          # perfect scores can't rescue it
        self.assertEqual(reasons["fails"], "verdict=FAILS")
        self.assertEqual(reasons["unvalidated"], "verdict=unvalidated")
        self.assertEqual(reasons["noscores"], "no metric scores returned")

    def test_rank_prompts_and_schema(self):
        c = self._cand("FD_x", "HOLDS-BOUNDED")
        up = core.rank_user_prompt("FD", "summary text", [c])
        self.assertIn("FD_x", up)
        self.assertIn("summary text", up)
        for m in core.RANK_METRICS:
            self.assertIn(m, core.rank_system_prompt().lower())
        item = core.RANK_OUTPUT_SCHEMA["properties"]["scores"]["items"]
        self.assertEqual(set(item["required"]), {"name", *core.RANK_METRICS})


class TestClassificationMatrix(unittest.TestCase):
    """validate_candidates classification, every branch, with model-checking mocked out."""

    SRC = (
        "spec HB observes eX {}\n"          # holds, canary trips -> HOLDS-BOUNDED
        "spec HB_canary observes eX {}\n"
        "spec VAC observes eX {}\n"          # holds, canary holds -> VACUOUS
        "spec VAC_canary observes eX {}\n"
        "spec UNKC observes eX {}\n"         # holds, canary errored -> UNKNOWN-VACUITY
        "spec UNKC_canary observes eX {}\n"
        "spec NOCAN observes eX {}\n"        # holds, no canary -> UNKNOWN-VACUITY
        "spec FAIL observes eX {}\n"         # bugs, monitor-owned -> FAILS
        "spec INC observes eX {}\n"          # bugs, sut-owned -> INCONCLUSIVE
    )
    CHECK = {  # name -> (bugs, cex, owner)
        "HB": (0, "", "monitor"), "HB_canary": (1, "", "monitor"),
        "VAC": (0, "", "monitor"), "VAC_canary": (0, "", "monitor"),
        "UNKC": (0, "", "monitor"), "UNKC_canary": (-1, "COMPILE-ERROR", "monitor"),
        "NOCAN": (0, "", "monitor"),
        "FAIL": (1, "Assertion Failed: _candidates.p", "monitor"),
        "INC": (1, "Assertion Failed: SUT", "sut"),
    }

    def _run(self, wire_ok=True):
        with mock.patch.object(core, "build_env", return_value={}), \
             mock.patch.object(core, "_wire", return_value=wire_ok), \
             mock.patch.object(core, "static_gate", return_value={}), \
             mock.patch.object(core, "_baseline_failures", return_value=frozenset()), \
             mock.patch.object(core, "_check_one",
                               side_effect=lambda proj, n, iters, env, base: self.CHECK[n]):
            return {v.name: v for v in core.validate_candidates(
                "/tmp/does-not-exist-proj", self.SRC, "TestMain", "M", iters=10)}

    def test_all_branches(self):
        v = self._run()
        self.assertEqual(v["HB"].verdict, "HOLDS-BOUNDED")
        self.assertEqual(v["VAC"].verdict, "VACUOUS")
        self.assertEqual(v["UNKC"].verdict, "UNKNOWN-VACUITY")
        self.assertEqual(v["NOCAN"].verdict, "UNKNOWN-VACUITY")
        self.assertEqual(v["FAIL"].verdict, "FAILS")
        self.assertEqual(v["INC"].verdict, "INCONCLUSIVE")
        # canaries are probes, not reported as real candidates
        self.assertNotIn("HB_canary", v)

    def test_all_emitted_verdicts_are_in_vocabulary(self):
        for verdict in self._run().values():
            self.assertIn(verdict.verdict, core.VERDICTS)

    def test_compile_error_branch(self):
        # Batch wire fails, and single-wire fails only for CE.
        src = "spec OK observes eX {}\nspec OK_canary observes eX {}\nspec CE observes eX {}\n"
        check = {"OK": (0, "", "monitor"), "OK_canary": (1, "", "monitor")}

        def fake_wire(project, names, block_of, main, assert_in, env):
            if len(names) > 1:
                return False               # force the isolate path
            return names[0] != "CE"        # CE never compiles

        with mock.patch.object(core, "build_env", return_value={}), \
             mock.patch.object(core, "_wire", side_effect=fake_wire), \
             mock.patch.object(core, "static_gate", return_value={}), \
             mock.patch.object(core, "_baseline_failures", return_value=frozenset()), \
             mock.patch.object(core, "_check_one",
                               side_effect=lambda proj, n, iters, env, base: check[n]):
            v = {x.name: x for x in core.validate_candidates(
                "/tmp/does-not-exist-proj", src, "TestMain", "M", iters=10)}
        self.assertEqual(v["CE"].verdict, "COMPILE-ERR")
        self.assertEqual(v["OK"].verdict, "HOLDS-BOUNDED")


class TestAutoCanary(unittest.TestCase):
    def test_replaces_assert_and_renames(self):
        block = ('spec M observes eX {\n  start state S {\n    on eX do (p: tP) {\n'
                 '      if (p.v > 0) { assert p.v < 10, format("bad; v={0}", p.v); }\n'
                 '    }\n  }\n}\n')
        canary = core.make_canary("M", block)
        self.assertIsNotNone(canary)
        self.assertIn("spec M_canary", canary)
        self.assertNotIn("p.v < 10", canary)                    # original condition gone
        self.assertIn('assert false, "canary";', canary)
        self.assertIn("if (p.v > 0)", canary)                    # guard preserved
        # the `;` inside the format string must not have truncated the replacement
        self.assertNotIn("bad; v=", canary)

    def test_no_assert_means_no_canary(self):
        self.assertIsNone(core.make_canary("L", "spec L observes eX { start state S { } }"))

    def test_multiple_asserts_all_replaced(self):
        block = "spec M observes eX { assert a == b; assert c > d, \"m\"; }"
        _, n = core._replace_asserts(block)
        self.assertEqual(n, 2)

    def test_validate_autogenerates_canary(self):
        # One candidate WITH an assert and NO proposer canary: auto-canary is derived and,
        # since it trips (guard reachable), the candidate is HOLDS-BOUNDED, not UNKNOWN-VACUITY.
        src = "spec A observes eX {\n  start state S { on eX do (p: tP) { assert p.v > 0; } }\n}\n"
        check = {"A": (0, "", "monitor"), "A_canary": (1, "", "monitor")}
        with mock.patch.object(core, "build_env", return_value={}), \
             mock.patch.object(core, "_wire", return_value=True), \
             mock.patch.object(core, "static_gate", return_value={}), \
             mock.patch.object(core, "_baseline_failures", return_value=frozenset()), \
             mock.patch.object(core, "_check_one",
                               side_effect=lambda proj, n, iters, env, base: check[n]):
            v = {x.name: x for x in core.validate_candidates(
                "/tmp/does-not-exist-proj", src, "TestMain", "M", iters=10)}
        self.assertEqual(v["A"].verdict, "HOLDS-BOUNDED")


class TestAttribution(unittest.TestCase):
    def test_candidates_file_is_definitively_monitor(self):
        self.assertEqual(core._attribute_owner(
            "Assertion Failed: _candidates.p:12", frozenset({"anything"})), "monitor")

    def test_preexisting_signature_is_sut(self):
        base = frozenset({"Assertion Failed: Atomicity.p:33 all-or-nothing"})
        self.assertEqual(core._attribute_owner(
            "Assertion Failed: Atomicity.p:33  all-or-nothing", base), "sut")

    def test_novel_deadlock_is_monitor(self):
        # The M5 regression: a monitor-induced deadlock has no trigger substring and no
        # baseline match — must be attributed to the monitor, not silently dropped as SUT.
        self.assertEqual(core._attribute_owner(
            "Deadlock detected. TestMain is waiting", frozenset()), "monitor")

    def test_empty_cex_defaults_to_monitor(self):
        self.assertEqual(core._attribute_owner("", frozenset({"sig"})), "monitor")


class TestStaticGate(unittest.TestCase):
    def _project(self, tmp: Path) -> Path:
        (tmp / "PSrc").mkdir()
        (tmp / "PSrc" / "decls.p").write_text(
            "type tPing = (fd: machine, trial: int);\n"
            "event ePing: tPing;\n"
            "event ePong: (node: machine);\n"
            "event eShutdown;\n")
        return tmp

    def test_clean_candidate_passes(self):
        import tempfile
        with tempfile.TemporaryDirectory() as td:
            proj = self._project(Path(td))
            block = ("spec OK observes ePing, ePong {\n  start state S {\n"
                     "    on ePing do (p: tPing) { assert p.trial >= 0; }\n"
                     "    on ePong do (q: (node: machine)) { assert q.node == q.node; }\n  }\n}")
            self.assertEqual(core.static_gate(str(proj), {"OK": block}), {})

    def test_flags_undeclared_event_unobserved_handler_and_phantom_field(self):
        import tempfile
        with tempfile.TemporaryDirectory() as td:
            proj = self._project(Path(td))
            block = ("spec BAD observes ePing, eNoSuch {\n  start state S {\n"
                     "    on ePing do (p: tPing) { assert p.attempt >= 0; }\n"
                     "    on ePong do (q: (node: machine)) { }\n  }\n}")
            errs = core.static_gate(str(proj), {"BAD": block})["BAD"]
        joined = " | ".join(errs)
        self.assertIn("eNoSuch", joined)                       # undeclared observed event
        self.assertIn("not in the observes list", joined)       # ePong handled, not observed
        self.assertIn("p.attempt", joined)                      # phantom payload field
        self.assertIn("trial", joined)                          # ...with the real fields named

    def test_gated_candidate_becomes_compile_err_with_diagnostic(self):
        import tempfile
        src = ("spec BAD observes eNoSuch { start state S { } }\n"
               "spec OK observes ePing {\n  start state S { on ePing do (p: tPing) "
               "{ assert p.trial >= 0; } }\n}\n")
        check = {"OK": (0, "", "monitor"), "OK_canary": (1, "", "monitor")}
        with tempfile.TemporaryDirectory() as td:
            proj = self._project(Path(td))
            with mock.patch.object(core, "build_env", return_value={}), \
                 mock.patch.object(core, "_wire", return_value=True), \
                 mock.patch.object(core, "_baseline_failures", return_value=frozenset()), \
                 mock.patch.object(core, "_check_one",
                                   side_effect=lambda p, n, iters, env, base: check[n]):
                v = {x.name: x for x in core.validate_candidates(
                    str(proj), src, "TestMain", "M", iters=10)}
        self.assertEqual(v["BAD"].verdict, "COMPILE-ERR")
        self.assertIn("static-gate:", v["BAD"].detail)
        self.assertIn("eNoSuch", v["BAD"].detail)
        self.assertEqual(v["OK"].verdict, "HOLDS-BOUNDED")     # gate didn't block the good one


if __name__ == "__main__":
    unittest.main(verbosity=2)
