"""
P Program Corpus Manager.

Handles indexing and searching of P program examples,
documentation, and tutorials.
"""

import os
import re
import logging
from dataclasses import dataclass, field
from pathlib import Path
from typing import List, Dict, Any, Optional, Set
import hashlib

from .embeddings import EmbeddingProvider, get_embedding_provider
from .vector_store import VectorStore, Document, SearchResult

logger = logging.getLogger(__name__)


@dataclass
class PExample:
    """A P code example with metadata."""
    id: str
    name: str
    description: str
    code: str
    category: str  # "machine", "event", "spec", "test", "full_project"
    tags: List[str] = field(default_factory=list)
    source_file: Optional[str] = None
    project_name: Optional[str] = None
    
    def to_document_content(self) -> str:
        """Convert to searchable content."""
        parts = [
            f"Name: {self.name}",
            f"Category: {self.category}",
            f"Description: {self.description}",
            f"Tags: {', '.join(self.tags)}",
            "",
            "Code:",
            self.code
        ]
        return "\n".join(parts)


class PCorpus:
    """
    Manager for the P program corpus.
    
    Provides:
    - Indexing of P programs and documentation
    - Semantic search for similar examples
    - Category-based filtering
    """
    
    def __init__(
        self, 
        store_path: str = ".p_corpus",
        embedding_provider: Optional[EmbeddingProvider] = None
    ):
        self.store_path = Path(store_path)
        self.store_path.mkdir(parents=True, exist_ok=True)
        
        self.embeddings = embedding_provider or get_embedding_provider()
        self.vector_store = VectorStore(
            persist_path=str(self.store_path / "vectors.json")
        )
        
        # Track indexed files to avoid re-indexing
        self._indexed_files: Set[str] = set()
        self._load_indexed_files()
    
    def index_p_file(self, file_path: str, project_name: Optional[str] = None) -> int:
        """
        Index a single P file.
        
        Returns number of examples indexed.
        """
        file_path = Path(file_path)
        if not file_path.exists():
            logger.warning(f"File not found: {file_path}")
            return 0
        
        # Check if already indexed
        file_hash = self._file_hash(file_path)
        if file_hash in self._indexed_files:
            logger.debug(f"File already indexed: {file_path}")
            return 0
        
        try:
            content = file_path.read_text()
        except Exception as e:
            logger.error(f"Failed to read {file_path}: {e}")
            return 0
        
        examples = self._extract_examples(content, str(file_path), project_name)
        
        for example in examples:
            self._index_example(example)
        
        self._indexed_files.add(file_hash)
        self._save_indexed_files()
        
        logger.info(f"Indexed {len(examples)} examples from {file_path}")
        return len(examples)
    
    def index_directory(self, dir_path: str, project_name: Optional[str] = None) -> int:
        """
        Index all P files in a directory recursively.
        
        Returns total number of examples indexed.
        """
        dir_path = Path(dir_path)
        if not dir_path.exists():
            logger.warning(f"Directory not found: {dir_path}")
            return 0
        
        total = 0
        for p_file in dir_path.rglob("*.p"):
            # Skip generated files
            if "PGenerated" in str(p_file) or "PCheckerOutput" in str(p_file):
                continue
            
            # Infer project name from directory structure
            proj = project_name
            if proj is None:
                # Try to find .pproj file
                for parent in p_file.parents:
                    pproj_files = list(parent.glob("*.pproj"))
                    if pproj_files:
                        proj = pproj_files[0].stem
                        break
            
            total += self.index_p_file(str(p_file), proj)
        
        return total
    
    def index_p_repo(self, repo_path: str) -> int:
        """
        Index the official P repository tutorials and examples.
        
        Args:
            repo_path: Path to the P repository root
            
        Returns:
            Number of examples indexed
        """
        repo_path = Path(repo_path)
        total = 0
        
        # Index tutorial examples
        tutorial_dirs = [
            "Tutorial/1_ClientServer",
            "Tutorial/2_TwoPhaseCommit",
            "Tutorial/3_PingPong",
            "Tutorial/4_FailureDetector",
            "Tutorial/5_Timer",
            "Tutorial/6_DistributedReplication",
            "Tutorial/7_ForeignTypes",
        ]
        
        for tutorial_dir in tutorial_dirs:
            full_path = repo_path / tutorial_dir
            if full_path.exists():
                total += self.index_directory(str(full_path))
        
        # Index test examples
        test_dirs = [
            "Tst/RegressionTests",
        ]
        
        for test_dir in test_dirs:
            full_path = repo_path / test_dir
            if full_path.exists():
                total += self.index_directory(str(full_path))
        
        return total
    
    def add_example(self, example: PExample) -> None:
        """Add a manually created example."""
        self._index_example(example)
    
    def search(
        self, 
        query: str, 
        top_k: int = 5,
        category: Optional[str] = None
    ) -> List[SearchResult]:
        """
        Search for similar P examples.
        
        Args:
            query: Natural language query or code snippet
            top_k: Number of results to return
            category: Optional category filter
            
        Returns:
            List of SearchResult
        """
        query_embedding = self.embeddings.embed(query)
        
        filter_metadata = {}
        if category:
            filter_metadata["category"] = category
        
        return self.vector_store.search(
            query_embedding, 
            top_k=top_k,
            filter_metadata=filter_metadata if filter_metadata else None
        )
    
    def search_by_tags(self, tags: List[str], limit: int = 10) -> List[Document]:
        """Search examples by tags."""
        results = []
        for doc in self.vector_store.list_all(limit=1000):
            doc_tags = doc.metadata.get("tags", [])
            if any(tag in doc_tags for tag in tags):
                results.append(doc)
                if len(results) >= limit:
                    break
        return results
    
    def get_examples_by_category(self, category: str, limit: int = 10) -> List[Document]:
        """Get examples by category."""
        return self.vector_store.search_by_metadata(
            {"category": category},
            limit=limit
        )
    
    def get_similar_machines(self, description: str, top_k: int = 3) -> List[SearchResult]:
        """Find similar machine implementations."""
        return self.search(description, top_k=top_k, category="machine")
    
    def get_similar_specs(self, description: str, top_k: int = 3) -> List[SearchResult]:
        """Find similar specification examples."""
        return self.search(description, top_k=top_k, category="spec")
    
    def get_protocol_examples(self, protocol_name: str, top_k: int = 5) -> List[SearchResult]:
        """Find examples related to a protocol."""
        query = f"protocol {protocol_name} distributed system P language"
        return self.search(query, top_k=top_k)
    
    def count(self) -> int:
        """Return number of indexed examples."""
        return self.vector_store.count()
    
    def _extract_examples(
        self, 
        content: str, 
        source_file: str,
        project_name: Optional[str]
    ) -> List[PExample]:
        """Extract examples from P file content."""
        examples = []
        
        # Extract machines
        machine_pattern = r'machine\s+(\w+)\s*\{(.*?)\n\}'
        for match in re.finditer(machine_pattern, content, re.DOTALL):
            machine_name = match.group(1)
            machine_code = f"machine {machine_name} {{{match.group(2)}\n}}"
            
            # Extract description from comments
            desc = self._extract_description(content, match.start())
            
            # Infer tags from machine content
            tags = self._infer_tags(machine_code)
            
            examples.append(PExample(
                id=f"machine_{machine_name}_{hashlib.md5(machine_code.encode()).hexdigest()[:8]}",
                name=machine_name,
                description=desc or f"Machine {machine_name}",
                code=machine_code,
                category="machine",
                tags=tags,
                source_file=source_file,
                project_name=project_name
            ))
        
        # Extract specs
        spec_pattern = r'spec\s+(\w+)\s+observes\s+[^{]+\{(.*?)\n\}'
        for match in re.finditer(spec_pattern, content, re.DOTALL):
            spec_name = match.group(1)
            spec_code = match.group(0)
            desc = self._extract_description(content, match.start())
            
            examples.append(PExample(
                id=f"spec_{spec_name}_{hashlib.md5(spec_code.encode()).hexdigest()[:8]}",
                name=spec_name,
                description=desc or f"Safety specification {spec_name}",
                code=spec_code,
                category="spec",
                tags=["safety", "specification", "monitor"],
                source_file=source_file,
                project_name=project_name
            ))
        
        # Extract test definitions
        test_pattern = r'(test\s+\w+\s*\[.*?\].*?;)'
        for match in re.finditer(test_pattern, content, re.DOTALL):
            test_code = match.group(1)
            test_name_match = re.search(r'test\s+(\w+)', test_code)
            test_name = test_name_match.group(1) if test_name_match else "Unknown"
            
            examples.append(PExample(
                id=f"test_{test_name}_{hashlib.md5(test_code.encode()).hexdigest()[:8]}",
                name=test_name,
                description=f"Test case {test_name}",
                code=test_code,
                category="test",
                tags=["test", "verification"],
                source_file=source_file,
                project_name=project_name
            ))
        
        # If it's a full file with events/types, index it as documentation
        if 'event ' in content or 'type ' in content:
            file_name = Path(source_file).stem
            
            # Extract just the types/events section
            types_events = self._extract_types_events(content)
            if types_events:
                examples.append(PExample(
                    id=f"types_{file_name}_{hashlib.md5(types_events.encode()).hexdigest()[:8]}",
                    name=f"{file_name} Types and Events",
                    description=f"Type and event definitions from {file_name}",
                    code=types_events,
                    category="types",
                    tags=["types", "events", "definitions"],
                    source_file=source_file,
                    project_name=project_name
                ))
        
        # Index full test driver files as complete examples.
        # Test drivers are best understood as a whole — the machine creation order,
        # setup event usage, and wiring patterns are only visible in context.
        is_test_file = (
            'PTst/' in source_file or '/PTst/' in source_file or
            'TestDriver' in Path(source_file).stem or
            'Test' in Path(source_file).stem
        )
        if is_test_file and 'machine ' in content:
            file_name = Path(source_file).stem
            tags = self._infer_tags(content)
            tags.append("test-driver-full")
            tags.append("machine-wiring")
            
            # Extract a description from comments at the top of the file
            desc = self._extract_file_description(content)
            
            examples.append(PExample(
                id=f"testdriver_{file_name}_{hashlib.md5(content.encode()).hexdigest()[:8]}",
                name=f"{file_name} (Full Test Driver)",
                description=desc or f"Complete test driver showing machine initialization and wiring patterns from {file_name}",
                code=content,
                category="test",
                tags=tags,
                source_file=source_file,
                project_name=project_name
            ))
        
        # Index files with BEST PRACTICE annotations as best-practice examples
        if 'BEST PRACTICE' in content:
            file_name = Path(source_file).stem
            tags = self._infer_tags(content)
            tags.append("best-practice-guide")
            
            examples.append(PExample(
                id=f"bestpractice_{file_name}_{hashlib.md5(content.encode()).hexdigest()[:8]}",
                name=f"{file_name} (Best Practices)",
                description=f"Annotated best practices from {file_name}",
                code=content,
                category="machine",
                tags=tags,
                source_file=source_file,
                project_name=project_name
            ))
        
        return examples
    
    def _extract_description(self, content: str, position: int) -> Optional[str]:
        """Extract description from comments before a definition."""
        # Look for comment block before position
        before = content[:position]
        lines = before.split('\n')[-5:]  # Last 5 lines
        
        comments = []
        for line in reversed(lines):
            stripped = line.strip()
            if stripped.startswith('//'):
                comments.insert(0, stripped[2:].strip())
            elif stripped.startswith('/*'):
                # Block comment
                comment_text = stripped[2:].rstrip('*/').strip()
                if comment_text:
                    comments.insert(0, comment_text)
            elif stripped:
                break  # Non-comment, non-empty line
        
        return ' '.join(comments) if comments else None
    
    def _extract_file_description(self, content: str) -> Optional[str]:
        """Extract description from the header comments at the top of a file."""
        lines = content.split('\n')
        comments = []
        for line in lines[:20]:  # Look at first 20 lines
            stripped = line.strip()
            if stripped.startswith('//'):
                text = stripped[2:].strip()
                # Skip separator lines like "// ========="
                if text and not all(c in '=-*/' for c in text):
                    comments.append(text)
            elif stripped.startswith('/*'):
                text = stripped[2:].rstrip('*/').strip()
                if text:
                    comments.append(text)
            elif stripped and not stripped.startswith('*'):
                break
        return ' '.join(comments) if comments else None
    
    def _infer_tags(self, code: str) -> List[str]:
        """Infer tags from code content."""
        tags = []
        
        if 'send ' in code:
            tags.append("message-passing")
        if 'goto ' in code:
            tags.append("state-machine")
        if 'defer ' in code:
            tags.append("deferred-events")
        if 'raise ' in code:
            tags.append("internal-events")
        if 'new ' in code:
            tags.append("machine-creation")
        if 'foreach' in code:
            tags.append("iteration")
        if 'map[' in code or 'seq[' in code:
            tags.append("collections")
        if '$' in code or 'choose(' in code:
            tags.append("nondeterminism")
        if 'assert ' in code:
            tags.append("assertions")
        
        # Enhanced pattern detection
        if 'ignore ' in code:
            tags.append("ignore-pattern")
        if re.search(r'while\s*\(.*sizeof.*\)\s*\{.*send\b', code, re.DOTALL):
            tags.append("broadcast-pattern")
        if re.search(r'eSetup\w+|eConfig\w+|eInform\w+|eInit\w+', code):
            tags.append("setup-event")
        if re.search(r'BEST\s+PRACTICE', code):
            tags.append("best-practice")
        if re.search(r'ANTI.?PATTERN', code):
            tags.append("anti-pattern")
        
        # Test driver detection
        if re.search(r'machine\s+\w*(?:Test|Scenario|Driver)\w*', code):
            tags.append("test-driver")
        if 'fun SetUp' in code or 'fun Setup' in code:
            tags.append("test-setup")
        
        # Distributed protocol patterns
        if 'majority' in code.lower() or 'quorum' in code.lower():
            tags.append("quorum-pattern")
        if re.search(r'seq\[machine\]|set\[machine\]', code):
            tags.append("machine-collection")
        if 'allComponents' in code or 'allMachines' in code or 'all_components' in code:
            tags.append("component-list")
        
        return tags
    
    def _extract_types_events(self, content: str) -> str:
        """Extract just types and events from content."""
        lines = []
        
        for line in content.split('\n'):
            stripped = line.strip()
            if (stripped.startswith('type ') or 
                stripped.startswith('event ') or
                stripped.startswith('enum ') or
                stripped.startswith('//')):
                lines.append(line)
        
        return '\n'.join(lines)
    
    def _index_example(self, example: PExample) -> None:
        """Index a single example."""
        content = example.to_document_content()
        embedding = self.embeddings.embed(content)
        
        doc = Document(
            id=example.id,
            content=content,
            embedding=embedding,
            metadata={
                "name": example.name,
                "category": example.category,
                "tags": example.tags,
                "source_file": example.source_file,
                "project_name": example.project_name,
                "code": example.code,
                "description": example.description
            }
        )
        
        self.vector_store.add(doc)
    
    def _file_hash(self, file_path: Path) -> str:
        """Get hash of file for tracking."""
        content = file_path.read_bytes()
        return hashlib.md5(content).hexdigest()
    
    def _load_indexed_files(self) -> None:
        """Load list of indexed files."""
        index_file = self.store_path / "indexed_files.json"
        if index_file.exists():
            import json
            with open(index_file, 'r') as f:
                self._indexed_files = set(json.load(f))
    
    def _save_indexed_files(self) -> None:
        """Save list of indexed files."""
        import json
        index_file = self.store_path / "indexed_files.json"
        with open(index_file, 'w') as f:
            json.dump(list(self._indexed_files), f)
