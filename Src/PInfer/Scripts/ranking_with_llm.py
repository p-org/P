import strands
from strands import tool
import os
import json
import math
import statistics
import concurrent.futures
import threading
import random
from typing import List, Dict, Tuple, Optional
from constants import benchmarks
from functools import lru_cache

# Default model configuration
DEFAULT_MODEL_ID = 'us.anthropic.claude-sonnet-4-20250514-v1:0'

# Default weights for compute_score tool
DEFAULT_WEIGHTS = {
    'quality': 0.8,
    'distinguishability': 0.2,
    'visibility': 0.0,
}

class AgentWrapper:
    """Wrapper for strands agent with token tracking and history management."""
    
    def __init__(self, agent: strands.Agent):
        self.agent = agent
        self.num_invocations = 0
        self.num_input_tokens = 0
        self.num_output_tokens = 0
        self.total_cycles = 0
        self.total_tokens = 0

    def __call__(self, prompt: str, **kwargs):
        result = self.agent(prompt, **kwargs)
        self.num_invocations += 1
        metrics = result.metrics.get_summary()
        self.num_input_tokens += metrics.get('accumulated_usage', {}).get('inputTokens', 0)
        self.num_output_tokens += metrics.get('accumulated_usage', {}).get('outputTokens', 0)
        self.total_tokens += metrics.get('accumulated_usage', {}).get('totalTokens', 0)
        self.total_cycles = metrics.get('total_cycles', 0)
        return result
    
    def clear_history(self):
        """Clear chat history for fresh iteration."""
        self.agent.messages = []

    def get_summary(self):
        """Get token usage summary."""
        return {
            'num_invocations': self.num_invocations,
            'num_input_tokens': self.num_input_tokens,
            'num_output_tokens': self.num_output_tokens,
            'total_cycles': self.total_cycles,
            'total_tokens': self.total_tokens
        }

def compute_score(generalization: float, criticality: float, 
                 distinguishability: float, visibility: float,
                 weights: Optional[Dict[str, float]] = None) -> float:
    """
    Computes overall score as weighted linear combination of metric scores.
    
    Args:
        generalization: Score 0.0-1.0 for specification generalizability
        criticality: Score 0.0-1.0 for violation severity  
        distinguishability: Score 0.0-1.0 for behavior differentiation
        visibility: Score 0.0-1.0 for violation visibility to users
        weights: Optional custom weights dict, defaults to DEFAULT_WEIGHTS
    
    Returns:
        Overall score 0.0-1.0
    """
    if weights is None:
        weights = DEFAULT_WEIGHTS
    
    # Validate inputs
    for score in [generalization, criticality, distinguishability, visibility]:
        if not 0.0 <= score <= 1.0:
            raise ValueError(f"Score {score} must be between 0.0 and 1.0")
    
    quality_score = (generalization * criticality) ** 0.5
    overall_score = quality_score * weights['quality'] + \
                    distinguishability * weights['distinguishability'] + \
                    visibility * weights['visibility']

    return round(overall_score, 4)

@lru_cache
def load_system_prompts():
    """Load and combine system prompts from the three required files."""
    with open('p_basics.txt', 'r') as f:
        p_basics = f.read()
    
    with open('p_infer_specs.txt', 'r') as f:
        p_infer_specs = f.read()
    
    with open('ranking_example.txt', 'r') as f:
        ranking_example = f.read()
    
    return p_basics, p_infer_specs, ranking_example

@lru_cache
def read_p_model(benchmark: str) -> str:
    """Read P model files from benchmark/PSrc directory."""
    p_model_content = "Here are the P model files:\n\n"
    psrc_dir = os.path.join(benchmark, 'PSrc')
    
    if not os.path.exists(psrc_dir):
        raise FileNotFoundError(f"PSrc directory not found for benchmark {benchmark}")
    
    for root, dirs, files in os.walk(psrc_dir):
        for file in files:
            if file.endswith('.p'):
                file_path = os.path.join(root, file)
                with open(file_path, 'r') as f:
                    content = f.read()
                p_model_content += f"<{file}>\n{content}\n\n"
    
    return p_model_content

def create_summarization_agent(model_id: str) -> AgentWrapper:
    """Create agent for summarizing P model events and flows."""
    p_basics, p_infer_specs, _ = load_system_prompts()
    
    system_prompt = f"""
{p_basics}

{p_infer_specs}

You are an expert in P programming language. Your task is to analyze P model files and provide a concise summary of:

1. **Events Definition**: List all events defined in the P model with their payload types
2. **Event Flows**: Describe the typical event flow patterns and interactions between state machines
3. **System Roles**: Identify the main roles/components and their responsibilities
4. **Protocol Workflow**: Explain the high-level protocol workflow

Provide a structured summary that will help another agent understand the system for specification ranking.
"""
    
    from strands.models import BedrockModel
    model = BedrockModel(model_id=model_id, region_name='us-east-1')
    agent = AgentWrapper(strands.Agent(
        system_prompt=system_prompt,
        model=model,
        callback_handler=None
    ))
    
    return agent

def create_ranking_agent(model_id: str, p_model_summary: str, k: int) -> AgentWrapper:
    """Create agent for ranking specifications with 4-metric scoring."""
    p_basics, p_infer_specs, ranking_example = load_system_prompts()
    
    system_prompt = f"""
{p_basics}

{p_infer_specs}

{ranking_example}

## P Model Summary
{p_model_summary}

## Overall Score

After analyzing a specification on all 4 metrics, the overall score for ranking specifications computed as
overall_score = sqrt(generalization_score * criticality_score) * {DEFAULT_WEIGHTS['quality']} + distinguishability_score * {DEFAULT_WEIGHTS['distinguishability']} + visibility_score * {DEFAULT_WEIGHTS['visibility']} 

## Your Task

You will be given a set of specifications to evaluate. For each specification:
1. Analyze it across all 4 metrics (Generalization, Criticality, Distinguishability, Visibility)
2. Assign scores 0.0-1.0 for each metric. To break tie, if a specification S1 is more important than S2, then S1 must have a higher overall score.
3. Compute the overall score using the formula provided above.
4. Output results for top {k} specifications with highest overall scores. You must follow the exact format specified in the ranking example. You must NOT output any other text, throught process, explanation or formatting.

Focus on specifications that enforce critical protocol properties over implementation details.
"""
    
    from strands.models import BedrockModel
    model = BedrockModel(model_id=model_id, region_name='us-east-1')
    agent = AgentWrapper(strands.Agent(
        system_prompt=system_prompt,
        model=model,
        callback_handler=None
    ))
    
    return agent

