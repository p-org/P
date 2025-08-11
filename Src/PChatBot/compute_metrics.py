import os
import json
from typing import Dict, List, Tuple

def get_token_usage_files(directory_path: str) -> List[str]:
    """
    Find all token_usage.json files in the given directory and its subdirectories.
    
    Args:
        directory_path (str): Path to search for token_usage.json files
        
    Returns:
        List[str]: List of paths to token_usage.json files
    """
    token_usage_files = []
    for root, _, files in os.walk(directory_path):
        for file in files:
            if file == "token_usage.json":
                token_usage_files.append(os.path.join(root, file))
    return token_usage_files

def get_empty_metrics() -> Dict[str, float]:
    """
    Get a dictionary with zero-initialized metrics.
    
    Returns:
        Dict[str, float]: Dictionary with metric names and initial values
    """
    return {
        "inputTokens": 0,
        "outputTokens": 0,
        "totalTokens": 0,
        "cacheReadInputTokens": 0,
        "cacheWriteInputTokens": 0
    }

def read_token_usage_file(file_path: str) -> Dict[str, float]:
    """
    Read and parse a single token_usage.json file.
    
    Args:
        file_path (str): Path to the token_usage.json file
        
    Returns:
        Dict[str, float]: Dictionary containing the cumulative metrics from the file
    """
    try:
        with open(file_path, 'r') as f:
            data = json.load(f)
            return data.get('cumulative', {})
    except (json.JSONDecodeError, IOError) as e:
        print(f"Error reading {file_path}: {e}")
        return {}

def aggregate_metrics(files: List[str]) -> Tuple[Dict[str, float], int]:
    """
    Aggregate metrics from multiple token usage files.
    
    Args:
        files (List[str]): List of paths to token_usage.json files
        
    Returns:
        Tuple[Dict[str, float], int]: Total metrics and count of processed files
    """
    total_metrics = get_empty_metrics()
    file_count = 0
    
    for file_path in files:
        metrics = read_token_usage_file(file_path)
        if metrics:
            for metric in total_metrics:
                total_metrics[metric] += metrics.get(metric, 0)
            file_count += 1
            
    return total_metrics, file_count

def compute_averages(total_metrics: Dict[str, float], file_count: int) -> Dict[str, float]:
    """
    Compute average metrics from total metrics.
    
    Args:
        total_metrics (Dict[str, float]): Sum of all metrics
        file_count (int): Number of files processed
        
    Returns:
        Dict[str, float]: Dictionary of averaged metrics
    """
    if file_count == 0:
        return get_empty_metrics()
    
    return {
        metric: total_metrics[metric] / file_count 
        for metric in total_metrics
    }

def save_results(directory_path: str, avg_metrics: Dict[str, float], file_count: int) -> None:
    """
    Save computed averages to a JSON file.
    
    Args:
        directory_path (str): Directory to save the results
        avg_metrics (Dict[str, float]): Computed average metrics
        file_count (int): Number of files processed
    """
    result = {
        "averages": avg_metrics,
        "total_files_processed": file_count
    }
    
    output_path = os.path.join(directory_path, "avg_token_usage.json")
    try:
        with open(output_path, 'w') as f:
            json.dump(result, f, indent=4)
        print(f"Results saved to {output_path}")
    except IOError as e:
        print(f"Error saving results: {e}")

def compute_average_token_usage(directory_path: str) -> None:
    """
    Compute and save average token usage across all token_usage.json files.
    
    Args:
        directory_path (str): Path to the directory containing trial folders
    """
    # Find all token usage files
    token_usage_files = get_token_usage_files(directory_path)
    
    if not token_usage_files:
        print("No token_usage.json files found in the directory")
        return
        
    # Aggregate metrics from all files
    total_metrics, file_count = aggregate_metrics(token_usage_files)
    
    # Compute averages
    avg_metrics = compute_averages(total_metrics, file_count)
    
    # Save results
    save_results(directory_path, avg_metrics, file_count)

if __name__ == "__main__":
    import sys
    if len(sys.argv) != 2:
        print("Usage: python compute_metrics.py <directory_path>")
        sys.exit(1)
    compute_average_token_usage(sys.argv[1])
