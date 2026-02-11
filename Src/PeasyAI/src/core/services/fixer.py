"""
Fixer Service

Handles automatic fixing of compilation and checker errors
with human-in-the-loop fallback.

Enhanced with:
- Structured error parsing
- Specialized fixers for common errors
- Better feedback in results
"""

import re
import json
import logging
from pathlib import Path
from typing import Dict, Any, Optional, List
from dataclasses import dataclass, field

from .base import BaseService, ServiceResult, EventCallback, ResourceLoader
from .compilation import CompilationService, ParsedError
from ..llm import LLMProvider, LLMConfig, Message, MessageRole

# Import new compilation utilities
try:
    from ..compilation import (
        PCompilerErrorParser,
        PCompilerError,
        ErrorCategory,
        PErrorFixer,
        CodeFix,
        fix_all_errors,
        parse_compilation_output,
        # Checker error handling
        PCheckerErrorParser,
        CheckerError,
        CheckerErrorCategory,
        TraceAnalysis,
        PCheckerErrorFixer,
        CheckerFix,
        analyze_and_suggest_fix,
    )
    HAS_NEW_FIXERS = True
    HAS_CHECKER_FIXERS = True
except ImportError:
    HAS_NEW_FIXERS = False
    HAS_CHECKER_FIXERS = False

logger = logging.getLogger(__name__)


@dataclass
class FixResult(ServiceResult):
    """Result of a fix operation"""
    fixed: bool = False
    filename: Optional[str] = None
    file_path: Optional[str] = None
    original_code: Optional[str] = None
    fixed_code: Optional[str] = None
    attempt_number: int = 0
    needs_guidance: bool = False
    guidance_request: Optional[Dict[str, Any]] = None
    # Enhanced analysis fields
    analysis: Optional[Dict[str, Any]] = None
    root_cause: Optional[str] = None
    suggested_fixes: Optional[List[str]] = None
    confidence: float = 0.0


class FixAttemptTracker:
    """
    Tracks fix attempts for human-in-the-loop fallback.
    
    After a configurable number of failed attempts (default 3),
    the fixer will request human guidance.
    """
    
    def __init__(self, max_attempts: int = 3):
        self.max_attempts = max_attempts
        self._attempts: Dict[str, List[str]] = {}
    
    def add_attempt(self, error_key: str, description: str):
        """Record an attempted fix"""
        if error_key not in self._attempts:
            self._attempts[error_key] = []
        self._attempts[error_key].append(description)
    
    def get_attempt_count(self, error_key: str) -> int:
        """Get number of attempts for an error"""
        return len(self._attempts.get(error_key, []))
    
    def get_attempts(self, error_key: str) -> List[str]:
        """Get all attempt descriptions for an error"""
        return self._attempts.get(error_key, [])
    
    def should_request_guidance(self, error_key: str) -> bool:
        """Check if we should request human guidance"""
        return self.get_attempt_count(error_key) >= self.max_attempts
    
    def clear(self, error_key: str):
        """Clear attempts for an error (after successful fix)"""
        if error_key in self._attempts:
            del self._attempts[error_key]
    
    def clear_all(self):
        """Clear all tracked attempts"""
        self._attempts.clear()


