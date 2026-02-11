"""
Event system for workflow observability.

This module provides an event-driven architecture for workflow monitoring:
- WorkflowEvent: Enumeration of workflow events
- EventEmitter: Pub/sub system for event listeners

This allows multiple UIs (Streamlit, CLI, MCP) to observe workflow progress
without tight coupling to the workflow engine.
"""

from enum import Enum
from typing import Any, Callable, Dict, List, Optional
from dataclasses import dataclass, field
from datetime import datetime
import threading


class WorkflowEvent(Enum):
    """Events emitted during workflow execution."""
    
    # Workflow lifecycle
    STARTED = "workflow.started"
    COMPLETED = "workflow.completed"
    FAILED = "workflow.failed"
    PAUSED = "workflow.paused"
    RESUMED = "workflow.resumed"
    
    # Step lifecycle
    STEP_STARTED = "step.started"
    STEP_COMPLETED = "step.completed"
    STEP_FAILED = "step.failed"
    STEP_SKIPPED = "step.skipped"
    STEP_RETRY = "step.retry"
    
    # Human interaction
    HUMAN_NEEDED = "human.needed"
    HUMAN_RESPONSE = "human.response"
    
    # Artifacts
    FILE_GENERATED = "file.generated"
    FILE_SAVED = "file.saved"
    
    # Compilation
    COMPILATION_STARTED = "compilation.started"
    COMPILATION_COMPLETED = "compilation.completed"
    COMPILATION_ERROR = "compilation.error"
    
    # Checker
    CHECKER_STARTED = "checker.started"
    CHECKER_COMPLETED = "checker.completed"
    CHECKER_ERROR = "checker.error"
    
    # Progress
    PROGRESS = "progress"
    LOG = "log"


@dataclass
class EventData:
    """Data structure for event payloads."""
    event: WorkflowEvent
    timestamp: datetime
    data: Dict[str, Any]
    workflow_id: Optional[str] = None
    step_name: Optional[str] = None


EventCallback = Callable[[EventData], None]


class EventEmitter:
    """
    Thread-safe event emitter for workflow observability.
    
    Supports:
    - Multiple listeners per event
    - Wildcard listeners (listen to all events)
    - Event history for debugging
    - Async emission (non-blocking)
    
    Usage:
        emitter = EventEmitter()
        
        # Listen to specific event
        emitter.on(WorkflowEvent.STEP_COMPLETED, lambda e: print(e.data))
        
        # Listen to all events
        emitter.on_all(lambda e: log(e))
        
        # Emit event
        emitter.emit(WorkflowEvent.STEP_COMPLETED, {"step": "generate_types"})
    """
    
    def __init__(self, keep_history: bool = True, max_history: int = 1000):
        self._listeners: Dict[WorkflowEvent, List[EventCallback]] = {}
        self._all_listeners: List[EventCallback] = []
        self._history: List[EventData] = []
        self._keep_history = keep_history
        self._max_history = max_history
        self._lock = threading.Lock()
        self._workflow_id: Optional[str] = None
    
    def set_workflow_id(self, workflow_id: str) -> None:
        """Set the current workflow ID for event tracking."""
        self._workflow_id = workflow_id
    
    def on(self, event: WorkflowEvent, callback: EventCallback) -> None:
        """
        Register a listener for a specific event.
        
        Args:
            event: The event type to listen for
            callback: Function to call when event is emitted
        """
        with self._lock:
            if event not in self._listeners:
                self._listeners[event] = []
            self._listeners[event].append(callback)
    
    def on_all(self, callback: EventCallback) -> None:
        """
        Register a listener for all events.
        
        Args:
            callback: Function to call for every event
        """
        with self._lock:
            self._all_listeners.append(callback)
    
    def off(self, event: WorkflowEvent, callback: EventCallback) -> None:
        """
        Remove a listener for a specific event.
        
        Args:
            event: The event type
            callback: The callback to remove
        """
        with self._lock:
            if event in self._listeners and callback in self._listeners[event]:
                self._listeners[event].remove(callback)
    
    def off_all(self, callback: EventCallback) -> None:
        """
        Remove a wildcard listener.
        
        Args:
            callback: The callback to remove
        """
        with self._lock:
            if callback in self._all_listeners:
                self._all_listeners.remove(callback)
    
    def emit(
        self, 
        event: WorkflowEvent, 
        data: Dict[str, Any],
        step_name: Optional[str] = None
    ) -> EventData:
        """
        Emit an event to all registered listeners.
        
        Args:
            event: The event type
            data: Event payload data
            step_name: Optional step name for context
            
        Returns:
            The EventData object that was emitted
        """
        event_data = EventData(
            event=event,
            timestamp=datetime.now(),
            data=data,
            workflow_id=self._workflow_id,
            step_name=step_name
        )
        
        with self._lock:
            # Store in history
            if self._keep_history:
                self._history.append(event_data)
                if len(self._history) > self._max_history:
                    self._history = self._history[-self._max_history:]
            
            # Get listeners (copy to avoid modification during iteration)
            specific_listeners = list(self._listeners.get(event, []))
            all_listeners = list(self._all_listeners)
        
        # Call listeners outside lock to prevent deadlocks
        for callback in specific_listeners:
            try:
                callback(event_data)
            except Exception as e:
                # Log but don't propagate listener errors
                print(f"Error in event listener for {event}: {e}")
        
        for callback in all_listeners:
            try:
                callback(event_data)
            except Exception as e:
                print(f"Error in wildcard listener for {event}: {e}")
        
        return event_data
    
    def get_history(
        self, 
        event_filter: Optional[WorkflowEvent] = None,
        limit: Optional[int] = None
    ) -> List[EventData]:
        """
        Get event history, optionally filtered.
        
        Args:
            event_filter: Optional event type to filter by
            limit: Optional maximum number of events to return
            
        Returns:
            List of EventData objects
        """
        with self._lock:
            history = self._history.copy()
        
        if event_filter:
            history = [e for e in history if e.event == event_filter]
        
        if limit:
            history = history[-limit:]
        
        return history
    
    def clear_history(self) -> None:
        """Clear the event history."""
        with self._lock:
            self._history.clear()
    
    def clear_listeners(self) -> None:
        """Remove all listeners."""
        with self._lock:
            self._listeners.clear()
            self._all_listeners.clear()


