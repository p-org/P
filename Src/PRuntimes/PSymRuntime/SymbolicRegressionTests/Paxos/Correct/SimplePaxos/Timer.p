//Functions for interacting with the timer machine
fun CreateTimer(owner : machine): machine {
	var m: machine;
	m = new Timer(owner);
	return m;
}

fun StartTimer(timer : machine, time: int) {
	send timer, START, 100;
}

fun CancelTimer(timer : machine) {
	send timer, CANCEL;
//  receive {
//    case CANCEL_SUCCESS: {}
//    case CANCEL_FAILURE: {
//      receive {
//        case TIMEOUT: {}
//      }
//    }
//  }
}


machine Timer {
  var client: machine;

  start state Init {
    entry (payload: machine) {
      client = payload;
      goto WaitForReq;
    }
  }

  state WaitForReq {
    on CANCEL goto WaitForReq;
    // with {
    //  send client, CANCEL_FAILURE, this;
    // }
    on START goto WaitForCancel;
  }

  state WaitForCancel {
    ignore START;
    on null goto WaitForReq with {
	  send client, TIMEOUT, this;
	}
    on CANCEL goto WaitForReq;
    //with {
    //  if ($) {
    //    send client, CANCEL_SUCCESS, this;
    //  } else {
    //    send client, CANCEL_FAILURE, this;
    //    send client, TIMEOUT, this;
    //  }
    //}
  }
}
