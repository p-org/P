"""
Google Gemini API Provider

Uses the google-genai SDK to access Gemini models.
"""

import time
import logging
from typing import List, Dict, Any, Optional

from .base import (
    LLMProvider,
    LLMConfig,
    LLMResponse,
    Message,
    MessageRole,
    TokenUsage,
    ProviderError,
    AuthenticationError,
    RateLimitError,
    ModelNotFoundError,
    TimeoutError as ProviderTimeoutError,
)

logger = logging.getLogger(__name__)


class GeminiProvider(LLMProvider):
    """
    Google Gemini API provider using the official google-genai SDK.

    Configuration:
        api_key: Gemini API key
        model: Default model name (e.g., 'gemini-3.6-flash')
        timeout: Request timeout in seconds (default: 600)
    """

    AVAILABLE_MODELS = [
        "gemini-3.6-flash",
        "gemini-3.5-flash",
        "gemini-3.1-pro-preview",
        "gemini-2.5-pro",
    ]

    DEFAULT_MODEL = "gemini-3.6-flash"

    def __init__(self, config: Dict[str, Any]):
        super().__init__(config)

        if not config.get("api_key"):
            raise ValueError("Gemini provider requires 'api_key' in config")

        self._api_key = config["api_key"]
        self._timeout = config.get("timeout", 600.0)
        self._default_model = config.get("model", self.DEFAULT_MODEL)

        # Initialize client lazily
        self._client = None

    def _get_client(self):
        """Get or create and configure the Gemini client"""
        if self._client is None:
            try:
                from google import genai
                from google.genai import types

                self._client = genai.Client(
                    api_key=self._api_key,
                    http_options=types.HttpOptions(
                        timeout=max(1, int(self._timeout * 1000))
                    ),
                )
            except ImportError:
                raise ProviderError(
                    self.name,
                    "google-genai package not installed. Run: pip install google-genai"
                )
        return self._client

    def complete(
        self,
        messages: List[Message],
        config: Optional[LLMConfig] = None,
        system_prompt: Optional[str] = None
    ) -> LLMResponse:
        """Send messages to Gemini API and get completion"""

        cfg = self._get_config(config)
        model = self._get_model(config)
        client = self._get_client()

        from google.genai import types

        # Build system prompt and format messages
        # Gemini expects roles to be 'user' or 'model'. 
        # It doesn't allow 'system' inside contents list, so we map 'SYSTEM' messages to system_instruction or prepend.
        effective_system_prompt = system_prompt or ""
        chat_contents = []

        for msg in messages:
            if msg.role == MessageRole.SYSTEM:
                if effective_system_prompt:
                    effective_system_prompt += "\n\n" + msg.get_full_content()
                else:
                    effective_system_prompt = msg.get_full_content()
            else:
                role = "user" if msg.role == MessageRole.USER else "model"
                part = types.Part.from_text(text=msg.get_full_content())
                chat_contents.append(
                    types.Content(
                        role=role,
                        parts=[part]
                    )
                )

        logger.info(f"Gemini request: model={model}, messages={len(chat_contents)}")
        start_time = time.time()

        try:

            # Configure generation parameters using GenerateContentConfig
            generate_config = types.GenerateContentConfig(
                temperature=cfg.temperature,
                top_p=cfg.top_p,
                max_output_tokens=cfg.max_tokens,
            )
            if effective_system_prompt:
                generate_config.system_instruction = effective_system_prompt

            # Request content generation
            response = client.models.generate_content(
                model=model,
                contents=chat_contents,
                config=generate_config
            )

            latency_ms = self._measure_latency(start_time)
            logger.info(f"Gemini response: latency={latency_ms}ms")

            # Extract content
            content = response.text if response.text else ""

            # Extract usage
            if hasattr(response, "usage_metadata") and response.usage_metadata:
                usage = TokenUsage(
                    input_tokens=response.usage_metadata.prompt_token_count or 0,
                    output_tokens=response.usage_metadata.candidates_token_count or 0,
                    total_tokens=response.usage_metadata.total_token_count or 0,
                )
            else:
                usage = TokenUsage()

            # Extract finish reason
            finish_reason = "stop"
            if response.candidates and len(response.candidates) > 0:
                candidate = response.candidates[0]
                if hasattr(candidate, "finish_reason"):
                    reason = candidate.finish_reason
                    if hasattr(reason, "name"):
                        finish_reason = reason.name.lower()
                    else:
                        finish_reason = str(reason).lower()

            return LLMResponse(
                content=content,
                usage=usage,
                finish_reason=finish_reason,
                latency_ms=latency_ms,
                model=model,
                provider=self.name,
                raw_response=response,
            )

        except Exception as e:
            latency_ms = self._measure_latency(start_time)
            error_str = str(e).lower()

            logger.error(f"Gemini error after {latency_ms}ms: {e}")

            try:
                import httpx
                from google.genai import errors as genai_errors

                if isinstance(e, genai_errors.APIError):
                    if e.code in (401, 403):
                        raise AuthenticationError(
                            self.name,
                            f"Authentication failed. Check your API key. Error: {e}",
                            original_error=e
                        )
                    if e.code == 429:
                        raise RateLimitError(
                            self.name,
                            f"Rate limit exceeded. Error: {e}",
                            original_error=e
                        )
                    if e.code == 404:
                        raise ModelNotFoundError(
                            self.name,
                            f"Model not found or not accessible: {model}. Error: {e}",
                            original_error=e
                        )
                    if e.code in (408, 504):
                        raise ProviderTimeoutError(
                            self.name,
                            f"Request timed out after {self._timeout}s",
                            original_error=e
                        )
                if isinstance(e, httpx.TimeoutException):
                    raise ProviderTimeoutError(
                        self.name,
                        f"Request timed out after {self._timeout}s",
                        original_error=e
                    )
            except ImportError:
                # The SDK and httpx are transitive runtime dependencies, but
                # retain string matching for unusual installation failures.
                pass

            # Fallback matching for transport and older-SDK exceptions.
            if "api key" in error_str or "unauthenticated" in error_str or "401" in error_str or "403" in error_str or "permission denied" in error_str:
                raise AuthenticationError(
                    self.name,
                    f"Authentication failed. Check your API key. Error: {e}",
                    original_error=e
                )
            elif "quota" in error_str or "rate limit" in error_str or "429" in error_str:
                raise RateLimitError(
                    self.name,
                    f"Rate limit exceeded. Error: {e}",
                    original_error=e
                )
            elif "not found" in error_str or "404" in error_str:
                raise ModelNotFoundError(
                    self.name,
                    f"Model not found or not accessible. Error: {e}",
                    original_error=e
                )
            elif "timeout" in error_str or "deadline" in error_str:
                raise ProviderTimeoutError(
                    self.name,
                    f"Request timed out after {self._timeout}s",
                    original_error=e
                )
            else:
                raise ProviderError(
                    self.name,
                    f"Request failed: {e}",
                    original_error=e
                )

    def available_models(self) -> List[str]:
        """List available models"""
        return self.AVAILABLE_MODELS.copy()

    @property
    def name(self) -> str:
        return "gemini"
