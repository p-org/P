#!/usr/bin/env python3
"""
Dependency preflight for PeasyAI development and MCP contract tests.
"""

from __future__ import annotations

import importlib.util
import shutil
import sys


REQUIRED_PYTHON_MODULES = [
    "pydantic",
    "fastmcp",
    "dotenv",
]

OPTIONAL_PYTHON_MODULES = [
    "pytest",
]

REQUIRED_BINARIES = [
    "python3",
]

OPTIONAL_BINARIES = [
    "p",
    "dotnet",
]


def has_module(module_name: str) -> bool:
    return importlib.util.find_spec(module_name) is not None


def has_binary(binary_name: str) -> bool:
    return shutil.which(binary_name) is not None


def main() -> int:
    missing_required = []

    print("== PeasyAI dependency preflight ==")

    for mod in REQUIRED_PYTHON_MODULES:
        ok = has_module(mod)
        print(f"[{'OK' if ok else 'MISSING'}] python module: {mod}")
        if not ok:
            missing_required.append(f"python module '{mod}'")

    for mod in OPTIONAL_PYTHON_MODULES:
        ok = has_module(mod)
        print(f"[{'OK' if ok else 'WARN'}] optional python module: {mod}")

    for binary in REQUIRED_BINARIES:
        ok = has_binary(binary)
        print(f"[{'OK' if ok else 'MISSING'}] binary: {binary}")
        if not ok:
            missing_required.append(f"binary '{binary}'")

    for binary in OPTIONAL_BINARIES:
        ok = has_binary(binary)
        print(f"[{'OK' if ok else 'WARN'}] optional binary: {binary}")

    if missing_required:
        print("\nMissing required dependencies:")
        for item in missing_required:
            print(f"- {item}")
        print("\nInstall them with:")
        print("  python3 -m pip install -r requirements.txt")
        return 1

    print("\nAll required dependencies are present.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
