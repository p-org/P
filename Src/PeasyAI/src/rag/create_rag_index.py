"""
SAMPLE INVOCATION
    python src/rag/create_rag_index.py resources/context_files 1000 resources/rag
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
from datetime import datetime

# Initialize the embedding model
RAG_MODEL_aMLML6v2 = 'all-MiniLM-L6-v2'
RAG_MODEL_aMLML12v2 = 'all-MiniLM-L12-v2'
MODEL_NAME = RAG_MODEL_aMLML12v2
MODEL = SentenceTransformer(MODEL_NAME)

def get_text_files(directory: str) -> List[str]:
    """Recursively get all .txt files in the directory and its subdirectories."""
    txt_files = []
    for root, _, files in os.walk(directory):
        for file in files:
            if file.endswith('.txt'):
                txt_files.append(os.path.join(root, file))
    return txt_files

def create_chunks(texts: List[str], chunk_size: int = 500, chunk_overlap: int = 100) -> List[str]:
    """Split texts into chunks using Langchain's RecursiveCharacterTextSplitter."""
    text_splitter = RecursiveCharacterTextSplitter(
        chunk_size=chunk_size,
        chunk_overlap=chunk_overlap,
        length_function=len,
        separators=["\n\n", "\n", ". ", " ", ""]
    )
    
    chunks = []
    for text in texts:
        chunks.extend(text_splitter.split_text(text))
    return chunks

def build_index(chunks: List[str]) -> faiss.IndexFlatL2:
    """Create a FAISS index from text chunks."""
    # Generate embeddings
    embeddings = MODEL.encode(chunks)
    
    # Normalize embeddings
    faiss.normalize_L2(embeddings)
    
    # Create FAISS index
    dimension = embeddings.shape[1]
    index = faiss.IndexFlatL2(dimension)
    index.add(embeddings.astype('float32'))
    
    return index

def save_index(index: faiss.IndexFlatL2, chunks: List[str], path: str):
    """Save the FAISS index and chunks to disk."""
    # Save the index
    faiss.write_index(index, f"{path}.faiss")
    
    # Save the chunks
    with open(f"{path}.pkl", "wb") as f:
        pickle.dump(chunks, f)

def load_index(path: str) -> Tuple[faiss.IndexFlatL2, List[str]]:
    """Load the FAISS index and chunks from disk."""
    # Load the index
    index = faiss.read_index(f"{path}.faiss")
    
    # Load the chunks
    with open(f"{path}.pkl", "rb") as f:
        chunks = pickle.load(f)
    
    return index, chunks

def search_index(index: faiss.IndexFlatL2, query: str, chunks: List[str], k: int = 5) -> List[Tuple[str, float]]:
    """Search the index for similar chunks."""
    # Generate query embedding
    query_embedding = MODEL.encode([query])
    
    # Normalize query embedding
    faiss.normalize_L2(query_embedding)
    
    # Search the index
    distances, indices = index.search(query_embedding.astype('float32'), k)
    
    # Get the corresponding chunks and create result tuples
    results = []
    for idx, dist in zip(indices[0], distances[0]):
        results.append((chunks[idx], float(dist)))
    
    return results


if __name__ == "__main__":
    directory = sys.argv[1]
    chunk_size = int(sys.argv[2])
    out_dir_arg = sys.argv[3]
    timestamp = datetime.now().strftime('%Y-%m-%d-%H-%M-%S')
    out_dir = f"{out_dir_arg}/{timestamp}"


    os.makedirs(out_dir)

    files = get_text_files(directory)
    
    texts = []
    sources = []
    for file in files:
        with open(file, 'r', encoding='utf-8') as f:
            texts.append(f.read())
            sources.append(file)
    
    chunks = create_chunks(texts, chunk_size = chunk_size)
    chunk_sources = []
    for i, text in enumerate(texts):
        text_chunks = create_chunks([text])
        chunk_sources.extend([sources[i]] * len(text_chunks))
    
    index = build_index(chunks)
    
    save_index(index, chunks, f"{out_dir}/faiss_index_{chunk_size}")
    with open(f"{out_dir}/faiss_index_{chunk_size}_sources.pkl", "wb") as f:
        pickle.dump(chunk_sources, f)
    
    # index, chunks = load_index("resources/faiss_index")

    query = "Enums syntax example"
    results = search_index(index, query, chunks)
    
    print("\n\nSearch Results for query:", query)
    print("=" * 80)
    for i, (chunk, score) in enumerate(results, 1):
        print(f"\nResult {i}:")
        print(f"Relevance Score: {1 - score:.2%}")
        print(f"Source: {os.path.basename(chunk_sources[chunks.index(chunk)])}")
        print(f" --- Content {"-"*60}")
        print(chunk)
        print(f" ------------{"-"*60}")
        print("=" * 80)
