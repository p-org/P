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


class ListProjectFilesParams(BaseModel):
    """Parameters for listing project files"""
    project_path: str = Field(..., description="Absolute path to the P project")


class ReadPFileParams(BaseModel):
    """Parameters for reading a P file"""
    file_path: str = Field(..., description="Absolute path to the P file")


def register_query_tools(mcp, get_services, with_metadata):
    """Register query tools."""

    @mcp.tool(
        name="syntax_help",
        description="Get syntax help and examples for P language constructs. Provide a topic like 'state machines', 'events', 'types', 'enums', 'statements', 'specs', 'monitors', 'tests', 'modules', 'send', 'goto', 'raise', 'compiler', or 'errors'. Returns relevant documentation from the P language guides."
    )
    def syntax_help(params: SyntaxHelperParams) -> Dict[str, Any]:
        logger.info(f"[TOOL] syntax_help: {params.topic}")

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
        return with_metadata("syntax_help", payload)

    @mcp.tool(
        name="list_project_files",
        description="List all P files (.p) in a project organized by folder (PSrc, PSpec, PTst). Useful for inspecting the structure of an existing or generated project before reading individual files with read_p_file."
    )
    def list_project_files(params: ListProjectFilesParams) -> Dict[str, Any]:
        logger.info(f"[TOOL] list_project_files: {params.project_path}")

        services = get_services()
        files = services["compilation"].get_project_files(params.project_path)

        organized = {"PSrc": [], "PSpec": [], "PTst": []}
        for filepath in files.keys():
            folder = filepath.split("/")[0] if "/" in filepath else "other"
            if folder in organized:
                organized[folder].append(filepath)

        payload = {
            "project_path": params.project_path,
            "files": organized,
            "total_files": len(files)
        }
        return with_metadata("list_project_files", payload)

    @mcp.tool(
        name="read_p_file",
        description="Read the full contents of a P file. Use this to inspect generated code, review existing machines/specs/tests, or gather context_files content to pass into generate_machine, generate_spec, or generate_test."
    )
    def read_p_file(params: ReadPFileParams) -> Dict[str, Any]:
        logger.info(f"[TOOL] read_p_file: {params.file_path}")

        services = get_services()
        content = services["compilation"].read_file(params.file_path)

        if content is not None:
            payload = {
                "success": True,
                "file_path": params.file_path,
                "content": content,
                "lines": len(content.splitlines())
            }
            return with_metadata("read_p_file", payload)

        payload = {
            "success": False,
            "error": f"Could not read file: {params.file_path}"
        }
        return with_metadata("read_p_file", payload)

    return {
        "syntax_help": syntax_help,
        "list_project_files": list_project_files,
        "read_p_file": read_p_file,
    }
