"""
Workflow Engine for PChatBot.

This module provides a flexible workflow execution system that supports:
- Sequential and parallel step execution
- Retry logic with configurable attempts
- Human-in-the-loop escalation
- Event-driven observability for multiple UIs

Usage:
    from src.core.workflow import WorkflowEngine, EventEmitter, WorkflowFactory
    from src.core.workflow.p_steps import GenerateTypesEventsStep
    
    # Create services
    generation_service = GenerationService(...)
    compilation_service = CompilationService(...)
    fixer_service = FixerService(...)
    
    # Create factory and engine
    factory = WorkflowFactory(generation_service, compilation_service, fixer_service)
    emitter = EventEmitter()
    engine = WorkflowEngine(emitter)
    
    # Register event listeners
    emitter.on(WorkflowEvent.STEP_COMPLETED, lambda data: print(f"Completed: {data}"))
    
    # Create and execute workflow
    workflow = factory.create_full_generation_workflow(machine_names=["Client", "Server"])
    engine.register_workflow(workflow)
    result = engine.execute("full_generation", {"design_doc": doc, "project_path": path})
"""

from .steps import (
    WorkflowStep,
    StepStatus,
    StepResult,
    CompositeStep,
)
from .events import (
    WorkflowEvent,
    EventEmitter,
    EventData,
    LoggingEventListener,
)
from .engine import (
    WorkflowEngine,
    WorkflowDefinition,
    WorkflowState,
)
try:
    from .factory import (
        WorkflowFactory,
        extract_machine_names_from_design_doc,
        create_workflow_engine_from_config,
    )
    HAS_FACTORY = True
except ImportError:
    # Optional dependency path (e.g., yaml) may be unavailable in lightweight environments.
    HAS_FACTORY = False

__all__ = [
    # Steps
    "WorkflowStep",
    "StepStatus", 
    "StepResult",
    "CompositeStep",
    # Events
    "WorkflowEvent",
    "EventEmitter",
    "EventData",
    "LoggingEventListener",
    # Engine
    "WorkflowEngine",
    "WorkflowDefinition",
    "WorkflowState",
]

if HAS_FACTORY:
    __all__.extend([
        "WorkflowFactory",
        "extract_machine_names_from_design_doc",
        "create_workflow_engine_from_config",
    ])
