import sys
import unittest
from pathlib import Path


PROJECT_ROOT = Path(__file__).parent.parent
SRC_ROOT = PROJECT_ROOT / "src"
sys.path.insert(0, str(SRC_ROOT))

from core.llm.snowflake import SnowflakeCortexProvider


class TestSnowflakeModelSelection(unittest.TestCase):
    def test_uses_fixed_default_model(self):
        provider = SnowflakeCortexProvider(
            {
                "api_key": "test-key",
                "base_url": "https://snowflake.invalid/v1",
            }
        )
        self.assertEqual(provider.default_model, "claude-sonnet-4-5")

    def test_explicit_config_model_overrides_default(self):
        provider = SnowflakeCortexProvider(
            {
                "api_key": "test-key",
                "base_url": "https://snowflake.invalid/v1",
                "model": "claude-opus-4-5",
            }
        )
        self.assertEqual(provider.default_model, "claude-opus-4-5")


if __name__ == "__main__":
    unittest.main()
