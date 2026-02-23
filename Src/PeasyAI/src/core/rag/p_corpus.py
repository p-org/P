"""
P Program Corpus Manager.

Handles indexing and searching of P program examples,
documentation, and tutorials.

All indexed content is sourced from the bundled resources/ directory
within the PeasyAI package — no external repo paths required.
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

# ── Locate the resources directory bundled with this package ──────────
_PACKAGE_ROOT = Path(__file__).parent.parent.parent.parent  # PeasyAI/
RESOURCES_DIR = _PACKAGE_ROOT / "resources"
RAG_EXAMPLES_DIR = RESOURCES_DIR / "rag_examples"
CONTEXT_FILES_DIR = RESOURCES_DIR / "context_files"


@dataclass
class PExample:
    """A P code example with metadata."""
    id: str
    name: str
    description: str
    code: str
    category: str  # "machine", "spec", "test", "types", "documentation", "full_project"
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
            self.code,
        ]
        return "\n".join(parts)


class PCorpus:
    """
    Manager for the P program corpus.

    All content is sourced from the bundled ``resources/`` directory so that
    PeasyAI works as a standalone MCP installation without needing the full
    P repository.

    Provides:
    - Indexing of P programs, examples, and documentation from resources/
    - Semantic search for similar examples
    - Category-based filtering
    """

    def __init__(
        self,
        store_path: str = ".p_corpus",
        embedding_provider: Optional[EmbeddingProvider] = None,
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

    # ── Public API ─────────────────────────────────────────────────────

    def index_p_file(
        self, file_path: str, project_name: Optional[str] = None
    ) -> int:
        """Index a single P file.  Returns number of examples indexed."""
        file_path = Path(file_path)
        if not file_path.exists():
            logger.warning(f"File not found: {file_path}")
            return 0

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

    def index_directory(
        self, dir_path: str, project_name: Optional[str] = None
    ) -> int:
        """Index all *.p files in a directory recursively."""
        dir_path = Path(dir_path)
        if not dir_path.exists():
            logger.warning(f"Directory not found: {dir_path}")
            return 0

        total = 0
        for p_file in dir_path.rglob("*.p"):
            if "PGenerated" in str(p_file) or "PCheckerOutput" in str(p_file):
                continue
            proj = project_name
            if proj is None:
                for parent in p_file.parents:
                    pproj_files = list(parent.glob("*.pproj"))
                    if pproj_files:
                        proj = pproj_files[0].stem
                        break
            total += self.index_p_file(str(p_file), proj)
        return total

    def index_bundled_resources(self) -> int:
        """
        **Primary indexing entry-point.**

        Index everything shipped in the ``resources/`` directory:
          1. rag_examples/  – curated P tutorial & protocol code
          2. context_files/  – language guides as documentation entries
          3. context_files/p_documentation_reference.txt – patterns & idioms

        This method is self-contained and does NOT require the broader
        P repository to be present on disk.
        """
        total = 0

        # ── 1. Curated P code examples ─────────────────────────────
        if RAG_EXAMPLES_DIR.exists():
            for project_dir in sorted(RAG_EXAMPLES_DIR.iterdir()):
                if project_dir.is_dir():
                    total += self.index_directory(
                        str(project_dir), project_name=project_dir.name
                    )

        # ── 2. Documentation reference file ────────────────────────
        doc_ref = CONTEXT_FILES_DIR / "p_documentation_reference.txt"
        if doc_ref.exists():
            total += self._index_documentation_file(doc_ref)

        # ── 3. Modular language guides as documentation ────────────
        modular_dir = CONTEXT_FILES_DIR / "modular"
        if modular_dir.exists():
            for guide_file in sorted(modular_dir.glob("*.txt")):
                total += self._index_guide_file(guide_file)

        # ── 4. P nuances / pitfalls ────────────────────────────────
        nuances = CONTEXT_FILES_DIR / "p_nuances.txt"
        if nuances.exists():
            total += self._index_guide_file(nuances)

        logger.info(f"Indexed {total} entries from bundled resources")
        return total

    # Keep backward compat — callers may still call index_p_repo, but
    # now it simply delegates to the bundled resources.
    def index_p_repo(self, repo_path: str) -> int:  # noqa: ARG002
        """
        Index P examples.

        For standalone installs this indexes the bundled resources/.
        If additional P files exist at *repo_path* they are indexed too,
        but the bundled resources are always the primary source.
        """
        total = self.index_bundled_resources()

        # Optionally index extra P files if a repo_path is given and valid
        repo = Path(repo_path)
        extra_dirs = [
            repo / "Tutorial",
            repo / "Tst" / "PortfolioTests",
            repo / "Tst" / "RegressionTests",
        ]
        for d in extra_dirs:
            if d.exists():
                total += self.index_directory(str(d))

        return total

    def add_example(self, example: PExample) -> None:
        """Add a manually created example."""
        self._index_example(example)

    def search(
        self,
        query: str,
        top_k: int = 5,
        category: Optional[str] = None,
    ) -> List[SearchResult]:
        """Semantic search for similar P examples."""
        query_embedding = self.embeddings.embed(query)
        filter_metadata = {}
        if category:
            filter_metadata["category"] = category
        return self.vector_store.search(
            query_embedding,
            top_k=top_k,
            filter_metadata=filter_metadata if filter_metadata else None,
        )

    def search_faceted(
        self,
        query: str,
        top_k: int = 5,
        categories: Optional[List[str]] = None,
        protocols: Optional[List[str]] = None,
        constructs: Optional[List[str]] = None,
        patterns: Optional[List[str]] = None,
    ) -> List[SearchResult]:
        """
        Faceted retrieval with lightweight metadata-aware reranking.

        This complements semantic similarity with explicit protocol/construct/pattern
        matches so RAG can generalize beyond protocol-only retrieval.
        """
        # Pull a wider candidate set first, then rerank/filter.
        base_k = max(top_k * 6, 30)
        candidates = self.search(query, top_k=base_k)

        protocol_set = {p.lower() for p in (protocols or [])}
        construct_set = {c.lower() for c in (constructs or [])}
        pattern_set = {p.lower() for p in (patterns or [])}
        category_set = {c.lower() for c in (categories or [])}

        reranked: List[tuple[SearchResult, float]] = []
        for result in candidates:
            meta = result.document.metadata
            category = str(meta.get("category", "")).lower()
            if category_set and category not in category_set:
                continue

            score = result.score
            doc_protocols = {
                str(x).lower() for x in (meta.get("protocol_tags") or [])
            }
            doc_constructs = {
                str(x).lower() for x in (meta.get("construct_tags") or [])
            }
            doc_patterns = {
                str(x).lower() for x in (meta.get("pattern_tags") or [])
            }
            doc_intents = {
                str(x).lower() for x in (meta.get("intent_tags") or [])
            }

            # Explicit facet matches boost ranking.
            if protocol_set:
                hit = len(protocol_set.intersection(doc_protocols))
                if hit > 0:
                    score += 0.15 + (0.03 * min(hit, 3))
                elif category not in ("documentation", "manual", "tutorial", "advanced", "getting_started"):
                    score -= 0.08

            if construct_set:
                hit = len(construct_set.intersection(doc_constructs))
                if hit > 0:
                    score += 0.10 + (0.02 * min(hit, 4))

            if pattern_set:
                hit = len(pattern_set.intersection(doc_patterns))
                if hit > 0:
                    score += 0.10 + (0.02 * min(hit, 4))

            # Prefer executable code over prose-only docs in most cases.
            has_code = bool(str(meta.get("code") or "").strip())
            if has_code:
                score += 0.04
            elif category in ("documentation", "manual", "tutorial", "advanced", "getting_started"):
                score -= 0.06

            # Small bonus for test/spec intents when explicitly asked.
            if construct_set and ("monitor" in construct_set or "hot-state" in construct_set):
                if "liveness" in doc_intents or "safety" in doc_intents:
                    score += 0.03

            reranked.append((result, score))

        reranked.sort(key=lambda x: x[1], reverse=True)
        return [r for r, _ in reranked[:top_k]]

    def search_by_tags(self, tags: List[str], limit: int = 10) -> List[Document]:
        results = []
        for doc in self.vector_store.list_all(limit=1000):
            doc_tags = doc.metadata.get("tags", [])
            if any(tag in doc_tags for tag in tags):
                results.append(doc)
                if len(results) >= limit:
                    break
        return results

    def get_examples_by_category(
        self, category: str, limit: int = 10
    ) -> List[Document]:
        return self.vector_store.search_by_metadata(
            {"category": category}, limit=limit
        )

    def get_similar_machines(
        self, description: str, top_k: int = 3
    ) -> List[SearchResult]:
        return self.search(description, top_k=top_k, category="machine")

    def get_similar_specs(
        self, description: str, top_k: int = 3
    ) -> List[SearchResult]:
        return self.search(description, top_k=top_k, category="spec")

    def get_protocol_examples(
        self, protocol_name: str, top_k: int = 5
    ) -> List[SearchResult]:
        query = f"protocol {protocol_name} distributed system P language"
        return self.search(query, top_k=top_k)

    def count(self) -> int:
        return self.vector_store.count()

    def count_by_category(self) -> Dict[str, int]:
        counts: Dict[str, int] = {}
        for doc in self.vector_store.list_all(limit=10000):
            cat = doc.metadata.get("category", "unknown")
            counts[cat] = counts.get(cat, 0) + 1
        return counts

    def has_facet_schema(self, sample_size: int = 200) -> bool:
        """
        Check whether indexed documents contain required facet metadata fields.

        Returns True only when sampled non-documentation entries include all
        expected facet keys.
        """
        docs = self.vector_store.list_all(limit=sample_size)
        if not docs:
            return False

        required = {"protocol_tags", "construct_tags", "pattern_tags", "intent_tags"}
        checked = 0
        for doc in docs:
            meta = doc.metadata or {}
            category = str(meta.get("category", "")).lower()
            if category in ("documentation", "manual", "tutorial", "advanced", "getting_started"):
                continue
            checked += 1
            if not required.issubset(set(meta.keys())):
                return False
        return checked > 0

    def rebuild_bundled_index(self) -> int:
        """
        Clear persisted corpus and rebuild exclusively from bundled resources.
        """
        self.vector_store.clear()
        self._indexed_files.clear()
        self._save_indexed_files()
        return self.index_bundled_resources()

    # ── Guide / documentation indexing ─────────────────────────────────

    def _index_guide_file(self, guide_path: Path) -> int:
        """Index a modular language guide file as a documentation entry."""
        file_hash = self._file_hash(guide_path)
        if file_hash in self._indexed_files:
            return 0

        try:
            content = guide_path.read_text()
        except Exception as e:
            logger.error(f"Failed to read guide {guide_path}: {e}")
            return 0

        name = guide_path.stem
        example = PExample(
            id=f"guide_{name}_{file_hash[:8]}",
            name=name,
            description=f"P language guide: {name}",
            code=content[:4000] if len(content) > 4000 else content,
            category="documentation",
            tags=["documentation", "guide", name.replace("p_", "").replace("_guide", "")],
            source_file=str(guide_path),
            project_name="PeasyAI_Guides",
        )
        self._index_example(example)
        self._indexed_files.add(file_hash)
        self._save_indexed_files()
        return 1

    def _index_documentation_file(self, doc_path: Path) -> int:
        """Index the bundled documentation reference by section."""
        file_hash = self._file_hash(doc_path)
        if file_hash in self._indexed_files:
            return 0

        try:
            content = doc_path.read_text()
        except Exception as e:
            logger.error(f"Failed to read doc {doc_path}: {e}")
            return 0

        count = 0
        # Split on <tag> ... </tag> sections
        section_pattern = r'<(\w+)>(.*?)</\1>'
        for match in re.finditer(section_pattern, content, re.DOTALL):
            tag_name = match.group(1)
            section_text = match.group(2).strip()
            if not section_text or len(section_text) < 50:
                continue

            tags = ["documentation", "pattern", "reference"]
            # Infer additional tags from section tag name
            if "pattern" in tag_name or "pitfall" in tag_name:
                tags.append("best-practice")
            if any(kw in tag_name for kw in ["paxos", "raft", "commit", "leader", "timer"]):
                tags.append("protocol")
                tags.append(tag_name.replace("_", "-"))

            example = PExample(
                id=f"docref_{tag_name}_{hashlib.md5(section_text.encode()).hexdigest()[:8]}",
                name=tag_name.replace("_", " ").title(),
                description=f"Documentation reference: {tag_name}",
                code=section_text,
                category="documentation",
                tags=tags,
                source_file=str(doc_path),
                project_name="PeasyAI_Docs",
            )
            self._index_example(example)
            count += 1

        self._indexed_files.add(file_hash)
        self._save_indexed_files()
        return count

    # ── Extraction Methods ─────────────────────────────────────────────

    def _extract_examples(
        self,
        content: str,
        source_file: str,
        project_name: Optional[str],
    ) -> List[PExample]:
        """Extract examples from P file content."""
        examples: List[PExample] = []

        # ── Machines (brace-balanced) ─────────────────────────────
        for machine_name, machine_code in self._extract_top_level_blocks(
            content, "machine"
        ):
            desc = self._extract_description(content, content.find(machine_code))
            tags = self._infer_tags(machine_code)
            examples.append(
                PExample(
                    id=f"machine_{machine_name}_{hashlib.md5(machine_code.encode()).hexdigest()[:8]}",
                    name=machine_name,
                    description=desc or f"Machine {machine_name}",
                    code=machine_code,
                    category="machine",
                    tags=tags,
                    source_file=source_file,
                    project_name=project_name,
                )
            )

        # ── Specs (brace-balanced) ────────────────────────────────
        for spec_name, spec_code in self._extract_top_level_blocks(
            content, "spec"
        ):
            desc = self._extract_description(content, content.find(spec_code))
            tags = ["safety", "specification", "monitor"]
            if "hot state" in spec_code or "hot " in spec_code:
                tags.append("liveness")
            if "cold state" in spec_code or "cold " in spec_code:
                tags.append("cold-state")
            tags.extend(self._infer_tags(spec_code))
            examples.append(
                PExample(
                    id=f"spec_{spec_name}_{hashlib.md5(spec_code.encode()).hexdigest()[:8]}",
                    name=spec_name,
                    description=desc or f"Safety specification {spec_name}",
                    code=spec_code,
                    category="spec",
                    tags=list(set(tags)),
                    source_file=source_file,
                    project_name=project_name,
                )
            )

        # ── Test declarations ─────────────────────────────────────
        test_pattern = r'(test\s+(?:param\s+\(.*?\)\s+(?:assume\s+\(.*?\)\s+)?(?:\d+\s+wise\s+)?)?\s*\w+\s*\[.*?\].*?;)'
        for match in re.finditer(test_pattern, content, re.DOTALL):
            test_code = match.group(0).strip()
            test_name_match = re.search(
                r'test\s+(?:param\s+\(.*?\)\s+(?:assume\s+\(.*?\)\s+)?(?:\d+\s+wise\s+)?)?(\w+)',
                test_code,
                re.DOTALL,
            )
            test_name = test_name_match.group(1) if test_name_match else "Unknown"
            examples.append(
                PExample(
                    id=f"test_{test_name}_{hashlib.md5(test_code.encode()).hexdigest()[:8]}",
                    name=test_name,
                    description=f"Test case {test_name}",
                    code=test_code,
                    category="test",
                    tags=["test", "verification"],
                    source_file=source_file,
                    project_name=project_name,
                )
            )

        # ── Module definitions ────────────────────────────────────
        module_pattern = r'(module\s+\w+\s*=\s*[^;]+;)'
        for match in re.finditer(module_pattern, content, re.DOTALL):
            mod_code = match.group(1).strip()
            mod_name_match = re.search(r'module\s+(\w+)', mod_code)
            mod_name = mod_name_match.group(1) if mod_name_match else "Unknown"
            examples.append(
                PExample(
                    id=f"module_{mod_name}_{hashlib.md5(mod_code.encode()).hexdigest()[:8]}",
                    name=mod_name,
                    description=f"Module definition {mod_name}",
                    code=mod_code,
                    category="types",
                    tags=["module", "module-system", "composition"],
                    source_file=source_file,
                    project_name=project_name,
                )
            )

        # ── Global functions ──────────────────────────────────────
        for func_name, func_code in self._extract_global_functions(content):
            desc = self._extract_description(content, content.find(func_code))
            tags = self._infer_tags(func_code)
            tags.append("global-function")
            if "receive" in func_code:
                tags.append("blocking-receive")
            examples.append(
                PExample(
                    id=f"globalfun_{func_name}_{hashlib.md5(func_code.encode()).hexdigest()[:8]}",
                    name=func_name,
                    description=desc or f"Global function {func_name}",
                    code=func_code,
                    category="machine",
                    tags=tags,
                    source_file=source_file,
                    project_name=project_name,
                )
            )

        # ── Types / events / enums ────────────────────────────────
        if "event " in content or "type " in content:
            file_name = Path(source_file).stem
            types_events = self._extract_types_events(content)
            if types_events.strip():
                examples.append(
                    PExample(
                        id=f"types_{file_name}_{hashlib.md5(types_events.encode()).hexdigest()[:8]}",
                        name=f"{file_name} Types and Events",
                        description=f"Type and event definitions from {file_name}",
                        code=types_events,
                        category="types",
                        tags=["types", "events", "definitions"],
                        source_file=source_file,
                        project_name=project_name,
                    )
                )

        # ── Full test driver files ────────────────────────────────
        is_test_file = (
            "PTst/" in source_file
            or "/PTst/" in source_file
            or "TestDriver" in Path(source_file).stem
            or "Test" in Path(source_file).stem
        )
        if is_test_file and "machine " in content:
            file_name = Path(source_file).stem
            tags = self._infer_tags(content)
            tags.extend(["test-driver-full", "machine-wiring"])
            desc = self._extract_file_description(content)
            examples.append(
                PExample(
                    id=f"testdriver_{file_name}_{hashlib.md5(content.encode()).hexdigest()[:8]}",
                    name=f"{file_name} (Full Test Driver)",
                    description=desc
                    or f"Complete test driver with machine initialization and wiring from {file_name}",
                    code=content,
                    category="test",
                    tags=tags,
                    source_file=source_file,
                    project_name=project_name,
                )
            )

        # ── Best-practice annotated files ─────────────────────────
        if "BEST PRACTICE" in content:
            file_name = Path(source_file).stem
            tags = self._infer_tags(content)
            tags.append("best-practice-guide")
            examples.append(
                PExample(
                    id=f"bestpractice_{file_name}_{hashlib.md5(content.encode()).hexdigest()[:8]}",
                    name=f"{file_name} (Best Practices)",
                    description=f"Annotated best practices from {file_name}",
                    code=content,
                    category="machine",
                    tags=tags,
                    source_file=source_file,
                    project_name=project_name,
                )
            )

        # ── Full multi-machine files ──────────────────────────────
        machine_count = len(list(re.finditer(r'\bmachine\s+\w+', content)))
        if machine_count >= 2 and not is_test_file:
            file_name = Path(source_file).stem
            tags = self._infer_tags(content)
            tags.extend(["multi-machine", "full-file"])
            desc = self._extract_file_description(content)
            examples.append(
                PExample(
                    id=f"fullfile_{file_name}_{hashlib.md5(content.encode()).hexdigest()[:8]}",
                    name=f"{file_name} (Complete File)",
                    description=desc
                    or f"Complete P file with multiple machines from {file_name}",
                    code=content,
                    category="full_project",
                    tags=tags,
                    source_file=source_file,
                    project_name=project_name,
                )
            )

        return examples

    # ── Brace-balanced block extraction ────────────────────────────────

    def _extract_top_level_blocks(
        self, content: str, keyword: str
    ) -> List[tuple]:
        """Extract top-level machine/spec blocks using balanced brace matching."""
        results = []
        if keyword == "spec":
            pattern = re.compile(r"\bspec\s+(\w+)\s+observes\b")
        else:
            pattern = re.compile(rf"\b{keyword}\s+(\w+)\s*\{{")

        for match in pattern.finditer(content):
            name = match.group(1)
            brace_start = content.find("{", match.start())
            if brace_start == -1:
                continue
            depth = 0
            end = brace_start
            for i in range(brace_start, len(content)):
                if content[i] == "{":
                    depth += 1
                elif content[i] == "}":
                    depth -= 1
                    if depth == 0:
                        end = i + 1
                        break
            if depth != 0:
                continue
            results.append((name, content[match.start() : end]))
        return results

    def _extract_global_functions(self, content: str) -> List[tuple]:
        """Extract global functions (outside any machine/spec block)."""
        results = []
        occupied = []
        for kw in ("machine", "spec"):
            for _, block in self._extract_top_level_blocks(content, kw):
                start = content.find(block)
                if start >= 0:
                    occupied.append((start, start + len(block)))

        def _inside(pos: int) -> bool:
            return any(s <= pos < e for s, e in occupied)

        for match in re.finditer(r"\bfun\s+(\w+)\s*\(", content):
            if _inside(match.start()):
                continue
            brace_start = content.find("{", match.start())
            if brace_start == -1:
                continue
            depth = 0
            end = brace_start
            for i in range(brace_start, len(content)):
                if content[i] == "{":
                    depth += 1
                elif content[i] == "}":
                    depth -= 1
                    if depth == 0:
                        end = i + 1
                        break
            if depth != 0:
                continue
            results.append((match.group(1), content[match.start() : end]))
        return results

    # ── Description extraction ─────────────────────────────────────────

    def _extract_description(
        self, content: str, position: int
    ) -> Optional[str]:
        if position < 0:
            return None
        before = content[:position]
        lines = before.split("\n")[-10:]
        comments: list[str] = []
        in_block = False
        for line in reversed(lines):
            stripped = line.strip()
            if stripped.endswith("*/"):
                in_block = True
                text = stripped.rstrip("*/").strip()
                if text and not text.startswith("/*"):
                    comments.insert(0, text)
                elif text.startswith("/*"):
                    text = text[2:].strip()
                    if text:
                        comments.insert(0, text)
                    in_block = False
                continue
            if in_block:
                text = stripped.lstrip("* ").strip()
                if stripped.startswith("/*"):
                    text = stripped[2:].lstrip("* ").strip()
                    in_block = False
                if text and not all(c in "=-*/" for c in text):
                    comments.insert(0, text)
                continue
            if stripped.startswith("//"):
                text = stripped[2:].strip()
                if text and not all(c in "=-*/" for c in text):
                    comments.insert(0, text)
            elif stripped.startswith("/*"):
                text = stripped[2:].rstrip("*/").strip()
                if text:
                    comments.insert(0, text)
            elif stripped:
                break
        return " ".join(comments) if comments else None

    def _extract_file_description(self, content: str) -> Optional[str]:
        lines = content.split("\n")
        comments: list[str] = []
        in_block = False
        for line in lines[:30]:
            stripped = line.strip()
            if in_block:
                if "*/" in stripped:
                    text = stripped.split("*/")[0].lstrip("* ").strip()
                    if text and not all(c in "=-*/" for c in text):
                        comments.append(text)
                    in_block = False
                else:
                    text = stripped.lstrip("* ").strip()
                    if text and not all(c in "=-*/" for c in text):
                        comments.append(text)
                continue
            if stripped.startswith("/*"):
                in_block = True
                text = stripped[2:].rstrip("*/").strip()
                if text:
                    comments.append(text)
                if "*/" in stripped:
                    in_block = False
            elif stripped.startswith("//"):
                text = stripped[2:].strip()
                if text and not all(c in "=-*/" for c in text):
                    comments.append(text)
            elif stripped and not stripped.startswith("*"):
                break
        return " ".join(comments) if comments else None

    # ── Tag inference ──────────────────────────────────────────────────

    def _infer_tags(self, code: str) -> List[str]:
        tags: list[str] = []
        if "send " in code:
            tags.append("message-passing")
        if "goto " in code:
            tags.append("state-machine")
        if "defer " in code:
            tags.append("deferred-events")
        if "raise " in code:
            tags.append("internal-events")
        if "new " in code:
            tags.append("machine-creation")
        if "foreach" in code:
            tags.append("iteration")
        if "map[" in code or "seq[" in code:
            tags.append("collections")
        if "$" in code or "choose(" in code:
            tags.append("nondeterminism")
        if "assert " in code:
            tags.append("assertions")
        if "receive" in code and "case " in code:
            tags.append("blocking-receive")
        if "announce " in code:
            tags.append("announce-event")
        if "hot state" in code or "hot " in code:
            tags.append("liveness")
        if "cold state" in code or "cold " in code:
            tags.append("cold-state")
        if "ignore " in code:
            tags.append("ignore-pattern")
        if re.search(r"while\s*\(.*sizeof.*\)\s*\{.*send\b", code, re.DOTALL):
            tags.append("broadcast-pattern")
        if re.search(r"eSetup\w+|eConfig\w+|eInform\w+|eInit\w+", code):
            tags.append("setup-event")
        if re.search(r"BEST\s+PRACTICE", code):
            tags.append("best-practice")
        if re.search(r"ANTI.?PATTERN", code):
            tags.append("anti-pattern")
        if re.search(r"machine\s+\w*(?:Test|Scenario|Driver)\w*", code):
            tags.append("test-driver")
        if "fun SetUp" in code or "fun Setup" in code:
            tags.append("test-setup")
        if "majority" in code.lower() or "quorum" in code.lower():
            tags.append("quorum-pattern")
        if re.search(r"seq\[machine\]|set\[machine\]", code):
            tags.append("machine-collection")

        # Protocol-specific
        lower = code.lower()
        if "paxos" in lower or "proposer" in lower or "acceptor" in lower:
            tags.append("paxos")
        if "commit" in lower and ("coordinator" in lower or "participant" in lower):
            tags.append("two-phase-commit")
        if "raft" in lower or ("leader" in lower and "follower" in lower):
            tags.append("raft")
        if "lock" in lower and ("acquire" in lower or "release" in lower):
            tags.append("distributed-lock")
        if "timer" in lower or "timeout" in lower:
            tags.append("timer-pattern")
        if "failure" in lower and ("detector" in lower or "inject" in lower):
            tags.append("failure-detection")
        return tags

    def _infer_facets(
        self,
        code: str,
        category: str,
        tags: List[str],
        description: Optional[str] = None,
        project_name: Optional[str] = None,
    ) -> Dict[str, List[str]]:
        """
        Infer structured facet metadata from code/tags/description.

        Facets are used by multi-lane retrieval:
          - protocol_tags
          - construct_tags
          - pattern_tags
          - intent_tags
        """
        text = f"{code}\n{description or ''}\n{project_name or ''}".lower()
        tagset = {t.lower() for t in tags}

        protocol_tags: List[str] = []
        construct_tags: List[str] = []
        pattern_tags: List[str] = []
        intent_tags: List[str] = []

        # Protocol facets
        if "paxos" in text or {"proposer", "acceptor", "learner"}.intersection(tagset):
            protocol_tags.append("paxos")
        if "raft" in text or ("leader" in text and "follower" in text):
            protocol_tags.append("raft")
        if "two-phase-commit" in tagset or ("coordinator" in text and "participant" in text and "commit" in text):
            protocol_tags.append("two-phase-commit")
        if "distributed-lock" in tagset or ("lock" in text and ("acquire" in text or "release" in text)):
            protocol_tags.append("distributed-lock")
        if "failure-detection" in tagset or ("failure" in text and "detector" in text):
            protocol_tags.append("failure-detector")

        # Construct facets (language-level)
        construct_map = {
            "send": "send",
            "receive": "receive",
            "goto": "goto",
            "defer": "defer",
            "ignore": "ignore",
            "announce": "announce",
            "new ": "new-machine",
            "hot state": "hot-state",
            "cold state": "cold-state",
            "spec ": "spec-machine",
            "module ": "module-system",
            "test ": "test-case",
            "event ": "events",
            "type ": "types",
            "enum ": "enums",
            "foreach": "foreach",
            "while ": "while-loop",
            "assert ": "assertions",
            "choose(": "nondeterminism",
            "$": "nondeterminism",
        }
        for needle, facet in construct_map.items():
            if needle in text:
                construct_tags.append(facet)

        # Pattern facets
        if "broadcast-pattern" in tagset or ("while" in text and "send" in text):
            pattern_tags.append("broadcast")
        if "quorum-pattern" in tagset or "majority" in text or "quorum" in text:
            pattern_tags.append("quorum")
        if "test-driver" in tagset or "test-driver-full" in tagset:
            pattern_tags.append("test-driver")
        if "setup-event" in tagset:
            pattern_tags.append("setup-event")
        if "blocking-receive" in tagset:
            pattern_tags.append("request-response")
        if "timer-pattern" in tagset:
            pattern_tags.append("timer-timeout")
        if "machine-creation" in tagset:
            pattern_tags.append("machine-wiring")

        # Intent facets
        if category == "spec" or "monitor" in tagset:
            intent_tags.append("safety")
        if "liveness" in tagset or "hot-state" in construct_tags:
            intent_tags.append("liveness")
        if category == "test":
            intent_tags.append("testing")
        if category == "machine":
            intent_tags.append("implementation")
        if category == "types":
            intent_tags.append("declarations")
        if category == "documentation":
            intent_tags.append("reference")

        # Deduplicate while preserving order.
        def _unique(seq: List[str]) -> List[str]:
            seen: Set[str] = set()
            out: List[str] = []
            for item in seq:
                if item not in seen:
                    seen.add(item)
                    out.append(item)
            return out

        return {
            "protocol_tags": _unique(protocol_tags),
            "construct_tags": _unique(construct_tags),
            "pattern_tags": _unique(pattern_tags),
            "intent_tags": _unique(intent_tags),
        }

    # ── Types / events extraction ──────────────────────────────────────

    def _extract_types_events(self, content: str) -> str:
        lines: list[str] = []
        in_declaration = False
        for line in content.split("\n"):
            stripped = line.strip()
            if in_declaration:
                lines.append(line)
                if ";" in stripped or "}" in stripped:
                    in_declaration = False
                continue
            if stripped.startswith(("type ", "event ", "enum ")):
                lines.append(line)
                if ";" not in stripped and "{" not in stripped:
                    in_declaration = True
                elif "{" in stripped and "}" not in stripped:
                    in_declaration = True
            elif stripped.startswith("//"):
                lines.append(line)
        return "\n".join(lines)

    # ── Internal persistence helpers ───────────────────────────────────

    def _index_example(self, example: PExample) -> None:
        content = example.to_document_content()
        embedding = self.embeddings.embed(content)
        facets = self._infer_facets(
            code=example.code,
            category=example.category,
            tags=example.tags,
            description=example.description,
            project_name=example.project_name,
        )
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
                "description": example.description,
                "protocol_tags": facets["protocol_tags"],
                "construct_tags": facets["construct_tags"],
                "pattern_tags": facets["pattern_tags"],
                "intent_tags": facets["intent_tags"],
            },
        )
        self.vector_store.add(doc)

    def _file_hash(self, file_path: Path) -> str:
        return hashlib.md5(file_path.read_bytes()).hexdigest()

    def _load_indexed_files(self) -> None:
        index_file = self.store_path / "indexed_files.json"
        if index_file.exists():
            import json

            with open(index_file, "r") as f:
                self._indexed_files = set(json.load(f))

    def _save_indexed_files(self) -> None:
        import json

        index_file = self.store_path / "indexed_files.json"
        with open(index_file, "w") as f:
            json.dump(list(self._indexed_files), f)
