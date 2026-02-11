"""
Environment Detection for P Compiler and .NET SDK

Auto-detects the location of P compiler and dotnet SDK.
"""

import os
import shutil
import subprocess
import logging
from pathlib import Path
from typing import Optional, Dict, Any
from dataclasses import dataclass

logger = logging.getLogger(__name__)


@dataclass
class EnvironmentInfo:
    """Information about the P development environment."""
    p_compiler_path: Optional[str] = None
    dotnet_path: Optional[str] = None
    dotnet_version: Optional[str] = None
    p_version: Optional[str] = None
    is_valid: bool = False
    issues: list = None
    
    def __post_init__(self):
        if self.issues is None:
            self.issues = []
    
    def to_dict(self) -> Dict[str, Any]:
        return {
            "p_compiler_path": self.p_compiler_path,
            "dotnet_path": self.dotnet_path,
            "dotnet_version": self.dotnet_version,
            "p_version": self.p_version,
            "is_valid": self.is_valid,
            "issues": self.issues,
        }


class EnvironmentDetector:
    """Detects and validates the P development environment."""
    
    # Common paths to search for P compiler
    P_SEARCH_PATHS = [
        Path.home() / ".dotnet" / "tools" / "p",
        Path("/usr/local/bin/p"),
        Path("/opt/homebrew/bin/p"),
    ]
    
    # Common paths to search for dotnet
    DOTNET_SEARCH_PATHS = [
        Path("/usr/local/share/dotnet/dotnet"),
        Path.home() / ".dotnet" / "dotnet",
        Path("/opt/homebrew/bin/dotnet"),
        Path("/usr/bin/dotnet"),
    ]
    
    @classmethod
    def detect(cls) -> EnvironmentInfo:
        """Detect the P development environment."""
        info = EnvironmentInfo()
        
        # Find P compiler
        info.p_compiler_path = cls._find_p_compiler()
        if not info.p_compiler_path:
            info.issues.append("P compiler not found. Install with: dotnet tool install -g P")
        
        # Find dotnet
        info.dotnet_path = cls._find_dotnet()
        if not info.dotnet_path:
            info.issues.append("dotnet SDK not found. Install from: https://dotnet.microsoft.com/download")
        else:
            info.dotnet_version = cls._get_dotnet_version(info.dotnet_path)
        
        # Get P version if available
        if info.p_compiler_path:
            info.p_version = cls._get_p_version(info.p_compiler_path)
        
        # Validate
        info.is_valid = info.p_compiler_path is not None and info.dotnet_path is not None
        
        return info
    
    @classmethod
    def _find_p_compiler(cls) -> Optional[str]:
        """Find the P compiler."""
        # First try which/where
        p_path = shutil.which("p")
        if p_path:
            return p_path
        
        # Search common paths
        for path in cls.P_SEARCH_PATHS:
            if path.exists():
                return str(path)
        
        # Check if it's in PATH but not found by which (Windows edge case)
        try:
            result = subprocess.run(
                ["p", "--version"],
                capture_output=True,
                timeout=5,
            )
            if result.returncode == 0:
                return "p"  # It's in PATH
        except (subprocess.SubprocessError, FileNotFoundError):
            pass
        
        return None
    
    @classmethod
    def _find_dotnet(cls) -> Optional[str]:
        """Find the dotnet SDK."""
        # First try which/where
        dotnet_path = shutil.which("dotnet")
        if dotnet_path:
            return dotnet_path
        
        # Search common paths
        for path in cls.DOTNET_SEARCH_PATHS:
            if path.exists():
                return str(path)
        
        # Check DOTNET_ROOT env var
        dotnet_root = os.environ.get("DOTNET_ROOT")
        if dotnet_root:
            dotnet_exe = Path(dotnet_root) / "dotnet"
            if dotnet_exe.exists():
                return str(dotnet_exe)
        
        return None
    
    @classmethod
    def _get_dotnet_version(cls, dotnet_path: str) -> Optional[str]:
        """Get the dotnet version."""
        try:
            result = subprocess.run(
                [dotnet_path, "--version"],
                capture_output=True,
                text=True,
                timeout=10,
            )
            if result.returncode == 0:
                return result.stdout.strip()
        except (subprocess.SubprocessError, FileNotFoundError):
            pass
        return None
    
    @classmethod
    def _get_p_version(cls, p_path: str) -> Optional[str]:
        """Get the P compiler version."""
        try:
            # P doesn't have a direct --version flag, but we can check
            # This is a placeholder - adjust based on actual P CLI
            return "installed"
        except Exception:
            pass
        return None
    
    @classmethod
    def get_environment_vars(cls) -> Dict[str, str]:
        """Get environment variables needed for P compilation."""
        info = cls.detect()
        env = os.environ.copy()
        
        if info.p_compiler_path:
            # Add P compiler directory to PATH
            p_dir = str(Path(info.p_compiler_path).parent)
            env["PATH"] = f"{p_dir}:{env.get('PATH', '')}"
        
        if info.dotnet_path:
            # Add dotnet directory to PATH
            dotnet_dir = str(Path(info.dotnet_path).parent)
            env["PATH"] = f"{dotnet_dir}:{env.get('PATH', '')}"
            env["DOTNET_ROOT"] = dotnet_dir
        
        return env
    
    @classmethod
    def setup_environment(cls) -> bool:
        """
        Set up the environment for P compilation.
        Returns True if successful.
        """
        info = cls.detect()
        
        if not info.is_valid:
            logger.error(f"Invalid P environment: {info.issues}")
            return False
        
        # Update os.environ
        if info.p_compiler_path:
            p_dir = str(Path(info.p_compiler_path).parent)
            current_path = os.environ.get("PATH", "")
            if p_dir not in current_path:
                os.environ["PATH"] = f"{p_dir}:{current_path}"
        
        if info.dotnet_path:
            dotnet_dir = str(Path(info.dotnet_path).parent)
            current_path = os.environ.get("PATH", "")
            if dotnet_dir not in current_path:
                os.environ["PATH"] = f"{dotnet_dir}:{current_path}"
            os.environ["DOTNET_ROOT"] = dotnet_dir
        
        logger.info(f"P environment configured: P={info.p_compiler_path}, dotnet={info.dotnet_path}")
        return True


def ensure_environment() -> EnvironmentInfo:
    """
    Ensure the P development environment is properly set up.
    Returns environment info with any issues.
    """
    info = EnvironmentDetector.detect()
    
    if info.is_valid:
        EnvironmentDetector.setup_environment()
    
    return info


def get_compile_command(project_path: str) -> tuple:
    """
    Get the command and environment for compiling a P project.
    Returns (command_list, environment_dict).
    """
    info = EnvironmentDetector.detect()
    env = EnvironmentDetector.get_environment_vars()
    
    if info.p_compiler_path:
        cmd = [info.p_compiler_path, "compile"]
    else:
        cmd = ["p", "compile"]  # Hope it's in PATH
    
    return cmd, env


def get_check_command(project_path: str, test_case: str = None, schedules: int = 100) -> tuple:
    """
    Get the command and environment for running PChecker.
    Returns (command_list, environment_dict).
    """
    info = EnvironmentDetector.detect()
    env = EnvironmentDetector.get_environment_vars()
    
    if info.p_compiler_path:
        cmd = [info.p_compiler_path, "check"]
    else:
        cmd = ["p", "check"]
    
    cmd.extend(["-s", str(schedules)])
    
    if test_case:
        cmd.extend(["-tc", test_case])
    
    return cmd, env
