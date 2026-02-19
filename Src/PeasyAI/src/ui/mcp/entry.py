"""
PeasyAI MCP Server – CLI entry point.

This module is the console_scripts entry point installed by pip::

    peasyai-mcp          # start the MCP server (stdio transport)
    peasyai-mcp init     # create ~/.peasyai/settings.json

Configuration is loaded from ``~/.peasyai/settings.json``
(like ``~/.claude/settings.json``).
"""

from __future__ import annotations

import argparse
import json
import logging
import os
import sys
from pathlib import Path

logger = logging.getLogger(__name__)


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def _ensure_src_on_path() -> None:
    """
    When running from a development checkout (not a pip-installed wheel),
    the ``src/`` directory might not be on ``sys.path``.  Add it so that
    bare imports like ``from core.llm import …`` keep working.
    """
    # In an installed wheel the hatch build already places core/ and ui/
    # as top-level packages, so this is a no-op.
    src_dir = Path(__file__).resolve().parent.parent.parent  # …/src
    project_root = src_dir.parent                            # …/PeasyAI

    for p in (str(src_dir), str(project_root)):
        if p not in sys.path:
            sys.path.insert(0, p)


def _resolve_resources_dir() -> Path:
    """
    Return the path to the ``resources/`` directory.

    * **Development**: ``<project_root>/resources/``
    * **Installed wheel**: ``<site-packages>/peasyai_resources/``
      (force-included by pyproject.toml)
    """
    # Dev checkout — resources/ next to src/
    project_root = Path(__file__).resolve().parent.parent.parent.parent
    dev_resources = project_root / "resources"
    if dev_resources.is_dir():
        return dev_resources

    # Installed wheel — peasyai_resources/ lives next to core/ in site-packages.
    # Walk up from this file (ui/mcp/entry.py → site-packages/) and look there.
    import site
    for sp in (site.getsitepackages() if hasattr(site, "getsitepackages") else []):
        candidate = Path(sp) / "peasyai_resources"
        if candidate.is_dir():
            return candidate

    # Last resort: relative to this file's site-packages root
    site_resources = Path(__file__).resolve().parent.parent.parent.parent / "peasyai_resources"
    if site_resources.is_dir():
        return site_resources

    return dev_resources  # best effort


# ---------------------------------------------------------------------------
# Sub-commands
# ---------------------------------------------------------------------------

def _cmd_init(args: argparse.Namespace) -> int:
    """Create ``~/.peasyai/settings.json`` with a starter template."""
    from core.config import init_settings, SETTINGS_FILE

    if SETTINGS_FILE.exists() and not args.force:
        print(f"Settings file already exists: {SETTINGS_FILE}")
        print("Use --force to overwrite.")
        return 0

    if args.force and SETTINGS_FILE.exists():
        SETTINGS_FILE.unlink()

    path = init_settings()
    print(f"✅ Created {path}")
    print()
    print("Next steps:")
    print(f"  1. Edit {path} with your LLM provider credentials")
    print("  2. Add the MCP server to Cursor or Claude Code (see below)")
    print()
    print("── Cursor (.cursor/mcp.json) ──────────────────────────────────")
    print(json.dumps({
        "mcpServers": {
            "peasyai": {
                "command": "peasyai-mcp",
                "args": [],
            }
        }
    }, indent=2))
    print()
    print("── Claude Code ────────────────────────────────────────────────")
    print("  claude mcp add peasyai -- peasyai-mcp")
    print()
    return 0


def _cmd_serve(args: argparse.Namespace) -> int:
    """Start the PeasyAI MCP server (default action)."""
    _ensure_src_on_path()

    # Set PEASYAI_RESOURCES_DIR so ResourceLoader can find bundled resources
    os.environ.setdefault("PEASYAI_RESOURCES_DIR", str(_resolve_resources_dir()))

    # Load config from ~/.peasyai/settings.json → env vars
    from core.config import apply_settings_to_env
    apply_settings_to_env()

    # Legacy .env fallback
    try:
        from dotenv import load_dotenv
        project_root = Path(__file__).resolve().parent.parent.parent.parent
        load_dotenv(project_root / ".env", override=False)
    except ImportError:
        pass

    from ui.mcp.server import mcp
    logger.info("Starting PeasyAI MCP Server …")
    mcp.run()
    return 0


def _cmd_show_config(args: argparse.Namespace) -> int:
    """Print the effective configuration (with secrets masked)."""
    _ensure_src_on_path()
    from core.config import load_settings, SETTINGS_FILE

    settings = load_settings()
    provider = settings.active_provider_name()
    pc = settings.active_provider_config()

    print(f"Config file : {SETTINGS_FILE}")
    print(f"File exists : {SETTINGS_FILE.exists()}")
    print(f"Provider    : {provider}")
    print(f"Model       : {settings.llm.model or pc.model or '(default)'}")
    print(f"Timeout     : {settings.llm.timeout}s")

    if pc.api_key:
        masked = pc.api_key[:4] + "…" + pc.api_key[-4:] if len(pc.api_key) > 8 else "****"
        print(f"API key     : {masked}")
    else:
        print(f"API key     : (not set)")

    if pc.base_url:
        print(f"Base URL    : {pc.base_url}")
    return 0


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

def main() -> int:
    parser = argparse.ArgumentParser(
        prog="peasyai-mcp",
        description="PeasyAI – MCP server for P language code generation & verification",
    )
    sub = parser.add_subparsers(dest="command")

    # peasyai-mcp  (no sub-command → start server)
    # peasyai-mcp init
    init_p = sub.add_parser("init", help="Create ~/.peasyai/settings.json")
    init_p.add_argument("--force", action="store_true", help="Overwrite existing file")

    # peasyai-mcp config
    sub.add_parser("config", help="Show effective configuration")

    args = parser.parse_args()

    logging.basicConfig(level=logging.INFO, format="[%(levelname)s] %(message)s")

    if args.command == "init":
        return _cmd_init(args)
    elif args.command == "config":
        return _cmd_show_config(args)
    else:
        # Default: start the MCP server
        return _cmd_serve(args)


if __name__ == "__main__":
    raise SystemExit(main())
