param g1 : int;
param g2 : int;
param g3 : int;
param g4 : int;
param g5 : bool;

machine Main {
		var M: machine;
		start state Init {
				entry {
						print format("global variable g1 = {0}", g1);
						print format("global variable g2 = {0}", g2);
						print format("global variable g3 = {0}", g3);
						print format("global variable g4 = {0}", g4);
						print format("global variable g5 = {0}", g5);
				}
		}
}


test BasicNonParametricTest [main=Main]:{Main};

test param (g1 in [-1, 2, 5], g2 in [3, 4], g3 in [5, 6], g4 in [7, 8], g5 in [true, false]) FullCartesianProductParametricTest [main=Main]:{Main};

test param (g1 in [1, -2], g2 in [3, 4, 2], g3 in [5, 6], g4 in [7, 8], g5 in [true, false]) assume (g1 + g2 < g3 + g4) ParametricTestWithAssumption [main=Main]:{Main};

param b1: bool;

test param (g1 in [1, 2], g2 in [-3, 4], g3 in [5, 6], g4 in [7, 8], b1 in [true, false], g5 in [true, false]) assume (b1 == (g1 + g2 > g3 + g4)) ParametricTestWithBoolAssumption [main=Main]:{Main};

test param (g1 in [1, 2], g2 in [3, -4], g3 in [5, 3, 20], g4 in [7, 8], b1 in [true, false], g5 in [true, false]) (1 wise) TWise1CoverageTest [main=Main]:{Main};

test param (g1 in [1, 2], g2 in [3, 4], g3 in [-5, 6], g4 in [7, 8, 10], b1 in [true, false], g5 in [true, false]) (2 wise) TWise2CoverageTest [main=Main]:{Main};

test param (g1 in [1, 2], g2 in [3, 4], g3 in [5, -6], g4 in [7, 8], b1 in [true, false], g5 in [true, false]) (3 wise) TWise3CoverageTest [main=Main]:{Main};