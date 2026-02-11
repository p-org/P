"""
Specialized PChecker Error Fixers

Implements automatic fixes for common PChecker runtime errors.

Enhanced with:
- Multi-file fix support (fixes across types, machines, tests)
- Sender trace-back analysis (fixes the sender, not just receiver)
- Test-driver vs protocol bug distinction
- Cascading impact analysis (fix all affected machines)
- Semantic mismatch detection (new event introduction)
"""

import re
import logging
from typing import Dict, List, Optional, Tuple
from dataclasses import dataclass, field
from pathlib import Path

from .checker_error_parser import (
    CheckerError, CheckerErrorCategory, TraceAnalysis,
    SenderInfo, CascadingImpact,
)

logger = logging.getLogger(__name__)


@dataclass
class FilePatch:
    """A single file modification within a multi-file fix."""
    file_path: str
    original_code: str
    fixed_code: str
    description: str


@dataclass
class CheckerFix:
    """A fix for a PChecker error, potentially spanning multiple files."""
    file_path: str
    original_code: str
    fixed_code: str
    description: str
    confidence: float  # 0.0 to 1.0
    requires_review: bool = False
    review_notes: Optional[str] = None
    # Multi-file fix support
    additional_patches: List[FilePatch] = field(default_factory=list)
    is_multi_file: bool = False
    fix_strategy: Optional[str] = None  # e.g., "ignore", "new_event", "test_driver_fix"


