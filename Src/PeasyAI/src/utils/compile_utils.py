"""Compilation utilities for P projects.

This module provides functions for compiling P projects and managing
compilation artifacts. For full compilation services including error
parsing and fixing, use :class:`core.services.compilation.CompilationService`.
"""

import logging
import os
import subprocess
from datetime import datetime
from pathlib import Path

from utils import file_utils

logger = logging.getLogger(__name__)
logging.basicConfig(level=logging.INFO, format="%(levelname)s: %(message)s")


def try_compile(ppath, captured_streams_output_dir):
    """Attempt to compile a P project.

    Args:
        ppath: Path to the P project directory or .pproj file.
        captured_streams_output_dir: Directory to save stdout/stderr output.

    Returns:
        True if compilation succeeded, False otherwise.
    """
    pgen_dir = Path(ppath).resolve() / "PGenerated"
    if pgen_dir.is_dir():
        import shutil
        shutil.rmtree(str(pgen_dir))

    p = Path(ppath)
    flags = ['-pf', ppath, "-o", str(p.parent)] if p.is_file() else []

    final_cmd_arr = ['p', 'compile', *flags]
    result = subprocess.run(final_cmd_arr, capture_output=True, cwd=ppath if not p.is_file() else None)

    out_dir = f"{captured_streams_output_dir}/compile"
    os.makedirs(out_dir, exist_ok=True)
    file_utils.write_output_streams(result, out_dir)
    return result.returncode == 0


def try_compile_project_state(project_state, captured_streams_output_dir=None):
    """Write project state to a temp directory and attempt compilation.

    Args:
        project_state: Dict mapping relative file paths to file contents.
        captured_streams_output_dir: Optional directory to save stdout/stderr.

    Returns:
        Tuple of (success: bool, project_dir: str, stdout: str).
    """
    timestamp = datetime.now().strftime('%Y-%m-%d-%H-%M-%S')
    tmp_project_dir = f"/tmp/compile-utils/{timestamp}"
    file_utils.write_project_state(project_state, tmp_project_dir)
    passed = try_compile(tmp_project_dir, f"{tmp_project_dir}/std_streams")
    stdout = file_utils.read_file(f"{tmp_project_dir}/std_streams/compile/stdout.txt")
    return passed, tmp_project_dir, stdout
