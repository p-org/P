// 1.7.7.  "null" event cannot be sent (parse error)

event E1 assert 1;
main machine Real1 {
    var test: bool;  //init with "false"
    start state Real1_Init {
        entry {
			raise E1;
        }
		exit {   }

		on E1 do {send this, null;};
		on null do {assert(false);};      
	}
	fun Action2() {
		assert(false);
    }
}
