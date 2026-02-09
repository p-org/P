"""Environment validation tools for MCP."""

from typing import Dict, Any, List
from pydantic import BaseModel
import os
import shutil

try:
    from core.compilation import ensure_environment
    HAS_NEW_COMPILATION = True
except ImportError:
    HAS_NEW_COMPILATION = False


class ValidateEnvironmentParams(BaseModel):
    """Parameters for environment validation"""
    pass


def register_env_tools(mcp, with_metadata):
    """Register environment validation tools."""

    @mcp.tool(
        name="validate_environment",
        description="Validate local P toolchain and LLM provider environment."
    )
    def validate_environment(params: ValidateEnvironmentParams) -> Dict[str, Any]:
        issues: List[str] = []
        details: Dict[str, Any] = {}

        if HAS_NEW_COMPILATION:
            env_info = ensure_environment()
            details["p_compiler_path"] = env_info.p_compiler_path
            details["dotnet_path"] = env_info.dotnet_path
            details["toolchain_issues"] = env_info.issues
            if not env_info.is_valid:
                issues.extend(env_info.issues or ["P environment is not valid"])
        else:
            p_path = shutil.which("p")
            dotnet_path = shutil.which("dotnet")
            details["p_compiler_path"] = p_path
            details["dotnet_path"] = dotnet_path
            if not p_path:
                issues.append("P compiler not found in PATH")
            if not dotnet_path:
                issues.append("dotnet not found in PATH")

        provider = None
        if os.environ.get("OPENAI_API_KEY") and os.environ.get("OPENAI_BASE_URL"):
            provider = "snowflake_cortex"
        elif os.environ.get("ANTHROPIC_API_KEY"):
            provider = "anthropic_direct"
        elif os.environ.get("OPENAI_API_KEY"):
            provider = "openai"
        elif os.environ.get("AWS_ACCESS_KEY_ID") and os.environ.get("AWS_SECRET_ACCESS_KEY"):
            provider = "bedrock"

        details["llm_provider_detected"] = provider
        if not provider:
            issues.append("No LLM provider credentials detected in environment")

        payload = {
            "success": len(issues) == 0,
            "issues": issues,
            "details": details,
            "message": "Environment valid" if len(issues) == 0 else "Environment issues detected",
        }
        return with_metadata("validate_environment", payload)

    return validate_environment
