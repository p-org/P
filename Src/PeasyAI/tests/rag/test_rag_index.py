import pytest
import os
from pathlib import Path
import sys

PROJECT_ROOT = Path(__file__).parent.parent.parent
SRC_ROOT = PROJECT_ROOT / "src"
sys.path.insert(0, str(SRC_ROOT))

from core.rag.vector_store import VectorStore, Document, SearchResult
from core.rag.embeddings import get_embedding_provider
from core.rag.p_corpus import PCorpus, PExample


def test_vector_store_add_and_search(tmp_path):
    store = VectorStore(persist_path=str(tmp_path / "test_vectors.json"))
    provider = get_embedding_provider()

    doc1 = Document(
        id="doc1",
        content="The P programming language is designed for distributed systems.",
        embedding=provider.embed("The P programming language is designed for distributed systems."),
        metadata={"name": "doc1", "category": "documentation"},
    )
    doc2 = Document(
        id="doc2",
        content="Python is a popular programming language.",
        embedding=provider.embed("Python is a popular programming language."),
        metadata={"name": "doc2", "category": "documentation"},
    )
    store.add(doc1)
    store.add(doc2)

    assert store.count() == 2

    query_embedding = provider.embed("What is P programming language?")
    results = store.search(query_embedding, top_k=2)
    assert len(results) == 2
    assert isinstance(results[0], SearchResult)


def test_vector_store_persistence(tmp_path):
    persist_path = str(tmp_path / "persist_test.json")
    provider = get_embedding_provider()

    store1 = VectorStore(persist_path=persist_path)
    store1.add(Document(
        id="persist1",
        content="Test document one.",
        embedding=provider.embed("Test document one."),
        metadata={"name": "persist1"},
    ))
    store1.add(Document(
        id="persist2",
        content="Test document two.",
        embedding=provider.embed("Test document two."),
        metadata={"name": "persist2"},
    ))
    assert store1.count() == 2

    store2 = VectorStore(persist_path=persist_path)
    assert store2.count() == 2

    query_embedding = provider.embed("test document")
    results = store2.search(query_embedding, top_k=1)
    assert len(results) == 1


def test_corpus_index_and_search(tmp_path):
    corpus = PCorpus(store_path=str(tmp_path / "test_corpus"))

    example = PExample(
        id="test_machine_1",
        name="TestMachine",
        description="A simple test machine for distributed locking",
        code='machine TestMachine {\n  start state Init {\n    entry { }\n  }\n}',
        category="machine",
        tags=["test", "distributed-lock"],
        project_name="TestProject",
    )
    corpus.add_example(example)
    assert corpus.count() == 1

    results = corpus.search("distributed lock machine", top_k=1)
    assert len(results) >= 1
    assert results[0].document.metadata["name"] == "TestMachine"


def test_corpus_index_bundled_resources(tmp_path):
    corpus = PCorpus(store_path=str(tmp_path / "bundled_corpus"))
    count = corpus.index_bundled_resources()
    assert count > 0, "Should index at least some bundled resources"
    assert corpus.count() > 0
