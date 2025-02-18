#!/bin/bash
#SBATCH --job-name=pinfer-raft-trace-controlled   # create a short name for your job
#SBATCH --nodes=1                # node count
#SBATCH --ntasks=1               # total number of tasks across all nodes
#SBATCH --cpus-per-task=64        # cpu-cores per task (>1 if multi-threaded tasks)
#SBATCH --mem-per-cpu=4G         # memory per cpu-core (4G is default)
#SBATCH --time=24:00:00          # total run time limit (HH:MM:SS)
#SBATCH --mail-type=end          # send email when job ends
#SBATCH --partition=malik
#SBATCH --mail-user=dh7120@princeton.edu
export PINFER_NUM_CORES=$SLURM_CPUS_PER_TASK
p infer -t /scratch/gpfs/dh7120/Raft/500 -ce eRaftConfig -z3
p infer -t /scratch/gpfs/dh7120/Raft/1000 -ce eRaftConfig -z3
p infer -t /scratch/gpfs/dh7120/Raft/2000 -ce eRaftConfig -z3
p infer -t /scratch/gpfs/dh7120/Raft/4000 -ce eRaftConfig -z3
p infer -t /scratch/gpfs/dh7120/Raft/8000 -ce eRaftConfig -z3
p infer -t /scratch/gpfs/dh7120/Raft/10000 -ce eRaftConfig -z3
p infer -t /scratch/gpfs/dh7120/Raft/20000 -ce eRaftConfig -z3
p infer -t /scratch/gpfs/dh7120/Raft/30000 -ce eRaftConfig -z3
p infer -t /scratch/gpfs/dh7120/Raft/40000 -ce eRaftConfig -z3
p infer -t /scratch/gpfs/dh7120/Raft/50000 -ce eRaftConfig -z3
