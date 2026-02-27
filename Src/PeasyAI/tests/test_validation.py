"""
Tests for the Validation Pipeline.

Tests the validators and pipeline for P code validation.
"""

import pytest
import sys
from pathlib import Path

# Add project paths
PROJECT_ROOT = Path(__file__).parent.parent
SRC_ROOT = PROJECT_ROOT / "src"
sys.path.insert(0, str(SRC_ROOT))

from core.validation import (
    ValidationPipeline,
    SyntaxValidator,
    TypeDeclarationValidator,
    EventDeclarationValidator,
    MachineStructureValidator,
    DesignDocValidator,
    ProjectPathValidator,
    IssueSeverity,
    NamedTupleConstructionValidator,
)


class TestSyntaxValidator:
    """Tests for SyntaxValidator."""
    
    def test_valid_machine(self):
        """Test validation of valid machine code."""
        code = """
        machine TestMachine {
            start state Init {
                entry {
                    goto Running;
                }
            }
            
            state Running {
                on eStop do {
                    goto Init;
                }
            }
        }
        """
        validator = SyntaxValidator()
        result = validator.validate(code)
        
        assert result.is_valid
        assert len(result.errors) == 0
    
    def test_unbalanced_braces(self):
        """Test detection of unbalanced braces."""
        code = """
        machine TestMachine {
            start state Init {
                entry {
                    goto Running;
                }
            }
        """  # Missing closing brace
        
        validator = SyntaxValidator()
        result = validator.validate(code)
        
        assert not result.is_valid
        assert any("brace" in issue.message.lower() for issue in result.errors)
    
    def test_unbalanced_parentheses(self):
        """Test detection of unbalanced parentheses."""
        code = """
        machine TestMachine {
            start state Init {
                entry {
                    if (x > 0 {
                        goto Running;
                    }
                }
            }
        }
        """
        
        validator = SyntaxValidator()
        result = validator.validate(code)
        
        assert not result.is_valid
        assert any("parenthes" in issue.message.lower() for issue in result.errors)


class TestTypeDeclarationValidator:
    """Tests for TypeDeclarationValidator."""
    
    def test_builtin_types(self):
        """Test that built-in types are recognized."""
        code = """
        var x: int;
        var y: bool;
        var z: string;
        """
        
        validator = TypeDeclarationValidator()
        result = validator.validate(code)
        
        # Built-in types should not cause warnings
        assert result.is_valid
    
    def test_declared_type(self):
        """Test that declared types are recognized."""
        code = """
        type MyType = (x: int, y: int);
        var point: MyType;
        """
        
        validator = TypeDeclarationValidator()
        result = validator.validate(code)
        
        assert result.is_valid
    
    def test_undeclared_type_warning(self):
        """Test warning for potentially undeclared types."""
        code = """
        var point: UndeclaredType;
        """
        
        validator = TypeDeclarationValidator()
        result = validator.validate(code)
        
        # Should have a warning (not error) for undeclared type
        assert any("UndeclaredType" in issue.message for issue in result.warnings)
    
    def test_type_from_context(self):
        """Test that types from context files are recognized."""
        code = """
        var point: SharedType;
        """
        context = {
            "types.p": "type SharedType = (x: int, y: int);"
        }
        
        validator = TypeDeclarationValidator()
        result = validator.validate(code, context)
        
        # Should not warn about SharedType since it's in context
        assert not any("SharedType" in issue.message for issue in result.issues)


class TestEventDeclarationValidator:
    """Tests for EventDeclarationValidator."""
    
    def test_declared_event(self):
        """Test that declared events are recognized."""
        code = """
        event eStart;
        
        machine TestMachine {
            start state Init {
                entry {
                    send eStart, this;
                }
            }
        }
        """
        
        validator = EventDeclarationValidator()
        result = validator.validate(code)
        
        assert result.is_valid
    
    def test_undeclared_event_warning(self):
        """Test warning for potentially undeclared events."""
        code = """
        machine TestMachine {
            start state Init {
                entry {
                    raise eUndeclared;
                }
            }
        }
        """
        
        validator = EventDeclarationValidator()
        result = validator.validate(code)
        
        assert any("eUndeclared" in issue.message for issue in result.warnings)
    
    def test_event_from_context(self):
        """Test that events from context files are recognized."""
        code = """
        machine TestMachine {
            start state Init {
                entry {
                    send eSharedEvent, this;
                }
            }
        }
        """
        context = {
            "events.p": "event eSharedEvent;"
        }
        
        validator = EventDeclarationValidator()
        result = validator.validate(code, context)
        
        assert not any("eSharedEvent" in issue.message for issue in result.issues)


