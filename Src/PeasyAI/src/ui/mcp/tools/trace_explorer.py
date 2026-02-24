"""Interactive trace exploration tools for P development."""

from typing import Dict, Any, List, Optional
from pydantic import BaseModel, Field
import re
from pathlib import Path

class ExploreTraceParams(BaseModel):
    """Parameters for trace exploration"""
    trace_file: str = Field(..., description="Path to PChecker trace file")
    step: Optional[int] = Field(None, description="Specific step to examine")

class StepThroughTraceParams(BaseModel):
    """Parameters for stepping through trace"""
    trace_file: str = Field(..., description="Path to trace file")
    direction: str = Field("forward", description="Direction: 'forward' or 'backward'")
    current_step: int = Field(0, description="Current step position")

def register_trace_tools(mcp, get_services, with_metadata):
    """Register trace exploration tools."""

    @mcp.tool(
        name="peasy-ai-explore-trace",
        description="Interactively explore a PChecker trace to understand execution flow"
    )
    def explore_trace(params: ExploreTraceParams) -> Dict[str, Any]:
        """Parse and present trace in an explorable format."""

        trace_content = Path(params.trace_file).read_text()

        # Parse trace into structured format
        steps = []
        current_step = {}

        for line in trace_content.split('\n'):
            if '<CreateLog>' in line:
                if current_step:
                    steps.append(current_step)
                current_step = {'type': 'create', 'raw': line}
                # Extract machine creation info
                match = re.search(r'Created Machine (\w+)-(\d+) of type (\w+)', line)
                if match:
                    current_step['machine_id'] = f"{match.group(1)}-{match.group(2)}"
                    current_step['machine_type'] = match.group(3)

            elif '<StateLog>' in line:
                if current_step:
                    steps.append(current_step)
                current_step = {'type': 'state', 'raw': line}
                # Extract state transition
                match = re.search(r'Machine (\S+)-.+ entering State (\w+)', line)
                if match:
                    current_step['machine'] = match.group(1)
                    current_step['state'] = match.group(2)

            elif '<SendLog>' in line:
                if current_step:
                    steps.append(current_step)
                current_step = {'type': 'send', 'raw': line}
                # Extract send event info
                match = re.search(r'sent event (\w+) to (\S+)', line)
                if match:
                    current_step['event'] = match.group(1)
                    current_step['target'] = match.group(2)

        if current_step:
            steps.append(current_step)

        # If specific step requested
        if params.step is not None:
            if 0 <= params.step < len(steps):
                return with_metadata("peasy-ai-explore-trace", {
                    "success": True,
                    "total_steps": len(steps),
                    "current_step": params.step,
                    "step_detail": steps[params.step],
                    "next_steps": steps[params.step+1:params.step+4] if params.step < len(steps)-1 else [],
                    "previous_steps": steps[max(0, params.step-3):params.step] if params.step > 0 else []
                })

        # Return overview
        return with_metadata("peasy-ai-explore-trace", {
            "success": True,
            "total_steps": len(steps),
            "summary": {
                "machines_created": len([s for s in steps if s['type'] == 'create']),
                "state_transitions": len([s for s in steps if s['type'] == 'state']),
                "events_sent": len([s for s in steps if s['type'] == 'send']),
            },
            "first_10_steps": steps[:10],
            "error_step": next((i for i, s in enumerate(steps) if 'error' in s.get('raw', '').lower()), None)
        })

    @mcp.tool(
        name="peasy-ai-query-trace",
        description="Query the state of machines at a specific point in the trace"
    )
    def query_trace_state(params: Dict[str, Any]) -> Dict[str, Any]:
        """Get machine states at a specific trace step."""

        trace_file = params["trace_file"]
        target_step = params["step"]

        # Build state up to target step
        machine_states = {}

        trace_content = Path(trace_file).read_text()
        current_step = 0

        for line in trace_content.split('\n'):
            if current_step > target_step:
                break

            if '<StateLog>' in line:
                match = re.search(r'Machine (\S+) entering State (\w+)', line)
                if match:
                    machine_states[match.group(1)] = {
                        'current_state': match.group(2),
                        'step_entered': current_step
                    }
                current_step += 1

        return with_metadata("peasy-ai-query-trace", {
            "success": True,
            "step": target_step,
            "machine_states": machine_states,
            "active_machines": list(machine_states.keys())
        })

    return {
        "explore_trace": explore_trace,
        "query_trace_state": query_trace_state
    }