class FixerService(BaseService):
    """
    Service for automatically fixing P code errors.
    
    Features:
    - Compilation error fixing
    - Checker error fixing
    - Human-in-the-loop fallback after max retries
    - Context-aware fixing using project files
    """
    
    def __init__(
        self,
        llm_provider: Optional[LLMProvider] = None,
        resource_loader: Optional[ResourceLoader] = None,
        callbacks: Optional[EventCallback] = None,
        compilation_service: Optional[CompilationService] = None,
        max_attempts: int = 3,
    ):
        super().__init__(llm_provider, resource_loader, callbacks)
        self._compilation = compilation_service or CompilationService(
            llm_provider, resource_loader, callbacks
        )
        self._tracker = FixAttemptTracker(max_attempts)
    
    @property
    def tracker(self) -> FixAttemptTracker:
        """Get the fix attempt tracker"""
        return self._tracker
    
    def _try_specialized_fix(
        self,
        error: ParsedError,
        file_content: str,
    ) -> Optional[CodeFix]:
        """
        Try to fix an error using specialized fixers.
        Returns CodeFix if successful, None otherwise.
        """
        if not HAS_NEW_FIXERS:
            return None
        
        try:
            # Convert ParsedError to PCompilerError
            p_error = PCompilerError(
                file=Path(error.file_path).name,
                line=error.line_number,
                column=error.column_number,
                error_type=ErrorCategory.UNKNOWN,
                category=PCompilerErrorParser._categorize_error(error.message),
                message=error.message,
                raw_message=error.message,
            )
            
            # Try specialized fixer
            fixer = PErrorFixer()
            if fixer.can_fix(p_error):
                fix = fixer.fix(p_error, file_content)
                if fix:
                    logger.info(f"Specialized fixer applied: {fix.description}")
                    return fix
        except Exception as e:
            logger.debug(f"Specialized fixer failed: {e}")
        
        return None

    def fix_compilation_error(
        self,
        project_path: str,
        error: ParsedError,
        user_guidance: Optional[str] = None,
    ) -> FixResult:
        """
        Fix a compilation error.
        
        First tries specialized fixers for common errors, then falls back to LLM.
        
        Args:
            project_path: Path to the P project
            error: Parsed error to fix
            user_guidance: Optional guidance from user after failed attempts
            
        Returns:
            FixResult with fix details
        """
        resolved_file_path = self._resolve_error_file_path(project_path, error.file_path)
        error_key = f"compile:{resolved_file_path}:{hash(error.message)}"
        attempt = self._tracker.get_attempt_count(error_key) + 1
        
        self._status(f"Fixing compilation error (attempt {attempt}): {error.message[:50]}...")
        
        # Check if we should request guidance
        if self._tracker.should_request_guidance(error_key) and not user_guidance:
            return self._create_guidance_request(
                error_key,
                "compilation",
                error,
                project_path,
            )
        
        try:
            # Read the file with the error
            file_content = self._compilation.read_file(resolved_file_path)
            if file_content is None:
                return FixResult(
                    success=False,
                    error=f"Could not read file: {resolved_file_path}",
                )
            
            # First try specialized fixers (faster, more reliable)
            specialized_fix = self._try_specialized_fix(error, file_content)
            if specialized_fix:
                # Apply the fix
                self._compilation.write_file(resolved_file_path, specialized_fix.fixed_code)
                
                # Verify by recompiling
                compile_result = self._compilation.compile(project_path)
                
                if compile_result.success:
                    self._tracker.clear(error_key)
                    self._status(f"Fix successful (specialized): {specialized_fix.description}")
                    
                    return FixResult(
                        success=True,
                        fixed=True,
                        filename=Path(resolved_file_path).name,
                        file_path=resolved_file_path,
                        original_code=file_content,
                        fixed_code=specialized_fix.fixed_code,
                        attempt_number=attempt,
                    )
                else:
                    # Revert the fix and try LLM
                    self._compilation.write_file(resolved_file_path, file_content)
                    logger.info("Specialized fix didn't resolve error, trying LLM")
            
            # Fall back to LLM-based fixing
            # Build fix messages — include all project files for cross-file context
            messages = self._build_compile_fix_messages(
                error,
                file_content,
                project_path,
                user_guidance,
                all_project_files=self._compilation.get_project_files(project_path),
            )
            
            # Get system prompt
            system_prompt = self.resources.load_context("about_p.txt")
            
            # Invoke LLM
            config = LLMConfig(max_tokens=4096)
            response = self.llm.complete(messages, config, system_prompt)
            
            # Extract fixed code
            filename, fixed_code = self._extract_p_code(response.content)
            
            if filename and fixed_code:
                # Write the fix
                self._compilation.write_file(resolved_file_path, fixed_code)
                
                # Verify by recompiling
                compile_result = self._compilation.compile(project_path)
                
                if compile_result.success:
                    # Fix worked!
                    self._tracker.clear(error_key)
                    self._status("Fix successful!")
                    
                    return FixResult(
                        success=True,
                        fixed=True,
                        filename=filename,
                        file_path=resolved_file_path,
                        original_code=file_content,
                        fixed_code=fixed_code,
                        attempt_number=attempt,
                        token_usage=response.usage.to_dict(),
                    )
                else:
                    # Fix didn't work
                    self._tracker.add_attempt(
                        error_key,
                        f"Attempt {attempt}: {compile_result.stdout[:200]}"
                    )
                    
                    return FixResult(
                        success=False,
                        fixed=False,
                        filename=filename,
                        file_path=resolved_file_path,
                        original_code=file_content,
                        fixed_code=fixed_code,
                        attempt_number=attempt,
                        error=compile_result.stdout,
                        token_usage=response.usage.to_dict(),
                    )
            else:
                return FixResult(
                    success=False,
                    error="Could not extract fixed code from LLM response",
                    attempt_number=attempt,
                )
                
        except Exception as e:
            logger.error(f"Error fixing compilation error: {e}")
            return FixResult(
                success=False,
                error=str(e),
                attempt_number=attempt,
            )
    
    def fix_checker_error(
        self,
        project_path: str,
        trace_log: str,
        error_category: Optional[str] = None,
        user_guidance: Optional[str] = None,
    ) -> FixResult:
        """
        Fix a PChecker error with enhanced analysis.
        
        Args:
            project_path: Path to the P project
            trace_log: The error trace from PChecker
            error_category: Optional category (e.g., 'assertion', 'deadlock')
            user_guidance: Optional guidance from user
            
        Returns:
            FixResult with fix details and analysis
        """
        error_key = f"checker:{project_path}:{hash(trace_log[:500])}"
        attempt = self._tracker.get_attempt_count(error_key) + 1
        
        self._status(f"Fixing checker error (attempt {attempt})...")
        
        # Read all project files
        project_files = self._compilation.get_project_files(project_path)
        
        # Step 1: Analyze the trace using specialized parser
        analysis_dict = {}
        root_cause = None
        suggested_fixes = []
        
        if HAS_CHECKER_FIXERS:
            try:
                trace_analysis, specialized_fix = analyze_and_suggest_fix(
                    trace_log, project_path, project_files
                )
                
                # Build analysis dict for response with enhanced context
                analysis_dict = {
                    "error_category": trace_analysis.error.category.value,
                    "error_message": trace_analysis.error.message,
                    "machine": trace_analysis.error.machine,
                    "state": trace_analysis.error.machine_state,
                    "event": trace_analysis.error.event_name,
                    "execution_steps": trace_analysis.execution_steps,
                    "machines_involved": trace_analysis.machines_involved,
                    "last_actions": trace_analysis.last_actions,
                }

                # Include enhanced analysis in response
                if trace_analysis.error.sender_info:
                    sender = trace_analysis.error.sender_info
                    analysis_dict["sender"] = {
                        "machine": sender.machine,
                        "state": sender.state,
                        "is_test_driver": sender.is_test_driver,
                        "is_initialization_pattern": sender.is_initialization_pattern,
                        "semantic_mismatch": sender.semantic_mismatch,
                    }
                if trace_analysis.error.cascading_impact:
                    cascade = trace_analysis.error.cascading_impact
                    analysis_dict["cascading_impact"] = {
                        "unhandled_in": cascade.unhandled_in,
                        "broadcasters": cascade.broadcasters,
                        "all_receivers": cascade.all_receivers,
                    }
                analysis_dict["is_test_driver_bug"] = trace_analysis.error.is_test_driver_bug
                analysis_dict["requires_new_event"] = trace_analysis.error.requires_new_event
                analysis_dict["requires_multi_file_fix"] = trace_analysis.error.requires_multi_file_fix
                if specialized_fix:
                    analysis_dict["fix_strategy"] = specialized_fix.fix_strategy
                    analysis_dict["is_multi_file_fix"] = specialized_fix.is_multi_file
                
                root_cause = trace_analysis.error.root_cause
                suggested_fixes = trace_analysis.error.suggested_fixes
                
                self._status(f"Identified error: {trace_analysis.error.category.value}")
                
                # Step 2: Try specialized fixer first
                if specialized_fix:
                    self._status(f"Applying specialized fix ({specialized_fix.fix_strategy or 'auto'}): {specialized_fix.description}")
                    
                    # Collect all file backups for potential revert
                    backups = {}
                    
                    # Apply primary fix
                    backups[specialized_fix.file_path] = specialized_fix.original_code
                    self._compilation.write_file(
                        specialized_fix.file_path,
                        specialized_fix.fixed_code
                    )
                    
                    # Apply additional patches for multi-file fixes
                    if specialized_fix.is_multi_file and specialized_fix.additional_patches:
                        for patch in specialized_fix.additional_patches:
                            backups[patch.file_path] = patch.original_code
                            self._compilation.write_file(
                                patch.file_path,
                                patch.fixed_code
                            )
                        self._status(
                            f"Applied {1 + len(specialized_fix.additional_patches)} "
                            f"file patches (strategy: {specialized_fix.fix_strategy})"
                        )
                    
                    # Verify by recompiling
                    compile_result = self._compilation.compile(project_path)
                    
                    if compile_result.success:
                        # Try checker with fewer schedules for quick verification
                        checker_result = self._compilation.run_checker(
                            project_path, schedules=20, timeout=30
                        )
                        
                        if checker_result.success:
                            # Vacuous pass detection: check if safety specs observed events
                            vacuous_warning = self._check_vacuous_pass(
                                project_path, project_files, trace_analysis
                            )

                            self._tracker.clear(error_key)
                            self._status("Fix successful (specialized fixer)!")
                            
                            result = FixResult(
                                success=True,
                                fixed=True,
                                filename=Path(specialized_fix.file_path).name,
                                file_path=specialized_fix.file_path,
                                original_code=specialized_fix.original_code,
                                fixed_code=specialized_fix.fixed_code,
                                attempt_number=attempt,
                                analysis=analysis_dict,
                                root_cause=root_cause,
                                suggested_fixes=suggested_fixes,
                                confidence=specialized_fix.confidence,
                            )
                            if vacuous_warning:
                                analysis_dict["vacuous_pass_warning"] = vacuous_warning
                            return result
                    
                    # Revert ALL patches if fix didn't work
                    for file_path, original_code in backups.items():
                        self._compilation.write_file(file_path, original_code)
                    logger.info("Specialized fix didn't resolve error, trying LLM")
                    
            except Exception as e:
                logger.warning(f"Specialized analysis failed: {e}")
        
        # Step 3: Check if we should request guidance
        if self._tracker.should_request_guidance(error_key) and not user_guidance:
            return self._create_enhanced_checker_guidance_request(
                error_key,
                trace_log,
                project_path,
                analysis_dict,
                root_cause,
                suggested_fixes,
            )
        
        # Step 4: Fall back to LLM-based fixing
        try:
            # Build fix messages with enhanced context (including sender & cascading analysis)
            messages = self._build_checker_fix_messages(
                trace_log,
                project_files,
                error_category,
                user_guidance,
                analysis_dict=analysis_dict,
                root_cause=root_cause,
                suggested_fixes=suggested_fixes,
            )
            
            # Get system prompt
            system_prompt = self.resources.load_context("about_p.txt")
            
            # Invoke LLM
            config = LLMConfig(max_tokens=4096)
            response = self.llm.complete(messages, config, system_prompt)
            
            # Extract all fixed files
            patches = self._extract_all_p_code(response.content)
            
            if patches:
                # Apply patches
                for filename, fixed_code in patches.items():
                    # Determine full path
                    for folder in ['PSrc', 'PSpec', 'PTst']:
                        full_path = Path(project_path) / folder / filename
                        if full_path.exists():
                            self._compilation.write_file(str(full_path), fixed_code)
                            break
                    else:
                        # Default to PSrc for new files
                        full_path = Path(project_path) / 'PSrc' / filename
                        self._compilation.write_file(str(full_path), fixed_code)
                
                # Verify by recompiling and rechecking
                compile_result = self._compilation.compile(project_path)
                
                if compile_result.success:
                    # Try checker
                    checker_result = self._compilation.run_checker(
                        project_path, schedules=20, timeout=30
                    )
                    
                    if checker_result.success:
                        self._tracker.clear(error_key)
                        self._status("Fix successful (LLM)!")
                        
                        return FixResult(
                            success=True,
                            fixed=True,
                            attempt_number=attempt,
                            token_usage=response.usage.to_dict(),
                            analysis=analysis_dict,
                            root_cause=root_cause,
                            suggested_fixes=suggested_fixes,
                        )
                
                # Fix didn't work
                self._tracker.add_attempt(
                    error_key,
                    f"Attempt {attempt}: Modified {list(patches.keys())}"
                )
                
                return FixResult(
                    success=False,
                    fixed=False,
                    attempt_number=attempt,
                    token_usage=response.usage.to_dict(),
                    analysis=analysis_dict,
                    root_cause=root_cause,
                    suggested_fixes=suggested_fixes,
                    error="Fix was applied but error persists",
                )
            else:
                return FixResult(
                    success=False,
                    error="Could not extract fixed code from LLM response",
                    attempt_number=attempt,
                    analysis=analysis_dict,
                    root_cause=root_cause,
                    suggested_fixes=suggested_fixes,
                )
                
        except Exception as e:
            logger.error(f"Error fixing checker error: {e}")
            return FixResult(
                success=False,
                error=str(e),
                attempt_number=attempt,
                analysis=analysis_dict,
                root_cause=root_cause,
                suggested_fixes=suggested_fixes,
            )
    
    def _create_enhanced_checker_guidance_request(
        self,
        error_key: str,
        trace_log: str,
        project_path: str,
        analysis: Dict[str, Any],
        root_cause: Optional[str],
        suggested_fixes: List[str],
    ) -> FixResult:
        """Create an enhanced guidance request with analysis."""
        previous_attempts = self._tracker.get_attempts(error_key)
        trace_summary = trace_log.splitlines()[-10:]
        
        return FixResult(
            success=False,
            fixed=False,
            needs_guidance=True,
            guidance_request={
                "context": f"Attempting to fix PChecker error in {project_path}",
                "problem": root_cause or "PChecker found a violation during model checking",
                "trace_summary": "\n".join(trace_summary),
                "attempts": previous_attempts,
                "questions": [
                    "What is the expected behavior when this event occurs?",
                    "Should the state machine handle this event differently?",
                    "Is there a race condition or ordering issue to address?",
                ],
                "suggested_actions": suggested_fixes or [
                    "Explain the expected state transition sequence",
                    "Provide invariants that should hold",
                    "Share the correct event handling logic",
                ],
            },
            attempt_number=self._tracker.get_attempt_count(error_key),
            analysis=analysis,
            root_cause=root_cause,
            suggested_fixes=suggested_fixes,
        )
    
    @staticmethod
    def _resolve_error_path(error: 'ParsedError', project_path: str) -> None:
        """
        Resolve a potentially-relative file_path inside a ParsedError to an
        absolute path so downstream read/write calls succeed regardless of cwd.
        """
        from pathlib import Path as _Path

        fp = _Path(error.file_path)
        if fp.is_absolute() and fp.exists():
            return
        # Try relative to the project directory first (most common)
        candidate = _Path(project_path) / error.file_path
        if candidate.exists():
            error.file_path = str(candidate)
            return
        # Try just the basename inside PSrc / PSpec / PTst
        basename = fp.name
        for sub in ('PSrc', 'PSpec', 'PTst'):
            candidate = _Path(project_path) / sub / basename
            if candidate.exists():
                error.file_path = str(candidate)
                return

    def fix_iteratively(
        self,
        project_path: str,
        max_iterations: int = 10,
    ) -> Dict[str, Any]:
        """
        Iteratively fix compilation errors until success or max iterations.
        
        Args:
            project_path: Path to the P project
            max_iterations: Maximum fix iterations
            
        Returns:
            Dictionary with final status and iteration details
        """
        self._status("Starting iterative compilation fix...")
        
        results = {
            "iterations": [],
            "success": False,
            "total_iterations": 0,
        }
        
        # Track error messages to detect spirals (same error repeating)
        recent_errors: List[str] = []
        SPIRAL_THRESHOLD = 3
        
        for i in range(max_iterations):
            self._progress("Compilation fix", i + 1, max_iterations)
            
            # Compile
            compile_result = self._compilation.compile(project_path)
            
            if compile_result.success:
                self._status(f"Compilation succeeded after {i} iterations")
                results["success"] = True
                results["total_iterations"] = i
                break
            
            # Parse error – try stdout first, then stderr as fallback
            combined = compile_result.stdout or ""
            if compile_result.stderr:
                combined = combined + "\n" + compile_result.stderr
            error = self._compilation.parse_error(combined)
            
            if error is None:
                self._error("Could not parse compilation error")
                results["iterations"].append({
                    "iteration": i + 1,
                    "error": "Could not parse error",
                    "output": combined[:500],
                })
                break
            
            # Detect spiral: if the same core error repeats, try incremental
            # regeneration once before giving up.
            core_msg = error.message[:80]
            recent_errors.append(core_msg)
            if recent_errors.count(core_msg) >= SPIRAL_THRESHOLD:
                # Attempt incremental regeneration: ask the LLM to rewrite
                # the failing file from scratch with the error as context.
                regen_result = self._try_incremental_regeneration(
                    project_path, error
                )
                if regen_result:
                    self._status(f"Incremental regeneration applied for {error.file_path}")
                    results["iterations"].append({
                        "iteration": i + 1,
                        "error": error.message,
                        "fixed": False,
                        "needs_guidance": False,
                        "incremental_regen": True,
                    })
                    # Clear spiral counter so the next compile gets a fresh shot
                    recent_errors.clear()
                    continue
                else:
                    self._warning(
                        f"Spiral detected: same error '{core_msg}' seen "
                        f"{SPIRAL_THRESHOLD} times. Stopping."
                    )
                    results["iterations"].append({
                        "iteration": i + 1,
                        "error": error.message,
                        "fixed": False,
                        "needs_guidance": False,
                        "spiral_detected": True,
                    })
                    break
            
            # Resolve relative paths so read/write works
            self._resolve_error_path(error, project_path)
            
            # Try to fix
            fix_result = self.fix_compilation_error(project_path, error)
            
            results["iterations"].append({
                "iteration": i + 1,
                "error": error.message,
                "fixed": fix_result.fixed,
                "needs_guidance": fix_result.needs_guidance,
            })
            
            if fix_result.needs_guidance:
                self._warning("Human guidance needed")
                results["needs_guidance"] = True
                results["guidance_request"] = fix_result.guidance_request
                break
        else:
            self._warning(f"Max iterations ({max_iterations}) reached")
            results["total_iterations"] = max_iterations
        
        return results
    
    def _try_incremental_regeneration(
        self,
        project_path: str,
        error: 'ParsedError',
    ) -> bool:
        """
        Attempt to regenerate just the failing file from scratch.

        Reads all other project files as context, asks the LLM to rewrite
        the broken file incorporating the error message, and writes it back.

        Returns True if a new version was written, False otherwise.
        """
        try:
            resolved = self._resolve_error_file_path(project_path, error.file_path)
            old_code = self._compilation.read_file(resolved)
            if old_code is None:
                return False

            all_files = self._compilation.get_project_files(project_path)
            filename = Path(resolved).name

            # Build context from all *other* files
            context_parts = []
            for rel_path, content in all_files.items():
                if Path(rel_path).name != filename:
                    context_parts.append(f"<{Path(rel_path).name}>\n{content}\n</{Path(rel_path).name}>")

            system_prompt = self.resources.load_context("about_p.txt")
            messages = [
                Message(role=MessageRole.USER, content="\n".join(context_parts)),
                Message(role=MessageRole.USER, content=(
                    f"The file {filename} has a compilation error that could not be fixed "
                    f"after multiple attempts:\n"
                    f"  Error at line {error.line_number}: {error.message}\n\n"
                    f"Current broken code:\n```\n{old_code}\n```\n\n"
                    f"Please rewrite {filename} from scratch so it compiles correctly. "
                    f"Use the types, events, and machines defined in the other project files above. "
                    f"Return the complete file wrapped in <{filename}>...</{filename}> tags."
                )),
            ]

            config = LLMConfig(max_tokens=4096)
            response = self.llm.complete(messages, config, system_prompt)

            # Extract code
            import re as _re
            match = _re.search(rf'<{_re.escape(filename)}>(.*?)</{_re.escape(filename)}>', response.content, _re.DOTALL)
            if match:
                new_code = match.group(1).strip()
                self._compilation.write_file(resolved, new_code)
                return True

        except Exception as e:
            logger.warning(f"Incremental regeneration failed: {e}")

        return False

    # =========================================================================
    # Vacuous pass detection
    # =========================================================================

    def _check_vacuous_pass(
        self,
        project_path: str,
        project_files: Dict[str, str],
        trace_analysis: Any = None,
    ) -> Optional[str]:
        """
        Check if a fix might have caused the test to pass vacuously.
        
        A vacuous pass occurs when a safety specification monitors events that
        are never delivered (e.g., because all are ignored), so the spec
        trivially passes without actually verifying anything.
        
        Returns a warning string if a vacuous pass is suspected, None otherwise.
        """
        try:
            # Find all spec machines and the events they observe
            spec_events: Dict[str, List[str]] = {}  # spec_name -> [events]
            for filepath, content in project_files.items():
                if not (filepath.startswith('PSpec/') or '/PSpec/' in filepath):
                    continue

                # Find spec declarations: "spec SpecName observes event1, event2 {"
                spec_pattern = r'spec\s+(\w+)\s+observes\s+([^{]+)\{'
                for match in re.finditer(spec_pattern, content):
                    spec_name = match.group(1)
                    events_str = match.group(2).strip()
                    events = [e.strip() for e in events_str.split(',')]
                    spec_events[spec_name] = events

            if not spec_events:
                return None

            # Check if any observed event is now universally ignored by all protocol machines
            # Re-read current project files (after fix was applied)
            current_files = self._compilation.get_project_files(project_path)

            warnings = []
            for spec_name, events in spec_events.items():
                for event in events:
                    # Check if any protocol machine still sends this event
                    has_sender = False
                    for filepath, content in current_files.items():
                        if filepath.startswith('PSpec/') or filepath.startswith('PTst/'):
                            continue
                        send_pattern = rf'send\s+[^,]+\s*,\s*{re.escape(event)}\b'
                        if re.search(send_pattern, content):
                            has_sender = True
                            break

                    if not has_sender:
                        warnings.append(
                            f"Spec '{spec_name}' observes event '{event}' but no "
                            f"protocol machine sends it — the spec may pass vacuously."
                        )

            if warnings:
                return " | ".join(warnings)

        except Exception as e:
            logger.debug(f"Vacuous pass check failed: {e}")

        return None

    # =========================================================================
    # Private helper methods
    # =========================================================================

    def _resolve_error_file_path(self, project_path: str, file_path: str) -> str:
        """
        Resolve compiler-reported file names to absolute project paths.
        P compiler may emit either absolute paths or basename like 'Safety.p'.
        """
        candidate = Path(file_path)
        if candidate.is_absolute() and candidate.exists():
            return str(candidate)
        if candidate.exists():
            return str(candidate.resolve())

        # Try common P project folders.
        for folder in ("PSrc", "PSpec", "PTst"):
            full_path = Path(project_path) / folder / file_path
            if full_path.exists():
                return str(full_path)

        # Fallback to original string; callers will surface read/write errors.
        return file_path
    
    def _build_compile_fix_messages(
        self,
        error: ParsedError,
        file_content: str,
        project_path: str,
        user_guidance: Optional[str],
        all_project_files: Optional[Dict[str, str]] = None,
    ) -> List[Message]:
        """Build messages for compilation error fixing, including cross-file context."""
        messages = []
        
        # Add guides
        p_basics = self.resources.load_modular_context("p_basics.txt")
        compiler_guide = self.resources.load_modular_context("p_compiler_guide.txt")
        
        try:
            common_errors = self.resources.load_modular_context("p_common_compilation_errors.txt")
            messages.append(Message(
                role=MessageRole.USER,
                content=f"<common_errors>\n{common_errors}\n</common_errors>"
            ))
        except:
            pass
        
        messages.append(Message(
            role=MessageRole.USER,
            content=f"<p_basics>\n{p_basics}\n</p_basics>"
        ))
        messages.append(Message(
            role=MessageRole.USER,
            content=f"<compiler_guide>\n{compiler_guide}\n</compiler_guide>"
        ))
        
        # Add ALL project files for cross-file context (types, other machines, etc.)
        error_filename = Path(error.file_path).name
        if all_project_files:
            for rel_path, content in all_project_files.items():
                fname = Path(rel_path).name
                if fname == error_filename:
                    continue  # Will be added separately as the error file
                messages.append(Message(
                    role=MessageRole.USER,
                    content=f"<{fname}>\n{content}\n</{fname}>"
                ))
        
        # Add the error file content
        messages.append(Message(
            role=MessageRole.USER,
            content=f"File with error ({error_filename}):\n```\n{file_content}\n```"
        ))
        
        # Add error details
        messages.append(Message(
            role=MessageRole.USER,
            content=f"""
Compilation error at line {error.line_number}, column {error.column_number}:
{error.message}
"""
        ))
        
        # Add previous attempts
        error_key = f"compile:{error.file_path}:{hash(error.message)}"
        previous = self._tracker.get_attempts(error_key)
        if previous:
            messages.append(Message(
                role=MessageRole.USER,
                content="Previous fix attempts that failed:\n" + "\n".join(f"- {a}" for a in previous)
            ))
        
        # Add user guidance
        if user_guidance:
            messages.append(Message(
                role=MessageRole.USER,
                content=f"User guidance: {user_guidance}"
            ))
        
        # Add instruction
        messages.append(Message(
            role=MessageRole.USER,
            content=f"""
Please fix the compilation error in this P code.
The error may be caused by mismatches with types/events defined in other project files shown above.
Return the complete fixed file content wrapped in XML tags using the filename.
Example: <{error_filename}>...fixed code...</{error_filename}>
"""
        ))
        
        return messages
    
    def _build_checker_fix_messages(
        self,
        trace_log: str,
        project_files: Dict[str, str],
        error_category: Optional[str],
        user_guidance: Optional[str],
        analysis_dict: Optional[Dict[str, Any]] = None,
        root_cause: Optional[str] = None,
        suggested_fixes: Optional[List[str]] = None,
    ) -> List[Message]:
        """Build messages for checker error fixing with enhanced context."""
        messages = []
        
        # Add guide
        p_basics = self.resources.load_modular_context("p_basics.txt")
        messages.append(Message(
            role=MessageRole.USER,
            content=f"<p_basics>\n{p_basics}\n</p_basics>"
        ))
        
        # Add all project files, clearly labeled by folder
        for filepath, content in project_files.items():
            messages.append(Message(
                role=MessageRole.USER,
                content=f"<{filepath}>\n{content}\n</{filepath}>"
            ))
        
        # Add trace (last 50 lines)
        trace_lines = trace_log.splitlines()[-50:]
        messages.append(Message(
            role=MessageRole.USER,
            content=f"PChecker Error Trace (last 50 lines):\n" + "\n".join(trace_lines)
        ))
        
        if error_category:
            messages.append(Message(
                role=MessageRole.USER,
                content=f"Error category: {error_category}"
            ))

        # Add enhanced analysis context
        if root_cause:
            messages.append(Message(
                role=MessageRole.USER,
                content=f"Root cause analysis: {root_cause}"
            ))

        if analysis_dict:
            # Include sender analysis
            sender = analysis_dict.get("sender")
            if sender:
                sender_ctx = (
                    f"Sender analysis:\n"
                    f"- Event sent by: {sender.get('machine', 'unknown')} "
                    f"in state '{sender.get('state', 'unknown')}'\n"
                    f"- Is test driver: {sender.get('is_test_driver', False)}\n"
                    f"- Is initialization pattern: {sender.get('is_initialization_pattern', False)}"
                )
                if sender.get("semantic_mismatch"):
                    sender_ctx += f"\n- Semantic mismatch: {sender['semantic_mismatch']}"
                messages.append(Message(
                    role=MessageRole.USER,
                    content=sender_ctx
                ))

            # Include cascading impact
            cascade = analysis_dict.get("cascading_impact")
            if cascade and cascade.get("unhandled_in"):
                cascade_ctx = "Cascading impact analysis:\n"
                for machine, states in cascade["unhandled_in"].items():
                    cascade_ctx += f"- {machine} also lacks handler in states: {', '.join(states)}\n"
                messages.append(Message(
                    role=MessageRole.USER,
                    content=cascade_ctx
                ))

            # Include fix strategy hints
            if analysis_dict.get("is_test_driver_bug"):
                messages.append(Message(
                    role=MessageRole.USER,
                    content=(
                        "IMPORTANT: This bug originates in the TEST DRIVER, not the protocol. "
                        "The test driver is misusing a protocol event for initialization. "
                        "The fix should introduce a new setup event and modify the test driver, "
                        "not just add ignore statements to protocol machines."
                    )
                ))
            if analysis_dict.get("requires_multi_file_fix"):
                messages.append(Message(
                    role=MessageRole.USER,
                    content=(
                        "IMPORTANT: This requires changes to MULTIPLE files. "
                        "Return ALL modified files, including types, machines, and test driver."
                    )
                ))

        if suggested_fixes:
            messages.append(Message(
                role=MessageRole.USER,
                content="Suggested fix approaches:\n" + "\n".join(
                    f"- {fix}" for fix in suggested_fixes
                )
            ))
        
        # Add user guidance
        if user_guidance:
            messages.append(Message(
                role=MessageRole.USER,
                content=f"User guidance: {user_guidance}"
            ))
        
        # Add instruction
        messages.append(Message(
            role=MessageRole.USER,
            content="""
Analyze the PChecker trace and fix the error.

CRITICAL RULES:
1. If the bug is in a test driver (PTst/ file), fix the test driver — don't just mask 
   the symptom by adding ignore to protocol machines.
2. If a protocol event is being misused for initialization, introduce a NEW dedicated 
   setup event in the types file and add handlers accordingly.
3. If multiple machines are affected, fix ALL of them. Return every modified file.
4. Ensure safety specifications can still observe the events they monitor 
   (don't make tests pass vacuously by suppressing all events).

Return any modified files wrapped in XML tags using their filenames.
Example: <Enums_Types_Events.p>...fixed types code...</Enums_Types_Events.p>
<MachineName.p>...fixed machine code...</MachineName.p>
<TestDriver.p>...fixed test code...</TestDriver.p>
"""
        ))
        
        return messages
    
    def _create_guidance_request(
        self,
        error_key: str,
        error_type: str,
        error: ParsedError,
        project_path: str,
    ) -> FixResult:
        """Create a guidance request for human-in-the-loop"""
        previous_attempts = self._tracker.get_attempts(error_key)
        
        return FixResult(
            success=False,
            fixed=False,
            needs_guidance=True,
            guidance_request={
                "context": f"Attempting to fix {error_type} error in {error.file_path}",
                "problem": error.message,
                "location": f"Line {error.line_number}, Column {error.column_number}",
                "attempts": previous_attempts,
                "questions": [
                    f"What is the expected behavior at line {error.line_number}?",
                    "Are there any missing type definitions or event declarations?",
                    "Should this code use a different P language construct?",
                ],
                "suggested_actions": [
                    "Provide the correct type definition",
                    "Explain the expected state machine behavior",
                    "Share similar working code as a reference",
                ],
            },
            attempt_number=self._tracker.get_attempt_count(error_key),
        )
    
    def _create_checker_guidance_request(
        self,
        error_key: str,
        trace_log: str,
        project_path: str,
    ) -> FixResult:
        """Create a guidance request for checker errors"""
        previous_attempts = self._tracker.get_attempts(error_key)
        
        # Extract key info from trace
        trace_summary = trace_log.splitlines()[-10:]
        
        return FixResult(
            success=False,
            fixed=False,
            needs_guidance=True,
            guidance_request={
                "context": f"Attempting to fix PChecker error in {project_path}",
                "problem": "PChecker found a violation during model checking",
                "trace_summary": "\n".join(trace_summary),
                "attempts": previous_attempts,
                "questions": [
                    "What is the expected behavior when this event occurs?",
                    "Should the state machine handle this event differently?",
                    "Is there a race condition or ordering issue to address?",
                ],
                "suggested_actions": [
                    "Explain the expected state transition sequence",
                    "Provide invariants that should hold",
                    "Share the correct event handling logic",
                ],
            },
            attempt_number=self._tracker.get_attempt_count(error_key),
        )
    
    def _extract_p_code(self, response: str) -> tuple:
        """Extract single P code file from response"""
        pattern = r'<(\w+\.p)>(.*?)</\1>'
        match = re.search(pattern, response, re.DOTALL)
        
        if match:
            return match.group(1), match.group(2).strip()
        return None, None
    
    def _extract_all_p_code(self, response: str) -> Dict[str, str]:
        """Extract all P code files from response"""
        pattern = r'<([^>]+\.p)>(.*?)</\1>'
        matches = re.findall(pattern, response, re.DOTALL)
        
        return {filename: code.strip() for filename, code in matches}
