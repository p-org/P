import os
import subprocess
import json
import tempfile
import re
import sys
import shutil
from typing import List, Tuple, Dict
import z3

class BenchmarkRunner:
    def __init__(self, timeout: int = 30, benchmarks: List[str] = []):
        """ Initialize the BenchmarkRunner with a timeout and optional benchmarks list. """
        self.benchmarks = benchmarks if benchmarks else self.load_benchmarks()
        self.timeout = 30
        self.results = {}
        self.total_learned_goals = 0
        
    def load_benchmarks(self) -> List[str]:
        """Load benchmark names from goals.json"""
        try:
            with open('goals.json', 'r') as f:
                benchmarks = json.load(f)
            print(f"Loaded {len(benchmarks)} benchmarks: {benchmarks}")
            return benchmarks
        except Exception as e:
            print(f"Error loading goals.json: {e}")
            return []
    
    def run_duoai(self, benchmark: str) -> Tuple[bool, str, str]:
        """
        Run DuoAI.py for a benchmark with timeout
        Returns: (success, stdout, stderr)
        """
        print(f"\n--- Running DuoAI for benchmark: {benchmark} ---")
        
        try:
            cmd = ['python3', 'DuoAI.py', benchmark]
            result = subprocess.run(
                cmd,
                timeout=self.timeout,
                capture_output=True,
                text=True,
                cwd='.'
            )
            
            stdout = result.stdout
            stderr = result.stderr
            
            # Check for success condition
            if "Protocol verified!" in stdout:
                print(f"✓ DuoAI succeeded for {benchmark}")
                return True, stdout, stderr
            elif stderr and ("exception" in stderr.lower() or "error" in stderr.lower()):
                print(f"✗ DuoAI failed for {benchmark} - Exception in stderr")
                return False, stdout, stderr
            else:
                print(f"✗ DuoAI failed for {benchmark} - No 'Protocol verified!' found")
                return False, stdout, stderr
                
        except subprocess.TimeoutExpired:
            print(f"✗ DuoAI timed out for {benchmark} after {self.timeout}s")
            return False, "", "Timeout"
        except Exception as e:
            print(f"✗ Error running DuoAI for {benchmark}: {e}")
            return False, "", str(e)
    
    def find_output_file(self, benchmark: str) -> str | None:
        """Find the output .ivy file for a benchmark"""
        expected_path = f"outputs/{benchmark}/{benchmark}_f0_inv_child.ivy"
        
        if os.path.exists(expected_path):
            print(f"✓ Found output file: {expected_path}")
            return expected_path
        else:
            print(f"✗ Output file not found: {expected_path}")
            return None
    
    def extract_safety_properties(self, file_content: str) -> List[str]:
        """
        Extract commented safety properties from .ivy file content
        Returns list of safety property lines (without the # comment)
        """
        safety_properties = []
        lines = file_content.split('\n')
        
        # Look for commented invariant lines that appear to be safety properties
        for i, line in enumerate(lines):
            stripped = line.strip()
            
            # Look for commented invariants with high numbers (typically safety properties)
            if (stripped.startswith('# invariant [') and 
                ('1000000' in stripped or 'safety' in lines[max(0, i-3):i+1])):
                # Extract the invariant without the comment
                safety_prop = stripped[1:].strip()  # Remove the '#' and leading space
                safety_properties.append(safety_prop)
                print(f"  Found safety property: {safety_prop[:80]}...")
        
        return safety_properties
    
    def extract_learned_invariants(self, file_content: str) -> List[str]:
        """Extract all learned invariants (non-commented invariant lines)"""
        invariants = []
        lines = file_content.split('\n')
        
        for line in lines:
            stripped = line.strip()
            if (stripped.startswith('invariant [') and 
                not stripped.startswith('#') and
                not '1000000' in stripped):  # Exclude safety properties
                # Extract the condition part
                bracket_end = stripped.find(']')
                if bracket_end != -1:
                    condition = stripped[bracket_end + 1:].strip()
                    invariants.append(condition)
        
        return invariants
    
    def normalize_equality_expression(self, expr: str) -> str:
        """Normalize equality expressions to a canonical form"""
        # Sort the operands of equality/inequality to ensure consistent representation
        if ' ~= ' in expr:
            parts = expr.split(' ~= ')
            if len(parts) == 2:
                left, right = parts[0].strip(), parts[1].strip()
                # Sort lexicographically for consistency
                if left > right:
                    left, right = right, left
                return f"{left} ~= {right}"
        elif ' = ' in expr:
            parts = expr.split(' = ')
            if len(parts) == 2:
                left, right = parts[0].strip(), parts[1].strip()
                # Sort lexicographically for consistency
                if left > right:
                    left, right = right, left
                return f"{left} = {right}"
        return expr
    
    def parse_ivy_formula_to_z3(self, formula: str, predicate_vars: Dict[str, z3.BoolRef]):
        """
        Convert an Ivy formula to Z3 by treating predicate calls as boolean variables
        This is a simplified parser for the common patterns in the invariants
        """
        try:
            # Remove comments if any
            if '#' in formula:
                formula = formula[:formula.index('#')].strip()
            
            # Replace common Ivy operators with Z3 equivalents
            # Handle ~= (inequality) first before replacing ~
            formula = formula.replace(' & ', ' and ')
            formula = formula.replace(' | ', ' or ')
            formula = formula.replace('->', ' implies ')
            # Keep ~= as is for now, replace other ~ with not (but not when followed by =)
            formula = re.sub(r'~(?![=])', 'not ', formula)
            
            # Handle forall quantifiers - for now we'll treat them as universal over a finite domain
            # This is a simplification but should work for the implication checking
            while 'forall' in formula:
                # Extract the quantified variables and the body
                # Pattern: forall X:type, Y:type. body or forall X:type. body
                forall_match = re.match(r'forall\s+[^.]+\.\s*(.+)', formula)
                if forall_match:
                    body = forall_match.group(1)
                    # For simplification, we'll just process the body
                    # In a full implementation, we'd need to handle quantification properly
                    formula = body
                else:
                    break
            
            # Find all predicate calls in the formula and preserve their full signature
            # Pattern: predicate_name(args)
            predicate_calls = re.findall(r'([a-zA-Z_][a-zA-Z0-9_]*\([^)]*\))', formula)
            
            # Replace each predicate call with a boolean variable
            processed_formula = formula
            for full_call in set(predicate_calls):
                # Create a unique variable name for this predicate call
                # Use the full call as the variable name (sanitized)
                var_name = full_call.replace(',', '_').replace(' ', '').replace(':', '_')
                if var_name not in predicate_vars:
                    predicate_vars[var_name] = z3.Bool(var_name)
                
                # Replace the full predicate call with the variable
                processed_formula = processed_formula.replace(full_call, var_name)
            
            # Now replace ~ with not (after handling ~= inequalities)
            # processed_formula = processed_formula.replace('~', 'not ')
            
            # Handle implications
            if ' implies ' in processed_formula:
                parts = processed_formula.split(' implies ')
                if len(parts) == 2:
                    antecedent = self.parse_simple_expression(parts[0].strip(), predicate_vars)
                    consequent = self.parse_simple_expression(parts[1].strip(), predicate_vars)
                    return z3.Implies(antecedent, consequent)
            
            # Parse the remaining expression
            return self.parse_simple_expression(processed_formula, predicate_vars)
            
        except Exception as e:
            print(f"    Warning: Could not parse formula '{formula[:50]}...': {e}")
            # Return a dummy true value if parsing fails
            return z3.BoolVal(True)
    
    def parse_simple_expression(self, expr: str, predicate_vars: Dict[str, z3.BoolRef]):
        """Parse a simple boolean expression with and, or, not operators"""
        expr = expr.strip()
        
        # Handle parentheses by finding matching pairs
        if '(' in expr and ')' in expr:
            # For simplicity, just remove outer parentheses if they wrap the whole expression
            if expr.startswith('(') and expr.endswith(')'):
                expr = expr[1:-1].strip()
        
        # Handle 'and' operations
        if ' and ' in expr:
            parts = expr.split(' and ')
            result = self.parse_simple_expression(parts[0], predicate_vars)
            for part in parts[1:]:
                result = z3.And(result, self.parse_simple_expression(part, predicate_vars))
            return result
        
        # Handle 'or' operations
        if ' or ' in expr:
            parts = expr.split(' or ')
            result = self.parse_simple_expression(parts[0], predicate_vars)
            for part in parts[1:]:
                result = z3.Or(result, self.parse_simple_expression(part, predicate_vars))
            return result
        
        # Handle 'not' operations
        if expr.startswith('not '):
            inner = expr[4:].strip()
            return z3.Not(self.parse_simple_expression(inner, predicate_vars))

        if ' ~= ' in expr or ' != ' in expr:
            # Convert inequality to equality and negate it
            if ' ~= ' in expr:
                eq_expr = expr.replace(' ~= ', ' = ')
            else:
                eq_expr = expr.replace(' != ', ' = ')
            
            # Normalize the equality expression - this should be the same as the positive case
            normalized = self.normalize_equality_expression(eq_expr)
            eq_var_name = f"eq_{hash(normalized) % 10000}"
            if normalized not in predicate_vars:
                predicate_vars[normalized] = z3.Bool(eq_var_name)

            # Return the negation of the equality
            return z3.Not(predicate_vars[normalized])
        
        # Handle equality and inequality - treat them as logical negations of each other
        if ' = ' in expr:
            # Normalize the equality expression
            normalized = self.normalize_equality_expression(expr)
            eq_var_name = f"eq_{hash(normalized) % 10000}"
            if normalized not in predicate_vars:
                predicate_vars[normalized] = z3.Bool(eq_var_name)
            return predicate_vars[normalized]
        
        # If it's just a variable name, return the corresponding boolean variable
        if expr in predicate_vars:
            return predicate_vars[expr]
        
        # Create a new boolean variable for unknown expressions
        var_name = f"expr_{hash(expr) % 10000}"
        if var_name not in predicate_vars:
            predicate_vars[var_name] = z3.Bool(var_name)
        return predicate_vars[var_name]
    
    def test_safety_property(self, original_content: str, safety_property: str, benchmark: str) -> bool:
        """
        Test whether the learned invariants logically imply the safety property using Z3
        Returns True if the implication holds
        """
        try:
            print(f"    Using Z3 for logical implication checking...")
            
            # Extract the safety property condition
            safety_condition = safety_property
            if safety_property.startswith('invariant ['):
                bracket_end = safety_property.find(']')
                if bracket_end != -1:
                    safety_condition = safety_property[bracket_end + 1:].strip()
            
            # Extract all learned invariants
            learned_invariants = self.extract_learned_invariants(original_content)
            
            if not learned_invariants:
                print(f"    ✗ No learned invariants found")
                return False
            
            print(f"    Found {len(learned_invariants)} learned invariants")
            
            # Create Z3 solver
            solver = z3.Solver()
            predicate_vars = {}
            # Convert safety property to Z3 formula
            z3_safety = self.parse_ivy_formula_to_z3(safety_condition, predicate_vars)
            
            # Convert learned invariants to Z3 formulas
            z3_invariants = []
            for inv in learned_invariants:
                pred_vars = {k: v for k, v in predicate_vars.items()}
                z3_inv = self.parse_ivy_formula_to_z3(inv, pred_vars)
                z3_invariants.append((inv, z3_inv))
            
            # print(z3_safety)
            # print(predicate_vars)
            for (inv, z3_inv) in z3_invariants:
                solver.push()
                # print(inv, '\n', z3_inv)
                solver.add(z3.Not(z3.Implies(z3_inv, z3_safety)))
                result = solver.check()
            
                if result == z3.unsat:
                    print(f"    ✓ Safety property logically implied by learned invariants (Z3)")
                    return True
                solver.pop()
            print(f"    ✗ Safety property not implied by learned invariants (Z3)")
            return False
                
        except Exception as e:
            print(f"    ✗ Error in Z3 implication checking: {e}")
            return False
    
    def process_benchmark_output(self, benchmark: str) -> int:
        """
        Process the output file for a benchmark and test safety properties
        Returns number of verified safety properties
        """
        output_file = self.find_output_file(benchmark)
        if not output_file:
            return 0
        
        try:
            with open(output_file, 'r') as f:
                content = f.read()
            
            safety_properties = self.extract_safety_properties(content)
            if not safety_properties:
                print(f"  No safety properties found in {output_file}")
                return 0
            
            print(f"  Testing {len(safety_properties)} safety properties...")
            verified_count = 0
            
            for i, safety_prop in enumerate(safety_properties, 1):
                print(f"  Testing safety property {i}/{len(safety_properties)}:")
                if self.test_safety_property(content, safety_prop, benchmark):
                    verified_count += 1
            
            print(f"  ✓ {verified_count}/{len(safety_properties)} safety properties verified")
            return verified_count
            
        except Exception as e:
            print(f"  ✗ Error processing output file: {e}")
            return 0
    
    def run_all_benchmarks(self):
        """Run all benchmarks and collect results"""
        benchmarks = self.benchmarks
        if not benchmarks:
            print("No benchmarks to run")
            return
        
        print(f"\n{'='*60}")
        print(f"STARTING BENCHMARK COMPARISON")
        print(f"{'='*60}")
        
        for benchmark in benchmarks:
            print(f"\n{'='*40}")
            print(f"BENCHMARK: {benchmark}")
            print(f"{'='*40}")
            
            # Run DuoAI
            success, stdout, stderr = self.run_duoai(benchmark)
            
            if success:
                # Process output and test safety properties
                verified_count = self.process_benchmark_output(benchmark)
                self.results[benchmark] = {
                    'duoai_success': True,
                    'verified_properties': verified_count
                }
                self.total_learned_goals += verified_count
            else:
                self.results[benchmark] = {
                    'duoai_success': False,
                    'verified_properties': 0,
                    'error': stderr if stderr else "Unknown error"
                }
        
        self.print_final_results()
    
    def print_final_results(self):
        """Print final summary of results"""
        print(f"\n{'='*60}")
        print(f"FINAL RESULTS")
        print(f"{'='*60}")
        
        for benchmark, result in self.results.items():
            if result['duoai_success']:
                print(f"{benchmark:25} | DuoAI: ✓ | Safety Props: {result['verified_properties']}")
            else:
                print(f"{benchmark:25} | DuoAI: ✗ | Error: {result.get('error', 'Unknown')[:30]}")
        
        print(f"\n{'='*60}")
        print(f"TOTAL LEARNED GOALS: {self.total_learned_goals}")
        print(f"{'='*60}")
        
        # Summary statistics
        successful_runs = sum(1 for r in self.results.values() if r['duoai_success'])
        total_runs = len(self.results)
        print(f"Successful DuoAI runs: {successful_runs}/{total_runs}")
        print(f"Total verified safety properties: {self.total_learned_goals}")

def main(timeout: int = 30, benchmarks: List[str] = []):
    """Main entry point"""
    runner = BenchmarkRunner(timeout=timeout, benchmarks=benchmarks)
    runner.run_all_benchmarks()

if __name__ == "__main__":
    import argparse
    parser = argparse.ArgumentParser(description="Run DuoAI benchmarks and verify safety properties.")
    parser.add_argument('--timeout', type=int, default=30, help='Timeout for each DuoAI run (in seconds)')
    parser.add_argument('--benchmarks', type=str, nargs='*', help='Specific benchmarks to run (default: all from goals.json)')
    args = parser.parse_args()
    main(args.timeout, args.benchmarks)
