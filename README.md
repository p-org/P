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

## Impact

P enables developers to model system designs as communicating state machines—a natural fit for microservices and service-oriented architectures. Teams across AWS building flagship products—from storage (S3, EBS), to databases (DynamoDB, MemoryDB, Aurora), to compute (EC2, IoT)—use P to reason about the correctness of their designs. P has helped these teams eliminate several critical bugs early in the development process.

<div align="center">
      <a href="https://www.youtube.com/watch?v=FdXZXnkMDxs">
         <img src="https://img.youtube.com/vi/FdXZXnkMDxs/hqdefault.jpg" style="width:40%;">
      </a>
      <br/>
      <em><a href="https://youtu.be/FdXZXnkMDxs?si=iFqpl16ONKZuS4C0">(Re:Invent 2023) Gain confidence in system correctness & resilience with Formal Methods</a></em>
</div>

> 📄 [**Systems Correctness Practices at Amazon Web Services**](https://cacm.acm.org/practice/systems-correctness-practices-at-amazon-web-services/) — _Marc Brooker and Ankush Desai_, Communications of the ACM, 2025.

### Why Teams Choose P

| Benefit | Description |
|---------|-------------|
| **Thinking Tool** | Writing specifications forces rigorous design thinking—many bugs are caught before any code runs. |
| **Bug Finder** | Model checking uncovers corner-case bugs that stress testing and integration testing miss. |
| **Faster Iteration** | After initial modeling, changes can be validated quickly before implementation. |

## What's New

### PeasyAI — AI-Powered Code Generation

Generate P state machines, specifications, and test drivers directly from design documents.

- Integrates with **Cursor** and **Claude Code** via MCP
- 27 specialized tools for P development
- Ensemble generation with auto-fix pipeline
- 1,200+ RAG examples for context-aware generation

👉 [Get started with PeasyAI](https://p-org.github.io/P/getstarted/peasyai/)

### PObserve — Runtime Monitoring

Validate that production systems conform to their formal P specifications.

- Check service logs against P monitors
- Bridge design-time verification with runtime behavior
- Works in both testing and production environments

👉 [Learn about PObserve](https://p-org.github.io/P/advanced/pobserve/pobserve/)

## The P Framework

| Component | Description |
|-----------|-------------|
| **P Language** | Model distributed systems as communicating state machines. Specify safety and liveness properties. |
| **P Checker** | Systematically explore message interleavings and failures to find deep bugs. Additional backends: PEx, PVerifier. |
| **[PeasyAI](https://p-org.github.io/P/getstarted/peasyai/)** | AI-powered code generation with auto-fix and human-in-the-loop support. |
| **[PObserve](https://p-org.github.io/P/advanced/pobserve/pobserve/)** | Validate service logs against P specifications. |

## Let the fun begin!

You can find most of the information about the P framework on: **[https://p-org.github.io/P/](https://p-org.github.io/P/)**

[What is P?](https://p-org.github.io/P/whatisP/) | [Getting Started](https://p-org.github.io/P/getstarted/install/) | [PeasyAI](https://p-org.github.io/P/getstarted/peasyai/) | [Tutorials](https://p-org.github.io/P/tutsoutline/) | [Case Studies](https://p-org.github.io/P/casestudies/) | [Publications](https://p-org.github.io/P/publications/)

If you have any questions, please feel free to create an [issue](https://github.com/p-org/P/issues), ask on [discussions](https://github.com/p-org/P/discussions), or [email us](mailto:ankushdesai@gmail.com).

> _P has always been a collaborative project between industry and academia (since 2013). The P team welcomes contributions and suggestions from all of you! See [CONTRIBUTING](CONTRIBUTING.md) for more information._
