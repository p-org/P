event x;
event y : seq [(a: int, a: int, a: int, a: int)];
main machine TestMachine {
  fun foo (x : event, x: event) {
		return true;
	}

/*
	fun foo (x : int, x: int) {
		return true;
	}*/
}

machine Xsender {

	start state Init {
		entry {
      send this, x, (a = 1, a = 2);
		}
	}

}