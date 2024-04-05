event Empty   assert 1           ;
event Sending assert 1 : machine ;
event Done    assert 1 : machine ;
event Unit    assert 0           ;
event Next    assert 1 : machine ;
event Send             : machine ;
event Ready                      ;

machine Node assume 100 {

  var IsSending    : bool;
  var NextMachine  : machine;
  var MyRing       : machine;

    start state Init_Main_Node {
			entry (payload: machine) { MyRing = payload; }
      on Next goto SetNext_Main_Node;
    }

    state Wait_Main_Node {
      on Empty goto SendEmpty_Main_Node;
      on Send goto StartSending_Main_Node;
      on Sending goto KeepSending_Main_Node;
      on Done goto StopSending_Main_Node;
    }

    state SetNext_Main_Node {
      entry (payload: machine) {
        NextMachine = payload;
        send MyRing, Ready;
        raise Unit;
      }

      on Unit goto Wait_Main_Node;
    }

    state SendEmpty_Main_Node {
      entry {
        send NextMachine, Empty;
        raise Unit;
      }

      on Unit goto Wait_Main_Node;
    }

    state StartSending_Main_Node {
      entry (payload: machine) {
        IsSending = true;
        send NextMachine, Sending, payload;
        raise Unit;
      }

      on Unit goto Wait_Main_Node;
    }

    state KeepSending_Main_Node {
      entry (payload: machine) {
        if (payload == this)
          send NextMachine, Done, this;
        else
          send NextMachine, Sending, payload;
        raise unit;
      }

      on Unit goto Wait_Main_Node;
    }

    state StopSending_Main_Node {
      entry (payload: machine) {
        if (IsSending == true)
          send NextMachine, Empty;
        else
          send NextMachine, Done, payload;
        raise Unit;
      }

      on Unit goto Wait_Main_Node;
    }
}
