"""
Direct Anthropic API Provider

Uses the Anthropic Python SDK to access Claude models directly.
"""

import time
import logging
from typing import List, Dict, Any, Optional

from .base import (
    LLMProvider,
    LLMConfig,
    LLMResponse,
    Message,
    TokenUsage,
    ProviderError,
    AuthenticationError,
    TimeoutError as ProviderTimeoutError,
)

logger = logging.getLogger(__name__)


class AnthropicProvider(LLMProvider):
    """
    Direct Anthropic API provider using the official SDK.
    
    Configuration:
        api_key: Anthropic API key
        base_url: Optional custom base URL
        model: Default model name (e.g., 'claude-3-5-sonnet-20241022')
        timeout: Request timeout in seconds (default: 600)
    """
    
    AVAILABLE_MODELS = [
        "claude-3-5-sonnet-20241022",
        "claude-3-5-haiku-20241022",
        "claude-3-opus-20240229",
        "claude-3-sonnet-20240229",
        "claude-3-haiku-20240307",
    ]
    
    DEFAULT_MODEL = "claude-3-5-sonnet-20241022"
    
    def __init__(self, config: Dict[str, Any]):
        super().__init__(config)
        
        if not config.get("api_key"):
            raise ValueError("Anthropic provider requires 'api_key' in config")
        
        self._api_key = config["api_key"]
        self._base_url = config.get("base_url")
        self._timeout = config.get("timeout", 600.0)
        self._default_model = config.get("model", self.DEFAULT_MODEL)
        
        # Initialize client lazily
        self._client = None
    
    def _get_client(self):
        """Get or create the Anthropic client"""
        if self._client is None:
            try:
                import anthropic
                import httpx
                
                client_kwargs = {
                    "api_key": self._api_key,
                    "timeout": httpx.Timeout(self._timeout, connect=60.0),
                }
                
                if self._base_url:
                    client_kwargs["base_url"] = self._base_url
                
                self._client = anthropic.Anthropic(**client_kwargs)
            except ImportError:
                raise ProviderError(
                    self.name,
                    "anthropic package not installed. Run: pip install anthropic"
                )
        return self._client
    
    def complete(
        self,
        messages: List[Message],
        config: Optional[LLMConfig] = None,
        system_prompt: Optional[str] = None
    ) -> LLMResponse:
        """Send messages to Anthropic API and get completion"""
        
        cfg = self._get_config(config)
        model = self._get_model(config)
        client = self._get_client()
        
        # Build message list
        formatted_messages = []
        for msg in messages:
            formatted_messages.append({
                "role": msg.role.value,
                "content": msg.get_full_content()
            })
        
        # Cap max tokens to avoid streaming requirement
        max_tokens = min(cfg.max_tokens, 8192)
        
        logger.info(f"Anthropic request: model={model}, messages={len(formatted_messages)}")
        start_time = time.time()
        
        try:
            # Build request kwargs
            request_kwargs = {
                "model": model,
                "max_tokens": max_tokens,
                "messages": formatted_messages,
            }
            
            if system_prompt:
                request_kwargs["system"] = system_prompt
            
            response = client.messages.create(**request_kwargs)
            
            latency_ms = self._measure_latency(start_time)
            logger.info(f"Anthropic response: latency={latency_ms}ms")
            
            # Extract content
            content = response.content[0].text if response.content else ""
            
            # Extract usage
            usage = TokenUsage(
                input_tokens=response.usage.input_tokens,
                output_tokens=response.usage.output_tokens,
                total_tokens=response.usage.input_tokens + response.usage.output_tokens,
            )
            
            return LLMResponse(
                content=content,
                usage=usage,
                finish_reason=response.stop_reason or "stop",
                latency_ms=latency_ms,
                model=model,
                provider=self.name,
                raw_response=response,
            )
            
        except Exception as e:
            latency_ms = self._measure_latency(start_time)
            error_str = str(e).lower()
            
            logger.error(f"Anthropic error after {latency_ms}ms: {e}")
            
            if "401" in error_str or "unauthorized" in error_str or "authentication" in error_str:
                raise AuthenticationError(
                    self.name,
                    f"Authentication failed. Check your API key. Error: {e}",
                    original_error=e
                )
            elif "timeout" in error_str:
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
        return "anthropic"


