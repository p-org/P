"""
Tests for core/config.py — PeasyAI configuration loader.

Covers: load_settings, apply_settings_to_env, to_env_vars, active_provider_name,
        active_provider_config, init_settings, malformed config handling.
"""

import json
import os
import sys
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

PROJECT_ROOT = Path(__file__).parent.parent
sys.path.insert(0, str(PROJECT_ROOT / "src"))

from core.config import (
    PeasyAISettings,
    LLMConfig,
    ProviderConfig,
    PCompilerConfig,
    GenerationConfig,
    load_settings,
    apply_settings_to_env,
    init_settings,
    _parse_provider,
)


class TestPeasyAISettingsDefaults(unittest.TestCase):
    """Test default values for settings dataclasses."""

    def test_default_settings(self):
        s = PeasyAISettings()
        self.assertEqual(s.llm.provider, "bedrock")
        self.assertIsNone(s.llm.model)
        self.assertEqual(s.llm.timeout, 600)
        self.assertEqual(s.generation.ensemble_size, 3)
        self.assertEqual(s.generation.output_dir, "./PGenerated")
        self.assertIsNone(s.p_compiler.path)

    def test_provider_config_defaults(self):
        pc = ProviderConfig()
        self.assertIsNone(pc.api_key)
        self.assertIsNone(pc.base_url)
        self.assertIsNone(pc.model)
        self.assertEqual(pc.timeout, 600)


class TestActiveProvider(unittest.TestCase):
    """Test active_provider_name and active_provider_config."""

    def test_default_provider_name(self):
        s = PeasyAISettings()
        with patch.dict(os.environ, {}, clear=True):
            os.environ.pop("LLM_PROVIDER", None)
            self.assertEqual(s.active_provider_name(), "bedrock")

    def test_env_var_overrides_provider(self):
        s = PeasyAISettings()
        with patch.dict(os.environ, {"LLM_PROVIDER": "anthropic"}):
            self.assertEqual(s.active_provider_name(), "anthropic")

    def test_alias_normalization(self):
        s = PeasyAISettings(
            llm=LLMConfig(
                provider="snowflake_cortex",
                providers={"snowflake": ProviderConfig(api_key="key123")},
            )
        )
        with patch.dict(os.environ, {}, clear=True):
            os.environ.pop("LLM_PROVIDER", None)
            self.assertEqual(s.active_provider_name(), "snowflake_cortex")
            pc = s.active_provider_config()
            self.assertEqual(pc.api_key, "key123")

    def test_unknown_provider_returns_empty_config(self):
        s = PeasyAISettings(llm=LLMConfig(provider="unknown"))
        with patch.dict(os.environ, {}, clear=True):
            os.environ.pop("LLM_PROVIDER", None)
            pc = s.active_provider_config()
            self.assertIsNone(pc.api_key)


class TestToEnvVars(unittest.TestCase):
    """Test to_env_vars for each provider type."""

    def test_snowflake_env_vars(self):
        s = PeasyAISettings(
            llm=LLMConfig(
                provider="snowflake",
                model="claude-sonnet-4-5",
                providers={"snowflake": ProviderConfig(
                    api_key="sk-test",
                    base_url="https://acct.snowflakecomputing.com/api",
                )},
            )
        )
        with patch.dict(os.environ, {}, clear=True):
            os.environ.pop("LLM_PROVIDER", None)
            env = s.to_env_vars()
        self.assertEqual(env["OPENAI_API_KEY"], "sk-test")
        self.assertEqual(env["OPENAI_BASE_URL"], "https://acct.snowflakecomputing.com/api")
        self.assertEqual(env["OPENAI_MODEL_NAME"], "claude-sonnet-4-5")
        self.assertEqual(env["LLM_PROVIDER"], "snowflake")

    def test_anthropic_env_vars(self):
        s = PeasyAISettings(
            llm=LLMConfig(
                provider="anthropic",
                providers={"anthropic": ProviderConfig(api_key="ant-key")},
            )
        )
        with patch.dict(os.environ, {}, clear=True):
            os.environ.pop("LLM_PROVIDER", None)
            env = s.to_env_vars()
        self.assertEqual(env["ANTHROPIC_API_KEY"], "ant-key")
        self.assertEqual(env["ANTHROPIC_MODEL_NAME"], "claude-3-5-sonnet-20241022")

    def test_bedrock_env_vars(self):
        s = PeasyAISettings(
            llm=LLMConfig(
                provider="bedrock",
                providers={"bedrock": ProviderConfig(region="us-east-1")},
            )
        )
        with patch.dict(os.environ, {}, clear=True):
            os.environ.pop("LLM_PROVIDER", None)
            env = s.to_env_vars()
        self.assertEqual(env["AWS_REGION"], "us-east-1")
        self.assertEqual(env["BEDROCK_MODEL_ID"], "anthropic.claude-3-5-sonnet-20241022-v2:0")

    def test_model_precedence_llm_level_over_provider(self):
        """llm.model should take priority over providers.snowflake.model."""
        s = PeasyAISettings(
            llm=LLMConfig(
                provider="snowflake",
                model="top-level-model",
                providers={"snowflake": ProviderConfig(
                    api_key="k", base_url="https://x.com", model="provider-model",
                )},
            )
        )
        with patch.dict(os.environ, {}, clear=True):
            os.environ.pop("LLM_PROVIDER", None)
            env = s.to_env_vars()
        self.assertEqual(env["OPENAI_MODEL_NAME"], "top-level-model")


