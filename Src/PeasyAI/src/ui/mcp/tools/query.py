"""Query tools for MCP."""

from typing import Dict, Any
from pydantic import BaseModel, Field
import logging

logger = logging.getLogger(__name__)


class SyntaxHelperParams(BaseModel):
    """Parameters for syntax help"""
    topic: str = Field(
        ...,
        description="The P language topic to get help on (e.g., 'state machines', 'events', 'types', 'send', 'goto')"
    )


def register_query_tools(mcp, get_services, with_metadata):
    """Register query tools."""

    @mcp.tool(
        name="peasy-ai-syntax-help",
        description="Get syntax help and examples for P language constructs. Provide a topic like 'state machines', 'events', 'types', 'enums', 'statements', 'specs', 'monitors', 'tests', 'modules', 'send', 'goto', 'raise', 'compiler', or 'errors'. Returns relevant documentation from the P language guides."
    )
    def syntax_help(params: SyntaxHelperParams) -> Dict[str, Any]:
        logger.info(f"[TOOL] peasy-ai-syntax-help: {params.topic}")

        services = get_services()
        resources = services["resources"]

        topic_lower = params.topic.lower()

        topic_files = {
            "machine": "modular/p_machines_guide.txt",
            "state": "modular/p_machines_guide.txt",
            "event": "modular/p_events_guide.txt",
            "type": "modular/p_types_guide.txt",
            "enum": "modular/p_enums_guide.txt",
            "statement": "modular/p_statements_guide.txt",
            "spec": "modular/p_spec_monitors_guide.txt",
            "monitor": "modular/p_spec_monitors_guide.txt",
            "test": "modular/p_test_cases_guide.txt",
            "module": "modular/p_module_system_guide.txt",
            "syntax": "P_syntax_guide.txt",
            "basic": "modular/p_basics.txt",
            "example": "modular/p_program_example.txt",
            "compiler": "modular/p_compiler_guide.txt",
            "error": "modular/p_common_compilation_errors.txt",
            "send": "modular/p_statements_guide.txt",
            "goto": "modular/p_machines_guide.txt",
            "raise": "modular/p_statements_guide.txt",
        }

        matching_files = []
        for keyword, filepath in topic_files.items():
            if keyword in topic_lower and filepath not in matching_files:
                matching_files.append(filepath)

        if not matching_files:
            matching_files = ["P_syntax_guide.txt", "modular/p_basics.txt"]

        content_parts = []
        for filepath in matching_files[:3]:
            try:
                content = resources.load(f"context_files/{filepath}")
                content_parts.append(f"=== {filepath} ===\n{content}")
            except Exception as e:
                logger.warning(f"Could not load {filepath}: {e}")

        payload = {
            "topic": params.topic,
            "content": "\n\n".join(content_parts),
            "files_referenced": matching_files[:3]
        }
        return with_metadata("peasy-ai-syntax-help", payload)

    return {
        "syntax_help": syntax_help,
    }
