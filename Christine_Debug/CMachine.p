param g1 : int;
param g2 : int;
param g3 : int;
param g4 : int;

machine Main {
	var M: machine;
	start state Init {
		entry {
			var mySeq: seq[int];
			mySeq += (element);           // Append element using +=
			mySeq += (index, element); 
		}
	}
}


// test BasicNonParametricTest [main=Main]:{Main};

test param (g1 in [-1, 2]) FullCartesianProductParametricTest [main=Main]:{Main};

// test param (g1 in [1, 2], g2 in [3, 4], g3 in [5, 6], g4 in [7, 8]) assume (g1 + g2 < g3 + g4) ParametricTestWithAssumption [main=Main]:{Main};

param b1: bool;

// test param (g1 in [1, 2], g2 in [3, 4], g3 in [5, 6], g4 in [7, 8], b1 in [true, false]) assume (b1 == (g1 + g2 > g3 + g4)) ParametricTestWithBoolAssumption [main=Main]:{Main};

// test param (g1 in [3, 1], g2 in [3, 5], g3 in [5, 6], g4 in [7, 8], b1 in [true, false]) (2 wise) TWisePairwiseCoverageTest [main=Main]:{Main};