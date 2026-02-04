"""
Core Services Layer

This module provides UI-agnostic services for P code generation,
compilation, checking, and error fixing.

These services can be used by any interface (Streamlit, CLI, MCP)
without any UI-specific dependencies.
"""

from .generation import GenerationService, GenerationResult
from .compilation import CompilationService, CompilationResult
from .fixer import FixerService, FixResult, FixAttemptTracker

__all__ = [
    "GenerationService",
    "GenerationResult",
    "CompilationService", 
    "CompilationResult",
    "FixerService",
    "FixResult",
    "FixAttemptTracker",
]


