import json
import matplotlib.pyplot as plt
import os

def get_dir_name(directory):
    """Extract directory name from path"""
    # Remove trailing slash if present
    directory = directory.rstrip('/')
    # Get the last part of the path
    return os.path.basename(directory)

def read_metrics(directory):
    """Read metrics from report.json and avg_token_usage.json in given directory"""
    metrics = {
        'avg_p_at_k': None, 
        'input_tokens': None,
        'settings': None
    }
    
    # Read report.json
    report_path = os.path.join(directory, 'report.json')
    try:
        with open(report_path, 'r') as f:
            data = json.load(f)
            metrics['name'] = data['name'] if 'name' in data else None
            metrics['avg_p_at_k'] = data['results']['avg_p_at_k']
            # Extract settings
            args = data['args']
            metrics['settings'] = {
                'k': args['k'],
                'n': args['n'],
                't': args['t'],
                'trials': args['trials']
            }
    except Exception as e:
        print(f"Error reading for report {report_path}: {e}")
    
    # Read input tokens
    tokens_path = os.path.join(directory, 'avg_token_usage.json')
    try:
        with open(tokens_path, 'r') as f:
            data = json.load(f)
            metrics['input_tokens'] = data['averages']['inputTokens']
    except Exception as e:
        print(f"Error reading for tokens {tokens_path}: {e}")
    
    return metrics

def visualize_metrics(directories, out_dir=None):
    """Create visualization of avg_p_at_k values and input tokens"""
    # Sort directories by name
    directories.sort()
    
    # Get metrics
    pk_values = []
    token_values = []
    labels = []
    settings = []
    
    for dir in directories:
        metrics = read_metrics(dir)
        if metrics['avg_p_at_k'] is not None and metrics['input_tokens'] is not None:
            pk_values.append(metrics['avg_p_at_k'])
            token_values.append(metrics['input_tokens'])
            dir_name = get_dir_name(dir)
            # label = f"{dir_name}_{metrics['name']}" if metrics['name'] else f"{dir_name}"
            label = f"{metrics['name']}" if metrics['name'] else f"{dir_name}"
            labels.append(label)
            if metrics['settings']:
                settings.append(metrics['settings'])
    
    # Create figure with two y-axes
    fig, ax1 = plt.subplots(figsize=(12, 6))
    ax2 = ax1.twinx()
    
    # Define sophisticated colors
    bar_color = '#465C7A'  # Muted blue-gray
    line_color = '#2A9D8F'  # Deep teal
    line_label_color = '#1B655C'  # Darker teal for better readability
    
    # Plot bars for P@K values on first y-axis
    x = range(len(pk_values))
    bars = ax1.bar(x, pk_values, color=bar_color, alpha=0.85, width=0.5)
    ax1.set_ylabel('Average P@K', color=bar_color, fontweight='medium', fontsize=11)
    ax1.tick_params(axis='y', labelcolor=bar_color, labelsize=10)
    
    # Plot line for token values on second y-axis
    line = ax2.plot(x, token_values, color=line_color, linewidth=2.5, marker='o', markersize=8)
    ax2.set_ylabel('Input Tokens', color=line_color, fontweight='medium', fontsize=11)
    ax2.tick_params(axis='y', labelcolor=line_color, labelsize=10)
    
    # Set background style
    ax1.set_facecolor('#F8F9FA')  # Light gray background
    fig.patch.set_facecolor('white')
    ax1.grid(True, linestyle='--', alpha=0.3, color='gray')
    
    # Customize plot
    # Check if settings are comparable
    if settings and all(s == settings[0] for s in settings):
        # All settings are the same, use them in the title
        s = settings[0]
        subtitle = f"pass@{s['k']}(n={s['n']},t={s['t']},trials={s['trials']})"
    else:
        subtitle = "WARNING: INCOMPARABLE SETTINGS"
    
    plt.suptitle('Average P@K Values and Input Tokens Across Runs', 
                fontweight='bold', fontsize=14)
    plt.title(subtitle, fontsize=12, pad=15)
    ax1.set_xlabel('Run Directory', fontweight='medium', fontsize=11)
    ax1.set_xticks(x)
    ax1.set_xticklabels(labels, rotation=30, ha='right', fontsize=10)
    
    # Add value labels with improved styling
    for i, (pk, tokens) in enumerate(zip(pk_values, token_values)):
        # P@K value above bars
        ax1.text(i + 0.2, pk, f'{pk:.2f}', ha='right', va='bottom', color=bar_color,
                fontweight='medium', fontsize=10)
        # Token value above points
        ax2.text(i, tokens, f'{int(tokens)}', ha='center', va='bottom', color=line_label_color,
                fontweight='bold', fontsize=10)
    
    # Add legend with improved styling
    from matplotlib.lines import Line2D
    legend_elements = [
        plt.Rectangle((0,0),1,1, color=bar_color, alpha=0.85),
        Line2D([0], [0], color=line_color, marker='o', linewidth=2.5, markersize=8)
    ]
    legend = ax1.legend(legend_elements, ['P@K', 'Input Tokens'], 
                       loc='upper left', framealpha=0.95, 
                       facecolor='white', edgecolor='none')
    plt.setp(legend.get_texts(), fontweight='medium', fontsize=10)
    
    # Adjust layout to prevent label cutoff
    plt.tight_layout()
    
    # Save plot
    plt.savefig(f'{out_dir if out_dir else "."}/pk_values_visualization.png')
    plt.close()


import argparse
from glob import glob
from pathlib import Path
import sys
if __name__ == "__main__":
    # Example usage:
    # directories = [
    #     'key-results/2025-06-24-15-04-43/',
    #     'key-results/2025-06-24-17-55-26/',
    #     'key-results/2025-06-24-22-26-45/',
    # ]

    parent_dir = sys.argv[1] # "key-results/dd2psrc"
    directories = [p for p in glob(f'{parent_dir}/*') if Path(p).is_dir()]

    # # If report.json doesn't exist in parent_dir, generate it using process_p_at_k_results
    # if not os.path.exists(f'{parent_dir}/report.json'):
    #     from evaluate_chatbot import process_p_at_k_results
    #     from src.evaluation.metrics.pass_at_k import compute_pass_at_k_value
        
    #     # Get results from each trial directory
    #     results = []
    #     for trial_dir in [d for d in glob(f'{parent_dir}/trial_*') if Path(d).is_dir()]:
    #         with open(f'{trial_dir}/result.json', 'r') as f:
    #             results_dict = json.load(f)
    #             p_at_k = compute_pass_at_k_value(results_dict)
    #             results.append((results_dict, p_at_k))
        
    #     # Process results to generate report.json
    #     # Create argparse.Namespace with minimal required attributes
    #     args = argparse.Namespace()
    #     args.metric = "pass_at_k"
    #     args.k = 1
    #     args.n = 1
    #     args.t = 1.0
    #     args.trials = len([d for d in glob(f'{parent_dir}/trial_*') if Path(d).is_dir()])
    #     args.benchmark_dir = None
    #     args.out_dir = parent_dir
    #     process_p_at_k_results(args, results, save_dir=parent_dir)
    visualize_metrics(directories, out_dir=parent_dir)
