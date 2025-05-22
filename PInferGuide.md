# PInfer - Automatic Invariant Learning from Traces
## Prerequisites
> Note that PInfer has only been tested on Amazon Linux.

### Installing Z3 (before building P)
1. Clone the Z3 repository: https://github.com/Z3Prover/z3 by `git clone git@github.com:Z3Prover/z3.git`
2. Run `python scripts/mk_make.py -x --dotnet`
3. `cd build; make -j$(nproc)`
4. `sudo make install`
5. Add `export LD_LIBRARY_PATH="$LD_LIBRARY_PATH:/path-to-z3/build/"` to `.bashrc` (or `.zshrc` if using Zsh) where `path-to-z3` is the directory of the cloned Z3 repository in step 1.

#### Environment setups
- Follow the instruction of [P language here](https://p-org.github.io/P/getstarted/install/) and install dotnet. The version used for developing is 8.0.401.
- Install Java 22 and maven
    + For amazon linux, [follow the guide here](https://docs.aws.amazon.com/corretto/latest/corretto-22-ug/generic-linux-install.html#rpm-linux-install-instruct)
    + Maven version: Apache Maven 3.5.2. Can be installed via `sudo yum install maven`
- Install PJavaRuntime: go to [Src/Runtimes/PJavaRuntimes](./Src/PRuntimes/PJavaRuntime/) and run `mvn install`
- Compile P: run `dotnet build -c Release` under the root of this repo. 

## Step 1: Getting traces
### Using the python script for generating traces
There is a `1_prepare_traces.py` under `Tutoiral/PInfer`. To use this script to generate traces, first add an entry to `configurations` dictionary in `Tutoiral/PInfer/constants.py`.
The key should be the name of the folder under `Tutorial/PInfer` that contains the P source code of the benchmark, and the value is a list of names of test cases to run to obtain the traces. 

You may optionally set an environment variable called `PINFER_TRACE_DIR` (e.g. `export PINFER_TRACE_DIR=/home/PInferTraces) that specifies the path to store the generated traces. 

After making the changes, run with
```
> python3 1_prepare_traces.py --benchmarks [names of benchmarks] --num_traces [a list of numbers representing number of traces to generate] [--trace_dir <path-to-store-traces>]
```

For instance, to generate 10k traces for Paxos storing into `$PINFER_TRACE_DIR`, run the following
> `python3 1_prepare_traces.py --benchmarks paxos --num_traces 10000`

### Event filters
By default, the generated traces have all Send and Announce events being recorded.
For some benchmarks, we may want to only record events that are relevant to the protocol.
To do so, add an entry in `event_combs` dictionary in `constants.py`, where the key is the name of directory of the benchmark and the value is in the form of `[(e1, e2, e3...)]` where `e1, e2, e3...` are events that are relevant to the protocol.

**Removing traces:** If you want to remove certain traces, simply delete the files. You don't need to remove the metadata in the JSON file.

**How are trace metadata being used?** When checking `n` events of type e1, e2, ..., en, PInfer will first look for any *exact* match on the index, i.e. a folder that have traces that contains events that are *exactly* of type e1, ..., en. If no such folder is found, PInfer looks for any folder that holds traces that is a superset of (e1, ..., en). 

> Alternatively, some of the codebase have `generateTrace.sh`. You can simply run `./generateTrace.sh <num schedules> <e1> <e2> ... <en>` and it will gather traces containing e1, e2,...,en using all test cases. 

## Step 2: Running PInfer
For our benchmarks, simply execute `run.sh` under the benchmark folders. 

To setup your own experiments:
### Fully-automated mode
Simply run `p infer`, it will infer combinations of events that might yield interesting properties and then perform the search over the lattice.

If traces are stored under some other paths, you can run it with `p infer -t <path-to-traces>`. Note that there must be a `metadata.json` under the provided path. 

If traces were generated using `1_prepare_traces.py`, then under `PINFER_TRACE_DIR`, traces are stored in folders that have the same name as the benchmark. For example `PINFER_TRACE_DIR/paxos/10000` stores 10k traces for paxos. This is the path you will need to pass to PInfer using `-t` commandline argument. For example, running PInfer under `paxos` with 10k traces is

```
> p infer -t $PINFER_TRACE_DIR/paxos/10000
```

To enable SMT-based pruning, add a `-z3` flag, e.g.,
```
> p infer -t $PINFER_TRACE_DIR/paxos/10000 -z3
```

Default parameters (upper bounds): 
- `term_depth`: 2
- `num_guards`: 2
- `num_filters`: 2
- `exists`: 1
- `config_event`: null

`arity` is determined by the maximum arity of prediates generated. 

These can be configured using command line argumets:
> p infer -td <`max term depth`> -max-guards <`max conj in guards`> -max-filters <`max conj. in filters`> -ce <`event name`>

### Hints
Hints is a construct in P program that provides manual hints for PInfer. 

Hints can be defined as follows:
```
hint <hint name> (e1: Event1 ... en: EventN) {
    term_depth =  <int>;
    exists =      <int>;
    num_guards =  <int>;
    num_filters = <int>;
    arity =       <int>;
    config_event = <event name>;
    include_guards = <bool expr>;    // predicates that must be included in the guards, e.g. e0.key == e1.key; conjuntion only.
    include_filters = <bool expr>;   // similar, but must be included in filters
}
```

If any of the field is not provided, the default values are:
- `exists` = 0
- `num_guards` = 0
- `num_filters` = 0
- `arity` = 1
- `config_event` = null
- `include_guards` = true
- `include_filters` = true

#### Generating SpecMiner for a hint
> p infer --action compile --hint <`name of the hint`>

PInfer will generate the SpecMiner specifically for the provided hint.

#### Searching the space defined by a hint
> p infer --action run --hint <`name of hint`> \[--traces <`folder`>\]

PInfer will search the formula in the form of `forall* G -> exists* F /\ R` that holds for all traces. PInfer starts with the provided parameters in the hint and search till the upperbound. 
By default PInfer looks for traces under `traces` folder. Notice that the traces must be indexed, i.e. having a `metadata.json` mapping sets of events to folders containing corresponding traces.

#### Searching a specific grid
**exact hints:** Mainly used for debugging purposes. When declaring a hint with `exact` keyword, i.e. `hint exact ...` then running it, PInfer will only check the search space defined by the given parameters in the hint but will not go up the grid. 

#### List out predicates generated
> p infer -lp
#### List out terms generated
> p infer -lt

## Step 3: Outputs
After PInfer finishes, invariants will be stored in `invariants.txt` in the folder PInfer was executed. Another file `inv_running.txt` will be updated as PInfer finds new invariants. Mined properties are pretty-printted. 