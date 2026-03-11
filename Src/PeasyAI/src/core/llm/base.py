"""
Base classes and data models for LLM providers.

This module defines the abstract interface that all LLM providers must implement,
along with the data structures for messages, responses, and configuration.
"""

from abc import ABC, abstractmethod
from dataclasses import dataclass, field
from typing import List, Dict, Any, Optional
from enum import Enum
import time


class MessageRole(Enum):
    """Role of a message in a conversation"""
    SYSTEM = "system"
    USER = "user"
    ASSISTANT = "assistant"


@dataclass
class Document:
    """A document attachment for a message"""
    name: str
    content: str
    format: str = "txt"
    
    def to_xml(self) -> str:
        """Convert document to XML format for inclusion in prompts"""
        return f"<{self.name}>\n{self.content}\n</{self.name}>"


@dataclass
class Message:
    """A single message in a conversation"""
    role: MessageRole
    content: str
    documents: Optional[List[Document]] = None
    
    def to_dict(self) -> Dict[str, Any]:
        """Convert to dictionary format for API calls"""
        return {
            "role": self.role.value,
            "content": self.content
        }
    
    def get_full_content(self) -> str:
        """Get content including any embedded documents"""
        parts = [self.content]
        if self.documents:
            for doc in self.documents:
                parts.append(doc.to_xml())
        return "\n\n".join(parts)


@dataclass
class TokenUsage:
    """Token usage statistics for an LLM call"""
    input_tokens: int = 0
    output_tokens: int = 0
    total_tokens: int = 0
    cache_read_tokens: int = 0
    cache_write_tokens: int = 0
    
    def to_dict(self) -> Dict[str, int]:
        return {
            "inputTokens": self.input_tokens,
            "outputTokens": self.output_tokens,
            "totalTokens": self.total_tokens,
            "cacheReadInputTokens": self.cache_read_tokens,
            "cacheWriteInputTokens": self.cache_write_tokens
        }


@dataclass
class LLMResponse:
    """Response from an LLM provider"""
    content: str
    usage: TokenUsage
    finish_reason: str
    latency_ms: int
    model: str
    provider: str
    raw_response: Optional[Any] = None
    
    def to_dict(self) -> Dict[str, Any]:
        """Convert to dictionary format (compatible with existing code)"""
        return {
            "output": {
                "message": {
                    "role": "assistant",
                    "content": [{"text": self.content}]
                }
            },
            "stopReason": self.finish_reason,
            "usage": self.usage.to_dict(),
            "metrics": {"latencyMs": self.latency_ms}
        }


@dataclass
class LLMConfig:
    """Configuration for an LLM call"""
    model: Optional[str] = None  # Use provider default if None
    max_tokens: int = 4096
    temperature: float = 1.0
    top_p: float = 0.999
    timeout: float = 600.0
    
    def to_dict(self) -> Dict[str, Any]:
        """Convert to inference config format (compatible with existing code)"""
        return {
            "maxTokens": self.max_tokens,
            "temperature": self.temperature,
            "topP": self.top_p
        }


class LLMProvider(ABC):
    """
    Abstract base class for LLM providers.
    
    All LLM providers (Bedrock, Snowflake, Anthropic, OpenAI) must implement
    this interface to be usable by PeasyAI.
    """
    
    def __init__(self, config: Dict[str, Any]):
        """
        Initialize the provider with configuration.
        
        Args:
            config: Provider-specific configuration dictionary
        """
        self._config = config
        self._default_model = config.get("model") or config.get("default_model")
    
    @abstractmethod
    def complete(
        self,
        messages: List[Message],
        config: Optional[LLMConfig] = None,
        system_prompt: Optional[str] = None
    ) -> LLMResponse:
        """
        Send messages to the LLM and get a completion.
        
        Args:
            messages: List of conversation messages
            config: Optional configuration overrides
            system_prompt: Optional system prompt
            
        Returns:
            LLMResponse with the completion
        """
        pass
    
    @abstractmethod
    def available_models(self) -> List[str]:
        """
        List available models for this provider.
        
        Returns:
            List of model identifiers
        """
        pass
    
    @property
    @abstractmethod
    def name(self) -> str:
        """
        Get the provider name for logging/identification.
        
        Returns:
            Provider name string
        """
        pass
    
    @property
    def default_model(self) -> str:
        """Get the default model for this provider"""
        return self._default_model
    
    def _measure_latency(self, start_time: float) -> int:
        """Calculate latency in milliseconds"""
        return int((time.time() - start_time) * 1000)
    
    def _get_model(self, config: Optional[LLMConfig]) -> str:
        """Get the model to use, from config or default"""
        if config and config.model:
            return config.model
        return self.default_model
    
    def _get_config(self, config: Optional[LLMConfig]) -> LLMConfig:
        """Get config with defaults applied"""
        return config or LLMConfig()


class ProviderError(Exception):
    """Base exception for provider errors"""
    def __init__(self, provider: str, message: str, original_error: Optional[Exception] = None):
        self.provider = provider
        self.original_error = original_error
        super().__init__(f"[{provider}] {message}")


class AuthenticationError(ProviderError):
    """Authentication failed with the provider"""
    pass


class RateLimitError(ProviderError):
    """Rate limit exceeded"""
    pass


class ModelNotFoundError(ProviderError):
    """Requested model not available"""
    pass


class TimeoutError(ProviderError):
    """Request timed out"""
    pass


