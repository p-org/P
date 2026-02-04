"""
Compilation Service

Handles P project compilation and PChecker execution.
This service is UI-agnostic.
"""

import subprocess
import logging
from pathlib import Path
from typing import Dict, Any, Optional, List, Tuple
from dataclasses import dataclass, field

from .base import BaseService, ServiceResult, EventCallback, ResourceLoader
from ..llm import LLMProvider

logger = logging.getLogger(__name__)


@dataclass
class CompilationResult(ServiceResult):
    """Result of a compilation operation"""
    stdout: str = ""
    stderr: str = ""
    return_code: int = -1


@dataclass
class CheckerResult(ServiceResult):
    """Result of a PChecker run"""
    test_results: Dict[str, bool] = field(default_factory=dict)
    passed_tests: List[str] = field(default_factory=list)
    failed_tests: List[str] = field(default_factory=list)
    trace_logs: Dict[str, str] = field(default_factory=dict)


@dataclass
class ParsedError:
    """A parsed compilation error"""
    file_path: str
    line_number: int
    column_number: int
    message: str
    error_type: str = "unknown"


class CompilationService(BaseService):
    """
    Service for compiling P projects and running PChecker.
    
    Provides:
    - Project compilation
    - Error parsing
    - PChecker execution
    - Result analysis
    """
    
    def __init__(
        self,
        llm_provider: Optional[LLMProvider] = None,
        resource_loader: Optional[ResourceLoader] = None,
        callbacks: Optional[EventCallback] = None,
    ):
        super().__init__(llm_provider, resource_loader, callbacks)
    
    def compile(self, project_path: str) -> CompilationResult:
        """
        Compile a P project.
        
        Args:
            project_path: Path to the P project directory
            
        Returns:
            CompilationResult with compilation output
        """
        self._status(f"Compiling P project at {project_path}...")
        
        try:
            # Verify project path exists
            if not Path(project_path).exists():
                return CompilationResult(
                    success=False,
                    error=f"Project path does not exist: {project_path}",
                    return_code=-1,
                )
            
            # Run P compiler
            result = subprocess.run(
                ['p', 'compile'],
                capture_output=True,
                cwd=project_path,
                text=True,
                timeout=300,  # 5 minute timeout
            )
            
            # Check for success
            success = "succeeded" in result.stdout.lower() or result.returncode == 0
            
            if success:
                self._status("Compilation succeeded")
            else:
                self._warning("Compilation failed")
            
            return CompilationResult(
                success=success,
                stdout=result.stdout,
                stderr=result.stderr,
                return_code=result.returncode,
            )
            
        except FileNotFoundError:
            return CompilationResult(
                success=False,
                error="P compiler not found. Make sure 'p' is in your PATH.",
                return_code=-1,
            )
        except subprocess.TimeoutExpired:
            return CompilationResult(
                success=False,
                error="Compilation timed out after 5 minutes",
                return_code=-1,
            )
        except Exception as e:
            logger.error(f"Compilation error: {e}")
            return CompilationResult(
                success=False,
                error=str(e),
                return_code=-1,
            )
    
    def parse_error(self, compilation_output: str) -> Optional[ParsedError]:
        """
        Parse the first error from compilation output.
        
        Args:
            compilation_output: The stdout/stderr from compilation
            
        Returns:
            ParsedError if an error was found, None otherwise
        """
        import re
        
        # Pattern for P compiler errors: filepath(line,col): error: message
        error_pattern = r'([^(\n]+)\((\d+),\s*(\d+)\):\s*(error|warning):\s*(.+?)(?=\n[^\s]|\Z)'
        
        match = re.search(error_pattern, compilation_output, re.MULTILINE | re.DOTALL)
        
        if match:
            return ParsedError(
                file_path=match.group(1).strip(),
                line_number=int(match.group(2)),
                column_number=int(match.group(3)),
                error_type=match.group(4),
                message=match.group(5).strip(),
            )
        
        return None
    
    def get_all_errors(self, compilation_output: str) -> List[ParsedError]:
        """
        Parse all errors from compilation output.
        
        Args:
            compilation_output: The stdout/stderr from compilation
            
        Returns:
            List of ParsedError objects
        """
        import re
        
        errors = []
        error_pattern = r'([^(\n]+)\((\d+),\s*(\d+)\):\s*(error|warning):\s*(.+?)(?=\n[^\s]|\Z)'
        
        for match in re.finditer(error_pattern, compilation_output, re.MULTILINE | re.DOTALL):
            errors.append(ParsedError(
                file_path=match.group(1).strip(),
                line_number=int(match.group(2)),
                column_number=int(match.group(3)),
                error_type=match.group(4),
                message=match.group(5).strip(),
            ))
        
        return errors
    
    def run_checker(
        self,
        project_path: str,
        schedules: int = 100,
        timeout: int = 60,
        test_name: Optional[str] = None,
    ) -> CheckerResult:
        """
        Run PChecker on a P project.
        
        Args:
            project_path: Path to the P project
            schedules: Number of schedules to explore
            timeout: Timeout in seconds
            test_name: Optional specific test to run
            
        Returns:
            CheckerResult with test results
        """
        self._status(f"Running PChecker with {schedules} schedules...")
        
        try:
            # Import checker utils
            from utils import checker_utils
            
            results, trace_dicts, trace_logs = checker_utils.try_pchecker(
                project_path,
                schedules=schedules,
                timeout=timeout,
            )
            
            # Build results
            test_results = {}
            passed_tests = []
            failed_tests = []
            
            for test, passed in results.items():
                test_results[test] = passed
                if passed:
                    passed_tests.append(test)
                else:
                    failed_tests.append(test)
            
            all_passed = all(results.values()) if results else False
            
            if all_passed:
                self._status(f"All {len(passed_tests)} tests passed")
            else:
                self._warning(f"{len(failed_tests)} test(s) failed")
            
            return CheckerResult(
                success=all_passed,
                test_results=test_results,
                passed_tests=passed_tests,
                failed_tests=failed_tests,
                trace_logs=trace_logs,
            )
            
        except Exception as e:
            logger.error(f"PChecker error: {e}")
            return CheckerResult(
                success=False,
                error=str(e),
            )
    
    def get_project_files(self, project_path: str) -> Dict[str, str]:
        """
        Get all P files in a project.
        
        Args:
            project_path: Path to the P project
            
        Returns:
            Dictionary mapping relative file paths to contents
        """
        files = {}
        project = Path(project_path)
        
        for folder in ['PSrc', 'PSpec', 'PTst']:
            folder_path = project / folder
            if folder_path.exists():
                for p_file in folder_path.glob('*.p'):
                    rel_path = f"{folder}/{p_file.name}"
                    try:
                        files[rel_path] = p_file.read_text(encoding='utf-8')
                    except Exception as e:
                        logger.warning(f"Could not read {rel_path}: {e}")
        
        return files
    
    def read_file(self, file_path: str) -> Optional[str]:
        """
        Read a single file's contents.
        
        Args:
            file_path: Path to the file
            
        Returns:
            File contents or None if file doesn't exist
        """
        try:
            return Path(file_path).read_text(encoding='utf-8')
        except Exception as e:
            logger.warning(f"Could not read {file_path}: {e}")
            return None
    
    def write_file(self, file_path: str, content: str) -> bool:
        """
        Write content to a file.
        
        Args:
            file_path: Path to the file
            content: Content to write
            
        Returns:
            True if successful, False otherwise
        """
        try:
            Path(file_path).parent.mkdir(parents=True, exist_ok=True)
            Path(file_path).write_text(content, encoding='utf-8')
            return True
        except Exception as e:
            logger.error(f"Could not write {file_path}: {e}")
            return False


