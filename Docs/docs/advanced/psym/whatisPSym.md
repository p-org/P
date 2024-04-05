PSym is a new checker for P models developed to complement the default P checker with the primary objective
to avoid repetition during state-space exploration. PSym guarantees to never repeat an already-explored execution, and
hence, can exhaustively explore all possible executions. PSym also has an inbuilt coverage tracker that reports estimated
coverage to give measurable and continuous feedback (even when no bug is found during exploration).

!!! tip "Recommendation"

    Exhaustively exploring all possible executions is generally not possible for large models due to
    time/memory constraints.
    We recommend always trying PSym with easier tests first, such as ones with only a small number of replica machines and
    `choose(*)` expressions with fewer choices.


P compiler has a dedicated backend for PSym, which compiles the P model into a symbolically-instrumented intermediate
representation in Java, packed as a single `.jar` file. Executing the `.jar` file runs PSym runtime. Commandline arguments
can be passed when running the `.jar` file to configure the exploration strategy. At the end of a run, PSym reports the
result (safe / buggy / partially-safe), an error trace (if found a bug), along with a coverage and statistics report.

``` mermaid
graph LR
  Pmodel(P Model <br/> *.p) --> Pcompiler[P Compiler]--> IR(Symbolic IR in Java <br/> *.jar) --> Psym[PSym Runtime];
  Psym[PSym Runtime] --> Result[Result <br/> Coverage <br/> Statistics];

  style Pcompiler fill:#FFEFD5,stroke:#FFEFD5,stroke-width:2px
  style Psym fill:#FFEFD5,stroke:#FFEFD5,stroke-width:2px
  style Result fill:#CCFF66,stroke:#CCFF66,stroke-width:2px
```
