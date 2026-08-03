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

                # Initialize the Client with api_key
                self._client = genai.Client(api_key=self._api_key)
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
                chat_contents.append({
                    "role": role,
                    "parts": [msg.get_full_content()]
                })

        max_tokens = min(cfg.max_tokens, 8192)

        logger.info(f"Gemini request: model={model}, messages={len(chat_contents)}")
        start_time = time.time()

        try:
            from google.genai import types

            # Configure generation parameters using GenerateContentConfig
            generate_config = types.GenerateContentConfig(
                temperature=cfg.temperature,
                top_p=cfg.top_p,
                max_output_tokens=max_tokens,
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
                    input_tokens=response.usage_metadata.prompt_token_count,
                    output_tokens=response.usage_metadata.candidates_token_count,
                    total_tokens=response.usage_metadata.total_token_count,
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
                from google.api_core import exceptions as google_exceptions
                is_google_exc = True
            except ImportError:
                is_google_exc = False
                google_exceptions = None

            if is_google_exc and google_exceptions:
                if isinstance(e, (google_exceptions.Unauthenticated, google_exceptions.PermissionDenied)):
                    raise AuthenticationError(
                        self.name,
                        f"Authentication failed. Check your API key. Error: {e}",
                        original_error=e
                    )
                elif isinstance(e, google_exceptions.ResourceExhausted):
                    raise RateLimitError(
                        self.name,
                        f"Rate limit exceeded. Error: {e}",
                        original_error=e
                    )
                elif isinstance(e, google_exceptions.NotFound):
                    raise ModelNotFoundError(
                        self.name,
                        f"Model not found: {model}. Error: {e}",
                        original_error=e
                    )
                elif isinstance(e, google_exceptions.DeadlineExceeded):
                    raise ProviderTimeoutError(
                        self.name,
                        f"Request timed out after {cfg.timeout}s",
                        original_error=e
                    )

            # Fallback string-based matching
            if "api key" in error_str or "unauthenticated" in error_str or "403" in error_str or "permission denied" in error_str:
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
                    f"Request timed out after {cfg.timeout}s",
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
