"""
Specialized PChecker Error Fixers

Implements automatic fixes for common PChecker runtime errors.
"""

import re
import logging
from typing import Dict, List, Optional, Tuple
from dataclasses import dataclass
from pathlib import Path

from .checker_error_parser import CheckerError, CheckerErrorCategory, TraceAnalysis

logger = logging.getLogger(__name__)


@dataclass
class CheckerFix:
    """A fix for a PChecker error."""
    file_path: str
    original_code: str
    fixed_code: str
    description: str
    confidence: float  # 0.0 to 1.0
    requires_review: bool = False
    review_notes: Optional[str] = None


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
        Fix unhandled event by adding ignore/defer statement.
        
        This is simpler than null target - we just add an ignore statement
        to the state where the event was received.
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
            return None
        
        # Find the state
        state_name = error.machine_state
        if not state_name:
            # Try to extract from the trace
            return None
        
        # Clean the state name (remove namespace prefix)
        if '.' in state_name:
            state_name = state_name.split('.')[-1]
        
        # Find the state in the code
        state_pattern = rf"(state\s+{state_name}\s*\{{[^}}]*)"
        state_match = re.search(state_pattern, machine_content, re.DOTALL)
        
        if not state_match:
            return None
        
        state_block = state_match.group(1)
        
        # Check if the event is already handled
        if error.event_name in state_block:
            return None
        
        # Add ignore statement
        # Find the last statement in the state block
        lines = state_block.split('\n')
        insert_idx = len(lines) - 1
        
        # Find proper indentation
        indent = "    "
        for line in lines:
            if line.strip().startswith('on ') or line.strip().startswith('ignore '):
                indent = line[:len(line) - len(line.lstrip())]
                break
        
        # Insert ignore statement
        ignore_stmt = f"{indent}ignore {error.event_name};"
        
        # Reconstruct state block
        new_state_block = '\n'.join(lines[:insert_idx]) + '\n' + ignore_stmt + '\n' + lines[insert_idx]
        
        fixed_content = machine_content.replace(state_block, new_state_block)
        
        return CheckerFix(
            file_path=self._get_full_path(machine_file),
            original_code=machine_content,
            fixed_code=fixed_content,
            description=f"Added 'ignore {error.event_name};' to state '{state_name}' in {error.machine_type}",
            confidence=0.8,
            requires_review=True,
            review_notes=f"Event '{error.event_name}' will now be silently ignored in state '{state_name}'. "
                        f"If you need to handle it differently, change 'ignore' to 'defer' or add a proper handler."
        )
    
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
