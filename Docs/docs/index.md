<style>
  .md-typeset h1,
  .md-content__button {
    display: none;
  }
</style>

<div align="center">
  <img src="icon.png" width="20%">
  <h2>Formal Modeling and Analysis of Distributed Systems</h2>
</div>

[![NuGet](https://img.shields.io/nuget/v/p.svg)](https://www.nuget.org/packages/P/)
[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](https://raw.githubusercontent.com/p-org/P/master/LICENSE.txt)
![GitHub Action (CI on Windows)](https://github.com/p-org/P/workflows/CI%20on%20Windows/badge.svg)
![GitHub Action (CI on Ubuntu)](https://github.com/p-org/P/workflows/CI%20on%20Ubuntu/badge.svg)
![GitHub Action (CI on MacOS)](https://github.com/p-org/P/workflows/CI%20on%20MacOS/badge.svg)

---

!!! danger "The Challenge"
    Distributed systems are notoriously hard to get right. Programming these systems is challenging because of the need to reason about correctness in the presence of myriad possible interleaving of messages and failures. Unsurprisingly, it is common for service teams to uncover correctness bugs after deployment. **Formal methods can play an important role in addressing this challenge!**

!!! abstract "P Overview"
    P is a state machine based programming language for formally modeling and specifying complex distributed systems. P allows programmers to model their system design as a collection of communicating state machines. P supports several backend analysis engines (based on automated reasoning techniques like model checking and symbolic execution) to check that the distributed system modeled in P satisfies the desired correctness specifications.

??? question "Why formal methods? How is AWS using P?"
    The following re:Invent 2023 talk answers this question, provides an overview of P, and its impact inside AWS:

    <div align="center">
          <a href="https://www.youtube.com/watch?v=FdXZXnkMDxs">
             <img src="https://img.youtube.com/vi/FdXZXnkMDxs/hqdefault.jpg" style="width:40%;">
          </a>
    </div>

    [(Re:Invent 2023) Gain confidence in system correctness & resilience with Formal Methods](https://youtu.be/FdXZXnkMDxs?si=iFqpl16ONKZuS4C0)

---

## Impact at AWS

Using P, developers model their system designs as communicating state machines — a mental model familiar to developers who build systems based on microservices and service-oriented architectures (SOAs). Teams across AWS that build some of its flagship products — from storage (Amazon S3, EBS), to databases (Amazon DynamoDB, MemoryDB, Aurora), to compute (EC2, IoT) — have been using P to reason about the correctness of their system designs.

For example, Amazon S3 used P to formally reason about the core distributed protocols involved in its strong consistency launch. P is also being used for programming safe robotics systems in Academia and was first used to implement and validate the USB device driver stack that ships with Microsoft Windows 8 and Windows Phone.

!!! abstract "Further Reading"
    :material-file-document: [**Systems Correctness Practices at Amazon Web Services**](https://cacm.acm.org/practice/systems-correctness-practices-at-amazon-web-services/) — _Marc Brooker and Ankush Desai_, Communications of the ACM, 2025.

## Experience and Lessons Learned

In our experience of using P inside AWS, Academia, and Microsoft, we have observed that P has helped developers in three critical ways:

<div class="grid cards" markdown>

-   :material-head-lightbulb:{ .lg .middle } **P as a Thinking Tool**

    ---

    Writing formal specifications forces developers to think about their system design rigorously, bridging gaps in understanding. A large fraction of bugs can be eliminated in the process of writing specifications itself!

-   :material-bug:{ .lg .middle } **P as a Bug Finder**

    ---

    Model checking finds corner-case bugs in system design that are missed by stress and integration testing.

-   :material-rocket-launch:{ .lg .middle } **P Boosts Developer Velocity**

    ---

    After the initial overhead of creating formal models, future updates and feature additions can be rolled out faster as non-trivial changes are rigorously validated before implementation.

</div>

!!! quote ""
    :sparkles: **_Programming concurrent, distributed systems is fun but challenging, however, a pinch of programming language design with a dash of automated reasoning can go a long way in addressing the challenge and amplifying the fun!._** :sparkles:

---

## Let the fun begin!

<div class="grid cards" markdown>

-   :material-help-circle-outline:{ .lg .middle } **[What is P?](whatisP.md)**

    ---

    Learn about the P framework and its components

-   :material-download:{ .lg .middle } **[Getting Started](getstarted/install.md)**

    ---

    Install P and start building your first program

-   :material-school:{ .lg .middle } **[Tutorials](tutsoutline.md)**

    ---

    Work through hands-on examples step by step

-   :material-flask:{ .lg .middle } **[Case Studies](casestudies.md)**

    ---

    See how P is used at AWS, Microsoft, and UC Berkeley

</div>

If you have any questions, please feel free to create an
[issue](https://github.com/p-org/P/issues), ask on
[discussions](https://github.com/p-org/P/discussions), or
[email us](mailto:ankushdesai@gmail.com).

!!! info "Contributions"
    _P has always been a collaborative project between industry and academia (since 2013)
    :drum:. The P team welcomes contributions and suggestions from all of you!! :punch:._
