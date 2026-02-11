"""
Base classes for services.

Provides common functionality for all services including
resource loading and LLM provider access.
"""

import os
import logging
from pathlib import Path
from typing import Optional, Dict, Any, Callable
from dataclasses import dataclass, field

from ..llm import LLMProvider, get_default_provider

logger = logging.getLogger(__name__)


@dataclass
class ServiceResult:
    """Base class for service operation results"""
    success: bool
    error: Optional[str] = None
    token_usage: Dict[str, int] = field(default_factory=dict)
    
    def to_dict(self) -> Dict[str, Any]:
        """Convert to dictionary"""
        return {
            "success": self.success,
            "error": self.error,
            "token_usage": self.token_usage,
        }


class EventCallback:
    """
    Callback interface for service events.
    
    This replaces direct Streamlit calls (backend_status.write)
    with a callback-based approach that any UI can implement.
    """
    
    def __init__(
        self,
        on_status: Optional[Callable[[str], None]] = None,
        on_progress: Optional[Callable[[str, int, int], None]] = None,
        on_error: Optional[Callable[[str], None]] = None,
        on_warning: Optional[Callable[[str], None]] = None,
    ):
        self._on_status = on_status or (lambda msg: logger.info(msg))
        self._on_progress = on_progress or (lambda step, current, total: logger.info(f"[{current}/{total}] {step}"))
        self._on_error = on_error or (lambda msg: logger.error(msg))
        self._on_warning = on_warning or (lambda msg: logger.warning(msg))
    
    def status(self, message: str):
        """Report a status update"""
        self._on_status(message)
    
    def progress(self, step: str, current: int, total: int):
        """Report progress on a multi-step operation"""
        self._on_progress(step, current, total)
    
    def error(self, message: str):
        """Report an error"""
        self._on_error(message)
    
    def warning(self, message: str):
        """Report a warning"""
        self._on_warning(message)


class ResourceLoader:
    """
    Loads resources from the resources directory.
    
    Provides access to:
    - Context files (P language guides)
    - Instruction templates
    - Few-shot examples
    """
    
    def __init__(self, resources_path: Optional[Path] = None):
        if resources_path is None:
            # Default to resources/ relative to project root
            project_root = Path(__file__).parent.parent.parent.parent
            resources_path = project_root / "resources"
        
        self.resources_path = resources_path
        self._cache: Dict[str, str] = {}
    
    def load(self, relative_path: str, use_cache: bool = True) -> str:
        """
        Load a resource file.
        
        Args:
            relative_path: Path relative to resources directory
            use_cache: Whether to use cached content
            
        Returns:
            File content as string
        """
        if use_cache and relative_path in self._cache:
            return self._cache[relative_path]
        
        full_path = self.resources_path / relative_path
        
        if not full_path.exists():
            raise FileNotFoundError(f"Resource not found: {relative_path}")
        
        content = full_path.read_text(encoding="utf-8")
        
        if use_cache:
            self._cache[relative_path] = content
        
        return content
    
    def load_context(self, filename: str) -> str:
        """Load a context file from context_files/"""
        return self.load(f"context_files/{filename}")
    
    def load_modular_context(self, filename: str) -> str:
        """Load a modular context file from context_files/modular/"""
        return self.load(f"context_files/modular/{filename}")
    
    def load_instruction(self, filename: str) -> str:
        """Load an instruction template from instructions/"""
        return self.load(f"instructions/{filename}")
    
    def clear_cache(self):
        """Clear the resource cache"""
        self._cache.clear()


class BaseService:
    """
    Base class for all services.
    
    Provides:
    - LLM provider access
    - Resource loading
    - Event callbacks
    """
    
    def __init__(
        self,
        llm_provider: Optional[LLMProvider] = None,
        resource_loader: Optional[ResourceLoader] = None,
        callbacks: Optional[EventCallback] = None,
    ):
        self._llm_provider = llm_provider
        self._resource_loader = resource_loader or ResourceLoader()
        self._callbacks = callbacks or EventCallback()
    
    @property
    def llm(self) -> LLMProvider:
        """Get the LLM provider (lazy initialization)"""
        if self._llm_provider is None:
            self._llm_provider = get_default_provider()
        return self._llm_provider
    
    @property
    def resources(self) -> ResourceLoader:
        """Get the resource loader"""
        return self._resource_loader
    
    @property
    def callbacks(self) -> EventCallback:
        """Get the event callbacks"""
        return self._callbacks
    
    def _status(self, message: str):
        """Emit a status message"""
        self._callbacks.status(message)
    
    def _progress(self, step: str, current: int, total: int):
        """Emit a progress update"""
        self._callbacks.progress(step, current, total)
    
    def _error(self, message: str):
        """Emit an error message"""
        self._callbacks.error(message)
    
    def _warning(self, message: str):
        """Emit a warning message"""
        self._callbacks.warning(message)


