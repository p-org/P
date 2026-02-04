"""
PChecker Error Parser and Analyzer

Parses PChecker trace logs to identify error types and extract actionable information.
"""

import re
import logging
from typing import Dict, List, Optional, Any, Tuple
from dataclasses import dataclass, field
from enum import Enum

logger = logging.getLogger(__name__)


class CheckerErrorCategory(Enum):
    """Categories of PChecker errors."""
    NULL_TARGET = "null_target"
    UNHANDLED_EVENT = "unhandled_event"
    ASSERTION_FAILURE = "assertion_failure"
    DEADLOCK = "deadlock"
    LIVENESS_VIOLATION = "liveness_violation"
    QUEUE_OVERFLOW = "queue_overflow"
    UNKNOWN = "unknown"


@dataclass
class MachineState:
    """Represents a machine's state at a point in execution."""
    machine_id: str
    machine_type: str
    state: str
    
    @classmethod
    def from_log(cls, log_line: str) -> Optional["MachineState"]:
        """Parse machine state from a log line."""
        # Pattern: 'MachineName(ID)' in state 'StateName'
        match = re.search(r"'?(\w+)\((\d+)\)'?\s+(?:in state|enters state|exits state)\s+'([^']+)'", log_line)
        if match:
            return cls(
                machine_id=match.group(2),
                machine_type=match.group(1),
                state=match.group(3).split('.')[-1]  # Get just the state name
            )
        return None


@dataclass
class EventInfo:
    """Information about an event in the trace."""
    event_name: str
    payload: Optional[str] = None
    sender: Optional[str] = None
    receiver: Optional[str] = None
    
    @classmethod
    def from_log(cls, log_line: str) -> Optional["EventInfo"]:
        """Parse event info from a log line."""
        # SendLog pattern
        send_match = re.search(
            r"'(\w+)\((\d+)\)'\s+.*sent event '(\w+)(?:\s+with payload \(([^)]+)\))?'\s+to '(\w+)\((\d+)\)'",
            log_line
        )
        if send_match:
            return cls(
                event_name=send_match.group(3),
                payload=send_match.group(4),
                sender=f"{send_match.group(1)}({send_match.group(2)})",
                receiver=f"{send_match.group(5)}({send_match.group(6)})"
            )
        
        # DequeueLog pattern
        dequeue_match = re.search(
            r"'(\w+)\((\d+)\)'\s+dequeued event '(\w+)(?:\s+with payload \(([^)]+)\))?'",
            log_line
        )
        if dequeue_match:
            return cls(
                event_name=dequeue_match.group(3),
                payload=dequeue_match.group(4),
                receiver=f"{dequeue_match.group(1)}({dequeue_match.group(2)})"
            )
        
        return None


@dataclass
class CheckerError:
    """Parsed PChecker error with analysis."""
    category: CheckerErrorCategory
    message: str
    machine_type: Optional[str] = None
    machine_id: Optional[str] = None
    machine_state: Optional[str] = None
    event_name: Optional[str] = None
    target_field: Optional[str] = None
    raw_error_line: Optional[str] = None
    
    # Analysis results
    root_cause: Optional[str] = None
    affected_machines: List[str] = field(default_factory=list)
    suggested_fixes: List[str] = field(default_factory=list)
    
    @property
    def machine(self) -> Optional[str]:
        if self.machine_type and self.machine_id:
            return f"{self.machine_type}({self.machine_id})"
        return self.machine_type


@dataclass
class TraceAnalysis:
    """Complete analysis of a PChecker trace."""
    error: CheckerError
    execution_steps: int
    machines_involved: Dict[str, List[str]]  # machine_type -> [states visited]
    events_sent: List[EventInfo]
    last_actions: List[str]
    
    def get_summary(self) -> str:
        """Get a human-readable summary."""
        lines = [
            f"## PChecker Error Analysis",
            f"",
            f"**Error Category:** {self.error.category.value}",
            f"**Error Message:** {self.error.message}",
        ]
        
        if self.error.machine:
            lines.append(f"**Affected Machine:** {self.error.machine} in state '{self.error.machine_state}'")
        
        if self.error.root_cause:
            lines.append(f"")
            lines.append(f"### Root Cause Analysis")
            lines.append(self.error.root_cause)
        
        if self.error.suggested_fixes:
            lines.append(f"")
            lines.append(f"### Suggested Fixes")
            for i, fix in enumerate(self.error.suggested_fixes, 1):
                lines.append(f"{i}. {fix}")
        
        if self.last_actions:
            lines.append(f"")
            lines.append(f"### Last Actions Before Error")
            for action in self.last_actions[-5:]:
                lines.append(f"  - {action}")
        
        return "\n".join(lines)


