Wow, we have reached the middle of our tutorials :yawning_face:, its time to take a break and have an espresso coffee! :coffee:
In the next example, instead of modeling a distributed protocol, we consider the fun example of modeling an espresso machine and see how we can use the P state machine to model a reactive system that must respond correctly to various user inputs.

P has been used in the past to implement device drivers and robotics systems ([case studies](casestudies.md)). One of the many challenges in implementing these systems is that they are reactive system and hence, must handle various streams of events (inputs) appropriately depending on its current mode of operation.
In this example, we consider the example of an Espresso coffee machine where the user interacts with the coffee machine through its control panel. The control panel must correctly interprets inputs from the user and sends commands to the coffee maker. We use this example to demonstrate how using P state machine, one can capture the required reactive behavior of a coffee maker and define how it must handle different user inputs.

!!! summary "Summary"
    This is a just for fun example to demonstrate how to model a reactive system as a P state machine. We also show how using P monitors we can check that the system moves through the correct modes of operation.