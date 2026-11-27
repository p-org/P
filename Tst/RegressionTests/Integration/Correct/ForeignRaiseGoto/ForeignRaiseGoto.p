// A raise or goto performed inside foreign code (or inside a P function that foreign code
// calls back) ends the P code that called the foreign function, exactly like a raise or goto
// written in P. Each state checks that the statements after the call never run.
event eStep;
event eBad;

fun ForeignRaise();
fun ForeignRaiseInt(): int;

machine Main {
  var x: int;
  start state Init {
    entry {
      ForeignRaise();
      assert false, "continued after a foreign raise";
    }
    on eStep goto ViaHelper;
  }

  state ViaHelper {
    entry {
      HelperRaise();
      assert false, "continued after a P function whose foreign callee raised";
    }
    on eStep goto AssignFromCall;
  }

  state AssignFromCall {
    entry {
      x = ForeignRaiseInt();
      raise eBad;
    }
    on eStep goto LocalGoto;
    on eBad do { assert false, "continued after a foreign raise in an assignment"; }
  }

  state LocalGoto {
    entry {
      ForeignGoto();
      assert false, "continued after a foreign goto";
    }
  }

  state Callback {
    entry {
      ForeignCallback();
      assert false, "continued after foreign code called back into a P goto";
    }
  }

  state Done {
    entry {
      assert x == 0, "the value of a call that raised was assigned";
    }
  }

  fun HelperRaise() {
    ForeignRaise();
    assert false, "continued in the P helper after a foreign raise";
  }

  fun GotoDone() {
    goto Done;
  }

  // Implemented in C#: goes to Callback.
  fun ForeignGoto();
  // Implemented in C#: calls GotoDone.
  fun ForeignCallback();
}
