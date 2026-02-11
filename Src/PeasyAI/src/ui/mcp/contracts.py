"""Shared response contract helpers for MCP tools."""

from typing import Dict, Any, Optional, Callable
import uuid
from datetime import datetime, timezone


def tool_metadata(
    tool_name: str,
    token_usage: Optional[Dict[str, Any]] = None,
    provider_name: Optional[str] = None,
    model: Optional[str] = None,
) -> Dict[str, Any]:
    return {
        "tool": tool_name,
        "operation_id": str(uuid.uuid4()),
        "timestamp": datetime.now(timezone.utc).isoformat().replace("+00:00", "Z"),
        "provider": provider_name,
        "model": model,
        "token_usage": token_usage or {},
    }


def with_metadata(
    tool_name: str,
    payload: Dict[str, Any],
    token_usage: Optional[Dict[str, Any]] = None,
    provider_name: Optional[str] = None,
    model: Optional[str] = None,
    provider_resolver: Optional[Callable[[], Any]] = None,
) -> Dict[str, Any]:
    # Standard MCP envelope fields used by Cursor/agents.
    payload.setdefault("api_version", "1.0")
    success = bool(payload.get("success", True))
    payload.setdefault("error_category", None if success else "internal")
    payload.setdefault("retryable", not success)
    payload.setdefault(
        "next_actions",
        [] if success else ["Retry the operation or inspect the error details."]
    )

    if provider_resolver:
        provider_obj = provider_resolver()
        if provider_obj:
            provider_name = provider_name or getattr(provider_obj, "name", None)
            model = model or getattr(provider_obj, "default_model", None)

    payload["metadata"] = tool_metadata(
        tool_name=tool_name,
        token_usage=token_usage,
        provider_name=provider_name,
        model=model,
    )
    return payload
