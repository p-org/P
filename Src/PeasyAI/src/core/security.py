"""
Security utilities for PeasyAI.

Provides path validation, input sanitization, and error redaction
to prevent path traversal attacks, arbitrary file access, and
information leakage through error messages.
"""

import logging
import os
import re
from pathlib import Path
from typing import Optional

logger = logging.getLogger(__name__)

# Maximum sizes for inputs that will be passed to the LLM or stored in memory.
MAX_DESIGN_DOC_BYTES = 500_000       # 500 KB
MAX_CODE_BYTES = 200_000             # 200 KB
MAX_TRACE_LOG_BYTES = 1_000_000      # 1 MB
MAX_ERROR_MESSAGE_BYTES = 10_000     # 10 KB
MAX_USER_GUIDANCE_BYTES = 50_000     # 50 KB

ALLOWED_P_EXTENSIONS = {".p", ".pproj"}

# Directories inside a P project that may contain writable files.
P_PROJECT_SUBDIRS = {"PSrc", "PSpec", "PTst"}


class PathSecurityError(Exception):
    """Raised when a path fails security validation."""


def validate_project_path(path: str) -> Path:
    """
    Validate that *path* looks like a legitimate P project directory.

    Checks:
    - Path is absolute
    - Path exists and is a directory
    - No ``..`` components after resolution
    - Contains at least one P project marker (.pproj or PSrc/)

    Returns the resolved ``Path`` on success, raises ``PathSecurityError``
    on failure.
    """
    resolved = Path(path).resolve()

    if not resolved.is_absolute():
        raise PathSecurityError("Project path must be absolute")

    if not resolved.is_dir():
        raise PathSecurityError("Project path does not exist or is not a directory")

    if ".." in Path(path).parts:
        raise PathSecurityError("Path traversal ('..') is not allowed")

    has_pproj = any(resolved.glob("*.pproj"))
    has_psrc = (resolved / "PSrc").is_dir()
    if not has_pproj and not has_psrc:
        raise PathSecurityError(
            "Path does not look like a P project "
            "(no .pproj file and no PSrc/ directory)"
        )

    return resolved


def validate_file_write_path(
    file_path: str,
    project_path: str,
) -> Path:
    """
    Validate that *file_path* is safe to write to.

    Checks:
    - Path is absolute
    - Resolved path lives inside *project_path* (no escape via symlinks or ``..``)
    - File extension is an allowed P extension

    Returns the resolved ``Path`` on success.
    """
    resolved_project = Path(project_path).resolve()
    resolved_file = Path(file_path).resolve()

    if not resolved_file.is_absolute():
        raise PathSecurityError("File path must be absolute")

    if ".." in Path(file_path).parts:
        raise PathSecurityError("Path traversal ('..') is not allowed in file paths")

    # Ensure the file lives within the project directory tree.
    try:
        resolved_file.relative_to(resolved_project)
    except ValueError:
        raise PathSecurityError(
            f"File path must be inside the project directory "
            f"({resolved_project})"
        )

    if resolved_file.suffix not in ALLOWED_P_EXTENSIONS:
        raise PathSecurityError(
            f"Only P files ({', '.join(ALLOWED_P_EXTENSIONS)}) can be written; "
            f"got '{resolved_file.suffix}'"
        )

    return resolved_file


def validate_file_read_path(
    file_path: str,
    project_path: Optional[str] = None,
) -> Path:
    """
    Validate that *file_path* is safe to read.

    If *project_path* is provided, the file must reside within it.
    """
    resolved = Path(file_path).resolve()

    if ".." in Path(file_path).parts:
        raise PathSecurityError("Path traversal ('..') is not allowed")

    if project_path:
        resolved_project = Path(project_path).resolve()
        try:
            resolved.relative_to(resolved_project)
        except ValueError:
            raise PathSecurityError(
                f"File path must be inside the project directory "
                f"({resolved_project})"
            )

    return resolved


def sanitize_error(error: Exception, context: str = "") -> str:
    """
    Produce a user-safe error message that does not leak internal paths.

    Replaces absolute paths with ``<path>/basename`` and strips stack traces.
    """
    msg = str(error)

    msg = re.sub(
        r'(/[^\s:,\'"]+)',
        lambda m: f"<path>/{Path(m.group(1)).name}" if len(m.group(1)) > 20 else m.group(1),
        msg,
    )

    prefix = f"[{context}] " if context else ""
    return f"{prefix}{type(error).__name__}: {msg}"


def check_input_size(value: str, name: str, max_bytes: int) -> None:
    """Raise ``ValueError`` if *value* exceeds *max_bytes*."""
    size = len(value.encode("utf-8", errors="replace"))
    if size > max_bytes:
        raise ValueError(
            f"{name} too large: {size:,} bytes exceeds the "
            f"{max_bytes:,} byte limit"
        )
