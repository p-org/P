import os
import json
import argparse
from constants import num_traces, benchmarks, trace_dir

def generate_slurm_job(name: str, trace_dirs):
    cmds = '\n'.join([f'p infer -t {trace_dir}' for trace_dir in trace_dirs])
    job = f'''#!/bin/bash
#SBATCH --job-name=pinfer-{name}-autogen   # create a short name for your job
#SBATCH --nodes=1                # node count
#SBATCH --ntasks=1               # total number of tasks across all nodes
#SBATCH --cpus-per-task=64        # cpu-cores per task (>1 if multi-threaded tasks)
#SBATCH --mem-per-cpu=4G         # memory per cpu-core (4G is default)
#SBATCH --time=24:00:00          # total run time limit (HH:MM:SS)
#SBATCH --mail-type=end          # send email when job ends
#SBATCH --mail-user=dh7120@princeton.edu
#SBATCH --partition=malik
export PINFER_NUM_CORES=$SLURM_CPUS_PER_TASK
{cmds}
    '''
    with open(f'{name}_auto.slurm', 'w') as f:
        f.write(job)


def run_benchmark(name: str, use_slurm: bool = False):
    os.chdir(name)
    if use_slurm:
        trace_dirs = [os.path.join(trace_dir, name, str(n)) for n in num_traces]
        generate_slurm_job(name, trace_dirs)
        os.system(f'sbatch {name}_auto.slurm')
        os.chdir('..')
        return
    for n in num_traces:
        trace_folder = os.path.join(trace_dir, name, str(n))
        if not os.path.exists(trace_folder):
            print(f'{trace_folder} does not exist. Skipping...')
            continue
        print(f'Running p infer on {trace_folder}')
        os.system(f'p infer -t {trace_folder}')
    os.chdir('..')

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--trace_dir', type=str, default=trace_dir)
    parser.add_argument('--benchmarks', type=str, nargs='+', default=benchmarks)
    parser.add_argument('--slurm', action='store_true')
    args = parser.parse_args()
    for name in args.benchmarks:
        print(f'[Step 2] Running benchmark: {name}')
        run_benchmark(name, args.slurm)
