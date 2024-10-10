/********************
 * This example tests goto with payload on state machine
 * ******************/

 machine Main {
  start state Init {
    entry {
      goto S, 1000;
    }
  }
  state S {
    entry (payload: int) { 
      assert (payload != 1000), format ("Expected payload to be 1000, actual payload is {0}", payload);
    }
  }
}