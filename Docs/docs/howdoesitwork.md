Asynchronous event-driven systems[^eds] are ubiquitous across domains such as device drivers,
distributed systems, and robotics. These systems are notoriously hard to get right as the
programmer needs to reason about numerous control paths resulting from the myriad interleaving of events (or messages) and failures. 
Unsurprisingly, it is easy to introduce subtle errors while attempting to fill in gaps between high-level system specifications and their
concrete implementations. In practice, it is extremely difficult to test asynchronous systems, most control paths remain untested, and serious bugs lie dormant for months or even years after deployment.



!!! quote ""
    _The P programming framework takes several steps towards addressing these challenges_. 



[^eds]: Event-driven asynchronous systems are systems that are built on top of the actor or communicating
state-machines model of computation where processes execute concurrently and communicate
with each other by sending message asynchronously.