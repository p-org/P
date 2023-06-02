<div align="center">
  <img src="Icon/icon.png" width="20%">
  <h2>Formal Modeling and Analysis of Distributed (Event-Driven) Systems </h2>
</div>

[![NuGet](https://img.shields.io/nuget/v/p.svg)](https://www.nuget.org/packages/P/)
[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](https://raw.githubusercontent.com/p-org/P/master/LICENSE.txt)
![GitHub Action (CI on Windows)](https://github.com/p-org/P/workflows/CI%20on%20Windows/badge.svg)
![GitHub Action (CI on Ubuntu)](https://github.com/p-org/P/workflows/CI%20on%20Ubuntu/badge.svg)
![GitHub Action (CI on MacOS)](https://github.com/p-org/P/workflows/CI%20on%20MacOS/badge.svg)
[![Tutorials](https://github.com/p-org/P/actions/workflows/tutorials.yml/badge.svg)](https://github.com/p-org/P/actions/workflows/tutorials.yml)

**Challenge**: 
Distributed systems are notoriously hard to get right. Programming these systems is challenging because of the need to reason about correctness in the presence of myriad possible interleaving of messages and failures. Unsurprisingly, it is common for service teams to uncover correctness bugs after deployment. Formal methods can play an important role in addressing this challenge!


**P Overview:** 
P is a state machine based programming language for formally modeling and specifying complex
distributed systems. P allows programmers to model their system design as a collection of
communicating state machines. P supports several backend analysis engines
(based on automated reasoning techniques like model
checking and symbolic execution) to check that the distributed system modeled in P
satisfy the desired correctness specifications.

> The following talk provides an overview of P, its impact in Academia and Industry, and answers the main question that gets asked most of the times by service teams and developers: **"why do formal modeling?"**:
[(P @ StrangeLoop) Formal Modeling and Analysis of Distributed Systems (Finding Critical Bugs Early!!)](https://www.youtube.com/watch?v=5YjsSDDWFDY)

<div align="center">
      <a href="https://www.youtube.com/watch?v=5YjsSDDWFDY">
         <img src="https://img.youtube.com/vi/5YjsSDDWFDY/0.jpg" style="width:40%;">
      </a>
</div>

**Impact**: P is currently being used extensively inside Amazon (AWS) for analysis of complex distributed systems. For example, Amazon S3 used P to formally reason about the core distributed protocols involved in its strong consistency launch. Teams across AWS are now using P for thinking and reasoning about their systems formally. P is also being used for programming safe robotics systems in Academia. P was first used to implement and validate the USB device driver stack that ships with Microsoft Windows 8 and Windows Phone.

**Experience and lessons learned**:
In our experience of using P inside AWS, Academia, and Microsoft. We have observed that P has helped developers in three critical ways: (1) **P as a thinking tool**: Writing formal specifications in P forces developers to think about their system design rigorously, and in turn helped in bridging gaps in their understanding of the system. A large fraction of the bugs can be eliminated in the process of writing specifications itself! (2) **P as a bug finder**: Model checking helped find corner case bugs in system design that were missed by stress and integration testing. (3) **P helped boost developer velocity**: After the initial overhead of creating the formal models, future updates and feature additions could be rolled out faster as these non-trivial changes are rigorously validated before implementation.

> :sparkles: **_Programming concurrent, distributed systems is fun but challenging, however, a pinch of programming language design with a dash of automated reasoning can go a long way in addressing the challenge and amplify the fun!._** :sparkles:



## Let the fun begin!

You can find most of the information about the P framework on: **[http://p-org.github.io/P/](http://p-org.github.io/P/)**.

[What is P?](http://p-org.github.io/P/whatisP/), [Getting Started](http://p-org.github.io/P/getstarted/install/), [Tutorials](http://p-org.github.io/P/tutsoutline/), [Case Studies](http://p-org.github.io/P/casestudies/) and related [Research Publications](http://p-org.github.io/P/publications/).
If you have any further questions, please feel free to create an [issue](https://github.com/p-org/P/issues), ask on
[discussions](https://github.com/p-org/P/discussions), or [email us](mailto:ankushdesai@gmail.com)


> _P has always been a collaborative project between industry and academia (since 2013) :drum:. The P team welcomes contributions and suggestions from all of you!! :punch:. See [CONTRIBUTING](CONTRIBUTING.md) for more information._