class PCheckerErrorParser:
    """Parses and analyzes PChecker error traces."""
    
    # Error patterns
    NULL_TARGET_PATTERN = re.compile(
        r"Target in send cannot be null\. Machine (\w+)\((\d+)\) trying to send event (\w+) to null target in state (\w+)"
    )
    
    UNHANDLED_EVENT_PATTERN = re.compile(
        r"(\w+)\((\d+)\) received event '([^']+)' that cannot be handled"
    )
    
    ASSERTION_PATTERN = re.compile(
        r"Assertion .* failed|assert .* failed|AssertionFailure"
    )
    
    DEADLOCK_PATTERN = re.compile(
        r"[Dd]eadlock|potential deadlock"
    )
    
    LIVENESS_PATTERN = re.compile(
        r"[Ll]iveness|hot state|[Ll]iveness monitor"
    )
    
    def parse(self, trace_log: str) -> CheckerError:
        """Parse a trace log and return a CheckerError."""
        lines = trace_log.strip().split('\n')
        
        # Find the error line
        error_line = None
        for line in lines:
            if '<ErrorLog>' in line:
                error_line = line
                break
        
        if not error_line:
            # Try to find any error indicator
            for line in reversed(lines):
                if 'error' in line.lower() or 'bug' in line.lower():
                    error_line = line
                    break
        
        if not error_line:
            return CheckerError(
                category=CheckerErrorCategory.UNKNOWN,
                message="Could not identify error from trace",
                raw_error_line=lines[-1] if lines else None
            )
        
        # Try to match specific error patterns
        
        # 1. Null target error
        null_match = self.NULL_TARGET_PATTERN.search(error_line)
        if null_match:
            return CheckerError(
                category=CheckerErrorCategory.NULL_TARGET,
                message=error_line,
                machine_type=null_match.group(1),
                machine_id=null_match.group(2),
                event_name=null_match.group(3),
                machine_state=null_match.group(4),
                raw_error_line=error_line,
            )
        
        # 2. Unhandled event error
        unhandled_match = self.UNHANDLED_EVENT_PATTERN.search(error_line)
        if unhandled_match:
            return CheckerError(
                category=CheckerErrorCategory.UNHANDLED_EVENT,
                message=error_line,
                machine_type=unhandled_match.group(1),
                machine_id=unhandled_match.group(2),
                event_name=unhandled_match.group(3),
                raw_error_line=error_line,
            )
        
        # 3. Assertion failure
        if self.ASSERTION_PATTERN.search(error_line):
            return CheckerError(
                category=CheckerErrorCategory.ASSERTION_FAILURE,
                message=error_line,
                raw_error_line=error_line,
            )
        
        # 4. Deadlock
        if self.DEADLOCK_PATTERN.search(error_line):
            return CheckerError(
                category=CheckerErrorCategory.DEADLOCK,
                message=error_line,
                raw_error_line=error_line,
            )
        
        # 5. Liveness violation
        if self.LIVENESS_PATTERN.search(error_line):
            return CheckerError(
                category=CheckerErrorCategory.LIVENESS_VIOLATION,
                message=error_line,
                raw_error_line=error_line,
            )
        
        return CheckerError(
            category=CheckerErrorCategory.UNKNOWN,
            message=error_line,
            raw_error_line=error_line,
        )
    
    def analyze(self, trace_log: str, project_files: Dict[str, str] = None) -> TraceAnalysis:
        """Perform full analysis of a trace log."""
        error = self.parse(trace_log)
        
        lines = trace_log.strip().split('\n')
        
        # Extract execution info
        machines_involved: Dict[str, List[str]] = {}
        events_sent: List[EventInfo] = []
        last_actions: List[str] = []
        
        for line in lines:
            # Track machine states
            state = MachineState.from_log(line)
            if state:
                if state.machine_type not in machines_involved:
                    machines_involved[state.machine_type] = []
                if state.state not in machines_involved[state.machine_type]:
                    machines_involved[state.machine_type].append(state.state)
            
            # Track events
            event = EventInfo.from_log(line)
            if event:
                events_sent.append(event)
            
            # Track recent actions
            if any(x in line for x in ['<SendLog>', '<DequeueLog>', '<GotoLog>', '<StateLog>']):
                # Clean up the line
                clean = line.replace('<SendLog>', '').replace('<DequeueLog>', '')
                clean = clean.replace('<GotoLog>', '').replace('<StateLog>', '').strip()
                last_actions.append(clean)
        
        # Analyze root cause based on error type
        self._analyze_root_cause(error, trace_log, project_files or {}, machines_involved)
        
        return TraceAnalysis(
            error=error,
            execution_steps=len([l for l in lines if '<' in l and 'Log>' in l]),
            machines_involved=machines_involved,
            events_sent=events_sent,
            last_actions=last_actions[-10:],  # Keep last 10
        )
    
    def _analyze_root_cause(
        self,
        error: CheckerError,
        trace_log: str,
        project_files: Dict[str, str],
        machines_involved: Dict[str, List[str]]
    ):
        """Analyze the root cause and generate suggestions."""
        
        if error.category == CheckerErrorCategory.NULL_TARGET:
            self._analyze_null_target(error, trace_log, project_files)
        
        elif error.category == CheckerErrorCategory.UNHANDLED_EVENT:
            self._analyze_unhandled_event(error, trace_log, project_files)
        
        elif error.category == CheckerErrorCategory.ASSERTION_FAILURE:
            self._analyze_assertion_failure(error, trace_log, project_files)
        
        elif error.category == CheckerErrorCategory.DEADLOCK:
            self._analyze_deadlock(error, trace_log, project_files, machines_involved)
    
    def _analyze_null_target(
        self,
        error: CheckerError,
        trace_log: str,
        project_files: Dict[str, str]
    ):
        """Analyze null target error."""
        error.root_cause = (
            f"Machine '{error.machine_type}' tried to send event '{error.event_name}' "
            f"to a null machine reference in state '{error.machine_state}'. "
            f"This typically means a machine field was not initialized before use."
        )
        
        # Find the machine file
        machine_file = None
        for filename, content in project_files.items():
            if f"machine {error.machine_type}" in content:
                machine_file = filename
                break
        
        if machine_file:
            # Try to identify the field
            content = project_files[machine_file]
            
            # Look for the state where the error occurred
            state_pattern = rf"state\s+{error.machine_state}\s*\{{"
            state_match = re.search(state_pattern, content)
            
            # Look for send statements with the event
            send_pattern = rf"send\s+(\w+)\s*,\s*{error.event_name}"
            send_matches = re.findall(send_pattern, content)
            
            if send_matches:
                target_var = send_matches[0]
                error.target_field = target_var
                
                error.suggested_fixes = [
                    f"Initialize the '{target_var}' field before entering state '{error.machine_state}'",
                    f"Add a configuration event (e.g., eConfigureXXX) to set '{target_var}' during initialization",
                    f"Use a 'with' clause on the transition to pass the target machine reference",
                    f"Check if '{target_var}' is set in the machine's start state entry function",
                ]
        else:
            error.suggested_fixes = [
                "Ensure all machine references are initialized before sending events",
                "Add configuration events to wire machines together at startup",
                "Check the test driver/harness to ensure proper machine initialization order",
            ]
    
    def _analyze_unhandled_event(
        self,
        error: CheckerError,
        trace_log: str,
        project_files: Dict[str, str]
    ):
        """Analyze unhandled event error."""
        # Find what state the machine was in
        state_match = re.search(
            rf"{error.machine_type}\({error.machine_id}\).*state\s+'([^']+)'",
            trace_log
        )
        if state_match:
            error.machine_state = state_match.group(1).split('.')[-1]
        
        error.root_cause = (
            f"Machine '{error.machine_type}' received event '{error.event_name}' "
            f"in state '{error.machine_state or 'unknown'}' but has no handler for it. "
            f"This can happen when events arrive out of order or a state transition "
            f"is missing an event handler."
        )
        
        error.suggested_fixes = [
            f"Add 'on {error.event_name} do HandleXXX;' to state '{error.machine_state}'",
            f"Add 'ignore {error.event_name};' if this event should be silently dropped",
            f"Add 'defer {error.event_name};' if this event should be handled later",
            "Review the state machine transitions to ensure all states handle expected events",
        ]
    
    def _analyze_assertion_failure(
        self,
        error: CheckerError,
        trace_log: str,
        project_files: Dict[str, str]
    ):
        """Analyze assertion failure."""
        # Try to extract assertion details
        assert_match = re.search(r"assert\s+([^,;]+)", trace_log)
        
        error.root_cause = (
            "An assertion in the P code failed during execution. "
            "This indicates the system reached an unexpected state."
        )
        
        if assert_match:
            error.root_cause += f" The assertion was: '{assert_match.group(1)}'"
        
        error.suggested_fixes = [
            "Review the assertion condition and ensure prerequisites are met",
            "Add guards or checks before the assertion to prevent invalid states",
            "Check if the assertion is overly strict for the current test scenario",
        ]
    
    def _analyze_deadlock(
        self,
        error: CheckerError,
        trace_log: str,
        project_files: Dict[str, str],
        machines_involved: Dict[str, List[str]]
    ):
        """Analyze deadlock error."""
        # List machines and their final states
        final_states = []
        for machine, states in machines_involved.items():
            if states:
                final_states.append(f"{machine}: {states[-1]}")
        
        error.root_cause = (
            "The system reached a deadlock where no machine can make progress. "
            f"Machines involved: {', '.join(final_states)}"
        )
        
        error.suggested_fixes = [
            "Check for circular dependencies between machines",
            "Ensure all expected events are being sent",
            "Add timeout mechanisms to break potential deadlocks",
            "Review the test driver to ensure all necessary events are triggered",
        ]
