
PSym is a new systematic explorer for P developed to complement the default P checker with the objective to:

- **Avoid repetition** during state-space exploration
- **Give measurable and continuous feedback** to the user, even when no bug is found during exploration

In addition to being **scalable**, **parallelizable**, and **push-button**.

### Techniques

PSym implements a collection of customizable techniques to accomplish the above objectives:

- **Never Repeat Executions**
   <p>
      Never repeat already-explored executions
   </p>

- **Tunable, On-Demand Symbolic Exploration**
   <p>
      Encode multiple values under different path conditions together<br/>
      Tune symbolic slice explored in each iteration
   </p>

- **Continuous Coverage Reporting** 
   <p>
      Continuous user feedback as Path Coverage
   </p>

- **Stateful Backtracking** 
   <p>
      Jump directly to any place without replay
   </p>

- **Never Repeat States**
   <p>
      Track distinct states to avoid state revisits
   </p>

- **Search Strategies** 
   <p>
      Coverage-guided A*, Random, Depth-first Search
   </p>
