// Foreign code sends and announces a payload, and changes it before it returns. Its sends and
// announces are performed when it returns to P code, but monitors and the target must observe
// each payload as it was when the foreign code sent or announced it.
event eSeq: seq[int];
event eAnnounced: seq[int];

fun ForeignSendAndChange(r: Receiver);

machine Main {
  start state Init {
    entry {
      var r: Receiver;
      r = new Receiver();
      ForeignSendAndChange(r);
      // Puts eSeq in the sends set of Main, for the foreign code.
      send r, eSeq, default(seq[int]);
    }
  }
}

machine Receiver {
  start state Init {
    on eSeq do (p: seq[int]) {
      assert sizeof(p) == 0, format("received payload changed after it was sent: {0}", p);
    }
  }
}

spec Unchanged observes eSeq, eAnnounced {
  start state Watch {
    on eSeq do (p: seq[int]) {
      assert sizeof(p) == 0, format("monitor saw a sent payload changed after it was sent: {0}", p);
    }
    on eAnnounced do (p: seq[int]) {
      assert sizeof(p) == 0, format("monitor saw an announced payload changed after it was announced: {0}", p);
    }
  }
}

test DefaultImpl [main=Main]: assert Unchanged in { Main, Receiver };
