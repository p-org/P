Energized with the Coffee :coffee:, lets get back to distributed systems. After the two phase commit protocol, the next protocol that we will jump to is a simple broadcast-based failure detector!

By this point in the tutorial, we have gotten familiar with the P language and most of its features. So, working through this example should be super fast! We use a broadcast based failure detector to show how to model network message loss in P. The failure detector basically broadcasts ping messages to all nodes in the system and uses a timer to wait for a pong response from all nodes. If certain node does not respond with a pong after multiple attempts, the failure detector marks the node as down and notifies the clients that have registered with the failure detector. We check using a liveness specification that if the failure injecter shutsdown a particular node then the failure detector always eventually detects that node as failed and notifies client.

!!! summary "Summary"
    In this example, we demonstrate how to use data nondeterminism to model message loss and unreliable sends. We also discuss how to model other types of network nondeterminism. Finally, we give another example of a liveness specification that the failure detector must satisfy.


![Placeholder](failuredetector.png){ align=center }