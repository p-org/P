"""
Vector Store for P Program Examples.

A simple in-memory vector store with persistence.
Supports similarity search using cosine similarity.
"""

import json
import logging
import math
from dataclasses import dataclass, asdict
from pathlib import Path
from typing import List, Dict, Any, Optional
import threading

logger = logging.getLogger(__name__)


@dataclass
class Document:
    """A document in the vector store."""
    id: str
    content: str
    embedding: List[float]
    metadata: Dict[str, Any]
    
    def to_dict(self) -> dict:
        return asdict(self)
    
    @classmethod
    def from_dict(cls, data: dict) -> "Document":
        return cls(**data)


@dataclass
class SearchResult:
    """A search result with relevance score."""
    document: Document
    score: float
    
    def to_dict(self) -> dict:
        return {
            "id": self.document.id,
            "content": self.document.content,
            "metadata": self.document.metadata,
            "score": self.score
        }


class VectorStore:
    """
    Simple in-memory vector store with persistence.
    
    Uses cosine similarity for search.
    Thread-safe for concurrent access.
    """
    
    def __init__(self, persist_path: Optional[str] = None):
        self.documents: Dict[str, Document] = {}
        self.persist_path = Path(persist_path) if persist_path else None
        self._lock = threading.RLock()
        
        if self.persist_path and self.persist_path.exists():
            self._load()
    
    def add(self, doc: Document) -> None:
        """Add a document to the store."""
        with self._lock:
            self.documents[doc.id] = doc
            self._save()
    
    def add_batch(self, docs: List[Document]) -> None:
        """Add multiple documents."""
        with self._lock:
            for doc in docs:
                self.documents[doc.id] = doc
            self._save()
    
    def get(self, doc_id: str) -> Optional[Document]:
        """Get a document by ID."""
        with self._lock:
            return self.documents.get(doc_id)
    
    def delete(self, doc_id: str) -> bool:
        """Delete a document by ID."""
        with self._lock:
            if doc_id in self.documents:
                del self.documents[doc_id]
                self._save()
                return True
            return False
    
    def search(
        self, 
        query_embedding: List[float], 
        top_k: int = 5,
        filter_metadata: Optional[Dict[str, Any]] = None
    ) -> List[SearchResult]:
        """
        Search for similar documents.
        
        Args:
            query_embedding: Query vector
            top_k: Number of results to return
            filter_metadata: Optional metadata filter (exact match)
            
        Returns:
            List of SearchResult sorted by relevance
        """
        with self._lock:
            results = []
            
            for doc in self.documents.values():
                # Apply metadata filter
                if filter_metadata:
                    match = all(
                        doc.metadata.get(k) == v 
                        for k, v in filter_metadata.items()
                    )
                    if not match:
                        continue
                
                # Calculate cosine similarity
                score = self._cosine_similarity(query_embedding, doc.embedding)
                results.append(SearchResult(document=doc, score=score))
            
            # Sort by score descending
            results.sort(key=lambda x: x.score, reverse=True)
            
            return results[:top_k]
    
    def search_by_metadata(
        self, 
        metadata: Dict[str, Any],
        limit: int = 10
    ) -> List[Document]:
        """Search documents by metadata only."""
        with self._lock:
            results = []
            for doc in self.documents.values():
                match = all(
                    doc.metadata.get(k) == v 
                    for k, v in metadata.items()
                )
                if match:
                    results.append(doc)
                    if len(results) >= limit:
                        break
            return results
    
    def list_all(self, limit: int = 100) -> List[Document]:
        """List all documents."""
        with self._lock:
            return list(self.documents.values())[:limit]
    
    def count(self) -> int:
        """Return number of documents."""
        with self._lock:
            return len(self.documents)
    
    def clear(self) -> None:
        """Clear all documents."""
        with self._lock:
            self.documents.clear()
            self._save()
    
    def _cosine_similarity(self, vec1: List[float], vec2: List[float]) -> float:
        """Calculate cosine similarity between two vectors."""
        if len(vec1) != len(vec2):
            return 0.0
        
        dot_product = sum(a * b for a, b in zip(vec1, vec2))
        magnitude1 = math.sqrt(sum(a * a for a in vec1))
        magnitude2 = math.sqrt(sum(b * b for b in vec2))
        
        if magnitude1 == 0 or magnitude2 == 0:
            return 0.0
        
        return dot_product / (magnitude1 * magnitude2)
    
    def _save(self) -> None:
        """Persist to disk."""
        if self.persist_path is None:
            return
        
        try:
            self.persist_path.parent.mkdir(parents=True, exist_ok=True)
            data = [doc.to_dict() for doc in self.documents.values()]
            with open(self.persist_path, 'w') as f:
                json.dump(data, f)
        except Exception as e:
            logger.error(f"Failed to save vector store: {e}")
    
    def _load(self) -> None:
        """Load from disk."""
        if self.persist_path is None or not self.persist_path.exists():
            return
        
        try:
            with open(self.persist_path, 'r') as f:
                data = json.load(f)
            
            for doc_data in data:
                doc = Document.from_dict(doc_data)
                self.documents[doc.id] = doc
            
            logger.info(f"Loaded {len(self.documents)} documents from {self.persist_path}")
        except Exception as e:
            logger.error(f"Failed to load vector store: {e}")
