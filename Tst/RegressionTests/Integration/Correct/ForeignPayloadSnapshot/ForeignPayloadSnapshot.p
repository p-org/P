// Foreign code sends, announces and creates a machine with a payload, and changes each payload
// before it returns. Its sends, announces and creates are performed when it returns to P code,
// but monitors, the target and the created machine must observe each payload as it was when the
// foreign code sent, announced or created with it.
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
      // Puts Created in the creates set of Main, for the foreign code.
      new Created(default(seq[int]));
    }
  }
}

machine Created {
  start state Init {
    entry (p: seq[int]) {
      assert sizeof(p) == 0, format("created machine got a payload changed after the create: {0}", p);
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

test DefaultImpl [main=Main]: assert Unchanged in { Main, Receiver, Created };
