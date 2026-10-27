// P values are copied when stored, passed or sent. A field, element or chosen value
// that is passed to a function, put in a tuple, sent, raised or announced must be a
// copy: changing it afterwards (on either side) must not change the original.
event eSeq: seq[int];
event eNamed: (x: seq[int], y: int);
event eAnnounced: seq[int];
event eDone;

spec Changer observes eAnnounced {
  start state Watch {
    // Monitors may change their payload parameter; the announcer must not see it.
    on eAnnounced do (p: seq[int]) { p += (0, 100); }
  }
}

fun Grow(s: seq[int]) { s += (0, 9); }

machine Main {
  var t: (a: seq[int], b: int);
  var m: map[int, seq[int]];
  var ss: seq[seq[int]];
  var st: set[seq[int]];
  var km: map[seq[int], int];
  var r: Receiver;

  start state Init {
    entry {
      var u: (x: seq[int], y: int);
      var p: (seq[int], int);
      var s: seq[int];
      var k: seq[int];

      // Function arguments.
      Grow(t.a);
      assert sizeof(t.a) == 0, "argument aliases a tuple field";
      m[0] = s;
      Grow(m[0]);
      assert sizeof(m[0]) == 0, "argument aliases a map value";
      ss += (0, s);
      Grow(ss[0]);
      assert sizeof(ss[0]) == 0, "argument aliases a sequence element";
      Grow(choose(ss));
      assert sizeof(ss[0]) == 0, "argument aliases a chosen element";

      // Tuple fields.
      u = (x = t.a, y = 1);
      u.x += (0, 5);
      assert sizeof(t.a) == 0, "named tuple field aliases a tuple field";
      u = (x = m[0], y = 1);
      u.x += (0, 5);
      assert sizeof(m[0]) == 0, "named tuple field aliases a map value";
      p = (ss[0], 2);
      p.0 += (0, 7);
      assert sizeof(ss[0]) == 0, "tuple field aliases a sequence element";

      // Choose.
      s = choose(ss);
      s += (0, 1);
      assert sizeof(ss[0]) == 0, "chosen value aliases a sequence element";

      // Announce.
      s = default(seq[int]);
      announce eAnnounced, s;
      assert sizeof(s) == 0, "announced payload aliases a variable";
      announce eAnnounced, t.a;
      assert sizeof(t.a) == 0, "announced payload aliases a tuple field";

      // Collections.
      ss += (1, t.a);
      ss[1] += (0, 1);
      assert sizeof(t.a) == 0, "sequence element aliases a tuple field";
      m += (1, ss[0]);
      m[1] += (0, 1);
      assert sizeof(ss[0]) == 0, "map value aliases a sequence element";

      // Set elements and map keys are frozen when stored, but not their source.
      st += (t.a);
      t.a += (0, 2);
      st += (ss[0]);
      ss[0] += (0, 3);
      km += (t.a, 1);
      t.a += (0, 4);
      km[ss[0]] = 2;
      ss[0] += (0, 5);
      assert sizeof(st) == 1 && default(seq[int]) in st, "set element changed with its source";
      k += (0, 2);
      assert sizeof(km) == 2 && km[k] == 1, "map key changed with its source";
      t.a = default(seq[int]);
      ss[0] = default(seq[int]);

      // Raise.
      raise eSeq, t.a;
    }

    on eSeq do (pl: seq[int]) {
      pl += (0, 3);
      assert sizeof(t.a) == 0, "raised payload aliases a tuple field";
      goto Sending;
    }
  }

  state Sending {
    entry {
      r = new Receiver();
      // The sender changes its values after sending them.
      send r, eSeq, t.a;
      send r, eNamed, (x = t.a, y = 1);
      send r, eSeq, m[0];
      send r, eSeq, ss[0];
      t.a += (0, 1);
      m[0] += (0, 1);
      ss[0] += (0, 1);
      send r, eDone;
    }
  }
}

machine Receiver {
  var received: seq[seq[int]];

  start state Init {
    on eSeq do (pl: seq[int]) {
      assert sizeof(pl) == 0, format("payload changed after it was sent: {0}", pl);
      received += (sizeof(received), pl);
    }
    on eNamed do (pl: (x: seq[int], y: int)) {
      assert sizeof(pl.x) == 0, format("payload changed after it was sent: {0}", pl);
      received += (sizeof(received), pl.x);
    }
    on eDone do {
      var i: int;
      while (i < sizeof(received)) {
        assert sizeof(received[i]) == 0, format("received payload {0} changed: {1}", i, received[i]);
        i = i + 1;
      }
    }
  }
}

test DefaultImpl [main=Main]: assert Changer in { Main, Receiver };