class TestMachineStructureValidator:
    """Tests for MachineStructureValidator."""
    
    def test_valid_machine(self):
        """Test validation of valid machine structure."""
        code = """
        machine TestMachine {
            start state Init {
                entry {
                    goto Running;
                }
            }
            
            state Running {
                on eStop do {
                    goto Init;
                }
            }
        }
        """
        
        validator = MachineStructureValidator()
        result = validator.validate(code)
        
        assert result.is_valid
    
    def test_missing_start_state(self):
        """Test detection of missing start state."""
        code = """
        machine TestMachine {
            state Running {
                on eStop do {
                    goto Init;
                }
            }
        }
        """
        
        validator = MachineStructureValidator()
        result = validator.validate(code)
        
        assert not result.is_valid
        assert any("start state" in issue.message.lower() for issue in result.errors)
    
    def test_no_states(self):
        """Test detection of machine with no states."""
        code = """
        machine TestMachine {
        }
        """
        
        validator = MachineStructureValidator()
        result = validator.validate(code)
        
        assert not result.is_valid
        assert any("start state" in issue.message.lower() for issue in result.errors)
    
    def test_non_machine_file(self):
        """Test that non-machine files pass validation."""
        code = """
        type Point = (x: int, y: int);
        event eMove: Point;
        """
        
        validator = MachineStructureValidator()
        result = validator.validate(code)
        
        # Non-machine files should pass
        assert result.is_valid


class TestValidationPipeline:
    """Tests for ValidationPipeline."""
    
    def test_pipeline_runs_all_validators(self):
        """Test that pipeline runs all validators."""
        code = """
        machine TestMachine {
            start state Init {
                entry {
                    goto Running;
                }
            }
            
            state Running {
            }
        }
        """
        
        pipeline = ValidationPipeline()
        result = pipeline.validate(code)
        
        assert len(result.validators_run) >= 4
        assert "SyntaxValidator" in result.validators_run
        assert "TypeDeclarationValidator" in result.validators_run
        assert "EventDeclarationValidator" in result.validators_run
        assert "MachineStructureValidator" in result.validators_run
    
    def test_pipeline_collects_all_issues(self):
        """Test that pipeline collects issues from all validators."""
        code = """
        machine TestMachine {
            state Running {
                entry {
                    send eUndeclared, this;
                    var x: UndeclaredType;
                }
            }
        }
        """
        
        pipeline = ValidationPipeline()
        result = pipeline.validate(code)
        
        # Should have issues from multiple validators
        assert len(result.issues) > 0
    
    def test_pipeline_summary(self):
        """Test pipeline result summary."""
        code = """
        machine TestMachine {
            start state Init {
                entry { }
            }
        }
        """
        
        pipeline = ValidationPipeline()
        result = pipeline.validate(code)
        
        summary = result.summary()
        assert "Validation" in summary
        assert "Validators run" in summary


class TestDesignDocValidator:
    """Tests for DesignDocValidator."""
    
    def test_valid_design_doc(self):
        """Test validation of valid design document."""
        doc = """
# Test System

## Introduction
This is a test system with two components.

## Components
- Client: Sends requests
- Server: Handles requests

## Interactions
Client sends eRequest to Server.
Server responds with eResponse.
        """
        
        validator = DesignDocValidator()
        result = validator.validate(doc)
        
        assert result.is_valid
    
    def test_too_short(self):
        """Test rejection of too-short documents."""
        doc = "Short doc"
        
        validator = DesignDocValidator()
        result = validator.validate(doc)
        
        assert not result.is_valid
        assert any("too short" in error.lower() for error in result.errors)
    
    def test_missing_sections_warning(self):
        """Test warning for missing sections."""
        doc = """
        This is a document without proper sections.
        It describes a system but doesn't use the expected format.
        """ * 10  # Make it long enough
        
        validator = DesignDocValidator()
        result = validator.validate(doc)
        
        # Should have warnings about missing sections
        assert len(result.warnings) > 0
    
    def test_extract_metadata(self):
        """Test metadata extraction."""
        doc = """
# My System

## Components

#### 1. Client
- **Role:** Sends requests

#### 2. Server
- **Role:** Handles requests
        """
        
        validator = DesignDocValidator()
        metadata = validator.extract_metadata(doc)
        
        assert metadata["title"] == "My System"
        assert "Client" in metadata["components"]
        assert "Server" in metadata["components"]


