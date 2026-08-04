import sys
import unittest
from pathlib import Path
from unittest.mock import MagicMock, patch

# Mock class definitions that mimic real google.genai classes for reliable tests
class MockPart:
    def __init__(self, text=None):
        self.text = text
    @classmethod
    def from_text(cls, text):
        return cls(text=text)

class MockContent:
    def __init__(self, role=None, parts=None):
        self.role = role
        self.parts = parts or []

class MockGenerateContentConfig:
    def __init__(self, temperature=None, top_p=None, max_output_tokens=None, system_instruction=None):
        self.temperature = temperature
        self.top_p = top_p
        self.max_output_tokens = max_output_tokens
        self.system_instruction = system_instruction

# Set up mock module structures
mock_genai = MagicMock()
mock_types = MagicMock()
mock_types.Part = MockPart
mock_types.Content = MockContent
mock_types.GenerateContentConfig = MockGenerateContentConfig
mock_genai.types = mock_types

sys.modules["google.genai"] = mock_genai
sys.modules["google.genai.types"] = mock_types

try:
    import google
    google.genai = mock_genai
except ImportError:
    mock_google = MagicMock()
    mock_google.genai = mock_genai
    sys.modules["google"] = mock_google

# Mock google.api_core and exceptions
class Unauthenticated(Exception): pass
class PermissionDenied(Exception): pass
class ResourceExhausted(Exception): pass
class NotFound(Exception): pass
class DeadlineExceeded(Exception): pass

mock_exceptions = MagicMock()
mock_exceptions.Unauthenticated = Unauthenticated
mock_exceptions.PermissionDenied = PermissionDenied
mock_exceptions.ResourceExhausted = ResourceExhausted
mock_exceptions.NotFound = NotFound
mock_exceptions.DeadlineExceeded = DeadlineExceeded

# Set up mock google.api_core module with correct attributes
mock_api_core = MagicMock()
mock_api_core.exceptions = mock_exceptions
sys.modules["google.api_core"] = mock_api_core
sys.modules["google.api_core.exceptions"] = mock_exceptions
PROJECT_ROOT = Path(__file__).parent.parent
SRC_ROOT = PROJECT_ROOT / "src"
sys.path.insert(0, str(SRC_ROOT))

from core.llm.base import Message, MessageRole, LLMConfig, ProviderError, AuthenticationError
from core.llm.gemini import GeminiProvider


