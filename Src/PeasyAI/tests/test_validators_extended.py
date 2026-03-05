"""
Extended validator tests — covers the 7 validators and PCodePostProcessor fixes
that were previously untested.

Validators: InlineInitValidator, VarDeclarationOrderValidator, CollectionOpsValidator,
            SpecObservesConsistencyValidator, DuplicateDeclarationValidator,
            SpecForbiddenKeywordValidator, PayloadFieldValidator, TestFileValidator

PostProcessor: var reordering, enum dot-access, entry function syntax, bare halt,
               forbidden keywords in spec, missing semicolons after return
"""

import sys
from pathlib import Path

import pytest

PROJECT_ROOT = Path(__file__).parent.parent
sys.path.insert(0, str(PROJECT_ROOT / "src"))

from core.validation.validators import (
    InlineInitValidator,
    VarDeclarationOrderValidator,
    CollectionOpsValidator,
    SpecObservesConsistencyValidator,
    DuplicateDeclarationValidator,
    SpecForbiddenKeywordValidator,
    PayloadFieldValidator,
    TestFileValidator,
    IssueSeverity,
)
from core.compilation.p_post_processor import PCodePostProcessor


# ═══════════════════════════════════════════════════════════════════════
# InlineInitValidator
# ═══════════════════════════════════════════════════════════════════════

class TestInlineInitValidator:

    def test_detects_inline_init(self):
        code = "    var x: int = 0;"
        v = InlineInitValidator()
        r = v.validate(code)
        assert not r.is_valid
        assert any("Inline initialization" in i.message for i in r.issues)

    def test_auto_fixes_inline_init(self):
        code = "    var x: int = 0;"
        v = InlineInitValidator()
        r = v.validate(code)
        fixed = code
        for issue in r.issues:
            if issue.auto_fixable:
                fixed = issue.apply_fix(fixed)
        assert "var x: int;" in fixed
        assert "x = 0;" in fixed

    def test_plain_declaration_passes(self):
        code = "var x: int;\nx = 0;"
        v = InlineInitValidator()
        r = v.validate(code)
        assert r.is_valid

    def test_multiple_inline_inits(self):
        code = "var a: int = 1;\nvar b: string = \"hello\";"
        v = InlineInitValidator()
        r = v.validate(code)
        assert len(r.issues) == 2


# ═══════════════════════════════════════════════════════════════════════
# VarDeclarationOrderValidator
# ═══════════════════════════════════════════════════════════════════════

class TestVarDeclarationOrderValidator:

    def test_all_vars_after_statements_flagged(self):
        """All vars after all statements should be flagged."""
        code = (
            "machine M {\n"
            "  start state Init {\n"
            "    entry {\n"
            "      x = 1;\n"
            "      var y: int;\n"
            "      y = 2;\n"
            "    }\n"
            "  }\n"
            "}\n"
        )
        v = VarDeclarationOrderValidator()
        r = v.validate(code)
        assert any("Variable declaration after statement" in i.message for i in r.issues)

    def test_interleaved_vars_flagged(self):
        """Vars interleaved with statements (var, stmt, var) should be flagged."""
        code = (
            "machine M {\n"
            "  start state Init {\n"
            "    entry {\n"
            "      var x: int;\n"
            "      x = 1;\n"
            "      var y: int;\n"
            "      y = 2;\n"
            "    }\n"
            "  }\n"
            "}\n"
        )
        v = VarDeclarationOrderValidator()
        r = v.validate(code)
        assert any("Variable declaration after statement" in i.message for i in r.issues)

    def test_vars_at_top_no_error(self):
        """Properly ordered vars (all at top) should not trigger errors."""
        code = (
            "machine M {\n"
            "  start state Init {\n"
            "    entry {\n"
            "      var x: int;\n"
            "      var y: int;\n"
            "      x = 1;\n"
            "      y = 2;\n"
            "    }\n"
            "  }\n"
            "}\n"
        )
        v = VarDeclarationOrderValidator()
        r = v.validate(code)
        var_order_errors = [i for i in r.issues if "Variable declaration after statement" in i.message]
        assert len(var_order_errors) == 0

    def test_vars_before_statements_pass(self):
        code = (
            "fun DoWork() {\n"
            "  var x: int;\n"
            "  var y: int;\n"
            "  x = 1;\n"
            "  y = 2;\n"
            "}\n"
        )
        v = VarDeclarationOrderValidator()
        r = v.validate(code)
        errors = [i for i in r.issues if i.severity == IssueSeverity.ERROR]
        assert len(errors) == 0

    def test_var_inside_loop_flagged(self):
        code = (
            "fun Process() {\n"
            "  var i: int;\n"
            "  while (i < 10) {\n"
            "    var temp: int;\n"
            "    temp = i;\n"
            "  }\n"
            "}\n"
        )
        v = VarDeclarationOrderValidator()
        r = v.validate(code)
        assert any("inside a loop" in i.message for i in r.issues)


