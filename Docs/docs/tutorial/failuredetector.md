
Energized with the Coffee :coffee:, lets get back to modeling distributed systems. After the two phase commit protocol, the next protocol that we will jump to is a simple broadcast-based failure detector!

By this point in the tutorial, we have gotten familiar with the P language and most of its features. So, working through this example should be super fast! 

??? note "How to use this example"

    We assume that you have cloned the P repository locally.
    ```shell 
    git clone https://github.com/p-org/P.git
    ```

    The recommended way to work through this example is to open the [P\Tutorial](https://github.com/p-org/P/tree/master/Tutorial) folder in IntelliJ side-by-side a browser using which you can simulatenously read the description for each example and browser the P program in IntelliJ. 

**System:** We consider a simple broadcast based failure detector that basically broadcasts ping messages to all nodes in the system and uses a timer to wait for a pong response from all nodes. If a certain node does not respond with a pong message after multiple attempts (either because of network failure or node failure), the failure detector marks the node as down and notifies the clients about the nodes that are potentially down. We use this example to show how to model network message loss in P and discuss how to model other types of network behaviours.

![Placeholder](failuredetector.png){ align=center }

**Correctness Specification:** We would like to check using a liveness specification that if the failure injecter shutsdown a particular node then the failure detector always eventually detects that node has failed and notifies client.

!!! summary "Summary"
    In this example, we demonstrate how to use data nondeterminism to model message loss and unreliable sends. We also discuss how to model other types of network nondeterminism. Finally, we give another example of a liveness specification that the failure detector must satisfy.


