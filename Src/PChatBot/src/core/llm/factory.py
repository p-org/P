"""
LLM Provider Factory

Creates and manages LLM provider instances based on configuration
or environment variables.
"""

import os
import logging
from typing import Dict, Any, Optional, Type

from .base import LLMProvider, ProviderError
from .snowflake import SnowflakeCortexProvider
from .bedrock import BedrockProvider
from .anthropic_direct import AnthropicProvider

logger = logging.getLogger(__name__)


class LLMProviderFactory:
    """
    Factory for creating LLM provider instances.
    
    Supports:
    - Explicit provider creation by name
    - Auto-detection from environment variables
    - Provider configuration via dictionaries
    """
    
    # Registry of available providers
    _providers: Dict[str, Type[LLMProvider]] = {
        "snowflake": SnowflakeCortexProvider,
        "snowflake_cortex": SnowflakeCortexProvider,
        "bedrock": BedrockProvider,
        "aws_bedrock": BedrockProvider,
        "anthropic": AnthropicProvider,
        "anthropic_direct": AnthropicProvider,
    }
    
    @classmethod
    def register_provider(cls, name: str, provider_class: Type[LLMProvider]):
        """
        Register a new provider type.
        
        Args:
            name: Name to register the provider under
            provider_class: The provider class to register
        """
        cls._providers[name.lower()] = provider_class
        logger.info(f"Registered LLM provider: {name}")
    
    @classmethod
    def available_providers(cls) -> list:
        """Get list of available provider names"""
        return list(set(cls._providers.values()))
    
    @classmethod
    def create(cls, provider_name: str, config: Dict[str, Any]) -> LLMProvider:
        """
        Create a provider instance by name.
        
        Args:
            provider_name: Name of the provider (e.g., 'snowflake', 'bedrock')
            config: Provider configuration dictionary
            
        Returns:
            Configured LLMProvider instance
            
        Raises:
            ValueError: If provider name is unknown
        """
        provider_name = provider_name.lower()
        
        if provider_name not in cls._providers:
            available = list(set(cls._providers.keys()))
            raise ValueError(
                f"Unknown provider: {provider_name}. "
                f"Available providers: {available}"
            )
        
        provider_class = cls._providers[provider_name]
        logger.info(f"Creating LLM provider: {provider_name}")
        
        return provider_class(config)
    
    @classmethod
    def from_env(cls) -> LLMProvider:
        """
        Create a provider based on environment variables.
        
        Detection order:
        1. Snowflake Cortex (if OPENAI_BASE_URL contains 'snowflake')
        2. Direct Anthropic (if ANTHROPIC_API_KEY is set)
        3. OpenAI-compatible (if OPENAI_API_KEY is set without snowflake URL)
        4. AWS Bedrock (default fallback)
        
        Returns:
            Configured LLMProvider instance
        """
        # Check for explicit provider selection
        explicit_provider = os.environ.get("LLM_PROVIDER", "").lower()
        
        if explicit_provider:
            logger.info(f"Using explicitly configured provider: {explicit_provider}")
            return cls._create_from_explicit_env(explicit_provider)
        
        # Auto-detect based on available credentials
        return cls._auto_detect_provider()
    
    @classmethod
    def _auto_detect_provider(cls) -> LLMProvider:
        """Auto-detect and create provider based on available env vars"""
        
        openai_base_url = os.environ.get("OPENAI_BASE_URL", "")
        openai_api_key = os.environ.get("OPENAI_API_KEY", "")
        anthropic_api_key = os.environ.get("ANTHROPIC_API_KEY", "")
        
        # 1. Check for Snowflake Cortex
        if openai_base_url and "snowflake" in openai_base_url.lower():
            logger.info("Auto-detected: Snowflake Cortex (OpenAI-compatible)")
            return cls.create("snowflake", {
                "api_key": openai_api_key,
                "base_url": openai_base_url,
                "model": os.environ.get("OPENAI_MODEL_NAME", "claude-4-5-opus-high"),
                "timeout": float(os.environ.get("LLM_TIMEOUT", "600")),
            })
        
        # 2. Check for direct Anthropic
        if anthropic_api_key:
            logger.info("Auto-detected: Direct Anthropic API")
            config = {
                "api_key": anthropic_api_key,
                "model": os.environ.get("ANTHROPIC_MODEL_NAME", "claude-3-5-sonnet-20241022"),
                "timeout": float(os.environ.get("LLM_TIMEOUT", "600")),
            }
            
            anthropic_base_url = os.environ.get("ANTHROPIC_BASE_URL")
            if anthropic_base_url:
                config["base_url"] = anthropic_base_url
            
            return cls.create("anthropic", config)
        
        # 3. Default to AWS Bedrock
        logger.info("Auto-detected: AWS Bedrock (default)")
        return cls.create("bedrock", {
            "region": os.environ.get("AWS_REGION", "us-west-2"),
            "model": os.environ.get(
                "BEDROCK_MODEL_ID",
                "anthropic.claude-3-5-sonnet-20241022-v2:0"
            ),
            "timeout": float(os.environ.get("LLM_TIMEOUT", "1000")),
        })
    
    @classmethod
    def _create_from_explicit_env(cls, provider_name: str) -> LLMProvider:
        """Create provider from explicit LLM_PROVIDER env var"""
        
        if provider_name in ("snowflake", "snowflake_cortex"):
            return cls.create("snowflake", {
                "api_key": os.environ.get("OPENAI_API_KEY"),
                "base_url": os.environ.get("OPENAI_BASE_URL"),
                "model": os.environ.get("OPENAI_MODEL_NAME", "claude-4-5-opus-high"),
                "timeout": float(os.environ.get("LLM_TIMEOUT", "600")),
            })
        
        elif provider_name in ("anthropic", "anthropic_direct"):
            config = {
                "api_key": os.environ.get("ANTHROPIC_API_KEY"),
                "model": os.environ.get("ANTHROPIC_MODEL_NAME", "claude-3-5-sonnet-20241022"),
                "timeout": float(os.environ.get("LLM_TIMEOUT", "600")),
            }
            
            anthropic_base_url = os.environ.get("ANTHROPIC_BASE_URL")
            if anthropic_base_url:
                config["base_url"] = anthropic_base_url
            
            return cls.create("anthropic", config)
        
        elif provider_name in ("bedrock", "aws_bedrock"):
            return cls.create("bedrock", {
                "region": os.environ.get("AWS_REGION", "us-west-2"),
                "model": os.environ.get(
                    "BEDROCK_MODEL_ID",
                    "anthropic.claude-3-5-sonnet-20241022-v2:0"
                ),
                "timeout": float(os.environ.get("LLM_TIMEOUT", "1000")),
            })
        
        else:
            raise ValueError(f"Unknown provider in LLM_PROVIDER: {provider_name}")


# Singleton instance for convenience
_default_provider: Optional[LLMProvider] = None


def get_default_provider() -> LLMProvider:
    """
    Get the default LLM provider (singleton).
    
    Creates a provider from environment variables on first call,
    then returns the same instance on subsequent calls.
    
    Returns:
        The default LLMProvider instance
    """
    global _default_provider
    
    if _default_provider is None:
        _default_provider = LLMProviderFactory.from_env()
    
    return _default_provider


def reset_default_provider():
    """Reset the default provider (for testing or reconfiguration)"""
    global _default_provider
    _default_provider = None