# ═══════════════════════════════════════════════════════════════════════
# CollectionOpsValidator
# ═══════════════════════════════════════════════════════════════════════

class TestCollectionOpsValidator:

    def test_append_function_flagged(self):
        code = "append(mySeq, value);"
        v = CollectionOpsValidator()
        r = v.validate(code)
        assert not r.is_valid
        assert any("append()" in i.message for i in r.issues)

    def test_receive_function_flagged(self):
        code = "var msg: int;\nmsg = receive();"
        v = CollectionOpsValidator()
        r = v.validate(code)
        assert not r.is_valid
        assert any("receive()" in i.message for i in r.issues)

    def test_wrong_seq_concat_warned(self):
        code = "mySeq = mySeq + (elem,);"
        v = CollectionOpsValidator()
        r = v.validate(code)
        assert any("concatenation" in i.message.lower() for i in r.issues)

    def test_valid_seq_append_passes(self):
        code = "mySeq += (sizeof(mySeq), elem);"
        v = CollectionOpsValidator()
        r = v.validate(code)
        assert r.is_valid


# ═══════════════════════════════════════════════════════════════════════
# SpecObservesConsistencyValidator
# ═══════════════════════════════════════════════════════════════════════

class TestSpecObservesConsistencyValidator:

    def test_handled_but_not_observed_flagged(self):
        code = """
event eStart;
event eStop;
spec Safety observes eStart {
    start state Init {
        on eStart goto Running;
    }
    state Running {
        on eStop goto Init;
    }
}
"""
        v = SpecObservesConsistencyValidator()
        r = v.validate(code)
        assert not r.is_valid
        assert any("eStop" in i.message and "does not list" in i.message for i in r.issues)

    def test_consistent_spec_passes(self):
        code = """
event eStart;
event eStop;
spec Safety observes eStart, eStop {
    start state Init {
        on eStart goto Running;
    }
    state Running {
        on eStop goto Init;
    }
}
"""
        v = SpecObservesConsistencyValidator()
        r = v.validate(code)
        assert r.is_valid

    def test_observes_undefined_event_warned(self):
        code = """
spec Safety observes eNonExistent {
    start state Init { }
}
"""
        v = SpecObservesConsistencyValidator()
        r = v.validate(code)
        assert any("undefined event" in i.message.lower() for i in r.issues)

    def test_event_from_context_recognized(self):
        code = """
spec Safety observes eStart {
    start state Init {
        on eStart goto Done;
    }
    state Done { }
}
"""
        context = {"types.p": "event eStart;"}
        v = SpecObservesConsistencyValidator()
        r = v.validate(code, context)
        assert r.is_valid


# ═══════════════════════════════════════════════════════════════════════
# DuplicateDeclarationValidator
# ═══════════════════════════════════════════════════════════════════════

