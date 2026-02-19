"""
PeasyAI Configuration Loader.

Loads configuration from ~/.peasyai/settings.json (like ~/.claude/settings.json).

Resolution order (highest priority first):
  1. Environment variables (override anything in the file)
  2. ~/.peasyai/settings.json

The config file is the single source of truth for LLM provider credentials,
model selection, compiler paths, and generation defaults.
"""

import json
import logging
import os
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, Dict, Optional

logger = logging.getLogger(__name__)

# ---------------------------------------------------------------------------
# Paths
# ---------------------------------------------------------------------------

PEASYAI_HOME = Path.home() / ".peasyai"
SETTINGS_FILE = PEASYAI_HOME / "settings.json"

# ---------------------------------------------------------------------------
# Data classes
# ---------------------------------------------------------------------------


@dataclass
class ProviderConfig:
    """Configuration for a single LLM provider."""
    api_key: Optional[str] = None
    base_url: Optional[str] = None
    model: Optional[str] = None
    model_id: Optional[str] = None
    region: Optional[str] = None
    timeout: float = 600


@dataclass
class LLMConfig:
    """Top-level LLM configuration."""
    provider: str = "bedrock"
    model: Optional[str] = None
    timeout: float = 600
    providers: Dict[str, ProviderConfig] = field(default_factory=dict)


@dataclass
class PCompilerConfig:
    """P compiler configuration."""
    path: Optional[str] = None
    dotnet_path: Optional[str] = None


@dataclass
class GenerationConfig:
    """Code generation defaults."""
    ensemble_size: int = 3
    output_dir: str = "./PGenerated"


@dataclass
class PeasyAISettings:
    """Root settings object loaded from ~/.peasyai/settings.json."""
    llm: LLMConfig = field(default_factory=LLMConfig)
    p_compiler: PCompilerConfig = field(default_factory=PCompilerConfig)
    generation: GenerationConfig = field(default_factory=GenerationConfig)

    # ── convenience helpers ──────────────────────────────────────────

    def active_provider_name(self) -> str:
        """Return the provider name that should be used."""
        # Env var override
        explicit = os.environ.get("LLM_PROVIDER", "").lower()
        if explicit:
            return explicit
        return self.llm.provider

    def active_provider_config(self) -> ProviderConfig:
        """Return the ProviderConfig for the active provider."""
        name = self.active_provider_name()
        # Normalise aliases
        canonical = {
            "snowflake_cortex": "snowflake",
            "aws_bedrock": "bedrock",
            "anthropic_direct": "anthropic",
        }.get(name, name)
        return self.llm.providers.get(canonical, ProviderConfig())

    def to_env_vars(self) -> Dict[str, str]:
        """
        Export the current config as environment variables.

        This bridges the gap: existing code reads env vars, so we populate
        them from the settings file.  Environment vars already set by the
        user take precedence (they are NOT overwritten).
        """
        env: Dict[str, str] = {}
        name = self.active_provider_name()
        pc = self.active_provider_config()

        if name in ("snowflake", "snowflake_cortex"):
            if pc.api_key:
                env["OPENAI_API_KEY"] = pc.api_key
            if pc.base_url:
                env["OPENAI_BASE_URL"] = pc.base_url
            model = self.llm.model or pc.model or "claude-sonnet-4-5"
            env["OPENAI_MODEL_NAME"] = model

        elif name in ("anthropic", "anthropic_direct"):
            if pc.api_key:
                env["ANTHROPIC_API_KEY"] = pc.api_key
            if pc.base_url:
                env["ANTHROPIC_BASE_URL"] = pc.base_url
            model = self.llm.model or pc.model or "claude-3-5-sonnet-20241022"
            env["ANTHROPIC_MODEL_NAME"] = model

        elif name in ("bedrock", "aws_bedrock"):
            if pc.region:
                env["AWS_REGION"] = pc.region
            model_id = pc.model_id or pc.model or "anthropic.claude-3-5-sonnet-20241022-v2:0"
            env["BEDROCK_MODEL_ID"] = model_id

        env["LLM_PROVIDER"] = name
        env["LLM_TIMEOUT"] = str(self.llm.timeout or pc.timeout or 600)

        return env


# ---------------------------------------------------------------------------
# Loader
# ---------------------------------------------------------------------------

