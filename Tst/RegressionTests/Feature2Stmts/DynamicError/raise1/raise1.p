/********************
 * This example tests raise with payload on state machine
 * ******************/

 event E1: int;

 machine Main {
  start state Init {
    entry {
      raise E1, 1000;
    }
    on E1 do (payload: int) { 
      assert (payload != 1000), format ("Expected payload to be 1000, actual payload is {0}", payload);
    }
  }
}