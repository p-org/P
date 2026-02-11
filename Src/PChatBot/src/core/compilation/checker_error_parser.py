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
class SenderInfo:
    """Information about who sent the problematic event."""
    machine_type: Optional[str] = None
    machine_id: Optional[str] = None
    state: Optional[str] = None
    is_test_driver: bool = False
    event_payload: Optional[str] = None
    is_initialization_pattern: bool = False
    semantic_mismatch: Optional[str] = None

    @property
    def machine(self) -> Optional[str]:
        if self.machine_type and self.machine_id:
            return f"{self.machine_type}({self.machine_id})"
        return self.machine_type


@dataclass
class CascadingImpact:
    """Analysis of which other machines/states would also be affected."""
    # machines that also lack handlers for the event: {machine_type: [states]}
    unhandled_in: Dict[str, List[str]] = field(default_factory=dict)
    # machines that broadcast this event
    broadcasters: List[str] = field(default_factory=list)
    # all receiver machines for this event
    all_receivers: List[str] = field(default_factory=list)


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

    # Enhanced analysis fields
    sender_info: Optional[SenderInfo] = None
    cascading_impact: Optional[CascadingImpact] = None
    is_test_driver_bug: bool = False
    requires_new_event: bool = False
    requires_multi_file_fix: bool = False
    
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
        
        if self.error.sender_info:
            sender = self.error.sender_info
            lines.append(f"**Sender:** {sender.machine or 'unknown'} in state '{sender.state or 'unknown'}'")
            if sender.is_test_driver:
                lines.append(f"**Bug Location:** Test driver (not protocol logic)")
            if sender.is_initialization_pattern:
                lines.append(f"**Pattern:** Event used for initialization (semantic mismatch)")
            if sender.semantic_mismatch:
                lines.append(f"**Semantic Mismatch:** {sender.semantic_mismatch}")
        
        if self.error.cascading_impact and self.error.cascading_impact.unhandled_in:
            lines.append(f"")
            lines.append(f"### Cascading Impact")
            for machine, states in self.error.cascading_impact.unhandled_in.items():
                lines.append(f"  - {machine} also lacks handler in states: {', '.join(states)}")
        
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
            raw_event = unhandled_match.group(3)
            # Strip namespace prefix (e.g., PImplementation.eLearn -> eLearn)
            clean_event = raw_event.split('.')[-1] if '.' in raw_event else raw_event
            return CheckerError(
                category=CheckerErrorCategory.UNHANDLED_EVENT,
                message=error_line,
                machine_type=unhandled_match.group(1),
                machine_id=unhandled_match.group(2),
                event_name=clean_event,
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
        """Analyze unhandled event error with sender trace-back and cascading impact."""
        # Find what state the machine was in
        state_match = re.search(
            rf"{error.machine_type}\({error.machine_id}\).*state\s+'([^']+)'",
            trace_log
        )
        if state_match:
            error.machine_state = state_match.group(1).split('.')[-1]

        # --- Sender trace-back analysis ---
        sender_info = self._trace_back_to_sender(error, trace_log, project_files)
        error.sender_info = sender_info

        # --- Cascading impact analysis ---
        cascading = self._analyze_cascading_impact(error, project_files)
        error.cascading_impact = cascading

        # --- Build root cause with enhanced context ---
        root_parts = [
            f"Machine '{error.machine_type}' received event '{error.event_name}' "
            f"in state '{error.machine_state or 'unknown'}' but has no handler for it."
        ]

        if sender_info:
            if sender_info.is_test_driver:
                root_parts.append(
                    f" The event was sent by test driver '{sender_info.machine_type}' "
                    f"during setup (not by protocol logic)."
                )
                error.is_test_driver_bug = True
            else:
                root_parts.append(
                    f" The event was sent by '{sender_info.machine or 'unknown'}' "
                    f"in state '{sender_info.state or 'unknown'}'."
                )

            if sender_info.is_initialization_pattern:
                root_parts.append(
                    f" This appears to be a SEMANTIC MISMATCH: the event '{error.event_name}' "
                    f"is a protocol event being misused for initialization/setup."
                )
                error.requires_new_event = True

            if sender_info.semantic_mismatch:
                root_parts.append(f" {sender_info.semantic_mismatch}")

        if cascading and cascading.unhandled_in:
            other_machines = [m for m in cascading.unhandled_in if m != error.machine_type]
            if other_machines:
                root_parts.append(
                    f" Additionally, machines [{', '.join(other_machines)}] also lack "
                    f"handlers for '{error.event_name}' — this is a multi-file issue."
                )
                error.requires_multi_file_fix = True

        error.root_cause = "".join(root_parts)

        # --- Build suggested fixes with enhanced context ---
        fixes = []
        if error.is_test_driver_bug:
            fixes.append(
                f"Fix the test driver: introduce a dedicated setup event "
                f"(e.g., eSetup{error.machine_type}) instead of reusing "
                f"the protocol event '{error.event_name}' for initialization"
            )
            fixes.append(
                f"Define the new setup event in Enums_Types_Events.p and "
                f"add a handler for it in {error.machine_type}'s Init state"
            )
        if error.requires_new_event:
            fixes.append(
                "This requires a design-level change: add a new event type "
                "to the types file, not just a handler in the receiving machine"
            )
        if error.requires_multi_file_fix:
            fixes.append(
                f"Add 'ignore {error.event_name};' to ALL machines that may "
                f"receive it: {', '.join(cascading.all_receivers) if cascading else 'unknown'}"
            )
        # Always include the mechanical fixes as fallback
        fixes.append(f"Add 'on {error.event_name} do HandleXXX;' to state '{error.machine_state}'")
        fixes.append(f"Add 'ignore {error.event_name};' if this event should be silently dropped")
        fixes.append(f"Add 'defer {error.event_name};' if this event should be handled later")

        error.suggested_fixes = fixes

    def _trace_back_to_sender(
        self,
        error: CheckerError,
        trace_log: str,
        project_files: Dict[str, str]
    ) -> Optional[SenderInfo]:
        """
        Trace back through the execution log to find who sent the problematic event
        and analyze their intent.
        """
        event_name = error.event_name
        if not event_name:
            return None

        # Strip namespace prefix (e.g. PImplementation.eLearn -> eLearn)
        clean_event = event_name.split('.')[-1] if '.' in event_name else event_name

        # Find the SendLog that sent this event to the error machine
        receiver_pattern = rf"<SendLog>\s*'(\w+)\((\d+)\)'\s+in state '([^']+)'.*sent event '{clean_event}.*to '{error.machine_type}\({error.machine_id}\)'"
        send_match = re.search(receiver_pattern, trace_log)

        if not send_match:
            # Try with namespace prefix
            receiver_pattern2 = rf"<SendLog>\s*'(\w+)\((\d+)\)'\s+in state '([^']+)'.*sent event '.*{clean_event}.*to '{error.machine_type}\({error.machine_id}\)'"
            send_match = re.search(receiver_pattern2, trace_log)

        if not send_match:
            return None

        sender_type = send_match.group(1)
        sender_id = send_match.group(2)
        sender_state = send_match.group(3).split('.')[-1]

        sender_info = SenderInfo(
            machine_type=sender_type,
            machine_id=sender_id,
            state=sender_state,
        )

        # --- Test driver detection ---
        # Heuristics: test driver machines are in PTst/ and often have names like
        # "Scenario*", "TestDriver*", "Test*", or are the main machine in a test decl.
        sender_info.is_test_driver = self._is_test_driver_machine(
            sender_type, sender_state, project_files
        )

        # --- Initialization pattern detection ---
        # If the sender is a test driver and the event was sent during the start
        # state's entry action, this is likely an initialization pattern.
        if sender_info.is_test_driver and sender_state == "Init":
            sender_info.is_initialization_pattern = True

        # --- Semantic mismatch detection ---
        # Check if the event has a "real" protocol purpose by looking at
        # whether protocol machines (non-test) also send this event.
        protocol_senders = self._find_protocol_senders(clean_event, project_files)
        if sender_info.is_test_driver and protocol_senders:
            sender_info.semantic_mismatch = (
                f"Event '{clean_event}' is used in protocol logic by "
                f"[{', '.join(protocol_senders)}], but the test driver is also "
                f"sending it with a dummy payload for setup purposes. "
                f"This is a semantic mismatch — use a dedicated setup event instead."
            )

        # Extract the payload from the SendLog line
        payload_match = re.search(r'with payload \(([^)]+)\)', send_match.group(0))
        if payload_match:
            sender_info.event_payload = payload_match.group(1)

            # Check for dummy payload indicators (e.g., value:0, default values)
            payload = sender_info.event_payload
            if sender_info.is_initialization_pattern:
                # Count how many fields look like defaults (0, false, null, default)
                default_indicators = re.findall(r':\s*(?:0|false|null|default)\b', payload)
                if default_indicators:
                    sender_info.semantic_mismatch = (
                        sender_info.semantic_mismatch or ""
                    ) + (
                        f" The payload contains {len(default_indicators)} default/zero value(s), "
                        f"suggesting this is a dummy initialization message, not a real protocol event."
                    )

        return sender_info

    def _is_test_driver_machine(
        self,
        machine_type: str,
        state: str,
        project_files: Dict[str, str]
    ) -> bool:
        """Determine if a machine type is a test driver (lives in PTst/)."""
        # Check if this machine is defined in PTst/ files
        for filepath, content in project_files.items():
            if filepath.startswith('PTst/') or '/PTst/' in filepath:
                if f"machine {machine_type}" in content:
                    return True
        
        # Heuristic: common test driver naming patterns
        test_patterns = [
            r'^Scenario\d*',
            r'^Test',
            r'TestDriver',
            r'TestHarness',
            r'_Test$',
        ]
        for pattern in test_patterns:
            if re.match(pattern, machine_type, re.IGNORECASE):
                return True

        return False

    def _find_protocol_senders(
        self,
        event_name: str,
        project_files: Dict[str, str]
    ) -> List[str]:
        """Find non-test machines that send a given event."""
        senders = []
        for filepath, content in project_files.items():
            if filepath.startswith('PTst/') or '/PTst/' in filepath:
                continue
            if filepath.startswith('PSpec/') or '/PSpec/' in filepath:
                continue
            # Look for send statements with this event
            # Use [^,]+ to match targets like allComponents[i]
            send_pattern = rf"send\s+[^,]+\s*,\s*{re.escape(event_name)}\b"
            if re.search(send_pattern, content):
                # Extract machine name from this file
                machine_match = re.search(r'machine\s+(\w+)', content)
                if machine_match:
                    senders.append(machine_match.group(1))
        return senders

    def _analyze_cascading_impact(
        self,
        error: CheckerError,
        project_files: Dict[str, str]
    ) -> Optional[CascadingImpact]:
        """
        Analyze which other machines and states also lack handlers for this event.
        This identifies multi-file fix requirements.
        """
        event_name = error.event_name
        if not event_name:
            return None

        # Strip namespace prefix
        clean_event = event_name.split('.')[-1] if '.' in event_name else event_name

        impact = CascadingImpact()

        # Find all machines that could receive this event
        # (look for send statements targeting various machines)
        for filepath, content in project_files.items():
            if filepath.startswith('PSpec/') or '/PSpec/' in filepath:
                continue

            # Find machines defined in this file
            machine_matches = re.finditer(r'machine\s+(\w+)\s*\{', content)
            for mm in machine_matches:
                machine_name = mm.group(1)

                # Check if this machine sends the event (broadcaster)
                # Use [^,]+ to match targets like allComponents[i]
                send_pattern = rf'send\s+[^,]+\s*,\s*{re.escape(clean_event)}\b'
                if re.search(send_pattern, content):
                    if machine_name not in impact.broadcasters:
                        impact.broadcasters.append(machine_name)

                # Find all states in this machine
                states = re.findall(
                    r'(?:start\s+)?state\s+(\w+)\s*\{([^}]*(?:\{[^}]*\}[^}]*)*)\}',
                    content
                )

                unhandled_states = []
                for state_name, state_body in states:
                    # Check if this state handles the event
                    handlers = [
                        rf'on\s+{re.escape(clean_event)}\b',
                        rf'ignore\s+[^;]*\b{re.escape(clean_event)}\b',
                        rf'defer\s+[^;]*\b{re.escape(clean_event)}\b',
                    ]
                    handled = any(re.search(h, state_body) for h in handlers)
                    if not handled:
                        unhandled_states.append(state_name)

                if unhandled_states:
                    impact.unhandled_in[machine_name] = unhandled_states
                    if machine_name not in impact.all_receivers:
                        impact.all_receivers.append(machine_name)

        return impact
    
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
