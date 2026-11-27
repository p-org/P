// A call in a print argument runs even when the checker does not format the message.
machine Main {
  fun Fail(): int {
    assert false, "called from a print argument";
    return 0;
  }
  start state Init {
    entry {
      print format("value {0}", Fail());
    }
  }
}