@lru_cache
def get_p_model_summary(benchmark: str, model_id: str) -> str:
    """Get P model summary using summarization agent."""
    print(f"Generating P model summary for {benchmark}...")
    if os.path.exists(os.path.join(benchmark, 'p_model_summary.txt')):
        print(f"P model summary already exists for {benchmark}, read from cache.")
        with open(os.path.join(benchmark, 'p_model_summary.txt'), 'r') as f:
            return f.read().strip()
    
    p_model_content = read_p_model(benchmark)
    summarization_agent = create_summarization_agent(model_id)
    
    prompt = f"""
Please analyze the following P model and provide a structured summary:

{p_model_content}

Provide the summary in a clear, organized format that will help with specification ranking.
"""
    
    response = summarization_agent(prompt)
    contents = response.message.get("content", [])
    
    if contents:
        summary = "\n".join(item.get("text", "") for item in contents if isinstance(item, dict) and "text" in item)
        result = summary.strip()
        with open(os.path.join(benchmark, 'p_model_summary.txt'), 'w') as f:
            f.write(result)
        print(f"P model summary generated and saved for {benchmark}")
        return result
    else:
        return "No summary generated"

@lru_cache
def read_specifications(benchmark: str) -> List[str]:
    """Read learned specifications from pruned_invariants.txt."""
    spec_file = os.path.join(benchmark, 'pruned_invariants.txt')
    
    if not os.path.exists(spec_file):
        raise FileNotFoundError(f"Specifications file not found: {spec_file}")
    
    with open(spec_file, 'r') as f:
        specs = [line.strip() for line in f.readlines() if line.strip()]
    
    return specs

@lru_cache
def read_confirmed_specs(benchmark: str) -> List[str]:
    """Read target specifications from confirmed_specs.txt."""
    confirmed_file = os.path.join(benchmark, 'confirmed_specs.txt')
    
    if not os.path.exists(confirmed_file):
        print(f"Warning: No confirmed specs file found for {benchmark}")
        return []
    
    with open(confirmed_file, 'r') as f:
        confirmed_specs = [line.strip() for line in f.readlines() if line.strip()]
    
    return confirmed_specs

def parse_ranking_response(response_text: str) -> List[Dict]:
    """Parse agent response to extract specification rankings."""
    rankings = []
    lines = response_text.strip().split('\n')
    
    current_spec: Dict = {}
    for line in lines:
        line = line.strip()
        if not line:
            continue
            
        if line.startswith('Specification: '):
            if current_spec:  # Save previous spec
                rankings.append(current_spec)
            current_spec = {'specification': line[len('Specification: '):].strip()}
        elif line.startswith('Generalization_score: '):
            current_spec['generalization_score'] = float(line.split(': ')[1])
        elif line.startswith('Criticality_score: '):
            current_spec['criticality_score'] = float(line.split(': ')[1])
        elif line.startswith('Distinguishability_score: '):
            current_spec['distinguishability_score'] = float(line.split(': ')[1])
        elif line.startswith('Visibility_score: '):
            current_spec['visibility_score'] = float(line.split(': ')[1])
            # Calculate overall score when we have all metrics
            if all(key in current_spec for key in ['generalization_score', 'criticality_score', 
                                                  'distinguishability_score', 'visibility_score']):
                current_spec['overall_score'] = compute_score(
                    float(current_spec['generalization_score']),
                    float(current_spec['criticality_score']),
                    float(current_spec['distinguishability_score']),
                    float(current_spec['visibility_score'])
                )
    
    if current_spec:  # Don't forget the last spec
        rankings.append(current_spec)
    
    return rankings

