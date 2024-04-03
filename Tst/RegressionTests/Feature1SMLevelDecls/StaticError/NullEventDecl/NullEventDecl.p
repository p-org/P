// 1.7.7.  "null" event cannot be declared (parse error)

event null assert 1;  //error
machine Main {
    var XYZ: bool;  //init with "false"
    start state Real1_Init {
        entry {
			raise null;
			raise E1;
	        }
		exit {   }

		on E1 do {send this, null; };
		on null do {assert(false);};      //unreachable
	}
	fun Action2() {
		assert(false);   //unreachable
    }
}
