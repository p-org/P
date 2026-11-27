// Functions that send or create are compiled as async methods, and the checker runs every
// operation on one thread. Their recursion must reach the depths that synchronous P functions
// reach: the sends at the bottom suspend the machine, and when it is resumed the calls complete
// back up through every level. With the default stack of a thread pool thread, this overflowed
// the stack, or .NET resumed the machine on another thread, which was reported as a bug.
event ePing;
event eDone: int;

machine Receiver {
  var pings: int;
  start state Counting {
    on ePing do { pings = pings + 1; }
    on eDone do (expected: int) {
      assert pings == expected, format("received {0} pings, expected {1}", pings, expected);
    }
  }
}

machine Main {
  var r: Receiver;
  var created: int;

  // Sends twice at the bottom, and every 500 levels on the way back up.
  fun SendAtBottom(n: int) : int {
    var count: int;
    if (n == 0) {
      send r, ePing;
      send r, ePing;
      return 2;
    }
    count = SendAtBottom(n - 1);
    if (n % 500 == 0) {
      send r, ePing;
      count = count + 1;
    }
    return count;
  }

  // Creates a machine at the bottom.
  fun CreateAtBottom(n: int) {
    if (n == 0) {
      new Receiver();
      created = created + 1;
      return;
    }
    CreateAtBottom(n - 1);
  }

  start state Init {
    entry {
      var depth: int;
      var count: int;
      depth = 1500;
      r = new Receiver();
      count = SendAtBottom(depth);
      assert count == 2 + depth / 500, format("sent {0} pings, expected {1}", count, 2 + depth / 500);
      CreateAtBottom(depth);
      assert created == 1, format("created {0} machines, expected 1", created);
      send r, eDone, count;
    }
  }
}
