"""
AWS Bedrock LLM Provider

Uses the AWS Bedrock Converse API to access Claude models.
"""

import time
import json
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


class BedrockProvider(LLMProvider):
    """
    AWS Bedrock provider using the Converse API.
    
    Configuration:
        region: AWS region (default: 'us-west-2')
        model: Default model ID (e.g., 'anthropic.claude-3-5-sonnet-20241022-v2:0')
        timeout: Request timeout in seconds (default: 1000)
    """
    
    AVAILABLE_MODELS = [
        "anthropic.claude-3-5-sonnet-20241022-v2:0",
        "anthropic.claude-3-haiku-20240307-v1:0",
        "anthropic.claude-3-opus-20240229-v1:0",
    ]
    
    DEFAULT_MODEL = "anthropic.claude-3-5-sonnet-20241022-v2:0"
    
    def __init__(self, config: Dict[str, Any]):
        super().__init__(config)
        
        self._region = config.get("region", "us-west-2")
        self._timeout = config.get("timeout", 1000)
        self._default_model = config.get("model", self.DEFAULT_MODEL)
        
        # Initialize client lazily
        self._client = None
    
    def _get_client(self):
        """Get or create the Bedrock client"""
        if self._client is None:
            try:
                import boto3
                from botocore.config import Config
                
                bedrock_config = Config(read_timeout=self._timeout)
                self._client = boto3.client(
                    service_name='bedrock-runtime',
                    region_name=self._region,
                    config=bedrock_config
                )
            except ImportError:
                raise ProviderError(
                    self.name,
                    "boto3 package not installed. Run: pip install boto3"
                )
        return self._client
    
    def _convert_messages_to_bedrock_format(
        self, 
        messages: List[Message]
    ) -> List[Dict[str, Any]]:
        """Convert messages to Bedrock Converse API format"""
        bedrock_messages = []
        
        for msg in messages:
            content_parts = []
            
            # Add main text content
            full_content = msg.get_full_content()
            if full_content:
                content_parts.append({"text": full_content})
            
            bedrock_messages.append({
                "role": msg.role.value,
                "content": content_parts
            })
        
        return bedrock_messages
    
    def complete(
        self,
        messages: List[Message],
        config: Optional[LLMConfig] = None,
        system_prompt: Optional[str] = None
    ) -> LLMResponse:
        """Send messages to AWS Bedrock and get completion"""
        
        cfg = self._get_config(config)
        model = self._get_model(config)
        client = self._get_client()
        
        # Convert messages to Bedrock format
        bedrock_messages = self._convert_messages_to_bedrock_format(messages)
        
        # Build system prompt
        system_prompt_content = None
        if system_prompt:
            system_prompt_content = [{"text": system_prompt}]
        
        # Build inference config
        inference_config = {
            "maxTokens": cfg.max_tokens,
            "temperature": cfg.temperature,
            "topP": cfg.top_p,
        }
        
        logger.info(f"Bedrock request: model={model}, messages={len(bedrock_messages)}")
        start_time = time.time()
        
        try:
            # Build request kwargs
            request_kwargs = {
                "modelId": model,
                "messages": bedrock_messages,
                "inferenceConfig": inference_config,
            }
            
            if system_prompt_content:
                request_kwargs["system"] = system_prompt_content
            
            response = client.converse(**request_kwargs)
            
            latency_ms = self._measure_latency(start_time)
            logger.info(f"Bedrock response: latency={latency_ms}ms")
            
            # Extract content
            content = ""
            output_message = response.get("output", {}).get("message", {})
            content_parts = output_message.get("content", [])
            if content_parts and "text" in content_parts[0]:
                content = content_parts[0]["text"]
            
            # Extract usage
            usage_data = response.get("usage", {})
            usage = TokenUsage(
                input_tokens=usage_data.get("inputTokens", 0),
                output_tokens=usage_data.get("outputTokens", 0),
                total_tokens=usage_data.get("totalTokens", 0),
                cache_read_tokens=usage_data.get("cacheReadInputTokens", 0),
                cache_write_tokens=usage_data.get("cacheWriteInputTokens", 0),
            )
            
            # Get actual latency from metrics if available
            metrics = response.get("metrics", {})
            if "latencyMs" in metrics:
                latency_ms = metrics["latencyMs"]
            
            return LLMResponse(
                content=content,
                usage=usage,
                finish_reason=response.get("stopReason", "end_turn"),
                latency_ms=latency_ms,
                model=model,
                provider=self.name,
                raw_response=response,
            )
            
        except Exception as e:
            latency_ms = self._measure_latency(start_time)
            error_str = str(e).lower()
            
            # Log the conversation for debugging
            logger.error(f"Bedrock error after {latency_ms}ms: {e}")
            
            if "credential" in error_str or "unauthorized" in error_str or "access denied" in error_str:
                raise AuthenticationError(
                    self.name,
                    f"AWS authentication failed. Check your credentials. Error: {e}",
                    original_error=e
                )
            elif "timeout" in error_str or "timed out" in error_str:
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
        return "bedrock"


