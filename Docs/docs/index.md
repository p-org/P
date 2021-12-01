<style>
  .md-typeset h1,
  .md-content__button {
    display: none;
  }
</style>

<div align="center">
  <img src="icon.png" width="20%">
  <h2>Modular and Safe Programming for Distributed Systems</h2>
</div>

[![NuGet](https://img.shields.io/nuget/v/p.svg)](https://www.nuget.org/packages/P/)
[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](https://raw.githubusercontent.com/p-org/P/master/LICENSE.txt)
![GitHub Action (CI on Windows)](https://github.com/p-org/P/workflows/CI%20on%20Windows/badge.svg)
![GitHub Action (CI on Ubuntu)](https://github.com/p-org/P/workflows/CI%20on%20Ubuntu/badge.svg)
![GitHub Action (CI on MacOS)](https://github.com/p-org/P/workflows/CI%20on%20MacOS/badge.svg)

P is a state machine based programming language for modeling and specifying complex
distributed systems. P allows programmers to model their system as a collection of
communicating state machines. P supports several backend analysis engines
(based on automated reasoning techniques like model
checking and symbolic execution) to check that the distributed system modeled in P
satisfies the desired correctness specifications. Not only can a P program be systematically
tested (e.g., with model checking), but it can also be compiled into executable code.
Essentially, P unifies modeling, specifying, implementing, and testing into one activity for the
programmer.

P is currently being used extensively inside Amazon (AWS) for analysis of
complex distributed systems. P is also being used for programming safe robotics systems. P
was first used to implement and validate the USB device driver stack that ships with
Microsoft Windows 8 and Windows Phone.

!!! quote ""
    :sparkles: **_Programming concurrent, distributed systems is fun but challenging, however, a pinch of programming language design with a dash of automated reasoning can go a long way in addressing the challenge and amplifying the fun!._** :sparkles:

## Let the fun begin!

You can find most of the information about the P framework on this webpage:
[what is P?](whatisP.md),  
[getting started](getstarted/install.md), [tutorials](tutsoutline.md),
[case studies](casestudies.md) and related [research publications](publications.md). If
you have any further questions, please feel free to create an
[issue](https://github.com/p-org/P/issues), ask on
[discussions](https://github.com/p-org/P/discussions), or
[email us](mailto:ankushdesai@gmail.com)

!!! info "Contributions"  
    _P has always been a collaborative project between industry and academia (since 2013)
    :drum:. The P team welcomes contributions and suggestions from all of you!! :punch:._

