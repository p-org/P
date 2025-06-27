param g1 : int;

machine Main {
	var M: machine;
	start state Init {
		entry {
			print format("global varaible g1 = {0}", g1);
		}
	}
}

test param (g1 in [2, 2]) Test1 [main=Main]:{Main};

