"""Workflow tools for MCP."""

from typing import Dict, Any, Optional, List
from pydantic import BaseModel, Field
import logging
import os

logger = logging.getLogger(__name__)

from core.workflow import (
    WorkflowEngine,
    WorkflowFactory,
    EventEmitter,
    WorkflowEvent,
    LoggingEventListener,
    extract_machine_names_from_design_doc,
)

_workflow_engine: Optional[WorkflowEngine] = None
_workflow_factory: Optional[WorkflowFactory] = None


class RunWorkflowParams(BaseModel):
    """Parameters for running a workflow"""
    workflow_name: str = Field(description="Name of the workflow to run: 'full_generation', 'compile_and_fix', 'full_verification', 'quick_check'")
    project_path: str = Field(description="Absolute path to the P project directory")
    design_doc: Optional[str] = Field(default=None, description="Design document (required for generation workflows)")
    machine_names: Optional[List[str]] = Field(default=None, description="List of machine names (auto-extracted from design_doc if not provided)")
    schedules: int = Field(default=100, description="Number of schedules for PChecker")
    timeout: int = Field(default=60, description="Timeout in seconds for PChecker")
    ensemble_size: int = Field(default=3, description="Number of candidates per file for ensemble selection")


class ResumeWorkflowParams(BaseModel):
    """Parameters for resuming a paused workflow"""
    workflow_id: str = Field(description="ID of the paused workflow")
    user_guidance: str = Field(description="User guidance to continue the workflow")


class ListWorkflowsParams(BaseModel):
    """Parameters for listing workflows"""
    pass


def _get_workflow_engine(get_services) -> tuple[WorkflowEngine, WorkflowFactory]:
    global _workflow_engine, _workflow_factory

    if _workflow_engine is None:
        services = get_services()

        emitter = EventEmitter()
        emitter.on_all(LoggingEventListener(verbose=True))

        state_store_path = os.environ.get("PCHATBOT_WORKFLOW_STATE_FILE", ".peasyai_workflows.json")
        _workflow_engine = WorkflowEngine(emitter, state_store_path=state_store_path)
        _workflow_factory = WorkflowFactory(
            generation_service=services["generation"],
            compilation_service=services["compilation"],
            fixer_service=services["fixer"]
        )

        _workflow_engine.register_workflow(
            _workflow_factory.create_compile_and_fix_workflow()
        )
        _workflow_engine.register_workflow(
            _workflow_factory.create_full_verification_workflow()
        )
        _workflow_engine.register_workflow(
            _workflow_factory.create_quick_check_workflow()
        )

        logger.info("Workflow engine initialized")

    return _workflow_engine, _workflow_factory


