import json
import matplotlib.pyplot as plt
import seaborn as sns
from collections import defaultdict
from matplotlib.ticker import FuncFormatter
import sys

# Set style for better-looking plots
sns.set_style("whitegrid")
plt.rcParams['figure.figsize'] = (12, 6)

FILENAME = sys.argv[1] if len(sys.argv) > 1 else 'rank_summary_smoke.json'

# Load the JSON data
with open(FILENAME, 'r') as f:
    data = json.load(f)

# Extract model names
models = list(data.keys())

# Extract k values from the first model (assuming all models have same k values)
k_values = sorted([int(k) for k in data[models[0]].keys() if (int(k) <= 60)])

# Initialize data structures for plotting
goals_percentage_by_model = defaultdict(list)
total_tokens_by_model = defaultdict(list)

# Process data for each model and k value
for model in models:
    # Store the last known values for each benchmark
    benchmark_last_values = {}
    
    for k in k_values:
        k_str = str(k)
        
        # Calculate percentage of goals learned and total tokens for this k
        total_found_goals = 0
        total_goals = 0
        total_tokens_sum = 0
        benchmark_count = 0
        
        # Get all benchmarks that appear in any k value for this model
        all_benchmarks = set()
        for k_val in data[model].keys():
            if int(k_val) <= 60:
                if data[model][k_val] is not None:
                    all_benchmarks.update(data[model][k_val].keys())
        
        # Process each benchmark
        for benchmark in all_benchmarks:
            summary = None
            
            # Check if benchmark exists at current k
            if int(k_str) > 60:
                continue
            if k_str in data[model] and benchmark in data[model][k_str]:
                summary = data[model][k_str][benchmark]
                # Update last known values if summary is not null
                if summary is not None:
                    benchmark_last_values[benchmark] = {
                        'found_goals': summary.get('found_goals', 0),
                        'total_goals': summary.get('total_goals', 0),
                        'total_tokens': summary.get('total_tokens', 0)
                    }
            
            # If no data at current k, use last known values
            if summary is None and benchmark in benchmark_last_values:
                summary = benchmark_last_values[benchmark]
            
            # Add to totals if we have data
            if summary is not None:
                total_found_goals += summary.get('found_goals', 0)
                total_goals += summary.get('total_goals', 0)
                total_tokens_sum += summary.get('total_tokens', 0)
                benchmark_count += 1
        total_tokens_sum /= benchmark_count
        
        # Calculate percentage
        if total_goals > 0:
            percentage = (total_found_goals / total_goals) * 100
        else:
            percentage = 0
        
        goals_percentage_by_model[model].append(percentage)
        total_tokens_by_model[model].append(total_tokens_sum)

# Create figure with two subplots
fig, (ax1, ax2) = plt.subplots(1, 2, figsize=(16, 6))

# Plot 1: Percentage of goals learned
for model in models:
    # Create a shorter label for the model
    model_label = model.split(':')[0].split('.')[-1]  # Extract just the model name
    ax1.plot(k_values, goals_percentage_by_model[model], marker='o', linewidth=2, 
             markersize=8, label=model_label)

ax1.set_xlabel('k (top-k value)', fontsize=12)
ax1.set_ylabel('Percentage of Goals Learned (%)', fontsize=12)
ax1.set_title('Percentage of Goals Learned vs. k for Different Models', fontsize=14, fontweight='bold')
ax1.legend(loc='best', fontsize=12)
ax1.grid(True, alpha=0.3)
ax1.set_xticks(k_values)

# Plot 2: Total tokens (cost)
for model in models:
    model_label = model.split(':')[0].split('.')[-1]  # Extract just the model name
    ax2.plot(k_values, total_tokens_by_model[model], marker='s', linewidth=2, 
             markersize=8, label=model_label)

ax2.set_xlabel('k (top-k value)', fontsize=12)
ax2.set_ylabel('Average Tokens (Cost)', fontsize=12)
ax2.set_title('Average Tokens (Cost) vs. k', fontsize=14, fontweight='bold')
ax2.legend(loc='best', fontsize=10)
ax2.grid(True, alpha=0.3)
ax2.set_xticks(k_values)

