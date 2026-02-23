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


# ── Comprehensive syntax hints per category ───────────────────────────
# These are drawn from the official P docs and common generation pitfalls.

MACHINE_SYNTAX_HINTS = [
    "Variables must be declared at the very start of functions, before any executable statements.",
    "Variable declaration and assignment must be separate: `var x: int;` then `x = 5;` — NOT `var x: int = 5;`",
    "Single-field named tuples require a trailing comma: `(field = value,)`",
    "Use `this` to reference the current machine (NOT `self`).",
    "State machines must have exactly one `start state`.",
    "Entry functions for non-start states take at most 1 parameter (the goto/transition payload).",
    "Exit functions cannot have parameters.",
    "Do NOT access functions contained inside other machines.",
    "Send syntax: `send target, eventName, payload;` — target must be a machine-typed variable.",
    "Collections are empty by default on declaration — do NOT redundantly re-initialize them.",
    "Sequence insert: `seq += (index, value)` — NOT `seq += (value)`.",
    "Set insert: `set += (element)` — parentheses required around element.",
    "The `foreach` iteration variable must be declared at the top of the function body.",
    "Switch/case is NOT supported — use if/else chains.",
    "`const` keyword is NOT supported in P.",
    "Compound assignment operators `+=`, `-=` are only for collections, NOT for `int`/`float`.",
    "The `!` operator with `in` requires parentheses: `!(x in collection)` — NOT `!x in collection`.",
    "`!in` and `not in` are NOT supported.",
    "Formatted strings: `format(\"text {0} {1}\", arg0, arg1)`",
]

SPEC_SYNTAX_HINTS = [
    "Spec machines observe events, they do NOT send/receive/create machines.",
    "Syntax: `spec Name observes event1, event2 { ... }`",
    "Entry functions in spec machines CANNOT take parameters.",
    "`$`, `$$`, `this`, `new`, `send`, `announce`, `receive`, and `pop` are NOT allowed in monitors.",
    "Use `hot state` for liveness properties — the system must eventually leave hot states.",
    "Use `cold state` for states where the system may remain indefinitely.",
    "Specs are synchronously composed: events are delivered to monitors before the target machine.",
    "Safety specs observe events and assert invariants using local state tracking.",
    "Liveness specs mark intermediate states as `hot` and check eventual convergence.",
    "Never generate empty functions or functions with only comments in spec machines.",
    "Spec observes list must include ALL events the spec handles (on/do/goto handlers).",
]

TEST_SYNTAX_HINTS = [
    "Test syntax: `test TestName [main=MainMachine]: assert Spec1, Spec2 in (union Module1, Module2, { TestMachine });`",
    "The main machine must be included in the module expression (typically via `{ TestMachine }`).",
    "Test driver machines set up the system by creating machines and sending configuration events.",
    "Use `announce` to initialize spec monitors before machines start communicating.",
    "Use `choose(N)` for nondeterministic choices the P checker will explore.",
    "Use `$` for nondeterministic boolean choices.",
    "NEVER declare events in both test driver files and source files — this causes duplicate declaration errors.",
    "Test case and module declarations go in a separate TestScript.p file from the TestDriver.p machine code.",
    "Parameterized tests: `test param (nClients in [2, 3, 4]) tcTest [main=TestDriver]: ...;`",
]

