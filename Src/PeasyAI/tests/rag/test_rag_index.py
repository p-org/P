import pytest
import os
import numpy as np
from pathlib import Path
import sys
sys.path.append("resources")
from create_rag_index import (
    get_text_files,
    create_chunks,
    build_index,
    save_index,
    load_index,
    search_index
)

def test_get_text_files():
    files = get_text_files("resources/context_files")
    assert len(files) > 0
    assert all(f.endswith('.txt') for f in files)
    assert "resources/context_files/about_p.txt" in files

def test_create_chunks():
    test_text = "This is a test document. It has multiple sentences. We will chunk it properly."
    chunks = create_chunks([test_text])
    assert len(chunks) > 0
    assert isinstance(chunks[0], str)
    assert len(chunks[0]) < len(test_text)

def test_build_and_search_index():
    test_texts = [
        "The P programming language is designed for distributed systems.",
        "Python is a popular programming language.",
        "Distributed systems are complex to design and implement."
    ]
    chunks = create_chunks(test_texts)
    index = build_index(chunks)
    
    # Test search
    query = "What is P programming language?"
    results = search_index(index, query, chunks, k=2)
    
    assert len(results) == 2
    assert isinstance(results[0], tuple)  # (chunk, score)
    assert "P programming" in results[0][0]

def test_save_and_load_index(tmp_path):
    test_texts = ["Test document one.", "Test document two."]
    chunks = create_chunks(test_texts)
    index = build_index(chunks)
    
    # Save index
    index_path = tmp_path / "test_index.faiss"
    save_index(index, chunks, str(index_path))
    
    # Load index
    loaded_index, loaded_chunks = load_index(str(index_path))
    
    # Verify search works with loaded index
    query = "test document"
    results = search_index(loaded_index, query, loaded_chunks, k=1)
    
    assert len(results) == 1
    assert "Test document" in results[0][0]
