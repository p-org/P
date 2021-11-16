event ePing: int;

machine Server {
        var ctr:int;
	start state Init {
		entry { ctr = 0; }
	        on ePing do (id:int) {
                    if (id / 2 > ctr) {
                      ctr = ctr - (id / 2);
                    } else {
                      ctr = ctr + id;
                    }
            	}
        }

   }

machine Writer {
	start state Init {
		entry(pld : (server:machine, id:int)) {
                        send pld.server, ePing, pld.id;
		}
	}
}

machine Main {
       start state Init {
           entry {
               var n:int;
               var j:int;
               var i:int;
               var server:machine;
               server = new Server();
               n = 5;
               i = 1;
               while (i < n) {
                   new Writer((server = server, id = i));
                   i = i + 1;
              }
           }
       }
}
