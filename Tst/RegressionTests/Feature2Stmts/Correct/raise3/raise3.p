/********************
 * This example tests raise with payload on monitor
 * ******************/

 event E1;
 event E2: int;

 machine Main {
  start state Init {
    entry {
      send this, E1;
    }
    on E1 do {
    }
  }
}
 spec M observes E1, E2 {
  start state Init {
    on E1 do {
      testFunc();
      assert false;
    }
    on E2 do (payload: int) { 
      assert (payload == 1000), format ("Expected payload to be 1000, actual payload is {0}", payload);
    }
  }
  fun testFunc() {
    raise E2, 1000;
  }
}

test DefaultImpl [main=Main]: assert M in { Main };