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
