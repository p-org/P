### Amazon S3 Strong Consistency

### Windows USB 3.0 Device Drivers
Event-driven asynchronous programs typically have layers of design, where the higher layers reason with how the various components (or machines) interact, and the protocol they follow, and where as lower layers manage more data-intensive computations, controlling local devices, etc. However, the programs often get written in traditional languages that offer no mechanisms to capture these abstractions, and hence over time leads to code where the individual layers are no longer discernible. High-level protocols, though often first designed on paper using clean graphical state-machine abstractions, eventually get lost in code, and hence verification tools for such programs face the daunting task of extracting these models from the programs.
The natural solution to the above problem is to build a programming language for asynchronous event-driven programs that preserves the protocol abstractions in code. Apart from the difficulty in designing such a language, this task is plagued by the reluctance of programmers to adopt a new language of programming and the discipline that it brings. However, this precise solution was pioneered by the P programming framework, where, during the development of Windows 8, the team building the USB driver stack used P for modeling, implementing, and model-checking of the USB 3.0 device drivers ([paper](https://ankushdesai.github.io/assets/papers/p.pdf))

### Programming Safe Robotics Systems

We built DRONA, a software framework for distributed mobile robotics systems. 
DRONA uses P language for implementing and model-checking the distributed robotics software stack. 
The C code generated from P compiler can be easily deployed on  Robot Operating System (ROS).
More details about the DRONA framework and simulation videos are available here:
**[https://drona-org.github.io/Drona/](https://drona-org.github.io/Drona/)**
See [fun demo video](https://www.youtube.com/watch?v=R8ztpfMPs5c) using P to control a quadrocopter and make sense of the MavLink stream, all visualized in a live DGML diagram.

### Programming Secure Distributed Systems

Programming secure distributed systems that have a formal guarantee of no information leakage is challenging.
We extended P to the PSec language to enable programming secure distributed systems.
We leverage Intel SGX enclaves to ensure that the security guarantees provided by the P language are enforced
at runtime. By combining information flow control with hardware enclaves, we prevent P programmers
from inadvertently leaking sensitive information while sending data securely across machines. We
formally proved the security properties of the extended P language and used it to program several real-world
examples, including a One Time Passcode application and a Secure Electronic Voting System.
Details about the PSec framework can be found [here](https://github.com/ShivKushwah/PSec).
