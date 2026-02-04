"""
Workflow Engine for PChatBot.

This module provides the core workflow execution engine that:
- Executes workflows as sequences of steps
- Handles retries with configurable limits
- Supports human-in-the-loop escalation
- Emits events for observability
- Manages workflow state for pause/resume
"""

from dataclasses import dataclass, field
from typing import Any, Callable, Dict, List, Optional
import uuid
from datetime import datetime

from .steps import WorkflowStep, StepResult, StepStatus
from .events import WorkflowEvent, EventEmitter


@dataclass
class WorkflowDefinition:
    """
    Definition of a workflow.
    
    Attributes:
        name: Unique identifier for the workflow
        description: Human-readable description
        steps: Ordered list of steps to execute
        continue_on_failure: If True, continue executing after step failures
        on_step_complete: Optional callback when a step completes
        on_human_needed: Optional callback when human input is needed
    """
    name: str
    description: str = ""
    steps: List[WorkflowStep] = field(default_factory=list)
    continue_on_failure: bool = False
    on_step_complete: Optional[Callable[[str, StepResult], None]] = None
    on_human_needed: Optional[Callable[[str, str, Dict], None]] = None


@dataclass 
class WorkflowState:
    """
    State of a workflow execution.
    
    Used for tracking progress, pause/resume, and debugging.
    """
    workflow_id: str
    workflow_name: str
    status: str  # "running", "completed", "failed", "paused"
    context: Dict[str, Any]
    current_step_index: int = 0
    started_at: Optional[datetime] = None
    completed_at: Optional[datetime] = None
    errors: List[str] = field(default_factory=list)
    completed_steps: List[str] = field(default_factory=list)
    skipped_steps: List[str] = field(default_factory=list)