class TestProjectPathValidator:
    """Tests for ProjectPathValidator."""
    
    def test_nonexistent_path(self, tmp_path):
        """Test validation of nonexistent path."""
        validator = ProjectPathValidator()
        result = validator.validate_existing_project(str(tmp_path / "nonexistent"))
        
        assert not result.is_valid
    
    def test_valid_project_structure(self, tmp_path):
        """Test validation of valid project structure."""
        # Create project structure
        (tmp_path / "PSrc").mkdir()
        (tmp_path / "PSpec").mkdir()
        (tmp_path / "PTst").mkdir()
        (tmp_path / "test.pproj").touch()
        (tmp_path / "PSrc" / "main.p").write_text("machine Main { }")
        
        validator = ProjectPathValidator()
        result = validator.validate_existing_project(str(tmp_path))
        
        assert result.is_valid
    
    def test_missing_directories_warning(self, tmp_path):
        """Test warning for missing directories."""
        # Create only PSrc
        (tmp_path / "PSrc").mkdir()
        (tmp_path / "PSrc" / "main.p").write_text("machine Main { }")
        
        validator = ProjectPathValidator()
        result = validator.validate_existing_project(str(tmp_path))
        
        # Should have warnings about missing directories
        assert any("PSpec" in w or "PTst" in w for w in result.warnings)
    
    def test_output_path_validation(self, tmp_path):
        """Test validation of output path."""
        output_path = tmp_path / "new_project"
        
        validator = ProjectPathValidator()
        result = validator.validate_output_path(str(output_path))
        
        assert result.is_valid


class TestNamedTupleConstructionValidator:
    """Tests for NamedTupleConstructionValidator."""

    def test_correct_new_with_named_tuple(self):
        """Correct named-tuple constructor should pass."""
        code = """
        machine Client {
            start state Init {
                entry InitEntry;
            }
            fun InitEntry(config: tClientConfig) {
                goto Active;
            }
            state Active { }
        }
        machine TestScenario {
            start state Init {
                entry {
                    var c: machine;
                    c = new Client((server = this,));
                }
            }
        }
        """
        context = {
            "types.p": "type tClientConfig = (server: machine);"
        }
        validator = NamedTupleConstructionValidator()
        result = validator.validate(code, context)
        assert result.is_valid
        assert not result.errors

    def test_bare_value_in_new(self):
        """Bare value instead of named tuple should be flagged as ERROR."""
        code = """
        machine FailureDetector {
            start state Init {
                entry InitEntry;
            }
            fun InitEntry(config: tFDConfig) {
                goto Monitoring;
            }
            state Monitoring { }
        }
        machine TestScenario {
            start state Init {
                entry {
                    var nodes: seq[machine];
                    var fd: machine;
                    fd = new FailureDetector(nodes);
                }
            }
        }
        """
        context = {
            "types.p": "type tFDConfig = (nodes: seq[machine]);"
        }
        validator = NamedTupleConstructionValidator()
        result = validator.validate(code, context)
        assert not result.is_valid
        assert any("bare value" in i.message for i in result.errors)

    def test_wrong_field_name_in_new(self):
        """Wrong field name should produce a warning."""
        code = """
        machine Node {
            start state Init {
                entry InitEntry;
            }
            fun InitEntry(config: tNodeConfig) {
                goto Alive;
            }
            state Alive { }
        }
        machine TestScenario {
            start state Init {
                entry {
                    var fd: machine;
                    var n: machine;
                    n = new Node((detector = fd,));
                }
            }
        }
        """
        context = {
            "types.p": "type tNodeConfig = (failureDetector: machine);"
        }
        validator = NamedTupleConstructionValidator()
        result = validator.validate(code, context)
        assert any("missing field" in i.message for i in result.warnings)
        assert any("unexpected field" in i.message for i in result.warnings)

    def test_bare_value_in_send(self):
        """Bare value in send payload should be flagged."""
        code = """
        machine Sender {
            start state Init {
                entry {
                    var target: machine;
                    send target, eRegister, (this);
                }
            }
        }
        """
        context = {
            "types.p": (
                "type tRegPayload = (client: machine);\n"
                "event eRegister: tRegPayload;"
            )
        }
        validator = NamedTupleConstructionValidator()
        result = validator.validate(code, context)
        assert not result.is_valid
        assert any("bare value" in i.message for i in result.errors)

    def test_correct_send_with_named_tuple(self):
        """Correct named-tuple send should pass."""
        code = """
        machine Sender {
            start state Init {
                entry {
                    var target: machine;
                    send target, eRegister, (client = this,);
                }
            }
        }
        """
        context = {
            "types.p": (
                "type tRegPayload = (client: machine);\n"
                "event eRegister: tRegPayload;"
            )
        }
        validator = NamedTupleConstructionValidator()
        result = validator.validate(code, context)
        assert result.is_valid

    def test_no_config_type_skipped(self):
        """Machines without a typed entry param should be skipped."""
        code = """
        machine Simple {
            start state Init {
                entry { }
            }
        }
        machine Test {
            start state Init {
                entry {
                    var s: machine;
                    s = new Simple();
                }
            }
        }
        """
        validator = NamedTupleConstructionValidator()
        result = validator.validate(code)
        assert result.is_valid

    def test_inline_entry_param(self):
        """Machines with inline entry (param: Type) should be detected."""
        code = """
        machine Worker {
            start state Init {
                entry (config: tWorkerConfig) {
                    goto Working;
                }
            }
            state Working { }
        }
        machine Test {
            start state Init {
                entry {
                    var w: machine;
                    w = new Worker(42);
                }
            }
        }
        """
        context = {
            "types.p": "type tWorkerConfig = (id: int);"
        }
        validator = NamedTupleConstructionValidator()
        result = validator.validate(code, context)
        assert not result.is_valid
        assert any("bare value" in i.message for i in result.errors)


