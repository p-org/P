"""
Embedding Providers for RAG.

Supports multiple embedding backends:
- OpenAI embeddings (via Snowflake Cortex or direct)
- Local sentence-transformers (for offline use)
"""

import os
import logging
from abc import ABC, abstractmethod
from typing import List, Optional
import hashlib
import json
from pathlib import Path

logger = logging.getLogger(__name__)


class EmbeddingProvider(ABC):
    """Abstract base class for embedding providers."""
    
    @abstractmethod
    def embed(self, text: str) -> List[float]:
        """Generate embedding for a single text."""
        pass
    
    @abstractmethod
    def embed_batch(self, texts: List[str]) -> List[List[float]]:
        """Generate embeddings for multiple texts."""
        pass
    
    @property
    @abstractmethod
    def dimension(self) -> int:
        """Return embedding dimension."""
        pass


class OpenAIEmbeddings(EmbeddingProvider):
    """OpenAI-compatible embeddings (works with Snowflake Cortex)."""
    
    def __init__(self, model: str = "text-embedding-3-small"):
        self.model = model
        self._dimension = 1536  # Default for text-embedding-3-small
        self._client = None
    
    def _get_client(self):
        if self._client is None:
            try:
                import httpx
            except ImportError:
                logger.warning("httpx not installed, using hash-based fallback embeddings")
                return None
            
            api_key = os.environ.get("OPENAI_API_KEY", "")
            base_url = os.environ.get("OPENAI_BASE_URL", "https://api.openai.com/v1")
            
            # Adjust for embeddings endpoint
            if "cortex" in base_url.lower():
                # Snowflake Cortex doesn't support embeddings via OpenAI API
                # Fall back to simple hash-based embeddings
                logger.warning("Cortex doesn't support embeddings, using local fallback")
                return None
            
            self._client = httpx.Client(
                base_url=base_url,
                headers={"Authorization": f"Bearer {api_key}"},
                timeout=30.0
            )
        return self._client
    
    def embed(self, text: str) -> List[float]:
        client = self._get_client()
        if client is None:
            return self._fallback_embed(text)
        
        try:
            response = client.post(
                "/embeddings",
                json={"input": text, "model": self.model}
            )
            response.raise_for_status()
            return response.json()["data"][0]["embedding"]
        except Exception as e:
            logger.warning(f"OpenAI embedding failed: {e}, using fallback")
            return self._fallback_embed(text)
    
    def embed_batch(self, texts: List[str]) -> List[List[float]]:
        client = self._get_client()
        if client is None:
            return [self._fallback_embed(t) for t in texts]
        
        try:
            response = client.post(
                "/embeddings",
                json={"input": texts, "model": self.model}
            )
            response.raise_for_status()
            data = response.json()["data"]
            return [d["embedding"] for d in sorted(data, key=lambda x: x["index"])]
        except Exception as e:
            logger.warning(f"OpenAI batch embedding failed: {e}, using fallback")
            return [self._fallback_embed(t) for t in texts]
    
    def _fallback_embed(self, text: str) -> List[float]:
        """Simple hash-based embedding fallback."""
        # Create a deterministic pseudo-embedding from text hash
        import hashlib
        import struct
        
        # Hash the text
        h = hashlib.sha256(text.encode()).digest()
        
        # Extend to full dimension using repeated hashing
        embedding = []
        seed = h
        while len(embedding) < self._dimension:
            seed = hashlib.sha256(seed).digest()
            # Unpack as floats, normalize to [-1, 1]
            for i in range(0, len(seed), 4):
                if len(embedding) >= self._dimension:
                    break
                val = struct.unpack('f', seed[i:i+4])[0]
                # Normalize
                val = max(-1.0, min(1.0, val / 1e38))
                embedding.append(val)
        
        return embedding[:self._dimension]
    
    @property
    def dimension(self) -> int:
        return self._dimension


