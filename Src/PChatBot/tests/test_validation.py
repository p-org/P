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
                    send eUndeclared, this;
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
        assert any("no states" in issue.message.lower() for issue in result.errors)
    
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
        
        assert len(result.validators_run) == 4
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
        <title>Test System</title>
        
        <introduction>
        This is a test system with two components.
        </introduction>
        
        <components>
        - Client: Sends requests
        - Server: Handles requests
        </components>
        
        <interactions>
        Client sends eRequest to Server.
        Server responds with eResponse.
        </interactions>
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
        <title>My System</title>
        <component>Client</component>
        <component>Server</component>
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


if __name__ == "__main__":
    pytest.main([__file__, "-v"])
