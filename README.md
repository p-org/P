P is a language for asynchronous event-driven programming. P allows the programmer to specify the system as a collection of interacting state machines, which communicate with each other using events. P unifies modeling and programming into one activity for the programmer. Not only can a P program be compiled into executable code, but it can also be validated using systematic testing. P allows the programmer to specify the environment, used to ``close'' the system, as nondeterministic ghost machines. Ghost machines are erased during compilation to executable code; a type system ensures that the erasure is semantics preserving. 

P has been used to implement and validate the USB device driver stack that ships with Microsoft Windows 8 and Windows Phone. We are currently investigating the applicability of P to the design and implementation of embedded and distributed systems.

Manual
=========

Check the [P manual](https://cdn.rawgit.com/p-org/P/master/Doc/Manual/pmanual.pdf) !

Publications
==========================================================

- [P: Safe asynchronous event-driven programming] (http://research.microsoft.com/pubs/191069/pldi212_desai.pdf), ACM SIGPLAN Conference on Programming Language Design and Implementation (PLDI), 2013.