class TestDuplicateDeclarationValidator:

    def test_duplicate_type_flagged(self):
        code = "type tConfig = (x: int);"
        context = {"other.p": "type tConfig = (y: int);"}
        v = DuplicateDeclarationValidator()
        r = v.validate(code, context)
        assert not r.is_valid
        assert any("tConfig" in i.message for i in r.issues)

    def test_duplicate_event_flagged(self):
        code = "event eStart;"
        context = {"types.p": "event eStart;"}
        v = DuplicateDeclarationValidator()
        r = v.validate(code, context)
        assert not r.is_valid

    def test_duplicate_machine_flagged(self):
        code = "machine Server {"
        context = {"server.p": "machine Server {"}
        v = DuplicateDeclarationValidator()
        r = v.validate(code, context)
        assert not r.is_valid

    def test_no_duplicates_passes(self):
        code = "type tFoo = (x: int);"
        context = {"other.p": "type tBar = (y: int);"}
        v = DuplicateDeclarationValidator()
        r = v.validate(code, context)
        assert r.is_valid

    def test_no_context_passes(self):
        code = "type tFoo = (x: int);\nevent eFoo;"
        v = DuplicateDeclarationValidator()
        r = v.validate(code)
        assert r.is_valid


# ═══════════════════════════════════════════════════════════════════════
# SpecForbiddenKeywordValidator
# ═══════════════════════════════════════════════════════════════════════

class TestSpecForbiddenKeywordValidator:

    def test_this_in_spec_flagged(self):
        code = """
spec Monitor observes eEvent {
    start state Init {
        entry {
            var m: machine;
            m = this;
        }
    }
}
"""
        v = SpecForbiddenKeywordValidator()
        r = v.validate(code)
        assert not r.is_valid
        assert any("this" in i.message for i in r.issues)

    def test_send_in_spec_flagged(self):
        code = """
spec Monitor observes eEvent {
    start state Init {
        on eEvent do {
            send target, eOther;
        }
    }
}
"""
        v = SpecForbiddenKeywordValidator()
        r = v.validate(code)
        assert not r.is_valid
        assert any("send" in i.message for i in r.issues)

    def test_new_in_spec_flagged(self):
        code = """
spec Monitor observes eEvent {
    start state Init {
        entry {
            var m: machine;
            m = new Helper();
        }
    }
}
"""
        v = SpecForbiddenKeywordValidator()
        r = v.validate(code)
        assert not r.is_valid
        assert any("new" in i.message for i in r.issues)

    def test_clean_spec_passes(self):
        code = """
event eCommit;
event eAbort;
spec Atomicity observes eCommit, eAbort {
    var committed: bool;
    start state Init {
        on eCommit goto Committed;
        on eAbort goto Aborted;
    }
    state Committed { }
    state Aborted { }
}
"""
        v = SpecForbiddenKeywordValidator()
        r = v.validate(code)
        assert r.is_valid

    def test_non_spec_code_passes(self):
        code = """
machine Worker {
    start state Init {
        entry {
            send coordinator, eReady;
        }
    }
}
"""
        v = SpecForbiddenKeywordValidator()
        r = v.validate(code)
        assert r.is_valid


# ═══════════════════════════════════════════════════════════════════════
# PayloadFieldValidator
# ═══════════════════════════════════════════════════════════════════════

class TestPayloadFieldValidator:

    def test_wrong_field_name_warned(self):
        code = """
fun HandleRequest(req: tRequest) {
    var x: int;
    x = req.wrongField;
}
"""
        context = {"types.p": "type tRequest = (sender: machine, amount: int);"}
        v = PayloadFieldValidator()
        r = v.validate(code, context)
        assert any("wrongField" in i.message for i in r.issues)

    def test_correct_field_passes(self):
        code = """
fun HandleRequest(req: tRequest) {
    var x: int;
    x = req.amount;
}
"""
        context = {"types.p": "type tRequest = (sender: machine, amount: int);"}
        v = PayloadFieldValidator()
        r = v.validate(code, context)
        assert not any("amount" in i.message for i in r.issues)

    def test_entry_param_checked(self):
        code = """
machine Server {
    start state Init {
        entry (config: tServerConfig) {
            var x: machine;
            x = config.badField;
        }
    }
}
"""
        context = {"types.p": "type tServerConfig = (coordinator: machine);"}
        v = PayloadFieldValidator()
        r = v.validate(code, context)
        assert any("badField" in i.message for i in r.issues)

    def test_on_do_param_checked(self):
        code = """
on eRequest do (payload: tReqPayload) {
    var s: machine;
    s = payload.client;
}
"""
        context = {"types.p": "type tReqPayload = (sender: machine, data: int);"}
        v = PayloadFieldValidator()
        r = v.validate(code, context)
        assert any("client" in i.message for i in r.issues)

    def test_no_context_skips(self):
        code = "fun Foo(x: tBar) { var y: int; y = x.field; }"
        v = PayloadFieldValidator()
        r = v.validate(code)
        assert r.is_valid


