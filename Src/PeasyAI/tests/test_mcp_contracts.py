import sys
import unittest
from pathlib import Path
from types import SimpleNamespace


PROJECT_ROOT = Path(__file__).parent.parent
SRC_ROOT = PROJECT_ROOT / "src"
sys.path.insert(0, str(SRC_ROOT))
sys.path.insert(0, str(PROJECT_ROOT))

from ui.mcp.contracts import with_metadata
from ui.mcp.tools.generation import register_generation_tools, GenerateTypesEventsParams
from ui.mcp.tools.compilation import register_compilation_tools, PCompileParams


class DummyMCP:
    def tool(self, *args, **kwargs):
        def decorator(fn):
            return fn
        return decorator


def _with_metadata(tool_name, payload, token_usage=None, provider_name=None, model=None):
    return with_metadata(
        tool_name=tool_name,
        payload=payload,
        token_usage=token_usage,
        provider_name=provider_name,
        model=model,
    )


class TestMCPContracts(unittest.TestCase):
    def test_with_metadata_success_defaults(self):
        payload = {"success": True, "message": "ok"}
        response = _with_metadata("demo_tool", payload)

        self.assertEqual(response["api_version"], "1.0")
        self.assertIsNone(response["error_category"])
        self.assertFalse(response["retryable"])
        self.assertEqual(response["next_actions"], [])
        self.assertIn("metadata", response)
        self.assertEqual(response["metadata"]["tool"], "demo_tool")

    def test_with_metadata_failure_defaults(self):
        payload = {"success": False, "error": "boom"}
        response = _with_metadata("demo_tool", payload)

        self.assertEqual(response["api_version"], "1.0")
        self.assertEqual(response["error_category"], "internal")
        self.assertTrue(response["retryable"])
        self.assertGreater(len(response["next_actions"]), 0)
        self.assertEqual(response["metadata"]["tool"], "demo_tool")

    def test_generation_tool_contract_shape(self):
        mcp = DummyMCP()

        mock_result = SimpleNamespace(
            success=True,
            filename="Enums_Types_Events.p",
            file_path="/tmp/Project/PSrc/Enums_Types_Events.p",
            code="event eTest;",
            error=None,
            token_usage={"inputTokens": 10, "outputTokens": 20},
        )
        generation = SimpleNamespace(generate_types_events=lambda **_: mock_result)

        get_services = lambda: {"generation": generation}
        tools = register_generation_tools(mcp, get_services, _with_metadata)

        response = tools["generate_types_events"](
            GenerateTypesEventsParams(
                design_doc="# T\n## Components\n## Interactions\n",
                project_path="/tmp/Project",
            )
        )

        self.assertTrue(response["success"])
        self.assertTrue(response["preview_only"])
        self.assertEqual(response["api_version"], "1.0")
        self.assertIsNone(response["error_category"])
        self.assertFalse(response["retryable"])
        self.assertIn("metadata", response)


    def test_compilation_tool_contract_shape(self):
        mcp = DummyMCP()
        compile_result = SimpleNamespace(
            success=True,
            stdout="Build succeeded",
            stderr="",
            return_code=0,
            error=None,
        )
        compilation = SimpleNamespace(compile=lambda _: compile_result)

        get_services = lambda: {"compilation": compilation}
        tools = register_compilation_tools(mcp, get_services, _with_metadata)

        response = tools["p_compile"](PCompileParams(path="/tmp/Project"))

        self.assertTrue(response["success"])
        self.assertEqual(response["api_version"], "1.0")
        self.assertFalse(response["retryable"])
        self.assertEqual(response["metadata"]["tool"], "peasy-ai-compile")


if __name__ == "__main__":
    unittest.main()
