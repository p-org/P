#!/bin/bash
#SBATCH --job-name=pinfer-paxos   # create a short name for your job
#SBATCH --nodes=1                # node count
#SBATCH --ntasks=1               # total number of tasks across all nodes
#SBATCH --cpus-per-task=64        # cpu-cores per task (>1 if multi-threaded tasks)
#SBATCH --mem-per-cpu=4G         # memory per cpu-core (4G is default)
#SBATCH --time=10:00:00          # total run time limit (HH:MM:SS)
#SBATCH --mail-type=end          # send email when job ends
#SBATCH --mail-user=dh7120@princeton.edu
#SBATCH --partition=malik
export PINFER_NUM_CORES=$SLURM_CPUS_PER_TASK
p infer -t /scratch/gpfs/dh7120/ChainReplication/10000 -z3
