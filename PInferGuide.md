# PInfer - Automatic Invariant Learning from Traces
## Prerequisites
> Note that PInfer has only been tested on Amazon Linux.

**Environment setups**
- Follow the instruction of [P language here](https://p-org.github.io/P/getstarted/install/) and install dotnet. The version used for developing is 8.0.401.
- Install Java 22 and maven
    + For amazon linux, [follow the guide here](https://docs.aws.amazon.com/corretto/latest/corretto-22-ug/generic-linux-install.html#rpm-linux-install-instruct)
    + Maven version: Apache Maven 3.5.2. Can be installed via `sudo yum install maven`
- Install PJavaRuntime: go to [Src/Runtimes/PJavaRuntimes](./Src/PRuntimes/PJavaRuntime/) and run `mvn install`
- Compile P: run `dotnet build -c Release` under the root of this repo. 

## Step 1: Getting traces
PInfer extends PChecker with a trace filtering mode. This mode automatically index logs aggregated into `traces` folder (you may specify another directory if wanted, see below). Usage:
```
p check -tc <testcase> -s <num schedules> --pinfer -ef <e1> <e2> ... <en>
```
where `ei` is the name of events to be filtered. If no `-ef` provided, then all `SendEvent` and `AnnounceEvent` will be recorded and indexed. 

Notice that there will be a `metadata.json` under `traces` that bookkeeps the folder and event information. Please do not edit it manually.

**Removing traces:** If you want to remove certain traces, simply delete the files. You don't need to remove the metadata in the JSON file.

**How are trace metadata being used?** When checking `n` events of type e1, e2, ..., en, PInfer will first look for any *exact* match on the index, i.e. a folder that have traces that contains events that are *exactly* of type e1, ..., en. If no such folder is found, PInfer looks for any folder that holds traces that is a superset of (e1, ..., en). 

> Alternatively, some of the codebase have `generateTrace.sh`. You can simply run `./generateTrace.sh <num schedules> <e1> <e2> ... <en>` and it will gather traces containing e1, e2,...,en using all test cases. 

## Step 2: Running PInfer
### Fully-automated mode
Simply run `p infer`, it will infer combinations of events that might yield interesting properties and then perform the search over the lattice. 

Default parameters (upper bounds): 
- `term_depth`: 1
- `num_guards`: 2
- `num_filters`: 2
- `exists`: 1
- `config_event`: null

`arity` is determined by the maximum arity of prediates generated. 

These can be configured using command line argumets:
> p infer -td <max term depth> -max-guards <max conj in guards> -max-filters <max conj. in filters> -ce <event name>

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
> p infer --action compile --hint <name of the hint>

PInfer will generate the SpecMiner specifically for the provided hint.

#### Searching the space defined by a hint
> p infer --action run --hint <name of hint> \[--traces <folder>\]

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