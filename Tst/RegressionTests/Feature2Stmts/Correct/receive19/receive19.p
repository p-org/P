//Testing variable scoping in nested "receive"

event e1 : int;

machine Main {
	var m: machine;
	start state Init {
		entry {
			m = new Receiver();
			send m, e1, 1;
			send m, e1, 2;
		}
	}
}

machine Receiver {
	start state Init {
		entry {
			var i : int;
			var x : int;
			while (i < 2) {
				receive {
					case e1: (xx : int) {
						if (xx == 2) {
							return;
						}
						x = xx;
					}
				}
				print format("x = {0}\n", x);
				i = i + 1;
			}
			print "done!\n";
		}
	}
}