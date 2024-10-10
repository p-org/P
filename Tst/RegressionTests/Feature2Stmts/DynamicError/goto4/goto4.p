/********************
 * This example tests raise with payload on monitor
 * ******************/

 event E1;

 machine Main {
  start state Init {
    entry {
      send this, E1;
    }
    on E1 do {
    }
  }
}
 spec M observes E1 {
  start state Init {
    on E1 do {
      testFunc();
      assert false;
    }
  }
  state S {
    entry (payload: int) {
      assert (payload != 1000), format ("Expected payload to be 1000, actual payload is {0}", payload);
    }
  }
  fun testFunc() {
    goto S, 1000;
  }
}

test DefaultImpl [main=Main]: assert M in { Main };