class PCheckerErrorFixer:
    """Fixes common PChecker errors automatically."""
    
    def __init__(self, project_path: str, project_files: Dict[str, str]):
        """
        Initialize fixer with project context.
        
        Args:
            project_path: Path to the P project root
            project_files: Dict mapping filenames to their content
        """
        self.project_path = project_path
        self.project_files = project_files
    
    def can_fix(self, error: CheckerError) -> bool:
        """Check if we can automatically fix this error."""
        return error.category in [
            CheckerErrorCategory.NULL_TARGET,
            CheckerErrorCategory.UNHANDLED_EVENT,
        ]
    
    def fix(self, analysis: TraceAnalysis) -> Optional[CheckerFix]:
        """
        Attempt to fix the error.
        
        Args:
            analysis: Full trace analysis
            
        Returns:
            CheckerFix if successful, None otherwise
        """
        error = analysis.error
        
        if error.category == CheckerErrorCategory.NULL_TARGET:
            return self._fix_null_target(error, analysis)
        
        elif error.category == CheckerErrorCategory.UNHANDLED_EVENT:
            return self._fix_unhandled_event(error, analysis)
        
        return None
    
    def _fix_null_target(
        self,
        error: CheckerError,
        analysis: TraceAnalysis
    ) -> Optional[CheckerFix]:
        """
        Fix null target error by identifying and suggesting initialization.
        
        This is a complex fix that may require:
        1. Adding a configuration event
        2. Modifying the test driver
        3. Adding initialization in the machine
        """
        # Find the machine file
        machine_file = None
        machine_content = None
        
        for filename, content in self.project_files.items():
            if f"machine {error.machine_type}" in content:
                machine_file = filename
                machine_content = content
                break
        
        if not machine_file or not machine_content:
            logger.warning(f"Could not find machine file for {error.machine_type}")
            return None
        
        # Identify the target field being sent to
        send_pattern = rf"send\s+(\w+)\s*,\s*{error.event_name}"
        send_matches = re.findall(send_pattern, machine_content)
        
        if not send_matches:
            return None
        
        target_field = send_matches[0]
        
        # Check if there's already a configuration event handler
        config_event = f"eConfig{error.machine_type}"
        has_config = config_event.lower() in machine_content.lower()
        
        if has_config:
            # Config event exists but field not being set properly
            return CheckerFix(
                file_path=self._get_full_path(machine_file),
                original_code=machine_content,
                fixed_code=machine_content,  # No auto-fix possible
                description=f"Field '{target_field}' is null. Check that {config_event} properly sets this field.",
                confidence=0.3,
                requires_review=True,
                review_notes=f"The machine has a configuration event but '{target_field}' is still null. "
                            f"Verify the test driver sends the config event with a valid machine reference."
            )
        
        # Generate a fix that adds configuration handling
        fixed_content = self._add_config_event_handler(
            machine_content,
            error.machine_type,
            target_field
        )
        
        if fixed_content == machine_content:
            return None
        
        return CheckerFix(
            file_path=self._get_full_path(machine_file),
            original_code=machine_content,
            fixed_code=fixed_content,
            description=f"Added configuration event handler to set '{target_field}' in {error.machine_type}",
            confidence=0.6,
            requires_review=True,
            review_notes=f"Added eConfig{error.machine_type} handler. You also need to:\n"
                        f"1. Define the event 'eConfig{error.machine_type}' in Enums_Types_Events.p\n"
                        f"2. Update the test driver to send this event with the target machine reference"
        )
    
    def _fix_unhandled_event(
        self,
        error: CheckerError,
        analysis: TraceAnalysis
    ) -> Optional[CheckerFix]:
        """
        Fix unhandled event with deep analysis.
        
        Strategy selection (in priority order):
        1. If sender is a test driver misusing a protocol event → fix test driver
        2. If event is broadcast to many machines → add ignore to ALL affected machines
        3. Simple case → add ignore to the single affected state
        """
        # Clean event name
        clean_event = error.event_name
        if clean_event and '.' in clean_event:
            clean_event = clean_event.split('.')[-1]

        if not clean_event:
            return None

        # Determine the best fix strategy based on enhanced analysis
        if error.is_test_driver_bug and error.sender_info:
            return self._fix_test_driver_misuse(error, analysis, clean_event)

        if error.requires_multi_file_fix and error.cascading_impact:
            return self._fix_cascading_unhandled_event(error, analysis, clean_event)

        # Fallback: simple single-state ignore fix
        return self._fix_single_state_unhandled(error, analysis, clean_event)

    def _fix_test_driver_misuse(
        self,
        error: CheckerError,
        analysis: TraceAnalysis,
        clean_event: str,
    ) -> Optional[CheckerFix]:
        """
        Fix a test driver that misuses a protocol event for initialization.
        
        Strategy: Remove the bad send from the test driver and add a dedicated
        setup event with proper handler in the receiving machine.
        """
        sender = error.sender_info
        if not sender:
            return None

        # Find the test driver file
        test_file = None
        test_content = None
        for filename, content in self.project_files.items():
            if f"machine {sender.machine_type}" in content:
                test_file = filename
                test_content = content
                break

        if not test_file or not test_content:
            return self._fix_single_state_unhandled(error, analysis, clean_event)

        # Find the receiver machine file
        receiver_file = None
        receiver_content = None
        for filename, content in self.project_files.items():
            if f"machine {error.machine_type}" in content:
                receiver_file = filename
                receiver_content = content
                break

        if not receiver_file or not receiver_content:
            return self._fix_single_state_unhandled(error, analysis, clean_event)

        # Find the types/events file
        types_file = None
        types_content = None
        for filename, content in self.project_files.items():
            if 'Enums_Types_Events' in filename or ('event ' in content and 'machine ' not in content):
                types_file = filename
                types_content = content
                break

        if not types_file or not types_content:
            return self._fix_single_state_unhandled(error, analysis, clean_event)

        # --- Determine the payload type of the protocol event ---
        event_type_match = re.search(
            rf'event\s+{re.escape(clean_event)}\s*:\s*(\w+)',
            types_content
        )
        event_payload_type = event_type_match.group(1) if event_type_match else None

        # --- Find what field the test driver was trying to set ---
        # Look at the send statement in the test driver for the event
        send_pattern = rf'send\s+\w+\s*,\s*{re.escape(clean_event)}\s*,\s*\(([^)]+)\)'
        send_match = re.search(send_pattern, test_content)
        
        if not send_match:
            return self._fix_single_state_unhandled(error, analysis, clean_event)

        # Determine what the test was trying to accomplish
        # e.g., send learner, eLearn, (allComponents = learner, agreedValue = 0)
        # → trying to set allComponents on the Learner
        payload_str = send_match.group(1)
        
        # Create a new setup event name
        setup_event_name = f"eSetup{error.machine_type}Components"

        # --- Determine the appropriate type for the setup event ---
        # Parse the payload fields to figure out what the test was setting
        # Look for fields with non-default values (not = 0, not = false)
        field_matches = re.findall(r'(\w+)\s*=\s*([^,]+)', payload_str)
        setup_fields = []
        for field_name, field_value in field_matches:
            field_value = field_value.strip()
            # Skip fields with default/dummy values
            if field_value in ('0', 'false', 'null', 'default(int)', 'default(bool)'):
                continue
            setup_fields.append(field_name)

        # Determine the type for the setup event based on receiver machine vars
        # Look at what variable types the receiver has
        setup_event_type = "seq[machine]"  # reasonable default for component setup
        var_pattern = rf'var\s+(\w+)\s*:\s*([^;]+);'
        var_matches = re.findall(var_pattern, receiver_content)
        for var_name, var_type in var_matches:
            if var_name in setup_fields:
                setup_event_type = var_type.strip()
                break

        # --- Build the multi-file fix ---
        patches = []

        # Patch 1: Add new setup event to types file
        new_types = types_content.rstrip()
        new_types += f"\n\n// Setup event for {error.machine_type} initialization\n"
        new_types += f"event {setup_event_name}: {setup_event_type};\n"
        patches.append(FilePatch(
            file_path=self._get_full_path(types_file),
            original_code=types_content,
            fixed_code=new_types,
            description=f"Added setup event '{setup_event_name}: {setup_event_type}' to types file"
        ))

        # Patch 2: Add handler in receiver machine
        fixed_receiver = self._add_setup_event_handler(
            receiver_content,
            error.machine_type,
            setup_event_name,
            setup_event_type,
            setup_fields,
            clean_event,
        )
        if fixed_receiver != receiver_content:
            patches.append(FilePatch(
                file_path=self._get_full_path(receiver_file),
                original_code=receiver_content,
                fixed_code=fixed_receiver,
                description=f"Added handler for '{setup_event_name}' and 'ignore {clean_event}' in {error.machine_type}"
            ))

        # Patch 3: Fix test driver to use new setup event
        # Replace the bad send with the new setup event
        old_send = send_match.group(0)
        # Build the new send — extract the target variable from the old send
        target_match = re.search(rf'send\s+(\w+)\s*,\s*{re.escape(clean_event)}', old_send)
        target_var = target_match.group(1) if target_match else error.machine_type.lower()
        
        # Build new send payload from non-default fields
        new_payload_parts = []
        for field_name, field_value in field_matches:
            field_value = field_value.strip()
            if field_value not in ('0', 'false', 'null'):
                new_payload_parts.append(field_value)

        if new_payload_parts and setup_event_type.startswith('seq['):
            # For sequence types, we need to send the sequence variable
            new_send = f"send {target_var}, {setup_event_name}, {new_payload_parts[0]}"
        elif new_payload_parts:
            new_send = f"send {target_var}, {setup_event_name}, {new_payload_parts[0]}"
        else:
            # Comment out the bad send instead
            new_send = f"// Removed: {old_send} (was misusing protocol event for setup)"

        fixed_test = test_content.replace(old_send, new_send)
        if fixed_test != test_content:
            patches.append(FilePatch(
                file_path=self._get_full_path(test_file),
                original_code=test_content,
                fixed_code=fixed_test,
                description=f"Replaced misused '{clean_event}' send with '{setup_event_name}' in test driver"
            ))

        # Patch 4: Add ignore for the protocol event in all affected machines
        cascading = error.cascading_impact
        if cascading:
            for machine_name, states in cascading.unhandled_in.items():
                if machine_name == error.machine_type:
                    continue  # Already handled in Patch 2
                if machine_name == sender.machine_type:
                    continue  # Test driver, doesn't need ignore

                for filename, content in self.project_files.items():
                    if f"machine {machine_name}" in content:
                        patched = self._add_ignore_to_all_states(
                            content, machine_name, clean_event, states
                        )
                        if patched != content:
                            patches.append(FilePatch(
                                file_path=self._get_full_path(filename),
                                original_code=content,
                                fixed_code=patched,
                                description=f"Added 'ignore {clean_event};' to {machine_name} states: {', '.join(states)}"
                            ))
                        break

        if not patches:
            return self._fix_single_state_unhandled(error, analysis, clean_event)

        # Use the first patch as the primary fix, rest as additional
        primary = patches[0]
        return CheckerFix(
            file_path=primary.file_path,
            original_code=primary.original_code,
            fixed_code=primary.fixed_code,
            description=f"Multi-file fix: {'; '.join(p.description for p in patches)}",
            confidence=0.7,
            requires_review=True,
            review_notes=(
                f"This fix introduces a new setup event '{setup_event_name}' and modifies "
                f"{len(patches)} files. The test driver was misusing protocol event "
                f"'{clean_event}' for initialization."
            ),
            additional_patches=patches[1:],
            is_multi_file=True,
            fix_strategy="new_event",
        )

    def _fix_cascading_unhandled_event(
        self,
        error: CheckerError,
        analysis: TraceAnalysis,
        clean_event: str,
    ) -> Optional[CheckerFix]:
        """
        Fix an unhandled event that affects multiple machines.
        Adds ignore statements to ALL affected machines and states.
        """
        cascading = error.cascading_impact
        if not cascading:
            return self._fix_single_state_unhandled(error, analysis, clean_event)

        patches = []
        for machine_name, states in cascading.unhandled_in.items():
            for filename, content in self.project_files.items():
                if f"machine {machine_name}" in content:
                    patched = self._add_ignore_to_all_states(
                        content, machine_name, clean_event, states
                    )
                    if patched != content:
                        patches.append(FilePatch(
                            file_path=self._get_full_path(filename),
                            original_code=content,
                            fixed_code=patched,
                            description=(
                                f"Added 'ignore {clean_event};' to {machine_name} "
                                f"states: {', '.join(states)}"
                            ),
                        ))
                    break

        if not patches:
            return None

        primary = patches[0]
        return CheckerFix(
            file_path=primary.file_path,
            original_code=primary.original_code,
            fixed_code=primary.fixed_code,
            description=f"Multi-file fix: added 'ignore {clean_event}' across {len(patches)} files",
            confidence=0.75,
            requires_review=True,
            review_notes=(
                f"Event '{clean_event}' was unhandled in {len(patches)} machine files. "
                f"All affected states now ignore it. Review to ensure this is the correct behavior."
            ),
            additional_patches=patches[1:],
            is_multi_file=True,
            fix_strategy="cascading_ignore",
        )

    def _fix_single_state_unhandled(
        self,
        error: CheckerError,
        analysis: TraceAnalysis,
        clean_event: str,
    ) -> Optional[CheckerFix]:
        """Original simple fix: add ignore to the single affected state."""
        machine_file = None
        machine_content = None
        
        for filename, content in self.project_files.items():
            if f"machine {error.machine_type}" in content:
                machine_file = filename
                machine_content = content
                break
        
        if not machine_file or not machine_content:
            return None
        
        state_name = error.machine_state
        if not state_name:
            return None
        if '.' in state_name:
            state_name = state_name.split('.')[-1]
        
        fixed_content = self._add_ignore_to_all_states(
            machine_content, error.machine_type, clean_event, [state_name]
        )

        if fixed_content == machine_content:
            return None
        
        return CheckerFix(
            file_path=self._get_full_path(machine_file),
            original_code=machine_content,
            fixed_code=fixed_content,
            description=f"Added 'ignore {clean_event};' to state '{state_name}' in {error.machine_type}",
            confidence=0.8,
            requires_review=True,
            review_notes=(
                f"Event '{clean_event}' will now be silently ignored in state '{state_name}'. "
                f"If you need to handle it differently, change 'ignore' to 'defer' or add a proper handler."
            ),
            fix_strategy="simple_ignore",
        )

    # =========================================================================
    # Helper methods for building fixes
    # =========================================================================

    def _add_setup_event_handler(
        self,
        machine_content: str,
        machine_type: str,
        setup_event_name: str,
        setup_event_type: str,
        fields_to_set: List[str],
        protocol_event_to_ignore: str,
    ) -> str:
        """Add a setup event handler and ignore for the misused protocol event."""
        # Find all states and add ignore for the protocol event
        result = self._add_ignore_to_all_states(
            machine_content, machine_type, protocol_event_to_ignore, None  # None = all states
        )

        # Find the start state to add the setup event handler
        start_match = re.search(r'(start\s+state\s+\w+\s*\{)', result)
        if not start_match:
            return result

        # Find where to insert the handler (after entry or at start of state)
        state_start = start_match.end()
        # Find the entry statement
        entry_match = re.search(r'entry\s+\w+\s*;', result[state_start:])

        if entry_match:
            insert_pos = state_start + entry_match.end()
        else:
            insert_pos = state_start

        # Build handler based on the type
        if fields_to_set:
            field_assignments = "\n".join(
                f"            {f} = payload;" for f in fields_to_set[:1]
            )
            handler = f"""
        on {setup_event_name} do (payload: {setup_event_type}) {{
{field_assignments}
        }}"""
        else:
            handler = f"\n        on {setup_event_name} do (payload: {setup_event_type}) {{ }}"

        result = result[:insert_pos] + handler + result[insert_pos:]
        return result

    def _add_ignore_to_all_states(
        self,
        content: str,
        machine_type: str,
        event_name: str,
        states: Optional[List[str]],
    ) -> str:
        """
        Add 'ignore event_name;' to specified states (or all states if states is None)
        in the given machine content.
        """
        # Find all states in the machine
        # We need to handle both "start state X {" and "state X {"
        state_pattern = r'((?:start\s+)?state\s+(\w+)\s*\{)'
        
        result = content
        offset = 0
        
        for match in re.finditer(state_pattern, content):
            state_decl = match.group(1)
            state_name = match.group(2)

            # Skip if we only want specific states and this isn't one
            if states is not None and state_name not in states:
                continue

            # Check if this event is already handled in this state
            # Find the state body (everything between the opening { and matching })
            state_start = match.start()
            brace_start = match.end() - 1  # Position of opening {
            
            # Find matching closing brace
            depth = 1
            pos = brace_start + 1
            while pos < len(result) and depth > 0:
                if result[pos] == '{':
                    depth += 1
                elif result[pos] == '}':
                    depth -= 1
                pos += 1
            
            state_body = result[brace_start:pos]
            
            # Check if event is already handled
            handlers = [
                rf'on\s+{re.escape(event_name)}\b',
                rf'ignore\s+[^;]*\b{re.escape(event_name)}\b',
                rf'defer\s+[^;]*\b{re.escape(event_name)}\b',
            ]
            if any(re.search(h, state_body) for h in handlers):
                continue

            # Determine indentation from existing state content
            indent = "        "
            for line in state_body.split('\n'):
                stripped = line.lstrip()
                if stripped.startswith('on ') or stripped.startswith('ignore ') or stripped.startswith('entry '):
                    indent = line[:len(line) - len(stripped)]
                    break

            # Check if there's an existing ignore statement we can extend
            existing_ignore = re.search(r'(ignore\s+[^;]+);', state_body)
            if existing_ignore:
                old_ignore = existing_ignore.group(0)
                # Add the event to the existing ignore list
                new_ignore = old_ignore.replace(';', f', {event_name};')
                result = result[:brace_start] + state_body.replace(old_ignore, new_ignore) + result[pos:]
            else:
                # Insert a new ignore statement before the closing brace
                ignore_line = f"{indent}ignore {event_name};\n"
                # Insert before the last line (closing brace)
                insert_pos = brace_start + len(state_body) - 1
                # Find the position just before the closing }
                while insert_pos > brace_start and result[insert_pos - 1] in (' ', '\t', '\n'):
                    insert_pos -= 1
                insert_pos += 1  # After the last newline
                
                result = result[:insert_pos] + ignore_line + result[insert_pos:]

        return result
    
    def _add_config_event_handler(
        self,
        machine_content: str,
        machine_type: str,
        target_field: str
    ) -> str:
        """Add a configuration event handler to the machine."""
        config_event = f"eConfig{machine_type}"
        config_type = f"tConfig{machine_type}"
        
        # Find the start state
        start_match = re.search(r"(start\s+state\s+\w+\s*\{)", machine_content)
        if not start_match:
            return machine_content
        
        # Add the handler after the entry statement or at the start of the state
        state_start = start_match.end()
        
        # Find entry statement
        entry_match = re.search(r"entry\s+\w+\s*;", machine_content[state_start:])
        if entry_match:
            insert_pos = state_start + entry_match.end()
        else:
            insert_pos = state_start
        
        # Generate handler
        handler = f"""
    on {config_event} do (config: {config_type}) {{
      {target_field} = config.{target_field};
    }}"""
        
        # Insert handler
        fixed_content = machine_content[:insert_pos] + handler + machine_content[insert_pos:]
        
        return fixed_content
    
    def _get_full_path(self, filename: str) -> str:
        """Get full path for a filename or relative path."""
        # If filename already includes folder prefix, use it directly
        if '/' in filename:
            full_path = Path(self.project_path) / filename
            if full_path.exists():
                return str(full_path)
            # Extract just the filename and search
            filename = Path(filename).name
        
        # Check each standard folder
        for folder in ['PSrc', 'PSpec', 'PTst']:
            full_path = Path(self.project_path) / folder / filename
            if full_path.exists():
                return str(full_path)
        
        # Default to PSrc
        return str(Path(self.project_path) / 'PSrc' / filename)


def analyze_and_suggest_fix(
    trace_log: str,
    project_path: str,
    project_files: Dict[str, str]
) -> Tuple[TraceAnalysis, Optional[CheckerFix]]:
    """
    Analyze a trace and attempt to generate a fix.
    
    Returns:
        Tuple of (TraceAnalysis, Optional[CheckerFix])
    """
    from .checker_error_parser import PCheckerErrorParser
    
    parser = PCheckerErrorParser()
    analysis = parser.analyze(trace_log, project_files)
    
    fixer = PCheckerErrorFixer(project_path, project_files)
    
    fix = None
    if fixer.can_fix(analysis.error):
        fix = fixer.fix(analysis)
    
    return analysis, fix
