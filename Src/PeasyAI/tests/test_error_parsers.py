"""
Tests for core/compilation/error_parser.py and checker_error_parser.py.

Covers: PCompilerErrorParser.parse, error categorization, CompilationResult,
        parse_compilation_output, PCheckerErrorParser.parse, PCheckerErrorParser.analyze.
"""

import sys
from pathlib import Path

import pytest

PROJECT_ROOT = Path(__file__).parent.parent
sys.path.insert(0, str(PROJECT_ROOT / "src"))

from core.compilation.error_parser import (
    PCompilerErrorParser,
    PCompilerError,
    ErrorType,
    ErrorCategory,
    CompilationResult,
    parse_compilation_output,
)
from core.compilation.checker_error_parser import (
    PCheckerErrorParser,
    CheckerErrorCategory,
    CheckerError,
    MachineState,
    EventInfo,
)


# ═══════════════════════════════════════════════════════════════════════
# PCompilerErrorParser
# ═══════════════════════════════════════════════════════════════════════

class TestPCompilerErrorParser:

    def test_parse_parse_error(self):
        output = "[Client.p] parse error: line 15:8 mismatched input 'var' expecting ')'"
        errors = PCompilerErrorParser.parse(output)
        assert len(errors) == 1
        e = errors[0]
        assert e.file == "Client.p"
        assert e.line == 15
        assert e.column == 8
        assert e.error_type == ErrorType.PARSE
        assert "mismatched" in e.message

    def test_parse_type_error(self):
        output = "[Server.p] error: line 42:10 undefined event 'eTest'"
        errors = PCompilerErrorParser.parse(output)
        assert len(errors) == 1
        assert errors[0].error_type == ErrorType.TYPE
        assert errors[0].category == ErrorCategory.UNDEFINED_EVENT

    def test_parse_semantic_error(self):
        output = "[Error:] [Types.p:5:3] duplicates declaration of 'tConfig'"
        errors = PCompilerErrorParser.parse(output)
        assert len(errors) == 1
        assert errors[0].error_type == ErrorType.SEMANTIC
        assert errors[0].category == ErrorCategory.DUPLICATE_DECLARATION

    def test_parse_multiple_errors(self):
        output = """
[Client.p] parse error: line 10:5 extraneous input 'var' expecting ')'
[Server.p] error: line 20:3 type 'tFoo' not found
[Error:] [Types.p:5:3] duplicates declaration of 'tConfig'
"""
        errors = PCompilerErrorParser.parse(output)
        assert len(errors) == 3

    def test_deduplication(self):
        output = """
[Client.p] parse error: line 10:5 some error
[Client.p] parse error: line 10:5 some error
"""
        errors = PCompilerErrorParser.parse(output)
        assert len(errors) == 1

    def test_no_errors(self):
        output = "Compilation succeeded. Build succeeded."
        errors = PCompilerErrorParser.parse(output)
        assert len(errors) == 0


class TestErrorCategorization:

    def test_var_declaration_order(self):
        cat = PCompilerErrorParser._categorize_error("extraneous input 'var' expecting ')'")
        assert cat == ErrorCategory.VAR_DECLARATION_ORDER

    def test_undefined_event(self):
        cat = PCompilerErrorParser._categorize_error("event 'eTest' not found in scope")
        assert cat == ErrorCategory.UNDEFINED_EVENT

    def test_undefined_type(self):
        cat = PCompilerErrorParser._categorize_error("type 'tConfig' not found")
        assert cat == ErrorCategory.UNDEFINED_TYPE

    def test_duplicate_declaration(self):
        cat = PCompilerErrorParser._categorize_error("duplicates declaration of 'eStart'")
        assert cat == ErrorCategory.DUPLICATE_DECLARATION

    def test_type_mismatch(self):
        cat = PCompilerErrorParser._categorize_error("got type: int expected: bool")
        assert cat == ErrorCategory.TYPE_MISMATCH

    def test_missing_semicolon(self):
        cat = PCompilerErrorParser._categorize_error("expecting ';' at end of statement")
        assert cat == ErrorCategory.MISSING_SEMICOLON

    def test_unknown_error(self):
        cat = PCompilerErrorParser._categorize_error("something completely unexpected")
        assert cat == ErrorCategory.UNKNOWN