# Format y-axis for tokens to show in thousands
ax2.yaxis.set_major_formatter(FuncFormatter(lambda x, p: f'{x/1000:.0f}K'))

# Adjust layout to prevent overlap
plt.tight_layout()

# Save the figure
plt.savefig('rank_summary_visualization.png', dpi=300, bbox_inches='tight')
plt.savefig('rank_summary_visualization.pdf', bbox_inches='tight')

# Also create individual plots for better visibility
# Individual plot for goals percentage
plt.figure(figsize=(10, 6))
for model in models:
    model_label = model.split(':')[0].split('.')[-1]
    plt.plot(k_values, goals_percentage_by_model[model], marker='o', linewidth=2, 
             markersize=8, label=model_label)

plt.xlabel('k (top-k value)', fontsize=12)
plt.ylabel('Percentage of Goals Learned (%)', fontsize=12)
plt.title('Percentage of Goals Learned vs. k for Different Models', fontsize=14, fontweight='bold')
plt.legend(loc='best', fontsize=10)
plt.grid(True, alpha=0.3)
plt.xticks(k_values)
plt.tight_layout()
plt.savefig('goals_percentage_plot.png', dpi=300, bbox_inches='tight')

# Individual plot for total tokens
plt.figure(figsize=(10, 6))
for model in models:
    model_label = model.split(':')[0].split('.')[-1]
    plt.plot(k_values, total_tokens_by_model[model], marker='s', linewidth=2, 
             markersize=8, label=model_label)

plt.xlabel('k (top-k value)', fontsize=12)
plt.ylabel('Total Tokens (Cost)', fontsize=12)
plt.title('Total Tokens (Cost) vs. k for Different Models', fontsize=14, fontweight='bold')
plt.legend(loc='best', fontsize=10)
plt.grid(True, alpha=0.3)
plt.xticks(k_values)
plt.gca().yaxis.set_major_formatter(FuncFormatter(lambda x, p: f'{x/1000:.0f}K'))
plt.tight_layout()
plt.savefig('total_tokens_plot.png', dpi=300, bbox_inches='tight')

# Print summary statistics
print("Summary Statistics:")
print("=" * 60)
for model in models:
    model_label = model.split(':')[0].split('.')[-1]
    print(f"\nModel: {model_label}")
    print("-" * 40)
    for i, k in enumerate(k_values):
        print(f"k={k}: {goals_percentage_by_model[model][i]:.1f}% goals, {total_tokens_by_model[model][i]:,} tokens")