def _parse_provider(raw: Dict[str, Any]) -> ProviderConfig:
    return ProviderConfig(
        api_key=raw.get("api_key"),
        base_url=raw.get("base_url"),
        model=raw.get("model"),
        model_id=raw.get("model_id"),
        region=raw.get("region"),
        timeout=float(raw.get("timeout", 600)),
    )


def load_settings(path: Optional[Path] = None) -> PeasyAISettings:
    """
    Load settings from ``~/.peasyai/settings.json``.

    If the file does not exist, returns defaults and logs a warning.
    """
    path = path or SETTINGS_FILE

    if not path.exists():
        logger.warning(
            "No settings file found at %s — using defaults / env vars. "
            "Run  peasyai-mcp init  or create the file manually.",
            path,
        )
        return PeasyAISettings()

    try:
        raw = json.loads(path.read_text(encoding="utf-8"))
    except (json.JSONDecodeError, OSError) as exc:
        logger.error("Failed to parse %s: %s — falling back to defaults", path, exc)
        return PeasyAISettings()

    logger.info("Loaded PeasyAI settings from %s", path)

    # ── LLM section ──────────────────────────────────────────────────
    llm_raw = raw.get("llm", {})
    providers: Dict[str, ProviderConfig] = {}
    for pname, pdata in llm_raw.get("providers", {}).items():
        providers[pname] = _parse_provider(pdata)

    llm = LLMConfig(
        provider=llm_raw.get("provider", "bedrock"),
        model=llm_raw.get("model"),
        timeout=float(llm_raw.get("timeout", 600)),
        providers=providers,
    )

    # ── P compiler section ───────────────────────────────────────────
    pc_raw = raw.get("p_compiler", {})
    p_compiler = PCompilerConfig(
        path=pc_raw.get("path"),
        dotnet_path=pc_raw.get("dotnet_path"),
    )

    # ── Generation section ───────────────────────────────────────────
    gen_raw = raw.get("generation", {})
    generation = GenerationConfig(
        ensemble_size=int(gen_raw.get("ensemble_size", 3)),
        output_dir=gen_raw.get("output_dir", "./PGenerated"),
    )

    return PeasyAISettings(llm=llm, p_compiler=p_compiler, generation=generation)


def apply_settings_to_env(settings: Optional[PeasyAISettings] = None) -> PeasyAISettings:
    """
    Load settings and export them as environment variables.

    Already-set env vars are NOT overwritten, so users can still
    override individual values via the shell environment.
    """
    settings = settings or load_settings()

    for key, value in settings.to_env_vars().items():
        if key not in os.environ or not os.environ[key]:
            os.environ[key] = value
            logger.debug("Set %s from ~/.peasyai/settings.json", key)
        else:
            logger.debug("Keeping existing env var %s", key)

    return settings


# ---------------------------------------------------------------------------
# CLI helpers
# ---------------------------------------------------------------------------

def init_settings() -> Path:
    """
    Create ``~/.peasyai/settings.json`` with a starter template.

    Returns the path to the created file.
    """
    PEASYAI_HOME.mkdir(parents=True, exist_ok=True)

    template = {
        "$schema": "https://raw.githubusercontent.com/p-org/P/main/Src/PeasyAI/.peasyai-schema.json",
        "llm": {
            "provider": "snowflake",
            "model": "claude-sonnet-4-5",
            "timeout": 600,
            "providers": {
                "snowflake": {
                    "api_key": "",
                    "base_url": "https://your-account.snowflakecomputing.com/api/v2/cortex/openai",
                },
                "anthropic": {
                    "api_key": "",
                    "model": "claude-3-5-sonnet-20241022",
                },
                "bedrock": {
                    "region": "us-west-2",
                    "model_id": "anthropic.claude-3-5-sonnet-20241022-v2:0",
                },
            },
        },
        "p_compiler": {
            "path": None,
            "dotnet_path": None,
        },
        "generation": {
            "ensemble_size": 3,
            "output_dir": "./PGenerated",
        },
    }

    if SETTINGS_FILE.exists():
        logger.info("Settings file already exists at %s", SETTINGS_FILE)
        return SETTINGS_FILE

    SETTINGS_FILE.write_text(
        json.dumps(template, indent=2) + "\n",
        encoding="utf-8",
    )
    logger.info("Created starter settings at %s", SETTINGS_FILE)
    return SETTINGS_FILE

