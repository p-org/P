// 1.7.9.  "null" event cannot be declared (parse error)

event null assert 1;
event E1 assert 1;
main machine Real1 {
    var test: bool;  //init with "false"
    start state Real1_Init {
        entry {
			raise null;
			raise E1;
	        }
		exit {   }

		on E1 do {send this, null; };
		on default do {assert(false);};      
	}
	fun Action2() {
		assert(false);
    }
}
