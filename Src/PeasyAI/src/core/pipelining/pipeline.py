"""
Refactored Prompting Pipeline

This module provides stateful conversation management using the
new LLM provider abstraction. It maintains backward compatibility
with existing code while enabling the use of multiple providers.
"""

import os
import json
import logging
from pathlib import Path
from typing import List, Dict, Any, Optional, Callable

from ..llm import (
    LLMProvider,
    LLMConfig,
    LLMResponse,
    Message,
    MessageRole,
    Document,
    get_default_provider,
)

logger = logging.getLogger(__name__)


class Pipeline:
    """
    Stateful conversation pipeline for LLM interactions.
    
    This is a refactored version of PromptingPipeline that uses
    the new LLM provider abstraction while maintaining the same
    interface for backward compatibility.
    
    Features:
    - Multi-turn conversation management
    - System prompt handling
    - Document attachment support
    - Token usage tracking
    - Provider-agnostic operation
    """
    
    def __init__(self, llm_provider: Optional[LLMProvider] = None):
        """
        Initialize the pipeline.
        
        Args:
            llm_provider: Optional LLM provider instance.
                         If not provided, auto-detects from environment.
        """
        self._provider = llm_provider
        self._messages: List[Message] = []
        self._system_prompt: Optional[str] = None
        self._usage_stats = {
            'cumulative': {
                'inputTokens': 0,
                'outputTokens': 0,
                'totalTokens': 0,
            },
            'sequential': []
        }
        self._removed_messages: List[List[Message]] = []
    
    @property
    def provider(self) -> LLMProvider:
        """Get the LLM provider (lazy initialization)"""
        if self._provider is None:
            self._provider = get_default_provider()
        return self._provider
    
    # =========================================================================
    # Message Management
    # =========================================================================
    
    def add_system_prompt(self, prompt: str):
        """
        Set the system prompt for the conversation.
        
        Args:
            prompt: The system prompt text
        """
        self._system_prompt = prompt
    
    def add_user_msg(self, text: str, document_paths: Optional[List[str]] = None):
        """
        Add a user message to the conversation.
        
        Args:
            text: The message text
            document_paths: Optional list of file paths to attach
        """
        documents = None
        if document_paths:
            documents = []
            for path in document_paths:
                try:
                    content = Path(path).read_text(encoding='utf-8')
                    name = Path(path).stem
                    documents.append(Document(name=name, content=content))
                except Exception as e:
                    logger.warning(f"Could not read document {path}: {e}")
        
        self._messages.append(Message(
            role=MessageRole.USER,
            content=text,
            documents=documents
        ))
    
    def add_assistant_msg(self, text: str):
        """
        Add an assistant message to the conversation.
        
        Args:
            text: The message text
        """
        self._messages.append(Message(
            role=MessageRole.ASSISTANT,
            content=text
        ))
    
    def add_text(self, text: str):
        """
        Add a user message with just text (legacy compatibility).
        
        Args:
            text: The message text
        """
        self.add_user_msg(text)
    
    def add_document(self, doc_path: str):
        """
        Add a user message with a document attachment.
        
        Args:
            doc_path: Path to the document file
        """
        try:
            content = Path(doc_path).read_text(encoding='utf-8')
            name = Path(doc_path).stem
            
            self._messages.append(Message(
                role=MessageRole.USER,
                content=f"Please refer to the attached document: {name}",
                documents=[Document(name=name, content=content)]
            ))
        except Exception as e:
            logger.error(f"Could not read document {doc_path}: {e}")
            raise
    
    def add_documents_inline(
        self,
        doc_paths: List[str],
        wrapper_func: Optional[Callable[[str, str], str]] = None,
    ):
        """
        Add multiple documents as inline content in a single message.
        
        Args:
            doc_paths: List of document file paths
            wrapper_func: Optional function to wrap each document's content.
                         Receives (filename, content) and returns wrapped content.
        """
        if wrapper_func is None:
            wrapper_func = lambda name, content: f"<{name}>\n{content}\n</{name}>"
        
        parts = []
        for path in doc_paths:
            try:
                content = Path(path).read_text(encoding='utf-8')
                name = Path(path).name
                parts.append(wrapper_func(name, content))
            except Exception as e:
                logger.warning(f"Could not read document {path}: {e}")
        
        if parts:
            self._messages.append(Message(
                role=MessageRole.USER,
                content="\n\n".join(parts)
            ))
    
    def remove_last_messages(self, n: int = 1):
        """
        Remove the last N messages from the conversation.
        
        Args:
            n: Number of messages to remove
        """
        if n > 0 and len(self._messages) >= n:
            removed = self._messages[-n:]
            self._messages = self._messages[:-n]
            self._removed_messages.append(removed)
    
    # =========================================================================
    # LLM Invocation
    # =========================================================================
    
    def invoke_llm(
        self,
        model: Optional[str] = None,
        candidates: int = 1,
        heuristic: str = 'random',
        inference_config: Optional[Dict[str, Any]] = None,
        **kwargs
    ) -> str:
        """
        Invoke the LLM and get a response.
        
        Args:
            model: Optional model override (uses provider default if not specified)
            candidates: Number of response candidates to generate
            heuristic: Selection heuristic if candidates > 1 ('random', 'first')
            inference_config: Optional dict with 'maxTokens', 'temperature', 'topP'
            **kwargs: Additional arguments (for backward compatibility)
            
        Returns:
            The LLM response text
        """
        # Build LLM config
        config = self._build_config(model, inference_config)
        
        # Generate candidates
        responses = []
        for _ in range(candidates):
            try:
                response = self.provider.complete(
                    messages=self._messages,
                    config=config,
                    system_prompt=self._system_prompt
                )
                
                # Track usage
                self._update_usage_stats(response)
                
                responses.append(response.content)
                
            except Exception as e:
                logger.error(f"LLM invocation failed: {e}")
                raise
        
        # Select response
        selected = self._select_response(responses, heuristic)
        
        # Add to conversation
        self._messages.append(Message(
            role=MessageRole.ASSISTANT,
            content=selected
        ))
        
        return selected
    
    def _build_config(
        self,
        model: Optional[str],
        inference_config: Optional[Dict[str, Any]]
    ) -> LLMConfig:
        """Build LLMConfig from parameters"""
        config = LLMConfig()
        
        if model:
            config.model = model
        
        if inference_config:
            if 'maxTokens' in inference_config:
                config.max_tokens = inference_config['maxTokens']
            if 'temperature' in inference_config:
                config.temperature = inference_config['temperature']
            if 'topP' in inference_config:
                config.top_p = inference_config['topP']
        
        return config
    
    def _select_response(self, responses: List[str], heuristic: str) -> str:
        """Select a response from candidates"""
        if not responses:
            raise ValueError("No responses to select from")
        
        if len(responses) == 1:
            return responses[0]
        
        if heuristic == 'random':
            import random
            return random.choice(responses)
        
        # Default to first
        return responses[0]
    
    def _update_usage_stats(self, response: LLMResponse):
        """Update token usage statistics"""
        usage = response.usage.to_dict()
        
        # Update cumulative
        for key in ['inputTokens', 'outputTokens', 'totalTokens']:
            if key in usage:
                self._usage_stats['cumulative'][key] += usage[key]
        
        # Add to sequential
        self._usage_stats['sequential'].append(usage)
    
    # =========================================================================
    # State Access
    # =========================================================================
    
    def get_last_response(self) -> Optional[str]:
        """Get the last assistant response"""
        for msg in reversed(self._messages):
            if msg.role == MessageRole.ASSISTANT:
                return msg.content
        return None
    
    def get_conversation(self) -> List[Message]:
        """Get the full conversation"""
        return self._messages.copy()
    
    def get_context(self) -> List[Message]:
        """Alias for get_conversation (legacy compatibility)"""
        return self.get_conversation()
    
    def get_system_prompt(self) -> Optional[str]:
        """Get the system prompt"""
        return self._system_prompt
    
    def get_token_usage(self) -> Dict[str, Any]:
        """Get token usage statistics"""
        return self._usage_stats
    
    def get_total_input_tokens(self) -> int:
        """Get total input tokens used"""
        return self._usage_stats['cumulative'].get('inputTokens', 0)
    
    def get_total_output_tokens(self) -> int:
        """Get total output tokens used"""
        return self._usage_stats['cumulative'].get('outputTokens', 0)
    
    def prune_context(self, pruner_func: Callable[[List[Message]], List[Message]]):
        """
        Prune the conversation using a custom function.
        
        Args:
            pruner_func: Function that takes message list and returns pruned list
        """
        self._messages = pruner_func(self._messages)
    
    def clear(self):
        """Clear the conversation"""
        self._messages = []
        self._system_prompt = None
        self._usage_stats = {
            'cumulative': {
                'inputTokens': 0,
                'outputTokens': 0,
                'totalTokens': 0,
            },
            'sequential': []
        }


# Backward compatibility alias
PromptingPipeline = Pipeline


