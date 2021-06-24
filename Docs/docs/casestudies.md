## Programming Safe Robotics Systems using P

We built DRONA, a software framework for distributed mobile robotics systems. DRONA uses P language for implementing and model-checking the distributed robotics software stack (**[wiki](https://github.com/Drona-Org/Drona/wiki/Drona-Software-Stack)**). The C code generated from P compiler can be easily deployed on  Robot Operating System (ROS).
More details about the DRONA framework and simulation videos are available here:
**[https://drona-org.github.io/Drona/](https://drona-org.github.io/Drona/)**

See [fun demo video](https://www.youtube.com/watch?v=R8ztpfMPs5c) using P to control a quadrocopter and make sense of the MavLink stream, all visualized in a live DGML diagram.

## Programming Secure Distributed Systems using P

Programming secure distributed systems that have a formal guarantee of no information leakage is challenging.
We extended P to the PSec language to enable programming secure distributed systems.
We leverage Intel SGX enclaves to ensure that the security guarantees provided by the P language are enforced
at runtime. By combining information flow control with hardware enclaves, we prevent P programmers
from inadvertently leaking sensitive information while sending data securely across machines. We
formally proved the security properties of the extended P language and used it to program several real-world
examples, including a One Time Passcode application and a Secure Electronic Voting System.
Details about the PSec framework can be found [here](https://github.com/ShivKushwah/PSec).

=== "Tab1"
    xx
    asd

=== "Tab2"
    asds
    asda


!!! bug

    There is an error here