"""
Streamlit Adapters for PeasyAI.

This module provides adapters that connect the workflow engine and services
to Streamlit's UI components. It handles:
- Converting workflow events to Streamlit status updates
- Managing Streamlit session state
- Providing callbacks for workflow progress
"""

import streamlit as st
from typing import Any, Callable, Dict, List, Optional
from pathlib import Path
import os
import sys

# Add project paths
PROJECT_ROOT = Path(__file__).parent.parent.parent.parent
SRC_ROOT = Path(__file__).parent.parent.parent

if str(SRC_ROOT) not in sys.path:
    sys.path.insert(0, str(SRC_ROOT))

from core.workflow import (
    WorkflowEngine,
    WorkflowFactory,
    EventEmitter,
    WorkflowEvent,
    EventData,
    extract_machine_names_from_design_doc,
)
from core.services import (
    GenerationService,
    CompilationService,
    FixerService,
)
from core.services.base import ResourceLoader
from core.llm import get_default_provider

# Load environment
from dotenv import load_dotenv
load_dotenv(PROJECT_ROOT / ".env", override=True)


class StreamlitEventListener:
    """
    Event listener that updates Streamlit UI elements based on workflow events.
    
    Converts workflow events into Streamlit status updates, progress bars,
    and informational messages.
    """
    
    def __init__(self, status_container=None):
        """
        Initialize the listener.
        
        Args:
            status_container: Optional Streamlit container for status updates
        """
        self.status_container = status_container
        self.current_status = None
        self.metrics = {
            "steps_completed": 0,
            "steps_total": 0,
            "errors": [],
        }
    
    def __call__(self, event_data: EventData) -> None:
        """Handle a workflow event."""
        event = event_data.event
        data = event_data.data
        
        if self.status_container is None:
            return
        
        if event == WorkflowEvent.STARTED:
            workflow = data.get("workflow", "Workflow")
            self.current_status = self.status_container.status(
                f"Running {workflow}...", 
                expanded=True
            )
            self.current_status.write(f"🚀 Started: {workflow}")
        
        elif event == WorkflowEvent.STEP_STARTED:
            step = data.get("step", "step")
            attempt = data.get("attempt", 1)
            if self.current_status:
                if attempt > 1:
                    self.current_status.write(f"🔄 Retrying: {step} (attempt {attempt})")
                else:
                    self.current_status.write(f"▶️ Running: {step}")
        
        elif event == WorkflowEvent.STEP_COMPLETED:
            step = data.get("step", "step")
            self.metrics["steps_completed"] += 1
            if self.current_status:
                self.current_status.write(f"✅ Completed: {step}")
        
        elif event == WorkflowEvent.STEP_FAILED:
            step = data.get("step", "step")
            error = data.get("error", "Unknown error")
            self.metrics["errors"].append(error)
            if self.current_status:
                self.current_status.write(f"❌ Failed: {step} - {error}")
        
        elif event == WorkflowEvent.STEP_SKIPPED:
            step = data.get("step", "step")
            if self.current_status:
                self.current_status.write(f"⏭️ Skipped: {step}")
        
        elif event == WorkflowEvent.HUMAN_NEEDED:
            step = data.get("step", "step")
            prompt = data.get("prompt", "Guidance needed")
            if self.current_status:
                self.current_status.write(f"⚠️ Human input needed for: {step}")
                self.current_status.write(f"   {prompt}")
        
        elif event == WorkflowEvent.COMPLETED:
            success = data.get("context", {}).get("success", False)
            if self.current_status:
                if success:
                    self.current_status.update(
                        label="✅ Workflow completed successfully!",
                        state="complete",
                        expanded=False
                    )
                else:
                    errors = data.get("context", {}).get("errors", [])
                    self.current_status.update(
                        label=f"⚠️ Workflow completed with {len(errors)} error(s)",
                        state="complete",
                        expanded=True
                    )
        
        elif event == WorkflowEvent.FAILED:
            error = data.get("error", "Unknown error")
            if self.current_status:
                self.current_status.update(
                    label=f"❌ Workflow failed: {error}",
                    state="error",
                    expanded=True
                )
        
        elif event == WorkflowEvent.FILE_GENERATED:
            path = data.get("path", "file")
            if self.current_status:
                self.current_status.write(f"📄 Generated: {os.path.basename(path)}")


