import os
import re
from collections import Counter
import matplotlib.pyplot as plt
from typing import Dict, List, Tuple
from pathlib import Path

def extract_error_type_from_error_msg(error_msg: str) -> str:
    """
    Extract error type from the error message.
    Categorizes common P language parse errors.
    """
    # Common error patterns
    if "mismatched input" in error_msg:
        return "MismatchedInputError"
    elif "extraneous input" in error_msg:
        return "ExtraneousInputError"
    elif "token recognition error" in error_msg:
        return "TokenRecognitionError"
    elif "missing" in error_msg:
        return "MissingTokenError"
    elif "no viable alternative" in error_msg:
        return "NoViableAltError"
    else:
        return "OtherParseError"

def extract_error_type_from_stdout(content: str, file_path: str = "") -> str:
    """
    Extract error type from stdout content.
    Returns empty string if compilation succeeded (to be skipped).
    """
    # Skip if compilation succeeded
    if "Compilation succeeded." in content:
        return ""
    
    lines = content.split('\n')
    for i, line in enumerate(lines):
        # Look for type checking error first (since it comes after parsing)
        if "[Error:]" in line and i + 1 < len(lines):
            try:
                # Type errors are usually on the next line after [Error:]
                error_line = lines[i + 1]
                # Extract error message after the file location info
                match = re.search(r']\s*(.+)', error_line)
                if match:
                    error_msg = match.group(1)
                    if "Illegal main machine" in error_msg:
                        return "IllegalMainMachineError"
                    elif "does not exist" in error_msg:
                        return "UndefinedReferenceError"
                    elif "type" in error_msg.lower():
                        return "TypeMismatchError"
                    else:
                        return "OtherTypeError"
            except Exception as e:
                print(f"Warning: Error parsing type error message in {file_path}: {e}")
                return "TypeErrorExtractFailed"
        
        # Look for parse error
        if "parse error:" in line:
            try:
                # Split into parts: before parse error, line info, and actual error message
                parts = line.split("parse error:", 1)
                if len(parts) == 2:
                    error_part = parts[1].strip()
                    # Find where the actual error message starts (after line X:Y)
                    match = re.search(r'line \d+:\d+\s+(.+)', error_part)
                    if match:
                        error_msg = match.group(1)
                        return extract_error_type_from_error_msg(error_msg)
            except Exception as e:
                print(f"Warning: Error parsing error message in {file_path}: {e}")
                return "ParseErrorExtractFailed"
    
    # If we get here, it's an unknown error type
    if file_path:
        print(f"Warning: Unknown error type found in {file_path}")
    return "UnknownError"

def find_stdout_files(base_dir: str) -> List[Tuple[str, str]]:
    """
    Find all stdout.txt files in the directory structure.
    Returns list of tuples (test_name, full_path)
    """
    stdout_files = []
    for root, _, files in os.walk(base_dir):
        if 'stdout.txt' in files:
            # Extract test name from path (e.g., "1_basicMachineStructure")
            path = Path(root)
            # Look for test directory by checking if parent directory is a trial directory
            current = path
            test_name = "unknown"
            while current.name and str(current) != base_dir:
                parent = current.parent
                if parent.name.startswith("trial_"):
                    test_name = current.name
                    break
                current = parent
            
            full_path = os.path.join(root, 'stdout.txt')
            print(f"Found stdout file: {full_path} (test: {test_name})")
            stdout_files.append((test_name, full_path))
    
    print(f"\nTotal stdout files found: {len(stdout_files)}")
    return stdout_files

def read_and_extract_errors(files: List[Tuple[str, str]]) -> Dict[str, List[str]]:
    """
    Read stdout files and extract errors, organized by test name
    """
    errors_by_test = {}
    for test_name, file_path in files:
        try:
            with open(file_path, 'r') as f:
                content = f.read()
                error_type = extract_error_type_from_stdout(content, file_path)
                
                # Skip if no error (compilation succeeded)
                if not error_type:
                    continue
                
                if test_name not in errors_by_test:
                    errors_by_test[test_name] = []
                errors_by_test[test_name].append(error_type)
        except Exception as e:
            print(f"Error reading {file_path}: {e}")
    return errors_by_test

def plot_error_frequency(errors: List[str], title: str, output_path: str):
    """
    Create and save a frequency plot of errors
    """
    if not errors:
        print(f"No errors to plot for: {title}")
        return
        
    counter = Counter(errors)
    
    plt.figure(figsize=(10, 6))
    plt.bar(counter.keys(), counter.values())
    plt.title(title)
    plt.xlabel('Error Type')
    plt.ylabel('Frequency')
    plt.xticks(rotation=45)
    plt.tight_layout()
    plt.savefig(output_path)
    plt.close()
    print(f"Generated plot: {output_path}")

def analyze_errors(results_dir: str):
    """
    Main function to analyze errors in the results directory
    """
    print(f"\nAnalyzing errors in: {results_dir}")
    
    # Clean up any existing error frequency plots
    for f in os.listdir(results_dir):
        if f.startswith('error_frequency_') and f.endswith('.png'):
            try:
                os.remove(os.path.join(results_dir, f))
                print(f"Cleaned up old graph: {f}")
            except Exception as e:
                print(f"Warning: Could not remove old graph {f}: {e}")
    
    # Find all stdout files
    stdout_files = find_stdout_files(results_dir)
    
    if not stdout_files:
        print("No stdout files found!")
        return
        
    # Extract errors by test
    print("\nExtracting errors from files...")
    errors_by_test = read_and_extract_errors(stdout_files)
    
    # Create all-errors list for overall frequency
    all_errors = []
    for errors in errors_by_test.values():
        all_errors.extend(errors)
    
    # Generate overall frequency plot
    plot_error_frequency(
        all_errors,
        'Error Frequency - All Tests',
        os.path.join(results_dir, 'error_frequency_all.png')
    )
    
    # Generate per-test frequency plots
    for test_name, errors in errors_by_test.items():
        plot_error_frequency(
            errors,
            f'Error Frequency - {test_name}',
            os.path.join(results_dir, f'error_frequency_{test_name}.png')
        )

if __name__ == "__main__":
    import sys
    if len(sys.argv) != 2:
        print("Usage: python analyze-errors.py <results_directory>")
        sys.exit(1)
    
    results_dir = sys.argv[1]
    if not os.path.isdir(results_dir):
        print(f"Error: {results_dir} is not a valid directory")
        sys.exit(1)
    
    analyze_errors(results_dir)
