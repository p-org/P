### [AWS] Amazon S3 Strong Consistency

In Dec 2020, Amazon S3 launched [Strong Consistency](https://aws.amazon.com/s3/consistency/) with guaranteed
[strong read-after-write consistency](https://aws.amazon.com/blogs/aws/amazon-s3-update-strong-read-after-write-consistency/).
The S3 team leveraged automated reasoning for ensuring the correctness of S3's Strong
Consistency design. Werner had a detailed blog post about the challenges involved.

!!! quote "Qoute from Werners blog: [Diving Deep on S3 Consistency](https://www.allthingsdistributed.com/2021/04/s3-strong-consistency.html)"
    Common testing techniques like unit testing and integration testing are valuable,
    necessary tools in any production system. But they aren’t enough when you need to build a
    system with such a high bar for correctness. We want a system that’s “provably correct”,
    not just “probably correct.” So, for strong consistency, we utilized a variety of
    techniques for ensuring that what we built is correct, and continues to be correct as the
    system evolves. We employed integration tests, deductive proofs of our proposed cache
    coherence algorithm, model checking to formalize our consistency design and to demonstrate
    its correctness, and we expanded on our model checking to examine actual runnable code.

P was used for creating formal models of all the core distributed protocols involved in
S3's strong consistency and checking that the system model satisfies the desired
correctness guarantees. Details about P and how it is being used by the S3 team can be
found in the [AWS Pi-Week Talk](https://pages.awscloud.com/pi-week-2021.html):
[**Use of Automated Reasoning for S3 Strong Consistency Launch**](https://www.twitch.tv/videos/962963706?t=0h15m15s).

### [AWS] Amazon IoT Devices: OTA Protocol

AWS FreeRTOS is a real-time operating system designed to run on IoT devices to enable them
to interact easily and reliably with AWS services. The Over the Air (OTA) update
functionality makes it possible to update a device with security fixes quickly and
reliably. The [OTA Library](https://freertos.org/ota/index.html), a part of the overall
OTA functionality that runs on the IoT devices, enables customers to learn of available
updates, download the updates, check their cryptographic signatures, and apply them. The
OTA system is a complex piece of software that performs firmware updates reliably and
securely --- keeping all devices in a consistent state --- in the presence of arbitrary
failures of devices and communication. The heart of the OTA system is an intricate
distributed protocol, the OTA protocol, that co-ordinates the execution of the different
agents involved.

P was used for creating formal models of the OTA protocol and checking its
correctness. During this process the team found 3 bugs in the model that pointed to
potential issues in the actual implementation itself.

Related Blog:
[**Using Formal Methods to validate OTA Protocol**](https://freertos.org/2020/12/using-formal-methods-to-validate-ota-protocol.html)

### [UC Berkeley] Programming Safe Robotics Systems

DRONA is a software framework for programming safe distributed mobile robotics systems.
DRONA uses P language for implementing and model-checking the correctness of robotics software stack. The
C code generated from P compiler can be deployed on Robot Operating System (ROS).
More details about the DRONA framework and simulation videos are available here:
**[https://drona-org.github.io/Drona/](https://drona-org.github.io/Drona/)**

See the [fun demo video](https://www.youtube.com/watch?v=R8ztpfMPs5c) using P to control a
quadrocopter and make sense of the MavLink stream, all visualized in a live DGML diagram.

### [UC Berkeley] Programming Secure Distributed Systems

Programming secure distributed systems that have a formal guarantee of no information
leakage is challenging. PSec framework extended the P language to enable programming secure
distributed systems. PSec leverages Intel SGX enclaves to ensure that the security guarantees
provided by the P language are enforced at runtime. By combining information flow control
with hardware enclaves, PSec prevents programmers from inadvertently leaking sensitive
information while sending data securely across machines. PSec was used to program several real-world examples,
including a One Time Passcode application and a Secure Electronic Voting System. Details
about the PSec framework can be found [here](https://github.com/ShivKushwah/PSec).

### [Microsoft] Windows USB 3.0 Device Drivers

Event-driven asynchronous programs typically have layers of design, where the higher
layers reason with how the various components (or machines) interact, and the protocol
they follow, and where as lower layers manage more data-intensive computations,
controlling local devices, etc. However, the programs often get written in traditional
languages that offer no mechanisms to capture these abstractions, and hence over time
leads to code where the individual layers are no longer discernible. High-level protocols,
though often first designed on paper using clean graphical state-machine abstractions,
eventually get lost in code, and hence verification tools for such programs face the
daunting task of extracting these models from the programs. The natural solution to the
above problem is to build a programming language for asynchronous event-driven programs
that preserves the protocol abstractions in code. Apart from the difficulty in designing
such a language, this task is plagued by the reluctance of programmers to adopt a new
language of programming and the discipline that it brings. However, this precise solution
was pioneered by the P programming framework, where, during the development of Windows 8,
the team building the USB driver stack used P for modeling, implementing, and
model-checking of the USB 3.0 device drivers
([paper](https://ankushdesai.github.io/assets/papers/p.pdf))

Related Blog:

- **[Building robust USB 3.0 support](https://blogs.msdn.microsoft.com/b8/2011/08/22/building-robust-usb-3-0-support/)**
- **[P: A programming language designed for asynchrony, fault-tolerance and uncertainty](https://www.microsoft.com/en-us/research/blog/p-programming-language-asynchrony/)**