# ═══════════════════════════════════════════════════════════════════════
# TestFileValidator
# ═══════════════════════════════════════════════════════════════════════

class TestTestFileValidator:

    def test_missing_test_decl_warned(self):
        code = """
machine Scenario {
    start state Init {
        entry {
            var s: machine;
            s = new Server();
        }
    }
}
"""
        v = TestFileValidator()
        r = v.validate(code)
        assert any("no test declarations" in i.message.lower() for i in r.issues)

    def test_with_test_decl_passes(self):
        code = """
machine Scenario {
    start state Init { entry { } }
}
test tcBasic [main=Scenario]: assert Safety in { Server, Scenario };
"""
        context = {"spec.p": "spec Safety observes eStart { start state Init { } }"}
        v = TestFileValidator()
        r = v.validate(code, context)
        missing_decl = [i for i in r.issues if "no test declarations" in i.message.lower()]
        assert len(missing_decl) == 0

    def test_missing_spec_assertion_warned(self):
        code = """
machine Scenario {
    start state Init { entry { } }
}
test tcBasic [main=Scenario]: { Server, Scenario };
"""
        context = {"spec.p": "spec Safety observes eStart { start state Init { } }"}
        v = TestFileValidator()
        r = v.validate(code, context)
        assert any("Safety" in i.message for i in r.issues)

    def test_no_machines_no_warning(self):
        code = "// Just a comment file\ntype tFoo = (x: int);"
        v = TestFileValidator()
        r = v.validate(code)
        assert r.is_valid


# ═══════════════════════════════════════════════════════════════════════
# PCodePostProcessor — extended fix tests
# ═══════════════════════════════════════════════════════════════════════

class TestPostProcessorVarReorder:

    def _process(self, code):
        proc = PCodePostProcessor()
        return proc.process(code)

    def test_vars_hoisted_to_top(self):
        """Post-processor hoists vars when they appear after statements."""
        code = (
            "fun Work() {\n"
            "  x = 1;\n"
            "  var y: int;\n"
            "  y = 2;\n"
            "}\n"
        )
        result = self._process(code)
        lines = result.code.strip().splitlines()
        var_y_idx = None
        assign_x_idx = None
        for i, line in enumerate(lines):
            if "var y: int" in line and var_y_idx is None:
                var_y_idx = i
            if "x = 1" in line and assign_x_idx is None:
                assign_x_idx = i
        assert var_y_idx is not None
        assert assign_x_idx is not None
        assert var_y_idx < assign_x_idx

    def test_interleaved_vars_hoisted(self):
        """Post-processor hoists interleaved vars (var, stmt, var)."""
        code = (
            "fun Work() {\n"
            "  var x: int;\n"
            "  x = 1;\n"
            "  var y: int;\n"
            "  y = 2;\n"
            "}\n"
        )
        result = self._process(code)
        lines = result.code.strip().splitlines()
        var_y_idx = None
        assign_x_idx = None
        for i, line in enumerate(lines):
            if "var y: int" in line and var_y_idx is None:
                var_y_idx = i
            if "x = 1" in line and assign_x_idx is None:
                assign_x_idx = i
        assert var_y_idx is not None
        assert assign_x_idx is not None
        assert var_y_idx < assign_x_idx

    def test_already_ordered_vars_untouched(self):
        """Vars already at top of function should not be reordered."""
        code = (
            "fun Work() {\n"
            "  var x: int;\n"
            "  var y: int;\n"
            "  x = 1;\n"
            "  y = 2;\n"
            "}\n"
        )
        result = self._process(code)
        assert "var x: int" in result.code
        assert "var y: int" in result.code
        assert not any("variable declaration" in f.lower() for f in result.fixes_applied)

    def test_already_ordered_untouched(self):
        code = (
            "fun Work() {\n"
            "  var x: int;\n"
            "  x = 1;\n"
            "}\n"
        )
        result = self._process(code)
        assert "var x: int" in result.code
        assert not any("variable declaration" in f.lower() for f in result.fixes_applied)


