param g1 : int;

machine Main {
	var M: machine;
	start state Init {
		entry {
			print format("global variable g1 = {0}", g1);
		}
	}
}


test param (g1 in [1, 2]) (0 wise) TWise0CoverageTest [main=Main]:{Main};