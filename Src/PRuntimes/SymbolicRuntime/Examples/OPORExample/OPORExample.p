event eReadIndexReq: (int, machine);
event eReadResp: int;
event eUpdateIndexReq: (int, int);

machine SharedArray {
	var array: seq[int];
	start state Init {
		entry(n:int) {
                        var i:int;
                        i = 0;
                        while (i < n) {
                            array += (i,0);
                            i = i + 1;
                        }
		}

	on eReadIndexReq do (payload: (int, machine)) {
		send payload.1, eReadResp, array[payload.0];
	}

	on eUpdateIndexReq do (payload: (int, int)){
		array[payload.0] = payload.1;
	}
   }
}

fun ReadAtIndex(s: machine, i: int, thread: machine) : int{
	var ret: int;
	send s, eReadIndexReq, (i, thread);
	receive {
		case eReadResp: (val: int) {
			ret = val;
		}
	}
	return ret;
}

fun UpdateAtIndex(sharedArray: machine, i: int, val: int) {
	var ret: int;
	send sharedArray, eUpdateIndexReq, (i, val);
}


machine ThreadZero {
	start state Init {
		entry (payload : (m:machine, n:int)) {
                        var i:int;
                        var read:int;
                        i = payload.n;
                        read = 1; 
                        while (read != 0) {
                           read = ReadAtIndex(payload.m, i, this);
                           i = i - 1;
                        }
		}

	}
}

machine ThreadI {
	
	start state Init {
		entry(payload : (m:machine, j:int)) {
                        var read:int;
                        read = ReadAtIndex(payload.m, payload.j - 1, this);
                        UpdateAtIndex(payload.m, payload.j, read + 1);
		}

	}
}

machine Main {
	
	// create the SharedArray machine
	// pass the reference of SharedArray to the thread machines
	// create 1 thread0 and i threadI machines

       start state Init {
           entry {
               var n:int;
               var j:int;
               var array:machine;
               n = 5;
               j = 1;
               array = new SharedArray(n + 1);
               new ThreadZero((m = array, n = n));
               while (j < n + 1) {
                   new ThreadI((m = array, j = j));
                   j = j + 1;
              }
           }
       }
}
