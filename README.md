P is a language for asynchronous event-driven programming. P allows the programmer to specify the system as a collection of interacting state machines, which communicate with each other using events. P unifies modeling and programming into one activity for the programmer. Not only can a P program be compiled into executable code, but it can also be validated using systematic testing. P has been used to implement and validate the USB device driver stack that ships with Microsoft Windows 8 and Windows Phone. P is also suitable for the design and implementation of networked, embedded, and distributed systems.

Manual
=========

Check the [P manual](https://github.com/p-org/P/blob/master/Doc/Manual/pmanual.pdf).

Information for building P framework is available [here](https://github.com/p-org/P/wiki).

Demos
==========================================================
See [fun demo video](https://www.youtube.com/watch?v=R8ztpfMPs5c) using P to control a quadrocopter and make sense of the MavLink stream, all visualized in a live DGML diagram.

Application to Robotics
=========================================================
We built DRONA, a software framework for distributed mobile robotics systems. DRONA uses P language for implementing and model-checking the distributed robotics software stack (**[wiki](https://github.com/Drona-Org/Drona/wiki/Drona-Software-Stack)**). The C code generated from P compiler can be easily deployed on  Robot Operating System (ROS).
More details about the DRONA framework and simulation videos are available here:
**[https://drona-org.github.io/Drona/](https://drona-org.github.io/Drona/)**

We are working on using the DRONA framework for building real-world drone applications using **[PX4](https://github.com/PX4/Firmware)** and extending DRONA with runtime monitoring capabilities for reliability.
Details about these extensions will be posted soon.

Publications
==========================================================
- **[P: Safe asynchronous event-driven programming]()**.
Ankush Desai, Vivek Gupta, Ethan Jackson, Shaz Qadeer, Sriram Rajamani, and Damien Zufferey.
In Proceedings of ACM SIGPLAN Conference on Programming Language Design and Implementation (PLDI), 2013.

- **[Systematic testing of asynchronous reactive systems](http://dx.doi.org/10.1145/2786805.2786861)**.
Ankush Desai, Shaz Qadeer, and Sanjit A. Seshia.
In Proceedings of the 2015 10th Joint Meeting on Foundations of Software Engineering (ESEC/FSE 2015). 

- **[DRONA: A Framework for Safe Distributed Mobile Robotics](https://people.eecs.berkeley.edu/~ankush/Papers/drona.pdf)**.
Ankush Desai, Indranil Saha, Jianqiao Yang, Shaz Qadeer, and Sanjit A. Seshia.
In Proceedings of the 8th ACM/IEEE International Conference on Cyber-Physical Systems (ICCPS), 2017.

Blogs
============================================================
- **[P: A programming language designed for asynchrony, fault-tolerance and uncertainty](https://www.microsoft.com/en-us/research/blog/p-programming-language-asynchrony/)** by Shaz Qadeer.
- **[Building robust USB 3.0 support](https://blogs.msdn.microsoft.com/b8/2011/08/22/building-robust-usb-3-0-support/)**



News
============================================================
- **TechWorld[2016]**: **[Microsoft open-sources P language for IoT](http://www.techworld.com.au/article/608591/microsoft-open-sources-p-language-iot/)**
- **InfoQ**[2016]: **[Microsoft Open-Sources P Language for Safe Async Event-Driven Programming](https://www.infoq.com/news/2016/10/microsoft-p-language-opensourced)**
- **Reddit[2016]**: **[Microsoft opensources P language](https://www.reddit.com/r/programming/comments/56nbbx/microsoft_opensources_p_language/)**