class TestCompilationResult:

    def test_success_result(self):
        r = CompilationResult(success=True, errors=[], output="Build succeeded")
        assert r.success
        assert r.get_first_error() is None
        assert r.get_errors_by_file() == {}

    def test_failed_result(self):
        e1 = PCompilerError(
            file="a.p", line=1, column=1, error_type=ErrorType.PARSE,
            category=ErrorCategory.UNKNOWN, message="err1", raw_message="err1",
        )
        e2 = PCompilerError(
            file="a.p", line=5, column=1, error_type=ErrorType.TYPE,
            category=ErrorCategory.UNKNOWN, message="err2", raw_message="err2",
        )
        e3 = PCompilerError(
            file="b.p", line=1, column=1, error_type=ErrorType.PARSE,
            category=ErrorCategory.UNKNOWN, message="err3", raw_message="err3",
        )
        r = CompilationResult(success=False, errors=[e1, e2, e3])
        assert r.get_first_error() == e1
        by_file = r.get_errors_by_file()
        assert len(by_file["a.p"]) == 2
        assert len(by_file["b.p"]) == 1

    def test_to_dict(self):
        r = CompilationResult(success=True, errors=[], warnings=["w1"])
        d = r.to_dict()
        assert d["success"] is True
        assert d["error_count"] == 0
        assert d["warnings"] == ["w1"]


class TestParseCompilationOutput:

    def test_success_output(self):
        output = "Building...\nCompilation succeeded\n~~ [PTool]: Thanks for using P! ~~"
        r = parse_compilation_output(output)
        assert r.success
        assert len(r.errors) == 0

    def test_failure_output(self):
        output = """
[Client.p] parse error: line 10:5 extraneous input 'var'
Build failed.
"""
        r = parse_compilation_output(output)
        assert not r.success
        assert len(r.errors) == 1

    def test_warnings_extracted(self):
        output = "Compilation succeeded\nwarning: unused variable 'x'"
        r = parse_compilation_output(output)
        assert r.success
        assert len(r.warnings) == 1


class TestPCompilerErrorToDict:

    def test_to_dict(self):
        e = PCompilerError(
            file="test.p", line=10, column=5,
            error_type=ErrorType.PARSE, category=ErrorCategory.MISSING_SEMICOLON,
            message="expecting ';'", raw_message="raw",
            suggestion="Add semicolon",
        )
        d = e.to_dict()
        assert d["file"] == "test.p"
        assert d["line"] == 10
        assert d["error_type"] == "parse"
        assert d["category"] == "missing_semicolon"
        assert d["suggestion"] == "Add semicolon"


# ═══════════════════════════════════════════════════════════════════════
# PCheckerErrorParser
# ═══════════════════════════════════════════════════════════════════════

