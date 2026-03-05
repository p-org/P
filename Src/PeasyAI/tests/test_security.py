"""Tests for core.security path validation and input sanitization."""

import os
import stat
import tempfile
from pathlib import Path

import pytest

from core.security import (
    PathSecurityError,
    check_input_size,
    sanitize_error,
    validate_file_read_path,
    validate_file_write_path,
    validate_project_path,
)


# ---------------------------------------------------------------------------
# Fixtures
# ---------------------------------------------------------------------------


@pytest.fixture()
def fake_project(tmp_path: Path):
    """Create a minimal P project directory structure."""
    (tmp_path / "PSrc").mkdir()
    (tmp_path / "PSpec").mkdir()
    (tmp_path / "PTst").mkdir()
    (tmp_path / "MyProject.pproj").write_text("<Project/>")
    (tmp_path / "PSrc" / "Main.p").write_text("machine Main {}")
    return tmp_path


# ---------------------------------------------------------------------------
# validate_project_path
# ---------------------------------------------------------------------------


class TestValidateProjectPath:
    def test_valid_project_accepted(self, fake_project: Path):
        result = validate_project_path(str(fake_project))
        assert result == fake_project.resolve()

    def test_rejects_nonexistent(self, tmp_path: Path):
        with pytest.raises(PathSecurityError, match="does not exist"):
            validate_project_path(str(tmp_path / "no_such_dir"))

    def test_rejects_non_directory(self, tmp_path: Path):
        f = tmp_path / "file.txt"
        f.write_text("hi")
        with pytest.raises(PathSecurityError, match="not a directory"):
            validate_project_path(str(f))

    def test_rejects_traversal(self, fake_project: Path):
        traversal = str(fake_project) + "/../" + fake_project.name
        with pytest.raises(PathSecurityError, match="traversal"):
            validate_project_path(traversal)

    def test_rejects_non_p_project(self, tmp_path: Path):
        (tmp_path / "random").mkdir()
        with pytest.raises(PathSecurityError, match="does not look like a P project"):
            validate_project_path(str(tmp_path / "random"))

    def test_accepts_project_with_only_psrc(self, tmp_path: Path):
        (tmp_path / "PSrc").mkdir()
        result = validate_project_path(str(tmp_path))
        assert result == tmp_path.resolve()

    def test_accepts_project_with_only_pproj(self, tmp_path: Path):
        (tmp_path / "Example.pproj").write_text("<Project/>")
        result = validate_project_path(str(tmp_path))
        assert result == tmp_path.resolve()


# ---------------------------------------------------------------------------
# validate_file_write_path
# ---------------------------------------------------------------------------


class TestValidateFileWritePath:
    def test_valid_write_accepted(self, fake_project: Path):
        fp = str(fake_project / "PSrc" / "NewMachine.p")
        result = validate_file_write_path(fp, str(fake_project))
        assert result == Path(fp).resolve()

    def test_rejects_escape(self, fake_project: Path):
        fp = str(fake_project / ".." / "evil.p")
        with pytest.raises(PathSecurityError, match="traversal"):
            validate_file_write_path(fp, str(fake_project))

    def test_rejects_outside_project(self, fake_project: Path):
        # Create a sibling directory that is outside the fake_project
        outside_dir = fake_project.parent / "elsewhere"
        outside_dir.mkdir(parents=True, exist_ok=True)
        outside = outside_dir / "evil.p"
        with pytest.raises(PathSecurityError, match="inside the project"):
            validate_file_write_path(str(outside), str(fake_project))

    def test_rejects_non_p_extension(self, fake_project: Path):
        fp = str(fake_project / "PSrc" / "hack.sh")
        with pytest.raises(PathSecurityError, match="Only P files"):
            validate_file_write_path(fp, str(fake_project))

    def test_accepts_pproj_extension(self, fake_project: Path):
        fp = str(fake_project / "New.pproj")
        result = validate_file_write_path(fp, str(fake_project))
        assert result.suffix == ".pproj"


# ---------------------------------------------------------------------------
# validate_file_read_path
# ---------------------------------------------------------------------------


class TestValidateFileReadPath:
    def test_valid_read_accepted(self, fake_project: Path):
        fp = str(fake_project / "PSrc" / "Main.p")
        result = validate_file_read_path(fp, str(fake_project))
        assert result == Path(fp).resolve()

    def test_rejects_traversal(self, fake_project: Path):
        fp = str(fake_project / ".." / "etc" / "passwd")
        with pytest.raises(PathSecurityError, match="traversal"):
            validate_file_read_path(fp, str(fake_project))

    def test_rejects_outside_project(self, fake_project: Path):
        # Create a sibling directory that is outside the fake_project
        outside_dir = fake_project.parent / "other"
        outside_dir.mkdir(parents=True, exist_ok=True)
        outside = outside_dir / "secret.p"
        with pytest.raises(PathSecurityError, match="inside the project"):
            validate_file_read_path(str(outside), str(fake_project))

    def test_accepts_without_project_constraint(self, tmp_path: Path):
        f = tmp_path / "anything.txt"
        f.write_text("data")
        result = validate_file_read_path(str(f))
        assert result == f.resolve()


# ---------------------------------------------------------------------------
# check_input_size
# ---------------------------------------------------------------------------


class TestCheckInputSize:
    def test_accepts_within_limit(self):
        check_input_size("hello", "test_field", 100)

    def test_rejects_oversized(self):
        with pytest.raises(ValueError, match="too large"):
            check_input_size("x" * 200, "test_field", 100)

    def test_counts_utf8_bytes(self):
        emoji = "\U0001f600"  # 4 bytes in UTF-8
        check_input_size(emoji, "emoji", 4)
        with pytest.raises(ValueError):
            check_input_size(emoji, "emoji", 3)


# ---------------------------------------------------------------------------
# sanitize_error
# ---------------------------------------------------------------------------


class TestSanitizeError:
    def test_redacts_long_paths(self):
        err = FileNotFoundError("/very/long/absolute/path/to/secret/file.txt not found")
        result = sanitize_error(err, "test")
        assert "/very/long" not in result
        assert "file.txt" in result
        assert "[test]" in result

    def test_keeps_short_paths(self):
        err = ValueError("port 8080 is busy")
        result = sanitize_error(err)
        assert "8080" in result

    def test_includes_context(self):
        err = RuntimeError("boom")
        result = sanitize_error(err, "compile")
        assert "[compile]" in result
        assert "RuntimeError" in result

    def test_no_context(self):
        err = RuntimeError("boom")
        result = sanitize_error(err)
        assert result.startswith("RuntimeError")
