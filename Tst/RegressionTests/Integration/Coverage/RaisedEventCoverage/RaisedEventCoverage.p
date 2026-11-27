// Recv handles a raised eX in A, then an eX sent by Sender in B. Sender is created in B, so the
// send always follows the raise. The activity coverage graph must link the second eX to Sender.

event eX;

machine Recv {
  start state A {
    entry { raise eX; }
    on eX goto B;
  }

  state B {
    entry { new Sender(this); }
    on eX goto C;
  }

  state C {
    ignore eX;
  }
}

machine Sender {
  start state S {
    entry (r: Recv) { send r, eX; }
  }
}

test tcRaisedEventCoverage [main=Recv]: { Recv, Sender };
