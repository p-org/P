import os
import subprocess

benchmarks = [
    ("leader-election", "basic_f"),
    ("consensus_epr", "auto"),
    ("2PC", "auto"),
    ("sharded_kv", "auto"),
    ("distributed_lock", "auto"),
    ("paxos", "auto"),
    ("vertical_paxos", "auto"),
    ("lock-server-async", "auto"),
    ("chain", "auto"),
]

safety_props = {
    'leader-election': ["forall A000:node, A001:node, A002:node . ~leader(A000) | ~leader(A001) | A000 = A001"],
    # ignoring other ones since SWISS cannot find anything
}

def check(benchmark, learned):
    if not learned:
        print(f"No invariants learned for {benchmark[0]}")
        return 0
    num_learned = 0
    if benchmark[0] in safety_props:
        for prop in safety_props[benchmark[0]]:
            if prop not in learned:
                print(f"Not matching: learned {learned} v.s. expected {safety_props[benchmark[0]]}")
            else:
                num_learned += 1
    return num_learned

def run_benchmark(benchmark):
    if not os.path.exists("logs"):
        os.makedirs("logs")
    print(f"--- Running benchmark: {benchmark[0]} with config: {benchmark[1]} ---")
    result = subprocess.run(
        ["./run.sh", f"benchmarks/{benchmark[0]}.ivy",
         "--config", benchmark[1],
         "--threads", "1", 
         "--minimal-models", 
         "--with-conjs"],
         capture_output=True,
         text=True,
         cwd="."
    )
    if result.returncode != 0:
        print(f"Error running benchmark {benchmark[0]}: {result.stderr}")
    else:
        log_dir = result.stdout.split("logs/")[-1].strip()
        log_dir = log_dir.split(" is ")[0].split(os.sep)[0]
        log_dir = os.path.join("logs", log_dir)
        if os.path.exists(log_dir):
            with open(os.path.join(log_dir, "invariants"), "r") as f:
                invariants = f.readlines()
                invariants = [line for line in invariants if not line.startswith("#")]
            print(f"{len(invariants)} learned invariants for {benchmark[0]}")
            return invariants
    return None

def main(benchmarks):
    total_invariants = 0
    stats = {}
    for benchmark in benchmarks:
        os.system('sleep 1') # avoid log dir conflict ...
        invariants = run_benchmark(benchmark)
        result = check(benchmark, invariants)
        stats[benchmark[0]] = result
        total_invariants += result
    print(f"\n{'=' * 60}\nBenchmark Summary\n{'=' * 60}")
    for benchmark, invariants in stats.items():
        print(f"{benchmark:25} | SWISS: {'✓' if invariants is not None else '✗'} | Safety Props:  {stats[benchmark]}")
    print(f"Total invariants learned across all benchmarks: {total_invariants}")

if __name__ == "__main__":
    import argparse
    parser = argparse.ArgumentParser(description="Run SWISS benchmarks and collect invariants.")
    parser.add_argument("--benchmarks", nargs="+", default=[b[0] for b in benchmarks],
                        help="List of benchmarks to run (default: all)")
    args = parser.parse_args()
    selected_benchmarks = [b for b in benchmarks if b[0] in args.benchmarks]
    if not selected_benchmarks:
        print("No valid benchmarks selected. Available benchmarks are:")
        for b in benchmarks:
            print(f"- {b[0]}")
    else:
        main(selected_benchmarks)