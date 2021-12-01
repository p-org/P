event e1;
event e2: int;

machine Main {
  var x: int;

  start state State0 {
    entry {
      x = 1;
      raise e1;
      x = 2;
    }

    on e1 goto State1;
  }

  state State1 {
    entry {
      assert x == 1;
      raise e2, 3;
      x = 4;
    }

    on e2 do (payload: int) {
      assert x == 1;
      assert payload == 3;
      goto State2;
      x = 5;
    }
  }

  state State2 {
    entry {
      assert x == 1;
      x = 6;
      x = raise_e1_nondet();
      assert x == 7;
      x = 8;
      goto3();
    }

    on e1 do {
      assert x == 6;
      x = 9;
      goto3();
    }
  }

  fun goto3() {
    goto State3;
    x = 10;
  }

  state State3 {
    entry {
      assert x == 8 || x == 9;
      x = 10;
      if ($) {
        goto State4, 11;
        x = 12;
      } else {
        raise e2, 13;
        x = 14;
      }
    }

    on e2 goto State4;
  }

  state State4 {
    entry (payload: int) {
      assert payload == 11 || payload == 13;
      assert x == 10;
    }
  }
}

fun raise_e1_nondet(): int {
  if ($) {
    return 7;
  } else {
    raise e1;
    return 100;
  }
}