class TestGeminiProvider(unittest.TestCase):
    def test_default_model_selection(self):
        provider = GeminiProvider({"api_key": "test-key"})
        self.assertEqual(provider.default_model, "gemini-3.6-flash")
        self.assertEqual(provider.name, "gemini")
        self.assertIn("gemini-3.6-flash", provider.available_models())

    def test_explicit_config_model_overrides_default(self):
        provider = GeminiProvider({"api_key": "test-key", "model": "gemini-3.1-pro-preview"})
        self.assertEqual(provider.default_model, "gemini-3.1-pro-preview")

    def test_missing_api_key_raises_error(self):
        with self.assertRaises(ValueError):
            GeminiProvider({})

    @patch("google.genai.Client")
    def test_complete_success(self, mock_client_class):
        # Setup mock behavior
        mock_response = MagicMock()
        mock_response.text = "Generated P code"
        
        # Setup mock usage_metadata
        mock_usage = MagicMock()
        mock_usage.prompt_token_count = 10
        mock_usage.candidates_token_count = 20
        mock_usage.total_token_count = 30
        mock_response.usage_metadata = mock_usage
        
        # Setup mock candidates for finish_reason
        mock_candidate = MagicMock()
        mock_candidate.finish_reason = MagicMock()
        mock_candidate.finish_reason.name = "STOP"
        mock_response.candidates = [mock_candidate]
        
        mock_client_instance = MagicMock()
        mock_client_instance.models.generate_content.return_value = mock_response
        mock_client_class.return_value = mock_client_instance

        # Instantiate provider
        provider = GeminiProvider({"api_key": "test-key"})
        
        messages = [
            Message(role=MessageRole.USER, content="Hello"),
            Message(role=MessageRole.ASSISTANT, content="Hi"),
            Message(role=MessageRole.SYSTEM, content="This is context")
        ]
        
        response = provider.complete(messages, system_prompt="System prompt")
        
        # Assertions
        mock_client_class.assert_called_once_with(api_key="test-key")
        
        # Verify generate_content called with correct model and arguments
        mock_client_instance.models.generate_content.assert_called_once()
        call_kwargs = mock_client_instance.models.generate_content.call_args[1]
        self.assertEqual(call_kwargs["model"], "gemini-3.6-flash")
        
        contents = call_kwargs["contents"]
        self.assertEqual(len(contents), 2)
        self.assertEqual(contents[0].role, "user")
        self.assertEqual(contents[0].parts[0].text, "Hello")
        self.assertEqual(contents[1].role, "model")
        self.assertEqual(contents[1].parts[0].text, "Hi")
        
        # Verify GenerateContentConfig configuration
        config = call_kwargs["config"]
        self.assertEqual(config.system_instruction, "System prompt\n\nThis is context")
        
        # Response checks
        self.assertEqual(response.content, "Generated P code")
        self.assertEqual(response.usage.input_tokens, 10)
        self.assertEqual(response.usage.output_tokens, 20)
        self.assertEqual(response.usage.total_tokens, 30)
        self.assertEqual(response.finish_reason, "stop")

    @patch("google.genai.Client")
    def test_complete_error_handling(self, mock_client_class):
        from google.api_core import exceptions as google_exceptions
        from core.llm.base import RateLimitError, ModelNotFoundError, TimeoutError as ProviderTimeoutError
        
        mock_client_instance = MagicMock()
        mock_client_class.return_value = mock_client_instance
        
        provider = GeminiProvider({"api_key": "test-key"})
        messages = [Message(role=MessageRole.USER, content="Hello")]
        
        # Test PermissionDenied raises AuthenticationError
        mock_client_instance.models.generate_content.side_effect = google_exceptions.PermissionDenied("Invalid key")
        with self.assertRaises(AuthenticationError):
            provider.complete(messages)

        # Test ResourceExhausted raises RateLimitError
        mock_client_instance.models.generate_content.side_effect = google_exceptions.ResourceExhausted("Rate limit exceeded")
        with self.assertRaises(RateLimitError):
            provider.complete(messages)

        # Test NotFound raises ModelNotFoundError
        mock_client_instance.models.generate_content.side_effect = google_exceptions.NotFound("Model not found")
        with self.assertRaises(ModelNotFoundError):
            provider.complete(messages)

        # Test DeadlineExceeded raises ProviderTimeoutError
        mock_client_instance.models.generate_content.side_effect = google_exceptions.DeadlineExceeded("Timeout")
        with self.assertRaises(ProviderTimeoutError):
            provider.complete(messages)

    @patch("google.genai.Client")
    def test_complete_error_handling_fallback_matching(self, mock_client_class):
        from core.llm.base import RateLimitError, ModelNotFoundError, TimeoutError as ProviderTimeoutError, ProviderError
        
        mock_client_instance = MagicMock()
        mock_client_class.return_value = mock_client_instance
        
        provider = GeminiProvider({"api_key": "test-key"})
        messages = [Message(role=MessageRole.USER, content="Hello")]

        # Text: API key
        mock_client_instance.models.generate_content.side_effect = Exception("error with api key mismatch")
        with self.assertRaises(AuthenticationError):
            provider.complete(messages)

        # Text: quota / rate limit
        mock_client_instance.models.generate_content.side_effect = Exception("exhausted rate limit quota")
        with self.assertRaises(RateLimitError):
            provider.complete(messages)

        # Text: not found
        mock_client_instance.models.generate_content.side_effect = Exception("this requested model was not found")
        with self.assertRaises(ModelNotFoundError):
            provider.complete(messages)

        # Text: timeout / deadline
        mock_client_instance.models.generate_content.side_effect = Exception("request timeout reached")
        with self.assertRaises(ProviderTimeoutError):
            provider.complete(messages)

        # Generic exception
        mock_client_instance.models.generate_content.side_effect = Exception("random failure")
        with self.assertRaises(ProviderError):
            provider.complete(messages)

    def test_get_client_import_error(self):
        from core.llm.base import ProviderError
        provider = GeminiProvider({"api_key": "test-key"})
        with patch.dict("sys.modules", {"google": None, "google.genai": None}):
            with self.assertRaises(ProviderError) as ctx:
                provider._get_client()
            self.assertIn("google-genai package not installed", str(ctx.exception))


if __name__ == "__main__":
    unittest.main()
