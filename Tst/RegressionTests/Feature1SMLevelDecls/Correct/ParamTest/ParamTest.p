param g1 : int;
param g2 : int;
param g3 : int;
param g4 : int;

machine Main {
	var M: machine;
	start state Init {
		entry {
			print format("global varaible g1 = {0}", g1);
      print format("global varaible g2 = {0}", g2);
			print format("global varaible g3 = {0}", g3);
			print format("global varaible g4 = {0}", g4);
		}
	}
}


test BasicNonParametricTest [main=Main]:{Main};

test param (g1 in [1, 2], g2 in [3, 4], g3 in [5, 6], g4 in [7, 8]) FullCartesianProductParametricTest [main=Main]:{Main};

test param (g1 in [1, 2], g2 in [3, 4], g3 in [5, 6], g4 in [7, 8]) assume (g1 + g2 < g3 + g4) ParametricTestWithAssumption [main=Main]:{Main};

param b1: bool;

test param (g1 in [1, 2], g2 in [3, 4], g3 in [5, 6], g4 in [7, 8], b1 in [true, false]) assume (b1 == (g1 + g2 > g3 + g4)) ParametricTestWithBoolAssumption [main=Main]:{Main};

test param (g1 in [1, 2], g2 in [3, 4], g3 in [5, 6], g4 in [7, 8], b1 in [true, false]) (1 wise) TWise1CoverageTest [main=Main]:{Main};

test param (g1 in [1, 2], g2 in [3, 4], g3 in [5, 6], g4 in [7, 8], b1 in [true, false]) (2 wise) TWise2CoverageTest [main=Main]:{Main};

test param (g1 in [1, 2], g2 in [3, 4], g3 in [5, 6], g4 in [7, 8], b1 in [true, false]) (3 wise) TWise3CoverageTest [main=Main]:{Main};