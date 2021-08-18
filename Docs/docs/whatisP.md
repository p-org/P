<style>
  .md-typeset h1,
  .md-content__button {
    display: none;
  }
</style>

![Placeholder](distsystem.png){ align=center }

Distributed systems are notoriously hard to get right (guaranteeing correctness) as the
programmer needs to reason about numerous control paths resulting from the myriad
interleaving of events (or messages) and failures. Unsurprisingly, programmers can easily
introduce subtle errors when designing these systems. In practice, it is extremely
difficult to test asynchronous systems, most control paths remain untested, and serious
bugs lie dormant for months or even years after deployment.

!!! quote ""  
  _The P programming framework takes several steps towards addressing these challenges. P is
  a unified framework for modeling, specifying, implementing, testing, and verifying complex
  distributed systems._

## P Framework

The P framework can be divided into three important parts:


![Placeholder](toolchain.png){ align=center width=90% }

### P Language

P provides a high-level state machine based programming language to model and specify
distributed systems. The syntactic sugar of state machines allows programmers to capture
their protocol logic as communicating state machines, which is how we normally think about
our complex system designs. P is more of a programming language rather than a mathematical
modelling language, making it easier for the programmers to both create models that closer
to the implementation and also maintain them as the system design evolves. P supports
specifying both safety and liveness specifications (global invariants) as monitor state
machines. The P module system enables programmers to model their system _modularly_ and
also use _compositional_ testing to scale the analysis to large distributed systems

<add details about the formal semantics and so on ... >
### Backend Analysis Engines
P supports explicit state model ...

Symbolic and Deductive verifier coming soon.

### Code Generation
P currently generates C# and C++ code. The generated C++ code has been used to program robotics systems and secured distributed systems.
P will also support Java backend and a the new version of the P compiler release will support it.

P also supports generating runtime monitors for the safety and liveness specifications that can be then used to check if the implementation conforms to the high-level P specifications.

