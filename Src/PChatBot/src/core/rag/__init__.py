"""
RAG (Retrieval-Augmented Generation) Module for P Programs.

Provides access to a database of P programs and documentation
to enhance code generation with real examples.
"""

from .embeddings import EmbeddingProvider, get_embedding_provider
from .vector_store import VectorStore, Document, SearchResult
from .p_corpus import PCorpus, PExample
from .rag_service import RAGService, get_rag_service

__all__ = [
    "EmbeddingProvider",
    "get_embedding_provider",
    "VectorStore",
    "Document",
    "SearchResult",
    "PCorpus",
    "PExample",
    "RAGService",
    "get_rag_service",
]