class TestPostProcessorEnumDotAccess:

    def _process(self, code):
        proc = PCodePostProcessor()
        return proc.process(code)

    def test_enum_dot_removed(self):
        code = "var x: tColor;\nx = tColor.RED;"
        result = self._process(code)
        assert "tColor.RED" not in result.code
        assert "RED" in result.code

    def test_no_false_positive_on_field_access(self):
        code = "var x: int;\nx = config.timeout;"
        result = self._process(code)
        assert "config.timeout" in result.code


class TestPostProcessorEntryFunctionSyntax:

    def _process(self, code):
        proc = PCodePostProcessor()
        return proc.process(code)

    def test_entry_parens_removed(self):
        code = "entry InitEntry();"
        result = self._process(code)
        assert "entry InitEntry;" in result.code
        assert "()" not in result.code

    def test_entry_with_param_untouched(self):
        code = "entry (config: tConfig) {"
        result = self._process(code)
        assert "entry (config: tConfig) {" in result.code

    def test_entry_block_untouched(self):
        code = "entry { goto Running; }"
        result = self._process(code)
        assert "entry { goto Running; }" in result.code


class TestPostProcessorBareHalt:

    def _process(self, code):
        proc = PCodePostProcessor()
        return proc.process(code)

    def test_bare_halt_fixed(self):
        code = "halt;"
        result = self._process(code)
        assert "raise halt;" in result.code

    def test_raise_halt_untouched(self):
        code = "raise halt;"
        result = self._process(code)
        assert result.code.count("raise halt;") == 1


class TestPostProcessorForbiddenInMonitors:

    def _process(self, code):
        proc = PCodePostProcessor()
        return proc.process(code)

    def test_this_as_machine_removed(self):
        code = """
spec Monitor observes eEvent {
    var selfRef: machine;
    start state Init {
        entry {
            selfRef = this as machine;
        }
    }
}
"""
        result = self._process(code)
        assert "this as machine" not in result.code

    def test_forbidden_keyword_warned(self):
        code = """
spec Monitor observes eEvent {
    start state Init {
        entry {
            send target, eOther;
        }
    }
}
"""
        result = self._process(code)
        assert any("send" in w.lower() for w in result.warnings)


class TestPostProcessorMissingSemicolons:

    def _process(self, code):
        proc = PCodePostProcessor()
        return proc.process(code)

    def test_return_missing_semicolon(self):
        code = "fun Foo() {\n    return true\n}"
        result = self._process(code)
        assert "return true;" in result.code

    def test_return_with_semicolon_untouched(self):
        code = "fun Foo() {\n    return true;\n}"
        result = self._process(code)
        assert result.code.count("return true;") == 1


class TestPostProcessorNamedFieldTuple:

    def _process(self, code):
        proc = PCodePostProcessor()
        return proc.process(code)

    def test_named_fields_stripped(self):
        code = "send target, eConfig, (server = this, count = 3,);"
        result = self._process(code)
        assert "server =" not in result.code
        assert "count =" not in result.code
        assert "(this, 3,)" in result.code

    def test_positional_untouched(self):
        code = "send target, eConfig, (this, 3,);"
        result = self._process(code)
        assert "(this, 3,)" in result.code


class TestPostProcessorTestDeclarations:

    def _process(self, code):
        proc = PCodePostProcessor()
        return proc.process(code, filename="TestDriver.p", is_test_file=True)

    def test_auto_generates_test_decl(self):
        code = """
machine Scenario {
    start state Init {
        entry {
            var s: machine;
            s = new Server();
            send s, eStart;
        }
    }
}
"""
        result = self._process(code)
        assert "test " in result.code
        assert "[main=Scenario]" in result.code

    def test_existing_test_decl_not_duplicated(self):
        code = """
machine Scenario {
    start state Init { entry { } }
}
test tcBasic [main=Scenario]: { Scenario };
"""
        result = self._process(code)
        assert result.code.count("test ") == 1


if __name__ == "__main__":
    pytest.main([__file__, "-v"])
