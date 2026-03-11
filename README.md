<div align="center">
  <img src="Icon/icon.png" width="20%">
  <h2>Formal Modeling and Analysis of Distributed Systems</h2>
</div>

[![NuGet](https://img.shields.io/nuget/v/p.svg)](https://www.nuget.org/packages/P/)
[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](https://raw.githubusercontent.com/p-org/P/master/LICENSE.txt)
![GitHub Action (CI on Windows)](https://github.com/p-org/P/workflows/CI%20on%20Windows/badge.svg)
![GitHub Action (CI on Ubuntu)](https://github.com/p-org/P/workflows/CI%20on%20Ubuntu/badge.svg)
![GitHub Action (CI on MacOS)](https://github.com/p-org/P/workflows/CI%20on%20MacOS/badge.svg)
[![Tutorials](https://github.com/p-org/P/actions/workflows/tutorials.yml/badge.svg)](https://github.com/p-org/P/actions/workflows/tutorials.yml)

---

P is a state machine based programming language for formally modeling and specifying complex distributed systems. P allows programmers to model their system design as a collection of communicating state machines and provides automated reasoning backends to check that the system satisfies the desired correctness specifications.

## What's New

- **[PeasyAI](https://p-org.github.io/P/getstarted/peasyai/)** — AI-powered code generation for P. Generate state machines, specifications, and test drivers from design documents. Integrates with **Cursor** and **Claude Code** via MCP with 27 tools, ensemble generation, auto-fix pipeline, and 1,200+ RAG examples.

- **[PObserve](https://p-org.github.io/P/advanced/pobserve/pobserve/)** — Runtime monitoring and conformance checking. Validate that your production system conforms to its formal P specifications by checking service logs against P monitors — bridging the gap between design-time verification and runtime behavior.

## The P Framework

| Component | Description |
|-----------|-------------|
| **P Language** | Model distributed systems as communicating state machines. Specify safety and liveness properties. |
| **[PeasyAI](https://p-org.github.io/P/getstarted/peasyai/)** | AI-powered code generation from design documents with auto-fix and human-in-the-loop support. |
| **P-Checker** | Systematically explore interleavings of messages and failures to find deep bugs. Additional backends: PEx, PVerifier. |
| **[PObserve](https://p-org.github.io/P/advanced/pobserve/pobserve/)** | Validate service logs against P specifications in testing and production. |

## Impact at AWS

Using P, developers model their system designs as communicating state machines — a mental model familiar to developers who build systems based on microservices and service-oriented architectures. Teams across AWS that build some of its flagship products — from storage (Amazon S3, EBS), to databases (Amazon DynamoDB, MemoryDB, Aurora), to compute (EC2, IoT) — have been using P to reason about the correctness of their system designs.

> 📄 [**Systems Correctness Practices at Amazon Web Services**](https://cacm.acm.org/practice/systems-correctness-practices-at-amazon-web-services/) — _Marc Brooker and Ankush Desai_, Communications of the ACM, 2025.

<div align="center">
      <a href="https://www.youtube.com/watch?v=FdXZXnkMDxs">
         <img src="https://img.youtube.com/vi/FdXZXnkMDxs/hqdefault.jpg" style="width:40%;">
      </a>
      <br/>
      <em><a href="https://youtu.be/FdXZXnkMDxs?si=iFqpl16ONKZuS4C0">(Re:Invent 2023) Gain confidence in system correctness & resilience with Formal Methods</a></em>
</div>

## Experience and Lessons Learned

P has helped developers in three critical ways:

1. **P as a Thinking Tool** — Writing formal specifications forces developers to think about their system design rigorously, bridging gaps in understanding. A large fraction of bugs can be eliminated in the process of writing specifications itself!

2. **P as a Bug Finder** — Model checking finds corner-case bugs in system design that are missed by stress and integration testing.

3. **P Boosts Developer Velocity** — After the initial overhead of creating formal models, future updates and feature additions can be rolled out faster as non-trivial changes are rigorously validated before implementation.

> ✨ **_Programming concurrent, distributed systems is fun but challenging, however, a pinch of programming language design with a dash of automated reasoning can go a long way in addressing the challenge and amplifying the fun!_** ✨

## Let the fun begin!

You can find most of the information about the P framework on: **[https://p-org.github.io/P/](https://p-org.github.io/P/)**

[What is P?](https://p-org.github.io/P/whatisP/) | [Getting Started](https://p-org.github.io/P/getstarted/install/) | [PeasyAI](https://p-org.github.io/P/getstarted/peasyai/) | [Tutorials](https://p-org.github.io/P/tutsoutline/) | [Case Studies](https://p-org.github.io/P/casestudies/) | [Publications](https://p-org.github.io/P/publications/)

If you have any questions, please feel free to create an [issue](https://github.com/p-org/P/issues), ask on [discussions](https://github.com/p-org/P/discussions), or [email us](mailto:ankushdesai@gmail.com).

> _P has always been a collaborative project between industry and academia (since 2013). The P team welcomes contributions and suggestions from all of you! See [CONTRIBUTING](CONTRIBUTING.md) for more information._