class WorkflowEngine:
    """
    Executes workflows with observability and human-in-the-loop support.
    
    The engine manages workflow execution by:
    1. Iterating through steps in order
    2. Checking if steps can be skipped
    3. Executing steps with retry logic
    4. Emitting events for UI updates
    5. Handling human escalation when needed
    6. Managing state for pause/resume
    
    Usage:
        from src.core.workflow import WorkflowEngine, EventEmitter, WorkflowDefinition
        
        emitter = EventEmitter()
        engine = WorkflowEngine(emitter)
        
        # Define workflow
        workflow = WorkflowDefinition(
            name="my_workflow",
            steps=[step1, step2, step3]
        )
        engine.register_workflow(workflow)
        
        # Execute
        result = engine.execute("my_workflow", {"input": "value"})
    """
    
    def __init__(self, event_emitter: EventEmitter):
        self.emitter = event_emitter
        self.workflows: Dict[str, WorkflowDefinition] = {}
        self.active_states: Dict[str, WorkflowState] = {}
    
    def register_workflow(self, workflow: WorkflowDefinition) -> None:
        """
        Register a workflow definition.
        
        Args:
            workflow: The workflow definition to register
        """
        self.workflows[workflow.name] = workflow
    
    def execute(
        self, 
        workflow_name: str, 
        initial_context: Dict[str, Any],
        resume_from: Optional[str] = None
    ) -> Dict[str, Any]:
        """
        Execute a workflow.
        
        Args:
            workflow_name: Name of the registered workflow to execute
            initial_context: Initial context/inputs for the workflow
            resume_from: Optional step name to resume from (for paused workflows)
            
        Returns:
            Final context dictionary with results and any errors
        """
        if workflow_name not in self.workflows:
            raise ValueError(f"Unknown workflow: {workflow_name}")
        
        workflow = self.workflows[workflow_name]
        workflow_id = str(uuid.uuid4())
        
        # Initialize state
        state = WorkflowState(
            workflow_id=workflow_id,
            workflow_name=workflow_name,
            status="running",
            context={**initial_context},
            started_at=datetime.now()
        )
        self.active_states[workflow_id] = state
        
        # Set workflow ID for event tracking
        self.emitter.set_workflow_id(workflow_id)
        
        # Emit start event
        self.emitter.emit(WorkflowEvent.STARTED, {
            "workflow": workflow_name,
            "workflow_id": workflow_id,
            "context": {k: v for k, v in state.context.items() if not k.startswith("_")}
        })
        
        # Find starting step index
        start_index = 0
        if resume_from:
            for i, step in enumerate(workflow.steps):
                if step.name == resume_from:
                    start_index = i
                    break
            state.current_step_index = start_index
        
        # Execute steps
        for i in range(start_index, len(workflow.steps)):
            step = workflow.steps[i]
            state.current_step_index = i
            
            # Check if step can be skipped
            if step.can_skip(state.context):
                self.emitter.emit(
                    WorkflowEvent.STEP_SKIPPED, 
                    {"step": step.name},
                    step_name=step.name
                )
                state.skipped_steps.append(step.name)
                continue
            
            # Validate context
            validation_error = step.validate_context(state.context)
            if validation_error:
                self.emitter.emit(
                    WorkflowEvent.STEP_FAILED,
                    {"step": step.name, "error": f"Validation failed: {validation_error}"},
                    step_name=step.name
                )
                if not workflow.continue_on_failure:
                    state.status = "failed"
                    state.errors.append(validation_error)
                    break
                continue
            
            # Execute step with retry logic
            result = self._execute_step_with_retry(step, state, workflow)
            
            # Handle result
            if result.status == StepStatus.COMPLETED:
                state.context.update(result.output or {})
                state.completed_steps.append(step.name)
                
                self.emitter.emit(
                    WorkflowEvent.STEP_COMPLETED,
                    {"step": step.name, "output": result.output},
                    step_name=step.name
                )
                
                if workflow.on_step_complete:
                    workflow.on_step_complete(step.name, result)
                
            elif result.status == StepStatus.WAITING_FOR_HUMAN:
                state.status = "paused"
                state.context["_paused_at"] = step.name
                state.context["needs_guidance"] = True
                state.context["guidance_context"] = result.human_prompt
                
                self.emitter.emit(
                    WorkflowEvent.HUMAN_NEEDED,
                    {
                        "step": step.name,
                        "prompt": result.human_prompt,
                        "workflow_id": workflow_id
                    },
                    step_name=step.name
                )
                
                if workflow.on_human_needed:
                    workflow.on_human_needed(step.name, result.human_prompt, state.context)
                
                # Return paused state - caller can resume later
                state.context["_workflow_id"] = workflow_id
                return state.context
                
            elif result.status == StepStatus.FAILED:
                state.errors.append(result.error or "Unknown error")
                
                self.emitter.emit(
                    WorkflowEvent.STEP_FAILED,
                    {"step": step.name, "error": result.error},
                    step_name=step.name
                )
                
                if not workflow.continue_on_failure:
                    state.status = "failed"
                    break
        
        # Complete workflow
        state.completed_at = datetime.now()
        state.status = "completed" if not state.errors else "failed"
        state.context["success"] = len(state.errors) == 0
        state.context["errors"] = state.errors
        state.context["completed_steps"] = state.completed_steps
        state.context["skipped_steps"] = state.skipped_steps
        
        self.emitter.emit(
            WorkflowEvent.COMPLETED if state.status == "completed" else WorkflowEvent.FAILED,
            {"context": state.context}
        )
        
        # Clean up
        del self.active_states[workflow_id]
        
        return state.context
    
    def resume(
        self, 
        workflow_id: str, 
        user_guidance: str
    ) -> Dict[str, Any]:
        """
        Resume a paused workflow with user guidance.
        
        Args:
            workflow_id: ID of the paused workflow
            user_guidance: User's response/guidance
            
        Returns:
            Final context dictionary
        """
        if workflow_id not in self.active_states:
            raise ValueError(f"No paused workflow with ID: {workflow_id}")
        
        state = self.active_states[workflow_id]
        
        if state.status != "paused":
            raise ValueError(f"Workflow {workflow_id} is not paused (status: {state.status})")
        
        # Add guidance to context
        state.context["user_guidance"] = user_guidance
        state.context.pop("needs_guidance", None)
        state.context.pop("guidance_context", None)
        
        # Emit resume event
        self.emitter.emit(
            WorkflowEvent.RESUMED,
            {"workflow_id": workflow_id, "guidance": user_guidance}
        )
        
        # Resume from paused step
        paused_step = state.context.pop("_paused_at", None)
        
        return self.execute(
            state.workflow_name,
            state.context,
            resume_from=paused_step
        )
    
    def _execute_step_with_retry(
        self,
        step: WorkflowStep,
        state: WorkflowState,
        workflow: WorkflowDefinition
    ) -> StepResult:
        """
        Execute a step with retry logic.
        
        Retries up to step.max_retries times on failure.
        After max retries, escalates to human-in-the-loop.
        
        Args:
            step: The step to execute
            state: Current workflow state
            workflow: The workflow definition
            
        Returns:
            StepResult from the step execution
        """
        last_result = None
        
        for attempt in range(step.max_retries):
            self.emitter.emit(
                WorkflowEvent.STEP_STARTED,
                {"step": step.name, "attempt": attempt + 1},
                step_name=step.name
            )
            
            try:
                result = step.execute(state.context)
            except Exception as e:
                result = StepResult.failure(str(e))
            
            last_result = result
            
            if result.status == StepStatus.COMPLETED:
                return result
            
            if result.status == StepStatus.WAITING_FOR_HUMAN:
                return result
            
            if result.status == StepStatus.SKIPPED:
                return result
            
            # Log retry
            if attempt < step.max_retries - 1:
                self.emitter.emit(
                    WorkflowEvent.STEP_RETRY,
                    {
                        "step": step.name,
                        "attempt": attempt + 1,
                        "error": result.error,
                        "remaining_attempts": step.max_retries - attempt - 1
                    },
                    step_name=step.name
                )
        
        # Max retries exceeded - escalate to human
        return StepResult.needs_guidance(
            f"Step '{step.name}' failed after {step.max_retries} attempts.\n"
            f"Last error: {last_result.error if last_result else 'Unknown'}\n"
            f"Please provide guidance on how to proceed.",
            {"last_error": last_result.error if last_result else None}
        )
    
    def get_active_workflows(self) -> List[WorkflowState]:
        """Get list of currently active/paused workflows."""
        return list(self.active_states.values())
    
    def cancel_workflow(self, workflow_id: str) -> bool:
        """
        Cancel an active workflow.
        
        Args:
            workflow_id: ID of the workflow to cancel
            
        Returns:
            True if cancelled, False if not found
        """
        if workflow_id in self.active_states:
            state = self.active_states[workflow_id]
            state.status = "cancelled"
            
            self.emitter.emit(
                WorkflowEvent.FAILED,
                {"workflow_id": workflow_id, "reason": "cancelled"}
            )
            
            del self.active_states[workflow_id]
            return True
        return False
