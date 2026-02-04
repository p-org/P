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
        error_key = f"compile:{error.file_path}:{hash(error.message)}"
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
            file_content = self._compilation.read_file(error.file_path)
            if file_content is None:
                return FixResult(
                    success=False,
                    error=f"Could not read file: {error.file_path}",
                )
            
            # First try specialized fixers (faster, more reliable)
            specialized_fix = self._try_specialized_fix(error, file_content)
            if specialized_fix:
                # Apply the fix
                self._compilation.write_file(error.file_path, specialized_fix.fixed_code)
                
                # Verify by recompiling
                compile_result = self._compilation.compile(project_path)
                
                if compile_result.success:
                    self._tracker.clear(error_key)
                    self._status(f"Fix successful (specialized): {specialized_fix.description}")
                    
                    return FixResult(
                        success=True,
                        fixed=True,
                        filename=Path(error.file_path).name,
                        file_path=error.file_path,
                        original_code=file_content,
                        fixed_code=specialized_fix.fixed_code,
                        attempt_number=attempt,
                    )
                else:
                    # Revert the fix and try LLM
                    self._compilation.write_file(error.file_path, file_content)
                    logger.info("Specialized fix didn't resolve error, trying LLM")
            
            # Fall back to LLM-based fixing
            # Build fix messages
            messages = self._build_compile_fix_messages(
                error,
                file_content,
                project_path,
                user_guidance,
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
                self._compilation.write_file(error.file_path, fixed_code)
                
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
                        file_path=error.file_path,
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
                        file_path=error.file_path,
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
                
                # Build analysis dict for response
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
                
                root_cause = trace_analysis.error.root_cause
                suggested_fixes = trace_analysis.error.suggested_fixes
                
                self._status(f"Identified error: {trace_analysis.error.category.value}")
                
                # Step 2: Try specialized fixer first
                if specialized_fix:
                    self._status(f"Applying specialized fix: {specialized_fix.description}")
                    
                    # Write the fix
                    self._compilation.write_file(
                        specialized_fix.file_path,
                        specialized_fix.fixed_code
                    )
                    
                    # Verify by recompiling
                    compile_result = self._compilation.compile(project_path)
                    
                    if compile_result.success:
                        # Try checker with fewer schedules for quick verification
                        checker_result = self._compilation.run_checker(
                            project_path, schedules=20, timeout=30
                        )
                        
                        if checker_result.success:
                            self._tracker.clear(error_key)
                            self._status("Fix successful (specialized fixer)!")
                            
                            return FixResult(
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
                    
                    # Revert if fix didn't work
                    self._compilation.write_file(
                        specialized_fix.file_path,
                        specialized_fix.original_code
                    )
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
            # Build fix messages with enhanced context
            messages = self._build_checker_fix_messages(
                trace_log,
                project_files,
                error_category,
                user_guidance,
            )
            
            # Add analysis context if available
            if root_cause:
                messages.insert(-1, Message(
                    role=MessageRole.USER,
                    content=f"Root cause analysis: {root_cause}"
                ))
            
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
        
        for i in range(max_iterations):
            self._progress("Compilation fix", i + 1, max_iterations)
            
            # Compile
            compile_result = self._compilation.compile(project_path)
            
            if compile_result.success:
                self._status(f"Compilation succeeded after {i} iterations")
                results["success"] = True
                results["total_iterations"] = i
                break
            
            # Parse error
            error = self._compilation.parse_error(compile_result.stdout)
            
            if error is None:
                self._error("Could not parse compilation error")
                results["iterations"].append({
                    "iteration": i + 1,
                    "error": "Could not parse error",
                    "output": compile_result.stdout[:500],
                })
                break
            
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
    
    # =========================================================================
    # Private helper methods
    # =========================================================================
    
    def _build_compile_fix_messages(
        self,
        error: ParsedError,
        file_content: str,
        project_path: str,
        user_guidance: Optional[str],
    ) -> List[Message]:
        """Build messages for compilation error fixing"""
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
        
        # Add file content
        filename = Path(error.file_path).name
        messages.append(Message(
            role=MessageRole.USER,
            content=f"File with error ({filename}):\n```\n{file_content}\n```"
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
Return the complete fixed file content wrapped in XML tags using the filename.
Example: <{filename}>...fixed code...</{filename}>
"""
        ))
        
        return messages
    
    def _build_checker_fix_messages(
        self,
        trace_log: str,
        project_files: Dict[str, str],
        error_category: Optional[str],
        user_guidance: Optional[str],
    ) -> List[Message]:
        """Build messages for checker error fixing"""
        messages = []
        
        # Add guide
        p_basics = self.resources.load_modular_context("p_basics.txt")
        messages.append(Message(
            role=MessageRole.USER,
            content=f"<p_basics>\n{p_basics}\n</p_basics>"
        ))
        
        # Add all project files
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
Return any modified files wrapped in XML tags using their filenames.
Example: <MachineName.p>...fixed code...</MachineName.p>
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
