import json
import matplotlib.pyplot as plt
import os
import numpy as np

# Set style for better aesthetics
plt.style.use('bmh')  # Using a built-in style that provides good aesthetics

def create_compile_vs_semantic_visualization(avg_pass_rates, args, parent_dir_name, avg_p_at_k, outname="p_at_k-compile-v-semantic.png"):
    # Check if we're using the nested format
    is_nested_format = any(isinstance(v, dict) for v in avg_pass_rates.values())
    if not is_nested_format:
        return  # Skip if using legacy format
    
    # Sort items by test name
    sorted_items = sorted(avg_pass_rates.items())
    test_names = [item[0] for item in sorted_items]
    
    # Initialize arrays for compile and semantic values
    compile_values = []
    semantic_values = []
    
    # Process each test
    for test_name, test_data in sorted_items:
        # Get compile value if it exists
        compile_values.append(test_data.get('compile', 0))
        
        # Get average of all tc* values
        tc_values = [v for k, v in test_data.items() if k.startswith('tc')]
        semantic_values.append(sum(tc_values) / len(tc_values) if tc_values else 0)
    
    # Prepare data for grouped bars
    x = np.arange(len(test_names))
    width = 0.25  # Reduced width for more compact bars
    
    # Create bar chart with improved aesthetics
    plt.figure(figsize=(max(8, len(test_names) * 1.2), 6), facecolor='white')
    
    # Create bars for compile and semantic values with gap between groups
    gap = 0.05  # Small gap between bars in a group
    colors = ['#3498db', '#2ecc71']  # Blue and green colors
    bars1 = plt.bar(x - width/2 - gap/2, compile_values, width, label='Compile',
                   color=colors[0], edgecolor='white', linewidth=1.5, alpha=0.8)
    bars2 = plt.bar(x + width/2 + gap/2, semantic_values, width, label='Correctness',
                   color=colors[1], edgecolor='white', linewidth=1.5, alpha=0.8)
    
    # Add grid for better readability
    plt.grid(True, axis='y', alpha=0.2, linestyle='--')
    
    # Customize the plot
    plt.ylim(0, 1.1)
    
    # Add value labels on top of each bar
    def add_value_labels(bars):
        for bar in bars:
            height = bar.get_height()
            if height > 0:
                plt.text(bar.get_x() + bar.get_width()/2., height,
                        f'{height:.2f}',
                        ha='center', va='bottom', fontsize=8)
    
    add_value_labels(bars1)
    add_value_labels(bars2)
    
    # Set x-axis labels
    plt.xticks(x, test_names, rotation=45, ha='right')
    
    # Set title using the specified format
    plt.title(f"ID: {parent_dir_name} pass@{args['k']}(n={args['n']},t={args['t']},trials={args['trials']}) = {avg_p_at_k:.2f} (avg)",
              pad=20, fontsize=12, fontweight='bold')
    
    plt.xlabel('Test Cases')
    plt.ylabel('Pass Rate')
    plt.legend()
    
    # Adjust layout to prevent label cutoff
    plt.tight_layout()
    
    # Save the plot
    output_path = os.path.join(os.path.dirname(outname), 'p_at_k-compile-v-semantic.png')
    plt.savefig(output_path)
    plt.close()

def visualize_json_results(json_path, outname="p_at_k_.png"):
    # Read and parse JSON file
    with open(json_path, 'r') as f:
        data = json.load(f)
    
    # Extract data
    args = data['args']
    results = data['results']
    
    # Get metrics
    avg_pass_rates = results['avg_pass_rates']
    avg_p_at_k = results['avg_p_at_k']
    
    # Detect if we're using the new nested format or legacy format
    is_nested_format = any(isinstance(v, dict) for v in avg_pass_rates.values())
    
    # Sort items by test name
    sorted_items = sorted(avg_pass_rates.items())
    test_names = [item[0] for item in sorted_items]
    
    if is_nested_format:
        # Get all unique subtest types for nested format
        subtest_types = set()
        for test_data in avg_pass_rates.values():
            subtest_types.update(test_data.keys())
        subtest_types = sorted(list(subtest_types))
    else:
        # For legacy format, create a single "pass_rate" subtest type
        subtest_types = ["pass_rate"]
    
    # Prepare data for grouped bars
    x = np.arange(len(test_names))
    width = 0.8 / len(subtest_types)  # Width of each bar, adjusted for number of subtests
    
    # Create figure with more height to accommodate labels
    plt.figure(figsize=(12, 8), dpi=100, facecolor='white')
    
    # Define color palette
    colors = ['#3498db', '#2ecc71', '#e74c3c', '#f1c40f', '#9b59b6']
    
    # Create bars for each subtest type
    bars = []
    for i, subtest in enumerate(subtest_types):
        subtest_values = []
        for test in test_names:
            if is_nested_format:
                # For nested format, get the subtest value
                subtest_values.append(avg_pass_rates[test].get(subtest, 0))
            else:
                # For legacy format, use the direct value
                subtest_values.append(avg_pass_rates[test])
        
        # Create offset bars for each subtest with gap
        width_with_gap = width * 0.7  # Reduce width more to create larger gap
        bar = plt.bar(x + i * width, subtest_values, width_with_gap,
                     label=subtest, color=colors[i % len(colors)],
                     edgecolor='white', linewidth=1, alpha=0.8)  # Added slight transparency
        bars.append(bar)
    
    # Customize the plot
    plt.xlabel('Test Cases')
    plt.ylabel('Pass Rate')
    plt.ylim(0, 1.1)
    
    # Set x-axis labels at the center of grouped bars
    plt.xticks(x + width * (len(subtest_types) - 1) / 2, test_names, 
               rotation=45, ha='right')
    
    # Add legend for subtest types with better positioning
    plt.legend(bbox_to_anchor=(1.02, 1), loc='upper left', borderaxespad=0)
    
    # Add value labels on top of each bar
    for bar_group in bars:
        for bar in bar_group:
            height = bar.get_height()
            if height > 0:  # Only show labels for non-zero values
                plt.text(bar.get_x() + bar.get_width()/2., height,
                        f'{height:.2f}',
                        ha='center', va='bottom', fontsize=8)
    
    # Get parent directory name from path
    parent_dir_name = os.path.basename(os.path.dirname(json_path))
    
    # Set title using the specified format with better font
    plt.title(f"ID: {parent_dir_name}\npass@{args['k']}(n={args['n']},t={args['t']},trials={args['trials']}) = {avg_p_at_k:.2f} (avg)",
              pad=20, fontsize=12, fontweight='bold')
    
    # Add grid for better readability
    plt.grid(True, axis='y', alpha=0.2, linestyle='--')
    
    # Set background color
    plt.gca().set_facecolor('white')
    
    # Adjust layout to prevent label cutoff and accommodate legend
    plt.tight_layout()
    
    # Save the plot
    output_path = os.path.join(os.path.dirname(json_path), outname)
    plt.savefig(output_path)
    plt.close()
    
    # Create the compile vs semantic visualization if using nested format
    if any(isinstance(v, dict) for v in avg_pass_rates.values()):
        create_compile_vs_semantic_visualization(
            avg_pass_rates, 
            args, 
            parent_dir_name, 
            avg_p_at_k, 
            output_path
        )
    
    return output_path

if __name__ == "__main__":
    import sys
    if len(sys.argv) != 2:
        print("Usage: python visualize_results.py <path_to_json>")
        sys.exit(1)
    
    json_path = sys.argv[1]
    output_path = visualize_json_results(json_path)
    print(f"Visualization saved to: {output_path}")