def plot_sonnet4_by_benchmark():
    """
    Create a single plot showing Sonnet 4 performance across all benchmarks.
    Uses forward-fill for missing data points (if all specs found at k1, use same value for k2 > k1).
    Saves both PNG and PDF formats.
    """
    # Load the JSON data
    with open(FILENAME, 'r') as f:
        data = json.load(f)
    
    # Find Sonnet 4 model key
    sonnet4_key = None
    for model_key in data.keys():
        if 'sonnet-4' in model_key.lower():
            sonnet4_key = model_key
            break
    
    if not sonnet4_key:
        print("Sonnet 4 model not found in data!")
        return
    
    print(f"Found Sonnet 4 model: {sonnet4_key}")
    
    sonnet4_data = data[sonnet4_key]
    
    # Get available k values (excluding k=60 for single column layout)
    k_values = sorted([int(k) for k in sonnet4_data.keys() if k.isdigit() and int(k) <= 50])
    print(f"Available k values: {k_values}")
    
    # Get all benchmarks
    all_benchmarks = set()
    for k_str in sonnet4_data.keys():
        if k_str.isdigit() and int(k_str) <= 70:
            if sonnet4_data[k_str] is not None:
                all_benchmarks.update(sonnet4_data[k_str].keys())
    
    # Remove benchmarks with errors
    valid_benchmarks = []
    for benchmark in all_benchmarks:
        has_valid_data = False
        for k in k_values:
            k_str = str(k)
            if (k_str in sonnet4_data and 
                benchmark in sonnet4_data[k_str] and 
                sonnet4_data[k_str][benchmark] is not None and
                'error' not in sonnet4_data[k_str][benchmark]):
                has_valid_data = True
                break
        if has_valid_data:
            valid_benchmarks.append(benchmark)
    
    valid_benchmarks.sort()
    print(f"Valid benchmarks: {valid_benchmarks}")
    
    # Process each benchmark with forward-fill logic
    benchmark_data_dict = {}
    
    for benchmark in valid_benchmarks:
        percentages = []
        num_found = []
        last_valid_percentage = None
        
        for k in k_values:
            k_str = str(k)
            
            # Check if data exists for this k
            if (k_str in sonnet4_data and 
                benchmark in sonnet4_data[k_str] and 
                sonnet4_data[k_str][benchmark] is not None and
                'error' not in sonnet4_data[k_str][benchmark]):
                
                benchmark_data = sonnet4_data[k_str][benchmark]
                found_goals = benchmark_data.get('found_goals', 0)
                total_goals = benchmark_data.get('total_goals', 0)
                
                if total_goals > 0:
                    percentage = (found_goals / total_goals) * 100
                else:
                    percentage = 0
                
                if last_valid_percentage is None or last_valid_percentage < percentage:
                    last_valid_percentage = percentage
                    percentages.append(percentage)
                    num_found.append(f"{found_goals}/{total_goals}")
                else:
                    # last_valid >= percentage, use last valid
                    percentages.append(last_valid_percentage)
                    num_found.append(num_found[-1])
                
            else:
                # Use forward-fill: if we have a previous value, use it
                if last_valid_percentage is not None:
                    percentages.append(last_valid_percentage)
                    num_found.append(num_found[-1])
                else:
                    # If no previous value, skip this k for this benchmark
                    percentages.append(None)
                    num_found.append(None)
        
        # Store data for this benchmark
        benchmark_data_dict[benchmark] = num_found
    
    # Get learned specifications count for each benchmark
    def get_learned_specs(benchmark_name):
        """Count lines in pruned_invariants.txt file for a benchmark"""
        try:
            with open(f"{benchmark_name}/pruned_invariants.txt", 'r') as f:
                lines = [line.strip() for line in f.readlines() if line.strip()]
                return len(lines)
        except FileNotFoundError:
            return "N/A"
    
    # Create a table instead of bar chart
    import numpy as np
    
    # Prepare data for table
    table_data = []
    row_labels = []

    row_label_maps = {
        'Kermit2PC': 'MVCC-2PC',
        'JournalLeaderElection': 'DBLeaderElection',
        'ClockBound': 'GlobalCLock',
        'Raft_hint': 'Raft',
        'paxos_hint': 'Paxos'
    }

    # Separate benchmarks into proprietary and non-proprietary
    proprietary_benchmarks = {'Kermit2PC', 'JournalLeaderElection', 'ClockBound'}
    
    non_proprietary = []
    proprietary = []
    
    for benchmark in valid_benchmarks:
        if benchmark in proprietary_benchmarks:
            proprietary.append(benchmark)
        else:
            non_proprietary.append(benchmark)
    
    # Sort each group
    non_proprietary.sort()
    proprietary.sort()
    
    # Process non-proprietary benchmarks first
    for benchmark in non_proprietary:
        num_found = benchmark_data_dict[benchmark]
        row_data = []
        
        # Add performance data for each k value
        for txt in num_found:
            if txt is not None:
                row_data.append(txt)
            else:
                row_data.append("-")
        
        table_data.append(row_data)
        
        # Get learned specs count and create multi-line label
        learned_count = get_learned_specs(benchmark)
        benchmark_name = row_label_maps.get(benchmark, benchmark)
        if benchmark_name is None:
            benchmark_name = benchmark
        benchmark_name = benchmark_name.replace("_", " ").title()
        
        # Create simplified label for single column layout
        if learned_count != "N/A":
            row_label = f"{benchmark_name} ({learned_count})"
        else:
            row_label = f"{benchmark_name} (N/A)"
        
        row_labels.append(row_label)
    
    # Process proprietary benchmarks
    for benchmark in proprietary:
        num_found = benchmark_data_dict[benchmark]
        row_data = []
        
        # Add performance data for each k value
        for txt in num_found:
            if txt is not None:
                row_data.append(txt)
            else:
                row_data.append("-")
        
        table_data.append(row_data)
        
        # Get learned specs count and create multi-line label
        learned_count = get_learned_specs(benchmark)
        benchmark_name = row_label_maps.get(benchmark, benchmark)
        if benchmark_name is None:
            benchmark_name = benchmark
        benchmark_name = benchmark_name.replace("_", " ").title()
        
        # Create simplified label for single column layout
        if learned_count != "N/A":
            row_label = f"{benchmark_name} ({learned_count})"
        else:
            row_label = f"{benchmark_name} (N/A)"
        
        row_labels.append(row_label)
    
    # Calculate and add summary row with percentages
    summary_row = []
    for k_idx in range(len(k_values)):
        total_found = 0
        total_specs = 0
        
        for benchmark in valid_benchmarks:
            num_found_text = benchmark_data_dict[benchmark][k_idx]
            if num_found_text is not None and "/" in num_found_text:
                found, total = num_found_text.split('/')
                total_found += int(found)
                total_specs += int(total)
        
        if total_specs > 0:
            percentage = (total_found / total_specs) * 100
            summary_row.append(f"{percentage:.1f}%")
        else:
            summary_row.append("0.0%")
    
    # Add summary row to table data and labels
    table_data.append(summary_row)
    row_labels.append("Spec Included %")
    
    # Calculate and add pruned percentage row
    pruned_row = []
    for k_idx in range(len(k_values)):
        k = k_values[k_idx]
        total_pruned = 0
        total_learned = 0
        
        for benchmark in valid_benchmarks:
            learned_count = get_learned_specs(benchmark)
            if learned_count != "N/A":
                # Ranked set size is min(learned_specs, k)
                ranked_set_size = min(learned_count, k)
                total_pruned += ranked_set_size
                total_learned += learned_count
        
        if total_learned > 0:
            pruned_percentage = 100 - (total_pruned / total_learned) * 100
            pruned_row.append(f"{pruned_percentage:.1f}%")
        else:
            pruned_row.append("0.0%")
    
    # Add pruned row to table data and labels
    table_data.append(pruned_row)
    row_labels.append("Pruned %")
    
    # Create figure for table (optimized for single column with reduced height)
    fig, ax = plt.subplots(figsize=(6.5, 8))
    ax.axis('tight')
    ax.axis('off')
    
    # Column headers
    col_labels = [f"k={k}" for k in k_values]
    
    # Create table
    table = ax.table(cellText=table_data,
                    rowLabels=row_labels,
                    colLabels=col_labels,
                    cellLoc='center',
                    loc='center',
                    bbox=[0, 0, 1, 1])
    
    # Style the table for single column layout
    table.auto_set_font_size(False)
    table.set_fontsize(12)  # Smaller font size for single column
    table.scale(1, 0.5)  # Much more aggressive row compression for minimal cell height
    
    # Color coding for better readability
    # Calculate indices for different sections
    non_proprietary_count = len(non_proprietary)
    proprietary_count = len(proprietary)
    total_benchmark_count = non_proprietary_count + proprietary_count
    summary_row_idx = total_benchmark_count + 1  # +1 because row 0 is header
    
    # Color all benchmark rows (both non-proprietary and proprietary)
    for i in range(total_benchmark_count):
        for j in range(len(k_values)):
            cell = table[(i+1, j)]  # +1 because row 0 is header
            
            # Get the percentage value
            pct_text = table_data[i][j]
            if pct_text != "-" and "/" in pct_text:
                found, not_found = pct_text.split('/')
                pct_value = float(found) / float(not_found) * 100 if not_found != '0' else 0
                
                # Color code based on percentage
                if pct_value == 100:
                    cell.set_facecolor('#90EE90')  # Light green for 100%
                elif pct_value >= 75:
                    cell.set_facecolor('#FFFFE0')  # Light yellow for 75-99%
                elif pct_value >= 50:
                    cell.set_facecolor('#FFE4B5')  # Light orange for 50-74%
                elif pct_value > 0:
                    cell.set_facecolor('#FFB6C1')  # Light pink for 1-49%
                else:
                    cell.set_facecolor('#F0F0F0')  # Light gray for 0%
            else:
                cell.set_facecolor('#F0F0F0')  # Light gray for missing data
    
    # No double line separator - will use different colors for row labels instead
    
    # Style the summary rows (last two rows) with special formatting
    pruned_row_idx = summary_row_idx + 1
    for j in range(len(k_values)):
        # Style Overall % row
        cell = table[(summary_row_idx, j)]
        cell.set_facecolor('#D3D3D3')  # Light gray background for summary
        cell.set_text_props(weight='bold', color='black')
        
        # Style Pruned % row
        cell = table[(pruned_row_idx, j)]
        cell.set_facecolor('#D3D3D3')  # Light gray background for summary
        cell.set_text_props(weight='bold', color='black')
    
    # Style header row
    for j in range(len(k_values)):
        cell = table[(0, j)]
        cell.set_facecolor('#4472C4')
        cell.set_text_props(weight='bold', color='white')
    
    # Style row labels (benchmark names) with different colors for proprietary vs non-proprietary
    total_data_rows = len(table_data)
    for i in range(total_data_rows):
        cell = table[(i+1, -1)]
        if i >= total_data_rows - 2:  # Last two rows are summary rows (Overall % and Pruned %)
            cell.set_facecolor('#D3D3D3')
            cell.set_text_props(weight='bold', color='black')
        elif i < non_proprietary_count:  # Non-proprietary benchmarks
            cell.set_facecolor('#4472C4')  # Blue for non-proprietary
            cell.set_text_props(weight='bold', color='white')
        else:  # Proprietary benchmarks
            cell.set_facecolor('#8B4513')  # Brown for proprietary
            cell.set_text_props(weight='bold', color='white')
    
    # plt.title('Claude Sonnet 4 Performance Across All Benchmarks\n(# of Challenge Specifications Included for different top-k)', 
    #           fontsize=16, fontweight='bold', pad=20)
    
    # Add legend
    # legend_text = ("Color Legend:\n"
    #               "Green: 100% (Perfect)\n"
    #               "Yellow: 75-99% (Excellent)\n"
    #               "Orange: 50-74% (Good)\n"
    #               "Pink: 1-49% (Partial)\n"
    #               "Gray: 0% or Missing")
    
    # plt.figtext(0.02, 0.02, legend_text, fontsize=10, 
    #             bbox=dict(boxstyle="round,pad=0.5", facecolor="lightgray", alpha=0.8))
    
    plt.tight_layout()
    
    # Save in both formats
    png_filename = 'sonnet4_all_benchmarks_table.png'
    pdf_filename = 'sonnet4_all_benchmarks_table.pdf'
    plt.savefig(png_filename, dpi=300, bbox_inches='tight')
    plt.savefig(pdf_filename, bbox_inches='tight')
    print(f"Saved table: {png_filename}, {pdf_filename}")
    
    plt.close()
    
    # Print summary statistics with forward-fill indication
    print("\n" + "="*80)
    print("SONNET 4 BENCHMARK PERFORMANCE SUMMARY")
    print("="*80)
    
    for benchmark in valid_benchmarks:
        print(f"\n{benchmark.replace('_', ' ').title()}:")
        print("-" * 50)
        
        percentages = benchmark_data_dict[benchmark]
        
        for i, k in enumerate(k_values):
            k_str = str(k)
            percentage = percentages[i]
            
            if percentage is not None:
                # Check if this is actual data or forward-filled
                is_forward_filled = False
                if (k_str not in sonnet4_data or 
                    benchmark not in sonnet4_data[k_str] or 
                    sonnet4_data[k_str][benchmark] is None or
                    'error' in sonnet4_data[k_str][benchmark]):
                    is_forward_filled = True
                
                if is_forward_filled:
                    print(f"k={k:2d}: {percentage}% (forward-filled)")
                else:
                    benchmark_data = sonnet4_data[k_str][benchmark]
                    found_goals = benchmark_data.get('found_goals', 0)
                    total_goals = benchmark_data.get('total_goals', 0)
                    total_tokens = benchmark_data.get('total_tokens', 0)
                    print(f"k={k:2d}: {found_goals:2d}/{total_goals:2d} specs - {total_tokens:,} tokens")

# Call the new function
plot_sonnet4_by_benchmark()

# Show the plots
plt.show()
