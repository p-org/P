// 1.7.7.  "null" event cannot be sent (parse error)

event E1 assert 1;
machine Main {
    var XYZ: bool;  //init with "false"
    start state Real1_Init {
        entry {
			raise E1;
        }
		exit {   }

		on E1 do {send this, null;}   //error
		on null do {assert(false);}   //unreachable
	}
	fun Action2() {
		assert(false);
    }
}