def create_overlapping_blocks(specs: List[str], max_block_size: int = 60, 
                             min_block_size: int = 10, overlap_ratio: float = 0.2) -> List[List[str]]:
    """
    Create overlapping blocks with shuffling for cross-block comparison.
    
    Args:
        specs: List of specifications to divide
        max_block_size: Maximum specs per block (60)
        min_block_size: Minimum specs per block (10) 
        overlap_ratio: Fraction of specs to overlap between blocks (20%)
    
    Returns:
        List of specification blocks with overlap
    """
    if len(specs) <= max_block_size:
        return [specs]  # No need to split
    
    # Shuffle specifications for randomization
    shuffled_specs = specs.copy()
    random.shuffle(shuffled_specs)
    
    # Calculate optimal block size
    num_specs = len(shuffled_specs)
    # Aim for blocks around 50 specs, but ensure they're between min and max
    target_block_size = min(max_block_size, max(min_block_size, num_specs // max(1, num_specs // 50)))
    
    blocks = []
    overlap_size = int(target_block_size * overlap_ratio)
    
    start = 0
    while start < num_specs:
        # Calculate end position for this block
        end = min(start + target_block_size, num_specs)
        
        # Ensure last block isn't too small
        if num_specs - end < min_block_size and end < num_specs:
            end = num_specs  # Include remaining specs in current block
        
        block = shuffled_specs[start:end]
        blocks.append(block)
        
        # Move start position with overlap
        start = end - overlap_size
        
        # Break if we've covered all specs
        if end >= num_specs:
            break
    
    print(f"Created {len(blocks)} blocks from {num_specs} specs:")
    for i, block in enumerate(blocks):
        print(f"  Block {i+1}: {len(block)} specs")
    
    return blocks

def rank_specifications_with_blocks(model_id: str, p_model_summary: str, specs: List[str], 
                                   keep_count: int, block_size: int) -> List[Dict]:
    """
    Rank large specification sets using overlapping blocks.
    
    Args:
        model_id: Model ID for creating agents
        p_model_summary: P model summary for agents
        specs: List of specifications to rank
        keep_count: Number of specifications to keep
    
    Returns:
        List of ranked specifications
    """
    print(f"Using block-based ranking for {len(specs)} specs (keep_count={keep_count})")
    
    # Create overlapping blocks
    blocks = create_overlapping_blocks(specs, max_block_size=block_size, overlap_ratio=0.6)
    
    # Calculate keep percentage
    keep_percentage = keep_count / len(specs)
    
    all_selected_specs = []
    block_agents_used = []
    
    # Process each block sequentially
    for i, block in enumerate(blocks):
        print(f"\nProcessing block {i+1}/{len(blocks)} ({len(block)} specs)")
        
        # Create fresh agent for this block
        block_agent = create_ranking_agent(model_id, p_model_summary, len(block))
        block_agents_used.append(block_agent)
        
        # Rank all specs in the block
        block_rankings = rank_specifications_iteration(block_agent, block)
        
        if not block_rankings:
            print(f"Warning: No rankings returned for block {i+1}")
            continue
        
        # Sort by overall score (descending)
        block_rankings.sort(key=lambda x: x.get('overall_score', 0), reverse=True)
        
        # Calculate how many to keep from this block
        block_keep_count = max(1, int(len(block) * keep_percentage))
        block_keep_count = min(block_keep_count, len(block_rankings))
        
        # Take top specs from this block
        selected_from_block = block_rankings[:block_keep_count]
        all_selected_specs.extend(selected_from_block)
        
        print(f"  Selected {len(selected_from_block)} specs from block {i+1}")
    
    # Remove duplicates (from overlaps) - keep the one with higher score
    unique_specs = {}
    for spec_data in all_selected_specs:
        spec_text = spec_data['specification']
        if spec_text not in unique_specs or spec_data['overall_score'] > unique_specs[spec_text]['overall_score']:
            unique_specs[spec_text] = spec_data
    
    combined_specs = list(unique_specs.values())
    print(f"After deduplication: {len(combined_specs)} unique specs")
    
    # If we have more than keep_count, do final ranking
    if len(combined_specs) > keep_count:
        print(f"Final ranking of {len(combined_specs)} specs to select top {keep_count}")
        combined_specs.sort(key=lambda x: x.get('overall_score', 0), reverse=True)
        return combined_specs[:keep_count]
    else:
        # Sort by overall score and return all
        combined_specs.sort(key=lambda x: x.get('overall_score', 0), reverse=True)
        return combined_specs

def rank_specifications_iteration(agent: AgentWrapper, specs: List[str]) -> List[Dict]:
    """Rank a set of specifications using the ranking agent."""
    agent.clear_history()  # Fresh start for each iteration
    
    specs_text = "\n".join(f"{i+1}. {spec}" for i, spec in enumerate(specs))
    
    prompt = f"""
Please evaluate and rank the following specifications:

{specs_text}

For each specification, provide scores and compute the overall score as described in your instructions.
Output results in the exact format specified.
"""
    
    response = agent(prompt)
    contents = response.message.get("content", [])
    
    if contents:
        response_text = "\n".join(item.get("text", "") for item in contents if isinstance(item, dict) and "text" in item)
        rankings = parse_ranking_response(response_text)
        return rankings
    else:
        return []

def iterative_ranking(benchmark: str, k: int, x: int, block_size: int, model_id: str = DEFAULT_MODEL_ID, 
                     weights: Optional[Dict[str, float]] = None) -> Dict:
    """
    Main iterative ranking function.
    
    Args:
        benchmark: Benchmark name
        k: Target number of top specifications
        x: Percentage of specifications to remove each iteration (1-99)
        model_id: Model ID for strands agents
        weights: Custom weights for compute_score (optional)
        
    Returns:
        Dictionary with ranking results and statistics
    """
    print(f"Starting iterative ranking for {benchmark} (k={k}, x={x}%)")
    
    # Validate parameters
    if not 1 <= x <= 99:
        raise ValueError("x must be between 1 and 99")
    
    # Read specifications
    specs = read_specifications(benchmark)
    confirmed_specs = read_confirmed_specs(benchmark)
    confirmed_set = set(confirmed_specs)
    
    print(f"Loaded {len(specs)} specifications, {len(confirmed_specs)} confirmed specs")
    
    if len(specs) <= k:
        print(f"Number of specs ({len(specs)}) already <= k ({k}), no filtering needed")
        # Still need to rank them
        p_model_summary = get_p_model_summary(benchmark, model_id)
        ranking_agent = create_ranking_agent(model_id, p_model_summary, k)
        final_rankings = rank_specifications_iteration(ranking_agent, specs)
        
        # Calculate statistics
        stats = calculate_final_statistics([ranking_agent], [final_rankings], confirmed_set)
        stats['iterations'] = 1
        stats['final_rankings'] = final_rankings
        stats['final_spec_count'] = len(specs)
        return stats
    
    # Get P model summary
    p_model_summary = get_p_model_summary(benchmark, model_id)
    
    # Initialize tracking
    iteration = 1
    iteration_stats = []
    agents_used = []
    all_rankings = []
    
    current_specs = specs.copy()
    
    # Iterative filtering
    while len(current_specs) > k:
        print(f"\nIteration {iteration}: {len(current_specs)} specifications")

        # Calculate how many to remove
        remove_count = max(1, int(len(current_specs) * x / 100))
        keep_count = max(k, len(current_specs) - remove_count)
        
        # Create fresh ranking agent for this iteration
        ranking_agent = create_ranking_agent(model_id, p_model_summary, keep_count)
        agents_used.append(ranking_agent)
        
        # Rank current specifications - use block-based ranking if keep_count > 60
        if keep_count > block_size:
            print(f"Using block-based ranking (keep_count={keep_count} > 60)")
            rankings = rank_specifications_with_blocks(model_id, p_model_summary, current_specs, keep_count, block_size)
        else:
            rankings = rank_specifications_iteration(ranking_agent, current_specs)
        
        all_rankings.append(rankings)
        
        if not rankings:
            print(f"Warning: No rankings returned for iteration {iteration}")
            break
        
        # Check if LLM returned expected number of rankings (only for non-block-based ranking)
        if keep_count <= block_size and len(rankings) < keep_count:
            error_msg = (
                f"Iteration {iteration}: LLM returned {len(rankings)} rankings, "
                f"but expected at least {keep_count} (keep_count). "
                f"This indicates the LLM did not follow instructions properly."
            )
            print(f"Error: {error_msg}")
            
            # Return error result instead of crashing
            return {
                'error': 'llm_ranking_count_mismatch',
                'error_message': error_msg,
                'benchmark': benchmark,
                'k': k,
                'x': x,
                'iteration': iteration,
                'expected_rankings': keep_count,
                'actual_rankings': len(rankings),
                'rankings_received': rankings,
                'agent_summary': ranking_agent.get_summary() if 'ranking_agent' in locals() else {}
            }

        # Sort by overall score (descending) and limit to keep_count
        rankings.sort(key=lambda x: x.get('overall_score', 0), reverse=True)
        if len(rankings) > keep_count:
            rankings = rankings[:keep_count]
        
        # Keep top specifications
        kept_rankings = rankings
        current_specs = [r['specification'] for r in kept_rankings]
        
        # Track iteration statistics
        iter_stats = {
            'iteration': iteration,
            'specs_count': len(current_specs) + remove_count,
            'removed_count': remove_count,
            'kept_count': keep_count,
            'avg_score': statistics.mean([r.get('overall_score', 0) for r in rankings]),
            'median_score': statistics.median([r.get('overall_score', 0) for r in rankings]),
            'std_score': statistics.stdev([r.get('overall_score', 0) for r in rankings]) if len(rankings) > 1 else 0,
            'confirmed_found': len([r for r in kept_rankings if r['specification'] in confirmed_set]),
            'agent_tokens': ranking_agent.get_summary()
        }
        iteration_stats.append(iter_stats)
        
        print(f"  Removed {remove_count} specs, kept {keep_count}")
        print(f"  Avg score: {iter_stats['avg_score']:.3f}, Confirmed found: {iter_stats['confirmed_found']}")
        
        iteration += 1
    
    # Final ranking of remaining specifications
    # print(f"\nFinal iteration: {len(current_specs)} specifications")
    # final_agent = create_ranking_agent(model_id, p_model_summary, len(current_specs))
    # agents_used.append(final_agent)
    # final_rankings = rank_specifications_iteration(final_agent, current_specs)
    # all_rankings.append(final_rankings)
    
    # if final_rankings:
    #     final_rankings.sort(key=lambda x: x.get('overall_score', 0), reverse=True)
    final_rankings = all_rankings[-1] if all_rankings else []
    
    # Calculate comprehensive statistics
    final_stats = calculate_final_statistics(agents_used, all_rankings, confirmed_set)
    
    # Score confirmed specifications
    confirmed_scoring_results = score_confirmed_specifications(benchmark, confirmed_specs, model_id)
    
    final_stats.update({
        'benchmark': benchmark,
        'k': k,
        'x': x,
        'iterations': iteration - 1,
        'initial_spec_count': len(specs),
        'final_spec_count': len(current_specs),
        'iteration_stats': iteration_stats,
        'final_rankings': final_rankings,
        'confirmed_specs_stats': confirmed_scoring_results
    })
    
    # Update total tokens to include confirmed spec scoring
    final_stats['total_tokens'] += confirmed_scoring_results.get('confirmed_scoring_tokens', 0)
    
    return final_stats

# Cache for confirmed specification scoring to avoid expensive LLM calls
_confirmed_specs_cache = {}

def score_confirmed_specifications(benchmark: str, confirmed_specs: List[str], model_id: str) -> Dict:
    """
    Score confirmed specifications using a ranking agent with caching.
    
    Args:
        benchmark: Benchmark name
        confirmed_specs: List of confirmed specifications
        model_id: Model ID for strands agents
        
    Returns:
        Dictionary with scored confirmed specifications
    """
    if not confirmed_specs:
        return {'confirmed_spec_scores': [], 'confirmed_scoring_tokens': 0}
    
    # Create cache key from benchmark, specs, and model_id
    specs_hash = hash(tuple(sorted(confirmed_specs)))  # Convert list to hashable tuple
    cache_key = (benchmark, specs_hash, model_id)
    
    # Check cache first
    if cache_key in _confirmed_specs_cache:
        print(f"Using cached scores for {len(confirmed_specs)} confirmed specifications for {benchmark}")
        return _confirmed_specs_cache[cache_key]
    
    print(f"Scoring {len(confirmed_specs)} confirmed specifications for {benchmark}...")
    
    # Get P model summary and create ranking agent
    p_model_summary = get_p_model_summary(benchmark, model_id)
    scoring_agent = create_ranking_agent(model_id, p_model_summary, len(confirmed_specs))
    
    # Score the confirmed specifications
    confirmed_rankings = rank_specifications_iteration(scoring_agent, confirmed_specs)
    
    # Sort by overall score (descending)
    if confirmed_rankings:
        confirmed_rankings.sort(key=lambda x: x.get('overall_score', 0), reverse=True)
    
    result = {
        'confirmed_spec_scores': confirmed_rankings,
        'confirmed_scoring_tokens': scoring_agent.total_tokens,
        'confirmed_scoring_agent_summary': scoring_agent.get_summary()
    }
    
    # Cache the result
    _confirmed_specs_cache[cache_key] = result
    
    return result

def calculate_final_statistics(agents: List[AgentWrapper], all_rankings: List[List[Dict]], 
                             confirmed_set: set) -> Dict:
    """Calculate comprehensive statistics from all iterations."""
    # Token statistics
    total_tokens = sum(agent.total_tokens for agent in agents)
    total_invocations = sum(agent.num_invocations for agent in agents)
    avg_tokens_per_iteration = total_tokens / len(agents) if agents else 0
    
    # Score statistics across all iterations
    all_scores = []
    for rankings in all_rankings:
        all_scores.extend([r.get('overall_score', 0) for r in rankings])
    
    score_stats = {
        'avg_score': statistics.mean(all_scores) if all_scores else 0,
        'median_score': statistics.median(all_scores) if all_scores else 0,
        'std_score': statistics.stdev(all_scores) if len(all_scores) > 1 else 0,
        'min_score': min(all_scores) if all_scores else 0,
        'max_score': max(all_scores) if all_scores else 0
    }
    
    # Confirmed specifications analysis
    final_rankings = all_rankings[-1] if all_rankings else []
    final_specs = {r['specification'] for r in final_rankings}
    confirmed_found = final_specs.intersection(confirmed_set)
    confirmed_not_found = confirmed_set - final_specs
    
    return {
        'total_tokens': total_tokens,
        'total_invocations': total_invocations,
        'avg_tokens_per_iteration': avg_tokens_per_iteration,
        'score_statistics': score_stats,
        'confirmed_specs_total': len(confirmed_set),
        'confirmed_specs_found': len(confirmed_found),
        'confirmed_specs_not_found': len(confirmed_not_found),
        'confirmed_found_list': list(confirmed_found),
        'confirmed_not_found_list': list(confirmed_not_found),
        'coverage_percentage': len(confirmed_found) / len(confirmed_set) * 100 if confirmed_set else 0
    }

def run_benchmark_suite(benchmarks_list: List[str], k: int, x: int, block_size: int, 
                       model_id: str = DEFAULT_MODEL_ID, output_file: Optional[str] = None):
    """Run iterative ranking on multiple benchmarks."""
    results = {}
    
    for benchmark in benchmarks_list:
        print(f"\n{'='*60}")
        print(f"Processing benchmark: {benchmark}")
        print(f"{'='*60}")
        
        try:
            result = iterative_ranking(benchmark, k, x, block_size, model_id)
            results[benchmark] = result
            
            # Print summary
            print(f"\nSummary for {benchmark}:")
            print(f"  Iterations: {result['iterations']}")
            print(f"  Final specs: {result['final_spec_count']}")
            print(f"  Confirmed found: {result['confirmed_specs_found']}/{result['confirmed_specs_total']}")
            print(f"  Coverage: {result['coverage_percentage']:.1f}%")
            print(f"  Total tokens: {result['total_tokens']:,}")
            
        except Exception as e:
            print(f"Error processing {benchmark}: {e}")
            results[benchmark] = {'error': str(e)}
        
        # Save intermediate results
        if output_file:
            with open(output_file, 'w') as f:
                json.dump(results, f, indent=2)
    
    return results

def parse_k_range(k_range_str: str, k_step: int = 10) -> List[int]:
    """Parse k range string into list of k values."""
    if ',' in k_range_str:
        # Comma-separated values: "10,20,30,40"
        return [int(k.strip()) for k in k_range_str.split(',')]
    elif '-' in k_range_str:
        # Range format: "10-70"
        start, end = map(int, k_range_str.split('-'))
        return list(range(start, end + 1, k_step))
    else:
        # Single value
        return [int(k_range_str)]

def process_benchmark_k_combination(benchmark: str, k: int, x: int, block_size: int, model_id: str) -> Tuple[str, int, Dict]:
    """
    Process a single benchmark-k combination for parallel execution.
    
    Args:
        benchmark: Benchmark name
        k: K value
        x: X percentage
        model_id: Model ID
        
    Returns:
        Tuple of (benchmark, k, result_dict)
    """
    try:
        result = iterative_ranking(benchmark, k, x, block_size, model_id)
        return benchmark, k, result
    except Exception as e:
        return benchmark, k, {'error': str(e)}

def smoke_test_k_values_parallel(benchmarks_list: List[str], k_values: List[int], x: int, block_size: int,
                                model_id: str = DEFAULT_MODEL_ID, output_file: str = 'smoke_test_k_values.json',
                                max_workers: int = 4):
    """
    Run smoke test across multiple k values and benchmarks with parallel processing.
    Includes optimization to skip higher k values for benchmarks with 100% coverage.
    
    Args:
        benchmarks_list: List of benchmark names to test
        k_values: List of k values to test
        x: Percentage of specs to remove each iteration
        model_id: Model ID for strands agents
        output_file: Output file for results
        max_workers: Maximum number of parallel workers
        
    Returns:
        Dictionary organized by k value, then by benchmark
    """
    import datetime
    
    # Track benchmarks that have achieved 100% coverage
    completed_benchmarks = set()
    
    # Initialize results structure
    results = {
        'metadata': {
            'x_percentage': x,
            'model_id': model_id,
            'benchmarks_tested': benchmarks_list,
            'k_values_tested': k_values,
            'timestamp': datetime.datetime.now().isoformat(),
            'parallel_processing': True,
            'max_workers': max_workers,
            'optimization_enabled': True
        }
    }
    
    # Add k value keys
    for k in k_values:
        results[str(k)] = {}
    
    print(f"Starting PARALLEL smoke test for k values: {k_values}")
    print(f"Benchmarks: {benchmarks_list}")
    print(f"Max workers: {max_workers}")
    print(f"Optimization: Skip higher k values for benchmarks with 100% coverage")
    print(f"Results will be saved to: {output_file}")
    print("="*80)
    
    # Track overall statistics
    overall_stats = {
        'total_tokens': 0,
        'total_time': 0,
        'successful_runs': 0,
        'failed_runs': 0,
        'skipped_runs': 0,
        'coverage_by_k': {},
        'tokens_by_k': {}
    }
    
    start_time = datetime.datetime.now()
    total_processed = 0
    
    # Process k values sequentially to enable optimization
    for k in k_values:
        print(f"\n{'='*80}")
        print(f"TESTING K = {k}")
        print(f"{'='*80}")
        
        # Determine which benchmarks to process for this k value
        remaining_benchmarks = [b for b in benchmarks_list if b not in completed_benchmarks]
        
        if not remaining_benchmarks:
            print(f"All benchmarks have achieved 100% coverage, skipping k={k}")
            # Add empty results for skipped benchmarks
            for benchmark in benchmarks_list:
                if benchmark in completed_benchmarks:
                    results[str(k)][benchmark] = {'skipped': True, 'reason': '100% coverage achieved with lower k'}
                    overall_stats['skipped_runs'] += 1
            continue
        
        print(f"Processing {len(remaining_benchmarks)} benchmarks: {remaining_benchmarks}")
        if len(completed_benchmarks) > 0:
            print(f"Skipping {len(completed_benchmarks)} benchmarks with 100% coverage: {list(completed_benchmarks)}")
        
        # Create combinations for this k value
        combinations = [(benchmark, k, x, block_size, model_id) for benchmark in remaining_benchmarks]
        
        # Thread-safe lock for updating results
        results_lock = threading.Lock()
        k_completed_count = 0
        
        def update_progress(benchmark: str, k_val: int, result: Dict):
            nonlocal k_completed_count, total_processed
            with results_lock:
                k_completed_count += 1
                total_processed += 1
                progress = (k_completed_count / len(remaining_benchmarks)) * 100
                
                # Store result
                results[str(k_val)][benchmark] = result
                
                # Update statistics
                if 'error' not in result:
                    overall_stats['successful_runs'] += 1
                    overall_stats['total_tokens'] += result['total_tokens']
                    print(f"[{k_completed_count}/{len(remaining_benchmarks)}] ({progress:.1f}%) ✓ {benchmark} k={k_val}: "
                          f"{result['confirmed_specs_found']}/{result['confirmed_specs_total']} confirmed, "
                          f"{result['total_tokens']:,} tokens")
                    
                    # Check if this benchmark achieved 100% coverage
                    if result['coverage_percentage'] >= 100.0:
                        completed_benchmarks.add(benchmark)
                        print(f"    🎯 {benchmark} achieved 100% coverage - will skip higher k values")
                else:
                    overall_stats['failed_runs'] += 1
                    print(f"[{k_completed_count}/{len(remaining_benchmarks)}] ({progress:.1f}%) ✗ {benchmark} k={k_val}: {result['error']}")
                
                # Save intermediate results every 3 completions
                if k_completed_count % 3 == 0 or k_completed_count == len(remaining_benchmarks):
                    with open(output_file, 'w') as f:
                        json.dump(results, f, indent=2)
        
        # Execute combinations in parallel for this k value
        with concurrent.futures.ThreadPoolExecutor(max_workers=max_workers) as executor:
            # Submit all tasks for this k value
            future_to_combination = {
                executor.submit(process_benchmark_k_combination, *combo): combo 
                for combo in combinations
            }
            
            # Process completed tasks
            for future in concurrent.futures.as_completed(future_to_combination):
                benchmark, k_val, result = future.result()
                update_progress(benchmark, k_val, result)
        
        # Add skipped benchmarks to results for this k value
        for benchmark in completed_benchmarks:
            if benchmark not in results[str(k)]:
                results[str(k)][benchmark] = {'skipped': True, 'reason': '100% coverage achieved with lower k'}
                overall_stats['skipped_runs'] += 1
    
    # Calculate k-level and overall statistics
    for k in k_values:
        k_stats = {
            'total_tokens': 0,
            'successful_benchmarks': 0,
            'failed_benchmarks': 0,
            'skipped_benchmarks': 0,
            'total_confirmed_found': 0,
            'total_confirmed_specs': 0
        }
        
        for benchmark in benchmarks_list:
            if benchmark in results[str(k)]:
                result = results[str(k)][benchmark]
                if 'skipped' in result:
                    k_stats['skipped_benchmarks'] += 1
                elif 'error' not in result:
                    k_stats['total_tokens'] += result['total_tokens']
                    k_stats['successful_benchmarks'] += 1
                    k_stats['total_confirmed_found'] += result['confirmed_specs_found']
                    k_stats['total_confirmed_specs'] += result['confirmed_specs_total']
                else:
                    k_stats['failed_benchmarks'] += 1
        
        k_coverage = (k_stats['total_confirmed_found'] / k_stats['total_confirmed_specs'] * 100) if k_stats['total_confirmed_specs'] > 0 else 0
        overall_stats['coverage_by_k'][str(k)] = k_coverage
        overall_stats['tokens_by_k'][str(k)] = k_stats['total_tokens']
        
        # Add k-level summary to results
        results[str(k)]['_summary'] = {
            'k_value': k,
            'successful_benchmarks': k_stats['successful_benchmarks'],
            'failed_benchmarks': k_stats['failed_benchmarks'],
            'skipped_benchmarks': k_stats['skipped_benchmarks'],
            'total_benchmarks': len(benchmarks_list),
            'overall_coverage_percentage': k_coverage,
            'total_confirmed_found': k_stats['total_confirmed_found'],
            'total_confirmed_specs': k_stats['total_confirmed_specs'],
            'total_tokens': k_stats['total_tokens']
        }
    
    # Calculate final overall statistics
    end_time = datetime.datetime.now()
    total_duration = (end_time - start_time).total_seconds()
    
    # Calculate actual combinations processed vs theoretical maximum
    theoretical_combinations = len(benchmarks_list) * len(k_values)
    actual_combinations = overall_stats['successful_runs'] + overall_stats['failed_runs']
    optimization_savings = theoretical_combinations - actual_combinations - overall_stats['skipped_runs']
    
    # Add overall summary to results
    results['overall_summary'] = {
        'theoretical_combinations': theoretical_combinations,
        'actual_combinations_processed': actual_combinations,
        'skipped_combinations': overall_stats['skipped_runs'],
        'optimization_savings': optimization_savings,
        'successful_runs': overall_stats['successful_runs'],
        'failed_runs': overall_stats['failed_runs'],
        'success_rate_percentage': (overall_stats['successful_runs'] / actual_combinations * 100) if actual_combinations > 0 else 0,
        'total_tokens_used': overall_stats['total_tokens'],
        'total_duration_seconds': total_duration,
        'average_tokens_per_run': overall_stats['total_tokens'] / overall_stats['successful_runs'] if overall_stats['successful_runs'] > 0 else 0,
        'coverage_by_k_value': overall_stats['coverage_by_k'],
        'tokens_by_k_value': overall_stats['tokens_by_k'],
        'parallel_speedup_estimate': f"{actual_combinations * 45 / total_duration:.1f}x" if total_duration > 0 else "N/A"
    }
    
    # Update metadata with final counts
    results['metadata']['total_combinations'] = actual_combinations
    results['metadata']['optimization_savings'] = optimization_savings
    
    print(f"\n{'='*80}")
    print("PARALLEL SMOKE TEST COMPLETE")
    print(f"{'='*80}")
    print(f"Total time: {total_duration:.1f} seconds ({total_duration/60:.1f} minutes)")
    print(f"Successful runs: {overall_stats['successful_runs']}")
    print(f"Skipped runs (optimization): {overall_stats['skipped_runs']}")
    print(f"Optimization savings: {optimization_savings} combinations")
    print(f"Total tokens: {overall_stats['total_tokens']:,}")
    print(f"Estimated speedup: {results['overall_summary']['parallel_speedup_estimate']}")
    print(f"Results saved to: {output_file}")
    
    # Print coverage summary by k value
    print(f"\nCoverage Summary by K Value:")
    for k in k_values:
        coverage = overall_stats['coverage_by_k'].get(str(k), 0)
        tokens = overall_stats['tokens_by_k'].get(str(k), 0)
        print(f"  K={k:2d}: {coverage:5.1f}% coverage, {tokens:,} tokens")
    
    # Final save
    with open(output_file, 'w') as f:
        json.dump(results, f, indent=2)
    
    return results

def smoke_test_k_values(benchmarks_list: List[str], k_values: List[int], x: int, block_size: int,
                       model_id: str = DEFAULT_MODEL_ID, output_file: str = 'smoke_test_k_values.json',
                       parallel: bool = False, max_workers: int = 4):
    """
    Run smoke test across multiple k values and benchmarks.
    
    Args:
        benchmarks_list: List of benchmark names to test
        k_values: List of k values to test
        x: Percentage of specs to remove each iteration
        model_id: Model ID for strands agents
        output_file: Output file for results
        parallel: Whether to use parallel processing
        max_workers: Maximum number of parallel workers (if parallel=True)
        
    Returns:
        Dictionary organized by k value, then by benchmark
    """
    if parallel:
        return smoke_test_k_values_parallel(benchmarks_list, k_values, x, block_size, model_id, output_file, max_workers)
    
    # Sequential implementation with optimization
    import datetime
    
    # Track benchmarks that have achieved 100% coverage
    completed_benchmarks = set()
    
    # Initialize results structure
    results = {
        'metadata': {
            'x_percentage': x,
            'model_id': model_id,
            'benchmarks_tested': benchmarks_list,
            'k_values_tested': k_values,
            'timestamp': datetime.datetime.now().isoformat(),
            'parallel_processing': False,
            'optimization_enabled': True
        }
    }
    
    # Add k value keys
    for k in k_values:
        results[str(k)] = {}
    
    print(f"Starting SEQUENTIAL smoke test for k values: {k_values}")
    print(f"Benchmarks: {benchmarks_list}")
    print(f"Optimization: Skip higher k values for benchmarks with 100% coverage")
    print(f"Results will be saved to: {output_file}")
    print("="*80)
    
    # Track overall statistics
    overall_stats = {
        'total_tokens': 0,
        'total_time': 0,
        'successful_runs': 0,
        'failed_runs': 0,
        'skipped_runs': 0,
        'coverage_by_k': {},
        'tokens_by_k': {}
    }
    
    start_time = datetime.datetime.now()
    current_combination = 0
    
    for k in k_values:
        print(f"\n{'='*80}")
        print(f"TESTING K = {k}")
        print(f"{'='*80}")
        
        # Determine which benchmarks to process for this k value
        remaining_benchmarks = [b for b in benchmarks_list if b not in completed_benchmarks]
        
        if not remaining_benchmarks:
            print(f"All benchmarks have achieved 100% coverage, skipping k={k}")
            # Add empty results for skipped benchmarks
            for benchmark in benchmarks_list:
                if benchmark in completed_benchmarks:
                    results[str(k)][benchmark] = {'skipped': True, 'reason': '100% coverage achieved with lower k'}
                    overall_stats['skipped_runs'] += 1
            continue
        
        print(f"Processing {len(remaining_benchmarks)} benchmarks: {remaining_benchmarks}")
        if len(completed_benchmarks) > 0:
            print(f"Skipping {len(completed_benchmarks)} benchmarks with 100% coverage: {list(completed_benchmarks)}")
        
        k_start_time = datetime.datetime.now()
        k_stats = {
            'total_tokens': 0,
            'successful_benchmarks': 0,
            'failed_benchmarks': 0,
            'skipped_benchmarks': 0,
            'total_confirmed_found': 0,
            'total_confirmed_specs': 0
        }
        
        for benchmark in remaining_benchmarks:
            current_combination += 1
            progress = (current_combination / len(remaining_benchmarks)) * 100
            
            print(f"\n[{current_combination}/{len(remaining_benchmarks)}] ({progress:.1f}%) Processing {benchmark} with k={k}")
            
            try:
                # Run the iterative ranking
                result = iterative_ranking(benchmark, k, x, block_size, model_id)
                results[str(k)][benchmark] = result
                
                # Update statistics
                k_stats['total_tokens'] += result['total_tokens']
                k_stats['successful_benchmarks'] += 1
                k_stats['total_confirmed_found'] += result['confirmed_specs_found']
                k_stats['total_confirmed_specs'] += result['confirmed_specs_total']
                overall_stats['successful_runs'] += 1
                
                print(f"  ✓ Success: {result['confirmed_specs_found']}/{result['confirmed_specs_total']} confirmed specs found")
                print(f"  ✓ Tokens: {result['total_tokens']:,}")
                
                # Check if this benchmark achieved 100% coverage
                if result['coverage_percentage'] >= 100.0:
                    completed_benchmarks.add(benchmark)
                    print(f"    🎯 {benchmark} achieved 100% coverage - will skip higher k values")
                
            except Exception as e:
                print(f"  ✗ Error: {e}")
                results[str(k)][benchmark] = {'error': str(e)}
                k_stats['failed_benchmarks'] += 1
                overall_stats['failed_runs'] += 1
            
            # Save intermediate results after each benchmark
            with open(output_file, 'w') as f:
                json.dump(results, f, indent=2)
        
        # Add skipped benchmarks to results for this k value
        for benchmark in completed_benchmarks:
            if benchmark not in results[str(k)]:
                results[str(k)][benchmark] = {'skipped': True, 'reason': '100% coverage achieved with lower k'}
                k_stats['skipped_benchmarks'] += 1
                overall_stats['skipped_runs'] += 1
        
        # Calculate k-level statistics
        k_end_time = datetime.datetime.now()
        k_duration = (k_end_time - k_start_time).total_seconds()
        
        k_coverage = (k_stats['total_confirmed_found'] / k_stats['total_confirmed_specs'] * 100) if k_stats['total_confirmed_specs'] > 0 else 0
        
        overall_stats['coverage_by_k'][str(k)] = k_coverage
        overall_stats['tokens_by_k'][str(k)] = k_stats['total_tokens']
        overall_stats['total_tokens'] += k_stats['total_tokens']
        
        print(f"\nK={k} Summary:")
        print(f"  Successful benchmarks: {k_stats['successful_benchmarks']}/{len(remaining_benchmarks)}")
        print(f"  Skipped benchmarks: {k_stats['skipped_benchmarks']}")
        print(f"  Overall coverage: {k_coverage:.1f}%")
        print(f"  Total tokens: {k_stats['total_tokens']:,}")
        print(f"  Time taken: {k_duration:.1f} seconds")
        
        # Add k-level summary to results
        results[str(k)]['_summary'] = {
            'k_value': k,
            'successful_benchmarks': k_stats['successful_benchmarks'],
            'failed_benchmarks': k_stats['failed_benchmarks'],
            'skipped_benchmarks': k_stats['skipped_benchmarks'],
            'total_benchmarks': len(benchmarks_list),
            'overall_coverage_percentage': k_coverage,
            'total_confirmed_found': k_stats['total_confirmed_found'],
            'total_confirmed_specs': k_stats['total_confirmed_specs'],
            'total_tokens': k_stats['total_tokens'],
            'duration_seconds': k_duration
        }
        
        # Reset combination counter for next k value
        current_combination = 0
    
    # Calculate final overall statistics
    end_time = datetime.datetime.now()
    total_duration = (end_time - start_time).total_seconds()
    
    # Calculate actual combinations processed vs theoretical maximum
    theoretical_combinations = len(benchmarks_list) * len(k_values)
    actual_combinations = overall_stats['successful_runs'] + overall_stats['failed_runs']
    optimization_savings = theoretical_combinations - actual_combinations - overall_stats['skipped_runs']
    
    # Add overall summary to results
    results['overall_summary'] = {
        'theoretical_combinations': theoretical_combinations,
        'actual_combinations_processed': actual_combinations,
        'skipped_combinations': overall_stats['skipped_runs'],
        'optimization_savings': optimization_savings,
        'successful_runs': overall_stats['successful_runs'],
        'failed_runs': overall_stats['failed_runs'],
        'success_rate_percentage': (overall_stats['successful_runs'] / actual_combinations * 100) if actual_combinations > 0 else 0,
        'total_tokens_used': overall_stats['total_tokens'],
        'total_duration_seconds': total_duration,
        'average_tokens_per_run': overall_stats['total_tokens'] / overall_stats['successful_runs'] if overall_stats['successful_runs'] > 0 else 0,
        'coverage_by_k_value': overall_stats['coverage_by_k'],
        'tokens_by_k_value': overall_stats['tokens_by_k']
    }
    
    # Update metadata with final counts
    results['metadata']['total_combinations'] = actual_combinations
    results['metadata']['optimization_savings'] = optimization_savings
    
    print(f"\n{'='*80}")
    print("SEQUENTIAL SMOKE TEST COMPLETE")
    print(f"{'='*80}")
    print(f"Total time: {total_duration:.1f} seconds ({total_duration/60:.1f} minutes)")
    print(f"Successful runs: {overall_stats['successful_runs']}")
    print(f"Skipped runs (optimization): {overall_stats['skipped_runs']}")
    print(f"Optimization savings: {optimization_savings} combinations")
    print(f"Total tokens: {overall_stats['total_tokens']:,}")
    print(f"Results saved to: {output_file}")
    
    # Print coverage summary by k value
    print(f"\nCoverage Summary by K Value:")
    for k in k_values:
        coverage = overall_stats['coverage_by_k'].get(str(k), 0)
        tokens = overall_stats['tokens_by_k'].get(str(k), 0)
        print(f"  K={k:2d}: {coverage:5.1f}% coverage, {tokens:,} tokens")
    
    # Final save
    with open(output_file, 'w') as f:
        json.dump(results, f, indent=2)
    
    return results

if __name__ == '__main__':
    import argparse
    
    parser = argparse.ArgumentParser(description='Iterative specification ranking with strands agents')
    parser.add_argument('--k', type=int, default=20, help='Target number of top specifications')
    parser.add_argument('--x', type=int, default=20, help='Percentage of specs to remove each iteration (1-99)')
    parser.add_argument('--benchmark', type=str, help='Single benchmark to process')
    parser.add_argument('--benchmarks', type=str, nargs='+', help='Multiple benchmarks to process')
    parser.add_argument('--all-benchmarks', action='store_true', help='Process all benchmarks from constants.py')
    parser.add_argument('--model-id', type=str, default=DEFAULT_MODEL_ID, help='Model ID for strands agents')
    parser.add_argument('--output', type=str, default='iterative_ranking_results.json', help='Output file for results')
    parser.add_argument('--weights', type=str, help='JSON string with custom weights for compute_score')
    parser.add_argument('--max-blocks', type=int, default=50, help='Maximum number of blocks to consider for ranking (default: 50)')
    
    # Smoke test arguments
    parser.add_argument('--smoke-test-k', action='store_true', help='Run smoke test across multiple k values')
    parser.add_argument('--k-range', type=str, default='10-70', help='K value range for smoke test (e.g., "10-70" or "10,20,30,40")')
    parser.add_argument('--k-step', type=int, default=10, help='Step size for k values in range (default: 10)')
    parser.add_argument('--parallel', action='store_true', help='Use parallel processing for smoke test')
    parser.add_argument('--max-workers', type=int, default=4, help='Maximum number of parallel workers (default: 4)')
    
    args = parser.parse_args()
    
    # Parse custom weights if provided
    custom_weights = None
    if args.weights:
        try:
            custom_weights = json.loads(args.weights)
            # Validate weights
            compute_score(0.5, 0.5, 0.5, 0.5, custom_weights)  # Test with dummy values
        except Exception as e:
            print(f"Error parsing weights: {e}")
            exit(1)
    
    # Determine benchmarks to process
    if args.benchmark:
        benchmarks_to_process = [args.benchmark]
    elif args.benchmarks:
        benchmarks_to_process = args.benchmarks
    elif args.all_benchmarks:
        benchmarks_to_process = benchmarks + ['Kermit2PC', 'JournalLeaderElection']
    else:
        print("Please specify --benchmark, --benchmarks, or --all-benchmarks")
        exit(1)
    
    # Check if smoke test mode
    if args.smoke_test_k:
        # Parse k values for smoke test
        k_values = parse_k_range(args.k_range, args.k_step)
        print(f"Running smoke test with k values: {k_values}")
        
        # Set output file for smoke test
        smoke_output = args.output.replace('.json', '_smoke_test_k.json') if args.output.endswith('.json') else f"{args.output}_smoke_test_k.json"
        
        # Run smoke test
        results = smoke_test_k_values(
            benchmarks_to_process,
            k_values,
            args.x,
            args.max_blocks,
            args.model_id,
            smoke_output,
            parallel=args.parallel,
            max_workers=args.max_workers
        )
        
        print(f"\nSmoke test results saved to {smoke_output}")
    else:
        # Run regular ranking
        results = run_benchmark_suite(
            benchmarks_to_process, 
            args.k, 
            args.x, 
            args.max_blocks,
            args.model_id, 
            args.output
        )
        
        # Final save
        with open(args.output, 'w') as f:
            json.dump(results, f, indent=2)
        
        print(f"\nResults saved to {args.output}")
