"""
Workflow Step definitions for PeasyAI.

This module defines the base abstractions for workflow steps:
- StepStatus: Enumeration of possible step states
- StepResult: Data class for step execution results
- WorkflowStep: Abstract base class for all workflow steps
"""

from abc import ABC, abstractmethod
from dataclasses import dataclass, field
from typing import Any, Dict, Optional, List
from enum import Enum


class StepStatus(Enum):
    """Status of a workflow step."""
    PENDING = "pending"
    RUNNING = "running"
    COMPLETED = "completed"
    FAILED = "failed"
    WAITING_FOR_HUMAN = "waiting_for_human"
    SKIPPED = "skipped"


@dataclass
class StepResult:
    """Result of executing a workflow step."""
    status: StepStatus
    output: Optional[Dict[str, Any]] = None
    error: Optional[str] = None
    needs_human: bool = False
    human_prompt: Optional[str] = None
    artifacts: Dict[str, Any] = field(default_factory=dict)
    
    @classmethod
    def success(cls, output: Optional[Dict[str, Any]] = None, artifacts: Optional[Dict[str, Any]] = None) -> "StepResult":
        """Create a successful result."""
        return cls(
            status=StepStatus.COMPLETED,
            output=output or {},
            artifacts=artifacts or {}
        )
    
    @classmethod
    def failure(cls, error: str) -> "StepResult":
        """Create a failed result."""
        return cls(
            status=StepStatus.FAILED,
            error=error
        )
    
    @classmethod
    def needs_guidance(cls, prompt: str, context: Optional[Dict[str, Any]] = None) -> "StepResult":
        """Create a result that requires human guidance."""
        return cls(
            status=StepStatus.WAITING_FOR_HUMAN,
            needs_human=True,
            human_prompt=prompt,
            output=context or {}
        )
    
    @classmethod
    def skipped(cls, reason: Optional[str] = None) -> "StepResult":
        """Create a skipped result."""
        return cls(
            status=StepStatus.SKIPPED,
            output={"skip_reason": reason} if reason else {}
        )


class WorkflowStep(ABC):
    """
    Abstract base class for workflow steps.
    
    Each step represents a discrete unit of work in a workflow.
    Steps can be retried on failure and can request human intervention.
    
    Attributes:
        name: Unique identifier for the step
        description: Human-readable description
        max_retries: Maximum number of retry attempts (default: 3)
        dependencies: List of step names that must complete first
    """
    
    name: str
    description: str
    max_retries: int = 3
    dependencies: List[str] = []
    
    @abstractmethod
    def execute(self, context: Dict[str, Any]) -> StepResult:
        """
        Execute the step.
        
        Args:
            context: Dictionary containing workflow state and inputs.
                    Common keys include:
                    - design_doc: The design document content
                    - project_path: Path to the P project
                    - user_guidance: Optional guidance from user (for retries)
        
        Returns:
            StepResult indicating success, failure, or need for human input
        """
        pass
    
    @abstractmethod
    def can_skip(self, context: Dict[str, Any]) -> bool:
        """
        Check if this step can be skipped.
        
        Useful for incremental workflows where some artifacts already exist.
        
        Args:
            context: Dictionary containing workflow state
            
        Returns:
            True if step can be skipped, False otherwise
        """
        pass
    
    def validate_context(self, context: Dict[str, Any]) -> Optional[str]:
        """
        Validate that required context keys are present.
        
        Override this to add custom validation.
        
        Args:
            context: Dictionary to validate
            
        Returns:
            Error message if validation fails, None if valid
        """
        return None
    
    def rollback(self, context: Dict[str, Any]) -> None:
        """
        Rollback any changes made by this step.
        
        Override this to implement cleanup on failure.
        
        Args:
            context: Dictionary containing workflow state
        """
        pass


class CompositeStep(WorkflowStep):
    """
    A step that executes multiple sub-steps.
    
    Useful for grouping related steps or implementing parallel execution.
    """
    
    def __init__(self, name: str, description: str, steps: List[WorkflowStep], parallel: bool = False):
        self.name = name
        self.description = description
        self.steps = steps
        self.parallel = parallel
        self.max_retries = 1  # Don't retry composite steps, retry individual sub-steps
    
    def execute(self, context: Dict[str, Any]) -> StepResult:
        """Execute all sub-steps."""
        results = []
        combined_output = {}
        combined_artifacts = {}
        
        for step in self.steps:
            if step.can_skip(context):
                results.append(StepResult.skipped(f"Step {step.name} skipped"))
                continue
                
            result = step.execute(context)
            results.append(result)
            
            if result.status == StepStatus.FAILED:
                return StepResult.failure(
                    f"Sub-step '{step.name}' failed: {result.error}"
                )
            
            if result.status == StepStatus.WAITING_FOR_HUMAN:
                return result
            
            if result.output:
                combined_output.update(result.output)
                context.update(result.output)
            
            if result.artifacts:
                combined_artifacts.update(result.artifacts)
        
        return StepResult.success(combined_output, combined_artifacts)
    
    def can_skip(self, context: Dict[str, Any]) -> bool:
        """Skip if all sub-steps can be skipped."""
        return all(step.can_skip(context) for step in self.steps)
