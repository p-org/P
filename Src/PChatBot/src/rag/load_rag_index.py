"""
SAMPLE INVOCATION
    python src/rag/load_rag_index.py resources/rag/indices/2025-07-02-12-18-53/faiss_index_2000{.faiss,.pkl,_sources.pkl} "What is the syntax for enums"
    python src/rag/load_rag_index.py resources/rag/indices/2025-07-02-12-18-53/faiss_index_2000{.faiss,.pkl,_sources.pkl} "$(cat resources/p-model-benchmark/1_basicA/1_basicMachineStructure.prompt)"
"""

import os
from pathlib import Path
from typing import List, Tuple
import faiss
import numpy as np
from sentence_transformers import SentenceTransformer
from langchain.text_splitter import RecursiveCharacterTextSplitter
import pickle
import sys
import nltk
nltk.download('punkt')
nltk.download('punkt_tab')
from nltk.tokenize import sent_tokenize


def load_index(model, faiss_index_path: str, chunks_path: str) -> Tuple[faiss.IndexFlatL2, List[str]]:
    """Load the FAISS index and chunks from disk."""
    # Load the index
    index = faiss.read_index(faiss_index_path)
    
    # Load the chunks
    with open(chunks_path, "rb") as f:
        chunks = pickle.load(f)
    
    return index, chunks

def search_index(model, index: faiss.IndexFlatL2, query: str, chunks: List[str], k: int = 5) -> List[Tuple[str, float]]:
    """Search the index for similar chunks."""
    # Generate query embedding
    print("CREATING QUERY EMBEDDING....")
    query_embedding = model.encode([query])
    
    # Normalize query embedding
    print("NORMALIZING....")
    faiss.normalize_L2(query_embedding)
    
    # Search the index
    print("SEARCHING INDEX....")
    distances, indices = index.search(query_embedding.astype('float32'), k)
    print("SEARCH COMPLETE!")
    # Get the corresponding chunks and create result tuples
    results = []
    for idx, dist in zip(indices[0], distances[0]):
        results.append((chunks[idx], float(dist)))
    
    return results

def get_summary_sentences(model, query):
    return sent_tokenize(query)

if __name__ == "__main__":
    faiss_index_path = sys.argv[1]
    chunks_path = sys.argv[2]
    chunk_sources_path = sys.argv[3]
    query = sys.argv[4]

    RAG_MODEL = SentenceTransformer('all-MiniLM-L6-v2')
    SUMMARY_MODEL = SentenceTransformer('paraphrase-MiniLM-L6-v2')


    index, chunks = load_index(RAG_MODEL, faiss_index_path, chunks_path)

    results = search_index(RAG_MODEL, index, query, chunks)
    
    with open(chunk_sources_path, "rb") as f:
        chunk_sources = pickle.load(f)
    
    summary = get_summary_sentences(SUMMARY_MODEL, query)
    
    print("\n\nSearch Results for query:", query)
    print(f"Query summary: {summary}")
    print("=" * 80)
    for i, (chunk, score) in enumerate(results, 1):
        print(f"\nResult {i}:")
        print(f"Relevance Score: {1 - score:.2%}")
        print(f"Source: {os.path.basename(chunk_sources[chunks.index(chunk)])}")
        print(f" --- Content {"-"*60}")
        print(chunk)
        print(f" ------------{"-"*60}")
        print("=" * 80)
