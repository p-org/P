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
        raise Unit;
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

machine Main {

		    var N1 : machine;
		    var N2 : machine;
		    var N3 : machine;
		    var N4 : machine;
		    var ReadyCount : int;
		    var Rand1 : bool;
		    var Rand2 : bool;
		    var RandSrc : machine;
		    var RandDst : machine;

    start state Boot_Main_Ring4 {
      entry {
        N1 = new Node(this);
        N2 = new Node(this);
        N3 = new Node(this);
        N4 = new Node(this);
        send N1, this, N2;
        send N2, Next, N3;
        send N3, Next, N4;
        send N4, Next, N1;
        ReadyCount = -1;
        raise Unit;
      }

      defer Ready;
      on Unit goto Stabilize_Main_Ring4;
    }

    state Stabilize_Main_Ring4 {
      entry {
        ReadyCount = ReadyCount + 1;
        if (ReadyCount == 4)
          raise Unit;
      }

      on Ready goto Stabilize_Main_Ring4;
      on Unit goto RandomComm_Main_Ring4;
    }

    state RandomComm_Main_Ring4 {
      entry {
                if ($)
                  Rand1 = true;
                else
                  Rand1 = false;
                if ($)
                  Rand2 = true;
                else
                  Rand2 = false;

                if (!Rand1 && !Rand2)
                   RandSrc = N1;
                if (!Rand1 && Rand2)
                   RandSrc = N2;
                if ((Rand1 && !Rand2) == 1)
                   RandSrc = N3;
                else
                   RandSrc = N4;
                if ($)
                  Rand1 = true;
                else
                  Rand1 = false;
                if ($)
                  Rand2 = true;
                else
                  Rand2 = false;
                if (!Rand1 && !Rand2)
                   RandDst = N1;
                if (!Rand1 && Rand2)
                   RandDst = N2;
                if (Rand1 && !Rand2)
                   RandDst = N3;
                else
                   RandDst = N4;

                send RandSrc, Send, RandDst;
                raise unit;
      }

      on Unit goto RandomComm_Main_Ring4;
    }
}
