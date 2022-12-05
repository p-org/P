/* foreign types */
type CustomInt;

/* foreign functions */
fun printInt(i: int);
fun changeInt(i: int): int;
fun convertToCustomInt(val: int): CustomInt;
fun convertFromCustomInt(val: CustomInt): int;
fun changeCustomInt(val: CustomInt): CustomInt;

machine Controller {
  var Follower0: machine;
  var Follower1: machine;

  fun debug() {
    var i: int;
    var j: int;
    var k: int;

    i = choose(5);
    printInt(i);
    j = changeInt(i);
    k = convertFromCustomInt(changeCustomInt(convertToCustomInt(j)));
    print format("i={0}, j={1}, k={2}", i, j, k);
  }

  start state Init {
    entry (payload: (_follower0: machine, _follower1: machine)) {
      Follower0 = payload._follower0;
      Follower1 = payload._follower1;
      debug();
      goto Send;
    }
  }
  state Send {
    entry {
      send Follower0, ping0, this;
      send Follower1, ping1, this;
      goto WaitPong1;
    }
  }
  state WaitPong1 {
    on pong0 goto WaitPong2;
    on pong1 goto WaitPong2;
  }
  state WaitPong2 {
    on pong0 goto Done;
    on pong1 goto Done;
  }
  state Done {}
}