class TestPCheckerErrorParser:

    def test_parse_null_target(self):
        trace = """
<StateLog> 'Coordinator(1)' enters state 'SendCommit'
<ErrorLog> Target in send cannot be null. Machine Coordinator(1) trying to send event eCommit to null target in state SendCommit
"""
        parser = PCheckerErrorParser()
        error = parser.parse(trace)
        assert error.category == CheckerErrorCategory.NULL_TARGET
        assert error.machine_type == "Coordinator"
        assert error.machine_id == "1"
        assert error.event_name == "eCommit"
        assert error.machine_state == "SendCommit"

    def test_parse_unhandled_event(self):
        trace = """
<StateLog> 'Server(2)' enters state 'Active'
<ErrorLog> Server(2) received event 'eShutdown' that cannot be handled
"""
        parser = PCheckerErrorParser()
        error = parser.parse(trace)
        assert error.category == CheckerErrorCategory.UNHANDLED_EVENT
        assert error.machine_type == "Server"
        assert error.event_name == "eShutdown"

    def test_parse_unhandled_event_with_namespace(self):
        trace = "<ErrorLog> Node(3) received event 'PImplementation.eLearn' that cannot be handled"
        parser = PCheckerErrorParser()
        error = parser.parse(trace)
        assert error.category == CheckerErrorCategory.UNHANDLED_EVENT
        assert error.event_name == "eLearn"

    def test_parse_assertion_failure(self):
        trace = "<ErrorLog> Assertion 'balance >= 0' failed in machine Account(1)"
        parser = PCheckerErrorParser()
        error = parser.parse(trace)
        assert error.category == CheckerErrorCategory.ASSERTION_FAILURE

    def test_parse_deadlock(self):
        trace = "<ErrorLog> Deadlock detected. All machines are blocked."
        parser = PCheckerErrorParser()
        error = parser.parse(trace)
        assert error.category == CheckerErrorCategory.DEADLOCK

    def test_parse_liveness_violation(self):
        trace = "<ErrorLog> Monitor 'Liveness' detected hot state violation"
        parser = PCheckerErrorParser()
        error = parser.parse(trace)
        assert error.category == CheckerErrorCategory.LIVENESS_VIOLATION

    def test_parse_unknown_error(self):
        trace = "Some output without any recognizable error pattern"
        parser = PCheckerErrorParser()
        error = parser.parse(trace)
        assert error.category == CheckerErrorCategory.UNKNOWN

    def test_parse_empty_trace(self):
        parser = PCheckerErrorParser()
        error = parser.parse("")
        assert error.category == CheckerErrorCategory.UNKNOWN


class TestPCheckerAnalyze:

    def test_analyze_extracts_machines(self):
        trace = """
<StateLog> 'Client(1)' enters state 'Init'
<StateLog> 'Server(2)' enters state 'Listening'
<SendLog> 'Client(1)' in state 'Init' sent event 'eRequest' to 'Server(2)'
<StateLog> 'Client(1)' enters state 'WaitingResponse'
<ErrorLog> Server(2) received event 'eShutdown' that cannot be handled
"""
        parser = PCheckerErrorParser()
        analysis = parser.analyze(trace)
        assert "Client" in analysis.machines_involved
        assert "Server" in analysis.machines_involved
        assert analysis.execution_steps > 0
        assert len(analysis.last_actions) > 0

    def test_analyze_generates_summary(self):
        trace = """
<StateLog> 'Worker(1)' enters state 'Active'
<ErrorLog> Worker(1) received event 'eStop' that cannot be handled
"""
        parser = PCheckerErrorParser()
        analysis = parser.analyze(trace)
        summary = analysis.get_summary()
        assert "PChecker Error Analysis" in summary
        assert "unhandled_event" in summary


class TestMachineState:

    def test_from_log_enters(self):
        line = "<StateLog> 'Client(1)' enters state 'Init'"
        ms = MachineState.from_log(line)
        assert ms is not None
        assert ms.machine_type == "Client"
        assert ms.machine_id == "1"
        assert ms.state == "Init"

    def test_from_log_no_match(self):
        line = "Some random log line"
        ms = MachineState.from_log(line)
        assert ms is None


class TestEventInfo:

    def test_from_send_log(self):
        line = "<SendLog> 'Client(1)' in state 'Active' sent event 'eRequest with payload (42)' to 'Server(2)'"
        ei = EventInfo.from_log(line)
        assert ei is not None
        assert ei.event_name == "eRequest"
        assert ei.sender == "Client(1)"
        assert ei.receiver == "Server(2)"

    def test_from_dequeue_log(self):
        line = "<DequeueLog> 'Server(2)' dequeued event 'eRequest with payload (42)'"
        ei = EventInfo.from_log(line)
        assert ei is not None
        assert ei.event_name == "eRequest"
        assert ei.receiver == "Server(2)"

    def test_no_match(self):
        line = "Just a regular line"
        ei = EventInfo.from_log(line)
        assert ei is None


if __name__ == "__main__":
    pytest.main([__file__, "-v"])
