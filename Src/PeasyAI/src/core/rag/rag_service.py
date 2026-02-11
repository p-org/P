"""
RAG Service for P Code Generation.

Provides context-enriched prompts using similar examples
from the P program corpus.
"""

import os
import logging
from dataclasses import dataclass, field
from pathlib import Path
from typing import List, Dict, Any, Optional

from .p_corpus import PCorpus, PExample
from .vector_store import SearchResult

logger = logging.getLogger(__name__)


@dataclass
class RAGContext:
    """Context retrieved from the corpus."""
    examples: List[Dict[str, Any]] = field(default_factory=list)
    documentation: List[str] = field(default_factory=list)
    syntax_hints: List[str] = field(default_factory=list)
    
    def to_prompt_section(self) -> str:
        """Convert to a prompt section."""
        sections = []
        
        if self.examples:
            sections.append("## Relevant P Code Examples")
            for i, ex in enumerate(self.examples, 1):
                sections.append(f"\n### Example {i}: {ex.get('name', 'Unknown')}")
                if ex.get('description'):
                    sections.append(f"Description: {ex['description']}")
                sections.append(f"```p\n{ex.get('code', '')}\n```")
        
        if self.documentation:
            sections.append("\n## P Language Documentation")
            for doc in self.documentation:
                sections.append(doc)
        
        if self.syntax_hints:
            sections.append("\n## Syntax Hints")
            for hint in self.syntax_hints:
                sections.append(f"- {hint}")
        
        return "\n".join(sections)


