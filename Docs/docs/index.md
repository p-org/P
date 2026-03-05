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

P is a state machine based programming language for formally modeling and specifying complex distributed systems. P allows programmers to model their system design as a collection of communicating state machines and provides automated reasoning backends to check that the system satisfies the desired correctness specifications.

![P framework toolchain overview](toolchain.jpg){ align=center }

---

## :material-new-box:{ .lg } What's New

<div class="grid cards" markdown>

-   :material-robot-outline:{ .lg .middle } **PeasyAI — AI-Assisted P Development**

    ---

    Generate P state machines, specifications, and test drivers from design documents using AI. Integrates with **Cursor** and **Claude Code** via MCP with 27 tools, ensemble generation, auto-fix pipeline, and 1,200+ RAG examples.

    [:octicons-arrow-right-24: Get started with PeasyAI](getstarted/peasyai.md)

-   :material-monitor-eye:{ .lg .middle } **PObserve — Runtime Monitoring**

    ---

    Validate that your **production system conforms to its formal P specifications** by checking service logs against P monitors — bridging the gap between design-time verification and runtime behavior, without additional instrumentation.

    [:octicons-arrow-right-24: Learn about PObserve](advanced/pobserve/pobserve.md)

</div>

---

## :material-shield-check:{ .lg } The P Framework

<div class="grid cards" markdown>

-   :material-file-document-edit-outline:{ .lg .middle } **P Language**

    ---

    Model distributed systems as communicating state machines. Specify safety and liveness properties. A programming language — not a mathematical notation.

-   :material-robot-outline:{ .lg .middle } **PeasyAI**

    ---

    AI-powered code generation from design documents. Generates types, machines, specs, and tests with auto-fix and human-in-the-loop support.

-   :material-shield-check-outline:{ .lg .middle } **P-Checker**

    ---

    Systematically explore interleavings of messages and failures to find deep bugs. Reproducible error traces for debugging. Additional backends: PEx, PVerifier.

-   :material-monitor-eye:{ .lg .middle } **PObserve**

    ---

    Validate service logs against P specifications in testing and production. Ensure implementation conforms to the verified design.

</div>

[:octicons-arrow-right-24: Learn more about the P framework](whatisP.md)

---

## :material-aws:{ .lg } Impact at AWS

Using P, developers model their system designs as communicating state machines — a mental model familiar to developers who build systems based on microservices and service-oriented architectures. Teams across AWS that build some of its flagship products — from storage (Amazon S3, EBS), to databases (Amazon DynamoDB, MemoryDB, Aurora), to compute (EC2, IoT) — have been using P to reason about the correctness of their system designs.

!!! abstract "Further Reading"
    :material-file-document: [**Systems Correctness Practices at Amazon Web Services**](https://cacm.acm.org/practice/systems-correctness-practices-at-amazon-web-services/) — _Marc Brooker and Ankush Desai_, Communications of the ACM, 2025.

??? question "Why formal methods? How is AWS using P?"
    The following re:Invent 2023 talk provides an overview of P and its impact inside AWS:

    <div align="center">
          <a href="https://www.youtube.com/watch?v=FdXZXnkMDxs">
             <img src="https://img.youtube.com/vi/FdXZXnkMDxs/hqdefault.jpg" style="width:40%;">
          </a>
    </div>

    [(Re:Invent 2023) Gain confidence in system correctness & resilience with Formal Methods](https://youtu.be/FdXZXnkMDxs?si=iFqpl16ONKZuS4C0)

---

## :material-lightbulb-on:{ .lg } Experience and Lessons Learned

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
