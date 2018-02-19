event x;
machine Main {
	var x1: int;
	start state Init {
		entry {
			x = foo();
			x1 = foo3();
		}
	}

	fun foo(x: any, y: int): int {
		return true;
	}

	fun foo1(x: any, y: int): int {
		return y;
	}

	fun foo2(x: any, y: int): int {
		return;
	}

	fun foo3(x: any, y: int) {
		//return;
	}
}

machine Xsender {
	start state Init {
		entry {
			return 3;
			send this, x;
		}
	}
}