class TestPostProcessorTrailingComma:
    """Tests for trailing comma removal in parameter lists."""

    def _process(self, code):
        from core.compilation.p_post_processor import PCodePostProcessor
        proc = PCodePostProcessor()
        return proc.process(code)

    def test_fun_trailing_comma(self):
        code = "fun InitEntry(config: tConfig,) {\n    goto Active;\n}"
        result = self._process(code)
        assert ",)" not in result.code
        assert "config: tConfig)" in result.code
        assert len(result.fixes_applied) > 0

    def test_entry_trailing_comma(self):
        code = "entry (payload: tPayload,) {\n    goto Active;\n}"
        result = self._process(code)
        assert ",)" not in result.code
        assert "payload: tPayload)" in result.code

    def test_on_do_trailing_comma(self):
        code = "on eRequest do (req: tRequest,) {\n    goto Active;\n}"
        result = self._process(code)
        assert ",)" not in result.code
        assert "req: tRequest)" in result.code

    def test_no_false_positive_on_tuple_values(self):
        """Trailing comma in tuple values should NOT be removed."""
        code = "send target, eEvent, (value,);"
        result = self._process(code)
        assert "(value,)" in result.code

    def test_multi_param_trailing_comma(self):
        code = "fun Foo(a: int, b: string,) {\n    return;\n}"
        result = self._process(code)
        assert ",)" not in result.code
        assert "b: string)" in result.code

    def test_no_trailing_comma_untouched(self):
        code = "fun Bar(x: int) {\n    return;\n}"
        result = self._process(code)
        assert "x: int)" in result.code
        assert not any("trailing comma" in f.lower() for f in result.fixes_applied)


