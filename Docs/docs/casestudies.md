## Case Studies

---

### :material-aws:{ .lg } Systems Correctness Practices at AWS

Teams across AWS that build some of its flagship products — from storage (Amazon S3, EBS), to databases (Amazon DynamoDB, MemoryDB, Aurora), to compute (EC2, IoT) — have been using P to reason about the correctness of their system designs. A 2025 Communications of the ACM article surveys the full portfolio of formal methods used across AWS, with P playing a central role.

!!! quote "From [Systems Correctness Practices at Amazon Web Services](https://cacm.acm.org/practice/systems-correctness-practices-at-amazon-web-services/) (CACM, 2025)"
    P is a state-machine-based language for modeling and analysis of distributed systems. Using P, developers model their system designs as communicating state machines, a mental model familiar to Amazon's developer population — most of whom develop systems based on microservices and service-oriented architectures. P has been developed at AWS since 2019 and is maintained as a strategic open source project.

The article also describes how **PObserve** bridges the gap between design-time verification and production, by validating structured service logs against P specifications post hoc — making the investment in formal specification valuable throughout the entire development lifecycle.

---

### :material-aws:{ .lg } Amazon S3 Strong Consistency

In Dec 2020, Amazon S3 launched [Strong Consistency](https://aws.amazon.com/s3/consistency/) with guaranteed
[strong read-after-write consistency](https://aws.amazon.com/blogs/aws/amazon-s3-update-strong-read-after-write-consistency/).
The S3 team leveraged automated reasoning for ensuring the correctness of S3's Strong
Consistency design.

!!! quote "From Werner Vogels' blog: [Diving Deep on S3 Consistency](https://www.allthingsdistributed.com/2021/04/s3-strong-consistency.html)"
    Common testing techniques like unit testing and integration testing are valuable,
    necessary tools in any production system. But they aren't enough when you need to build a
    system with such a high bar for correctness. We want a system that's "provably correct",
    not just "probably correct." So, for strong consistency, we utilized a variety of
    techniques for ensuring that what we built is correct, and continues to be correct as the
    system evolves.

P was used for creating formal models of all the core distributed protocols involved in
S3's strong consistency and checking that the system model satisfies the desired
correctness guarantees. Details about P and how it is being used by the S3 team can be
found in the [AWS Pi-Week Talk](https://pages.awscloud.com/pi-week-2021.html):
[**Amazon S3 Strong Consistency**](https://youtu.be/B0yXz6EeCaA?list=PL2yQDdvlhXf8vAnQB10dCPIeWUKdHUgOP).

---

### :material-devices:{ .lg } Amazon IoT Devices: OTA Protocol

AWS FreeRTOS is a real-time operating system designed to run on IoT devices to enable them
to interact easily and reliably with AWS services. The Over the Air (OTA) update
functionality makes it possible to update a device with security fixes quickly and
reliably. The [OTA Library](https://freertos.org/ota/index.html) enables customers to learn of available
updates, download the updates, check their cryptographic signatures, and apply them.

P was used for creating formal models of the OTA protocol and checking its
correctness. During this process the team found **3 bugs** in the model that pointed to
potential issues in the actual implementation itself.

!!! info "Related Blog"
    [**Using Formal Methods to validate OTA Protocol**](https://freertos.org/2020/12/using-formal-methods-to-validate-ota-protocol.html)

---

### :material-robot:{ .lg } Programming Safe Robotics Systems (UC Berkeley)

DRONA is a software framework for programming safe distributed mobile robotics systems.
DRONA uses P language for implementing and model-checking the correctness of robotics software stack. The
C code generated from P compiler can be deployed on Robot Operating System (ROS).

:material-play-circle: See the [fun demo video](https://www.youtube.com/watch?v=R8ztpfMPs5c) using P to control a quadrocopter and make sense of the MavLink stream, all visualized in a live DGML diagram.

!!! info "More Details"
    **[https://drona-org.github.io/Drona/](https://drona-org.github.io/Drona/)**

---

### :material-shield-lock:{ .lg } Programming Secure Distributed Systems (UC Berkeley)

Programming secure distributed systems that have a formal guarantee of no information
leakage is challenging. PSec framework extended the P language to enable programming secure
distributed systems. PSec leverages Intel SGX enclaves to ensure that the security guarantees
provided by the P language are enforced at runtime. By combining information flow control
with hardware enclaves, PSec prevents programmers from inadvertently leaking sensitive
information while sending data securely across machines.

PSec was used to program several real-world examples,
including a One Time Passcode application and a Secure Electronic Voting System.

!!! info "More Details"
    **[https://github.com/ShivKushwah/PSec](https://github.com/ShivKushwah/PSec)**

---

### :material-microsoft-windows:{ .lg } Windows USB 3.0 Device Drivers (Microsoft)

P was pioneered during the development of Windows 8, where the team building the USB driver stack used P for modeling, implementing, and model-checking of the USB 3.0 device drivers ([paper](https://ankushdesai.github.io/assets/papers/p.pdf)). High-level protocols, though often first designed on paper using clean graphical state-machine abstractions, eventually get lost in code. P preserves these protocol abstractions in code, making verification tractable.

!!! info "Related Blogs"
    - [**Building robust USB 3.0 support**](https://blogs.msdn.microsoft.com/b8/2011/08/22/building-robust-usb-3-0-support/)
    - [**P: A programming language designed for asynchrony, fault-tolerance and uncertainty**](https://www.microsoft.com/en-us/research/blog/p-programming-language-asynchrony/)
