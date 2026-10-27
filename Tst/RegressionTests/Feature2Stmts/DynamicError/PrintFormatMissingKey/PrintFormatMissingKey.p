// A print argument that fails must fail even when the checker does not format the message.
machine Main {
  var m: map[int, int];
  start state Init {
    entry {
      m[1] = 10;
      print format("value {0}", m[2]);
    }
  }
}
