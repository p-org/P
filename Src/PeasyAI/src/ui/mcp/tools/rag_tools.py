"""RAG tools for MCP."""

from typing import Dict, Any, Optional
from pydantic import BaseModel, Field
from pathlib import Path
import logging

logger = logging.getLogger(__name__)

try:
    from core.rag import RAGService, get_rag_service, PExample
    HAS_RAG = True
except ImportError:
    HAS_RAG = False
    logger.warning("RAG module not available")


class SearchExamplesParams(BaseModel):
    """Parameters for searching P examples"""
    query: str = Field(..., description="Natural language query or code snippet to search for")
    category: Optional[str] = Field(
        default=None,
        description="Filter by category: 'machine', 'spec', 'test', 'types', 'documentation', 'full_project'"
    )
    top_k: int = Field(default=5, description="Number of results to return")


class GetContextParams(BaseModel):
    """Parameters for getting generation context"""
    context_type: str = Field(
        ...,
        description="Type of context: 'machine', 'spec', 'test', 'types'"
    )
    description: str = Field(..., description="Description of what you're generating")
    design_doc: Optional[str] = Field(
        default=None,
        description="Optional design document for additional context"
    )
    num_examples: int = Field(default=3, description="Number of examples to include")


class IndexPFilesParams(BaseModel):
    """Parameters for indexing P files"""
    path: str = Field(..., description="Path to a P file or directory to index")
    project_name: Optional[str] = Field(
        default=None,
        description="Optional project name for the indexed files"
    )


class GetProtocolExamplesParams(BaseModel):
    """Parameters for getting protocol examples"""
    protocol_name: str = Field(
        ...,
        description="Name of the protocol (e.g., 'paxos', 'two-phase commit', 'raft')"
    )
    top_k: int = Field(default=5, description="Number of examples to return")


class GetCorpusStatsParams(BaseModel):
    """Parameters for getting corpus statistics"""
    pass


