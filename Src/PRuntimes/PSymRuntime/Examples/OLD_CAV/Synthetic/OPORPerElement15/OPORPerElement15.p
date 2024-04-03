event eReadIndexReq: machine;
event eReadZeroIndexReq: machine;
event eReadResp: int;
event eUpdateIndexReq: int;

machine ArrayElement {
        var element: int;
	start state Init {
		entry { element = 0; }

	on eReadIndexReq do (m: machine) {
		send m, eReadResp, element;
	}

	on eReadZeroIndexReq do (m: machine) {
		send m, eReadResp, element;
	}

	on eUpdateIndexReq do (pld: int){
		element = pld;
	}
   }
}

machine ThreadZero {
        var array: seq[machine];
        var i:int;
	start state Init {
		entry (pld : (array:seq[machine], n:int)) {
                        array = pld.array;
                        i = pld.n;
                        send array[i], eReadZeroIndexReq, this;
                        i = i - 1;
                        goto Loop;
		}

	}
        state Loop {
                on eReadResp do (res: int) {
                        if (res != 0) {
                           i = i - 1;
                           send array[i], eReadZeroIndexReq, this;
                        }
                }

        }

}

machine ThreadI {
        var prev:machine;
        var next:machine;
	
	start state Init {
		entry(pld : (prev:machine, next:machine)) {
                        prev = pld.prev;
                        next = pld.next;
                        send prev, eReadIndexReq, this;
                        goto Update;
		}
	}
        state Update {
                on eReadResp do (res: int) {
        	        send next, eUpdateIndexReq, res + 1;
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
               var i:int;
               var array:seq[machine];
               n = 15;
               j = 1;
               i = 0;
               while (i < n + 1) {
                   array += (i, new ArrayElement());
                   i = i + 1;
               }
               new ThreadZero((array = array, n = n));
               //new ThreadI((prev = array[0], next = array[0]));
               while (j < n + 1) {
                   new ThreadI((prev = array[j - 1], next = array[j]));
                   j = j + 1;
              }
           }
       }
}
