"""RAG tools for MCP."""

from typing import Dict, Any, Optional
from pydantic import BaseModel, Field
from pathlib import Path
import logging

from core.security import PathSecurityError, validate_project_path

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




def register_rag_tools(mcp, with_metadata):
    """Register RAG tools."""

    @mcp.tool(
        name="peasy-ai-search-examples",
        description="""Search the P program database for similar examples.

Use this to find real P code examples that match your needs:
- Search by concept: "distributed lock protocol"
- Search by pattern: "state machine with deferred events"
- Search by protocol: "paxos", "two-phase commit", "raft", "failure detector"
- Search by code: paste a code snippet to find similar code

Filter results by category: 'machine', 'spec', 'test', 'types', 'documentation', 'full_project'.
Returns relevant P code examples with descriptions, similarity scores, and corpus size."""
    )
    def search_p_examples(params: SearchExamplesParams) -> Dict[str, Any]:
        if not HAS_RAG:
            payload = {
                "success": False,
                "error": "RAG module not available. Install sentence-transformers for full functionality."
            }
            return with_metadata("peasy-ai-search-examples", payload)

        logger.info(f"[TOOL] peasy-ai-search-examples: {params.query[:50]}...")

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
            return with_metadata("peasy-ai-search-examples", payload)
        except Exception as e:
            logger.error(f"Search error: {e}")
            payload = {"success": False, "error": f"Search failed: {type(e).__name__}"}
            return with_metadata("peasy-ai-search-examples", payload)

    @mcp.tool(
        name="peasy-ai-get-context",
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
            return with_metadata("peasy-ai-get-context", payload)

        logger.info(f"[TOOL] peasy-ai-get-context: {params.context_type}")

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
                return with_metadata("peasy-ai-get-context", payload)

            payload = {
                "success": True,
                "context_type": params.context_type,
                "examples": context.examples,
                "syntax_hints": context.syntax_hints,
                "documentation": context.documentation,
                "prompt_section": context.to_prompt_section()
            }
            return with_metadata("peasy-ai-get-context", payload)
        except Exception as e:
            logger.error(f"Context error: {e}")
            payload = {"success": False, "error": f"Context lookup failed: {type(e).__name__}"}
            return with_metadata("peasy-ai-get-context", payload)

    @mcp.tool(
        name="peasy-ai-index-examples",
        description="""Index P files into the examples database.

Use this to add your own P programs to the searchable corpus:
- Index a single P file
- Index an entire directory of P files
- Index the official P tutorial examples

Indexed examples can then be found via peasy-ai-search-examples."""
    )
    def index_p_examples(params: IndexPFilesParams) -> Dict[str, Any]:
        if not HAS_RAG:
            payload = {
                "success": False,
                "error": "RAG module not available."
            }
            return with_metadata("peasy-ai-index-examples", payload)

        logger.info(f"[TOOL] peasy-ai-index-examples: {params.path}")

        try:
            rag = get_rag_service()
            path = Path(params.path).resolve()

            # Validate path to prevent path traversal attacks
            try:
                validate_project_path(str(path))
            except PathSecurityError as e:
                payload = {"success": False, "error": str(e)}
                return with_metadata("peasy-ai-index-examples", payload)

            if not path.exists():
                payload = {"success": False, "error": f"Path not found: {path.name}"}
                return with_metadata("peasy-ai-index-examples", payload)

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
            return with_metadata("peasy-ai-index-examples", payload)
        except Exception as e:
            logger.error(f"Index error: {e}")
            payload = {"success": False, "error": f"Indexing failed: {type(e).__name__}"}
            return with_metadata("peasy-ai-index-examples", payload)

    return {
        "search_p_examples": search_p_examples,
        "get_generation_context": get_generation_context,
        "index_p_examples": index_p_examples,
    }