class TestLoadSettings(unittest.TestCase):
    """Test load_settings with temp files (auto-cleaned)."""

    def _write_temp(self, content: str) -> Path:
        """Write content to a temp file and return its Path."""
        fd, path = tempfile.mkstemp(suffix=".json", prefix="peasyai_test_")
        os.write(fd, content.encode())
        os.close(fd)
        self._tmp_files.append(path)
        return Path(path)

    def setUp(self):
        self._tmp_files: list = []

    def tearDown(self):
        for p in self._tmp_files:
            try:
                os.unlink(p)
            except OSError:
                pass

    def test_missing_file_returns_defaults(self):
        s = load_settings(Path("/tmp/nonexistent_peasyai_test_xyz.json"))
        self.assertEqual(s.llm.provider, "bedrock")

    def test_valid_settings_file(self):
        data = {
            "llm": {
                "provider": "snowflake",
                "model": "test-model",
                "timeout": 300,
                "providers": {
                    "snowflake": {
                        "api_key": "test-key",
                        "base_url": "https://test.snowflakecomputing.com",
                    }
                },
            },
            "p_compiler": {"path": "/usr/local/bin/p"},
            "generation": {"ensemble_size": 5, "output_dir": "/tmp/gen"},
        }
        tmp = self._write_temp(json.dumps(data))
        s = load_settings(tmp)
        self.assertEqual(s.llm.provider, "snowflake")
        self.assertEqual(s.llm.model, "test-model")
        self.assertEqual(s.llm.timeout, 300)
        self.assertEqual(s.llm.providers["snowflake"].api_key, "test-key")
        self.assertEqual(s.p_compiler.path, "/usr/local/bin/p")
        self.assertEqual(s.generation.ensemble_size, 5)
        self.assertEqual(s.generation.output_dir, "/tmp/gen")

    def test_malformed_json_returns_defaults(self):
        tmp = self._write_temp("{ this is not valid json !!!")
        s = load_settings(tmp)
        self.assertEqual(s.llm.provider, "bedrock")

    def test_empty_json_returns_defaults(self):
        tmp = self._write_temp("{}")
        s = load_settings(tmp)
        self.assertEqual(s.llm.provider, "bedrock")
        self.assertEqual(s.generation.ensemble_size, 3)

    def test_partial_config_fills_defaults(self):
        data = {"llm": {"provider": "anthropic"}}
        tmp = self._write_temp(json.dumps(data))
        s = load_settings(tmp)
        self.assertEqual(s.llm.provider, "anthropic")
        self.assertEqual(s.llm.timeout, 600)
        self.assertEqual(s.generation.ensemble_size, 3)


class TestApplySettingsToEnv(unittest.TestCase):
    """Test apply_settings_to_env."""

    @patch.dict(os.environ, {}, clear=True)
    def test_sets_env_vars(self):
        s = PeasyAISettings(
            llm=LLMConfig(
                provider="snowflake",
                providers={"snowflake": ProviderConfig(
                    api_key="apply-test-key",
                    base_url="https://apply.snowflakecomputing.com",
                )},
            )
        )
        apply_settings_to_env(s)
        self.assertEqual(os.environ.get("OPENAI_API_KEY"), "apply-test-key")
        self.assertEqual(os.environ.get("LLM_PROVIDER"), "snowflake")

    @patch.dict(os.environ, {"OPENAI_API_KEY": "existing-key"}, clear=False)
    def test_existing_env_not_overwritten(self):
        s = PeasyAISettings(
            llm=LLMConfig(
                provider="snowflake",
                providers={"snowflake": ProviderConfig(api_key="new-key", base_url="https://x.com")},
            )
        )
        apply_settings_to_env(s)
        self.assertEqual(os.environ["OPENAI_API_KEY"], "existing-key")


class TestParseProvider(unittest.TestCase):
    """Test _parse_provider helper."""

    def test_full_config(self):
        raw = {
            "api_key": "k",
            "base_url": "https://x.com",
            "model": "m",
            "model_id": "mid",
            "region": "us-west-2",
            "timeout": 120,
        }
        pc = _parse_provider(raw)
        self.assertEqual(pc.api_key, "k")
        self.assertEqual(pc.base_url, "https://x.com")
        self.assertEqual(pc.model, "m")
        self.assertEqual(pc.model_id, "mid")
        self.assertEqual(pc.region, "us-west-2")
        self.assertEqual(pc.timeout, 120)

    def test_empty_config(self):
        pc = _parse_provider({})
        self.assertIsNone(pc.api_key)
        self.assertEqual(pc.timeout, 600)


if __name__ == "__main__":
    unittest.main()
