import builtins
from types import SimpleNamespace
from unittest.mock import MagicMock, patch

import httpx
import pytest
from google.genai import errors

from core.llm.base import (
    AuthenticationError,
    LLMConfig,
    Message,
    MessageRole,
    ModelNotFoundError,
    ProviderError,
    RateLimitError,
    TimeoutError as ProviderTimeoutError,
)
from core.llm.gemini import GeminiProvider


def _provider_with_client(**config):
    provider = GeminiProvider({"api_key": "test-key", **config})
    provider._client = MagicMock()
    return provider


def _api_error(code, status, message):
    return errors.ClientError(
        code,
        {"error": {"code": code, "status": status, "message": message}},
    )


def test_default_model_selection():
    provider = GeminiProvider({"api_key": "test-key"})
    assert provider.default_model == "gemini-3.6-flash"
    assert provider.name == "gemini"
    assert "gemini-3.6-flash" in provider.available_models()


def test_explicit_config_model_overrides_default():
    provider = GeminiProvider(
        {"api_key": "test-key", "model": "gemini-3.1-pro-preview"}
    )
    assert provider.default_model == "gemini-3.1-pro-preview"


def test_missing_api_key_raises_error():
    with pytest.raises(ValueError):
        GeminiProvider({})


@patch("google.genai.Client")
def test_client_uses_timeout_in_milliseconds(client_class):
    provider = GeminiProvider({"api_key": "test-key", "timeout": 12.5})

    provider._get_client()

    kwargs = client_class.call_args.kwargs
    assert kwargs["api_key"] == "test-key"
    assert kwargs["http_options"].timeout == 12_500


def test_complete_success():
    provider = _provider_with_client()
    provider._client.models.generate_content.return_value = SimpleNamespace(
        text="Generated P code",
        usage_metadata=SimpleNamespace(
            prompt_token_count=10,
            candidates_token_count=20,
            total_token_count=30,
        ),
        candidates=[
            SimpleNamespace(
                finish_reason=SimpleNamespace(name="STOP"),
            )
        ],
    )
    messages = [
        Message(role=MessageRole.USER, content="Hello"),
        Message(role=MessageRole.ASSISTANT, content="Hi"),
        Message(role=MessageRole.SYSTEM, content="This is context"),
    ]

    response = provider.complete(
        messages,
        config=LLMConfig(max_tokens=16_384),
        system_prompt="System prompt",
    )

    call = provider._client.models.generate_content.call_args.kwargs
    assert call["model"] == "gemini-3.6-flash"
    assert [content.role for content in call["contents"]] == ["user", "model"]
    assert call["contents"][0].parts[0].text == "Hello"
    assert call["contents"][1].parts[0].text == "Hi"
    assert call["config"].system_instruction == "System prompt\n\nThis is context"
    assert call["config"].max_output_tokens == 16_384
    assert response.content == "Generated P code"
    assert response.usage.input_tokens == 10
    assert response.usage.output_tokens == 20
    assert response.usage.total_tokens == 30
    assert response.finish_reason == "stop"


def test_missing_usage_counts_are_normalized_to_zero():
    provider = _provider_with_client()
    provider._client.models.generate_content.return_value = SimpleNamespace(
        text="ok",
        usage_metadata=SimpleNamespace(
            prompt_token_count=None,
            candidates_token_count=None,
            total_token_count=None,
        ),
        candidates=[],
    )

    response = provider.complete([Message(MessageRole.USER, "Hello")])

    assert response.usage.input_tokens == 0
    assert response.usage.output_tokens == 0
    assert response.usage.total_tokens == 0


@pytest.mark.parametrize(
    ("exception", "expected"),
    [
        (_api_error(401, "UNAUTHENTICATED", "Invalid key"), AuthenticationError),
        (_api_error(403, "PERMISSION_DENIED", "Forbidden"), AuthenticationError),
        (_api_error(404, "NOT_FOUND", "Unknown model"), ModelNotFoundError),
        (_api_error(429, "RESOURCE_EXHAUSTED", "Quota exceeded"), RateLimitError),
        (_api_error(504, "DEADLINE_EXCEEDED", "Deadline"), ProviderTimeoutError),
        (httpx.ReadTimeout("timed out"), ProviderTimeoutError),
        (Exception("random failure"), ProviderError),
    ],
)
def test_complete_error_mapping(exception, expected):
    provider = _provider_with_client()
    provider._client.models.generate_content.side_effect = exception

    with pytest.raises(expected):
        provider.complete([Message(MessageRole.USER, "Hello")])


def test_get_client_import_error():
    provider = GeminiProvider({"api_key": "test-key"})
    real_import = builtins.__import__

    def fail_google_import(name, *args, **kwargs):
        if name == "google":
            raise ImportError("google-genai unavailable")
        return real_import(name, *args, **kwargs)

    with patch("builtins.__import__", side_effect=fail_google_import):
        with pytest.raises(ProviderError, match="google-genai package not installed"):
            provider._get_client()
