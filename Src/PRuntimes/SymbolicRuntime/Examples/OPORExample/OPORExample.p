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

	on eReadIndexReq do (pld: (int, machine)) {
		send pld.1, eReadResp, array[pld.0];
	}

	on eUpdateIndexReq do (pld: (int, int)){
		array[pld.0] = pld.1;
	}
   }
}


machine ThreadZero {
        var read:int;
	start state Init {
		entry (pld : (m:machine, n:int)) {
                        var i:int;
                        i = pld.n;
                        read = 1; 
                        while (read != 0) {
                           ZeroReadAtIndex(pld.m, i, this);
                           i = i - 1;
                        }
		}

	}
        fun ZeroReadAtIndex(s: machine, i: int, thread: machine) {
        	send s, eReadIndexReq, (i, thread);
        	receive {
        		case eReadResp: (val: int) {
        			read = val;
        		}
        	}
        }

}

machine ThreadI {
        var read:int;
	
	start state Init {
		entry(pld : (m:machine, j:int)) {
                        ReadAtIndex(pld.m, pld.j - 1, this);
                        UpdateAtIndex(pld.m, pld.j, read + 1);
		}

	}

        fun ReadAtIndex(s: machine, i: int, thread: machine) {
        	send s, eReadIndexReq, (i, thread);
        	receive {
        		case eReadResp: (val: int) {
        			read = val;
        		}
        	}
        }

        fun UpdateAtIndex(sharedArray: machine, i: int, val: int) {
        	send sharedArray, eUpdateIndexReq, (i, val);
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
