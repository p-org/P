[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](https://raw.githubusercontent.com/p-org/P/master/LICENSE.txt)
![GitHub Action (CI on Windows)](https://github.com/p-org/P/workflows/CI%20on%20Windows/badge.svg)
![GitHub Action (CI on Ubuntu)](https://github.com/p-org/P/workflows/CI%20on%20Ubuntu/badge.svg)
![GitHub Action (CI on MacOS)](https://github.com/p-org/P/workflows/CI%20on%20MacOS/badge.svg)

P is a state machine based programming language for modeling and specifying complex distributed systems. P allows the programmer to specify the system as a collection of interacting state machines, which communicate with each other using events. P unifies modeling, programming, and testing into one activity for the programmer. Not only can a P program be compiled into executable code, but it can also be systematically tested using Model Checking. 
P is currently being used extensively inside Amazon (AWS) for model checking complex distributed systems.
P has been used to implement and validate the USB device driver stack that ships with Microsoft Windows 8 and Windows Phone.
We have also used P for programming safe robotics systems.

Selected Publications
==========================================================
- **[Compositional Programming and Testing of Dynamic Distributed Systems](https://ankushdesai.github.io/assets/papers/modp.pdf)**.
Ankush Desai, Amar Phanishayee, Shaz Qadeer, and Sanjit Seshia.
International Conference on Object-Oriented Programming, Systems, Languages, and Applications (OOPSLA), 2018.

- **[DRONA: A Framework for Safe Distributed Mobile Robotics](https://ankushdesai.github.io/assets/papers/drona.pdf)**.
Ankush Desai, Indranil Saha, Jianqiao Yang, Shaz Qadeer, and Sanjit A. Seshia.
In Proceedings of the 8th ACM/IEEE International Conference on Cyber-Physical Systems (ICCPS), 2017.

- **[Systematic Testing of Asynchronous Reactive Systems](https://ankushdesai.github.io/assets/papers/fse-desai.pdf)**.
Ankush Desai, Shaz Qadeer, and Sanjit A. Seshia.
In Proceedings of the 2015 10th Joint Meeting on Foundations of Software Engineering (ESEC/FSE 2015). 

- **[P: Safe asynchronous event-driven programming](https://ankushdesai.github.io/assets/papers/p.pdf)**.
Ankush Desai, Vivek Gupta, Ethan Jackson, Shaz Qadeer, Sriram Rajamani, and Damien Zufferey.
In Proceedings of ACM SIGPLAN Conference on Programming Language Design and Implementation (PLDI), 2013.

Manual
=========

Check the [P Wiki](https://github.com/p-org/P/wiki/Introduction-to-P-language).

Information for building P framework is available [here](https://github.com/p-org/P/wiki).

Programming Safe Robotics Systems using P
=========================================================
We built DRONA, a software framework for distributed mobile robotics systems. DRONA uses P language for implementing and model-checking the distributed robotics software stack (**[wiki](https://github.com/Drona-Org/Drona/wiki/Drona-Software-Stack)**). The C code generated from P compiler can be easily deployed on  Robot Operating System (ROS).
More details about the DRONA framework and simulation videos are available here:
**[https://drona-org.github.io/Drona/](https://drona-org.github.io/Drona/)**

See [fun demo video](https://www.youtube.com/watch?v=R8ztpfMPs5c) using P to control a quadrocopter and make sense of the MavLink stream, all visualized in a live DGML diagram.

Programming Secure Distributed Systems using P
========================================================
Programming secure distributed systems that have a formal guarantee of no information leakage is challenging.
We extended P to the PSec language to enable programming secure distributed systems.
We leverage Intel SGX enclaves to ensure that the security guarantees provided by the P language are enforced
at runtime. By combining information flow control with hardware enclaves, we prevent P programmers
from inadvertently leaking sensitive information while sending data securely across machines. We
formally proved the security properties of the extended P language and used it to program several real-world
examples, including a One Time Passcode application and a Secure Electronic Voting System.
Details about the PSec framework can be found [here](https://github.com/ShivKushwah/PSec).

Blogs
============================================================
- **[P: A programming language designed for asynchrony, fault-tolerance and uncertainty](https://www.microsoft.com/en-us/research/blog/p-programming-language-asynchrony/)**
- **[Building robust USB 3.0 support](https://blogs.msdn.microsoft.com/b8/2011/08/22/building-robust-usb-3-0-support/)**

- **[Using Formal Methods to Validate OTA Protocol](https://freertos.org/2020/12/using-formal-methods-to-validate-ota-protocol.html)**

News
============================================================
- **TechWorld[2016]**: **[Microsoft open-sources P language for IoT](http://www.techworld.com.au/article/608591/microsoft-open-sources-p-language-iot/)**
- **InfoQ**[2016]: **[Microsoft Open-Sources P Language for Safe Async Event-Driven Programming](https://www.infoq.com/news/2016/10/microsoft-p-language-opensourced)**
- **Reddit[2016]**: **[Microsoft opensources P language](https://www.reddit.com/r/programming/comments/56nbbx/microsoft_opensources_p_language/)**


