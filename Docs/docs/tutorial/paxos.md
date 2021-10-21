How can we finish our tutorials on modeling distributed systems without giving tribute to the Paxos protocol :pray: (and our inspiration [Leslie Lamport](http://www.lamport.org/)). Let's end the tutorial with a simplified **[single decree paxos](https://mwhittaker.github.io/blog/single_decree_paxos/)**.

In this example, we present a simplified model of the single decree paxos. We say simplified because general paxos is resilient against arbitrary network (lossy, duplicate, re-order, and delay), in our case we only model message loss and delay, and check correctness of paxos in the presence of such a network. This is a fun exercise, we encourage you to play around and create variants of paxos!

!!! summary "Summary"
    In this example, we present a simplified model of the single decree paxos. (Todo: add details about the properties checked)