class RAGService:
    """
    RAG Service for enriching prompts with P examples.
    
    Usage:
        rag = RAGService()
        rag.index_p_repo("/path/to/P")  # Index P repository
        
        # Get context for machine generation
        context = rag.get_machine_context("distributed lock manager")
        prompt = f"{context.to_prompt_section()}\n\nGenerate machine: {description}"
    """
    
    def __init__(
        self, 
        corpus_path: Optional[str] = None,
        auto_index: bool = True
    ):
        # Default corpus path
        if corpus_path is None:
            corpus_path = os.environ.get(
                "P_CORPUS_PATH",
                str(Path(__file__).parent.parent.parent.parent / ".p_corpus")
            )
        
        self.corpus = PCorpus(store_path=corpus_path)
        self._indexed = False
        
        # Try to auto-index P repository
        if auto_index and self.corpus.count() == 0:
            self._auto_index()
    
    def _auto_index(self) -> None:
        """Try to automatically index P examples."""
        # Check for P repo environment variable
        p_repo = os.environ.get("P_REPO_PATH")
        if p_repo and Path(p_repo).exists():
            logger.info(f"Auto-indexing P repository: {p_repo}")
            self.corpus.index_p_repo(p_repo)
            self._indexed = True
            return
        
        # Try common locations
        common_paths = [
            Path(__file__).parent.parent.parent.parent.parent.parent,  # P repo root
            Path.home() / "P",
            Path("/Users/adesai/workspace/public/P"),  # Specific user path
        ]
        
        for path in common_paths:
            tutorial_path = path / "Tutorial"
            if tutorial_path.exists():
                logger.info(f"Auto-indexing P tutorials from: {path}")
                self.corpus.index_p_repo(str(path))
                self._indexed = True
                return
        
        logger.info("No P repository found for auto-indexing")
    
    def index_p_repo(self, repo_path: str) -> int:
        """Manually index P repository."""
        count = self.corpus.index_p_repo(repo_path)
        self._indexed = True
        return count
    
    def index_directory(self, dir_path: str) -> int:
        """Index P files in a directory."""
        return self.corpus.index_directory(dir_path)
    
    def index_file(self, file_path: str) -> int:
        """Index a single P file."""
        return self.corpus.index_p_file(file_path)
    
    def add_example(self, example: PExample) -> None:
        """Add a custom example."""
        self.corpus.add_example(example)
    
    def get_machine_context(
        self, 
        description: str,
        design_doc: Optional[str] = None,
        num_examples: int = 3
    ) -> RAGContext:
        """
        Get context for generating a machine.
        
        Args:
            description: Machine description or name
            design_doc: Optional design document for more context
            num_examples: Number of examples to retrieve
            
        Returns:
            RAGContext with relevant examples
        """
        context = RAGContext()
        
        # Search for similar machines
        query = f"machine state machine {description}"
        if design_doc:
            # Extract key terms from design doc
            query += f" {self._extract_keywords(design_doc)}"
        
        results = self.corpus.search(query, top_k=num_examples, category="machine")
        
        for result in results:
            if result.score > 0.3:  # Minimum relevance threshold
                context.examples.append({
                    "name": result.document.metadata.get("name"),
                    "description": result.document.metadata.get("description"),
                    "code": result.document.metadata.get("code"),
                    "score": result.score
                })
        
        # Add syntax hints
        context.syntax_hints = [
            "Variables must be declared at the start of functions",
            "Single-field tuples require trailing comma: (field = value,)",
            "Use 'this' to reference the current machine",
            "State machines use 'start state' for the initial state",
        ]
        
        return context
    
    def get_spec_context(
        self, 
        description: str,
        machines: Optional[List[str]] = None,
        num_examples: int = 3
    ) -> RAGContext:
        """Get context for generating a specification."""
        context = RAGContext()
        
        query = f"specification monitor safety {description}"
        if machines:
            query += f" {' '.join(machines)}"
        
        results = self.corpus.search(query, top_k=num_examples, category="spec")
        
        for result in results:
            if result.score > 0.3:
                context.examples.append({
                    "name": result.document.metadata.get("name"),
                    "description": result.document.metadata.get("description"),
                    "code": result.document.metadata.get("code"),
                    "score": result.score
                })
        
        context.syntax_hints = [
            "Specs observe events, not machines directly",
            "Use 'hot state' for liveness properties",
            "Use 'cold state' for states that should eventually be left",
        ]
        
        return context
    
    def get_test_context(
        self, 
        description: str,
        machines: Optional[List[str]] = None,
        num_examples: int = 3
    ) -> RAGContext:
        """Get context for generating a test."""
        context = RAGContext()
        
        query = f"test driver scenario {description}"
        if machines:
            query += f" {' '.join(machines)}"
        
        results = self.corpus.search(query, top_k=num_examples, category="test")
        
        for result in results:
            if result.score > 0.3:
                context.examples.append({
                    "name": result.document.metadata.get("name"),
                    "description": result.document.metadata.get("description"),
                    "code": result.document.metadata.get("code"),
                    "score": result.score
                })
        
        context.syntax_hints = [
            "Test syntax: test TestName [main=MainMachine]: (union SystemConfig, { TestMachine });",
            "Tests require a main machine and system configuration",
        ]
        
        return context
    
    def get_types_context(
        self, 
        description: str,
        num_examples: int = 3
    ) -> RAGContext:
        """Get context for generating types and events."""
        context = RAGContext()
        
        query = f"type event enum definition {description}"
        results = self.corpus.search(query, top_k=num_examples, category="types")
        
        for result in results:
            if result.score > 0.3:
                context.examples.append({
                    "name": result.document.metadata.get("name"),
                    "description": result.document.metadata.get("description"),
                    "code": result.document.metadata.get("code"),
                    "score": result.score
                })
        
        context.syntax_hints = [
            "Named tuple types: type tRequest = (client: machine, value: int);",
            "Events can have optional payloads: event eRequest: tRequest;",
            "Enums: enum Status { SUCCESS, FAILURE }",
        ]
        
        return context
    
    def get_protocol_examples(self, protocol_name: str) -> RAGContext:
        """Get examples for a specific protocol type."""
        context = RAGContext()
        
        # Search for protocol-related examples
        results = self.corpus.get_protocol_examples(protocol_name, top_k=5)
        
        for result in results:
            context.examples.append({
                "name": result.document.metadata.get("name"),
                "description": result.document.metadata.get("description"),
                "code": result.document.metadata.get("code"),
                "project": result.document.metadata.get("project_name"),
                "score": result.score
            })
        
        return context
    
    def search(self, query: str, top_k: int = 5) -> List[SearchResult]:
        """General search across all examples."""
        return self.corpus.search(query, top_k=top_k)
    
    def get_documentation(self, topic: str) -> List[str]:
        """Get documentation for a P language topic."""
        # Built-in documentation
        docs = {
            "state_machine": [
                "## P State Machines",
                "State machines in P are defined with the `machine` keyword.",
                "Each machine has states, and transitions between states happen via `goto`.",
                "The `start state` defines the initial state of the machine.",
            ],
            "events": [
                "## P Events",
                "Events are the primary communication mechanism in P.",
                "Send events with: `send target, eventName, payload;`",
                "Handle events in states with: `on eventName do HandlerFunction;`",
            ],
            "types": [
                "## P Types",
                "P supports: int, bool, string, machine, event, any",
                "Named tuples: `type tRequest = (field1: int, field2: string);`",
                "Collections: `seq[int]`, `set[int]`, `map[int, string]`",
            ],
        }
        
        return docs.get(topic, [f"No documentation found for: {topic}"])
    
    def get_stats(self) -> Dict[str, Any]:
        """Get statistics about the corpus."""
        return {
            "total_examples": self.corpus.count(),
            "indexed": self._indexed,
        }
    
    def _extract_keywords(self, text: str, max_keywords: int = 10) -> str:
        """Extract keywords from text."""
        import re
        # Remove common words and extract meaningful terms
        words = re.findall(r'\b[A-Za-z]{3,}\b', text.lower())
        
        # Common words to skip
        stopwords = {
            'the', 'and', 'for', 'that', 'with', 'this', 'from', 'are',
            'will', 'can', 'has', 'have', 'should', 'when', 'each', 'all'
        }
        
        keywords = [w for w in words if w not in stopwords]
        
        # Get unique keywords, preserving order
        seen = set()
        unique = []
        for w in keywords:
            if w not in seen:
                seen.add(w)
                unique.append(w)
        
        return ' '.join(unique[:max_keywords])


# Singleton instance
_rag_service: Optional[RAGService] = None


def get_rag_service() -> RAGService:
    """Get the singleton RAG service instance."""
    global _rag_service
    if _rag_service is None:
        _rag_service = RAGService()
    return _rag_service