def register_rag_tools(mcp, with_metadata):
    """Register RAG tools."""

    @mcp.tool(
        name="search_p_examples",
        description="""Search the P program database for similar examples.

Use this to find real P code examples that match your needs:
- Search by concept: "distributed lock protocol"
- Search by pattern: "state machine with deferred events"
- Search by code: paste a code snippet to find similar code

Returns relevant P code examples with descriptions and similarity scores."""
    )
    def search_p_examples(params: SearchExamplesParams) -> Dict[str, Any]:
        if not HAS_RAG:
            payload = {
                "success": False,
                "error": "RAG module not available. Install sentence-transformers for full functionality."
            }
            return with_metadata("search_p_examples", payload)

        logger.info(f"[TOOL] search_p_examples: {params.query[:50]}...")

        try:
            rag = get_rag_service()
            results = rag.search(
                query=params.query,
                top_k=params.top_k
            )

            if params.category:
                results = [r for r in results if r.document.metadata.get("category") == params.category]

            examples = []
            for result in results:
                examples.append({
                    "name": result.document.metadata.get("name"),
                    "description": result.document.metadata.get("description"),
                    "category": result.document.metadata.get("category"),
                    "code": result.document.metadata.get("code"),
                    "project": result.document.metadata.get("project_name"),
                    "source_file": result.document.metadata.get("source_file"),
                    "relevance_score": round(result.score, 3)
                })

            payload = {
                "success": True,
                "query": params.query,
                "results": examples,
                "total_found": len(examples),
                "corpus_size": rag.get_stats()["total_examples"]
            }
            return with_metadata("search_p_examples", payload)
        except Exception as e:
            logger.error(f"Search error: {e}")
            payload = {"success": False, "error": str(e)}
            return with_metadata("search_p_examples", payload)

    @mcp.tool(
        name="get_generation_context",
        description="""Get contextual examples and hints for P code generation.

Before generating P code, call this to get relevant examples that will improve generation quality:
- For machines: get similar state machine implementations
- For specs: get similar safety/liveness specifications  
- For tests: get similar test drivers
- For types: get similar type/event definitions

Returns examples with code and syntax hints to use in your prompt."""
    )
    def get_generation_context(params: GetContextParams) -> Dict[str, Any]:
        if not HAS_RAG:
            payload = {
                "success": False,
                "error": "RAG module not available. Install sentence-transformers for full functionality."
            }
            return with_metadata("get_generation_context", payload)

        logger.info(f"[TOOL] get_generation_context: {params.context_type}")

        try:
            rag = get_rag_service()

            if params.context_type == "machine":
                context = rag.get_machine_context(
                    params.description,
                    design_doc=params.design_doc,
                    num_examples=params.num_examples
                )
            elif params.context_type == "spec":
                context = rag.get_spec_context(
                    params.description,
                    num_examples=params.num_examples
                )
            elif params.context_type == "test":
                context = rag.get_test_context(
                    params.description,
                    num_examples=params.num_examples
                )
            elif params.context_type == "types":
                context = rag.get_types_context(
                    params.description,
                    num_examples=params.num_examples
                )
            else:
                payload = {
                    "success": False,
                    "error": f"Unknown context type: {params.context_type}. Use: machine, spec, test, types"
                }
                return with_metadata("get_generation_context", payload)

            payload = {
                "success": True,
                "context_type": params.context_type,
                "examples": context.examples,
                "syntax_hints": context.syntax_hints,
                "documentation": context.documentation,
                "prompt_section": context.to_prompt_section()
            }
            return with_metadata("get_generation_context", payload)
        except Exception as e:
            logger.error(f"Context error: {e}")
            payload = {"success": False, "error": str(e)}
            return with_metadata("get_generation_context", payload)

    @mcp.tool(
        name="index_p_examples",
        description="""Index P files into the examples database.

Use this to add your own P programs to the searchable corpus:
- Index a single P file
- Index an entire directory of P files
- Index the official P tutorial examples

Indexed examples can then be found via search_p_examples."""
    )
    def index_p_examples(params: IndexPFilesParams) -> Dict[str, Any]:
        if not HAS_RAG:
            payload = {
                "success": False,
                "error": "RAG module not available."
            }
            return with_metadata("index_p_examples", payload)

        logger.info(f"[TOOL] index_p_examples: {params.path}")

        try:
            rag = get_rag_service()
            path = Path(params.path)

            if not path.exists():
                payload = {"success": False, "error": f"Path not found: {params.path}"}
                return with_metadata("index_p_examples", payload)

            if path.is_file():
                count = rag.index_file(str(path))
            else:
                count = rag.index_directory(str(path))

            payload = {
                "success": True,
                "indexed_count": count,
                "total_in_corpus": rag.get_stats()["total_examples"],
                "message": f"Indexed {count} examples from {params.path}"
            }
            return with_metadata("index_p_examples", payload)
        except Exception as e:
            logger.error(f"Index error: {e}")
            payload = {"success": False, "error": str(e)}
            return with_metadata("index_p_examples", payload)

    @mcp.tool(
        name="get_protocol_examples",
        description="""Get P code examples for a specific distributed protocol.

Use this when implementing common distributed systems protocols:
- "paxos" - consensus protocol
- "two-phase commit" - distributed transaction
- "raft" - consensus/leader election
- "failure detector" - detecting node failures

Returns relevant examples from the P corpus."""
    )
    def get_protocol_examples(params: GetProtocolExamplesParams) -> Dict[str, Any]:
        if not HAS_RAG:
            payload = {
                "success": False,
                "error": "RAG module not available."
            }
            return with_metadata("get_protocol_examples", payload)

        logger.info(f"[TOOL] get_protocol_examples: {params.protocol_name}")

        try:
            rag = get_rag_service()
            context = rag.get_protocol_examples(
                params.protocol_name,
                top_k=params.top_k
            )

            payload = {
                "success": True,
                "protocol": params.protocol_name,
                "examples": context.examples,
                "total_found": len(context.examples)
            }
            return with_metadata("get_protocol_examples", payload)
        except Exception as e:
            logger.error(f"Protocol examples error: {e}")
            payload = {"success": False, "error": str(e)}
            return with_metadata("get_protocol_examples", payload)

    @mcp.tool(
        name="get_corpus_stats",
        description="Get statistics about the P program corpus (number of indexed examples, etc.)"
    )
    def get_corpus_stats(params: GetCorpusStatsParams) -> Dict[str, Any]:
        if not HAS_RAG:
            payload = {
                "success": False,
                "error": "RAG module not available."
            }
            return with_metadata("get_corpus_stats", payload)

        try:
            rag = get_rag_service()
            stats = rag.get_stats()

            by_category = stats.get("by_category", {})

            payload = {
                "success": True,
                "total_examples": stats["total_examples"],
                "indexed": stats["indexed"],
                "categories": ["machine", "spec", "test", "types", "documentation", "full_project"],
                "by_category": by_category,
                "message": f"Corpus contains {stats['total_examples']} indexed P code examples"
            }
            return with_metadata("get_corpus_stats", payload)
        except Exception as e:
            payload = {"success": False, "error": str(e)}
            return with_metadata("get_corpus_stats", payload)

    return {
        "search_p_examples": search_p_examples,
        "get_generation_context": get_generation_context,
        "index_p_examples": index_p_examples,
        "get_protocol_examples": get_protocol_examples,
        "get_corpus_stats": get_corpus_stats,
    }
