import os
from pathlib import Path
import re
from collections import defaultdict
import matplotlib.pyplot as plt
import numpy as np
import argparse
import json

# Set style for better looking plots
plt.style.use('bmh')  # Using a built-in style that's modern and clean

def categorize_error(log):
    """Categorize an error log based on its content."""
    log_lower = log.lower()
    if "deadlock detected" in log_lower:
        return "Deadlock"
    elif "received event" in log_lower and "cannot be handled" in log_lower:
        return "Unhandled Event"
    elif "in hot state" in log_lower and "at the end of program" in log_lower:
        return "Liveness: Ended in hot state"
    elif "assertion failed" in log_lower:
        return "Failed Assertion"
    elif "exception" in log_lower:
        return "Exception"
    else:
        return log[:41] + ('...' if len(log) > 41 else '')

def analyze_test_dir(test_dir):
    """Analyze a single test directory for errors."""
    error_data = {}
    std_streams = os.path.join(test_dir, "std_streams")
    
    if not os.path.exists(std_streams):
        return []
        
    subtests = os.listdir(std_streams)
    # Iterate through sub-test directories
    for subtest in subtests:
        subtest_path = os.path.join(std_streams, subtest)
        
        # Skip the compile directory
        if subtest == "compile" or not os.path.isdir(subtest_path):
            continue
            
        stdout_path = os.path.join(subtest_path, "stdout.txt")
        trace_path = os.path.join(subtest_path, "trace.txt")
        
        if not os.path.exists(stdout_path) or not os.path.exists(trace_path):
            continue
            
        # Check stdout.txt for bug count
        with open(stdout_path, 'r') as f:
            stdout_content = f.read()
            bug_match = re.search(r'Found (\d+) bug', stdout_content)
            if not bug_match or int(bug_match.group(1)) == 0:
                continue
                
            # If bugs found, analyze trace.txt
            with open(trace_path, 'r') as f:
                trace_content = f.read()
                error_logs = re.findall(r'<ErrorLog> (.*?)$', trace_content, re.MULTILINE)
                
                if len(error_logs) > 1:
                    print(f"Warning: Multiple ErrorLog lines found in {trace_path}")
                elif len(error_logs) == 0 and int(bug_match.group(1)) > 0:
                    print(f"Warning: No ErrorLog line found despite bugs reported in {stdout_path}")
                    continue
                
                # Store error category for this subtest
                error_data[subtest] = categorize_error(error_logs[0])
    
    return error_data

def main():
    parser = argparse.ArgumentParser(description='Analyze checker errors in test directories')
    parser.add_argument('dir_path', help='Path to the directory containing trial_* directories')
    args = parser.parse_args()
    
    base_dir = Path(args.dir_path)
    if not base_dir.exists():
        print(f"Error: Directory {base_dir} does not exist")
        return
        
    # Structure to store raw data
    raw_data = {}
    all_errors = []
    
    # Iterate through trial directories
    for trial_dir in base_dir.glob('trial_*'):
        if not trial_dir.is_dir():
            continue
            
        trial_name = trial_dir.name
        raw_data[trial_name] = {}
            
        # Iterate through test directories in each trial
        for test_dir in trial_dir.iterdir():
            if not test_dir.is_dir():
                continue
                
            test_name = test_dir.name
            errors = analyze_test_dir(str(test_dir))
            
            if errors:  # Only add to raw_data if there are errors
                raw_data[trial_name][test_name] = errors
                all_errors.extend(errors.values())
    
    if not all_errors:
        print("No errors found in any of the test directories")
        return
        
    # Save raw data to JSON file
    raw_data_path = os.path.join(str(base_dir), 'raw_data.json')
    with open(raw_data_path, 'w') as f:
        json.dump(raw_data, f, indent=2)
    print(f"Raw data saved as {raw_data_path}")
        
    # Count error frequencies
    error_counts = defaultdict(int)
    for error in all_errors:
        error_counts[error] += 1
    
    # Define fixed colors for known categories
    category_colors = {
        "Deadlock": "#FF9999",  # Light red
        "Unhandled Event": "#99FF99",  # Light green
        "Liveness: Ended in hot state": "#9999FF",  # Light blue
        "Failed Assertion": "#FFFF99",  # Light yellow
        "Exception": "#FF99FF"  # Light purple
    }
    
    # Create figure and axis objects
    fig, ax = plt.subplots(figsize=(15, 10))
    
    # Count how many uncategorized errors we have
    uncategorized_count = sum(1 for category in error_counts.keys() if category not in category_colors)
    
    # Assign colors to bars (fixed colors for known categories, palette colors for others)
    bar_colors = []
    other_colors = plt.cm.Set3(np.linspace(0, 1, max(1, uncategorized_count)))  # Ensure at least 1 color
    other_color_idx = 0
    
    for category in error_counts.keys():
        if category in category_colors:
            bar_colors.append(category_colors[category])
        else:
            bar_colors.append(other_colors[other_color_idx % len(other_colors)])  # Use modulo to cycle through colors
            other_color_idx += 1
    
    # Create bar plot
    bars = ax.bar(range(len(error_counts)), list(error_counts.values()), 
                 color=bar_colors, edgecolor='gray', linewidth=1, alpha=0.8)
    
    # Customize the plot
    ax.set_xticks(range(len(error_counts)))
    ax.set_xticklabels(list(error_counts.keys()), rotation=45, ha='right')
    ax.set_title('Frequency of Checker Errors', pad=20, fontsize=16, fontweight='bold', color='#2f2f2f')
    ax.set_xlabel('Error Type', labelpad=10, fontsize=12, color='#2f2f2f')
    ax.set_ylabel('Frequency', labelpad=10, fontsize=12, color='#2f2f2f')
    
    # Style the ticks
    ax.tick_params(axis='both', colors='#2f2f2f')
    
    # Add value labels on top of bars with improved style
    for bar in bars:
        height = bar.get_height()
        ax.text(bar.get_x() + bar.get_width()/2., height,
                f'{int(height)}',
                ha='center', va='bottom',
                fontsize=10, fontweight='bold', color='#2f2f2f')
    
    # Add grid for better readability
    ax.yaxis.grid(True, linestyle='--', alpha=0.7)
    ax.set_axisbelow(True)  # Put grid behind bars
    
    # Adjust layout
    plt.subplots_adjust(bottom=0.4)  # Increased bottom margin for labels
    
    # Remove top and right spines
    ax.spines['top'].set_visible(False)
    ax.spines['right'].set_visible(False)
    
    # Save the plot
    output_path = os.path.join(str(base_dir), 'error_frequency.png')
    plt.savefig(output_path)
    print(f"Plot saved as {output_path}")

if __name__ == "__main__":
    main()