class LocalEmbeddings(EmbeddingProvider):
    """Local sentence-transformers embeddings."""
    
    def __init__(self, model_name: str = "all-MiniLM-L6-v2"):
        self.model_name = model_name
        self._model = None
        self._dimension = 384  # Default for MiniLM
    
    def _get_model(self):
        if self._model is None:
            try:
                from sentence_transformers import SentenceTransformer
                self._model = SentenceTransformer(self.model_name)
                self._dimension = self._model.get_sentence_embedding_dimension()
            except ImportError:
                logger.warning("sentence-transformers not installed, using hash fallback")
                return None
        return self._model
    
    def embed(self, text: str) -> List[float]:
        model = self._get_model()
        if model is None:
            return self._fallback_embed(text)
        return model.encode(text).tolist()
    
    def embed_batch(self, texts: List[str]) -> List[List[float]]:
        model = self._get_model()
        if model is None:
            return [self._fallback_embed(t) for t in texts]
        return model.encode(texts).tolist()
    
    def _fallback_embed(self, text: str) -> List[float]:
        """Hash-based fallback."""
        import hashlib
        import struct
        
        h = hashlib.sha256(text.encode()).digest()
        embedding = []
        seed = h
        while len(embedding) < self._dimension:
            seed = hashlib.sha256(seed).digest()
            for i in range(0, len(seed), 4):
                if len(embedding) >= self._dimension:
                    break
                val = struct.unpack('f', seed[i:i+4])[0]
                val = max(-1.0, min(1.0, val / 1e38))
                embedding.append(val)
        return embedding[:self._dimension]
    
    @property
    def dimension(self) -> int:
        return self._dimension


class CachedEmbeddings(EmbeddingProvider):
    """Wrapper that caches embeddings to disk."""
    
    def __init__(self, provider: EmbeddingProvider, cache_dir: str = ".embedding_cache"):
        self.provider = provider
        self.cache_dir = Path(cache_dir)
        self.cache_dir.mkdir(parents=True, exist_ok=True)
    
    def _cache_key(self, text: str) -> str:
        return hashlib.md5(text.encode()).hexdigest()
    
    def _cache_path(self, key: str) -> Path:
        return self.cache_dir / f"{key}.json"
    
    def embed(self, text: str) -> List[float]:
        key = self._cache_key(text)
        cache_path = self._cache_path(key)
        
        if cache_path.exists():
            with open(cache_path, 'r') as f:
                return json.load(f)
        
        embedding = self.provider.embed(text)
        
        with open(cache_path, 'w') as f:
            json.dump(embedding, f)
        
        return embedding
    
    def embed_batch(self, texts: List[str]) -> List[List[float]]:
        results = []
        uncached_texts = []
        uncached_indices = []
        
        for i, text in enumerate(texts):
            key = self._cache_key(text)
            cache_path = self._cache_path(key)
            
            if cache_path.exists():
                with open(cache_path, 'r') as f:
                    results.append(json.load(f))
            else:
                results.append(None)
                uncached_texts.append(text)
                uncached_indices.append(i)
        
        if uncached_texts:
            new_embeddings = self.provider.embed_batch(uncached_texts)
            
            for idx, embedding in zip(uncached_indices, new_embeddings):
                results[idx] = embedding
                key = self._cache_key(texts[idx])
                cache_path = self._cache_path(key)
                with open(cache_path, 'w') as f:
                    json.dump(embedding, f)
        
        return results
    
    @property
    def dimension(self) -> int:
        return self.provider.dimension


_default_provider: Optional[EmbeddingProvider] = None


def get_embedding_provider() -> EmbeddingProvider:
    """Get the default embedding provider."""
    global _default_provider
    
    if _default_provider is None:
        # Try to use local embeddings first (faster, no API calls)
        try:
            from sentence_transformers import SentenceTransformer
            _default_provider = CachedEmbeddings(LocalEmbeddings())
            logger.info("Using local sentence-transformers for embeddings")
        except ImportError:
            # Fall back to OpenAI-compatible
            _default_provider = CachedEmbeddings(OpenAIEmbeddings())
            logger.info("Using OpenAI-compatible embeddings")
    
    return _default_provider