class LoggingEventListener:
    """
    A pre-built event listener that logs all events.
    
    Useful for debugging and CLI output.
    """
    
    def __init__(self, verbose: bool = False):
        self.verbose = verbose
    
    def __call__(self, event_data: EventData) -> None:
        """Handle an event by logging it."""
        event = event_data.event
        data = event_data.data
        
        if event == WorkflowEvent.STARTED:
            print(f"\n🚀 Workflow started: {data.get('workflow', 'unknown')}")
        
        elif event == WorkflowEvent.COMPLETED:
            success = data.get('context', {}).get('success', False)
            icon = "✅" if success else "⚠️"
            print(f"\n{icon} Workflow completed")
        
        elif event == WorkflowEvent.FAILED:
            print(f"\n❌ Workflow failed: {data.get('error', 'unknown error')}")
        
        elif event == WorkflowEvent.STEP_STARTED:
            step = data.get('step', 'unknown')
            attempt = data.get('attempt', 1)
            if attempt > 1:
                print(f"  🔄 Retrying {step} (attempt {attempt})")
            elif self.verbose:
                print(f"  ▶️ Starting: {step}")
        
        elif event == WorkflowEvent.STEP_COMPLETED:
            step = data.get('step', 'unknown')
            print(f"  ✓ Completed: {step}")
        
        elif event == WorkflowEvent.STEP_FAILED:
            step = data.get('step', 'unknown')
            error = data.get('error', 'unknown error')
            print(f"  ✗ Failed: {step} - {error}")
        
        elif event == WorkflowEvent.STEP_SKIPPED:
            if self.verbose:
                step = data.get('step', 'unknown')
                print(f"  ⏭️ Skipped: {step}")
        
        elif event == WorkflowEvent.HUMAN_NEEDED:
            step = data.get('step', 'unknown')
            prompt = data.get('prompt', 'Guidance needed')
            print(f"\n⚠️ Human input needed for '{step}':")
            print(f"   {prompt}")
        
        elif event == WorkflowEvent.FILE_GENERATED:
            if self.verbose:
                path = data.get('path', 'unknown')
                print(f"  📄 Generated: {path}")
        
        elif event == WorkflowEvent.PROGRESS:
            if self.verbose:
                message = data.get('message', '')
                print(f"  ... {message}")
