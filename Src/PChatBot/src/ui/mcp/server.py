"""
P ChatBot MCP Server for Cursor IDE Integration.

This MCP server exposes tools for P code generation, compilation, and error fixing.
It uses the Phase 1 service layer for all operations, ensuring consistency with
the Streamlit and CLI interfaces.
"""

from fastmcp import FastMCP
import logging
from typing import Dict, Any, Optional
from pathlib import Path
import os
import sys

# ============================================================================
# PATH SETUP
# ============================================================================

PROJECT_ROOT = Path(__file__).parent.parent.parent.parent
SRC_ROOT = Path(__file__).parent.parent.parent

if str(SRC_ROOT) not in sys.path:
    sys.path.insert(0, str(SRC_ROOT))
if str(PROJECT_ROOT) not in sys.path:
    sys.path.insert(0, str(PROJECT_ROOT))

os.chdir(str(PROJECT_ROOT))

from dotenv import load_dotenv
load_dotenv(PROJECT_ROOT / ".env")

# ============================================================================
# IMPORTS FROM PHASE 1 SERVICE LAYER
# ============================================================================

from core.llm import get_default_provider
from core.services import GenerationService, CompilationService, FixerService
from core.services.base import ResourceLoader
from ui.mcp.contracts import with_metadata as contract_with_metadata

try:
    from core.compilation import ensure_environment
    HAS_NEW_COMPILATION = True
except ImportError:
    HAS_NEW_COMPILATION = False

# ============================================================================
# MCP SERVER SETUP
# ============================================================================

mcp = FastMCP("P-ChatBot")

logging.basicConfig(
    level=logging.INFO,
    format='[%(levelname)s] %(message)s'
)
logger = logging.getLogger(__name__)

# ============================================================================
# SERVICE INITIALIZATION
# ============================================================================

_services: Dict[str, Any] = {}


def get_services() -> Dict[str, Any]:
    """Get or create service instances."""
    if not _services:
        logger.info("Initializing services...")
        
        if HAS_NEW_COMPILATION:
            env_info = ensure_environment()
            if env_info.is_valid:
                logger.info(f"P environment: P={env_info.p_compiler_path}, dotnet={env_info.dotnet_path}")
            else:
                logger.warning(f"P environment issues: {env_info.issues}")
        
        provider = get_default_provider()
        logger.info(f"Using LLM provider: {provider.name}")
        
        resource_loader = ResourceLoader(PROJECT_ROOT / "resources")
        
        _services["llm_provider"] = provider
        _services["generation"] = GenerationService(
            llm_provider=provider,
            resource_loader=resource_loader
        )
        _services["compilation"] = CompilationService(
            llm_provider=provider,
            resource_loader=resource_loader
        )
        _services["fixer"] = FixerService(
            llm_provider=provider,
            resource_loader=resource_loader,
            compilation_service=_services["compilation"]
        )
        _services["resources"] = resource_loader
        
        logger.info("Services initialized")
    
    return _services


# ============================================================================
# METADATA HELPERS
# ============================================================================

def _with_metadata(
    tool_name: str,
    payload: Dict[str, Any],
    token_usage: Optional[Dict[str, Any]] = None,
    provider_name: Optional[str] = None,
    model: Optional[str] = None,
) -> Dict[str, Any]:
    return contract_with_metadata(
        tool_name=tool_name,
        token_usage=token_usage,
        payload=payload,
        provider_name=provider_name,
        model=model,
        provider_resolver=lambda: _services.get("llm_provider"),
    )


# ============================================================================
# TOOL REGISTRATION
# ============================================================================

from ui.mcp.tools.env import register_env_tools
from ui.mcp.tools.generation import register_generation_tools
from ui.mcp.tools.compilation import register_compilation_tools
from ui.mcp.tools.fixing import register_fixing_tools
from ui.mcp.tools.query import register_query_tools
from ui.mcp.tools.rag_tools import register_rag_tools
from ui.mcp.tools.workflows import register_workflow_tools
from ui.mcp.resources import register_resources

register_env_tools(mcp, _with_metadata)
register_generation_tools(mcp, get_services, _with_metadata)
register_compilation_tools(mcp, get_services, _with_metadata)
register_fixing_tools(mcp, get_services, _with_metadata)
register_query_tools(mcp, get_services, _with_metadata)
register_rag_tools(mcp, _with_metadata)
register_workflow_tools(mcp, get_services, _with_metadata)
register_resources(mcp, get_services)


# ============================================================================
# MAIN
# ============================================================================

if __name__ == "__main__":
    logger.info("Starting P-ChatBot MCP Server...")
    logger.info(f"Project root: {PROJECT_ROOT}")
    mcp.run()
