"""
Snowflake Cortex LLM Provider

Uses the OpenAI-compatible API endpoint provided by Snowflake Cortex
to access Claude models.
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


class SnowflakeCortexProvider(LLMProvider):
    """
    Snowflake Cortex provider using OpenAI-compatible API.
    
    Configuration:
        api_key: Snowflake programmatic access token
        base_url: Cortex OpenAI endpoint URL
        model: Default model name (default: 'claude-opus-4-6')
        timeout: Request timeout in seconds (default: 600)
    """
    
    AVAILABLE_MODELS = [
        "claude-opus-4-6",
        "claude-sonnet-4-6",
        "claude-opus-4-5",
        "claude-sonnet-4-5",
        "claude-haiku-4-5",
    ]
    
    def __init__(self, config: Dict[str, Any]):
        super().__init__(config)
        
        # Validate required config
        if not config.get("api_key"):
            raise ValueError("Snowflake Cortex requires 'api_key' in config")
        if not config.get("base_url"):
            raise ValueError("Snowflake Cortex requires 'base_url' in config")
        
        self._api_key = config["api_key"]
        self._base_url = config["base_url"].rstrip("/")
        self._timeout = config.get("timeout", 600.0)
        self._default_model = config.get("model", "claude-opus-4-6")
        
        # Initialize OpenAI client lazily
        self._client = None
    
    def _get_client(self):
        """Get or create the OpenAI client"""
        if self._client is None:
            try:
                from openai import OpenAI
                self._client = OpenAI(
                    api_key=self._api_key,
                    base_url=self._base_url,
                    timeout=self._timeout,
                )
            except ImportError:
                raise ProviderError(
                    self.name,
                    "OpenAI package not installed. Run: pip install openai"
                )
        return self._client

    def complete(
        self,
        messages: List[Message],
        config: Optional[LLMConfig] = None,
        system_prompt: Optional[str] = None
    ) -> LLMResponse:
        """Send messages to Snowflake Cortex and get completion"""
        
        cfg = self._get_config(config)
        model = self._get_model(config)
        client = self._get_client()
        
        # Build message list
        formatted_messages = []
        
        # Add system prompt if provided
        if system_prompt:
            formatted_messages.append({
                "role": "system",
                "content": system_prompt
            })
        
        # Add conversation messages
        for msg in messages:
            formatted_messages.append({
                "role": msg.role.value,
                "content": msg.get_full_content()
            })
        
        # Cap max tokens to avoid issues
        max_tokens = min(cfg.max_tokens, 8192)
        
        logger.info(f"Snowflake Cortex request: model={model}, messages={len(formatted_messages)}")
        start_time = time.time()
        
        try:
            response = client.chat.completions.create(
                model=model,
                messages=formatted_messages,
                max_completion_tokens=max_tokens,
                temperature=cfg.temperature,
                top_p=cfg.top_p,
            )
            
            latency_ms = self._measure_latency(start_time)
            logger.info(f"Snowflake Cortex response: latency={latency_ms}ms")
            
            # Extract usage
            usage = TokenUsage(
                input_tokens=response.usage.prompt_tokens if response.usage else 0,
                output_tokens=response.usage.completion_tokens if response.usage else 0,
                total_tokens=response.usage.total_tokens if response.usage else 0,
            )
            
            return LLMResponse(
                content=response.choices[0].message.content,
                usage=usage,
                finish_reason=response.choices[0].finish_reason or "stop",
                latency_ms=latency_ms,
                model=model,
                provider=self.name,
                raw_response=response,
            )
            
        except Exception as e:
            latency_ms = self._measure_latency(start_time)
            error_str = str(e).lower()
            
            if "401" in error_str or "unauthorized" in error_str or "authentication" in error_str:
                raise AuthenticationError(
                    self.name,
                    f"Authentication failed. Check your Snowflake access token. Error: {e}",
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
        return "snowflake_cortex"