class StreamlitWorkflowAdapter:
    """
    Adapter that connects the workflow engine to Streamlit.
    
    Provides a clean interface for running workflows from Streamlit
    components with automatic UI updates.
    """
    
    _instance = None
    
    @classmethod
    def get_instance(cls) -> "StreamlitWorkflowAdapter":
        """Get or create the singleton adapter instance."""
        if cls._instance is None:
            cls._instance = cls()
        return cls._instance
    
    def __init__(self):
        """Initialize the adapter with services and engine."""
        self._initialized = False
        self._engine: Optional[WorkflowEngine] = None
        self._factory: Optional[WorkflowFactory] = None
        self._emitter: Optional[EventEmitter] = None
        self._services: Dict[str, Any] = {}
    
    def _ensure_initialized(self) -> None:
        """Lazy initialization of services."""
        if self._initialized:
            return
        
        # Get LLM provider
        provider = get_default_provider()
        
        # Create resource loader
        resource_loader = ResourceLoader(PROJECT_ROOT / "resources")
        
        # Create services
        self._services["generation"] = GenerationService(
            llm_provider=provider,
            resource_loader=resource_loader
        )
        self._services["compilation"] = CompilationService(
            llm_provider=provider,
            resource_loader=resource_loader
        )
        self._services["fixer"] = FixerService(
            llm_provider=provider,
            resource_loader=resource_loader,
            compilation_service=self._services["compilation"]
        )
        
        # Create event emitter
        self._emitter = EventEmitter()
        
        # Create engine and factory
        self._engine = WorkflowEngine(self._emitter)
        self._factory = WorkflowFactory(
            generation_service=self._services["generation"],
            compilation_service=self._services["compilation"],
            fixer_service=self._services["fixer"]
        )
        
        # Register standard workflows
        self._engine.register_workflow(
            self._factory.create_compile_and_fix_workflow()
        )
        self._engine.register_workflow(
            self._factory.create_full_verification_workflow()
        )
        self._engine.register_workflow(
            self._factory.create_quick_check_workflow()
        )
        
        self._initialized = True
    
    def generate_project(
        self,
        design_doc: str,
        project_path: str,
        status_container=None,
        machine_names: Optional[List[str]] = None
    ) -> Dict[str, Any]:
        """
        Generate a P project from a design document.
        
        Args:
            design_doc: The design document content
            project_path: Where to create the project
            status_container: Optional Streamlit status container
            machine_names: Optional list of machine names (auto-extracted if not provided)
            
        Returns:
            Dictionary with results including generated files and any errors
        """
        self._ensure_initialized()
        
        # Extract machine names if not provided
        if not machine_names:
            machine_names = extract_machine_names_from_design_doc(design_doc)
            if not machine_names:
                return {
                    "success": False,
                    "error": "Could not extract machine names from design document"
                }
        
        # Create workflow
        workflow = self._factory.create_full_generation_workflow(machine_names)
        self._engine.register_workflow(workflow)
        
        # Add Streamlit listener
        listener = StreamlitEventListener(status_container)
        self._emitter.on_all(listener)
        
        try:
            # Execute workflow
            result = self._engine.execute("full_generation", {
                "design_doc": design_doc,
                "project_path": project_path
            })
            
            # Collect generated files
            generated_files = {}
            for key, value in result.items():
                if key.endswith("_code") and value:
                    # Extract filename from key
                    if key == "types_events_code":
                        filename = "Enums_Types_Events.p"
                    elif key.startswith("machine_code_"):
                        filename = key.replace("machine_code_", "") + ".p"
                    elif key.startswith("spec_code_"):
                        filename = key.replace("spec_code_", "") + ".p"
                    elif key.startswith("test_code_"):
                        filename = key.replace("test_code_", "") + ".p"
                    else:
                        continue
                    generated_files[filename] = value
            
            return {
                "success": result.get("success", False),
                "files": generated_files,
                "project_path": result.get("project_path", project_path),  # Use updated path from workflow
                "completed_steps": result.get("completed_steps", []),
                "errors": result.get("errors", []),
                "metrics": listener.metrics
            }
            
        finally:
            # Remove listener
            self._emitter.off_all(listener)
    
    def compile_project(
        self,
        project_path: str,
        status_container=None
    ) -> Dict[str, Any]:
        """
        Compile a P project and fix errors.
        
        Args:
            project_path: Path to the P project
            status_container: Optional Streamlit status container
            
        Returns:
            Compilation results
        """
        self._ensure_initialized()
        
        listener = StreamlitEventListener(status_container)
        self._emitter.on_all(listener)
        
        try:
            result = self._engine.execute("compile_and_fix", {
                "project_path": project_path
            })
            
            return {
                "success": result.get("success", False),
                "errors": result.get("errors", []),
                "completed_steps": result.get("completed_steps", [])
            }
        finally:
            self._emitter.off_all(listener)
    
    def run_checker(
        self,
        project_path: str,
        schedules: int = 100,
        timeout: int = 60,
        status_container=None
    ) -> Dict[str, Any]:
        """
        Run PChecker on a project.
        
        Args:
            project_path: Path to the P project
            schedules: Number of schedules
            timeout: Timeout in seconds
            status_container: Optional Streamlit status container
            
        Returns:
            Checker results
        """
        self._ensure_initialized()
        
        # Use compilation service directly for checker
        result = self._services["compilation"].run_checker(
            project_path=project_path,
            schedules=schedules,
            timeout=timeout
        )
        
        return {
            "success": result.success,
            "output": result.output,
            "errors": result.errors if hasattr(result, "errors") else []
        }
    
    def fix_checker_error(
        self,
        project_path: str,
        trace_log: str,
        user_guidance: Optional[str] = None
    ) -> Dict[str, Any]:
        """
        Fix a PChecker error.
        
        Args:
            project_path: Path to the P project
            trace_log: The error trace log
            user_guidance: Optional user guidance
            
        Returns:
            Fix results
        """
        self._ensure_initialized()
        
        result = self._services["fixer"].fix_checker_error(
            project_path=project_path,
            trace_log=trace_log,
            user_guidance=user_guidance
        )
        
        return {
            "success": result.success,
            "needs_guidance": result.needs_guidance,
            "guidance_questions": result.guidance_questions,
            "fix_description": result.fix_description,
            "error": result.error
        }


def get_adapter() -> StreamlitWorkflowAdapter:
    """Get the Streamlit workflow adapter singleton."""
    return StreamlitWorkflowAdapter.get_instance()
