//(16, 17): same name appears more than once in name tuple or named tuple type
//(16, 25): same name appears more than once in name tuple or named tuple type
//(16, 33): same name appears more than once in name tuple or named tuple type
//(16, 41): same name appears more than once in name tuple or named tuple type
//(17, 1): no start state in machine
//(18, 11): same name appears more than once in name tuple or named tuple type
//(18, 22): same name appears more than once in name tuple or named tuple type
//(19, 3): return value has incorrect type
//(34, 7): invalid payload type : event expects no payload
//(34, 22): same name appears more than once in name tuple or named tuple type
//(34, 29): same name appears more than once in name tuple or named tuple type
//(35, 19): same name appears more than once in name tuple or named tuple type
//(35, 26): same name appears more than once in name tuple or named tuple type

event x;
event y : seq [(a: int, a: int, a: int, a: int)];
main machine TestMachine {
  fun foo(x : event, x: event) {
		return true;
		//This is the correct return stmt (inferred return type is NIL):
		//return;
	}

/*
	fun foo1 (x : int, x: int) {
		return true;
	} */
}

machine Xsender {

	start state Init {
		entry {
      send this, x, (a = 1, a = 2);
	  send this, y, (a = 1, a = 2);
		}
	}

}