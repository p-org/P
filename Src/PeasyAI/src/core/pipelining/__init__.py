"""
Pipelining Module

Provides conversation management for LLM interactions.
"""

# Import from new module
from .pipeline import Pipeline, PromptingPipeline

# Also export from legacy module for backward compatibility
try:
    from .prompting_pipeline import PromptingPipeline as LegacyPromptingPipeline
except ImportError:
    LegacyPromptingPipeline = PromptingPipeline

__all__ = [
    "Pipeline",
    "PromptingPipeline",
    "LegacyPromptingPipeline",
]