def register_workflow_tools(mcp, get_services, with_metadata):
    """Register workflow tools."""

    @mcp.tool(
        name="run_workflow",
        description="""Execute a predefined multi-step workflow. Available workflows:
- compile_and_fix: Compile the project and automatically fix errors (requires project_path).
- full_verification: Compile, fix compilation errors, run PChecker, and automatically fix any PChecker bugs found (requires project_path). IMPORTANT: Always use this workflow after generating P code to verify and fix correctness end-to-end. Do NOT manually edit files to fix PChecker errors — this workflow handles it automatically by reading trace files and applying AI-driven fixes.
- quick_check: Run PChecker only on an already-compiled project (requires project_path).
- full_generation: Generate a complete P project from a design doc (requires design_doc and project_path). NOTE: Prefer the step-by-step generation tools instead for better quality and user control. Only use full_generation when the user explicitly requests hands-off automated generation."""
    )
    def run_workflow(params: RunWorkflowParams) -> Dict[str, Any]:
        engine, factory = _get_workflow_engine(get_services)

        context = {
            "project_path": params.project_path,
        }

        if params.design_doc:
            context["design_doc"] = params.design_doc

        if params.workflow_name == "full_generation":
            if not params.design_doc:
                payload = {
                    "success": False,
                    "error": "design_doc is required for full_generation workflow"
                }
                return with_metadata("run_workflow", payload)

            machine_names = params.machine_names
            if not machine_names:
                machine_names = extract_machine_names_from_design_doc(params.design_doc)
                if not machine_names:
                    payload = {
                        "success": False,
                        "error": "Could not extract machine names from design_doc. Please provide machine_names explicitly."
                    }
                    return with_metadata("run_workflow", payload)

            workflow = factory.create_full_generation_workflow(
                machine_names=machine_names,
                ensemble_size=params.ensemble_size,
            )
            engine.register_workflow(workflow)

        try:
            result = engine.execute(params.workflow_name, context)

            if result.get("needs_guidance"):
                payload = {
                    "success": False,
                    "paused": True,
                    "workflow_id": result.get("_workflow_id"),
                    "guidance_needed": result.get("guidance_context"),
                    "message": "Workflow paused - human guidance needed"
                }
                return with_metadata("run_workflow", payload)

            payload = {
                "success": result.get("success", False),
                "completed_steps": result.get("completed_steps", []),
                "skipped_steps": result.get("skipped_steps", []),
                "errors": result.get("errors", [])
            }
            return with_metadata("run_workflow", payload)

        except Exception as e:
            logger.error(f"Workflow execution failed: {e}")
            payload = {
                "success": False,
                "error": str(e)
            }
            return with_metadata("run_workflow", payload)

    @mcp.tool(
        name="resume_workflow",
        description="Resume a paused workflow with user guidance. Call this after run_workflow returns paused=true and needs_guidance. Provide the workflow_id from the paused response and the user's guidance text."
    )
    def resume_workflow(params: ResumeWorkflowParams) -> Dict[str, Any]:
        engine, _ = _get_workflow_engine(get_services)

        try:
            result = engine.resume(params.workflow_id, params.user_guidance)

            if result.get("needs_guidance"):
                payload = {
                    "success": False,
                    "paused": True,
                    "workflow_id": result.get("_workflow_id"),
                    "guidance_needed": result.get("guidance_context"),
                    "message": "Workflow still needs guidance"
                }
                return with_metadata("resume_workflow", payload)

            payload = {
                "success": result.get("success", False),
                "completed_steps": result.get("completed_steps", []),
                "errors": result.get("errors", [])
            }
            return with_metadata("resume_workflow", payload)

        except ValueError as e:
            payload = {
                "success": False,
                "error": str(e)
            }
            return with_metadata("resume_workflow", payload)
        except Exception as e:
            logger.error(f"Workflow resume failed: {e}")
            payload = {
                "success": False,
                "error": str(e)
            }
            return with_metadata("resume_workflow", payload)

    @mcp.tool(
        name="list_workflows",
        description="List available workflows and any active or paused workflows. Returns the set of workflow names you can pass to run_workflow, plus status of any in-flight workflows."
    )
    def list_workflows(params: ListWorkflowsParams) -> Dict[str, Any]:
        engine, _ = _get_workflow_engine(get_services)

        available = [
            {
                "name": "full_generation",
                "description": "Generate complete P project from design document",
                "requires": ["design_doc", "project_path"]
            },
            {
                "name": "compile_and_fix",
                "description": "Compile project and automatically fix errors",
                "requires": ["project_path"]
            },
            {
                "name": "full_verification",
                "description": "Compile, fix compilation errors, run PChecker, and automatically fix any PChecker bugs (full end-to-end verification)",
                "requires": ["project_path"]
            },
            {
                "name": "quick_check",
                "description": "Run PChecker on compiled project",
                "requires": ["project_path"]
            }
        ]

        active = [
            {
                "workflow_id": state.workflow_id,
                "name": state.workflow_name,
                "status": state.status,
                "current_step": state.current_step_index,
                "completed_steps": state.completed_steps
            }
            for state in engine.get_active_workflows()
        ]

        persistence = engine.get_persistence_status()

        payload = {
            "available_workflows": available,
            "active_workflows": active,
            "persistence": persistence,
        }
        return with_metadata("list_workflows", payload)

    return {
        "run_workflow": run_workflow,
        "resume_workflow": resume_workflow,
        "list_workflows": list_workflows,
    }