class TestPostProcessorTestDeclUnion:
    """Tests for (union { ... }) fix in test declarations."""

    def _process(self, code):
        from core.compilation.p_post_processor import PCodePostProcessor
        proc = PCodePostProcessor()
        return proc.process(code, is_test_file=True)

    def test_union_syntax_removed(self):
        code = (
            'test tcBasic [main=TestDriver]:\n'
            '    assert Safety in\n'
            '    (union { Server, Client, TestDriver });'
        )
        result = self._process(code)
        assert "(union" not in result.code
        assert "{ Server, Client, TestDriver }" in result.code
        assert len(result.fixes_applied) > 0

    def test_union_without_assert(self):
        code = 'test tcBasic [main=TestDriver]: (union { Server, Client });'
        result = self._process(code)
        assert "(union" not in result.code
        assert "{ Server, Client }" in result.code

    def test_missing_semicolon_added(self):
        code = (
            'test tcBasic [main=TestDriver]:\n'
            '    assert Safety in\n'
            '    { Server, Client, TestDriver }\n'
        )
        result = self._process(code)
        assert "TestDriver };" in result.code

    def test_existing_semicolon_untouched(self):
        code = (
            'test tcBasic [main=TestDriver]:\n'
            '    assert Safety in\n'
            '    { Server, Client, TestDriver };\n'
        )
        result = self._process(code)
        assert result.code.count("};") == 0 or result.code.count(";") == code.count(";")

    def test_multiple_test_decls(self):
        code = (
            'test tc1 [main=D1]: assert S in (union { A, B, D1 });\n'
            'test tc2 [main=D2]: assert S in (union { A, C, D2 });\n'
        )
        result = self._process(code)
        assert "(union" not in result.code
        assert "{ A, B, D1 }" in result.code
        assert "{ A, C, D2 }" in result.code

    def test_multiline_missing_semicolon(self):
        """Multi-line test declaration missing semicolon should be fixed."""
        code = (
            'test tcBasic [main=Scenario1]:\n'
            '    assert Safety, Liveness in\n'
            '    { Server, Client, Scenario1 }\n'
        )
        result = self._process(code)
        assert "Scenario1 };" in result.code


class TestDocumentationReviewParser:
    """Tests for the LLM documentation review response parser."""

    def test_parse_valid_response(self):
        from core.services.generation import GenerationService
        original = "machine Coordinator {\n    start state Init {}\n}\n"
        response = (
            "Some analysis text.\n\n"
            "<documented_code>\n"
            "// Coordinator for the Two Phase Commit protocol\n"
            "machine Coordinator {\n"
            "    start state Init {}\n"
            "}\n"
            "</documented_code>"
        )
        result = GenerationService._parse_documentation_review_response(response, original)
        assert result is not None
        assert "// Coordinator for the Two Phase Commit protocol" in result
        assert "machine Coordinator" in result

    def test_parse_missing_tags(self):
        from core.services.generation import GenerationService
        original = "machine Coordinator {\n    start state Init {}\n}\n"
        response = "Here is the code with comments:\nmachine Coordinator {\n    start state Init {}\n}\n"
        result = GenerationService._parse_documentation_review_response(response, original)
        assert result is None

    def test_parse_empty_documented_code(self):
        from core.services.generation import GenerationService
        original = "machine Coordinator {\n    start state Init {}\n}\n"
        response = "<documented_code>\n</documented_code>"
        result = GenerationService._parse_documentation_review_response(response, original)
        assert result is None

    def test_parse_rejects_dropped_declarations(self):
        from core.services.generation import GenerationService
        original = "machine Coordinator {\n    start state Init {}\n}\n"
        response = (
            "<documented_code>\n"
            "// Only comments, no machine declaration\n"
            "// The coordinator handles transactions\n"
            "</documented_code>"
        )
        result = GenerationService._parse_documentation_review_response(response, original)
        assert result is None

    def test_parse_accepts_matching_declarations(self):
        from core.services.generation import GenerationService
        original = (
            "machine Coordinator {\n    start state Init {}\n}\n"
            "machine Participant {\n    start state Init {}\n}\n"
        )
        response = (
            "<documented_code>\n"
            "// Coordinator orchestrates 2PC\n"
            "machine Coordinator {\n    start state Init {}\n}\n"
            "// Participant votes on transactions\n"
            "machine Participant {\n    start state Init {}\n}\n"
            "</documented_code>"
        )
        result = GenerationService._parse_documentation_review_response(response, original)
        assert result is not None
        assert "machine Coordinator" in result
        assert "machine Participant" in result

    def test_parse_spec_declarations_preserved(self):
        from core.services.generation import GenerationService
        original = "spec Atomicity observes eCommit {\n    start state Init {}\n}\n"
        response = (
            "<documented_code>\n"
            "// Atomicity: ensures all-or-nothing commit semantics\n"
            "spec Atomicity observes eCommit {\n    start state Init {}\n}\n"
            "</documented_code>"
        )
        result = GenerationService._parse_documentation_review_response(response, original)
        assert result is not None
        assert "spec Atomicity" in result

    def test_post_processor_no_design_doc_param(self):
        """Verify PCodePostProcessor.process() no longer accepts design_doc."""
        from core.compilation.p_post_processor import PCodePostProcessor
        import inspect
        sig = inspect.signature(PCodePostProcessor.process)
        assert "design_doc" not in sig.parameters


if __name__ == "__main__":
    pytest.main([__file__, "-v"])
