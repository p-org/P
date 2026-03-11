"""
P Compilation Module

Provides compilation, error parsing, and error fixing capabilities.
"""

from .error_parser import (
    PCompilerError,
    PCompilerErrorParser,
    ErrorType,
    ErrorCategory,
    CompilationResult,
    parse_compilation_output,
)

from .error_fixers import (
    CodeFix,
    PErrorFixer,
    apply_fix,
    fix_all_errors,
)

from .environment import (
    EnvironmentInfo,
    EnvironmentDetector,
    ensure_environment,
    get_compile_command,
    get_check_command,
)

from .checker_error_parser import (
    CheckerErrorCategory,
    CheckerError,
    TraceAnalysis,
    PCheckerErrorParser,
    MachineState,
    EventInfo,
    SenderInfo,
    CascadingImpact,
)

from .checker_fixers import (
    CheckerFix,
    FilePatch,
    PCheckerErrorFixer,
    analyze_and_suggest_fix,
)

from .p_post_processor import (
    PCodePostProcessor,
    PostProcessResult,
    post_process_file,
)

__all__ = [
    # Error parsing
    "PCompilerError",
    "PCompilerErrorParser", 
    "ErrorType",
    "ErrorCategory",
    "CompilationResult",
    "parse_compilation_output",
    # Error fixing
    "CodeFix",
    "PErrorFixer",
    "apply_fix",
    "fix_all_errors",
    # Environment
    "EnvironmentInfo",
    "EnvironmentDetector",
    "ensure_environment",
    "get_compile_command",
    "get_check_command",
    # Checker error handling
    "CheckerErrorCategory",
    "CheckerError",
    "TraceAnalysis",
    "PCheckerErrorParser",
    "MachineState",
    "EventInfo",
    "SenderInfo",
    "CascadingImpact",
    "CheckerFix",
    "FilePatch",
    "PCheckerErrorFixer",
    "analyze_and_suggest_fix",
    # Post-processing
    "PCodePostProcessor",
    "PostProcessResult",
    "post_process_file",
]