TYPES_SYNTAX_HINTS = [
    "Named tuple types: `type tRequest = (client: machine, value: int);`",
    "Events must use a separately declared type for payloads — NOT inline named tuples.",
    "Correct: `type tPayload = (x: int);` then `event eMsg: tPayload;`",
    "WRONG: `event eMsg: (x: int);` — inline payload types are not allowed.",
    "Events without payloads: `event ePing;`",
    "Enums: `enum Status { SUCCESS, FAILURE, TIMEOUT }`",
    "Enums with values: `enum Code { OK = 200, ERROR = 500 }`",
    "Enum values are global constants and must have unique names across the project.",
    "`type` names, `event` names, and `enum` names must all be unique and not clash with reserved keywords.",
    "P supports: `int`, `bool`, `float`, `string`, `machine`, `event`, `any`, `data`.",
    "Collections: `seq[T]`, `set[T]`, `map[K, V]`.",
    "Default values: `default(type)` — e.g., `default(int)` is `0`, `default(seq[int])` is empty sequence.",
]


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
                code = ex.get('code', '')
                if code:
                    sections.append(f"```p\n{code}\n```")
        
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
        
        # Cache for documentation loaded from modular guide files
        self._doc_cache: Dict[str, str] = {}
        
        # Auto-index/migrate corpus so runtime uses current facet schema.
        if auto_index:
            if self.corpus.count() == 0:
                self._auto_index()
            elif not self.corpus.has_facet_schema():
                logger.info("Detected legacy corpus schema; rebuilding bundled index")
                rebuilt = self.corpus.rebuild_bundled_index()
                if rebuilt > 0:
                    self._indexed = True
    
    def _auto_index(self) -> None:
        """
        Automatically index the bundled resources.

        This is fully self-contained — it indexes the curated examples and
        documentation shipped inside ``resources/`` so no external repo is
        needed.  If ``P_REPO_PATH`` is set, extra examples from the full
        repo are indexed as a bonus.
        """
        # Always start with the bundled resources (standalone)
        logger.info("Auto-indexing bundled PeasyAI resources")
        count = self.corpus.index_bundled_resources()
        if count > 0:
            self._indexed = True

        # Optionally index extra files from the full P repo if available
        p_repo = os.environ.get("P_REPO_PATH")
        if p_repo and Path(p_repo).exists():
            logger.info(f"Also indexing extra examples from P repo: {p_repo}")
            extra_dirs = [
                Path(p_repo) / "Tutorial",
                Path(p_repo) / "Tst" / "RegressionTests",
            ]
            for d in extra_dirs:
                if d.exists():
                    self.corpus.index_directory(str(d))
            self._indexed = True
    
    def _load_guide(self, guide_name: str) -> str:
        """
        Load a modular guide file from the resources/context_files directory.
        Returns the content or empty string if not found.
        """
        if guide_name in self._doc_cache:
            return self._doc_cache[guide_name]
        
        # Try to locate the resources directory
        resources_dir = Path(__file__).parent.parent.parent.parent / "resources" / "context_files"
        guide_path = resources_dir / guide_name
        
        if guide_path.exists():
            try:
                content = guide_path.read_text()
                self._doc_cache[guide_name] = content
                return content
            except Exception as e:
                logger.debug(f"Failed to load guide {guide_name}: {e}")
        
        return ""
    
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

    def _context_categories(self, context_type: str) -> List[str]:
        if context_type == "machine":
            return ["machine", "full_project", "test"]
        if context_type == "spec":
            return ["spec", "documentation", "full_project"]
        if context_type == "test":
            return ["test", "machine", "full_project", "documentation"]
        if context_type == "types":
            return ["types", "machine", "documentation"]
        return ["machine", "spec", "test", "types", "full_project", "documentation"]

    def _derive_facets(
        self,
        text: str,
        context_type: str,
        context_files: Optional[Dict[str, str]] = None,
    ) -> Dict[str, List[str]]:
        """
        Infer retrieval facets from user description/design text and
        optionally from already-generated context files (types, machines).

        Facets drive multi-lane retrieval beyond protocol names:
        protocol + language construct + pattern.
        """
        # Combine description text with event/type names from context files
        # so facets reflect the actual generated code, not just the prose.
        extra = ""
        if context_files:
            for content in context_files.values():
                extra += " " + content
        lower = ((text or "") + " " + extra).lower()
        protocols: List[str] = []
        constructs: List[str] = []
        patterns: List[str] = []

        # Protocol facets
        if any(k in lower for k in ["paxos", "proposer", "acceptor", "learner"]):
            protocols.append("paxos")
        if any(k in lower for k in ["raft", "leader election", "appendentries", "follower", "candidate", "leader heartbeat"]):
            protocols.append("raft")
        if any(k in lower for k in ["two phase", "2pc", "coordinator", "participant", "atomic commit"]):
            protocols.append("two-phase-commit")
        if any(k in lower for k in ["distributed lock", "mutex", "acquire lock", "release lock"]):
            protocols.append("distributed-lock")
        if any(k in lower for k in ["failure detector", "suspect", "heartbeat timeout", "crash detector",
                                      "heartbeat", "liveness check", "node crash", "node failure"]):
            protocols.append("failure-detector")
        if any(k in lower for k in ["ring", "token ring", "chang-roberts", "ring election"]):
            protocols.append("ring-election")
        if any(k in lower for k in ["client server", "client-server", "request response", "rpc"]):
            protocols.append("client-server")

        # Construct facets
        construct_hints = {
            "send": ["send", "message"],
            "receive": ["receive", "request-response", "blocking"],
            "goto": ["goto", "transition", "state"],
            "defer": ["defer"],
            "ignore": ["ignore"],
            "announce": ["announce", "monitor init"],
            "new-machine": ["new machine", "spawn", "create machine"],
            "hot-state": ["hot state", "eventually", "liveness"],
            "cold-state": ["cold state"],
            "spec-machine": ["spec", "monitor", "safety"],
            "module-system": ["module", "compose", "union"],
            "test-case": ["test", "checker", "schedule"],
            "events": ["event", "payload"],
            "types": ["type", "tuple"],
            "enums": ["enum"],
            "nondeterminism": ["choose(", "nondeterministic", "$"],
            "assertions": ["assert", "invariant"],
            "foreach": ["foreach", "iterate"],
            "while-loop": ["while"],
        }
        for facet, kws in construct_hints.items():
            if any(kw in lower for kw in kws):
                constructs.append(facet)

        # Pattern facets
        if any(k in lower for k in ["broadcast", "fan-out", "multicast"]):
            patterns.append("broadcast")
        if any(k in lower for k in ["quorum", "majority"]):
            patterns.append("quorum")
        if any(k in lower for k in ["timeout", "timer", "heartbeat", "periodic",
                                      "etimeout", "estarttimer", "ecanceltimer"]):
            patterns.append("timer-timeout")
        if any(k in lower for k in ["setup", "initialize", "bootstrap", "wiring"]):
            patterns.append("setup-event")
            patterns.append("machine-wiring")
        if any(k in lower for k in ["test driver", "scenario"]):
            patterns.append("test-driver")
        if any(k in lower for k in ["request response", "request-response", "rpc", "blocking receive"]):
            patterns.append("request-response")
        if any(k in lower for k in ["coffee", "espresso", "vending", "appliance", "dispenser",
                                      "grinding", "brewing", "idle"]):
            patterns.append("sequential-states")
        if any(k in lower for k in ["election", "leader", "vote", "ballot", "term"]):
            patterns.append("leader-election")

        # Cross-derive: protocols that inherently use certain patterns
        if "failure-detector" in protocols and "timer-timeout" not in patterns:
            patterns.append("timer-timeout")
        if "raft" in protocols:
            if "timer-timeout" not in patterns:
                patterns.append("timer-timeout")
            if "leader-election" not in patterns:
                patterns.append("leader-election")
            if "broadcast" not in patterns:
                patterns.append("broadcast")

        # Context-sensitive defaults
        if context_type == "spec":
            constructs.extend(["spec-machine", "assertions"])
        elif context_type == "test":
            constructs.extend(["test-case", "announce", "module-system", "nondeterminism"])
            patterns.append("test-driver")
        elif context_type == "types":
            constructs.extend(["types", "events", "enums"])
        elif context_type == "machine":
            constructs.extend(["send", "goto"])

        def _unique(items: List[str]) -> List[str]:
            seen = set()
            out = []
            for item in items:
                if item not in seen:
                    seen.add(item)
                    out.append(item)
            return out

        return {
            "protocols": _unique(protocols),
            "constructs": _unique(constructs),
            "patterns": _unique(patterns),
        }

    def _build_faceted_query(
        self,
        description: str,
        design_doc: Optional[str],
        context_type: str,
        facets: Dict[str, List[str]],
    ) -> str:
        parts = [context_type, "P language", description]
        if design_doc:
            parts.append(self._extract_keywords(design_doc))
        parts.extend(facets.get("protocols", []))
        parts.extend(facets.get("constructs", []))
        parts.extend(facets.get("patterns", []))
        return " ".join([p for p in parts if p]).strip()

    def _examples_from_results(
        self,
        results: List[SearchResult],
        min_score: float = 0.28,
        limit: int = 3,
    ) -> List[Dict[str, Any]]:
        examples: List[Dict[str, Any]] = []
        for result in results:
            if result.score < min_score:
                continue
            meta = result.document.metadata
            examples.append(
                {
                    "name": meta.get("name"),
                    "description": meta.get("description"),
                    "code": meta.get("code"),
                    "project": meta.get("project_name"),
                    "category": meta.get("category"),
                    "score": result.score,
                }
            )
            if len(examples) >= limit:
                break
        return examples
    
    def get_machine_context(
        self, 
        description: str,
        design_doc: Optional[str] = None,
        num_examples: int = 3,
        context_files: Optional[Dict[str, str]] = None,
    ) -> RAGContext:
        """
        Get context for generating a machine.
        
        Args:
            description: Machine description or name
            design_doc: Optional design document for more context
            num_examples: Number of examples to retrieve
            context_files: Already-generated P files (e.g. types/events)
                           used to enrich facet derivation
            
        Returns:
            RAGContext with relevant examples, documentation, and syntax hints
        """
        context = RAGContext()
        
        facets = self._derive_facets(
            f"{description} {design_doc or ''}",
            context_type="machine",
            context_files=context_files,
        )
        query = self._build_faceted_query(description, design_doc, "machine", facets)

        results = self.corpus.search_faceted(
            query=query,
            top_k=max(4, num_examples + 1),
            categories=self._context_categories("machine"),
            protocols=facets["protocols"],
            constructs=facets["constructs"],
            patterns=facets["patterns"],
        )
        context.examples.extend(
            self._examples_from_results(results, min_score=0.28, limit=max(3, num_examples))
        )
        
        # Load relevant documentation from modular guides
        machine_doc = self._load_guide("modular/p_machines_guide.txt")
        if machine_doc:
            # Extract the most relevant sections rather than dumping the whole guide
            context.documentation.append(
                self._extract_doc_section(machine_doc, [
                    "about_event_handler",
                    "about_send_statements",
                    "about_defer_statement",
                    "about_ignore_statement",
                    "about_machine_creation",
                    "about_entry_exit_functions",
                ])
            )
        
        # Add comprehensive syntax hints
        context.syntax_hints = list(MACHINE_SYNTAX_HINTS)
        
        # Add protocol-specific hints based on description
        desc_lower = description.lower()
        if any(kw in desc_lower for kw in ['lock', 'mutex', 'acquire', 'release']):
            context.syntax_hints.append("For lock protocols, use `receive { case eLockGranted: ... }` for blocking lock acquisition.")
        if any(kw in desc_lower for kw in ['consensus', 'paxos', 'raft', 'vote']):
            context.syntax_hints.append("For consensus protocols, track quorum with `set[machine]` and check `sizeof(votes) > sizeof(all) / 2`.")
        if any(kw in desc_lower for kw in ['timer', 'timeout', 'heartbeat']):
            context.syntax_hints.append(
                "For timer patterns, use the standard Timer module: "
                "`timer = CreateTimer(this);` to create, "
                "`StartTimer(timer);` to start, "
                "`CancelTimer(timer);` to cancel. "
                "Handle `eTimeOut` in your machine's state. "
                "Do NOT re-implement the Timer machine — use the existing Timer module."
            )
        
        return context
    
    def get_spec_context(
        self, 
        description: str,
        machines: Optional[List[str]] = None,
        num_examples: int = 3
    ) -> RAGContext:
        """Get context for generating a specification."""
        context = RAGContext()
        
        full_desc = description
        if machines:
            full_desc += " " + " ".join(machines)
        facets = self._derive_facets(full_desc, context_type="spec")
        query = self._build_faceted_query(full_desc, None, "specification monitor safety", facets)

        results = self.corpus.search_faceted(
            query=query,
            top_k=max(4, num_examples + 1),
            categories=self._context_categories("spec"),
            protocols=facets["protocols"],
            constructs=facets["constructs"],
            patterns=facets["patterns"],
        )
        context.examples.extend(
            self._examples_from_results(results, min_score=0.26, limit=max(3, num_examples))
        )
        
        # Load spec guide documentation
        spec_doc = self._load_guide("modular/p_spec_monitors_guide.txt")
        if spec_doc:
            context.documentation.append(spec_doc)
        
        context.syntax_hints = list(SPEC_SYNTAX_HINTS)
        
        return context
    
    def get_test_context(
        self, 
        description: str,
        machines: Optional[List[str]] = None,
        num_examples: int = 3
    ) -> RAGContext:
        """Get context for generating a test."""
        context = RAGContext()
        
        full_desc = description
        if machines:
            full_desc += " " + " ".join(machines)
        facets = self._derive_facets(full_desc, context_type="test")
        query = self._build_faceted_query(full_desc, None, "test driver scenario", facets)

        results = self.corpus.search_faceted(
            query=query,
            top_k=max(4, num_examples + 1),
            categories=self._context_categories("test"),
            protocols=facets["protocols"],
            constructs=facets["constructs"],
            patterns=facets["patterns"],
        )
        context.examples.extend(
            self._examples_from_results(results, min_score=0.26, limit=max(3, num_examples))
        )
        
        # Load test guide and module system documentation
        test_doc = self._load_guide("modular/p_test_cases_guide.txt")
        if test_doc:
            context.documentation.append(test_doc)
        
        module_doc = self._load_guide("modular/p_module_system_guide.txt")
        if module_doc:
            context.documentation.append(module_doc)
        
        context.syntax_hints = list(TEST_SYNTAX_HINTS)
        
        return context
    
    def get_types_context(
        self, 
        description: str,
        num_examples: int = 3
    ) -> RAGContext:
        """Get context for generating types and events."""
        context = RAGContext()
        
        facets = self._derive_facets(description, context_type="types")
        query = self._build_faceted_query(description, None, "type event enum definition", facets)
        results = self.corpus.search_faceted(
            query=query,
            top_k=max(4, num_examples + 1),
            categories=self._context_categories("types"),
            protocols=facets["protocols"],
            constructs=facets["constructs"],
            patterns=facets["patterns"],
        )
        context.examples.extend(
            self._examples_from_results(results, min_score=0.26, limit=max(3, num_examples))
        )
        
        # Load types, events, and enums guides
        types_doc = self._load_guide("modular/p_types_guide.txt")
        if types_doc:
            context.documentation.append(types_doc)
        
        events_doc = self._load_guide("modular/p_events_guide.txt")
        if events_doc:
            context.documentation.append(events_doc)
        
        enums_doc = self._load_guide("modular/p_enums_guide.txt")
        if enums_doc:
            context.documentation.append(enums_doc)
        
        context.syntax_hints = list(TYPES_SYNTAX_HINTS)
        
        return context
    
    def get_protocol_examples(self, protocol_name: str, top_k: int = 5) -> RAGContext:
        """
        Get examples for a specific protocol type.

        Retrieval is intentionally code-first:
        - Prioritize categories with executable code (machine/spec/test/full_project/types)
        - Penalize pure documentation hits
        - Boost results with matching protocol tags/keywords in code, description, and project name
        """
        context = RAGContext()
        normalized = protocol_name.strip().lower()

        protocol_queries = {
            "paxos": [
                "paxos proposer acceptor learner ballot quorum consensus",
                "single decree paxos prepare accept phase1 phase2",
            ],
            "two-phase commit": [
                "two phase commit coordinator participant prepare commit abort atomicity",
                "2pc transaction commit protocol",
            ],
            "2pc": [
                "two phase commit coordinator participant prepare commit abort atomicity",
                "2pc transaction commit protocol",
            ],
            "raft": [
                "raft leader follower candidate election append entries log replication term",
                "raft consensus leader election heartbeat commit index",
            ],
            "failure detector": [
                "failure detector heartbeat timeout suspect crash monitor",
                "distributed failure detection ping timeout",
            ],
            "distributed lock": [
                "distributed lock acquire release lock server mutual exclusion",
                "lock manager token lock grant revoke",
            ],
        }

        protocol_keywords = {
            "paxos": ["paxos", "proposer", "acceptor", "learner", "ballot", "quorum"],
            "two-phase commit": ["2pc", "two phase", "coordinator", "participant", "prepare", "commit", "abort"],
            "2pc": ["2pc", "two phase", "coordinator", "participant", "prepare", "commit", "abort"],
            "raft": ["raft", "leader", "follower", "candidate", "term", "appendentries", "heartbeat"],
            "failure detector": ["failure", "detector", "timeout", "heartbeat", "suspect", "crash"],
            "distributed lock": ["lock", "acquire", "release", "mutex", "critical section"],
        }
        strict_protocol_tokens = {
            "paxos": ["paxos"],
            "two-phase commit": ["two phase", "2pc"],
            "2pc": ["two phase", "2pc"],
            "raft": ["raft"],
            "failure detector": ["failure detector", "failure-detect", "detector"],
            "distributed lock": ["distributed lock", "lock server", "lock"],
        }

        queries = protocol_queries.get(
            normalized,
            [f"protocol {normalized} distributed system P language"],
        )
        keywords = protocol_keywords.get(normalized, [normalized])

        protocol_facet_map = {
            "paxos": ["paxos"],
            "two-phase commit": ["two-phase-commit"],
            "2pc": ["two-phase-commit"],
            "raft": ["raft"],
            "failure detector": ["failure-detector"],
            "distributed lock": ["distributed-lock"],
        }
        pattern_facet_map = {
            "paxos": ["quorum"],
            "two-phase commit": ["request-response"],
            "2pc": ["request-response"],
            "raft": ["quorum", "timer-timeout"],
            "failure detector": ["timer-timeout"],
            "distributed lock": ["request-response"],
        }
        construct_facet_map = {
            "paxos": ["send", "receive", "goto", "events"],
            "two-phase commit": ["send", "receive", "events"],
            "2pc": ["send", "receive", "events"],
            "raft": ["send", "goto", "events", "types"],
            "failure detector": ["send", "events", "nondeterminism"],
            "distributed lock": ["send", "receive", "events"],
        }

        # Category priority order: code-first
        priority_categories = ["machine", "spec", "test", "full_project", "types"]
        candidate_results: List[SearchResult] = []
        per_bucket_k = max(6, top_k * 3)
        protocol_facets = protocol_facet_map.get(normalized, [normalized])
        pattern_facets = pattern_facet_map.get(normalized, [])
        construct_facets = construct_facet_map.get(normalized, [])

        for q in queries:
            candidate_results.extend(
                self.corpus.search_faceted(
                    query=q,
                    top_k=per_bucket_k,
                    categories=priority_categories,
                    protocols=protocol_facets,
                    constructs=construct_facets,
                    patterns=pattern_facets,
                )
            )
            candidate_results.extend(self.corpus.search(q, top_k=max(5, top_k)))

        # Deduplicate by document id, keep best reranked score
        best_by_id: Dict[str, tuple[SearchResult, float, int, int]] = {}
        for result in candidate_results:
            meta = result.document.metadata
            category = (meta.get("category") or "").lower()
            code = (meta.get("code") or "")
            description = (meta.get("description") or "").lower()
            project = (meta.get("project_name") or "").lower()
            tags = [str(t).lower() for t in (meta.get("tags") or [])]
            code_lower = code.lower()

            score = result.score

            # Category boost/penalty
            if category == "machine":
                score += 0.18
            elif category == "spec":
                score += 0.12
            elif category == "test":
                score += 0.10
            elif category == "full_project":
                score += 0.08
            elif category == "types":
                score += 0.04
            elif category in ("documentation", "manual", "tutorial", "advanced", "getting_started"):
                score -= 0.20

            # Prefer results that actually include code
            if code.strip():
                score += 0.08
            else:
                score -= 0.15

            # Keyword and tag boosts
            keyword_hits = 0
            for kw in keywords:
                kw = kw.lower()
                if kw in code_lower:
                    keyword_hits += 2
                if kw in description:
                    keyword_hits += 1
                if kw in project:
                    keyword_hits += 1
                if any(kw in tag for tag in tags):
                    keyword_hits += 2
            score += min(0.20, keyword_hits * 0.02)
            if normalized in protocol_keywords and keyword_hits == 0:
                # For known protocols, demote unrelated generic results.
                score -= 0.25

            strict_hits = 0
            for tok in strict_protocol_tokens.get(normalized, []):
                tok = tok.lower()
                if tok in code_lower:
                    strict_hits += 2
                if tok in description:
                    strict_hits += 1
                if tok in project:
                    strict_hits += 1
                if any(tok in tag for tag in tags):
                    strict_hits += 2

            # Protocol-structure heuristics
            if normalized in ("raft", "paxos", "two-phase commit", "2pc") and "machine " in code_lower:
                score += 0.04
            if normalized in ("two-phase commit", "2pc") and ("coordinator" in code_lower and "participant" in code_lower):
                score += 0.06
            if normalized == "raft" and ("leader" in code_lower and "follower" in code_lower):
                score += 0.06
            if normalized == "paxos" and ("proposer" in code_lower and "acceptor" in code_lower):
                score += 0.06

            doc_id = result.document.id
            prev = best_by_id.get(doc_id)
            if prev is None or score > prev[1]:
                best_by_id[doc_id] = (result, score, keyword_hits, strict_hits)

        ranked = sorted(best_by_id.values(), key=lambda x: x[1], reverse=True)

        # For known protocols, prefer entries with explicit keyword matches.
        # If no such entries exist, fall back to the full ranked set.
        if normalized in protocol_keywords:
            strict_ranked = [r for r in ranked if r[3] > 0]
            if strict_ranked:
                ranked = strict_ranked
            keyword_ranked = [r for r in ranked if r[2] > 0]
            if keyword_ranked:
                ranked = keyword_ranked

        for result, reranked_score, _keyword_hits, _strict_hits in ranked[:top_k]:
            context.examples.append({
                "name": result.document.metadata.get("name"),
                "description": result.document.metadata.get("description"),
                "code": result.document.metadata.get("code"),
                "project": result.document.metadata.get("project_name"),
                "category": result.document.metadata.get("category"),
                "score": reranked_score,
            })

        return context
    
    def search(self, query: str, top_k: int = 5) -> List[SearchResult]:
        """General search across all examples."""
        return self.corpus.search(query, top_k=top_k)
    
    def get_documentation(self, topic: str) -> List[str]:
        """
        Get documentation for a P language topic.
        
        Loads from the modular guide files for comprehensive coverage.
        Falls back to built-in summaries if guide files aren't available.
        """
        topic_lower = topic.lower().replace(" ", "_")
        
        # Map topics to guide files
        topic_guides = {
            "state_machine": "modular/p_machines_guide.txt",
            "machine": "modular/p_machines_guide.txt",
            "state": "modular/p_machines_guide.txt",
            "events": "modular/p_events_guide.txt",
            "event": "modular/p_events_guide.txt",
            "types": "modular/p_types_guide.txt",
            "type": "modular/p_types_guide.txt",
            "enum": "modular/p_enums_guide.txt",
            "enums": "modular/p_enums_guide.txt",
            "statement": "modular/p_statements_guide.txt",
            "statements": "modular/p_statements_guide.txt",
            "spec": "modular/p_spec_monitors_guide.txt",
            "specification": "modular/p_spec_monitors_guide.txt",
            "monitor": "modular/p_spec_monitors_guide.txt",
            "test": "modular/p_test_cases_guide.txt",
            "tests": "modular/p_test_cases_guide.txt",
            "module": "modular/p_module_system_guide.txt",
            "modules": "modular/p_module_system_guide.txt",
            "compiler": "modular/p_compiler_guide.txt",
            "error": "modular/p_common_compilation_errors.txt",
            "errors": "modular/p_common_compilation_errors.txt",
            "basics": "modular/p_basics.txt",
            "example": "modular/p_program_example.txt",
            "syntax": "P_syntax_guide.txt",
            "nuances": "p_nuances.txt",
        }
        
        # Find the best matching guide
        guide_file = None
        for key, gf in topic_guides.items():
            if key in topic_lower:
                guide_file = gf
                break
        
        if guide_file:
            content = self._load_guide(guide_file)
            if content:
                return [content]
        
        # Fallback built-in documentation
        docs = {
            "state_machine": [
                "## P State Machines",
                "State machines in P are defined with the `machine` keyword.",
                "Each machine has states, and transitions between states happen via `goto`.",
                "The `start state` defines the initial state of the machine.",
                "Sends are asynchronous: `send target, eventName, payload;`",
                "Event handlers: `on eEvent do HandlerFunction;` or `on eEvent goto StateName;`",
            ],
            "events": [
                "## P Events",
                "Events are the primary communication mechanism in P.",
                "Send events with: `send target, eventName, payload;`",
                "Handle events in states with: `on eventName do HandlerFunction;`",
                "Events with payloads require a separately declared type.",
            ],
            "types": [
                "## P Types",
                "P supports: int, bool, float, string, machine, event, any, data",
                "Named tuples: `type tRequest = (field1: int, field2: string);`",
                "Collections: `seq[int]`, `set[int]`, `map[int, string]`",
                "Enums: `enum Status { SUCCESS, FAILURE }`",
            ],
        }
        
        return docs.get(topic_lower, [f"No documentation found for: {topic}"])
    
    def get_stats(self) -> Dict[str, Any]:
        """Get statistics about the corpus."""
        total = self.corpus.count()
        by_category = self.corpus.count_by_category()
        
        return {
            "total_examples": total,
            "indexed": self._indexed,
            "by_category": by_category,
        }
    
    def _extract_keywords(self, text: str, max_keywords: int = 10) -> str:
        """Extract keywords from text."""
        import re
        # Remove common words and extract meaningful terms
        words = re.findall(r'\b[A-Za-z]{3,}\b', text.lower())
        
        # Common words to skip
        stopwords = {
            'the', 'and', 'for', 'that', 'with', 'this', 'from', 'are',
            'will', 'can', 'has', 'have', 'should', 'when', 'each', 'all',
            'its', 'which', 'other', 'their', 'them', 'then', 'into',
            'been', 'more', 'not', 'but', 'also', 'any', 'may', 'some',
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
    
    def _extract_doc_section(self, doc_content: str, section_tags: List[str]) -> str:
        """
        Extract specific XML-tagged sections from a documentation file.
        
        Returns concatenated content of matching sections, truncated
        to a reasonable length for prompt injection.
        """
        import re
        sections = []
        
        for tag in section_tags:
            pattern = rf'<{tag}>(.*?)</{tag}>'
            matches = re.findall(pattern, doc_content, re.DOTALL)
            for match in matches:
                cleaned = match.strip()
                if cleaned:
                    sections.append(cleaned)
        
        combined = "\n\n".join(sections)
        
        # Truncate to ~3000 chars to keep prompt size reasonable
        if len(combined) > 3000:
            combined = combined[:3000] + "\n... (truncated)"
        
        return combined


# Singleton instance
_rag_service: Optional[RAGService] = None


def get_rag_service() -> RAGService:
    """Get the singleton RAG service instance."""
    global _rag_service
    if _rag_service is None:
        _rag_service = RAGService()
    return _rag_service
