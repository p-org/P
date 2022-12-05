event eDefer;
event eTransition;

machine Main {
  start state Init {
    entry {
      send this, eDefer;
      send this, eTransition;
    }

    defer eDefer;
    on eTransition goto Process;
  }

  state Process {
    on eDefer goto Done;
  }

  state Done {}
}
